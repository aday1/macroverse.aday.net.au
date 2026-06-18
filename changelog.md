Changelog - Macroverse - Wired Atelier
======================================

Deployment lanes (live / test / dev / aday)
-------------------------------------------

Hosted stacks on Linode (GHCR images). Work on `dev`, promote to `main` for live.

| Lane | URL | Branch | Image tag | Role |
|------|-----|--------|-----------|------|
| Live | macroverse.aday.net.au | main | :live | Public cloud library (~2400 ISF shaders + starter-pack); stable tip |
| Test | macroverse-test.aday.net.au | dev | :dev | Pre-promotion integration |
| Dev | macroverse-dev.aday.net.au | dev | :dev | Same :dev image as test |
| Private | macroverse-private.aday.net.au | main | :aday | Full library; basic auth on all paths (including audience streams) |

Each lane serves `/deploy-meta.json` (CI build + deploy timestamps, git SHA, lane_sync) and `/api/version` (semver + release tag). The GitHub showcase refreshes `docs/lane-status.json` on every Pages deploy by probing all lanes.

Showcase: https://showcase.macroverse.aday.net.au/ (GitHub Pages CNAME; mirror https://aday1.github.io/macroverse.aday.net.au/)

### VJ-Generated shader factory (Jun 2026)

- **300 new ISF VJ shaders** in `shaders/VJ-Generated/ISF/` — parametric Wired Atelier templates (macroverse, techno, cosmic, psychedelic, geometric, ambient, plasma, tunnel).
- **QA pipeline** — `scripts/validate-shader-batch.mjs` compile + blank-pixel gate; failures quarantined under `shaders/_quarantine/` (86 unshipped; seeds/tags in `scripts/reports/quarantine-manifest.json`).
- **ISF metadata indexing** — native scan + boot backfill parse ISF `TAGS` into gallery sets (`vj-*`, `macroverse-origin`) and tags for all `VJ-Generated` shaders.
- **Baked thumbnails** — `thumbnails-baked.json` (~2400+ entries, full public library bake) copied into Docker image as `thumbnails.json`; gallery grids no longer require runtime thumbnail generation on live/dev.
- **Template v2 + w3 wave** — brighter factory templates; 150 w3 variants added (140 pass QA); `geo-voronoi` reworked (`w3b` replacements). Quarantine archive now 96 entries (unshipped).
- **w4 factory wave (Jun 2026)** — 12 new templates (`glitch-scanlines`, `rgb-split`, `aurora-curtain`, `beat-radar`, etc.); **288 net-new** VJ-Generated shaders (738 total factory set on live/dev); aday lane syncs full FortyTwo tree including VJ-Generated.
- **Factory orchestrator** — `scripts/run-shader-factory.ps1` for repeatable generation waves; `scripts/seed-generated-metadata.mjs` for quarantine manifest.

### Public library expansion (Jun 2026)

- **Live + test lanes** now ship the full public ISF library (`shaders/VJ-Sorted-Production/ISF/`, ~2400 shaders) plus starter-pack, including the **MacroVerse Origin** set under `macroverse/`.
- Docker image no longer caps at 60 starter-pack files; CI gate requires ≥500 public shaders and still blocks `resolume/`.
- Boot auto-scan merges any on-disk shaders missing from the SQLite index (fixes stale 53-entry factory index after deploy).

Version 42.2 — WebXR VR VJ + live remote extensions
---------------------------------------------------

WebXR audience and VJ controller modes on hosted lanes. VR links from the VJ deck QR panel; host still renders the mix.

### Shipping in 42.2

- **WebXR VR page** (`vj-vr.html`) — audience immersive dome (or flat screen mode) and VJ controller role with DOM overlay HUD.
- **VR QR shortcuts** — VJ deck QR panel: Copy VR audience / Copy VR VJ (signed `viewToken` / `controlToken` URLs).
- **VJ controller VR** — remote crossfader, deck clips, mix mode, Auto VJ on/off and BPM over WebSocket; live GLSL push (`vj:shader-live`) to host decks.
- **Audience VR** — SSE stream of live A/B mix inside WebXR; mouse X/Y steer when audience participation is enabled on host.
- **Auto VJ toggles** — independent shader-swap vs depth/param motion when Auto VJ is on (persisted in localStorage).
- **Shared stream core** — `vjStreamCore.ts` for deck A/B + mix rendering on flat output and VR pages.

### Showcase & origin (Jun 2026)

- **Origin story** moved under the showcase banner with Fringe + Nick Wilson bio links.
- **Nick Wilson audio link** on showcase and about — older recording (not the Fringe gig); show AV recording and EP news noted as coming soon.
- Showcase UX: compact section nav, origin media chips, deploy lanes moved out of hero header.
- **Showcase alias** `showcase.macroverse.aday.net.au` → GitHub Pages (`docs/CNAME` + Cloudflare CNAME to `aday1.github.io`).
- **Showcase first viewport** — shader demo sliders + origin story + live lane links above the fold; deep sections (lanes, three ways in, demos, screenshots, help) on scroll.
- **Showcase hero UX** — wider 1280px layout, two-column splash (shader demo + origin), content-hugging card (no empty viewport gap), live lane chips.
- **Showcase hero** — Three ways in moved into hero panel; about.html restyled like showcase with M42 logo and red-green WebGL background.
- **VR notice** — “VR SUPPORT UNDER DEVELOPMENT” on showcase, about, vj-vr.html, splash VR QRs, and gig QR hub.
- **MacroVerse Origin shader set** — six compiling ISF shaders in `starter-pack/macroverse/` (Energy Field through Living Our Best Life); VJ set `macroverse-origin`; baked into factory index; Gallery Seed + Wire export.
- **Showcase hero demo clips** — Pipeline, Expose, and Fix chain loops in a compact three-across strip below the slider + origin row (no column height mismatch).

### Notes

- Flat 2D GLSL shaders map onto the VR dome; ray-marched 360 content looks best in sphere mode.
- Additional live-VJ modules (code overlay, output FX, per-deck QR, library filters) ship in this build; some host-deck wiring is still landing.

Version 42.1 — Stable hosted release (Forty-Two)
------------------------------------------------

First **stable** line under dev / live / aday lanes on Linode (GHCR).
Product identity **Macroverse 42**; semver **42.1** supersedes interim 42.0.x tags.
Develop on this line with fewer micro-releases.

### Shipping in 42.1

- Rich gradient sliders with touch-friendly hit targets and scroll isolation.
- Dev vs live lane sync in deploy-meta and build-info UI.
- VJ sessions, audience QR, LAN bridge operator docs (relic-era features consolidated).
- Deploy-linode fetch+reset aligned with ArtBastard stack.

### Fixed (consolidated from 42.0.x)

- Range sliders: explicit hit targets, z-index, `touch-action` so drags do not scroll the page.
- Layout scroll and flex overflow polish on hosted UI.

## Pre-release relics (development archaeology)

Interim 0.1.x version numbers below were never official shipping tags.

See git relic commits before the official hosted era for detailed archaeology.
