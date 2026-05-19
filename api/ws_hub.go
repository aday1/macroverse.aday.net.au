package main

import (
	"encoding/json"
	"net/http"
	"strings"
	"sync"
	"time"

	"github.com/gorilla/websocket"
)

var wsUpgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool { return true },
}

type wsConn struct {
	conn       *websocket.Conn
	role       string
	sessionID  string
	bridgeID   string
	leader     bool
	canControl bool
	send       chan []byte
}

type vjControlState struct {
	Crossfader       float64                `json:"crossfader"`
	MixMode          string                 `json:"mixMode"`
	DeckAGlobalIndex int                    `json:"deckAGlobalIndex"`
	DeckBGlobalIndex int                    `json:"deckBGlobalIndex"`
	PageA            int                    `json:"pageA"`
	PageB            int                    `json:"pageB"`
	ParamsA          map[string]interface{} `json:"paramsA"`
	ParamsB          map[string]interface{} `json:"paramsB"`
	FlipV            bool                   `json:"flipV"`
	FlipH            bool                   `json:"flipH"`
	Rotation         int                    `json:"rotation"`
	AutoVjEnabled    bool                   `json:"autoVjEnabled"`
	AutoVjBpm        float64                `json:"autoVjBpm"`
}

type vjControlPatch struct {
	Crossfader       *float64               `json:"crossfader,omitempty"`
	MixMode          *string                `json:"mixMode,omitempty"`
	DeckAGlobalIndex *int                   `json:"deckAGlobalIndex,omitempty"`
	DeckBGlobalIndex *int                   `json:"deckBGlobalIndex,omitempty"`
	PageA            *int                   `json:"pageA,omitempty"`
	PageB            *int                   `json:"pageB,omitempty"`
	ParamsA          map[string]interface{} `json:"paramsA,omitempty"`
	ParamsB          map[string]interface{} `json:"paramsB,omitempty"`
	FlipV            *bool                  `json:"flipV,omitempty"`
	FlipH            *bool                  `json:"flipH,omitempty"`
	Rotation         *int                   `json:"rotation,omitempty"`
	AutoVjEnabled    *bool                  `json:"autoVjEnabled,omitempty"`
	AutoVjBpm        *float64               `json:"autoVjBpm,omitempty"`
}

type sessionRoom struct {
	clients    map[*wsConn]bool
	control    vjControlState
	bridge     *wsConn
	leader     *wsConn
	clockBpm   float64
	clockBeat  float64
	clockBar   float64
	clockPlay  bool
	linkPeers  int
}

var (
	wsHub struct {
		sync.Mutex
		rooms map[string]*sessionRoom
	}
)

func wsHubInit() {
	wsHub.rooms = make(map[string]*sessionRoom)
}

func getRoom(sessionID string) *sessionRoom {
	sid := normalizeVJSessionID(sessionID)
	wsHub.Lock()
	defer wsHub.Unlock()
	r, ok := wsHub.rooms[sid]
	if !ok {
		r = &sessionRoom{
			clients: make(map[*wsConn]bool),
			control: vjControlState{MixMode: "crossfade", AutoVjBpm: 120},
		}
		wsHub.rooms[sid] = r
	}
	return r
}

func (r *sessionRoom) broadcast(sessionID string, msg interface{}, except *wsConn) {
	data, err := json.Marshal(msg)
	if err != nil {
		return
	}
	wsHub.Lock()
	defer wsHub.Unlock()
	for c := range r.clients {
		if except != nil && c == except {
			continue
		}
		select {
		case c.send <- data:
		default:
		}
	}
	if r.bridge != nil && (except == nil || r.bridge != except) {
		select {
		case r.bridge.send <- data:
		default:
		}
	}
	_ = sessionID
}

func wsHandleConnection(w http.ResponseWriter, r *http.Request) {
	conn, err := wsUpgrader.Upgrade(w, r, nil)
	if err != nil {
		return
	}
	c := &wsConn{
		conn: conn,
		role: "client",
		send: make(chan []byte, 256),
	}
	go c.writePump()
	c.readPump()
}

func (c *wsConn) writePump() {
	defer c.conn.Close()
	for msg := range c.send {
		if err := c.conn.WriteMessage(websocket.TextMessage, msg); err != nil {
			break
		}
	}
}

func (c *wsConn) readPump() {
	defer func() {
		c.disconnect()
		c.conn.Close()
	}()
	c.conn.SetReadLimit(512 * 1024)
	_ = c.conn.SetReadDeadline(time.Now().Add(60 * time.Second))
	c.conn.SetPongHandler(func(string) error {
		return c.conn.SetReadDeadline(time.Now().Add(60 * time.Second))
	})
	for {
		_, data, err := c.conn.ReadMessage()
		if err != nil {
			break
		}
		_ = c.conn.SetReadDeadline(time.Now().Add(60 * time.Second))
		c.handleMessage(data)
	}
}

func (c *wsConn) disconnect() {
	if c.sessionID == "" {
		return
	}
	room := getRoom(c.sessionID)
	wsHub.Lock()
	delete(room.clients, c)
	if room.leader == c {
		room.leader = nil
		for other := range room.clients {
			if other.role == "client" {
				room.leader = other
				other.leader = true
				break
			}
		}
	}
	if room.bridge == c {
		room.bridge = nil
	}
	wsHub.Unlock()
}

func (c *wsConn) handleMessage(data []byte) {
	var envelope struct {
		Type      string          `json:"type"`
		SessionID string          `json:"sessionId"`
		Token     string          `json:"token"`
		Role      string          `json:"role"`
		Payload   json.RawMessage `json:"payload"`
	}
	if err := json.Unmarshal(data, &envelope); err != nil {
		return
	}
	switch envelope.Type {
	case "auth":
		c.handleAuth(envelope.Token, envelope.Role, envelope.SessionID)
	case "session:join":
		c.joinSession(envelope.SessionID)
	case "vj:claim-leader":
		c.claimLeader()
	case "vj:control":
		c.handleVjControl(envelope.Payload)
	case "vj:config":
		c.handleVjConfig(envelope.Payload)
	case "vj:frame", "vj:shader", "vj:clear":
		c.relayVjOutput(envelope.Type, data)
	case "bridge:hello":
		c.handleBridgeHello(envelope.Payload)
	case "bridge:heartbeat":
		// noop
	case "bridge:clock:state":
		c.handleBridgeClock(envelope.Payload)
	case "bridge:osc:in":
		c.handleBridgeOscIn(envelope.Payload)
	case "clock:request":
		c.requestBridgeClock()
	}
}

func (c *wsConn) handleAuth(token, role, sessionID string) {
	if role == "bridge" {
		payload, err := verifyBridgeToken(token)
		if err != nil {
			c.sendJSON(map[string]interface{}{"type": "auth:error", "message": "invalid token"})
			return
		}
		c.role = "bridge"
		c.bridgeID = payload.BridgeID
		c.sessionID = payload.SessionID
		c.canControl = false
	} else if strings.TrimSpace(token) != "" {
		payload, err := verifyVjToken(strings.TrimSpace(token), vjRoleOperator)
		if err != nil {
			c.sendJSON(map[string]interface{}{"type": "auth:error", "message": "invalid control token"})
			return
		}
		c.role = "client"
		c.sessionID = payload.SessionID
		c.canControl = true
	} else {
		c.role = "client"
		c.sessionID = normalizeVJSessionID(sessionID)
		c.canControl = !vjRequireControlToken()
	}
	c.joinSession(c.sessionID)
	c.sendJSON(map[string]interface{}{
		"type":       "auth:ok",
		"role":       c.role,
		"sessionId":  c.sessionID,
		"canControl": c.canControl,
	})
}

func (c *wsConn) joinSession(sessionID string) {
	if sessionID == "" {
		return
	}
	c.sessionID = normalizeVJSessionID(sessionID)
	room := getRoom(c.sessionID)
	wsHub.Lock()
	room.clients[c] = true
	if c.role == "bridge" {
		room.bridge = c
	} else if room.leader == nil {
		room.leader = c
		c.leader = true
	}
	ctrl := room.control
	wsHub.Unlock()

	c.sendJSON(map[string]interface{}{
		"type":                  "session:joined",
		"sessionId":             c.sessionID,
		"leader":                c.leader,
		"canControl":            c.canControl,
		"control":               ctrl,
		"audienceParticipation": getAudienceParticipation(c.sessionID),
	})
}

func (c *wsConn) claimLeader() {
	if c.role != "client" || c.sessionID == "" || !c.canControl {
		return
	}
	room := getRoom(c.sessionID)
	wsHub.Lock()
	for client := range room.clients {
		client.leader = false
	}
	room.leader = c
	c.leader = true
	wsHub.Unlock()
	room.broadcast(c.sessionID, map[string]interface{}{"type": "vj:leader", "socketId": c.conn.RemoteAddr().String()}, nil)
}

func (c *wsConn) handleVjConfig(raw json.RawMessage) {
	if c.sessionID == "" || !c.canControl {
		return
	}
	var cfg struct {
		AudienceParticipation *bool `json:"audienceParticipation"`
	}
	if err := json.Unmarshal(raw, &cfg); err != nil {
		return
	}
	if cfg.AudienceParticipation == nil {
		return
	}
	setAudienceParticipation(c.sessionID, *cfg.AudienceParticipation)
	room := getRoom(c.sessionID)
	room.broadcast(c.sessionID, map[string]interface{}{
		"type":                  "vj:config",
		"sessionId":             c.sessionID,
		"audienceParticipation": *cfg.AudienceParticipation,
	}, c)
}

func broadcastVjAudienceMouse(sessionID string, mouseX, mouseY float64) {
	room := getRoom(sessionID)
	room.broadcast(sessionID, map[string]interface{}{
		"type":      "vj:audience-mouse",
		"sessionId": sessionID,
		"mouseX":    mouseX,
		"mouseY":    mouseY,
	}, nil)
}

func (c *wsConn) handleVjControl(raw json.RawMessage) {
	if c.sessionID == "" || !c.canControl {
		return
	}
	var patch vjControlPatch
	if err := json.Unmarshal(raw, &patch); err != nil {
		return
	}
	room := getRoom(c.sessionID)
	wsHub.Lock()
	mergeVjControlPatch(&room.control, &patch)
	ctrl := room.control
	wsHub.Unlock()
	room.broadcast(c.sessionID, map[string]interface{}{
		"type":      "vj:control",
		"sessionId": c.sessionID,
		"control":   ctrl,
	}, c)
}

func mergeVjControlPatch(dst *vjControlState, src *vjControlPatch) {
	if src.MixMode != nil {
		dst.MixMode = *src.MixMode
	}
	if src.DeckAGlobalIndex != nil {
		dst.DeckAGlobalIndex = *src.DeckAGlobalIndex
	}
	if src.DeckBGlobalIndex != nil {
		dst.DeckBGlobalIndex = *src.DeckBGlobalIndex
	}
	if src.Crossfader != nil {
		dst.Crossfader = *src.Crossfader
	}
	if src.PageA != nil {
		dst.PageA = *src.PageA
	}
	if src.PageB != nil {
		dst.PageB = *src.PageB
	}
	if src.FlipV != nil {
		dst.FlipV = *src.FlipV
	}
	if src.FlipH != nil {
		dst.FlipH = *src.FlipH
	}
	if src.Rotation != nil {
		dst.Rotation = *src.Rotation
	}
	if src.AutoVjEnabled != nil {
		dst.AutoVjEnabled = *src.AutoVjEnabled
	}
	if src.AutoVjBpm != nil && *src.AutoVjBpm > 0 {
		dst.AutoVjBpm = *src.AutoVjBpm
	}
	if src.ParamsA != nil {
		if dst.ParamsA == nil {
			dst.ParamsA = map[string]interface{}{}
		}
		for k, v := range src.ParamsA {
			dst.ParamsA[k] = v
		}
	}
	if src.ParamsB != nil {
		if dst.ParamsB == nil {
			dst.ParamsB = map[string]interface{}{}
		}
		for k, v := range src.ParamsB {
			dst.ParamsB[k] = v
		}
	}
}

func (c *wsConn) relayVjOutput(msgType string, data []byte) {
	if c.sessionID == "" || !c.canControl {
		return
	}
	room := getRoom(c.sessionID)
	wsHub.Lock()
	isLeader := room.leader == c || (room.leader == nil && c.role == "client")
	wsHub.Unlock()
	if !isLeader && msgType == "vj:frame" {
		return
	}
	vjOutputBroadcast(c.sessionID, string(data))
	room.broadcast(c.sessionID, json.RawMessage(data), c)
}

func (c *wsConn) handleBridgeHello(raw json.RawMessage) {
	if c.role != "bridge" {
		return
	}
	c.sendJSON(map[string]interface{}{"type": "bridge:paired", "sessionId": c.sessionID, "bridgeId": c.bridgeID})
}

func (c *wsConn) handleBridgeClock(raw json.RawMessage) {
	if c.role != "bridge" || c.sessionID == "" {
		return
	}
	var st struct {
		Bpm       float64 `json:"bpm"`
		Beat      float64 `json:"beat"`
		Bar       float64 `json:"bar"`
		IsPlaying bool    `json:"isPlaying"`
		LinkPeers int     `json:"linkPeers"`
	}
	if err := json.Unmarshal(raw, &st); err != nil {
		return
	}
	room := getRoom(c.sessionID)
	wsHub.Lock()
	room.clockBpm = st.Bpm
	room.clockBeat = st.Beat
	room.clockBar = st.Bar
	room.clockPlay = st.IsPlaying
	room.linkPeers = st.LinkPeers
	wsHub.Unlock()
	room.broadcast(c.sessionID, map[string]interface{}{
		"type":      "clock:state",
		"sessionId": c.sessionID,
		"bpm":       st.Bpm,
		"beat":      st.Beat,
		"bar":       st.Bar,
		"isPlaying": st.IsPlaying,
		"linkPeers": st.LinkPeers,
		"source":    "ableton-link",
	}, nil)
}

func (c *wsConn) handleBridgeOscIn(raw json.RawMessage) {
	if c.sessionID == "" {
		return
	}
	room := getRoom(c.sessionID)
	room.broadcast(c.sessionID, map[string]interface{}{
		"type":      "osc:event",
		"sessionId": c.sessionID,
		"payload":   json.RawMessage(raw),
	}, nil)
}

func (c *wsConn) requestBridgeClock() {
	if c.sessionID == "" {
		return
	}
	room := getRoom(c.sessionID)
	wsHub.Lock()
	b := room.bridge
	wsHub.Unlock()
	if b != nil {
		b.sendJSON(map[string]interface{}{"type": "bridge:clock:request"})
	}
}

func (c *wsConn) sendJSON(v interface{}) {
	data, err := json.Marshal(v)
	if err != nil {
		return
	}
	select {
	case c.send <- data:
	default:
	}
}

func listWSRooms() []map[string]interface{} {
	wsHub.Lock()
	defer wsHub.Unlock()
	out := make([]map[string]interface{}, 0, len(wsHub.rooms))
	for id, room := range wsHub.rooms {
		out = append(out, map[string]interface{}{
			"id":              id,
			"clientCount":     len(room.clients),
			"bridgeConnected": room.bridge != nil,
			"hasLeader":       room.leader != nil,
		})
	}
	return out
}
