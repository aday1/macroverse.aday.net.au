# AGENT-REBUILD — Macroverse 42 (The Wired Atelier)

Rebuild this project from scratch. Read this file, `AGENTS.md`, and `docs/BRIDGE.md` before writing code. Preserve all Non-negotiables.

This document is a **from-scratch rebuild runbook**. Follow phases in order; do not skip `exeDir` rules.

## Voice and aesthetics

- **Family:** Wired Atelier shader craft (see local/private Cursor skill directory)
- **Visual:** Copper/amber workbench, shader canvas hero, VJ deck — not generic admin UI
- **Copy:** Instrument/pipeline metaphors (GLSL, ISF, Wire); dry operator build-info text
- **Prefs:** No emoji; no `go run` for production paths

## Rebuild from scratch

### Prerequisites

- Go 1.21+, Node.js 20+, npm
- `shaders/` directory with at least a few `.glsl` / ISF sample files for scan tests
- Copy `shader-preview-settings.default.json` to `shader-preview-settings.json` at repo root

### Path A (recommended): clone and regenerate

    git clone https://github.com/aday1/macroverse.aday.net.au.git
    cd macroverse.aday.net.au
    cp shader-preview-settings.default.json shader-preview-settings.json

Rebuild layer-by-layer using phases below. Keep `build.sh` and `api/console_*.go` build tags unless replacing cross-platform console support.

### Path B: empty directory

Create:

    macroverse42/
      build.sh
      api/
        main.go
        go.mod
        console_windows.go   # + console_other.go build tags
      frontend/
        package.json
        vite.config.ts
        src/                 # vanilla TS UI
      frontend-build/        # output sync target for Go embed/serve
      shaders/
      shader-preview-settings.default.json
      macroverse-bridge-agent/

Initialize:

    cd api && go mod init macroverse && go get required modules
    cd ../frontend && npm create vite@latest . -- --template vanilla-ts

Wire Vite proxy `/api` -> `http://127.0.0.1:8765`. Then follow phases.

### Phased rebuild

| Phase | What to build | Done when |
| --- | --- | --- |
| 0 | `api/go.mod`, `main.go` with `exeDir()` resolving repo root (dir of `Macroverse42` binary) | Built binary started from repo root finds `shaders/` |
| 1 | Shader scan: walk `shaders/`, write SQLite `macroverse.db` or migrate from `shader-index.json` | `GET /api/shaders` (or equivalent list route) returns entries |
| 2 | Static file server for `frontend-build/` or embedded dist; listen **8765** | http://localhost:8765 serves HTML shell |
| 3 | Frontend: library list, preview pane, code editor pane | Selecting shader loads source; compile errors shown |
| 4 | Parameter extraction / ISF metadata; fix-chain hooks (regex, optional Ollama) | Broken shader can be queued for fix per existing UX |
| 5 | Wire export / clipboard routes used by Resolume workflow | Export button produces Wire snippet |
| 6 | VJ deck A/B, gallery, MJPEG or preview stream endpoints | Deck switch updates output route |
| 7 | WebSocket hub `api/ws_hub.go`, session id in settings + URL param | Two browsers stay in sync on same session |
| 8 | `POST /api/bridge/token`, `macroverse-bridge-agent/` native client | Bridge doc smoke in `docs/BRIDGE.md` succeeds on LAN |
| 9 | `/deploy-meta.json` generator in CI or runtime build info panel | Build-info panel shows lane sync on deployed host |
| 10 | `./build.sh` production path: `frontend` vite build -> `frontend-build/`, CGO_ENABLED=0 go build | Single `./Macroverse42` serves prod UI without separate Vite process |

**MVP:** phases 0-4 (local shader lab at :8765).

**Full parity:** phases 5-10 plus VJ tokens documented in `AGENTS.md`.

### Build and verify after each phase

Production binary (always from repo root):

    cd api && go build -o ../Macroverse42 .
    ./Macroverse42

Dev with HMR:

    ./Macroverse42
    cd frontend && npx vite --host 0.0.0.0

Open http://localhost:5173 (proxies API). **Never** `go run .` for path resolution.

## Canonical paths

| Field | Value |
| --- | --- |
| GitHub | https://github.com/aday1/macroverse.aday.net.au |
| Local | `C:/aday.repo/macroverse.aday.net.au` |
| Private source | `aday1/Macroversed-FortyTwoEdition` (features); merge here to ship |
| Vault | private notes vault (local-only) |

## Non-negotiables

| Layer | Requirement |
| --- | --- |
| Backend | Go single binary `Macroverse42` on port **8765** |
| Frontend | Vite + TypeScript (vanilla DOM); baked into binary for production |
| Data | SQLite `macroverse.db` + `shaders/` tree; settings JSON at workspace root |
| Identity | GLSL/ISF shader lab + Resolume Wire export + VJ deck A/B |
| Deploy lanes | `main` = live, `dev` = test; do not ship experiments on `main` alone |
| exeDir | Always **build** binary and run from workspace root — never `go run .` |

First-run files beside binary: `shader-preview-settings.json`, `shaders/`, `macroverse.db` (auto-created).

## Test gates

No automated test suite today. Smoke manually:

- `/api` health and shader list load
- Preview pane (WebGL may fail in software-rendered VMs — API/UI still valid)
- `go vet ./...` in `api/`
- `cd frontend && npx vite build` (transpile; `tsc --noEmit` has known legacy errors)

## Deploy

| Lane | Branch | Tag | URL |
| --- | --- | --- | --- |
| Live | `main` | `:live` | https://macroverse.aday.net.au |
| Dev | `dev` | `:dev` | https://macroverse-test.aday.net.au |
| Private | `main` | `:aday` | https://macroverse-private.aday.net.au (basic auth, all paths) |

Workflows: `macroverse-image` -> `deploy-linode` (`workflow_run`). VPS compose: `/root/compose/macroverse.aday.net.au`.

Promote ethos: reset `dev` from `main` after release; see vault `Live-Dev-Promote-Ethos.md`.

## VJ sessions and LAN bridge

- Session: Settings UI, `?sessionId=`, or `macroverse-vj-session-id` in localStorage
- WebSocket: `GET /ws` (`api/ws_hub.go`)
- Bridge: `macroverse-bridge-agent/`, `docs/BRIDGE.md`, `POST /api/bridge/token`
- VJ output relay: signed view tokens (not raw sessionId in QR)

## Rebuild order

1. Go API: scan/index shaders, SQLite migrate, fix chain hooks
2. Static file server + embedded/built frontend assets
3. Shader preview UI (code editor, params, categories)
4. Wire export / clipboard flows
5. VJ deck A/B + gallery + MJPEG out
6. WebSocket session sync
7. Bridge agent + token mint smoke
8. Deploy-meta / build-info panel (`/deploy-meta.json`)

## File map

| Path | Role |
| --- | --- |
| `api/main.go` | Go entry (console split by OS build tags) |
| `api/` | REST, SQLite, shader scan, fix chain |
| `frontend/` | Vite UI |
| `build.sh` | Production build |
| `macroverse-bridge-agent/` | Pi WebSocket client |
| `docs/BRIDGE.md` | Operator bridge doc |
| `shader-preview-settings.json` | Runtime settings |
| `ops/` | DNS token setup, deploy helpers |

## Secrets (names only)

- `BRIDGE_TOKEN_SECRET` — production bridge minting
- `VJ_OPERATOR_SECRET` — optional hardening for VJ token mint
- `CLOUDFLARE_DNS_API_TOKEN` — `sync-cloudflare-dns.yml` (Zone DNS Edit)
- 1Password / vault: see homelab `Remote-access-and-API-map`

## Anti-patterns

- Do not use `go run` for file path resolution
- Do not delete `macroverse.db` on production without backup (re-migrate is slow)
- No emoji in source
- Pair deploy with ArtBastard: Macroverse image **before** ArtBastard when both change (`flock` on VPS)

## Out of scope

- Full private shader library (~2500) lives in separate repo + private local index (not in this repository)
- Legacy `macroverse42.fly.dev` host
