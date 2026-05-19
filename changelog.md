Changelog - Macroverse - Wired Atelier
======================================

Version 0.1.9 — First-visit quick start, audience VJ participation
------------------------------------------------------------------

### Quick start (new computers)
- First full app load on a browser shows a Quick start dialog (localStorage `macroverse-quick-start-seen-v1`).
- Skipped for VJ view-only / output URLs. Reopen via command palette: Quick start guide. `?quickstart=1` forces show; `?quickstart=0` skips.

### Audience and co-VJ participation
- Audience stream (`viewToken`, `audienceUi=1`): touch steers live show on main screens; phone-only interact hint.
- Co-VJ collaboration (`controlToken`): full deck sync over WebSocket.
- Projector QR layout: position, scale, rotation, subtle FX (settings + drag on VJ output).

Version 0.1.8 — VJ show sessions, WebSocket deck sync, LAN bridge agent
-----------------------------------------------------------------------

### Sessions and multi-device VJ
- Per-gig `sessionId` on VJ output SSE and deck control WebSocket.
- Settings: VJ Show Session ID + HDMI preview URL hint.
- Any browser on the same session shares crossfader, mix mode, deck clips, pages.

### LAN bridge (Pi)
- `macroverse-bridge-agent/`: outbound WSS to `/ws`, Ableton Link clock relay.
- `POST /api/bridge/token`, `GET /api/bridge/status`, `GET /api/vj-sessions`.
- Operator guide: `docs/BRIDGE.md`.

Version 0.1.7 — Showcase videos, comprehensive Help refresh, broken-stub fix
-----------------------------------------------------------------------------

### Showcase: dropped the stub HTML doc that was hiding the rich page
- The bootstrap commit had prepended a 25-line "deploy portal" stub
  on top of the rich Macroverse 42 showcase page in docs/index.html.
  Two HTML documents in one file confused browsers and effectively
  hid the rich page (live WebGL background, GLSL Lab easter egg,
  full Help section, screenshots).
- The hostname index that the stub used to carry has been folded
  into a new "Live deployments" mini-block under the CTA buttons:
  Live · Test · Aday (basic auth) · ArtBastard live + test.

### Showcase: six procedural ffmpeg demo loops
- New docs/videos/ directory with hero-loop / vj-crossfade /
  pipeline-flow / gallery-grid / expose-params / fix-chain MP4
  files plus a poster .webp per video.
- All loops are deterministic, web-safe (H.264 yuv420p faststart,
  no audio), 960x540 @ 30fps, capped under 1.5 MB each, total
  ~2.5 MB. Built by docs/videos/build-videos.sh from ffmpeg's
  built-in lavfi sources (mandelbrot, life, cellauto, gradients).
- Embedded in a new "Live demo loops" section as muted autoplay
  loops with poster fallback. Clearly labelled as procedural
  placeholders since the Cloud VM has no working WebGL for real
  screen recordings.
- Run "bash docs/videos/build-videos.sh" to rebuild any time.

### Showcase: comprehensive Help section rewrite
- The Help section now mirrors the in-app Help modal in
  frontend/src/init/bootstrap.ts and covers eighteen subsections:
    1. Getting started
    2. View tabs (Preview / Code / Split / VJ / Gallery / Pipeline /
       Wire Hub)
    3. Top app bar (logo, shader name, view dropdown, Last build
       badge, Save, palette, hamburger)
    4. Mobile bottom tab bar + More sheet
    5. Command palette (Ctrl+K, "/", Up/Down/Enter/Esc)
    6. Gallery shortcuts
    7. Preset VJ Sets (Shift+1 through Shift+9)
    8. Common actions (preview, search, rename, sets, list views,
       trash, dead, version history)
    9. Expose parameters and Wire export (Expose, Search params,
       sweet-spot underlines, Refactor AI, Visual Modify Vibe via
       Cursor or GitHub Copilot, Check ISF Wire, Clipboard to Wire,
       texture handling)
   10. Vibe Station (Modify current / Create new with genres:
       Particles, Fractal, 3D Sphere/Cube/Torus, Tunnel, Kaleidoscope,
       Audio, Gradient)
   11. VJ deck (decks, crossfader, mix modes, Auto VJ, FFT A/B, OSC
       A/B, pop-out output, text templates, mouse XY)
   12. Roliblock (multi-device, BLE/USB, deck assign, LED filters,
       stretched mode, debug page, view modes, background render)
   13. External controls (OSC Listen, MIDI Learn, Audio FFT, Texture
       inputs, webcam)
   14. Output and streaming (MJPEG MacroCam, OBS, v4l2loopback,
       Spout/NDI right-click)
   15. Display effects (Scan, Vignette, CRT code, Full Screen, theme)
   16. Update button (5s background fetch, amber ahead-indicator,
       Apply & Restart cross-platform script flow)
   17. Settings reference (Source paths, LLM Provider Chain, Hard
       Reset path + behaviour, NUKE, READONLY, DEMO_BANNER, Default
       view, Mobile/Desktop, Graveyard log)
   18. Master keyboard shortcut table

### Showcase: misc tidy
- Added a Demo loops button in the CTA strip jumping to the new
  section.
- Added the missing emoji-picker.webp tile to the Screenshots grid.
- The Screenshots section now points up at the Demo loops section
  so visitors land on motion content first.

### README
- Added five new sections aligned with the same coverage:
  Command Palette, Top App Bar & Mobile UI, Pipeline View & Wire
  Hub View, Update Button, Display Effects, Roliblock & BLE MIDI.
- Expanded the UI cheat sheet with eleven new rows (palette,
  Pipeline / Wire tabs, Update, mobile bottom bar, More sheet,
  display effects, MIDI Learn, OSC Listen, Audio FFT, Roliblock).
- Renamed the Screenshots section to "Screenshots & Demo Loops"
  with a per-file rundown and rebuild instructions.

----------------------------------------

Version 0.1.6 — Multi-Roliblock, BLE MIDI, VJ deck features, Wire pipeline
-----------------------------------------------------------------------------

### Multi-Roliblock support
- N simultaneous Roli Lightpad Blocks with independent MIDI input/output, handshake, LED buffer, and settings per device.
- Per-device deck assignment (auto, deckA, deckB, shared) with auto-detect for Roli USB devices.
- Per-device LED processing controls: contrast, brightness, saturation, gamma, grayscale, invert, posterize, channel isolation.
- Stretched LED display mode: sample 30x15 from output canvas, split left/right to two devices.

### BLE MIDI (Bluetooth Low Energy)
- Web Bluetooth API integration for wireless Roli Lightpad connection via BLE MIDI service.
- Per-device BLE pairing: Device A can be BLE while Device B is USB, or any combination.
- BLE-aware timing: 250ms handshake delay, 80ms LED stream interval (vs 150ms/50ms USB).
- Connection badges show BLE/USB state with colour-coded indicators.
- SysEx chunking for BLE MTU limits with async writeValueWithoutResponse.

### Roliblock debug page
- Dual-device debug page with independent shaders, mouse XY, WebGL preview, and LED stream per device slot.
- Shader library cycling: Prev/Next/Random buttons and auto-cycle timer per device slot.
- View mode switching: A Only, B Only, Both (default), Combined (30x15 side-by-side LED preview).
- Background rendering: keeps LED streaming when browser tab is hidden via setInterval fallback.
- BLE Pair/Disconnect buttons per device with connection state badges.

### Wire pipeline enhancements
- 2,094 Wire patches with 5 FX topologies (feedback, beat-sync, glitch, geometric, colour).
- Wire feature toggles for topology generation.
- Pipeline diagram view tab showing full signal flow.

### LED processing controls
- Per-device LED filters accessible from Roliblock panel.
- Light theme preset option.

----------------------------------------

Version 0.1.5 — In-app update button: pull, rebuild, and relaunch from GitHub
-------------------------------------------------------------------------------

### Update button
- New **Update** button in the index panel toolbar (next to Paths / Settings / ? / Scan).
- On startup, 5 seconds after load, the app silently runs `git fetch` + `git log HEAD..origin/HEAD`
  in the background. If commits exist on the remote, the button turns amber and shows `Update (N)`.
- Clicking **Update** opens a modal listing all incoming commits with local → remote SHA info.
- **Apply & Restart** writes a temp batch script that runs `git pull origin` → `build.bat`
  (npm ci + npm run build + go build) → launches the new `Macroverse42.exe`, then exits
  the current process so Windows can overwrite the running binary.
- Works on Linux/macOS too: uses `build.sh` and a bash script.
- Button shows "Already up to date." in the status bar if no remote changes are found.
- Gracefully no-ops when the app is not running inside a git repository.

### Backend
- `GET /api/update/check` — fetches from remote, returns `{ hasUpdates, commits, localHead, remoteHead }`.
- `POST /api/update/apply` — writes and launches the update script, then exits.

----------------------------------------

Version 0.1.4 — Interactive showcase: mouse warp, live GLSL sliders, easter egg lab
--------------------------------------------------------------------------------------

### Showcase page: mouse-driven background shader
- Moving the cursor across the GitHub Pages showcase now pulls the domain-warp field in
  real time. The shader receives a smoothly lerped uMouse uniform (0..1 x/y) and offsets
  the FBM origin so the fractal warps toward your cursor position.

### Showcase page: visible shader parameter sliders
- Four live sliders embedded in the page header below the CTA buttons:
    - WARP: domain-warp intensity (0 → flat noise, 2 → heavily warped)
    - SPEED: time scale (0.1 → near-frozen, 3 → frantic)
    - GRID: network-node grid brightness (0 → invisible, 2 → bright)
    - PALETTE: shift the colour blend point (-0.5 → darker/cooler, +0.5 → lighter/hot)
  All sliders update the live WebGL shader instantly with no page reload.

### Showcase page: GLSL Lab easter egg
- The actual fragment shader source code running behind the page is exposed in a hidden
  panel (the "GLSL Lab"). Activate it by pressing ~ (backtick/tilde) or triple-clicking
  the MACROVERSE 42 title.
- The panel shows a live textarea with the full GLSL source. Edit it and press
  Ctrl+Enter (or the COMPILE button) to recompile and apply it live in the page.
  Press RESET to restore the original shader.
- Lab sliders are synced with the page sliders — changes in either propagate to both.
- This demonstrates exactly what Macroverse does for your shader library.

### Showcase page: readability and colour fixes
- Raised --dim colour from #554488 to #9977cc — all dim/hint text is now clearly readable
  against the dark background.
- Raised --text from #c0b8d8 to #d0c8e8 for slightly better body-text contrast.
- Fixed h3 elements in the Help section using undefined var(--accent) —
  now correctly use var(--teal).
- Rewrote the header sub-tagline: "your whole library, live — search it, tag it, perform it"
  (previous version was considered clumsy).

### Showcase page: Help section h3 links to #help anchor in nav
- "Help & Shortcuts" CTA button added in a previous release now correctly scrolls to the
  Help section rendered in this release.

----------------------------------------

Version 0.1.3 — Settings overhaul, scoped git reset, help docs, library gitignore
-----------------------------------------------------------------------------------

### Settings: LLM Provider Chain
- Settings panel now prominently shows the full LLM Provider Chain: Local regex · Ollama · Cursor.
- Each provider has an independent enable/disable toggle — turn off Cursor if you have no API key,
  disable Ollama if you don't run it locally, or run fully offline with only the free local regex layer.
- Ollama model and endpoint are configurable per-provider in the Settings panel.
- Provider priority order is user-controlled (drag number to reorder).

### Hard Reset (git) — configurable target path
- The "Hard Reset (git)" button in Settings zips the configured target folder and restores it
  from the first git commit on main. The target path defaults to `shaders/custom/` and is
  configurable in Settings → Hard Reset Path — point it at any folder tracked by a git repo.
- Backend: `/api/git/hard-reset-shaders` reads the path from AppSettings at request time.
- UI: the Settings panel shows a "Hard Reset target path" input above the Hard Reset button.
- Confirmation dialog lists exactly what will happen:
    1. A timestamped zip backup of the target folder is saved next to macroverse.db.
    2. All files in the target folder are checked out from the very first git commit on main.
    3. The app rescans after reset.

### NUKE button — explicit confirmation
- The NUKE (backup + clear index) confirmation dialog now lists all 3 actions explicitly:
    1. BACKUP — index copied to timestamped snapshot.
    2. CLEAR — entire SQLite index wiped (tags, sets, history, all metadata).
    3. RESCAN — all source paths re-walked; every file re-indexed from scratch.
- Shader files on disk are never touched.

### Source paths and .gitignore
- `.gitignore` updated: personal shader libraries stay local only, never committed.

### Help section on GitHub Pages showcase
- `docs/index.html` now has a full "Help & Keyboard Shortcuts" section covering:
    - Gallery navigation (arrow keys, page jump, tag/set shortcuts, favourite, rename, HUD).
    - Preset VJ Set reference (all 9 slots with names).
    - Common action table (preview, search, rename, tags, Expose, Wire export, history, VJ deck).
    - Settings reference (source paths, LLM chain, Hard Reset, Nuke).
    - OBS / streaming command snippets.

### README
- Fixed missing `## LLM Provider Chain` section heading (was accidentally removed in a previous edit).

### Navigation help link on showcase
- Footer and help section added at `https://aday1.github.io/Macroverse/#help`.

----------------------------------------

Version 0.1.2 — Gallery overhaul, mobile UI, read-only demo, stability fixes
------------------------------------------------------------------------------

Gallery mode
- Arrow keys (← → ↑ ↓) navigate between cells; Alt+← / Alt+→ jump pages.
- Per-page presets changed to 1 / 4 / 8 / 14 / 24 with auto grid layout.
- Tag shortcuts (1–9) now toggle: pressing the same key removes the tag.
- Shift + 1–9 toggles preset VJ Sets on the focused shader.
- A key opens a set-toggle prompt showing preset sets and current membership.
- F key toggles favourite on the focused shader.
- ? key shows/hides an in-gallery keyboard shortcut HUD.
- All tag/set shortcuts display a compact hint bar below the toolbar.
- Export ▾ menu: exports filtered list, page, or paths (.txt) by set or tag.
  Path exports (one path per line) can be fed directly into Resolume Avenue.
- Seed VJ Sets button: auto-assigns shaders to 9 preset VJ sets by format
  and name/category heuristics (vj-ambient, vj-techno, vj-cosmic, vj-glitch,
  vj-geometric, vj-organic, vj-wire-ready, vj-dark, vj-colour).
- Sidebar shows current sets and tags for focused shader.

Mobile / responsive UI
- Mobile/Desktop toggle button in the status bar; preference saved to localStorage.
- Force-mobile CSS class mirrors the @media mobile rules on any device/viewport.
- Gallery grid drops to 2 columns; sidebar hidden on mobile.

Read-only demo mode
- READONLY=true env var blocks save, delete, rename, factory-reset API endpoints
  with a 403 JSON response — public demo can VJ but cannot mutate the library.
- DEMO_BANNER=true shows a persistent orange banner: "Read-only demo instance".
- /api/config endpoint returns { readonly, demo } for frontend feature-gating.

Reliability fixes
- On startup: stale index entries (files deleted while server was offline) are
  now purged automatically before any request is served.
- File watcher: deletions are now detected (files in knownFiles that disappear
  from the current scan trigger an incremental reindex).
- doFactoryReset now fires triggerIncrementalIndex() immediately after clearing
  the DB so the fresh index reflects only files actually on disk.
- Full-screen button: fixed this-binding bug where exitFullscreen /
  requestFullscreen were called without correct context on some browsers.

Build and cache
- build.bat and build.sh now sync frontend/dist/ → frontend-build/ after
  each npm build so the Go binary always serves the latest frontend.
- Static file server adds Cache-Control: no-cache to .css files alongside
  .js and .html to prevent stale stylesheet issues after upgrades.
- *.exe and the Linux Macroverse42 binary added to .gitignore.


----------------------------------------

- Fix release archives: now include ascii.art (required for terminal splash
  screen), shader-preview-settings.json (default config), and START.txt
  (quickstart guide) alongside the binary and frontend-build/ folder.
- Inject git tag as version string at build time via -X main.releaseTag so the
  splash screen and /api/version endpoint report the correct release tag.
- Add START.txt: box-art quickstart covering launch, Wire pipeline, settings
  reference, OBS/virtual webcam, and build-from-source instructions.
- Add build.sh (Linux/Mac) and build.bat (Windows) for source builds with
  optional version argument passed through to ldflags.

Version 0.1.0 — First Official Release
---------------------------------------

- Release workflow: cross-compiles Go binary for Windows/Linux/macOS (amd64 + arm64);
  packages each with frontend-build/ into zip/tar.gz; triggers on v* tags via
  softprops/action-gh-release@v2. Binaries available on the Releases page.
- Showcase page (docs/index.html): full redesign with live WebGL GLSL background
  (fractal domain-warp FBM, Macroverse palette: void→purple→indigo→electric blue→DMT teal
  with copper bloom), glitch title animation, network node decoration, serial experiments
  lain / psychonaut aesthetic, two-use-case layout, feature grid, protocol footer.
- UI theme (frontend/theme.css): shifted from Amiga Workbench blue to dark Wired Atelier
  palette (deep void black-purple surfacing to indigo); code editor colours updated to
  electric violet keywords, DMT teal strings, copper numbers, cyan functions; scanline
  and ambient bloom overlays added to body; selection colour set to purple.
- UI glow (frontend/layout.css): active tab pulse animation, preview area inner glow,
  Wire toolbar accent glow, params group emerald glow, resizer hover glow, search focus ring.
- README: rewritten with dual use-case framing (standalone VJ / Wire pipeline),
  improved Expose Parameters section, cleaner quick start, updated mermaid diagram
  with new dark palette, corrected all GitHub links to aday1/Macroverse.

Documentation and CI (post-42)
------------------------------

- Documentation: README screenshot refs updated (optional docs/screenshots/); changelog and docs/index.html aligned; AGENTS.md updated (Blocks playground removed with temp_roliblocks_test cleanup).
- CI: Build workflow now runs go vet; test pipeline (build + vet) runs on push/PR to main. GitHub Pages deploy from Actions (docs/).
- Cleanup: temp_roliblocks_test/ removed; root package.json scripts blocks-playground and blocks removed.

Version 42 "The Wired Atelier"
------------------------------

Release (public GitHub).
- Branding: Macroverse 42 "The Wired Atelier"; removed Pre-Release labeling.
- Binary: Macroverse42.exe (Windows) / Macroverse42 (Linux/Mac). Pipeline and shortcuts updated.
- Default theme: Workbench 3.1 (lighter palette).
- Full Screen button in status bar; F11 support.
- GitHub Actions: build workflow (frontend + Go). GitHub Pages showcase site.
- README and in-app copy: vibrant intro, psychedelic tone; exe name and paths updated.

Version 41 (Pre-Release)
------------------------

Branding
- Restamped product as "Macroverse - Wired Atelier - Version 41 (Pre-Release)" across UI, docs, and backend banner.
- Updated ascii.art to Macroverse (pre-release) banner.
- Binary is Macroverse-41.exe (Windows) / Macroverse-41 (Linux/Mac). Old macroverse-v3/v5 exes removed; all pipeline and docs updated.

Layout and UI
- Index panel restored as resizable left column (split) instead of bottom panel; drag divider to resize (180-480px).
- Index collapse toggles to narrow strip (32px) with expand button; state persisted in localStorage.
- View tabs: Preview, Code, Split (V/H), VJ. Default view configurable in Settings.

Shader management
- Emoji badge styling and index list structure cleanup.
- Trash management: show/hide trash, trash count badge, move to trash from context menu.
- Dead shader tagging: tag as DEAD when auto-fix cannot resolve; show/hide dead filter with count badge.
- Set management: filter by set, manage sets (rename, remove, add).
- List views: List, Compact, Grid with thumbnail previews.
- Paths and Settings (Paths, Settings, Help, Scan) in index toolbar.

VJ Scratchpad
- A/B deck mixer with crossfader; VJ tab in main view.
- Auto VJ feature and deck controls; auto crossfade and shader loading refactor.
- Texture management and webcam support on VJ deck.
- MIDI and OSC mapping; FFT A/B, OSC A/B state; BroadcastChannel for VJ output.
- VJ output state and streaming API; remote stream handling and connection logic.
- Pop-out VJ output window; fix streaming to new clients on port 8765.
- Text templates: neon, dotmatrix, lcd; 16-segment display injection; mouse XY pad.
- VJ pop-out fixes: replace captureStream with dedicated page via BroadcastChannel.

Backend and API
- Git repository API and frontend integration: version history, revert to version.
- VJ output API refactor; status recorder Flush for response handling.
- Shader file filtering and remote stream status improvements.
- Error handling and UI for shader management and Cursor AI integration.

ISF and Wire
- normalizeISFForWire; ISF input handling; precision and block handling improvements.
- Expose parameters (regex-based and AI); Refactor/Vibe (Cursor API).
- Check ISF Wire, Clipboard to Wire, Expose in code toolbar.
- Deep fix: strip unsupported #version for WebGL 1.

Shaders and params
- Parameter parsing and VJ deck texture management.
- Dynamic uniform variables for resolution/scaling (e.g. plasma5.fs, checker.frag, core-borders-screens.fs).
- Params search popover; external control wrap.

Misc
- Preview pop-out with mouse XY pad.
- Default view setting (Split V, Split H, Preview only, Code only).
- WebGL cloud VM gotcha documented in AGENTS.md.
- Cross-platform: console_windows.go / console_other.go build tags; no PowerShell dependency.
- MacroCam MJPEG stream for OBS / virtual webcam.

Earlier (V3 / V5 era)
- Initial Resolume Wire shader lab: index, preview, code editor, parameters, export to Wire.
- SQLite migration from shader-index.json; source paths, scan, thumbnails.
- LLM provider chain (Local, Ollama, Cursor); shader fix chain; auto-commit on save.
- Themes, CRT/scanline/vignette options; Amiga-style UI.
