# Macroverse on Kefka (Steam Deck)

Macroverse runs natively on Kefka (SteamOS, Hyprland session) from `~/Apps/macroverse`,
next to ArtBastard, the Yggdrasil Deck kiosk and the NYX desk. Nothing here changes
those apps; the only shared file is one added line in `~/.config/kefka/game-classes`.

| Piece | Where |
| --- | --- |
| Backend + UI | `http://127.0.0.1:8765/` (loopback only by default), `macroverse.service`, enabled at login |
| Deck window | **Macroverse Deck** menu entry, or `~/.local/bin/macroverse-deck-open` (open or focus) |
| Window unit | `macroverse-deck.service`, on demand: flatpak Chrome app window, XWayland, class `macroverse-deck`, fullscreen |
| Input bridge | `macroverse-input.service`, `http://127.0.0.1:18783/events` (SSE) and `/health` |
| Bridge code | `~/.local/share/macroverse/deck-input.py` (source: `ops/kefka/deck-input.py`) |
| Browser profile | `~/.local/share/macroverse-browser` inside the Flatpak home (host: `~/.var/app/com.google.Chrome/.local/share/macroverse-browser`) |
| Overrides | `~/.config/macroverse/kefka.env` (all commented out by default) |
| Data | `~/Apps/macroverse/macroverse.db`, `shader-preview-settings.json`, `shaders/` |

The backend runs as a local, writable install (`MACROVERSE_HOST_MODE=desktop`,
`MACROVERSE_LANE=local`) bound to `127.0.0.1`. For phones, other machines or the gig
audience links, set `MACROVERSE_BIND_HOST=0.0.0.0` in `kefka.env` and
`systemctl --user restart macroverse`. That exposes an unauthenticated, writable
backend to the LAN and tailnet, so leave it off unless needed.

## Controls

While the Deck window is focused, `deckhypr` sees the class `macroverse-deck` in
`game-classes`, switches to GAME mode and stops emulating the desktop mouse/keys.
Use the touchscreen for pointer input in that window. Steam and QAM stay with
`deckhypr`. Switching to any other window gives the desktop controls back.

The page reads the controller in two ways:

- **Built-in Deck** (page loaded from `127.0.0.1`/`localhost`): the read-only bridge.
  It opens the `Steam Deck` and `Steam Deck Motion Sensors` evdev devices without
  grabbing them, and publishes only while deckhypr is in GAME mode, its pad is `kernel`,
  its launcher is closed and the focused Hyprland window has the exact class
  `macroverse-deck`. Otherwise `nativeAllowed` is false and `pad`/`motion` are `null`.
- **External gamepads**: the browser Gamepad API (localhost or HTTPS only).

Bridge events (`data:` JSON, about 50 per second):

```text
connected, motionConnected   reader opened the pad / fresh IMU samples
nativeAllowed, nativeReason  the desktop-ownership gate above
pad.buttons[16]              A B X Y LB RB LT RT View Menu L3 R3 Up Down Left Right (LT/RT analog 0..1)
pad.axes[4]                  lx ly rx ry, -1..1, +y down; rest centre learned, unattended drift parked
pad.touch[2], pad.touchpads  left/right trackpad touched, [[x,y],[x,y]] -1..1
pad.connection               changes on reconnect, resync or gate handoff: require neutral input again
motion.q                     body->world quaternion [w,x,y,z]; +X right, +Y up, +Z toward the user
motion.rate, gravity, bias, resting   deg/s, g, learned gyro bias, at rest
```

`/health` returns the same fields without `pad`/`motion`, plus `bridge` and
`deckClass`. Both paths answer only the exact origins `http://127.0.0.1:8765` and
`http://localhost:8765` (403 otherwise). Kefka's right stick drifts by itself; the
bridge parks a steady deflected stick while the Deck rests untouched.

## Install / update

From a workstation with Go and Node (holybell), in this repo:

```bash
ops/kefka/deploy-kefka.sh /path/to/build-dir --install   # add --defer-deckhypr while the Deck is in use
```

That builds the current tree (vite + `CGO_ENABLED=0` linux/amd64), rsyncs it to
`kefka:~/Apps/macroverse` (keeping `macroverse.db`, settings and the index) and runs
`ops/kefka/install-kefka.sh` there. The installer backs up every file it replaces
(`*.bak.<stamp>`), installs the units, bridge, launcher, icon and menu entry, seeds
missing settings/index files, adds `^macroverse-deck$` to `game-classes` once and
restarts `deckhypr` only when that file changed (`--defer-deckhypr`: on the first
`macroverse-deck-open` instead). It enables and restarts `macroverse.service` and
never opens the window.

## Checks

```bash
python3 ops/kefka/test-input.py                       # offline bridge tests (holybell or Kefka)
ops/kefka/render-smoke.sh OUT_DIR                     # holybell: headless Chrome on Kefka, screenshots + report.json
curl -fsS http://127.0.0.1:8765/api/config
curl -fsS -H 'Origin: http://127.0.0.1:8765' http://127.0.0.1:18783/health
systemctl --user status macroverse macroverse-input macroverse-deck
hyprctl clients -j | grep '"class"'
```

`connected:true` proves device access; `nativeAllowed:true` additionally needs the
focused Deck window in GAME mode. Neither proves a button was pressed.
`render-smoke.sh` runs a niced, windowless flatpak Chrome on Kefka with a throwaway
profile, loads a shader into the preview and the VJ decks, and reports the WebGL
renderer, compile-error overlay, console errors and whether each canvas animates.

## Stop / roll back

```bash
systemctl --user stop macroverse-deck macroverse-input
systemctl --user disable --now macroverse
```

To remove: delete the three `~/.config/systemd/user/macroverse*.service` units,
`~/.local/bin/macroverse-deck-open`, `~/.local/share/applications/macroverse.desktop`,
`~/.local/share/icons/hicolor/scalable/apps/macroverse.svg` and
`~/.local/share/macroverse/`; remove only the `^macroverse-deck$` line and its comment
from `game-classes` (or restore the `.bak.<stamp>` copy), then
`systemctl --user daemon-reload && systemctl --user restart deckhypr`.
`~/Apps/macroverse` and the browser profile hold user data; keep them unless asked.
