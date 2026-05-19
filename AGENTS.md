# AGENTS.md

## Cursor Cloud specific instructions

Macroverse 42 - The Wired Atelier is a shader preview tool for Resolume Wire. Two services are needed for development:

### Services

| Service | Command | Port |
|---------|---------|------|
| Go backend | `cd /workspace/api && go build -o /workspace/Macroverse42 . && cd /workspace && ./Macroverse42` | 8765 |
| Vite frontend | `cd /workspace/frontend && npx vite --host 0.0.0.0` | 5173 |

The Vite dev server proxies `/api` requests to the Go backend on port 8765.

### Deployment

Production app (not Fly.io): push `main` / `dev` -> `macroverse-image` (GHCR) ->
`deploy-linode` on the Linode Macroverse compose stack. Live at
https://macroverse.aday.net.au/

### Gotchas

- The Go backend uses `exeDir()` (directory of the executable) to locate `shader-preview-settings.json`, `shader-index.json`, and the `shaders/` directory. When using `go run .`, the binary lands in a temp directory and file resolution fails. Always build with `go build -o /workspace/Macroverse42 .` from `api/` and run the binary from the workspace root.
- A `shader-preview-settings.json` must exist in the workspace root. Copy from `shader-preview-settings.default.json` if missing.
- The original codebase had Windows-only `syscall` calls in `api/main.go` `init()`. These were split into `api/console_windows.go` and `api/console_other.go` using build tags for cross-platform compilation.
- TypeScript strict type-checking (`tsc --noEmit`) has pre-existing errors. The Vite build (`npx vite build`) succeeds because Vite only transpiles, it does not type-check.
- There are no automated tests (Go or frontend). The Go backend has no `_test.go` files.
- The Go backend's first startup creates `macroverse.db` (SQLite) and migrates data from `shader-index.json`. This takes a few seconds. If you delete `macroverse.db`, the next startup will re-migrate.
- Start the Go backend first, then the Vite dev server. The backend must be running on 8765 for the Vite proxy to work.
- The Go backend reads stdin for interactive console commands (R=restart, S=scan, etc.). In headless/background mode, redirect stdin from /dev/null: `nohup ./Macroverse42 </dev/null > /tmp/macroverse.log 2>&1 &`
- WebGL shader previews show "compile failed" in the Cloud VM due to software-rendered GPU. The app infrastructure (API, UI, code editor, parameters) works correctly; only the WebGL canvas rendering is affected.

### Lint / Build

- Go: `cd /workspace/api && go vet ./...`
- Frontend build: `cd /workspace/frontend && npx vite build`
- Frontend types: `cd /workspace/frontend && npx tsc --noEmit` (has pre-existing errors)
