# Macroverse Session Report: 2026-03-07

## Roliblock LED Processing Controls + Light Theme + VJ Bug Fixes

**Commit:** `c773bb6` on `main`
**Deployment:** Fly.io `macroverse42` (LHR region)

---

## 1. Roliblock VJ Mode Bug Fix

### Problem

Two bugs reported when using Roli Lightpad Block in VJ mode:

1. **LEDs showed preview shader instead of VJ output** when the VJ tab was active
2. **VJ crossfader from Roli was missing** (CC 73 had no effect)

### Root Cause

`syncRoliblockFromView()` in `params.ts` checked `state.mode` to determine LED source and crossfader routing, but the mode defaulted to `'preview'` and was **never auto-synced** when the user switched view tabs. So when the VJ tab was active:

- `mode === 'preview'` remained true
- Preview LED code path ran, VJ LED code path was skipped
- Crossfader callback was set to `null` because mode was not `'vj'`

### Fix

Modified `syncRoliblockFromView()` (params.ts:180-209) to auto-sync the Roli mode with the active view tab:

- **VJ tab active** -> forces mode to `'vj'`, updates radio button UI
- **Leaving VJ tab** (except gallery) -> reverts mode to `'preview'`, updates radio button UI
- **Gallery tab** -> preserves current mode (gallery has its own Roli touch handler)

The `effectiveMode` variable now reads from `roliblockEngine.state.mode` *after* sync, ensuring downstream routing (touch callbacks, crossfader, LED source) is always correct.

### Files Changed

| File | Lines | Change |
|------|-------|--------|
| `frontend/src/panels/params.ts` | 180-209 | Rewrote `syncRoliblockFromView()` with auto-sync logic |

---

## 2. Roliblock LED Image Processing Controls

### Problem

The Roli Lightpad Block has a 15x15 LED grid (225 pixels, BGR565 color). When displaying shader output, the extreme downsampling from typical 640x360+ canvas to 15x15 results in washed-out, low-contrast pixels. Users need the ability to fine-tune LED output quality.

### Solution

Added a full per-pixel image processing pipeline between canvas sampling and LED sending:

```
Canvas -> drawImage (high-quality) -> 15x15 RGBA -> processLedPixels() -> rgbaToBgr565 -> SysEx
                                                         |
                                         contrast / brightness / saturation / gamma
```

### Implementation Details

#### Processing Pipeline (`processLedPixels()`)

Per-pixel processing chain applied to the 225-pixel RGBA buffer:

1. **Brightness** (+/- shift): `c = c + brightness * 255`
2. **Contrast** (scale around midpoint): `c = (c - 128) * contrast + 128`
3. **Saturation** (scale around luma): `luma = 0.299R + 0.587G + 0.114B; c = luma + (c - luma) * saturation`
4. **Gamma** (power curve): `c = 255 * pow(c/255, 1/gamma)`

Short-circuits when all settings are neutral (zero computation cost).

#### Slider Ranges

| Control    | Min  | Max | Default | Neutral | Effect |
|------------|------|-----|---------|---------|--------|
| Contrast   | 0.0  | 3.0 | 1.0     | 1.0     | Punchy LEDs at >1, washed at <1 |
| Brightness | -1.0 | 1.0 | 0.0     | 0.0     | Shift all channels up/down |
| Saturation | 0.0  | 3.0 | 1.0     | 1.0     | Vivid colors at >1, grayscale at 0 |
| Gamma      | 0.2  | 3.0 | 1.0     | 1.0     | Reveal dark detail at >1, crush at <1 |

#### Downsampling Quality Improvement

- **Before:** Created a new `<canvas>` element every frame (25fps), used browser default `imageSmoothingQuality`
- **After:** Cached scratch canvas with `imageSmoothingQuality: 'high'`, `clearRect` between frames
- Result: Better bilinear/bicubic filtering during the 640x360 -> 15x15 downsample, reduces aliasing

#### Persistence

Settings stored in `localStorage` under key `macroverse-roliblock-led`. Auto-loaded on module init, auto-saved on every slider change. Survives page refresh.

#### UI

Four range sliders added to the Roliblock panel under a "LED Processing" sub-header, styled consistently with the existing panel (9px font, `var(--crt-dim)` labels, `var(--amiga-copper)` section header). Each slider has a live numeric readout.

### Files Changed

| File | Lines | Change |
|------|-------|--------|
| `frontend/src/engines/roliblock.ts` | 48-71 | `ledSettings` state object + localStorage load/save |
| `frontend/src/engines/roliblock.ts` | 300-345 | `processLedPixels()` function |
| `frontend/src/engines/roliblock.ts` | 284-302 | Cached scratch canvas + `imageSmoothingQuality: 'high'` |
| `frontend/src/engines/roliblock.ts` | 390 | Integrated `processLedPixels()` into `sampleAndSendLed()` |
| `frontend/src/engines/roliblock.ts` | 428-432 | Added `ledSettings` getter + 4 setter methods on export |
| `frontend/src/panels/params.ts` | 1062-1084 | LED Processing slider HTML |
| `frontend/src/panels/params.ts` | 1475-1498 | Slider event handlers + localStorage init |

### Performance

- 225 pixels x 4 floating-point operations per channel = ~2,700 operations per frame
- At 25fps LED rate (40ms interval), this is negligible (<0.1ms per frame)
- Short-circuit when all values are neutral: zero allocation, returns input directly

---

## 3. Light Theme Preset

### Problem

All 7 existing theme presets were dark palettes. Users working in daylight or bright environments needed a lighter option.

### Solution

Added a **"Light"** theme preset to `THEME_PRESETS` in `themeUtils.ts`:

| Variable | Value | Description |
|----------|-------|-------------|
| amigaBg | `#e8e4f0` | Warm lavender background |
| amigaSurface | `#f4f2f8` | Near-white surface |
| amigaPanel | `#ffffff` | Pure white panels |
| amigaText | `#1a1428` | Near-black text (high contrast) |
| amigaTextDim | `#7a6e98` | Muted purple for secondary text |
| amigaAccent | `#3366bb` | Deep blue accent |
| amigaCopper | `#cc6600` | Warm copper (darkened for light bg) |
| editorBg | `#faf8ff` | Off-white editor |
| editorKeyword | `#8822cc` | Purple keywords |
| editorString | `#227744` | Green strings |
| editorFunction | `#2266bb` | Blue functions |
| editorComment | `#998ab8` | Muted purple comments |

The light theme appears as a button in Settings > Themes, between "Workbench 3.1" and "Synthwave". Clicking it applies instantly; clicking Save persists it.

### Scrollbar Fix

Fixed hardcoded `#1a1040` scrollbar thumb color in `theme.css` to use `var(--bevel-light)`. This ensures scrollbars adapt to any theme, not just dark ones.

### Files Changed

| File | Lines | Change |
|------|-------|--------|
| `frontend/src/themeUtils.ts` | 85-111 | Added Light theme preset |
| `frontend/theme.css` | 41 | Fixed scrollbar thumb to use CSS variable |

---

## 4. Deployment Summary

| Step | Status |
|------|--------|
| Git commit `c773bb6` | Done |
| Git push to `origin/main` | Done |
| Fly.io deploy `macroverse42` | Deployed |

### Fly.io Config

- **App:** `macroverse42`
- **Region:** LHR (London)
- **VM:** shared CPU, 1 core, 256MB RAM
- **Environment:** `READONLY=true`, `DEMO_BANNER=true`, `DISABLE_EXTERNAL_LLM=true`

---

## 5. Technical Architecture Notes

### LED Pipeline (Full Path)

```
Shader Canvas (WebGL, 640x360+)
  |
  v
sampleCanvasToRgba()          -- cached 15x15 scratch canvas, imageSmoothingQuality: 'high'
  |                              drawImage() downsamples to 15x15
  v
Uint8ClampedArray (900 bytes)  -- 225 pixels x 4 channels (RGBA)
  |
  v
processLedPixels()             -- brightness -> contrast -> saturation -> gamma
  |                              short-circuits when all neutral
  v
sendLedToDevice()              -- rgbaToBgr565() per pixel -> 450 bytes
  |                              DataChangeList delta encoding (only changed pixels sent)
  v
buildBlockSysEx()              -- 7-bit packed BLOCKS protocol + checksum
  |
  v
MIDI SysEx                     -- Sent to Roli Lightpad Block via WebMIDI
  |
  v
15x15 RGB LED Grid             -- 65K colors (BGR565)
```

### Rate Limiting

- LED send interval: 40ms (~25fps)
- Touch input: real-time (no rate limit)
- Settings save: on every slider change (throttled by user interaction speed)

### Theme System Architecture

```
theme.css (CSS variables, defaults)
  |
  v
applyTheme() in themeUtils.ts  -- overrides :root CSS variables at runtime
  |
  v
Settings panel                 -- 8 preset buttons + HSV color customization
  |
  v
postSettings() -> server       -- persists to macroverse.db
```

The Light theme is the first bright preset in the system. All CSS throughout the app uses variables (`--amiga-bg`, `--crt-fg`, etc.), so theme switching works globally without any component-level changes.
