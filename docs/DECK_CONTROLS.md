# Deck controls (Steam Deck and gamepads) in the VJ view

The VJ view reads a controller and drives the decks, mixer and library with it. Mouse,
keyboard, touch, MIDI and OSC keep working unchanged. Nothing runs until a controller
appears.

| Source | When |
| --- | --- |
| Kefka native bridge (`ops/kefka/deck-input.py`) | The page is served from `127.0.0.1`/`localhost`. The page reads SSE from `http://127.0.0.1:18783/events`. You get sticks, triggers, trackpads, all buttons and the gyro. The bridge publishes only while the `macroverse-deck` window is focused and deckhypr is in GAME mode (see `docs/KEFKA.md`). |
| External gamepad (Gamepad API, standard mapping) | Any page with no native samples. There are no trackpads or gyro. On a local page the built-in Deck is never read this way, because deckhypr owns it outside GAME mode. |

When a controller connects, all buttons and sticks must be released before anything
acts, and again after the window loses focus. While a Kefka native window is active, the
back-paddle keys that deckhypr types in GAME mode (F2, F4, R, L) are swallowed outside
text fields. Audience (view-only) links never take controller input.

## View, Menu, HUD

- **View** shows or hides the HUD. The HUD lists every control with its live value and
  its current target. It also shows the focused deck, the crossfader, the browse cursor,
  the gyro state and the last action. Touch passes through the HUD. Its two buttons open
  the mapping editor or hide the HUD.
- **Menu** opens the mapping editor. Navigate it with the D-pad, A and B, or by touch.
  LB and RB switch tabs. You can also open it from the command palette with "Deck
  controls: mapping editor".

View and Menu are reserved and cannot be remapped.

## Auto mapping

The defaults are generated from what the VJ deck exposes: the focused deck and its
shader params. They update when you change focus or load a shader. A user override
replaces the auto setting only for the fields it sets, so everything else stays auto.

| Control | Auto target |
| --- | --- |
| LT / RT | Crossfader toward A / B at a rate (pull harder for faster) |
| LB / RB | Focus deck A / B (blue / orange outline) |
| L stick, R stick | The focused deck's continuous params, four per bank (bool, vec and sampler params are skipped). Rate mode, soft curve; up and right increase. |
| L3 | Next param bank (params 5 to 8 and so on) |
| D-pad left / right | Browse cursor in the library by one (repeats while held) |
| D-pad up / down | Browse cursor by ten |
| A / B | Load the cursor shader to deck A / B |
| X | Auto VJ on/off |
| Y | Cut (animated) to the focused deck |
| R3 | Centre the focused deck's mouse |
| L trackpad | Focused deck mouse X/Y (absolute, while touched) |
| Gyro roll | Crossfader (steer right moves toward B) |
| Gyro yaw / pitch | Focused deck mouse X/Y |
| R trackpad touch | Gyro enabler |

## Gyro

The gyro uses relative aiming with a ratchet. It moves its targets only while the
enabler is active:

- Default enabler: finger on the right trackpad.
- Other enablers (set in Menu, Gyro & general tab): hold LT, toggle with R3, finger on
  the left trackpad, always on, or off. The input used as the enabler is left out of the
  auto mapping.

Angles come from the bridge's orientation quaternion as changes since the enabler was
engaged:

- yaw: turn right about world up
- pitch: tilt the back up
- roll: steer clockwise

A deadband applies from activation (1.5° by default; set it in the same tab).

Lifting the enabler leaves the value where it is. Engaging it again starts a new stroke
from wherever the Deck points. While the bridge reports the Deck at rest (that is when it
learns gyro bias), nothing accumulates, so a Deck lying still never drifts a value. At
sensitivity 1, a 60° sweep moves a target across its full range.

## Learn and overrides

- **Learn tab:** pick a target, then move a stick, trigger or trackpad, press a button, or
  tilt the Deck. The first deliberate input is bound to the target. Inputs that were
  already held when learning started must be released first.
- **Learning from the page:** the "Pick on screen…" button highlights the learnable page
  controls. Tap a deck param slider, the crossfader or Auto VJ, then move a control.
- **View or Menu** cancels learning.
- **Inputs tab:** A opens a target picker for the selected control.
  - D-pad left and right set sensitivity.
  - X cycles the mode (rate or absolute, toggle or hold).
  - L3 inverts the control and R3 cycles the curve (linear, soft or expo).
  - Y puts the control back to auto.
- **Gyro & general tab:** "Reset every control to auto" clears all overrides.

The mapping is saved in this browser's localStorage under `macroverse-deck-controls-v1`.
The HUD visibility is saved under `macroverse-deck-hud-visible`.

## Development

- Pure logic lives in `frontend/src/engines/steamdeckMapping.ts`: normalisation, auto
  mapping, overrides, curves, gyro ratchet, learn capture and the mapper. Run its offline
  tests with `cd frontend && npm run test:deck` (node:test, Node 22.6+ type stripping).
- The runtime is `frontend/src/engines/steamdeck.ts`. The UI is
  `frontend/src/ui/steamdeckHud.ts`. The VJ deck registers its control surface in
  `engines/vjController.ts`.
- To use a fake or alternate bridge on a local page, add
  `?deckBridge=http://127.0.0.1:PORT/events` to the URL, or set the localStorage key
  `macroverse-deck-bridge-url`. Only loopback URLs are accepted. Use `off` to disable.
  `?view=vj` opens the VJ view directly.
- For an end-to-end check, run `npm run test:deck:e2e` against a running build
  (`MV_BASE_URL`, default `http://127.0.0.1:18765`). It drives headless Chromium at
  1280x800 with `tests/e2e/fake-deck-bridge.mjs` and a fake Gamepad API, and writes
  screenshots to `$DECK_E2E_DIR` (default: a temp dir).
- For diagnosis on the device, `window.__macroverseDeck` in the page console shows
  `source`, `result` and `bindings`.
