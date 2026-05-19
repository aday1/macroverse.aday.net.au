package main

import (
	"strings"
	"sync"
)

const defaultVJSessionID = "default"

type vjSessionRoom struct {
	clients     map[chan string]bool
	lastShaderA string
	lastShaderB string
	lastFrame   string
}

var vjSessions struct {
	sync.Mutex
	byID map[string]*vjSessionRoom
}

func vjOutputInit() {
	vjSessions.byID = make(map[string]*vjSessionRoom)
}

func normalizeVJSessionID(id string) string {
	id = strings.TrimSpace(id)
	if id == "" {
		return defaultVJSessionID
	}
	if len(id) > 64 {
		return id[:64]
	}
	return id
}

func vjSessionIDFromRequest(querySession string, bodySession string) string {
	if strings.TrimSpace(querySession) != "" {
		return normalizeVJSessionID(querySession)
	}
	return normalizeVJSessionID(bodySession)
}

func vjOutputBroadcast(sessionID string, msg string) {
	sid := normalizeVJSessionID(sessionID)
	vjSessions.Lock()
	room := vjSessions.byID[sid]
	if room == nil {
		room = &vjSessionRoom{clients: make(map[chan string]bool)}
		vjSessions.byID[sid] = room
	}
	if strings.Contains(msg, `"type":"shader"`) {
		if strings.Contains(msg, `"deck":"A"`) {
			room.lastShaderA = msg
		} else if strings.Contains(msg, `"deck":"B"`) {
			room.lastShaderB = msg
		}
	} else if strings.Contains(msg, `"type":"frame"`) {
		room.lastFrame = msg
	} else if strings.Contains(msg, `"type":"clear"`) {
		if strings.Contains(msg, `"deck":"A"`) {
			room.lastShaderA = ""
		} else if strings.Contains(msg, `"deck":"B"`) {
			room.lastShaderB = ""
		}
	}
	for ch := range room.clients {
		select {
		case ch <- msg:
		default:
		}
	}
	vjSessions.Unlock()
}

func vjOutputStreamSubscribe(sessionID string) (chan string, func()) {
	sid := normalizeVJSessionID(sessionID)
	ch := make(chan string, 128)
	vjSessions.Lock()
	room := vjSessions.byID[sid]
	if room == nil {
		room = &vjSessionRoom{clients: make(map[chan string]bool)}
		vjSessions.byID[sid] = room
	}
	room.clients[ch] = true
	if room.lastShaderA != "" {
		select {
		case ch <- room.lastShaderA:
		default:
		}
	}
	if room.lastShaderB != "" {
		select {
		case ch <- room.lastShaderB:
		default:
		}
	}
	if room.lastFrame != "" {
		select {
		case ch <- room.lastFrame:
		default:
		}
	}
	vjSessions.Unlock()

	unsub := func() {
		vjSessions.Lock()
		if room := vjSessions.byID[sid]; room != nil {
			delete(room.clients, ch)
		}
		vjSessions.Unlock()
	}
	return ch, unsub
}

func listVJSessions() []map[string]interface{} {
	vjSessions.Lock()
	defer vjSessions.Unlock()
	out := make([]map[string]interface{}, 0, len(vjSessions.byID))
	for id, room := range vjSessions.byID {
		out = append(out, map[string]interface{}{
			"id":          id,
			"clientCount": len(room.clients),
			"hasSignal":   room.lastShaderA != "" || room.lastShaderB != "" || room.lastFrame != "",
		})
	}
	return out
}
