# Macroverse Session Report: 2026-03-07

> Historical note: production is now on Linode at https://macroverse.aday.net.au/
> (Fly.io `macroverse42` retired).

## Multi-Roliblock Support, Per-Deck Assignment, LED Filters, Wire Feature Toggles

**Commit:** `f3f79d3` on `main`
**Deployment (historical):** Fly.io `macroverse42` (LHR region)
**Live URL (historical):** https://macroverse42.fly.dev/

---

## 1. Multi-Roliblock Device Architecture

### Problem

The Roliblock integration was a singleton — only one Roli Lightpad Block device could be connected at a time. Module-level state (`roliblockEngine`) managed a single MIDI input, single MIDI output, single set of LED settings, and single touch callback. Users with two Lightpad Blocks had no way to use both simultaneously.

### Solution

Replaced the singleton module-level pattern with a `RoliblockDevice` class and a `roliblockManager` device manager.

**Key types:**
- `RoliblockDeviceState` — per-device config (id, label, enabled, MIDI input/output IDs, deck assignment, crossfader toggle, LED settings)
- `LedSettings` — contrast, brightness, saturation, gamma, grayscale, invert, posterize, channel isolation
- `DeckAssignment` — `'auto'` | `'deckA'` | `'deckB'` | `'shared'`
- `LedDisplayMode` — `'independent'` | `'stretched'`

**RoliblockDevice class** encapsulates all per-device state:
- Own MIDI input/output connections
- Own handshake state and packet counter
- Own LED processing pipeline (scratch canvas, previous LED data for delta encoding)
- Own mouse and crossfader callbacks
- Methods: `connectInput()`, `connectOutput()`, `doHandshake()`, `sendLedToDevice()`, `sampleCanvasToRgba()`, `processLedPixels()`, `sampleAndSendLed()`, `enable()`, `disable()`

**roliblockManager** provides:
- `devices` Map for N simultaneous devices
- Shared `MIDIAccess` (single `requestMIDIAccess({ sysex: true })` call)
- `autoDetectDevices()` — scans MIDI I/O, matches Roli-like pairs, creates device instances
- `getDeviceForDeck('A'|'B')` — returns the device assigned to a specific deck
- `addDevice()` / `removeDevice()` for manual management
- `saveAll()` / `loadAll()` for localStorage persistence under `'macroverse-roliblocks'`
- Backward-compatible `state` getter and `setMode()` / `setOnMouse()` / `setOnCrossfader()` that delegate to the first device

### Files Changed

| File | Lines Changed | Change |
|------|--------------|--------|
| `frontend/src/engines/roliblock.ts` | ~1142 (+750/-392) | Full rewrite: RoliblockDevice class, roliblockManager, sendStretchedLed, backward compat |
| `frontend/src/render.ts` | ~12 | `roliblockEngine` -> `roliblockManager`, LED send iterates all devices |
| `frontend/src/panels/gallery.ts` | ~8 | `roliblockEngine` -> `roliblockManager`, gallery LED send iterates all devices |

---

## 2. Per-Deck VJ Assignment

### Problem

With a single Roliblock, touch input drove shared `mouseX`/`mouseY` uniforms for both VJ decks. With two devices, users need independent control: Roli 1 drives Deck A parameters, Roli 2 drives Deck B parameters.

### Solution

Added per-deck mouse variables in `vjDeck.ts`:

```typescript
let _vjMouseAX = 0.5, _vjMouseAY = 0.5;  // Deck A specific
let _vjMouseBX = 0.5, _vjMouseBY = 0.5;  // Deck B specific
let _perDeckMouseEnabled = false;

export function setVjMouseForDeckA(x, y): void { ... }
export function setVjMouseForDeckB(x, y): void { ... }
export function setPerDeckMouseEnabled(v): void { ... }
```

Modified `setDeckUniforms()` to accept a `deckKey` parameter (`'A'` | `'B'`), selecting the appropriate mouse coordinates based on deck assignment.

Modified VJ frame LED send logic to dispatch per-device:
- **Independent mode**: Device assigned to `deckA` samples from Deck A canvas, `deckB` from Deck B canvas, `shared` from output canvas
- **Stretched mode**: Samples output canvas to 30x15, splits left 15x15 to device 1 and right 15x15 to device 2

### Files Changed

| File | Lines Changed | Change |
|------|--------------|--------|
| `frontend/src/panels/vjDeck.ts` | ~56 | Per-deck mouse exports, setDeckUniforms deckKey, VJ frame LED multi-device dispatch |

---

## 3. Enhanced LED Filters

### Problem

The existing LED processing only had contrast, brightness, saturation, and gamma controls. Users needed more creative options for the 15x15 LED grid: grayscale conversion, color inversion, posterization, and channel isolation.

### Solution

Extended `processLedPixels()` in `RoliblockDevice` with four new filter stages:

1. **Channel Isolation** (`'all'` | `'r'` | `'g'` | `'b'`) — zeros out non-selected channels
2. **Grayscale** — BT.601 luminance weighting: `0.299R + 0.587G + 0.114B`
3. **Posterize** (0 = off, 2-16 levels) — quantizes each channel to N discrete levels
4. **Invert** — `255 - value` per channel

Fast-path: when all settings are neutral (contrast=1, brightness=0, saturation=1, gamma=1, grayscale=off, invert=off, posterize=0, channelIsolation='all'), returns input data without allocation.

### Files Changed

| File | Lines Changed | Change |
|------|--------------|--------|
| `frontend/src/engines/roliblock.ts` | (included in rewrite) | processLedPixels extended with 4 new filter stages |

---

## 4. Multi-Device Panel UI

### Problem

The params.ts Roliblock panel was designed for a single device with simple mode radio buttons and slider controls. Needed a complete rewrite for N-device management.

### Solution

Rewrote the Roliblock section in `params.ts` with:

**Global controls:**
- Request MIDI button (shared MIDIAccess)
- Refresh Devices button (rescans MIDI I/O)
- Add Device button (creates new device slot)
- LED Display Mode toggle (Independent / Stretched)

**Per-device slots** (rendered dynamically):
- Device header with label and remove button
- Touch Input MIDI select dropdown
- LED Output MIDI select dropdown
- Deck Assignment select (Auto / Deck A / Deck B / Shared)
- Crossfader enable checkbox
- Enable / Disable button
- Full LED Processing section with sliders and controls:
  - Contrast (0-3), Brightness (-1 to 1), Saturation (0-3), Gamma (0.2-3)
  - Grayscale checkbox, Invert checkbox
  - Posterize slider (off / 2-16 levels)
  - Channel isolation radio buttons (All / R / G / B)

**syncRoliblockFromView()** refactored to iterate all devices:
- VJ view: per-deck mouse routing based on device assignment, crossfader callback
- Gallery view: all devices route touch to gallery mouse
- Preview view: all devices route to single preview mouse

### Files Changed

| File | Lines Changed | Change |
|------|--------------|--------|
| `frontend/src/panels/params.ts` | ~481 | Complete Roliblock panel rewrite for multi-device UI + filter controls |

---

## 5. Wire Generation Feature Toggles

### Problem

The `seed-wire-from-sets.js` script generated Wire patches with all features always enabled: MIDI controller nodes (APC40 MK II + Roli Blocks), FFT Spectrum In nodes, webcam Texture In nodes, and full FX chains. Users needed the ability to selectively disable features for simpler patches or specific hardware setups.

### Solution

**New CLI flags:**
```
--fft / --no-fft           Include FFT audio-reactive nodes (default: on)
--webcam / --no-webcam     Include webcam/video input Texture In nodes (default: on)
--glitch / --no-glitch     Include glitch FX chain in GLITCH topology (default: on)
--midi / --no-midi         Include MIDI controller nodes (default: on)
--fx-level basic|advanced  basic = ISF only, advanced = full FX chain (default: advanced)
--interactive              Step-by-step feature selection via readline prompts
```

**Feature flags object** passed through all generation functions:
```javascript
const features = {
  fft: true, webcam: true, glitch: true, midi: true,
  fxLevel: 'advanced'
};
```

**Conditional generation** in all patch functions:
- `generatePatchForGroup()`: `if (features.webcam)` around Texture In nodes, `if (features.midi)` around MIDI section, `if (features.fft)` around FFT section
- `addModulationSection()`: Fixed variable naming conflicts (`cfFid` -> `cfFidOut`), properly scoped MIDI and FFT blocks
- `generateEnhancedPatch()` router and all 5 topology functions (`generateFeedbackPatch`, `generateBeatPatch`, `generateGlitchPatch`, `generateGeometricPatch`, `generateColourPatch`): Accept and pass `features`

**Interactive mode** (`--interactive`):
```
Macroverse Wire Patch Generator
================================

Generate for which set? (all/vj-ambient/vj-techno/...): all
Mode? (standard/enhanced/all): enhanced

Feature toggles:
  Include FFT / audio-reactive nodes? (Y/n): y
  Include webcam / video input nodes? (Y/n): n
  Include MIDI controller nodes? (Y/n): y
  FX level (basic/advanced): advanced

Generating with: fft=yes webcam=no glitch=yes midi=yes fx=advanced
```

**Avenue script help text** updated with description and Related scripts section.

### Key Bug Fix: addModulationSection()

The function had variable naming conflicts:
- Outer `let cfFid` conflicted with inner `const [cfFid, cfFnode]` declarations
- Outer `const faderIds = []` conflicted with inner re-declarations
- FFT section was not wrapped in feature conditional
- MIDI `if` block was opened but not properly closed

**Fix**: Renamed outer to `cfFidOut`, removed inner array re-declarations (push directly to outer arrays), properly closed MIDI block, wrapped FFT in `if (features.fft)`.

### Files Changed

| File | Lines Changed | Change |
|------|--------------|--------|
| `scripts/seed-wire-from-sets.js` | ~345 | Feature toggle flags, addModulationSection fix, interactive mode, all topology signatures updated |
| `scripts/seed-avenue-from-sets.js` | ~14 | Help text updated with description and Related scripts |

---

## Build & Deploy

- **Frontend build**: `npm run build` — success, 54 modules transformed, built in 5.85s
- **Git commit**: `f3f79d3` — 7 files, +1294/-764 lines
- **Fly.io deploy**: Image 9.5 MB, rolling strategy, machine `48e454dce20638` updated
- **Live**: https://macroverse42.fly.dev/

---

## Summary

| Feature | Status | Impact |
|---------|--------|--------|
| Multi-Roliblock (N devices) | Shipped | RoliblockDevice class + roliblockManager, shared MIDIAccess |
| Per-Deck Assignment | Shipped | Roli 1 -> Deck A, Roli 2 -> Deck B (touch + LEDs) |
| Stretched LED Mode | Shipped | 30x15 combined output across 2 Lightpad Blocks |
| Enhanced LED Filters | Shipped | Grayscale, invert, posterize, channel isolation |
| Multi-Device Panel UI | Shipped | Per-device controls in params panel |
| Wire Feature Toggles | Shipped | --fft, --webcam, --midi, --glitch, --fx-level flags |
| Wire Interactive Mode | Shipped | --interactive readline prompts |
| Fly.io Deployment | Live | macroverse42.fly.dev |

**Total changes:** 7 files, +1,294 / -764 lines
