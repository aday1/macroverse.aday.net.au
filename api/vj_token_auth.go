package main

import (
	"encoding/json"
	"os"
	"time"
)

const (
	vjRoleViewer   = "viewer"
	vjRoleOperator = "operator"
)

type VjTokenPayload struct {
	Role      string `json:"role"`
	SessionID string `json:"sessionId"`
	Exp       int64  `json:"exp"`
}

func vjTokenSecret() string {
	if s := os.Getenv("VJ_TOKEN_SECRET"); s != "" {
		return s
	}
	if s := os.Getenv("MACROVERSE_VJ_TOKEN_SECRET"); s != "" {
		return s
	}
	return bridgeSecret()
}

func mintVjToken(role, sessionID string, expiresInSec int) (string, error) {
	if expiresInSec <= 0 {
		expiresInSec = 60 * 60 * 24 * 30
	}
	payload := VjTokenPayload{
		Role:      role,
		SessionID: normalizeVJSessionID(sessionID),
		Exp:       time.Now().Add(time.Duration(expiresInSec) * time.Second).UnixMilli(),
	}
	payloadStr, err := json.Marshal(payload)
	if err != nil {
		return "", err
	}
	sig := hmacSHA256(payloadStr, vjTokenSecret())
	return base64URLEncode(payloadStr) + "." + sig, nil
}

func verifyVjToken(token, expectedRole string) (*VjTokenPayload, error) {
	parts := splitToken(token)
	if len(parts) != 2 {
		return nil, errInvalidToken
	}
	payloadBytes, err := base64URLDecode(parts[0])
	if err != nil {
		return nil, errInvalidToken
	}
	expected := hmacSHA256(payloadBytes, vjTokenSecret())
	if !hmacEqual(parts[1], expected) {
		return nil, errInvalidToken
	}
	var payload VjTokenPayload
	if err := json.Unmarshal(payloadBytes, &payload); err != nil {
		return nil, errInvalidToken
	}
	if expectedRole != "" && payload.Role != expectedRole {
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

func vjMintOperatorSecret() string {
	if s := os.Getenv("VJ_OPERATOR_SECRET"); s != "" {
		return s
	}
	if s := os.Getenv("MACROVERSE_VJ_OPERATOR_SECRET"); s != "" {
		return s
	}
	return ""
}

func vjRequireViewToken() bool {
	return os.Getenv("VJ_REQUIRE_VIEW_TOKEN") == "1" || os.Getenv("VJ_REQUIRE_VIEW_TOKEN") == "true"
}

func vjRequireControlToken() bool {
	return os.Getenv("VJ_REQUIRE_CONTROL_TOKEN") == "1" || os.Getenv("VJ_REQUIRE_CONTROL_TOKEN") == "true"
}
