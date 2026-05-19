package main

import (
	"crypto/hmac"
	"crypto/sha256"
	"encoding/base64"
	"encoding/json"
	"os"
	"time"
)

func base64URLEncode(b []byte) string {
	return base64.RawURLEncoding.EncodeToString(b)
}

func base64URLDecode(s string) ([]byte, error) {
	return base64.RawURLEncoding.DecodeString(s)
}

type BridgeTokenPayload struct {
	Role      string `json:"role"`
	BridgeID  string `json:"bridgeId"`
	SessionID string `json:"sessionId"`
	Exp       int64  `json:"exp"`
}

func bridgeSecret() string {
	if s := os.Getenv("BRIDGE_TOKEN_SECRET"); s != "" {
		return s
	}
	if s := os.Getenv("MACROVERSE_BRIDGE_SECRET"); s != "" {
		return s
	}
	return "macroverse-bridge-dev-secret-change-in-production"
}

func mintBridgeToken(bridgeID, sessionID string, expiresInSec int) (string, error) {
	if expiresInSec <= 0 {
		expiresInSec = 60 * 60 * 24 * 30
	}
	payload := BridgeTokenPayload{
		Role:      "bridge",
		BridgeID:  bridgeID,
		SessionID: normalizeVJSessionID(sessionID),
		Exp:       time.Now().Add(time.Duration(expiresInSec) * time.Second).UnixMilli(),
	}
	payloadStr, err := json.Marshal(payload)
	if err != nil {
		return "", err
	}
	sig := hmacSHA256(payloadStr, bridgeSecret())
	return base64URLEncode(payloadStr) + "." + sig, nil
}

func verifyBridgeToken(token string) (*BridgeTokenPayload, error) {
	parts := splitToken(token)
	if len(parts) != 2 {
		return nil, errInvalidToken
	}
	payloadBytes, err := base64URLDecode(parts[0])
	if err != nil {
		return nil, errInvalidToken
	}
	expected := hmacSHA256(payloadBytes, bridgeSecret())
	if !hmacEqual(parts[1], expected) {
		return nil, errInvalidToken
	}
	var payload BridgeTokenPayload
	if err := json.Unmarshal(payloadBytes, &payload); err != nil {
		return nil, errInvalidToken
	}
	if payload.Role != "bridge" || payload.BridgeID == "" {
		return nil, errInvalidToken
	}
	if payload.SessionID == "" {
		payload.SessionID = defaultVJSessionID
	}
	if payload.Exp < time.Now().UnixMilli() {
		return nil, errInvalidToken
	}
	return &payload, nil
}

func hmacSHA256(payload []byte, secret string) string {
	mac := hmac.New(sha256.New, []byte(secret))
	mac.Write(payload)
	return base64URLEncode(mac.Sum(nil))
}

func hmacEqual(a, b string) bool {
	if len(a) != len(b) {
		return false
	}
	var eq int
	for i := 0; i < len(a); i++ {
		eq |= int(a[i] ^ b[i])
	}
	return eq == 0
}

var errInvalidToken = &tokenError{"invalid bridge token"}

type tokenError struct{ msg string }

func (e *tokenError) Error() string { return e.msg }

func splitToken(t string) []string {
	for i := 0; i < len(t); i++ {
		if t[i] == '.' {
			return []string{t[:i], t[i+1:]}
		}
	}
	return []string{t}
}
