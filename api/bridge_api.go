package main

import (
	"encoding/json"
	"net/http"
	"strings"
)

func handleBridgeToken(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "method not allowed", 405)
		return
	}
	var req struct {
		BridgeID  string `json:"bridgeId"`
		SessionID string `json:"sessionId"`
		ExpiresIn int    `json:"expiresIn"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "invalid json", 400)
		return
	}
	if strings.TrimSpace(req.BridgeID) == "" {
		req.BridgeID = "macroverse-bridge"
	}
	token, err := mintBridgeToken(req.BridgeID, req.SessionID, req.ExpiresIn)
	if err != nil {
		http.Error(w, err.Error(), 500)
		return
	}
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"token":     token,
		"bridgeId":  req.BridgeID,
		"sessionId": normalizeVJSessionID(req.SessionID),
	})
}

func handleBridgeStatus(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "method not allowed", 405)
		return
	}
	sessionID := normalizeVJSessionID(r.URL.Query().Get("sessionId"))
	room := getRoom(sessionID)
	wsHub.Lock()
	bridgeConnected := room.bridge != nil
	clientCount := len(room.clients)
	wsHub.Unlock()
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"sessionId":       sessionID,
		"bridgeConnected": bridgeConnected,
		"clientCount":     clientCount,
		"defaultSessionId": defaultVJSessionID,
	})
}

func listAllSessions() []map[string]interface{} {
	vj := listVJSessions()
	ws := listWSRooms()
	byID := make(map[string]map[string]interface{})
	for _, s := range vj {
		id, _ := s["id"].(string)
		byID[id] = map[string]interface{}{
			"id":              id,
			"streamClients":   s["clientCount"],
			"hasSignal":       s["hasSignal"],
			"wsClientCount":   0,
			"bridgeConnected": false,
		}
	}
	for _, s := range ws {
		id, _ := s["id"].(string)
		m, ok := byID[id]
		if !ok {
			m = map[string]interface{}{"id": id, "streamClients": 0, "hasSignal": false}
			byID[id] = m
		}
		m["wsClientCount"] = s["clientCount"]
		m["bridgeConnected"] = s["bridgeConnected"]
	}
	out := make([]map[string]interface{}, 0, len(byID))
	for _, m := range byID {
		out = append(out, m)
	}
	return out
}
