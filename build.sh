#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────
# Macroverse 42 — build script (Linux / Mac)
# Requires: Go 1.21+, Node.js 20+
# ─────────────────────────────────────────────────────────────────
set -e

ROOT="$(cd "$(dirname "$0")" && pwd)"
BIN="$ROOT/Macroverse42"
VERSION="${1:-dev}"

echo ""
echo "  MACROVERSE 42 — build"
echo "  ─────────────────────────────────────────"

# Frontend
echo "  [1/2] Building frontend..."
cd "$ROOT/frontend"
npm ci --silent
npm run build --silent
cp -r "$ROOT/frontend/dist/." "$ROOT/frontend-build/"
echo "        frontend-build -> dist/ (synced to frontend-build/)"

# Go binary
echo "  [2/2] Building Go binary..."
cd "$ROOT/api"
CGO_ENABLED=0 go build \
  -ldflags="-s -w -X main.releaseTag=${VERSION}" \
  -o "$BIN" .

echo ""
echo "  ✓  Built: Macroverse42"
echo "  ✓  Run:   ./Macroverse42"
echo "     Then open http://localhost:8765"
echo ""
