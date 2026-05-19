package main

import (
	"encoding/json"
	"net/http"
	"strings"
)

func handleVjTokens(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "method not allowed", 405)
		return
	}
	var req struct {
		SessionID      string `json:"sessionId"`
		ControlToken   string `json:"controlToken"`
		OperatorSecret string `json:"operatorSecret"`
		ExpiresIn      int    `json:"expiresIn"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "invalid json", 400)
		return
	}
	sid := normalizeVJSessionID(req.SessionID)

	if secret := vjMintOperatorSecret(); secret != "" {
		if req.OperatorSecret != secret {
			http.Error(w, "operator secret required", 403)
			return
		}
	} else if strings.TrimSpace(req.ControlToken) != "" {
		payload, err := verifyVjToken(strings.TrimSpace(req.ControlToken), vjRoleOperator)
		if err != nil || payload.SessionID != sid {
			http.Error(w, "invalid control token", 403)
			return
		}
	}

	viewer, err := mintVjToken(vjRoleViewer, sid, req.ExpiresIn)
	if err != nil {
		http.Error(w, err.Error(), 500)
		return
	}
	control, err := mintVjToken(vjRoleOperator, sid, req.ExpiresIn)
	if err != nil {
		http.Error(w, err.Error(), 500)
		return
	}
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"sessionId":    sid,
		"viewerToken":  viewer,
		"controlToken": control,
	})
}

func vjSessionIDFromStreamRequest(r *http.Request) (string, int) {
	viewToken := strings.TrimSpace(r.URL.Query().Get("viewToken"))
	if viewToken != "" {
		payload, err := verifyVjToken(viewToken, vjRoleViewer)
		if err != nil {
			return "", http.StatusForbidden
		}
		return payload.SessionID, 0
	}
	if vjRequireViewToken() {
		return "", http.StatusForbidden
	}
	return vjSessionIDFromRequest(r.URL.Query().Get("sessionId"), ""), 0
}

func vjSessionIDFromConfigRequest(r *http.Request) (string, int) {
	viewToken := strings.TrimSpace(r.URL.Query().Get("viewToken"))
	if viewToken != "" {
		payload, err := verifyVjToken(viewToken, vjRoleViewer)
		if err != nil {
			return "", http.StatusForbidden
		}
		return payload.SessionID, 0
	}
	controlToken := strings.TrimSpace(r.URL.Query().Get("controlToken"))
	if controlToken != "" {
		payload, err := verifyVjToken(controlToken, vjRoleOperator)
		if err != nil {
			return "", http.StatusForbidden
		}
		return payload.SessionID, 0
	}
	if vjRequireViewToken() && vjRequireControlToken() {
		return "", http.StatusForbidden
	}
	return vjSessionIDFromRequest(r.URL.Query().Get("sessionId"), ""), 0
}

func handleVjSessionConfigGet(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "method not allowed", 405)
		return
	}
	sessionID, status := vjSessionIDFromConfigRequest(r)
	if status != 0 {
		http.Error(w, "token required", status)
		return
	}
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"sessionId":             sessionID,
		"audienceParticipation": getAudienceParticipation(sessionID),
	})
}

func handleVjSessionConfigPost(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "method not allowed", 405)
		return
	}
	sessionID, status := vjSessionIDFromStatePost(r)
	if status != 0 {
		http.Error(w, "control token required", status)
		return
	}
	var body struct {
		AudienceParticipation bool `json:"audienceParticipation"`
	}
	if err := json.NewDecoder(r.Body).Decode(&body); err != nil {
		http.Error(w, "invalid json", 400)
		return
	}
	setAudienceParticipation(sessionID, body.AudienceParticipation)
	broadcastVjSessionConfig(sessionID, body.AudienceParticipation)
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"ok":                    true,
		"sessionId":             sessionID,
		"audienceParticipation": body.AudienceParticipation,
	})
}

func broadcastVjSessionConfig(sessionID string, enabled bool) {
	room := getRoom(sessionID)
	room.broadcast(sessionID, map[string]interface{}{
		"type":                  "vj:config",
		"sessionId":             sessionID,
		"audienceParticipation": enabled,
	}, nil)
}

func handleVjAudienceMouse(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "method not allowed", 405)
		return
	}
	sessionID, status := vjSessionIDFromStreamRequest(r)
	if status != 0 {
		http.Error(w, "view token required", status)
		return
	}
	if !getAudienceParticipation(sessionID) {
		http.Error(w, "audience participation disabled", http.StatusForbidden)
		return
	}
	var body struct {
		MouseX float64 `json:"mouseX"`
		MouseY float64 `json:"mouseY"`
	}
	if err := json.NewDecoder(r.Body).Decode(&body); err != nil {
		http.Error(w, "invalid json", 400)
		return
	}
	if body.MouseX < 0 {
		body.MouseX = 0
	} else if body.MouseX > 1 {
		body.MouseX = 1
	}
	if body.MouseY < 0 {
		body.MouseY = 0
	} else if body.MouseY > 1 {
		body.MouseY = 1
	}
	broadcastVjAudienceMouse(sessionID, body.MouseX, body.MouseY)
	vjPublishAudienceMouseToStream(sessionID, body.MouseX, body.MouseY)
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{"ok": true})
}

func vjSessionIDFromStatePost(r *http.Request) (string, int) {
	controlToken := strings.TrimSpace(r.URL.Query().Get("controlToken"))
	if controlToken != "" {
		payload, err := verifyVjToken(controlToken, vjRoleOperator)
		if err != nil {
			return "", http.StatusForbidden
		}
		return payload.SessionID, 0
	}
	if vjRequireControlToken() {
		return "", http.StatusForbidden
	}
	return vjSessionIDFromRequest(r.URL.Query().Get("sessionId"), ""), 0
}
