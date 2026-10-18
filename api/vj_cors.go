package main

import (
	"net/http"
	"net/url"
	"strings"
)

var vjBrowserOrigins = map[string]bool{
	"https://errordiffusion.net":     true,
	"https://www.errordiffusion.net": true,
	"https://errordiffusion.cc":      true,
	"https://www.errordiffusion.cc":  true,
}

func allowedVjBrowserOrigin(origin string) bool {
	if vjBrowserOrigins[origin] {
		return true
	}
	parsed, err := url.Parse(origin)
	if err != nil || parsed.Scheme != "http" {
		return false
	}
	host := strings.ToLower(parsed.Hostname())
	return host == "localhost" || host == "127.0.0.1"
}

func vjBrowserAPIPath(path string) bool {
	return path == "/api/vj/view-token" || path == "/api/vj-output/stream"
}

// applyVjBrowserCORS allows viewer-only VJ data to feed trusted browser tools.
// It deliberately excludes operator and state-writing endpoints.
func applyVjBrowserCORS(w http.ResponseWriter, r *http.Request) bool {
	if !vjBrowserAPIPath(r.URL.Path) {
		return false
	}
	origin := strings.TrimSpace(r.Header.Get("Origin"))
	if origin == "" || !allowedVjBrowserOrigin(origin) {
		return false
	}
	w.Header().Set("Access-Control-Allow-Origin", origin)
	w.Header().Add("Vary", "Origin")
	w.Header().Set("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
	w.Header().Set("Access-Control-Allow-Headers", "Content-Type")
	w.Header().Set("Access-Control-Max-Age", "86400")
	if r.Method == http.MethodOptions {
		w.WriteHeader(http.StatusNoContent)
		return true
	}
	return false
}
