package main

import (
	"bytes"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"testing"
)

func TestAllowedVjBrowserOrigin(t *testing.T) {
	allowed := []string{
		"https://errordiffusion.net",
		"https://www.errordiffusion.cc",
		"http://localhost:4173",
		"http://127.0.0.1:8080",
	}
	for _, origin := range allowed {
		if !allowedVjBrowserOrigin(origin) {
			t.Fatalf("expected origin to be allowed: %s", origin)
		}
	}

	denied := []string{
		"https://example.com",
		"https://localhost:4173",
		"http://errordiffusion.net",
		"not a URL",
	}
	for _, origin := range denied {
		if allowedVjBrowserOrigin(origin) {
			t.Fatalf("expected origin to be denied: %s", origin)
		}
	}
}

func TestVjBrowserCORSPreflight(t *testing.T) {
	req := httptest.NewRequest(http.MethodOptions, "/api/vj/view-token", nil)
	req.Header.Set("Origin", "https://errordiffusion.net")
	rec := httptest.NewRecorder()

	if !applyVjBrowserCORS(rec, req) {
		t.Fatal("expected trusted preflight to be handled")
	}
	if rec.Code != http.StatusNoContent {
		t.Fatalf("expected 204, got %d", rec.Code)
	}
	if got := rec.Header().Get("Access-Control-Allow-Origin"); got != "https://errordiffusion.net" {
		t.Fatalf("unexpected allow origin: %q", got)
	}
}

func TestVjBrowserCORSExcludesControlAPI(t *testing.T) {
	req := httptest.NewRequest(http.MethodOptions, "/api/vj-output/state", nil)
	req.Header.Set("Origin", "https://errordiffusion.net")
	rec := httptest.NewRecorder()

	if applyVjBrowserCORS(rec, req) {
		t.Fatal("control endpoint must not be exposed cross-origin")
	}
	if got := rec.Header().Get("Access-Control-Allow-Origin"); got != "" {
		t.Fatalf("unexpected allow origin: %q", got)
	}
}

func TestVjViewTokenIsViewerOnly(t *testing.T) {
	body := bytes.NewBufferString(`{"sessionId":"error-diffusion-test","expiresIn":60}`)
	req := httptest.NewRequest(http.MethodPost, "/api/vj/view-token", body)
	rec := httptest.NewRecorder()

	handleVjViewToken(rec, req)
	if rec.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", rec.Code, rec.Body.String())
	}
	var payload map[string]string
	if err := json.Unmarshal(rec.Body.Bytes(), &payload); err != nil {
		t.Fatalf("invalid response JSON: %v", err)
	}
	if payload["controlToken"] != "" {
		t.Fatal("viewer endpoint must never return a control token")
	}
	token := payload["viewerToken"]
	verified, err := verifyVjToken(token, vjRoleViewer)
	if err != nil {
		t.Fatalf("viewer token did not verify: %v", err)
	}
	if verified.SessionID != "error-diffusion-test" {
		t.Fatalf("unexpected session ID: %q", verified.SessionID)
	}
}
