import { entries, listSetFilter, getThumbnail, appSettings } from '../state.js';
import { fetchShader } from '../api.js';
import { prepareFragmentForOffscreenRender, stripLeadingGarbage } from '../render.js';
import { parseExposeFromSource, type ExposeItem } from './params.js';
import {
  audioEngine,
  autoAssignFftBandMap,
  FFT_BAND_LABELS,
  FFT_BAND_COUNT,
  isFftAutoSelectEnabled,
  setFftAutoSelectEnabled,
} from '../engines/audio.js';
import { oscEngine } from '../engines/osc.js';
import { midiEngine } from '../engines/midi.js';
import { vjController } from '../engines/vjController.js';
import { roliblockManager, sendStretchedLed } from '../engines/roliblock.js';
import { initVjContextMenus } from './vjContextMenu.js';
import {
  getAudienceParticipationEnabled,
  pushAudienceParticipation,
  setAudienceParticipationEnabled,
} from '../vjAudienceParticipation.js';
import { getVjSessionId, isVjViewOnlyMode } from '../vjSession.js';
import { onAudienceMouse, onVjConfig } from '../vjWs.js';
import { ensureVjTokens, vjControlQuery, vjViewQuery, clearVjSessionTokens } from '../vjTokens.js';
import {
  gigOutputQrLayoutFromPointer,
  getGigOutputQrFramePayload,
  getGigOutputQrLayout,
  isGigOutputQrActive,
  isGigOutputQrVisible,
  isGigStreamQrActive,
  refreshGigOutputQrSession,
  registerGigOutputQrCanvas,
  setGigOutputQrFrameRelay,
  setGigOutputQrLayout,
  setGigOutputQrVisible,
} from '../gigOutputQr.js';
import { createVjPreviewGigQrBlock } from '../gigQrUi.js';
import { createRichSlider, enhanceRangeInput, sliderColorForKey, updateSliderFill } from '../ui/richSlider.js';
import { registerShaderDropTarget } from '../shaderPointerDrag.js';
import { connectVjSession, onRemoteVjControl, onVjShaderLive, publishVjControl } from '../vjWs.js';
import { applyVjLibraryFilter, loadVjLibraryFilter } from '../vjLibraryFilter.js';
import { buildVjLibraryFilterStrip } from '../vjLibraryFilterUI.js';
import {
  CROSSFADE_TAP_MS,
  startCrossfadeAnim,
  tickCrossfadeAnim,
  type CrossfadeAnim,
} from '../vjCrossfadeAnim.js';
import { isDeckLiveBound, setDeckLiveBind, toggleDeckLiveBind } from '../vjLiveBind.js';
import { getDeckQrState, renderDeckQrOverlay, setDeckQrEnabled, type VjDeckQrId } from '../vjDeckQr.js';
import { getCodeOverlayState, renderCodeOverlayFromState } from '../vjCodeOverlay.js';
import { applyOutputFxCanvas, getOutputFxState } from '../vjOutputFx.js';
import { buildGigOutputUrl } from '../gigQr.js';
import {
  isMixMode,
  mixModeLabelForInt,
  MIX_MODES,
  pickRandomMixModeInt,
  resolveMixModeInt,
  VJ_MIX_FRAG_SRC,
  type MixMode,
} from '../vjMixShader.js';
import {
  AUTO_VJ_BAR_BEATS,
  AUTO_VJ_BEAT_CYCLES,
  AUTO_VJ_PARAM_MODES,
  AUTO_VJ_STORE,
  autoVjBeatClock,
  autoVjParamModeLabel,
  createAutoVjParamState,
  loadAutoVjParamConfig,
  onAutoVjBeatTick,
  pickRandomAutoVjParamMode,
  saveAutoVjStorage,
  tickAutoVjDeckParams,
  type AutoVjParamConfig,
} from '../autoVjEngine.js';
import { enqueueIdleThumbnailGeneration } from '../init/idleThumbnailGen.js';

const CLIPS_PER_PAGE = 40;

const VJ_FFT_A_KEY = 'macroverse-vj-fft-a';
const VJ_FFT_B_KEY = 'macroverse-vj-fft-b';
const VJ_OSC_A_KEY = 'macroverse-vj-osc-a';
const VJ_OSC_B_KEY = 'macroverse-vj-osc-b';

const VJ_FLEX_DECKS_KEY = 'macroverse-vj-flex-decks-v5';
const VJ_FLEX_PREVIEW_KEY = 'macroverse-vj-flex-preview-v5';
const VJ_FLEX_CONTROLS_KEY = 'macroverse-vj-flex-controls-v5';
const VJ_QR_WIDTH_KEY = 'macroverse-vj-qr-width';
const VJ_FLEX_DECKS_DEFAULT = 7.5;
const VJ_FLEX_PREVIEW_DEFAULT = 2.2;
const VJ_FLEX_CONTROLS_DEFAULT = 1.3;
const VJ_QR_WIDTH_DEFAULT = 220;
const VJ_QR_WIDTH_MIN = 140;
const VJ_QR_WIDTH_MAX = 400;
const VJ_LAYOUT_VERSION = '14';
const VJ_CAROUSEL_MAX = 150;

function readVjLayoutNumber(key: string, fallback: number): number {
  try {
    const v = parseFloat(localStorage.getItem(key) || '');
    return Number.isFinite(v) && v > 0 ? v : fallback;
  } catch {
    return fallback;
  }
}

function saveVjLayoutNumber(key: string, value: number): void {
  try { localStorage.setItem(key, String(Math.round(value * 100) / 100)); } catch (_) {}
}

function bindVjRowResizer(
  handle: HTMLElement,
  top: HTMLElement,
  bottom: HTMLElement,
  minTop = 0.9,
  minBottom = 0.9,
  onCommit?: () => void
): void {
  handle.title = 'Drag to resize rows (double-click to reset)';
  const onPointerDown = (e: PointerEvent) => {
    if (e.button !== 0) return;
    e.preventDefault();
    handle.setPointerCapture(e.pointerId);
    document.body.style.cursor = 'row-resize';
    const startY = e.clientY;
    const startTop = parseFloat(top.style.flex || '') || parseFloat(getComputedStyle(top).flexGrow) || 1;
    const startBottom = parseFloat(bottom.style.flex || '') || parseFloat(getComputedStyle(bottom).flexGrow) || 1;
    const parent = top.parentElement;
    if (!parent) return;
    const totalFlex = startTop + startBottom;
    const onMove = (ev: PointerEvent) => {
      const delta = ev.clientY - startY;
      const flexDelta = (delta / Math.max(1, parent.clientHeight)) * totalFlex;
      const newTop = Math.max(minTop, startTop + flexDelta);
      const newBottom = Math.max(minBottom, startBottom - flexDelta);
      top.style.flex = String(newTop);
      bottom.style.flex = String(newBottom);
      onCommit?.();
    };
    const onUp = () => {
      document.body.style.cursor = '';
      handle.releasePointerCapture(e.pointerId);
      handle.removeEventListener('pointermove', onMove);
      handle.removeEventListener('pointerup', onUp);
      handle.removeEventListener('pointercancel', onUp);
    };
    handle.addEventListener('pointermove', onMove);
    handle.addEventListener('pointerup', onUp);
    handle.addEventListener('pointercancel', onUp);
  };
  handle.addEventListener('pointerdown', onPointerDown);
}

function bindVjPreviewQrResizer(
  handle: HTMLElement,
  previewCol: HTMLElement,
  qrRoot: HTMLElement,
  onCommit?: (width: number) => void
): void {
  handle.title = 'Drag to resize preview vs QR (double-click to reset)';
  const onPointerDown = (e: PointerEvent) => {
    if (e.button !== 0) return;
    e.preventDefault();
    handle.setPointerCapture(e.pointerId);
    document.body.style.cursor = 'col-resize';
    const startX = e.clientX;
    const startQrW = qrRoot.offsetWidth;
    const onMove = (ev: PointerEvent) => {
      const delta = startX - ev.clientX;
      const w = Math.max(VJ_QR_WIDTH_MIN, Math.min(VJ_QR_WIDTH_MAX, startQrW + delta));
      qrRoot.style.flex = `0 0 ${w}px`;
      onCommit?.(w);
      vjPreviewResizeFn?.();
    };
    const onUp = () => {
      document.body.style.cursor = '';
      handle.releasePointerCapture(e.pointerId);
      handle.removeEventListener('pointermove', onMove);
      handle.removeEventListener('pointerup', onUp);
      handle.removeEventListener('pointercancel', onUp);
    };
    handle.addEventListener('pointermove', onMove);
    handle.addEventListener('pointerup', onUp);
    handle.addEventListener('pointercancel', onUp);
  };
  handle.addEventListener('pointerdown', onPointerDown);
  handle.addEventListener('dblclick', () => {
    qrRoot.style.flex = `0 0 ${VJ_QR_WIDTH_DEFAULT}px`;
    saveVjLayoutNumber(VJ_QR_WIDTH_KEY, VJ_QR_WIDTH_DEFAULT);
    vjPreviewResizeFn?.();
  });
}

const vjChannel = new BroadcastChannel('macroverse-vj-output');

let lastRelayPost = 0;
const RELAY_THROTTLE_MS = 250;

/** True while the VJ view tab is active; pauses WebGL loop and API relay when false. */
let vjDeckTabActive = false;
let vjFrameLoopStart: (() => void) | null = null;
let vjFrameLoopStop: (() => void) | null = null;
let vjPreviewResizeFn: (() => void) | null = null;
let vjActiveLoadDeckByPath: ((deck: 'A' | 'B', path: string) => void) | null = null;

if (typeof window !== 'undefined') {
  window.addEventListener('macroverse-vj-load-path', ((ev: Event) => {
    const detail = (ev as CustomEvent<{ deck?: string; path?: string }>).detail;
    if (!detail?.path || !vjActiveLoadDeckByPath) return;
    vjActiveLoadDeckByPath(detail.deck === 'B' ? 'B' : 'A', detail.path);
  }) as EventListener);
}

export function setVjDeckTabActive(active: boolean): void {
  vjDeckTabActive = active;
  if (active) {
    vjPreviewResizeFn?.();
    vjFrameLoopStart?.();
  } else {
    vjFrameLoopStop?.();
  }
}

function sendVJMessage(msg: unknown): void {
  vjChannel.postMessage(msg);
  const msgType =
    typeof msg === 'object' && msg !== null ? (msg as { type?: string }).type : undefined;
  const isFrame = msgType === 'frame';
  if (isFrame && !vjDeckTabActive && !isGigOutputQrActive() && !isGigStreamQrActive()) return;
  const now = performance.now();
  if (isFrame) {
    if (now - lastRelayPost < RELAY_THROTTLE_MS) return;
    lastRelayPost = now;
  }
  fetch(`/api/vj-output/state?${vjControlQuery()}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(msg)
  }).then((res) => {
    if (res.status === 403) clearVjSessionTokens();
  }).catch(() => {});
}

function fuzzyMatch(query: string, text: string): boolean {
  const q = query.trim().toLowerCase();
  if (!q) return true;
  const t = text.toLowerCase();
  let i = 0;
  for (let j = 0; j < t.length && i < q.length; j++) {
    if (t[j] === q[i]) i++;
  }
  return i === q.length;
}

function carouselDisplayName(e: IndexEntry): string {
  return (e.fixedName ?? e.name ?? e.path ?? '').slice(0, 32);
}

function carouselThumbUrl(path: string): string {
  const url = getThumbnail(path);
  if (!url) return '';
  if (url.startsWith('data:') || url.startsWith('blob:')) return url;
  if (url.startsWith('http://') || url.startsWith('https://') || url.startsWith('/')) return url;
  // Bare hostname-like tokens cause ERR_NAME_NOT_RESOLVED if used as img.src
  if (/^[a-z0-9.-]+$/i.test(url) && !url.includes('/')) return '';
  return '';
}

function buildVjShaderCarousel(opts: {
  getShaderList: () => IndexEntry[];
  onLoadDeck: (deck: 'A' | 'B', path: string) => void;
  getActivePaths: () => { a: string; b: string };
}): {
  root: HTMLElement;
  refresh: () => void;
  updateActive: () => void;
  refreshThumb: (path: string) => void;
  scrollToPath: (path: string) => void;
} {
  const root = document.createElement('div');
  root.className = 'vj-shader-carousel panel-section';

  const head = document.createElement('div');
  head.className = 'vj-shader-carousel-head';

  const title = document.createElement('div');
  title.className = 'panel-section-head vj-shader-carousel-title';
  title.textContent = 'Shader gallery';

  const countEl = document.createElement('span');
  countEl.className = 'vj-shader-carousel-count';

  head.appendChild(title);
  head.appendChild(countEl);

  const trackWrap = document.createElement('div');
  trackWrap.className = 'vj-shader-carousel-track-wrap';

  const scrollLeft = document.createElement('button');
  scrollLeft.type = 'button';
  scrollLeft.className = 'vj-shader-carousel-scroll-btn';
  scrollLeft.textContent = '\u2039';
  scrollLeft.title = 'Scroll left';

  const scrollRight = document.createElement('button');
  scrollRight.type = 'button';
  scrollRight.className = 'vj-shader-carousel-scroll-btn';
  scrollRight.textContent = '\u203A';
  scrollRight.title = 'Scroll right';

  const track = document.createElement('div');
  track.className = 'vj-shader-carousel-track';
  track.setAttribute('role', 'listbox');
  track.setAttribute('aria-label', 'Shader library carousel');

  trackWrap.appendChild(scrollLeft);
  trackWrap.appendChild(track);
  trackWrap.appendChild(scrollRight);

  root.appendChild(head);
  root.appendChild(trackWrap);

  const cardByPath = new Map<string, HTMLElement>();
  let lastCarouselFireKey = '';
  let lastCarouselFireAt = 0;

  function fireCarouselLoad(deck: 'A' | 'B', path: string): void {
    const key = deck + ':' + path;
    const now = performance.now();
    if (key === lastCarouselFireKey && now - lastCarouselFireAt < 300) return;
    lastCarouselFireKey = key;
    lastCarouselFireAt = now;
    const card = cardByPath.get(path);
    if (card) {
      card.classList.add(deck === 'A' ? 'vj-carousel-tap-a' : 'vj-carousel-tap-b');
      window.setTimeout(() => card.classList.remove('vj-carousel-tap-a', 'vj-carousel-tap-b'), 220);
    }
    opts.onLoadDeck(deck, path);
  }

  let carouselVisObserver: IntersectionObserver | null = null;
  let carouselScrollTimer: ReturnType<typeof setTimeout> | null = null;

  function pathsVisibleInTrack(): string[] {
    if (cardByPath.size === 0) return [];
    const trackRect = track.getBoundingClientRect();
    const pad = 48;
    const out: string[] = [];
    for (const [path, card] of cardByPath) {
      const r = card.getBoundingClientRect();
      if (r.right < trackRect.left - pad || r.left > trackRect.right + pad) continue;
      if (!getThumbnail(path)) out.push(path);
    }
    return out;
  }

  function requestVisibleCarouselThumbnails(): void {
    if (!AUTO_GENERATE_MISSING_CAROUSEL_THUMBNAILS) return;
    const visible = pathsVisibleInTrack();
    if (visible.length > 0) enqueueIdleThumbnailGeneration(visible, { front: true });
  }

  function bindCarouselVisibilityObserver(): void {
    carouselVisObserver?.disconnect();
    if (typeof IntersectionObserver === 'undefined') {
      requestAnimationFrame(() => requestVisibleCarouselThumbnails());
      return;
    }
    carouselVisObserver = new IntersectionObserver(
      (observed) => {
        if (!AUTO_GENERATE_MISSING_CAROUSEL_THUMBNAILS) return;
        const need: string[] = [];
        for (const ent of observed) {
          if (!ent.isIntersecting) continue;
          const path = (ent.target as HTMLElement).dataset.path;
          if (path && !getThumbnail(path)) need.push(path);
        }
        if (need.length > 0) enqueueIdleThumbnailGeneration(need, { front: true });
      },
      { root: track, rootMargin: '64px 0px', threshold: 0.08 }
    );
    for (const card of cardByPath.values()) carouselVisObserver.observe(card);
    requestAnimationFrame(() => requestVisibleCarouselThumbnails());
  }

  function scheduleVisibleCarouselThumbnails(): void {
    if (carouselScrollTimer) return;
    carouselScrollTimer = setTimeout(() => {
      carouselScrollTimer = null;
      requestVisibleCarouselThumbnails();
    }, 180);
  }

  track.addEventListener('scroll', scheduleVisibleCarouselThumbnails, { passive: true });

  function filterEntries(): IndexEntry[] {
    return opts.getShaderList();
  }

  function makeThumbEl(e: IndexEntry): HTMLElement {
    const path = e.path ?? '';
    const name = carouselDisplayName(e);
    const url = path ? carouselThumbUrl(path) : '';
    if (url) {
      const img = document.createElement('img');
      img.className = 'vj-shader-carousel-thumb';
      img.src = url;
      img.alt = name;
      img.draggable = false;
      return img;
    }
    const ph = document.createElement('span');
    ph.className = 'vj-shader-carousel-thumb-ph';
    ph.textContent = (name.charAt(0) || '?').toUpperCase();
    return ph;
  }

  function updateActive(): void {
    const { a, b } = opts.getActivePaths();
    for (const [path, card] of cardByPath) {
      card.classList.toggle('is-on-a', path === a && !!a);
      card.classList.toggle('is-on-b', path === b && !!b);
    }
  }

  function refreshThumb(path: string): void {
    const card = cardByPath.get(path);
    if (!card) return;
    const thumbSlot = card.querySelector('.vj-shader-carousel-thumb-slot');
    if (!thumbSlot) return;
    const e = opts.getShaderList().find((x) => (x.path ?? '') === path);
    if (!e) return;
    thumbSlot.replaceChildren(makeThumbEl(e));
  }

  function render(): void {
    const filtered = filterEntries();
    const capped = filtered.length > VJ_CAROUSEL_MAX;
    const slice = capped ? filtered.slice(0, VJ_CAROUSEL_MAX) : filtered;
    cardByPath.clear();
    track.replaceChildren();

    if (slice.length === 0) {
      const empty = document.createElement('div');
      empty.className = 'vj-shader-carousel-empty';
      empty.textContent = opts.getShaderList().length === 0 ? 'No shaders in library' : 'No matches';
      track.appendChild(empty);
    } else {
      for (const e of slice) {
        const path = e.path ?? '';
        if (!path) continue;
        const name = carouselDisplayName(e);
        const card = document.createElement('div');
        card.className = 'vj-shader-carousel-card';
        card.dataset.path = path;
        card.title = path + ' — tap preview for Deck A, or use A / B buttons';
        card.draggable = false;

        const thumbSlot = document.createElement('div');
        thumbSlot.className = 'vj-shader-carousel-thumb-slot';
        thumbSlot.title = 'Load on Deck A';
        thumbSlot.appendChild(makeThumbEl(e));

        const nameEl = document.createElement('div');
        nameEl.className = 'vj-shader-carousel-name';
        nameEl.textContent = name;
        nameEl.title = 'Load on Deck A';

        const actions = document.createElement('div');
        actions.className = 'vj-shader-carousel-actions';

        const btnA = document.createElement('button');
        btnA.type = 'button';
        btnA.className = 'vj-shader-carousel-load vj-shader-carousel-load-a';
        btnA.textContent = 'A';
        btnA.title = 'Load on Deck A';
        btnA.addEventListener('click', (ev) => {
          ev.preventDefault();
          ev.stopPropagation();
          fireCarouselLoad('A', path);
        });

        const btnB = document.createElement('button');
        btnB.type = 'button';
        btnB.className = 'vj-shader-carousel-load vj-shader-carousel-load-b';
        btnB.textContent = 'B';
        btnB.title = 'Load on Deck B';
        btnB.addEventListener('click', (ev) => {
          ev.preventDefault();
          ev.stopPropagation();
          fireCarouselLoad('B', path);
        });

        const loadDeckAFromPreview = (ev: Event): void => {
          ev.preventDefault();
          ev.stopPropagation();
          fireCarouselLoad('A', path);
        };
        thumbSlot.addEventListener('click', loadDeckAFromPreview);
        nameEl.addEventListener('click', loadDeckAFromPreview);

        actions.appendChild(btnA);
        actions.appendChild(btnB);

        card.appendChild(thumbSlot);
        card.appendChild(nameEl);
        card.appendChild(actions);

        track.appendChild(card);
        cardByPath.set(path, card);
      }
    }

    const total = filtered.length;
    countEl.textContent = capped ? `${VJ_CAROUSEL_MAX} of ${total} — filter to see more` : String(total);
    updateActive();
    bindCarouselVisibilityObserver();
  }

  function scrollToPath(path: string): void {
    const card = cardByPath.get(path);
    if (card) card.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'nearest' });
  }

  root.addEventListener('keydown', (ev) => {
    if (ev.key === 'ArrowLeft') {
      ev.preventDefault();
      scrollLeft.click();
    } else if (ev.key === 'ArrowRight') {
      ev.preventDefault();
      scrollRight.click();
    }
  });

  const scrollStep = () => Math.max(120, track.clientWidth * 0.6);
  scrollLeft.addEventListener('click', () => { track.scrollBy({ left: -scrollStep(), behavior: 'smooth' }); });
  scrollRight.addEventListener('click', () => { track.scrollBy({ left: scrollStep(), behavior: 'smooth' }); });

  render();

  let thumbProgressTimer: ReturnType<typeof setTimeout> | null = null;
  const refreshLoadedThumbs = () => {
    for (const path of cardByPath.keys()) {
      if (getThumbnail(path)) refreshThumb(path);
    }
  };
  window.addEventListener('thumbnails-progress', () => {
    if (thumbProgressTimer) return;
    thumbProgressTimer = setTimeout(() => {
      thumbProgressTimer = null;
      refreshLoadedThumbs();
    }, 150);
  });
  window.addEventListener('thumbnails-loading-done', () => {
    refreshLoadedThumbs();
    requestVisibleCarouselThumbnails();
  });
  window.addEventListener('thumbnails-generating-done', refreshLoadedThumbs);

  return { root, refresh: render, updateActive, refreshThumb, scrollToPath };
}

const DECK_CANVAS_W = 420;
const DECK_CANVAS_H = 236;
/** Fallback internal size until preview column is measured. */
const OUTPUT_CANVAS_FALLBACK_W = 960;
const OUTPUT_CANVAS_FALLBACK_H = 540;
const AUTO_GENERATE_MISSING_CAROUSEL_THUMBNAILS = false;

const VERT_SRC = `precision highp float;
attribute vec2 a_pos;
varying vec2 v_uv;
void main() {
  vec2 uv = a_pos * 0.5 + 0.5;
  v_uv = uv;
  gl_Position = vec4(a_pos, 0.0, 1.0);
}`;

const QUAD_VERTS = new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]);

function getSampler2DNamesFromPrepared(preparedFrag: string): string[] {
  const re = /uniform\s+sampler2D\s+(\w+)\s*[;=]/gi;
  const out: string[] = [];
  let m: RegExpExecArray | null;
  while ((m = re.exec(preparedFrag)) !== null) out.push(m[1]);
  return out;
}

function compileShader(gl: WebGLRenderingContext, type: number, src: string): WebGLShader {
  const s = gl.createShader(type);
  if (!s) throw new Error('createShader failed');
  gl.shaderSource(s, src);
  gl.compileShader(s);
  if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) {
    const log = gl.getShaderInfoLog(s);
    gl.deleteShader(s);
    throw new Error(log || 'compile failed');
  }
  return s;
}

function createDeckProgram(gl: WebGLRenderingContext, fragSrc: string): WebGLProgram {
  const prepared = prepareFragmentForOffscreenRender(stripLeadingGarbage(fragSrc || ''));
  const v = compileShader(gl, gl.VERTEX_SHADER, VERT_SRC);
  const f = compileShader(gl, gl.FRAGMENT_SHADER, prepared);
  const p = gl.createProgram();
  if (!p) throw new Error('createProgram failed');
  gl.attachShader(p, v);
  gl.attachShader(p, f);
  gl.linkProgram(p);
  gl.deleteShader(v);
  gl.deleteShader(f);
  if (!gl.getProgramParameter(p, gl.LINK_STATUS)) {
    const log = gl.getProgramInfoLog(p);
    gl.deleteProgram(p);
    throw new Error(log || 'link failed');
  }
  return p;
}

function createMixProgram(gl: WebGLRenderingContext): WebGLProgram {
  const v = compileShader(gl, gl.VERTEX_SHADER, VERT_SRC);
  const f = compileShader(gl, gl.FRAGMENT_SHADER, VJ_MIX_FRAG_SRC);
  const p = gl.createProgram();
  if (!p) throw new Error('createProgram failed');
  gl.attachShader(p, v);
  gl.attachShader(p, f);
  gl.linkProgram(p);
  gl.deleteShader(v);
  gl.deleteShader(f);
  if (!gl.getProgramParameter(p, gl.LINK_STATUS)) {
    const log = gl.getProgramInfoLog(p);
    gl.deleteProgram(p);
    throw new Error(log || 'link failed');
  }
  return p;
}

function createTextureFromCanvas(gl: WebGLRenderingContext): WebGLTexture {
  const tex = gl.createTexture();
  if (!tex) throw new Error('createTexture failed');
  gl.bindTexture(gl.TEXTURE_2D, tex);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
  return tex;
}

function updateTextureFromCanvas(gl: WebGLRenderingContext, tex: WebGLTexture, canvas: HTMLCanvasElement): void {
  gl.bindTexture(gl.TEXTURE_2D, tex);
  gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, canvas);
}

let _vjMouseXRef = 0.5;
let _vjMouseYRef = 0.5;
let _vjMouseAX = 0.5;
let _vjMouseAY = 0.5;
let _vjMouseBX = 0.5;
let _vjMouseBY = 0.5;
let _perDeckMouseEnabled = true;

function getDeckMouse(deckId: 'A' | 'B'): { mx: number; my: number } {
  if (!_perDeckMouseEnabled) {
    return { mx: _vjMouseXRef, my: _vjMouseYRef };
  }
  if (deckId === 'A') return { mx: _vjMouseAX, my: _vjMouseAY };
  return { mx: _vjMouseBX, my: _vjMouseBY };
}

function setDeckMouseFromPointer(
  canvas: HTMLCanvasElement,
  deckKey: 'A' | 'B',
  clientX: number,
  clientY: number
): void {
  const rect = canvas.getBoundingClientRect();
  if (rect.width < 1 || rect.height < 1) return;
  const x = Math.max(0, Math.min(1, (clientX - rect.left) / rect.width));
  const y = Math.max(0, Math.min(1, 1 - (clientY - rect.top) / rect.height));
  if (deckKey === 'A') setVjMouseForDeckA(x, y);
  else setVjMouseForDeckB(x, y);
}

function attachDeckCanvasPointer(canvas: HTMLCanvasElement, deckKey: 'A' | 'B'): void {
  canvas.classList.add('vj-deck-canvas');
  const onPointer = (clientX: number, clientY: number) => {
    setDeckMouseFromPointer(canvas, deckKey, clientX, clientY);
  };
  canvas.addEventListener('pointerdown', (e) => {
    onPointer(e.clientX, e.clientY);
    canvas.setPointerCapture(e.pointerId);
    e.preventDefault();
  });
  canvas.addEventListener('pointermove', (e) => {
    if (canvas.hasPointerCapture(e.pointerId) || e.buttons !== 0) {
      onPointer(e.clientX, e.clientY);
    }
  });
  canvas.addEventListener('pointerup', (e) => {
    try { canvas.releasePointerCapture(e.pointerId); } catch (_) {}
  });
  canvas.addEventListener('pointercancel', (e) => {
    try { canvas.releasePointerCapture(e.pointerId); } catch (_) {}
  });
}

export function setVjMouseFromRoliblock(x: number, y: number): void {
  _vjMouseXRef = Math.max(0, Math.min(1, x));
  _vjMouseYRef = Math.max(0, Math.min(1, y));
}

export function setVjMouseForDeckA(x: number, y: number): void {
  _vjMouseAX = Math.max(0, Math.min(1, x));
  _vjMouseAY = Math.max(0, Math.min(1, y));
}

export function setVjMouseForDeckB(x: number, y: number): void {
  _vjMouseBX = Math.max(0, Math.min(1, x));
  _vjMouseBY = Math.max(0, Math.min(1, y));
}

export function setPerDeckMouseEnabled(v: boolean): void {
  _perDeckMouseEnabled = v;
}

export let vjOutputCanvasRef: HTMLCanvasElement | null = null;

function setDeckUniforms(
  gl: WebGLRenderingContext,
  prog: WebGLProgram,
  w: number,
  h: number,
  time: number,
  overrides?: Record<string, number | boolean>,
  meta?: { id: string; type: string }[],
  deckKey?: 'A' | 'B'
): void {
  gl.useProgram(prog);
  const { mx, my } = deckKey ? getDeckMouse(deckKey) : { mx: _vjMouseXRef, my: _vjMouseYRef };
  const timeLoc = gl.getUniformLocation(prog, 'TIME');
  if (timeLoc) gl.uniform1f(timeLoc, time);
  const resLoc = gl.getUniformLocation(prog, 'RENDERSIZE');
  if (resLoc) gl.uniform2f(resLoc, w, h);
  const tsLoc = gl.getUniformLocation(prog, 'uTimeScale');
  if (tsLoc) gl.uniform1f(tsLoc, 1.0);
  const mouseLoc = gl.getUniformLocation(prog, 'uMouse');
  if (mouseLoc) gl.uniform2f(mouseLoc, mx, my);
  const iFrameLoc = gl.getUniformLocation(prog, 'iFrame');
  if (iFrameLoc) gl.uniform1f(iFrameLoc, Math.floor(time * 60));
  if (overrides && meta) {
    for (const p of meta) {
      if (overrides[p.id] === undefined) continue;
      const loc = gl.getUniformLocation(prog, p.id);
      if (!loc) continue;
      if (p.type === 'bool') gl.uniform1i(loc, (overrides[p.id] as boolean) ? 1 : 0);
      else gl.uniform1f(loc, overrides[p.id] as number);
    }
  }
}

let vjDeckInitialized = false;
let onVjShaderIndexUpdated: (() => void) | null = null;

if (typeof window !== 'undefined') {
  window.addEventListener('macroverse-shader-index-updated', () => {
    onVjShaderIndexUpdated?.();
  });
}

export function initVJDeck(): void {
  const container = document.getElementById('vjDeckContainer');
  if (!container) return;

  const existing = container.querySelector('.vj-deck-wrap') as HTMLElement | null;
  if (existing) {
    if (existing.dataset.layoutV === VJ_LAYOUT_VERSION && existing.querySelector('.vj-shader-carousel')) {
      if (vjDeckTabActive) {
        vjFrameLoopStop?.();
        vjFrameLoopStart?.();
      }
      return;
    }
    vjFrameLoopStop?.();
    vjActiveLoadDeckByPath = null;
    existing.remove();
    vjDeckInitialized = false;
  }
  const pendingLoad = container.querySelector('.vj-deck-loading');
  if (pendingLoad) {
    const pendingAt = Number(pendingLoad.getAttribute('data-started') || '0');
    if (pendingAt && Date.now() - pendingAt < 15000) return;
    pendingLoad.remove();
  }

  vjFrameLoopStop?.();
  container.replaceChildren();
  const loading = document.createElement('div');
  loading.className = 'vj-deck-loading';
  loading.setAttribute('data-started', String(Date.now()));
  loading.style.cssText = 'padding:16px;font-size:11px;color:var(--crt-dim);';
  loading.textContent = 'Loading VJ deck…';
  container.appendChild(loading);
  window.setTimeout(() => {
    try {
      buildVjDeck();
    } catch (err) {
      console.error('[VJ] buildVjDeck failed:', err);
      container.replaceChildren();
      const errEl = document.createElement('div');
      errEl.style.cssText = 'padding:16px;font-size:11px;color:#c44;line-height:1.5;';
      const detail = err instanceof Error ? err.message : String(err);
      errEl.textContent =
        'VJ deck failed to load. Try a hard refresh (Ctrl+Shift+R). Console: [VJ] buildVjDeck failed — ' +
        detail.slice(0, 240);
      container.appendChild(errEl);
    }
  }, 0);
}

function buildVjDeck(): void {
  const container = document.getElementById('vjDeckContainer');
  if (!container || container.querySelector('.vj-deck-wrap')) return;

  container.innerHTML = '';

  if (isVjViewOnlyMode()) {
    const viewOnly = document.createElement('div');
    viewOnly.style.cssText =
      'padding:8px 10px;margin-bottom:8px;font-size:10px;background:var(--amiga-surface);color:var(--crt-dim);border:1px solid var(--bevel-dark);line-height:1.4;';
    viewOnly.textContent =
      'Audience stream link: watch the viz. Touch X/Y only works when the operator enables Audience participation. For full deck control, use the VJ collaboration link (top-bar VJ chip), not this URL.';
    container.appendChild(viewOnly);
  }

  loadVjLibraryFilter();
  function getShaderList(): IndexEntry[] {
    return applyVjLibraryFilter(entries);
  }
  const deckAEntry: { value: IndexEntry | null } = { value: null };
  const deckBEntry: { value: IndexEntry | null } = { value: null };
  let crossfader = 0;
  const manualCrossfadeAnim: CrossfadeAnim = {
    active: false,
    startVal: 0,
    target: 0,
    startTime: 0,
    durationMs: CROSSFADE_TAP_MS,
  };
  function updateCrossfaderUi(): void {
    crossfaderInput.value = String(crossfader);
    updateSliderFill(crossfaderInput);
    crossfaderVal.textContent = (crossfader * 100).toFixed(1) + '%';
    deckTagA.classList.toggle('is-active', crossfader < 0.08);
    deckTagB.classList.toggle('is-active', crossfader > 0.92);
  }
  function animateCrossfaderTo(target: number): void {
    if (Math.abs(crossfader - target) < 0.001) return;
    startCrossfadeAnim(manualCrossfadeAnim, crossfader, target, CROSSFADE_TAP_MS, performance.now());
  }
  let mixMode: MixMode = 'crossfade';
  let activeRandomMixInt = pickRandomMixModeInt();
  let randomMixTransitionArmed = true;
  let mixModeHint: HTMLSpanElement | null = null;

  function effectiveMixModeInt(): number {
    return resolveMixModeInt(mixMode, activeRandomMixInt);
  }

  function pickRandomMixForTransition(): void {
    activeRandomMixInt = pickRandomMixModeInt();
    updateRandomMixHint();
    randomMixTransitionArmed = false;
  }

  function updateRandomMixHint(): void {
    if (!mixModeHint) return;
    mixModeHint.textContent = mixMode === 'random' ? `(${mixModeLabelForInt(activeRandomMixInt)})` : '';
  }

  function trackRandomMixTransition(): void {
    if (mixMode !== 'random') return;
    const atRest = crossfader < 0.02 || crossfader > 0.98;
    if (atRest) randomMixTransitionArmed = true;
    else if (randomMixTransitionArmed) pickRandomMixForTransition();
  }
  let currentPageA = 0;
  let currentPageB = 0;
  let deckAGlobalIndex = -1;
  let deckBGlobalIndex = -1;
  let applyingRemoteControl = false;
  let outputFlipV = false;
  let outputFlipH = false;
  let outputRotation: 0 | 90 | 180 | 270 = 0;
  let autoVjEnabled = false;
  let autoVjBpm = 120;
  const autoVjTapTimes: number[] = [];
  let autoVjLastBeatTime = 0;
  let autoVjBeatCount = 0;
  let autoVjCrossfadeTarget = 1;
  let autoVjCrossfadeStart = 0;
  let autoVjCrossfadeStartVal = 0;
  let autoVjParamConfig: AutoVjParamConfig = loadAutoVjParamConfig();
  const autoVjParamState = createAutoVjParamState();
  let autoVjParamHint: HTMLSpanElement | null = null;
  const deckAProgRef: { current: WebGLProgram | null } = { current: null };
  const deckBProgRef: { current: WebGLProgram | null } = { current: null };
  let mixProgram: WebGLProgram | null = null;
  let glA: WebGLRenderingContext | null = null;
  let glB: WebGLRenderingContext | null = null;
  let glMix: WebGLRenderingContext | null = null;
  let rafMix = 0;
  let bgVjInterval = 0;
  const startTime = performance.now();

  let lastPreparedA = '';
  let lastPreparedB = '';
  let deckAMeta: ExposeItem[] = [];
  let deckBMeta: ExposeItem[] = [];
  let deckASamplerNames: string[] = [];
  let deckBSamplerNames: string[] = [];
  const deckAParamValues: Record<string, number | boolean> = {};
  const deckBParamValues: Record<string, number | boolean> = {};
  function loadFftMapFromStorage(key: string): Record<number, string> {
    try {
      const s = localStorage.getItem(key);
      if (!s) return {};
      const parsed = JSON.parse(s) as Record<string, unknown>;
      const out: Record<number, string> = {};
      for (const [k, v] of Object.entries(parsed)) {
        const n = Number(k);
        if (Number.isInteger(n) && n >= 0 && typeof v === 'string') out[n] = v;
      }
      return out;
    } catch (_) {
      return {};
    }
  }
  const deckAFftMap: Record<number, string> = loadFftMapFromStorage(VJ_FFT_A_KEY);
  const deckBFftMap: Record<number, string> = loadFftMapFromStorage(VJ_FFT_B_KEY);
  function saveVjFftMaps(): void {
    try {
      localStorage.setItem(VJ_FFT_A_KEY, JSON.stringify(deckAFftMap));
      localStorage.setItem(VJ_FFT_B_KEY, JSON.stringify(deckBFftMap));
    } catch (_) {}
  }

  function fftAssignableNames(meta: ExposeItem[]): string[] {
    return meta
      .filter((m) => m.type === 'float' || m.type === 'bool' || m.type === 'int')
      .map((m) => m.name);
  }

  function shouldAutoAssignDeckFft(): boolean {
    return isFftAutoSelectEnabled() || autoVjEnabled || audioEngine.active;
  }

  function maybeAutoAssignDeckFft(deck: 'A' | 'B', meta: ExposeItem[]): void {
    if (!shouldAutoAssignDeckFft()) return;
    const names = fftAssignableNames(meta);
    if (deck === 'A') autoAssignFftBandMap(deckAFftMap, names);
    else autoAssignFftBandMap(deckBFftMap, names);
    saveVjFftMaps();
  }

  const deckAOscAddresses: Record<string, string> = (() => {
    try {
      const s = localStorage.getItem(VJ_OSC_A_KEY);
      if (s) return JSON.parse(s) as Record<string, string>;
    } catch (_) {}
    return {};
  })();
  const deckBOscAddresses: Record<string, string> = (() => {
    try {
      const s = localStorage.getItem(VJ_OSC_B_KEY);
      if (s) return JSON.parse(s) as Record<string, string>;
    } catch (_) {}
    return {};
  })();
  function saveVjOscAddresses(): void {
    try {
      localStorage.setItem(VJ_OSC_A_KEY, JSON.stringify(deckAOscAddresses));
      localStorage.setItem(VJ_OSC_B_KEY, JSON.stringify(deckBOscAddresses));
    } catch (_) {}
  }
  function buildVjOscMaps(): void {
    const mapA: Record<string, string> = {};
    deckAMeta.forEach((p) => {
      const addr = deckAOscAddresses[p.name] || '/vj/a/' + p.name;
      mapA[addr] = p.name;
    });
    oscEngine.setVjDeckAAddressMap(mapA);
    const mapB: Record<string, string> = {};
    deckBMeta.forEach((p) => {
      const addr = deckBOscAddresses[p.name] || '/vj/b/' + p.name;
      mapB[addr] = p.name;
    });
    oscEngine.setVjDeckBAddressMap(mapB);
  }

  type VjTextureSource = 'none' | 'webcam' | 'smpte' | 'test1' | 'test2' | 'test3';
  let vjTextureSource: VjTextureSource = 'none';
  let vjWebcamVideo: HTMLVideoElement | null = null;
  let vjWebcamStream: MediaStream | null = null;
  const smpteCanvas = document.createElement('canvas');
  smpteCanvas.width = 256;
  smpteCanvas.height = 256;
  const testImages: HTMLImageElement[] = [];
  let deckAInputTex: WebGLTexture | null = null;
  let deckBInputTex: WebGLTexture | null = null;
  const vjDefaultTexByGl = new WeakMap<WebGLRenderingContext, WebGLTexture>();

  function drawSmptePattern(): void {
    const ctx = smpteCanvas.getContext('2d');
    if (!ctx) return;
    const w = smpteCanvas.width;
    const h = smpteCanvas.height;
    const barW = w / 7;
    const colors = ['#c0c0c0', '#c0c0b0', '#00a0b0', '#00b050', '#c03080', '#b02020', '#2020a0'];
    for (let i = 0; i < 7; i++) {
      ctx.fillStyle = colors[i];
      ctx.fillRect(i * barW, 0, barW + 1, h * 0.75);
    }
    const grad = ctx.createLinearGradient(0, h * 0.75, 0, h);
    grad.addColorStop(0, '#000');
    grad.addColorStop(1, '#fff');
    ctx.fillStyle = grad;
    ctx.fillRect(0, h * 0.75, w, h * 0.25);
  }
  drawSmptePattern();

  (function createTestImages(): void {
    const size = 128;
    const canv = document.createElement('canvas');
    canv.width = size;
    canv.height = size;
    const ctx = canv.getContext('2d');
    if (!ctx) return;
    for (let i = 0; i < 3; i++) {
      const img = new Image();
      testImages.push(img);
    }
    ctx.fillStyle = '#1a1a2e';
    ctx.fillRect(0, 0, size, size);
    const cell = 8;
    for (let y = 0; y < size; y += cell) {
      for (let x = 0; x < size; x += cell) {
        ctx.fillStyle = (x / cell + y / cell) % 2 === 0 ? '#16213e' : '#0f3460';
        ctx.fillRect(x, y, cell, cell);
      }
    }
    testImages[0].src = canv.toDataURL('image/png');
    const gr = ctx.createLinearGradient(0, 0, size, size);
    gr.addColorStop(0, '#e94560');
    gr.addColorStop(0.5, '#0f3460');
    gr.addColorStop(1, '#533483');
    ctx.fillStyle = gr;
    ctx.fillRect(0, 0, size, size);
    testImages[1].src = canv.toDataURL('image/png');
    ctx.fillStyle = '#0f0f23';
    ctx.fillRect(0, 0, size, size);
    ctx.fillStyle = '#00ff88';
    ctx.beginPath();
    ctx.arc(size / 2, size / 2, size / 3, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = '#ff0088';
    ctx.lineWidth = 4;
    ctx.stroke();
    testImages[2].src = canv.toDataURL('image/png');
  })();

  function getDefaultSamplerTexture(gl: WebGLRenderingContext): WebGLTexture {
    let tex = vjDefaultTexByGl.get(gl);
    if (tex) return tex;
    tex = gl.createTexture();
    if (!tex) throw new Error('createTexture failed');
    gl.bindTexture(gl.TEXTURE_2D, tex);
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0, gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([64, 64, 64, 255]));
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    vjDefaultTexByGl.set(gl, tex);
    return tex;
  }

  function getOrCreateVjInputTexture(gl: WebGLRenderingContext, deckId: 'A' | 'B'): WebGLTexture {
    const ref = deckId === 'A' ? deckAInputTex : deckBInputTex;
    if (ref) return ref;
    const tex = gl.createTexture();
    if (!tex) throw new Error('createTexture failed');
    gl.bindTexture(gl.TEXTURE_2D, tex);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    if (deckId === 'A') deckAInputTex = tex;
    else deckBInputTex = tex;
    return tex;
  }

  function uploadVjSourceToTexture(gl: WebGLRenderingContext, tex: WebGLTexture, deckId: 'A' | 'B'): void {
    gl.bindTexture(gl.TEXTURE_2D, tex);
    if (vjTextureSource === 'webcam' && vjWebcamVideo && vjWebcamVideo.readyState >= 2) {
      gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, vjWebcamVideo);
    } else if (vjTextureSource === 'smpte') {
      gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, smpteCanvas);
    } else if ((vjTextureSource === 'test1' || vjTextureSource === 'test2' || vjTextureSource === 'test3') && testImages.length >= 3) {
      const idx = vjTextureSource === 'test1' ? 0 : vjTextureSource === 'test2' ? 1 : 2;
      const img = testImages[idx];
      if (img.complete && img.naturalWidth) gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
    }
  }

  const wrap = document.createElement('div');
  wrap.className = 'vj-deck-wrap';
  wrap.dataset.layoutV = VJ_LAYOUT_VERSION;

  const decksBlock = document.createElement('div');
  decksBlock.className = 'vj-decks-block';

  const decksStage = document.createElement('div');
  decksStage.className = 'vj-decks-stage';

  const previewStage = document.createElement('div');
  previewStage.className = 'vj-preview-stage';

  const controlsScroll = document.createElement('div');
  controlsScroll.className = 'vj-controls-scroll';

  function buildDeckUI(
    label: string,
    onSelect: (path: string) => void,
    valuesRef: Record<string, number | boolean>,
    deckKey: 'A' | 'B',
    oscAddressesRef: Record<string, string>,
    onOscAddressChange: () => void
  ): { el: HTMLElement; canvas: HTMLCanvasElement; navLabel: HTMLElement; setDisplayPath: (path: string) => void; updateParams: (meta: ExposeItem[]) => void; navigateShader: (delta: number) => void; loadShader: (path: string) => void } {
    const deck = document.createElement('div');
    deck.className = 'vj-deck panel-section';
    deck.dataset.vjDeck = deckKey;
    deck.title = 'Drag a shader from the list and drop here';

    const head = document.createElement('div');
    head.className = 'panel-section-head';
    head.textContent = label;

    let currentPath = '';

    const input = document.createElement('input');
    input.type = 'text';
    input.placeholder = 'Filter shaders...';
    input.autocomplete = 'off';
    input.className = 'vj-shader-filter';

    const navRow = document.createElement('div');
    navRow.className = 'vj-shader-nav';
    const prevBtn = document.createElement('button');
    prevBtn.type = 'button';
    prevBtn.className = 'vj-shader-nav-btn';
    prevBtn.textContent = '\u2039 Prev';
    prevBtn.title = 'Previous shader in list';
    const navLabel = document.createElement('span');
    navLabel.className = 'vj-shader-nav-label';
    navLabel.textContent = '—';
    const nextBtn = document.createElement('button');
    nextBtn.type = 'button';
    nextBtn.className = 'vj-shader-nav-btn';
    nextBtn.textContent = 'Next \u203A';
    nextBtn.title = 'Next shader in list';
    navRow.appendChild(prevBtn);
    navRow.appendChild(navLabel);
    navRow.appendChild(nextBtn);

    const picker = document.createElement('div');
    picker.className = 'vj-shader-picker';
    picker.setAttribute('role', 'listbox');
    picker.setAttribute('aria-label', label + ' shader list');

    function getDisplayName(e: IndexEntry): string {
      return (e.fixedName ?? e.name ?? e.path ?? '').slice(0, 48);
    }

    function getDisplayNameWithIndex(e: IndexEntry): string {
      const idx = getShaderList().indexOf(e) + 1;
      const name = getDisplayName(e);
      return name ? idx + '. ' + name : String(idx);
    }

    function syncNavLabel(): void {
      if (getShaderList().length === 0) {
        navLabel.textContent = '0 / 0';
        return;
      }
      const idx = currentPath ? getShaderList().findIndex((e) => (e.path ?? '') === currentPath) : -1;
      if (idx >= 0) {
        const name = getDisplayName(getShaderList()[idx]);
        navLabel.textContent = `${idx + 1} / ${getShaderList().length}${name ? ' — ' + name : ''}`;
      } else {
        navLabel.textContent = `— / ${getShaderList().length}`;
      }
    }

    function pickShader(path: string): void {
      if (!path) return;
      currentPath = path;
      deck.dataset.vjShaderPath = path;
      syncNavLabel();
      renderPicker();
      onSelect(path);
    }

    function navigateShader(delta: number): void {
      if (getShaderList().length === 0) return;
      let idx = currentPath ? getShaderList().findIndex((e) => (e.path ?? '') === currentPath) : 0;
      if (idx < 0) idx = 0;
      idx = (idx + delta + getShaderList().length) % getShaderList().length;
      const entry = getShaderList()[idx];
      if (entry?.path) pickShader(entry.path);
    }

    prevBtn.addEventListener('click', () => navigateShader(-1));
    nextBtn.addEventListener('click', () => navigateShader(1));

    function renderPicker(): void {
      const query = input.value.trim();
      const filtered = query
        ? getShaderList().filter((e) => {
            if (fuzzyMatch(query, getDisplayName(e)) || fuzzyMatch(query, (e.path ?? ''))) return true;
            const tags = e.tags || [];
            if (tags.length && (fuzzyMatch(query, tags.join(' ')) || tags.some((t) => fuzzyMatch(query, t)))) return true;
            return false;
          })
        : getShaderList();
      picker.replaceChildren();
      if (filtered.length === 0) {
        const empty = document.createElement('div');
        empty.className = 'vj-shader-picker-empty';
        empty.textContent = 'No matches';
        picker.appendChild(empty);
        return;
      }
      for (const e of filtered) {
        const path = e.path ?? '';
        const row = document.createElement('button');
        row.type = 'button';
        row.className = 'vj-shader-picker-row' + (path && path === currentPath ? ' is-active' : '');
        row.textContent = getDisplayNameWithIndex(e);
        row.title = path;
        row.setAttribute('role', 'option');
        row.setAttribute('aria-selected', path === currentPath ? 'true' : 'false');
        row.addEventListener('click', () => {
          if (path) pickShader(path);
        });
        picker.appendChild(row);
      }
      if (currentPath) {
        const active = picker.querySelector('.vj-shader-picker-row.is-active');
        if (active) active.scrollIntoView({ block: 'nearest' });
      }
    }

    input.addEventListener('input', () => renderPicker());
    input.addEventListener('keydown', (ev) => {
      if (ev.key === 'ArrowDown') {
        ev.preventDefault();
        navigateShader(1);
      } else if (ev.key === 'ArrowUp') {
        ev.preventDefault();
        navigateShader(-1);
      }
    });

    function setDisplayPath(path: string): void {
      currentPath = path || '';
      if (path) deck.dataset.vjShaderPath = path;
      else delete deck.dataset.vjShaderPath;
      syncNavLabel();
      renderPicker();
    }

    const filterRow = document.createElement('div');
    filterRow.className = 'vj-shader-filter-row';
    filterRow.appendChild(input);

    deck.appendChild(head);
    deck.appendChild(filterRow);
    deck.appendChild(navRow);
    deck.appendChild(picker);
    renderPicker();

    const canvas = document.createElement('canvas');
    canvas.width = DECK_CANVAS_W;
    canvas.height = DECK_CANVAS_H;
    canvas.classList.add('vj-deck-canvas');
    attachDeckCanvasPointer(canvas, deckKey);

    const previewWrap = document.createElement('div');
    previewWrap.className = 'vj-deck-preview';
    previewWrap.appendChild(canvas);

    deck.appendChild(previewWrap);

    deck.addEventListener('dragover', (ev: DragEvent) => {
      if (ev.dataTransfer?.types.includes('text/plain') || ev.dataTransfer?.types.includes('application/x-macroverse-shader-path')) {
        ev.preventDefault();
        ev.dataTransfer.dropEffect = 'copy';
      }
    });
    deck.addEventListener('drop', (ev: DragEvent) => {
      ev.preventDefault();
      const path = ev.dataTransfer?.getData('text/plain') || ev.dataTransfer?.getData('application/x-macroverse-shader-path');
      if (path && typeof path === 'string' && path.trim()) onSelect(path.trim());
    });

    registerShaderDropTarget({
      el: deck,
      onDrop: (path) => onSelect(path.trim()),
    });

    const paramsContainer = document.createElement('div');
    paramsContainer.className = 'vj-deck-params';
    deck.appendChild(paramsContainer);

    const prefix = '/vj/' + deckKey.toLowerCase() + '/';
    function updateParams(meta: ExposeItem[]): void {
      paramsContainer.innerHTML = '';
      for (const p of meta) {
        const row = document.createElement('div');
        row.className = 'vj-deck-param-row';
        const lab = document.createElement('span');
        lab.textContent = p.name;
        lab.title = p.name;
        row.appendChild(lab);
        if (p.type === 'bool') {
          const cb = document.createElement('input');
          cb.type = 'checkbox';
          cb.checked = !!(valuesRef[p.name] as boolean);
          cb.className = 'vj-deck-param-check';
          cb.addEventListener('change', () => {
            valuesRef[p.name] = cb.checked;
          });
          row.appendChild(cb);
        } else {
          const sl = document.createElement('input');
          sl.type = 'range';
          sl.min = String(p.min);
          sl.max = String(p.max);
          sl.step = String(Number.isInteger(p.min) && Number.isInteger(p.max) ? 1 : (p.max - p.min) / 100);
          const v = Number(valuesRef[p.name] ?? p.def);
          sl.value = String(Math.max(p.min, Math.min(p.max, v)));
          lab.style.color = sliderColorForKey(deckKey + '-' + p.name);
          row.appendChild(sl);
          enhanceRangeInput(sl, {
            colorKey: deckKey + '-' + p.name,
            className: 'mv-rich-slider--vj-fader',
          });
          sl.addEventListener('input', () => {
            valuesRef[p.name] = parseFloat(sl.value);
          });
        }
        const oscInp = document.createElement('input');
        oscInp.type = 'text';
        oscInp.value = oscAddressesRef[p.name] || prefix + p.name;
        oscInp.title = 'OSC address (0-1); clear for default';
        oscInp.className = 'vj-osc-field';
        oscInp.addEventListener('change', () => {
          const v = oscInp.value.trim();
          if (v && v !== prefix + p.name) {
            oscAddressesRef[p.name] = v;
          } else {
            delete oscAddressesRef[p.name];
            oscInp.value = prefix + p.name;
          }
          onOscAddressChange();
        });
        row.appendChild(oscInp);
        paramsContainer.appendChild(row);
      }
    }

    return { el: deck, canvas, navLabel, setDisplayPath, updateParams, navigateShader, loadShader: pickShader };
  }

  let shaderCarousel: ReturnType<typeof buildVjShaderCarousel>;

  const deckA = buildDeckUI('Deck A', (path) => {
    const idx = getShaderList().findIndex((e) => (e.path ?? '') === path);
    if (idx >= 0) deckAGlobalIndex = idx;
    deckAEntry.value = getShaderList().find((e) => (e.path ?? '') === path) ?? null;
    deckA.setDisplayPath(path);
    shaderCarousel.updateActive();
    shaderCarousel.scrollToPath(path);
    if (!glA) return;
    loadShaderForDeck(path, deckAProgRef, glA, deckAEntry, deckAParamValues, (meta) => {
      deckAMeta = meta;
      deckA.updateParams(meta);
      maybeAutoAssignDeckFft('A', meta);
      rebuildFftUI();
      buildVjOscMaps();
    }, 'A');
  }, deckAParamValues, 'A', deckAOscAddresses, () => { saveVjOscAddresses(); buildVjOscMaps(); });
  const deckB = buildDeckUI('Deck B', (path) => {
    const idx = getShaderList().findIndex((e) => (e.path ?? '') === path);
    if (idx >= 0) deckBGlobalIndex = idx;
    deckBEntry.value = getShaderList().find((e) => (e.path ?? '') === path) ?? null;
    deckB.setDisplayPath(path);
    shaderCarousel.updateActive();
    shaderCarousel.scrollToPath(path);
    if (!glB) return;
    loadShaderForDeck(path, deckBProgRef, glB, deckBEntry, deckBParamValues, (meta) => {
      deckBMeta = meta;
      deckB.updateParams(meta);
      maybeAutoAssignDeckFft('B', meta);
      rebuildFftUI();
      buildVjOscMaps();
    }, 'B');
  }, deckBParamValues, 'B', deckBOscAddresses, () => { saveVjOscAddresses(); buildVjOscMaps(); });

  function buildMobileDeckQuickNavRow(
    deckKey: 'A' | 'B',
    deck: { el: HTMLElement; navLabel: HTMLElement; navigateShader: (delta: number) => void }
  ): HTMLElement {
    const row = document.createElement('div');
    row.className = 'vj-mobile-deck-nav-row';
    row.dataset.vjDeck = deckKey;

    const tag = document.createElement('span');
    tag.className = 'vj-mobile-deck-nav-tag';
    tag.textContent = deckKey;
    row.appendChild(tag);

    const prev = document.createElement('button');
    prev.type = 'button';
    prev.className = 'vj-mobile-deck-nav-btn';
    prev.textContent = '\u2039 Prev';
    prev.title = 'Previous shader on Deck ' + deckKey;
    prev.setAttribute('aria-label', 'Previous shader on Deck ' + deckKey);
    prev.addEventListener('click', () => deck.navigateShader(-1));
    row.appendChild(prev);

    const label = document.createElement('span');
    label.className = 'vj-mobile-deck-nav-label';
    const syncLabel = () => {
      label.textContent = deck.navLabel.textContent || '\u2014';
      label.title = label.textContent || '';
    };
    syncLabel();
    new MutationObserver(syncLabel).observe(deck.navLabel, { childList: true, characterData: true, subtree: true });
    row.appendChild(label);

    const next = document.createElement('button');
    next.type = 'button';
    next.className = 'vj-mobile-deck-nav-btn';
    next.textContent = 'Next \u203A';
    next.title = 'Next shader on Deck ' + deckKey;
    next.setAttribute('aria-label', 'Next shader on Deck ' + deckKey);
    next.addEventListener('click', () => deck.navigateShader(1));
    row.appendChild(next);

    const params = document.createElement('button');
    params.type = 'button';
    params.className = 'vj-mobile-deck-nav-btn vj-mobile-deck-param-jump';
    params.textContent = 'Params';
    params.title = 'Jump to Deck ' + deckKey + ' parameters';
    params.setAttribute('aria-label', 'Jump to Deck ' + deckKey + ' parameters');
    params.addEventListener('click', () => {
      const scroller = document.getElementById('vjDeckContainer') as HTMLElement | null;
      const stickyHeight = previewStage.getBoundingClientRect().height;
      const behavior: ScrollBehavior = window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth';
      if (scroller) {
        const scrollerRect = scroller.getBoundingClientRect();
        const top = deck.el.getBoundingClientRect().top - scrollerRect.top + scroller.scrollTop - stickyHeight - 8;
        scroller.scrollTo({ top: Math.max(0, top), behavior });
      } else {
        const top = deck.el.getBoundingClientRect().top + window.scrollY - stickyHeight - 8;
        window.scrollTo({ top: Math.max(0, top), behavior });
      }
    });
    row.appendChild(params);

    return row;
  }

  const mobileDeckQuickNav = document.createElement('div');
  mobileDeckQuickNav.className = 'vj-mobile-deck-nav';
  mobileDeckQuickNav.setAttribute('aria-label', 'Mobile deck shader navigation');
  mobileDeckQuickNav.appendChild(buildMobileDeckQuickNavRow('A', deckA));
  mobileDeckQuickNav.appendChild(buildMobileDeckQuickNavRow('B', deckB));

  const mobileCarouselHost = document.createElement('div');
  mobileCarouselHost.className = 'vj-mobile-carousel-host';

  const desktopCarouselHost = document.createElement('div');
  desktopCarouselHost.className = 'vj-desktop-carousel-host';

  function placeShaderCarouselForLayout(): void {
    const host = document.documentElement.classList.contains('layout-phone')
      ? mobileCarouselHost
      : desktopCarouselHost;
    if (shaderCarousel.root.parentElement !== host) {
      host.appendChild(shaderCarousel.root);
      requestAnimationFrame(() => {
        shaderCarousel.refresh();
        shaderCarousel.updateActive();
      });
    }
  }

  function resizeDeckCanvas(canvas: HTMLCanvasElement): void {
    const wrap = canvas.parentElement;
    if (!wrap) return;
    const rect = wrap.getBoundingClientRect();
    if (rect.width < 8 || rect.height < 8) return;
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    const w = Math.max(1, Math.round(rect.width * dpr));
    const h = Math.max(1, Math.round(rect.height * dpr));
    if (canvas.width === w && canvas.height === h) return;
    canvas.width = w;
    canvas.height = h;
    canvas.style.width = '100%';
    canvas.style.height = '100%';
    canvas.style.objectFit = 'fill';
  }

  function resizeDeckPreviews(): void {
    resizeDeckCanvas(deckA.canvas);
    resizeDeckCanvas(deckB.canvas);
  }

  const deckPreviewResizeObserver = new ResizeObserver(() => resizeDeckPreviews());
  const deckAPreviewWrap = deckA.canvas.parentElement;
  const deckBPreviewWrap = deckB.canvas.parentElement;
  if (deckAPreviewWrap) deckPreviewResizeObserver.observe(deckAPreviewWrap);
  if (deckBPreviewWrap) deckPreviewResizeObserver.observe(deckBPreviewWrap);
  window.addEventListener('resize', resizeDeckPreviews);

  shaderCarousel = buildVjShaderCarousel({
    getShaderList,
    onLoadDeck: (deck, path) => {
      if (vjActiveLoadDeckByPath) vjActiveLoadDeckByPath(deck, path);
      else if (deck === 'A') deckA.loadShader(path);
      else deckB.loadShader(path);
    },
    getActivePaths: () => ({
      a: deckA.el.dataset.vjShaderPath || '',
      b: deckB.el.dataset.vjShaderPath || '',
    }),
  });

  const vjFilterStrip = buildVjLibraryFilterStrip(() => {
    shaderCarousel.refresh();
    vjFilterStrip.refresh();
  });

  onVjShaderIndexUpdated = () => {
    shaderCarousel.refresh();
    vjFilterStrip.refresh();
    const pathA = deckA.el.dataset.vjShaderPath || '';
    const pathB = deckB.el.dataset.vjShaderPath || '';
    deckA.setDisplayPath(pathA);
    deckB.setDisplayPath(pathB);
  };
  if (entries.length > 0) {
    shaderCarousel.refresh();
    vjFilterStrip.refresh();
  }

  window.addEventListener('thumbnail-captured', (ev: Event) => {
    const path = (ev as CustomEvent<{ path?: string }>).detail?.path;
    if (path) shaderCarousel.refreshThumb(path);
  });

  const mixerStrip = document.createElement('div');
  mixerStrip.className = 'vj-mixer-strip';

  const crossfaderRow = document.createElement('div');
  crossfaderRow.className = 'vj-crossfader-row vj-crossfader-row--main';
  const deckTagA = document.createElement('button');
  deckTagA.type = 'button';
  deckTagA.className = 'vj-mixer-deck-tag vj-mixer-deck-tag--a';
  deckTagA.textContent = 'A';
  deckTagA.title = 'Crossfade to Deck A';
  const deckTagB = document.createElement('button');
  deckTagB.type = 'button';
  deckTagB.className = 'vj-mixer-deck-tag vj-mixer-deck-tag--b';
  deckTagB.textContent = 'B';
  deckTagB.title = 'Crossfade to Deck B';
  const xfadeSlider = createRichSlider({
    label: 'Crossfade',
    min: 0,
    max: 1,
    step: 0.005,
    value: 0,
    colorKey: 'vj-crossfader',
    className: 'mv-rich-slider--crossfader mv-rich-slider--crossfader-main mv-rich-slider--inline',
    inputClassName: 'vj-crossfader',
    hideLabel: true,
    formatValue: (v) => (v * 100).toFixed(1) + '% A to B'
  });
  const crossfaderInput = xfadeSlider.input;
  const crossfaderVal = xfadeSlider.valueEl!;
  crossfaderRow.appendChild(deckTagA);
  crossfaderRow.appendChild(xfadeSlider.root);
  crossfaderRow.appendChild(deckTagB);

  deckTagA.addEventListener('click', () => animateCrossfaderTo(0));
  deckTagB.addEventListener('click', () => animateCrossfaderTo(1));

  const mixModeRow = document.createElement('div');
  mixModeRow.className = 'vj-control-row vj-mixer-meta-item';
  const mixModeLabel = document.createElement('label');
  mixModeLabel.textContent = 'Mix';
  const mixModeSelect = document.createElement('select');
  for (const m of MIX_MODES) {
    const opt = document.createElement('option');
    opt.value = m.value;
    opt.textContent = m.label;
    mixModeSelect.appendChild(opt);
  }
  mixModeRow.appendChild(mixModeLabel);
  mixModeRow.appendChild(mixModeSelect);
  mixModeHint = document.createElement('span');
  mixModeHint.className = 'vj-mix-random-hint';
  mixModeHint.style.cssText = 'font-size: 10px; color: var(--crt-dim); white-space: nowrap;';
  mixModeRow.appendChild(mixModeHint);
  updateRandomMixHint();

  const textureRow = document.createElement('div');
  textureRow.className = 'vj-control-row vj-mixer-meta-item';
  const textureLabel = document.createElement('label');
  textureLabel.textContent = 'Texture';
  const textureStatus = document.createElement('span');
  textureStatus.style.cssText = 'font-size: 11px; color: var(--crt-dim);';
  textureStatus.textContent = 'none';
  const textureBtns: { value: VjTextureSource; el: HTMLButtonElement }[] = [];
  function setVjTextureSource(s: VjTextureSource): void {
    if (s === vjTextureSource && s !== 'webcam') return;
    if (vjTextureSource === 'webcam' && vjWebcamStream) {
      vjWebcamStream.getTracks().forEach((t) => t.stop());
      vjWebcamStream = null;
      vjWebcamVideo = null;
    }
    vjTextureSource = s;
    textureStatus.textContent = s;
    textureBtns.forEach((b) => { b.el.classList.toggle('is-active', b.value === s); });
    if (s === 'webcam') {
      navigator.mediaDevices.getUserMedia({ video: true, audio: false })
        .then((stream) => {
          const video = document.createElement('video');
          video.srcObject = stream;
          video.setAttribute('playsinline', '');
          video.play().catch(() => {});
          vjWebcamVideo = video;
          vjWebcamStream = stream;
          textureStatus.textContent = 'webcam';
        })
        .catch(() => {
          vjTextureSource = 'smpte';
          textureStatus.textContent = 'smpte (no webcam)';
          textureBtns.forEach((b) => { b.el.classList.toggle('is-active', b.value === 'smpte'); });
        });
    }
  }
  textureRow.appendChild(textureLabel);
  for (const val of ['none', 'webcam', 'smpte', 'test1', 'test2', 'test3'] as VjTextureSource[]) {
    const btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'vj-btn vj-btn--chip' + (val === vjTextureSource ? ' is-active' : '');
    btn.textContent = val === 'none' ? 'None' : val === 'test1' ? 'Test 1' : val === 'test2' ? 'Test 2' : val === 'test3' ? 'Test 3' : val;
    btn.addEventListener('click', () => setVjTextureSource(val));
    textureBtns.push({ value: val, el: btn });
    textureRow.appendChild(btn);
  }
  textureRow.appendChild(textureStatus);

  const mixerMetaRow = document.createElement('div');
  mixerMetaRow.className = 'vj-mixer-meta-row';
  mixerMetaRow.appendChild(mixModeRow);
  mixerMetaRow.appendChild(textureRow);

  mixerStrip.appendChild(crossfaderRow);

  const autoVjSection = document.createElement('div');
  autoVjSection.className = 'panel-section vj-autovj-section';
  const autoVjHead = document.createElement('div');
  autoVjHead.className = 'panel-section-head';
  autoVjHead.textContent = 'Auto VJ';
  autoVjSection.appendChild(autoVjHead);
  const autoVjRow = document.createElement('div');
  autoVjRow.className = 'vj-control-row';
  const autoVjToggleBtn = document.createElement('button');
  autoVjToggleBtn.type = 'button';
  autoVjToggleBtn.className = 'vj-btn vj-btn--toggle';
  autoVjToggleBtn.textContent = 'Off';
  autoVjToggleBtn.title = 'Auto-pilot BPM clock: crossfade, optional shader swap and param depth. Use Shaders/Depth toggles below.';
  const autoVjBpmLabel = document.createElement('label');
  autoVjBpmLabel.textContent = 'BPM';
  const autoVjBpmInput = document.createElement('input');
  autoVjBpmInput.type = 'number';
  autoVjBpmInput.min = '30';
  autoVjBpmInput.max = '240';
  autoVjBpmInput.value = '120';
  autoVjBpmInput.className = 'vj-shader-filter vj-field-narrow';
  const autoVjTapBtn = document.createElement('button');
  autoVjTapBtn.type = 'button';
  autoVjTapBtn.className = 'vj-btn';
  autoVjTapBtn.textContent = 'Tap';
  autoVjTapBtn.title = 'Tap to set BPM from tempo';
  autoVjRow.appendChild(autoVjToggleBtn);
  autoVjRow.appendChild(autoVjBpmLabel);
  autoVjRow.appendChild(autoVjBpmInput);
  autoVjRow.appendChild(autoVjTapBtn);
  autoVjSection.appendChild(autoVjRow);

  const autoVjParamRow = document.createElement('div');
  autoVjParamRow.className = 'vj-control-row vj-autovj-param-row';
  const autoVjParamLabel = document.createElement('label');
  autoVjParamLabel.textContent = 'Params';
  autoVjParamLabel.title = 'Auto-move deck parameters on BPM clock';
  const autoVjParamSelect = document.createElement('select');
  autoVjParamSelect.className = 'vj-field-select';
  for (const m of AUTO_VJ_PARAM_MODES) {
    const opt = document.createElement('option');
    opt.value = m.value;
    opt.textContent = m.label;
    if (m.value === autoVjParamConfig.paramMode) opt.selected = true;
    autoVjParamSelect.appendChild(opt);
  }
  const autoVjCycleLabel = document.createElement('label');
  autoVjCycleLabel.textContent = 'Cycle';
  autoVjCycleLabel.title = 'Beats per parameter LFO cycle';
  const autoVjCycleSelect = document.createElement('select');
  autoVjCycleSelect.className = 'vj-field-select vj-field-narrow';
  for (const beats of AUTO_VJ_BEAT_CYCLES) {
    const opt = document.createElement('option');
    opt.value = String(beats);
    opt.textContent = beats === 1 ? '1 beat' : `${beats} beats`;
    if (beats === autoVjParamConfig.beatsPerCycle) opt.selected = true;
    autoVjCycleSelect.appendChild(opt);
  }
  const autoVjBarLabel = document.createElement('label');
  autoVjBarLabel.textContent = 'Bar';
  autoVjBarLabel.title = 'Beats before shader swap and random picks';
  const autoVjBarSelect = document.createElement('select');
  autoVjBarSelect.className = 'vj-field-select vj-field-narrow';
  for (const beats of AUTO_VJ_BAR_BEATS) {
    const opt = document.createElement('option');
    opt.value = String(beats);
    opt.textContent = `${beats}b`;
    if (beats === autoVjParamConfig.barBeats) opt.selected = true;
    autoVjBarSelect.appendChild(opt);
  }
  const autoVjDepthLabel = document.createElement('label');
  autoVjDepthLabel.textContent = 'Depth';
  const autoVjDepthInput = document.createElement('input');
  autoVjDepthInput.type = 'range';
  autoVjDepthInput.min = '10';
  autoVjDepthInput.max = '100';
  autoVjDepthInput.step = '5';
  autoVjDepthInput.value = String(Math.round(autoVjParamConfig.depth * 100));
  autoVjDepthInput.className = 'vj-autovj-depth';
  autoVjDepthInput.title = 'How far params move across their range';
  autoVjParamHint = document.createElement('span');
  autoVjParamHint.className = 'vj-autovj-param-hint';
  autoVjParamHint.style.cssText = 'font-size: 10px; color: var(--crt-dim); white-space: nowrap;';
  autoVjParamRow.appendChild(autoVjParamLabel);
  autoVjParamRow.appendChild(autoVjParamSelect);
  autoVjParamRow.appendChild(autoVjCycleLabel);
  autoVjParamRow.appendChild(autoVjCycleSelect);
  autoVjParamRow.appendChild(autoVjBarLabel);
  autoVjParamRow.appendChild(autoVjBarSelect);
  autoVjParamRow.appendChild(autoVjDepthLabel);
  autoVjParamRow.appendChild(autoVjDepthInput);
  autoVjParamRow.appendChild(autoVjParamHint);
  autoVjSection.appendChild(autoVjParamRow);

  const autoVjFeatureRow = document.createElement('div');
  autoVjFeatureRow.className = 'vj-control-row vj-autovj-feature-row';
  const autoVjShaderSwapLabel = document.createElement('label');
  autoVjShaderSwapLabel.textContent = 'Shaders';
  autoVjShaderSwapLabel.title = 'Pick new random shaders each bar';
  const autoVjShaderSwapBtn = document.createElement('button');
  autoVjShaderSwapBtn.type = 'button';
  autoVjShaderSwapBtn.className = 'vj-btn vj-btn--toggle';
  autoVjShaderSwapBtn.textContent = autoVjParamConfig.shaderSwap ? 'On' : 'Off';
  autoVjShaderSwapBtn.classList.toggle('is-on', autoVjParamConfig.shaderSwap);
  autoVjShaderSwapBtn.title = 'Auto-change deck shaders on bar';
  const autoVjParamMoveLabel = document.createElement('label');
  autoVjParamMoveLabel.textContent = 'Depth';
  autoVjParamMoveLabel.title = 'Automate parameter movement (depth LFO)';
  const autoVjParamMoveBtn = document.createElement('button');
  autoVjParamMoveBtn.type = 'button';
  autoVjParamMoveBtn.className = 'vj-btn vj-btn--toggle';
  autoVjParamMoveBtn.textContent = autoVjParamConfig.paramMove ? 'On' : 'Off';
  autoVjParamMoveBtn.classList.toggle('is-on', autoVjParamConfig.paramMove);
  autoVjParamMoveBtn.title = 'Auto-move deck params using depth slider';
  autoVjFeatureRow.appendChild(autoVjShaderSwapLabel);
  autoVjFeatureRow.appendChild(autoVjShaderSwapBtn);
  autoVjFeatureRow.appendChild(autoVjParamMoveLabel);
  autoVjFeatureRow.appendChild(autoVjParamMoveBtn);
  autoVjSection.appendChild(autoVjFeatureRow);

  function syncAutoVjFeatureUi(): void {
    autoVjShaderSwapBtn.textContent = autoVjParamConfig.shaderSwap ? 'On' : 'Off';
    autoVjShaderSwapBtn.classList.toggle('is-on', autoVjParamConfig.shaderSwap);
    autoVjParamMoveBtn.textContent = autoVjParamConfig.paramMove ? 'On' : 'Off';
    autoVjParamMoveBtn.classList.toggle('is-on', autoVjParamConfig.paramMove);
    autoVjDepthInput.disabled = !autoVjParamConfig.paramMove;
    autoVjParamSelect.disabled = !autoVjParamConfig.paramMove;
    autoVjCycleSelect.disabled = !autoVjParamConfig.paramMove;
    autoVjBarSelect.disabled = !autoVjParamConfig.shaderSwap;
  }
  syncAutoVjFeatureUi();

  autoVjShaderSwapBtn.addEventListener('click', () => {
    autoVjParamConfig.shaderSwap = !autoVjParamConfig.shaderSwap;
    saveAutoVjStorage(AUTO_VJ_STORE.shaderSwap, autoVjParamConfig.shaderSwap ? '1' : '0');
    syncAutoVjFeatureUi();
  });
  autoVjParamMoveBtn.addEventListener('click', () => {
    autoVjParamConfig.paramMove = !autoVjParamConfig.paramMove;
    saveAutoVjStorage(AUTO_VJ_STORE.paramMove, autoVjParamConfig.paramMove ? '1' : '0');
    syncAutoVjFeatureUi();
  });

  function updateAutoVjParamHint(): void {
    if (!autoVjParamHint) return;
    if (autoVjParamConfig.paramMode === 'random') {
      autoVjParamHint.textContent = `(${autoVjParamModeLabel(autoVjParamState.activeRandomMode)})`;
    } else {
      autoVjParamHint.textContent = '';
    }
  }

  autoVjParamState.activeRandomMode = pickRandomAutoVjParamMode();
  updateAutoVjParamHint();

  autoVjParamSelect.addEventListener('change', () => {
    autoVjParamConfig.paramMode = autoVjParamSelect.value as AutoVjParamConfig['paramMode'];
    saveAutoVjStorage(AUTO_VJ_STORE.paramMode, autoVjParamConfig.paramMode);
    if (autoVjParamConfig.paramMode === 'random') {
      autoVjParamState.activeRandomMode = pickRandomAutoVjParamMode();
    }
    updateAutoVjParamHint();
  });
  autoVjCycleSelect.addEventListener('change', () => {
    autoVjParamConfig.beatsPerCycle = parseInt(autoVjCycleSelect.value, 10) || 4;
    saveAutoVjStorage(AUTO_VJ_STORE.beatsPerCycle, String(autoVjParamConfig.beatsPerCycle));
  });
  autoVjBarSelect.addEventListener('change', () => {
    autoVjParamConfig.barBeats = parseInt(autoVjBarSelect.value, 10) || 4;
    saveAutoVjStorage(AUTO_VJ_STORE.barBeats, String(autoVjParamConfig.barBeats));
  });
  autoVjDepthInput.addEventListener('input', () => {
    autoVjParamConfig.depth = Math.max(0.1, Math.min(1, parseInt(autoVjDepthInput.value, 10) / 100));
    saveAutoVjStorage(AUTO_VJ_STORE.depth, String(autoVjParamConfig.depth));
  });

  // --- MIDI section ---
  const midiSection = document.createElement('div');
  midiSection.className = 'panel-section vj-midi-section';
  const midiHead = document.createElement('div');
  midiHead.className = 'panel-section-head';
  midiHead.style.cssText = 'display: flex; align-items: center; gap: 8px;';
  midiHead.textContent = 'MIDI';
  const midiStatusDot = document.createElement('span');
  midiStatusDot.style.cssText = 'width: 6px; height: 6px; border-radius: 50%; background: #aa3333; display: inline-block;';
  midiHead.appendChild(midiStatusDot);
  midiSection.appendChild(midiHead);

  const midiControlRow = document.createElement('div');
  midiControlRow.className = 'vj-control-row';
  const midiEnableBtn = document.createElement('button');
  midiEnableBtn.type = 'button';
  midiEnableBtn.className = 'vj-btn vj-btn--toggle';
  midiEnableBtn.textContent = 'Enable';
  const midiTemplateSelect = document.createElement('select');
  midiTemplateSelect.className = 'vj-field-select';
  midiTemplateSelect.innerHTML = '<option value="apc40_mk2">APC40 / Akai</option><option value="custom">Custom</option><option value="none">None</option>';
  midiTemplateSelect.value = midiEngine.vjTemplate;
  const midiDeviceSelect = document.createElement('select');
  midiDeviceSelect.className = 'vj-field-select vj-field-select--device';
  midiDeviceSelect.innerHTML = '<option value="">All devices</option>';
  const midiLastLabel = document.createElement('span');
  midiLastLabel.id = 'midiLastReceivedVJ';
  midiLastLabel.style.cssText = 'font-size: 8px; color: var(--crt-dim); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; max-width: 140px;';
  midiControlRow.appendChild(midiEnableBtn);
  midiControlRow.appendChild(midiTemplateSelect);
  midiControlRow.appendChild(midiDeviceSelect);
  midiControlRow.appendChild(midiLastLabel);
  midiSection.appendChild(midiControlRow);

  const midiTemplateHint = document.createElement('div');
  midiTemplateHint.style.cssText = 'font-size: 8px; color: var(--crt-dim); margin: 2px 0 6px; line-height: 1.35;';
  midiTemplateHint.textContent = 'AKAI/APC: grid loads A, Shift+grid loads B, scenes launch rows, track knobs=A, device knobs=B, faders follow Shift.';
  midiSection.appendChild(midiTemplateHint);

  const midiMappingsWrap = document.createElement('div');
  midiMappingsWrap.style.cssText = 'display: flex; gap: 8px; flex-wrap: wrap; max-height: 120px; overflow-y: auto;';

  function buildMidiMappingsUI(): void {
    midiMappingsWrap.innerHTML = '';
    const sections: { label: string; actions: { id: string; name: string }[] }[] = [
      {
        label: 'Crossfader',
        actions: [{ id: 'vj/crossfader', name: 'Xfade' }]
      },
      {
        label: 'Auto',
        actions: [{ id: 'vj/autoVj', name: 'Auto VJ' }]
      },
      {
        label: 'Deck A / Track',
        actions: Array.from({ length: 8 }, (_, i) => ({
          id: 'vj/deckA/param/' + i,
          name: 'A' + i + (deckAMeta[i] ? ' ' + deckAMeta[i].name.slice(0, 6) : '')
        }))
      },
      {
        label: 'Deck B / Device',
        actions: Array.from({ length: 8 }, (_, i) => ({
          id: 'vj/deckB/param/' + i,
          name: 'B' + i + (deckBMeta[i] ? ' ' + deckBMeta[i].name.slice(0, 6) : '')
        }))
      },
      {
        label: 'Pages',
        actions: [
          { id: 'vj/deckA/pageUp', name: 'A PgUp' },
          { id: 'vj/deckA/pageDown', name: 'A PgDn' },
          { id: 'vj/deckB/pageLeft', name: 'B PgLt' },
          { id: 'vj/deckB/pageRight', name: 'B PgRt' }
        ]
      }
    ];
    for (const sec of sections) {
      const col = document.createElement('div');
      col.style.cssText = 'display: flex; flex-direction: column; gap: 1px; min-width: 80px;';
      const hdr = document.createElement('div');
      hdr.style.cssText = 'font-size: 8px; color: var(--crt-dim); margin-bottom: 1px;';
      hdr.textContent = sec.label;
      col.appendChild(hdr);
      for (const act of sec.actions) {
        const row = document.createElement('div');
        row.style.cssText = 'display: flex; align-items: center; gap: 2px; font-size: 8px;';
        const nameEl = document.createElement('span');
        nameEl.style.cssText = 'min-width: 40px; color: var(--crt-fg); overflow: hidden; text-overflow: ellipsis; white-space: nowrap;';
        nameEl.textContent = act.name;
        nameEl.title = act.id;
        row.appendChild(nameEl);
        const mapping = midiEngine.getVjActionMapping(act.id);
        const ccLabel = document.createElement('span');
        ccLabel.style.cssText = 'min-width: 28px; color: var(--amiga-accent); font-size: 8px;';
        ccLabel.textContent = mapping ? 'CC' + mapping.cc : '--';
        row.appendChild(ccLabel);
        const learnBtn = document.createElement('button');
        learnBtn.type = 'button';
        learnBtn.textContent = 'Learn';
        learnBtn.style.cssText = 'font-size: 7px; padding: 0px 4px; background: var(--amiga-bg); color: var(--amiga-copper); border: 1px solid var(--bevel-dark); cursor: pointer;';
        learnBtn.onclick = () => {
          learnBtn.textContent = '...';
          learnBtn.style.color = '#ff8833';
          midiEngine.learnVjAction(act.id, () => {
            buildMidiMappingsUI();
          });
        };
        row.appendChild(learnBtn);
        col.appendChild(row);
      }
      midiMappingsWrap.appendChild(col);
    }
  }
  midiSection.appendChild(midiMappingsWrap);

  function refreshMidiDevices(): void {
    const prevVal = midiDeviceSelect.value;
    midiDeviceSelect.innerHTML = '<option value="">All devices</option>';
    for (const inp of midiEngine.inputs) {
      const opt = document.createElement('option');
      opt.value = inp.id;
      opt.textContent = inp.name.slice(0, 24);
      midiDeviceSelect.appendChild(opt);
    }
    if (prevVal) midiDeviceSelect.value = prevVal;
  }

  function updateMidiStatus(): void {
    midiStatusDot.style.background = midiEngine.active ? '#22cc44' : '#aa3333';
    midiEnableBtn.textContent = midiEngine.active ? 'Enabled' : 'Enable';
    midiEnableBtn.classList.toggle('is-on', midiEngine.active);
  }

  midiEnableBtn.onclick = async () => {
    if (midiEngine.active) return;
    try {
      await midiEngine.start();
      refreshMidiDevices();
      buildMidiMappingsUI();
      updateMidiStatus();
    } catch (e) {
      midiLastLabel.textContent = 'MIDI: ' + (e instanceof Error ? e.message : String(e));
    }
  };
  midiTemplateSelect.onchange = () => {
    midiEngine.setVjTemplate(midiTemplateSelect.value as import('../engines/midi.js').VJMidiTemplateId);
    buildMidiMappingsUI();
  };
  midiDeviceSelect.onchange = () => {
    midiEngine.selectedInputId = midiDeviceSelect.value || null;
  };
  if (midiEngine.active) {
    refreshMidiDevices();
    updateMidiStatus();
  }
  buildMidiMappingsUI();

  // --- OSC section ---
  const oscSection = document.createElement('div');
  oscSection.className = 'panel-section vj-osc-section';
  const oscHead = document.createElement('div');
  oscHead.className = 'panel-section-head';
  oscHead.style.cssText = 'display: flex; align-items: center; gap: 8px;';
  oscHead.textContent = 'OSC';
  const oscStatusDot = document.createElement('span');
  oscStatusDot.style.cssText = 'width: 6px; height: 6px; border-radius: 50%; background: #aa3333; display: inline-block;';
  oscHead.appendChild(oscStatusDot);
  oscSection.appendChild(oscHead);

  const oscControlRow = document.createElement('div');
  oscControlRow.className = 'vj-control-row';
  const oscToggleBtn = document.createElement('button');
  oscToggleBtn.type = 'button';
  oscToggleBtn.className = 'vj-btn vj-btn--toggle';
  oscToggleBtn.textContent = oscEngine.active ? 'Stop' : 'Listen';
  const oscPortInput = document.createElement('input');
  oscPortInput.type = 'number';
  oscPortInput.value = String(oscEngine.port || 9000);
  oscPortInput.className = 'vj-field-select vj-field-narrow';
  oscPortInput.title = 'UDP port';
  const oscLastLabel = document.createElement('span');
  oscLastLabel.id = 'oscLastReceivedVJ';
  oscLastLabel.style.cssText = 'font-size: 8px; color: var(--crt-dim); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; max-width: 140px;';
  const oscInfo = document.createElement('span');
  oscInfo.style.cssText = 'font-size: 8px; color: var(--crt-dim);';
  oscInfo.textContent = 'Per-param OSC: /vj/a/name, /vj/b/name (see sliders)';
  oscControlRow.appendChild(oscToggleBtn);
  oscControlRow.appendChild(oscPortInput);
  oscControlRow.appendChild(oscLastLabel);
  oscSection.appendChild(oscControlRow);
  oscSection.appendChild(oscInfo);

  function updateOscStatus(): void {
    oscStatusDot.style.background = oscEngine.active ? '#22cc44' : '#aa3333';
    oscToggleBtn.textContent = oscEngine.active ? 'Stop' : 'Listen';
    oscToggleBtn.classList.toggle('is-on', oscEngine.active);
  }
  oscToggleBtn.onclick = async () => {
    if (oscEngine.active) {
      await oscEngine.stop();
    } else {
      try {
        await oscEngine.start(parseInt(oscPortInput.value, 10) || 9000);
        buildVjOscMaps();
      } catch (e) {
        oscLastLabel.textContent = 'OSC: ' + (e instanceof Error ? e.message : String(e));
      }
    }
    updateOscStatus();
  };
  updateOscStatus();

  // --- Audio FFT section ---
  const audioSection = document.createElement('div');
  audioSection.className = 'panel-section vj-audio-section';
  const audioHead = document.createElement('div');
  audioHead.className = 'panel-section-head';
  audioHead.style.cssText = 'display: flex; align-items: center; gap: 8px;';
  audioHead.textContent = 'Audio FFT';
  const audioStatusDot = document.createElement('span');
  audioStatusDot.style.cssText = 'width: 6px; height: 6px; border-radius: 50%; background: #aa3333; display: inline-block;';
  audioHead.appendChild(audioStatusDot);
  audioSection.appendChild(audioHead);

  const audioControlRow = document.createElement('div');
  audioControlRow.className = 'vj-control-row';
  const audioToggleBtn = document.createElement('button');
  audioToggleBtn.type = 'button';
  audioToggleBtn.className = 'vj-btn vj-btn--toggle';
  audioToggleBtn.textContent = audioEngine.active ? 'Stop' : 'Start';
  const vjGainSlider = createRichSlider({
    label: 'Gain',
    min: 0,
    max: 3,
    step: 0.1,
    value: audioEngine.gain,
    colorKey: 'vj-audio-gain',
    compact: true,
    formatValue: (v) => v.toFixed(1)
  });
  const audioGainSlider = vjGainSlider.input;
  const audioGainVal = vjGainSlider.valueEl!;
  audioControlRow.appendChild(audioToggleBtn);
  audioControlRow.appendChild(vjGainSlider.root);
  audioSection.appendChild(audioControlRow);

  const audioDeviceLabel = document.createElement('div');
  audioDeviceLabel.style.cssText = 'font-size: 8px; color: var(--crt-dim); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; max-width: 100%;';
  audioDeviceLabel.textContent = audioEngine.active ? audioEngine.deviceLabel : 'No device';
  audioSection.appendChild(audioDeviceLabel);

  const fftCanvas = document.createElement('canvas');
  fftCanvas.width = 320;
  fftCanvas.height = 48;
  fftCanvas.style.cssText = 'width: 100%; max-width: 320px; height: 48px; display: block; background: var(--amiga-bg); border: 1px solid var(--bevel-dark);';
  fftCanvas.title = 'FFT bands (mapped = brighter)';
  audioSection.appendChild(fftCanvas);

  function updateAudioStatus(): void {
    audioStatusDot.style.background = audioEngine.active ? '#22cc44' : '#aa3333';
    audioToggleBtn.textContent = audioEngine.active ? 'Stop' : 'Start';
    audioToggleBtn.classList.toggle('is-on', audioEngine.active);
    audioDeviceLabel.textContent = audioEngine.active ? (audioEngine.deviceLabel || 'Default input') : 'No device';
  }
  const audioErrorSpan = document.createElement('span');
  audioErrorSpan.style.cssText = 'font-size: 9px; color: #cc4444; max-width: 160px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;';
  audioControlRow.appendChild(audioErrorSpan);
  audioToggleBtn.onclick = async () => {
    audioErrorSpan.textContent = '';
    if (audioEngine.active) {
      audioEngine.stop();
    } else {
      try {
        await audioEngine.start();
        maybeAutoAssignDeckFft('A', deckAMeta);
        maybeAutoAssignDeckFft('B', deckBMeta);
        rebuildFftUI();
      } catch (e) {
        audioErrorSpan.textContent = (e instanceof Error ? e.message : String(e)).slice(0, 40);
        const { status } = await import('../dom.js');
        status('Audio FFT: ' + (e instanceof Error ? e.message : String(e)), true);
      }
    }
    updateAudioStatus();
  };
  updateAudioStatus();
  audioGainSlider.oninput = () => {
    const v = parseFloat(audioGainSlider.value);
    audioEngine.setGain(v);
    audioGainVal.textContent = v.toFixed(1);
  };
  updateAudioStatus();

  const fftSection = document.createElement('div');
  fftSection.style.cssText = 'display: flex; flex-direction: column; gap: 4px;';
  const fftHeadRow = document.createElement('div');
  fftHeadRow.style.cssText = 'display: flex; align-items: center; justify-content: space-between; gap: 8px; flex-wrap: wrap; margin-bottom: 2px;';
  const fftHead = document.createElement('div');
  fftHead.style.cssText = 'font-size: 9px; color: var(--crt-dim);';
  fftHead.textContent = 'Band Mapping (both decks)';
  const fftAutoSelectLabel = document.createElement('label');
  fftAutoSelectLabel.style.cssText = 'display: flex; align-items: center; gap: 4px; font-size: 9px; color: var(--crt-dim); cursor: pointer; user-select: none;';
  const fftAutoSelectCheck = document.createElement('input');
  fftAutoSelectCheck.type = 'checkbox';
  fftAutoSelectCheck.checked = isFftAutoSelectEnabled();
  fftAutoSelectCheck.title = 'When on, new shaders auto-map FFT bands to params (sources and effects)';
  fftAutoSelectCheck.addEventListener('change', () => {
    setFftAutoSelectEnabled(fftAutoSelectCheck.checked);
    if (fftAutoSelectCheck.checked) {
      maybeAutoAssignDeckFft('A', deckAMeta);
      maybeAutoAssignDeckFft('B', deckBMeta);
      rebuildFftUI();
    }
  });
  fftAutoSelectLabel.appendChild(fftAutoSelectCheck);
  fftAutoSelectLabel.appendChild(document.createTextNode('Auto-select'));
  fftHeadRow.appendChild(fftHead);
  fftHeadRow.appendChild(fftAutoSelectLabel);
  fftSection.appendChild(fftHeadRow);
  const fftCols = document.createElement('div');
  fftCols.style.cssText = 'display: flex; gap: 12px; flex-wrap: wrap;';
  const fftColA = document.createElement('div');
  fftColA.style.cssText = 'flex: 1; min-width: 140px;';
  const fftColB = document.createElement('div');
  fftColB.style.cssText = 'flex: 1; min-width: 140px;';
  fftColA.style.maxHeight = '100px';
  fftColA.style.overflowY = 'auto';
  fftColB.style.maxHeight = '100px';
  fftColB.style.overflowY = 'auto';
  fftCols.appendChild(fftColA);
  fftCols.appendChild(fftColB);
  fftSection.appendChild(fftCols);

  function rebuildFftUI(): void {
    fftColA.innerHTML = '';
    fftColB.innerHTML = '';
    const labA = document.createElement('div');
    labA.style.cssText = 'font-size: 9px; color: var(--crt-dim); margin-bottom: 2px;';
    labA.textContent = 'Deck A';
    fftColA.appendChild(labA);
    const autoA = document.createElement('button');
    autoA.type = 'button';
    autoA.className = 'vj-btn vj-btn--chip';
    autoA.textContent = 'Auto';
    autoA.title = 'Assign bands 0-' + (FFT_BAND_COUNT - 1) + ' to first params in order';
    autoA.onclick = () => {
      if (deckAMeta.length === 0) {
        import('../dom.js').then(({ status }) => status('Load a shader in Deck A first, then click Auto to assign FFT bands.', true));
        return;
      }
      autoAssignFftBandMap(deckAFftMap, fftAssignableNames(deckAMeta));
      saveVjFftMaps();
      rebuildFftUI();
    };
    fftColA.appendChild(autoA);
    for (let i = 0; i < FFT_BAND_COUNT; i++) {
      const row = document.createElement('div');
      row.style.cssText = 'display: flex; align-items: center; gap: 4px; margin: 2px 0; font-size: 9px;';
      const lbl = document.createElement('span');
      lbl.style.cssText = 'width: 32px; flex-shrink: 0; color: var(--crt-dim);';
      lbl.textContent = FFT_BAND_LABELS[i].slice(0, 4);
      const sel = document.createElement('select');
      sel.className = 'vj-field-select vj-field-select--grow';
      sel.innerHTML = '<option value="">--</option>' + deckAMeta.map((p) => '<option value="' + p.name + '"' + (deckAFftMap[i] === p.name ? ' selected' : '') + '>' + p.name.slice(0, 12) + '</option>').join('');
      sel.addEventListener('change', () => {
        if (sel.value) deckAFftMap[i] = sel.value;
        else delete deckAFftMap[i];
        saveVjFftMaps();
      });
      row.appendChild(lbl);
      row.appendChild(sel);
      fftColA.appendChild(row);
    }
    const labB = document.createElement('div');
    labB.style.cssText = 'font-size: 9px; color: var(--crt-dim); margin-bottom: 2px;';
    labB.textContent = 'Deck B';
    fftColB.appendChild(labB);
    const autoB = document.createElement('button');
    autoB.type = 'button';
    autoB.className = 'vj-btn vj-btn--chip';
    autoB.textContent = 'Auto';
    autoB.title = 'Assign bands 0-' + (FFT_BAND_COUNT - 1) + ' to first params in order';
    autoB.onclick = () => {
      if (deckBMeta.length === 0) {
        import('../dom.js').then(({ status }) => status('Load a shader in Deck B first, then click Auto to assign FFT bands.', true));
        return;
      }
      autoAssignFftBandMap(deckBFftMap, fftAssignableNames(deckBMeta));
      saveVjFftMaps();
      rebuildFftUI();
    };
    fftColB.appendChild(autoB);
    for (let i = 0; i < FFT_BAND_COUNT; i++) {
      const row = document.createElement('div');
      row.style.cssText = 'display: flex; align-items: center; gap: 4px; margin: 2px 0; font-size: 9px;';
      const lbl = document.createElement('span');
      lbl.style.cssText = 'width: 32px; flex-shrink: 0; color: var(--crt-dim);';
      lbl.textContent = FFT_BAND_LABELS[i].slice(0, 4);
      const sel = document.createElement('select');
      sel.className = 'vj-field-select vj-field-select--grow';
      sel.innerHTML = '<option value="">--</option>' + deckBMeta.map((p) => '<option value="' + p.name + '"' + (deckBFftMap[i] === p.name ? ' selected' : '') + '>' + p.name.slice(0, 12) + '</option>').join('');
      sel.addEventListener('change', () => {
        if (sel.value) deckBFftMap[i] = sel.value;
        else delete deckBFftMap[i];
        saveVjFftMaps();
      });
      row.appendChild(lbl);
      row.appendChild(sel);
      fftColB.appendChild(row);
    }
  }
  rebuildFftUI();

  const outputSection = document.createElement('div');
  outputSection.className = 'vj-output-section';
  const outputHead = document.createElement('div');
  outputHead.className = 'vj-control-row';
  outputHead.style.justifyContent = 'space-between';
  const outputLabel = document.createElement('div');
  outputLabel.style.cssText = 'font-size: 11px; text-transform: uppercase; color: var(--amiga-copper); letter-spacing: 0.08em;';
  outputLabel.textContent = 'Master Preview';
  const outputActions = document.createElement('div');
  outputActions.className = 'vj-control-row';
  outputActions.style.flexWrap = 'nowrap';
  const popOutBtn = document.createElement('button');
  popOutBtn.type = 'button';
  popOutBtn.className = 'vj-btn vj-btn--action';
  popOutBtn.textContent = 'Pop out';
  popOutBtn.title = 'Open output on another screen; reacts to MIDI/OSC/FFT';
  const audienceQrBtn = document.createElement('button');
  audienceQrBtn.type = 'button';
  audienceQrBtn.className = 'vj-btn vj-btn--action';
  audienceQrBtn.textContent = 'Audience QR';
  audienceQrBtn.title = 'Show or hide join QR on the VJ output (preview, pop-out, HDMI)';
  const syncAudienceQrBtn = () => {
    if (isGigOutputQrVisible()) {
      audienceQrBtn.textContent = 'Hide QR on output';
    } else {
      audienceQrBtn.textContent = 'Audience QR';
    }
  };
  audienceQrBtn.addEventListener('click', () => {
    if (isGigOutputQrVisible()) setGigOutputQrVisible(false);
    else setGigOutputQrVisible(true);
    syncAudienceQrBtn();
  });
  window.addEventListener('macroverse-gig-output-qr-visible', () => syncAudienceQrBtn());
  window.addEventListener('macroverse-gig-audience-qr-visible', () => syncAudienceQrBtn());
  outputHead.appendChild(outputLabel);
  outputActions.appendChild(audienceQrBtn);
  outputActions.appendChild(popOutBtn);
  outputHead.appendChild(outputActions);

  const transformSection = document.createElement('div');
  transformSection.className = 'vj-output-transform-row';
  transformSection.style.cssText = 'display: flex; flex-wrap: wrap; align-items: center; gap: 8px 12px; font-size: 9px;';
  const flipVLabel = document.createElement('label');
  flipVLabel.style.cssText = 'display: flex; align-items: center; gap: 4px; color: var(--crt-dim); cursor: pointer;';
  const flipVCheck = document.createElement('input');
  flipVCheck.type = 'checkbox';
  flipVCheck.checked = outputFlipV;
  flipVCheck.addEventListener('change', () => { outputFlipV = flipVCheck.checked; });
  flipVLabel.appendChild(flipVCheck);
  flipVLabel.appendChild(document.createTextNode('Flip V'));
  const flipHLabel = document.createElement('label');
  flipHLabel.style.cssText = 'display: flex; align-items: center; gap: 4px; color: var(--crt-dim); cursor: pointer;';
  const flipHCheck = document.createElement('input');
  flipHCheck.type = 'checkbox';
  flipHCheck.checked = outputFlipH;
  flipHCheck.addEventListener('change', () => { outputFlipH = flipHCheck.checked; });
  flipHLabel.appendChild(flipHCheck);
  flipHLabel.appendChild(document.createTextNode('Flip H'));
  const rotLabel = document.createElement('label');
  rotLabel.style.cssText = 'display: flex; align-items: center; gap: 4px; color: var(--crt-dim);';
  rotLabel.textContent = 'Rotate';
  const rotSelect = document.createElement('select');
  rotSelect.className = 'vj-field-select';
  for (const deg of [0, 90, 180, 270] as const) {
    const opt = document.createElement('option');
    opt.value = String(deg);
    opt.textContent = deg + ' deg';
    if (deg === outputRotation) opt.selected = true;
    rotSelect.appendChild(opt);
  }
  rotSelect.addEventListener('change', () => { outputRotation = Number(rotSelect.value) as 0 | 90 | 180 | 270; });
  rotLabel.appendChild(rotSelect);
  transformSection.appendChild(flipVLabel);
  transformSection.appendChild(flipHLabel);
  transformSection.appendChild(rotLabel);

  const previewAndQrRow = document.createElement('div');
  previewAndQrRow.className = 'vj-preview-and-qr-row';

  const previewCol = document.createElement('div');
  previewCol.className = 'vj-preview-col';

  const previewAspect = document.createElement('div');
  previewAspect.className = 'vj-preview-aspect';

  const outputCanvas = document.createElement('canvas');
  outputCanvas.className = 'vj-output-canvas';
  outputCanvas.width = OUTPUT_CANVAS_FALLBACK_W;
  outputCanvas.height = OUTPUT_CANVAS_FALLBACK_H;

  const outputQrOverlay = document.createElement('canvas');
  outputQrOverlay.className = 'vj-output-qr-overlay';
  outputQrOverlay.width = OUTPUT_CANVAS_FALLBACK_W;
  outputQrOverlay.height = OUTPUT_CANVAS_FALLBACK_H;

  previewAspect.appendChild(outputCanvas);
  previewAspect.appendChild(outputQrOverlay);
  previewCol.appendChild(previewAspect);
  registerGigOutputQrCanvas(outputQrOverlay, { surface: 'output' });

  let draggingOutputQr = false;
  const syncOutputQrOverlayPointer = () => {
    const on = isGigOutputQrVisible();
    outputQrOverlay.style.pointerEvents = on ? 'auto' : 'none';
    outputQrOverlay.style.cursor = on ? (draggingOutputQr ? 'grabbing' : 'grab') : 'default';
  };
  const moveOutputQrFromPointer = (clientX: number, clientY: number) => {
    setGigOutputQrLayout(gigOutputQrLayoutFromPointer(clientX, clientY, outputQrOverlay));
  };
  outputQrOverlay.addEventListener('pointerdown', (e) => {
    if (!isGigOutputQrVisible()) return;
    draggingOutputQr = true;
    syncOutputQrOverlayPointer();
    outputQrOverlay.setPointerCapture(e.pointerId);
    moveOutputQrFromPointer(e.clientX, e.clientY);
    e.preventDefault();
  });
  outputQrOverlay.addEventListener('pointermove', (e) => {
    if (!draggingOutputQr) return;
    moveOutputQrFromPointer(e.clientX, e.clientY);
    e.preventDefault();
  });
  const endOutputQrDrag = () => {
    draggingOutputQr = false;
    syncOutputQrOverlayPointer();
  };
  outputQrOverlay.addEventListener('pointerup', endOutputQrDrag);
  outputQrOverlay.addEventListener('pointercancel', endOutputQrDrag);
  outputQrOverlay.addEventListener(
    'wheel',
    (e) => {
      if (!isGigOutputQrVisible()) return;
      e.preventDefault();
      const L = getGigOutputQrLayout();
      setGigOutputQrLayout({ scale: L.scale + (e.deltaY > 0 ? -0.02 : 0.02) });
    },
    { passive: false }
  );
  window.addEventListener('macroverse-gig-output-qr-visible', syncOutputQrOverlayPointer);
  window.addEventListener('macroverse-gig-output-qr-layout', syncOutputQrOverlayPointer);
  syncOutputQrOverlayPointer();

  const gigQrBlock = createVjPreviewGigQrBlock();
  const qrWidth = readVjLayoutNumber(VJ_QR_WIDTH_KEY, VJ_QR_WIDTH_DEFAULT);
  gigQrBlock.root.style.flex = `0 0 ${qrWidth}px`;
  const previewQrResizer = document.createElement('div');
  previewQrResizer.className = 'vj-col-resizer split-resizer';
  previewQrResizer.setAttribute('aria-label', 'Resize master preview vs stream QR');
  previewAndQrRow.appendChild(previewCol);
  previewAndQrRow.appendChild(previewQrResizer);
  previewAndQrRow.appendChild(gigQrBlock.root);
  if (!gigQrBlock.isPanelVisible()) {
    previewQrResizer.style.display = 'none';
  }
  outputSection.appendChild(previewAndQrRow);

  vjOutputCanvasRef = outputCanvas;
  outputCanvas.classList.add('vj-deck-canvas', 'vj-output-canvas--interactive');
  const setMasterMouse = (clientX: number, clientY: number) => {
    const rect = outputCanvas.getBoundingClientRect();
    if (rect.width < 1 || rect.height < 1) return;
    _vjMouseXRef = Math.max(0, Math.min(1, (clientX - rect.left) / rect.width));
    _vjMouseYRef = Math.max(0, Math.min(1, 1 - (clientY - rect.top) / rect.height));
  };
  outputCanvas.addEventListener('pointerdown', (e) => {
    setMasterMouse(e.clientX, e.clientY);
    outputCanvas.setPointerCapture(e.pointerId);
  });
  outputCanvas.addEventListener('pointermove', (e) => {
    if (outputCanvas.hasPointerCapture(e.pointerId) || e.buttons !== 0) {
      setMasterMouse(e.clientX, e.clientY);
    }
  });
  outputCanvas.addEventListener('pointerup', (e) => {
    try { outputCanvas.releasePointerCapture(e.pointerId); } catch (_) {}
  });
  outputCanvas.addEventListener('mouseleave', () => {
    _vjMouseXRef = 0.5;
    _vjMouseYRef = 0.5;
  });

  const outputUrlRow = document.createElement('div');
  outputUrlRow.className = 'vj-output-url-row';
  outputUrlRow.style.cssText = 'display: flex; align-items: center; gap: 6px; flex-wrap: wrap; font-size: 9px;';
  const outputUrlLabel = document.createElement('span');
  outputUrlLabel.style.cssText = 'color: var(--crt-dim); white-space: nowrap;';
  outputUrlLabel.textContent = 'URL (pop-out / OBS Browser Source):';
  const outputUrlInput = document.createElement('input');
  outputUrlInput.type = 'text';
  outputUrlInput.readOnly = true;
  const syncOutputUrlInput = () => {
    outputUrlInput.value = typeof window !== 'undefined' ? buildGigOutputUrl(getVjSessionId()) : '';
  };
  syncOutputUrlInput();
  outputUrlInput.title = 'Pi HDMI / OBS: open this URL on the projector machine for this gig session';
  outputUrlInput.style.cssText = 'flex: 1; min-width: 120px; font-size: 9px; padding: 2px 6px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); font-family: inherit;';
  const outputUrlCopy = document.createElement('button');
  outputUrlCopy.type = 'button';
  outputUrlCopy.className = 'vj-btn vj-btn--action';
  outputUrlCopy.textContent = 'Copy';
  outputUrlRow.appendChild(outputUrlLabel);
  outputUrlRow.appendChild(outputUrlInput);
  outputUrlRow.appendChild(outputUrlCopy);
  outputUrlCopy.addEventListener('click', () => {
    outputUrlInput.select();
    navigator.clipboard.writeText(outputUrlInput.value).then(() => {
      const t = outputUrlCopy.textContent;
      outputUrlCopy.textContent = 'Copied';
      setTimeout(() => { outputUrlCopy.textContent = t; }, 1500);
    }).catch(() => {});
  });

  window.addEventListener('macroverse-vj-session-changed', () => {
    void ensureVjTokens(getVjSessionId()).then(() => {
      syncOutputUrlInput();
      gigQrBlock.refresh();
      refreshGigOutputQrSession();
    });
  });

  let popOutWin: Window | null = null;
  popOutBtn.addEventListener('click', () => {
    if (popOutWin && !popOutWin.closed) {
      popOutWin.focus();
      return;
    }
    popOutWin = window.open(`/vj-output.html?remote=1&${vjViewQuery()}`, 'macroverse-vj-output');
    if (!popOutWin) {
      console.warn('[VJ] Pop-out blocked by browser. Allow popups for this site.');
      return;
    }
    popOutWin.addEventListener('beforeunload', () => { popOutWin = null; });
  });

  audioSection.appendChild(fftSection);

  const flexDecks = readVjLayoutNumber(VJ_FLEX_DECKS_KEY, VJ_FLEX_DECKS_DEFAULT);
  const flexPreview = readVjLayoutNumber(VJ_FLEX_PREVIEW_KEY, VJ_FLEX_PREVIEW_DEFAULT);
  const flexControls = readVjLayoutNumber(VJ_FLEX_CONTROLS_KEY, VJ_FLEX_CONTROLS_DEFAULT);

  decksBlock.style.flex = String(flexDecks);
  previewStage.style.flex = String(flexPreview);
  controlsScroll.style.flex = String(flexControls);

  decksStage.appendChild(deckA.el);
  decksStage.appendChild(deckB.el);
  decksBlock.appendChild(decksStage);

  const libraryStack = document.createElement('div');
  libraryStack.className = 'vj-library-stack';
  libraryStack.appendChild(vjFilterStrip.root);
  libraryStack.appendChild(desktopCarouselHost);

  previewStage.appendChild(mixerStrip);
  previewStage.appendChild(outputSection);
  previewStage.appendChild(mobileDeckQuickNav);
  previewStage.appendChild(mobileCarouselHost);
  placeShaderCarouselForLayout();
  controlsScroll.appendChild(mixerMetaRow);
  controlsScroll.appendChild(outputHead);
  controlsScroll.appendChild(transformSection);
  controlsScroll.appendChild(outputUrlRow);
  controlsScroll.appendChild(midiSection);
  controlsScroll.appendChild(oscSection);
  controlsScroll.appendChild(audioSection);

  const resizerDecksPreview = document.createElement('div');
  resizerDecksPreview.className = 'vj-row-resizer split-resizer';
  resizerDecksPreview.setAttribute('aria-label', 'Resize deck row vs master preview');

  const resizerPreviewControls = document.createElement('div');
  resizerPreviewControls.className = 'vj-row-resizer split-resizer';
  resizerPreviewControls.setAttribute('aria-label', 'Resize master preview vs controls');

  wrap.appendChild(autoVjSection);
  wrap.appendChild(libraryStack);
  wrap.appendChild(decksBlock);
  wrap.appendChild(resizerDecksPreview);
  wrap.appendChild(previewStage);
  wrap.appendChild(resizerPreviewControls);
  wrap.appendChild(controlsScroll);
  container.appendChild(wrap);

  deckPreviewResizeObserver.observe(decksStage);
  requestAnimationFrame(() => {
    resizeDeckPreviews();
    requestAnimationFrame(resizeDeckPreviews);
  });

  bindVjRowResizer(resizerDecksPreview, decksBlock, previewStage, 1.2, 1, () => {
    saveVjLayoutNumber(VJ_FLEX_DECKS_KEY, parseFloat(decksBlock.style.flex) || flexDecks);
    saveVjLayoutNumber(VJ_FLEX_PREVIEW_KEY, parseFloat(previewStage.style.flex) || flexPreview);
    vjPreviewResizeFn?.();
    resizeDeckPreviews();
  });
  resizerDecksPreview.addEventListener('dblclick', () => {
    decksBlock.style.flex = String(VJ_FLEX_DECKS_DEFAULT);
    previewStage.style.flex = String(VJ_FLEX_PREVIEW_DEFAULT);
    saveVjLayoutNumber(VJ_FLEX_DECKS_KEY, VJ_FLEX_DECKS_DEFAULT);
    saveVjLayoutNumber(VJ_FLEX_PREVIEW_KEY, VJ_FLEX_PREVIEW_DEFAULT);
    vjPreviewResizeFn?.();
  });
  bindVjRowResizer(resizerPreviewControls, previewStage, controlsScroll, 1, 0.8, () => {
    saveVjLayoutNumber(VJ_FLEX_PREVIEW_KEY, parseFloat(previewStage.style.flex) || flexPreview);
    saveVjLayoutNumber(VJ_FLEX_CONTROLS_KEY, parseFloat(controlsScroll.style.flex) || flexControls);
    vjPreviewResizeFn?.();
  });
  resizerPreviewControls.addEventListener('dblclick', () => {
    previewStage.style.flex = String(VJ_FLEX_PREVIEW_DEFAULT);
    controlsScroll.style.flex = String(VJ_FLEX_CONTROLS_DEFAULT);
    saveVjLayoutNumber(VJ_FLEX_PREVIEW_KEY, VJ_FLEX_PREVIEW_DEFAULT);
    saveVjLayoutNumber(VJ_FLEX_CONTROLS_KEY, VJ_FLEX_CONTROLS_DEFAULT);
    vjPreviewResizeFn?.();
  });
  bindVjPreviewQrResizer(previewQrResizer, previewCol, gigQrBlock.root, (w) => {
    saveVjLayoutNumber(VJ_QR_WIDTH_KEY, w);
    gigQrBlock.refresh();
  });
  window.addEventListener('macroverse-vj-preview-qr-visible', ((e: CustomEvent<{ visible: boolean }>) => {
    const visible = e.detail?.visible !== false;
    previewQrResizer.style.display = visible ? '' : 'none';
    if (visible) {
      const w = readVjLayoutNumber(VJ_QR_WIDTH_KEY, VJ_QR_WIDTH_DEFAULT);
      gigQrBlock.root.style.flex = `0 0 ${w}px`;
      gigQrBlock.refresh();
    }
  }) as EventListener);

  function resizeOutputPreview(): void {
    const rect = previewAspect.getBoundingClientRect();
    if (rect.width < 8 || rect.height < 8) return;
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    const w = Math.max(1, Math.round(rect.width * dpr));
    const h = Math.max(1, Math.round(rect.height * dpr));
    if (outputCanvas.width === w && outputCanvas.height === h) return;
    outputCanvas.width = w;
    outputCanvas.height = h;
    outputQrOverlay.width = w;
    outputQrOverlay.height = h;
  }
  vjPreviewResizeFn = resizeOutputPreview;
  const previewResizeObserver = new ResizeObserver(() => resizeOutputPreview());
  previewResizeObserver.observe(previewAspect);
  previewResizeObserver.observe(previewCol);
  previewResizeObserver.observe(previewStage);
  window.addEventListener('resize', resizeOutputPreview);
  window.addEventListener('resize', () => requestAnimationFrame(placeShaderCarouselForLayout));
  requestAnimationFrame(resizeOutputPreview);

  vjDeckInitialized = true;

  const glOpts: WebGLContextAttributes = {
    preserveDrawingBuffer: true,
    premultipliedAlpha: false,
    powerPreference: 'default'
  };

  glA = deckA.canvas.getContext('webgl', glOpts);
  glB = deckB.canvas.getContext('webgl', glOpts);
  glMix = outputCanvas.getContext('webgl', glOpts);

  if (glA) glA.getExtension('OES_standard_derivatives');
  if (glB) glB.getExtension('OES_standard_derivatives');
  if (glMix) glMix.getExtension('OES_standard_derivatives');

  if (!glA || !glB || !glMix) {
    const errEl = document.createElement('div');
    errEl.style.cssText = `color: #aa4444; padding: 12px;`;
    errEl.textContent = 'WebGL not available';
    container.appendChild(errEl);
    return;
  }

  const bufA = glA.createBuffer();
  glA.bindBuffer(glA.ARRAY_BUFFER, bufA);
  glA.bufferData(glA.ARRAY_BUFFER, QUAD_VERTS, glA.STATIC_DRAW);

  const bufB = glB.createBuffer();
  glB.bindBuffer(glB.ARRAY_BUFFER, bufB);
  glB.bufferData(glB.ARRAY_BUFFER, QUAD_VERTS, glB.STATIC_DRAW);

  const texA = createTextureFromCanvas(glMix);
  const texB = createTextureFromCanvas(glMix);
  const bufMix = glMix.createBuffer();
  glMix.bindBuffer(glMix.ARRAY_BUFFER, bufMix);
  glMix.bufferData(glMix.ARRAY_BUFFER, QUAD_VERTS, glMix.STATIC_DRAW);

  mixProgram = createMixProgram(glMix);

  function runDeck(
    gl: WebGLRenderingContext,
    canvas: HTMLCanvasElement,
    progRef: { current: WebGLProgram | null },
    buf: WebGLBuffer | null,
    overrides: Record<string, number | boolean>,
    meta: ExposeItem[],
    samplerNames: string[],
    deckId: 'A' | 'B'
  ): void {
    const w = canvas.width;
    const h = canvas.height;
    const t = (performance.now() - startTime) / 1000;

    gl.viewport(0, 0, w, h);
    gl.clearColor(0.05, 0.05, 0.08, 1);
    gl.clear(gl.COLOR_BUFFER_BIT);

    if (!progRef.current) return;

    gl.useProgram(progRef.current);
    const metaForUniforms = meta.map((p) => ({ id: p.name, type: p.type }));
    setDeckUniforms(gl, progRef.current, w, h, t, overrides, metaForUniforms, deckId);

    const { mx, my } = getDeckMouse(deckId);
    const names = ['time', 'resolution', 'mouse', 'iGlobalTime', 'iTime', 'iResolution', 'FRAMEINDEX', 'timeScale', 'mouseX', 'mouseY'];
    for (const name of names) {
      const loc = gl.getUniformLocation(progRef.current, name);
      if (!loc) continue;
      if (name === 'time' || name === 'iGlobalTime' || name === 'iTime') gl.uniform1f(loc, t);
      else if (name === 'resolution' || name === 'iResolution') gl.uniform2f(loc, w, h);
      else if (name === 'mouse') gl.uniform2f(loc, mx, my);
      else if (name === 'FRAMEINDEX') gl.uniform1f(loc, Math.floor(t * 60));
      else if (name === 'timeScale') gl.uniform1f(loc, 1.0);
      else if (name === 'mouseX') gl.uniform1f(loc, mx);
      else if (name === 'mouseY') gl.uniform1f(loc, my);
    }

    if (samplerNames.length > 0) {
      if (vjTextureSource !== 'none') {
        const tex = getOrCreateVjInputTexture(gl, deckId);
        uploadVjSourceToTexture(gl, tex, deckId);
        for (let i = 0; i < samplerNames.length; i++) {
          gl.activeTexture(gl.TEXTURE0 + i);
          gl.bindTexture(gl.TEXTURE_2D, tex);
          const uloc = gl.getUniformLocation(progRef.current, samplerNames[i]);
          if (uloc) gl.uniform1i(uloc, i);
        }
      } else {
        const defaultTex = getDefaultSamplerTexture(gl);
        for (let i = 0; i < samplerNames.length; i++) {
          gl.activeTexture(gl.TEXTURE0 + i);
          gl.bindTexture(gl.TEXTURE_2D, defaultTex);
          const uloc = gl.getUniformLocation(progRef.current, samplerNames[i]);
          if (uloc) gl.uniform1i(uloc, i);
        }
      }
    }

    gl.bindBuffer(gl.ARRAY_BUFFER, buf);
    const loc = gl.getAttribLocation(progRef.current, 'a_pos');
    gl.enableVertexAttribArray(loc);
    gl.vertexAttribPointer(loc, 2, gl.FLOAT, false, 0, 0);
    gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
  }

  function runDeckALogic(): void {
    if (audioEngine.active) {
      for (const [bi, paramId] of Object.entries(deckAFftMap)) {
        const p = deckAMeta.find((m) => m.name === paramId);
        if (p) {
          const norm = audioEngine.bands[Number(bi)];
          if (p.type === 'bool') deckAParamValues[paramId] = norm > 0.5;
          else {
            const scaled = p.min + (p.max - p.min) * norm;
            deckAParamValues[paramId] = Math.min(p.max, Math.max(p.min, scaled));
          }
        }
      }
    }
    for (const [paramName, normVal] of Object.entries(oscEngine.vjDeckAPending)) {
      const p = deckAMeta.find((m) => m.name === paramName);
      if (p) {
        if (p.type === 'bool') deckAParamValues[paramName] = normVal > 0.5;
        else {
          const scaled = p.min + (p.max - p.min) * normVal;
          deckAParamValues[paramName] = Math.min(p.max, Math.max(p.min, scaled));
        }
      }
    }
    oscEngine.vjDeckAPending = {};
    runDeck(glA, deckA.canvas, deckAProgRef, bufA, deckAParamValues, deckAMeta, deckASamplerNames, 'A');
  }

  function runDeckBLogic(): void {
    if (audioEngine.active) {
      for (const [bi, paramId] of Object.entries(deckBFftMap)) {
        const p = deckBMeta.find((m) => m.name === paramId);
        if (p) {
          const norm = audioEngine.bands[Number(bi)];
          if (p.type === 'bool') deckBParamValues[paramId] = norm > 0.5;
          else {
            const scaled = p.min + (p.max - p.min) * norm;
            deckBParamValues[paramId] = Math.min(p.max, Math.max(p.min, scaled));
          }
        }
      }
    }
    for (const [paramName, normVal] of Object.entries(oscEngine.vjDeckBPending)) {
      const p = deckBMeta.find((m) => m.name === paramName);
      if (p) {
        if (p.type === 'bool') deckBParamValues[paramName] = normVal > 0.5;
        else {
          const scaled = p.min + (p.max - p.min) * normVal;
          deckBParamValues[paramName] = Math.min(p.max, Math.max(p.min, scaled));
        }
      }
    }
    oscEngine.vjDeckBPending = {};
    runDeck(glB, deckB.canvas, deckBProgRef, bufB, deckBParamValues, deckBMeta, deckBSamplerNames, 'B');
  }

  function runMixLogic(): void {
    updateTextureFromCanvas(glMix, texA, deckA.canvas);
    updateTextureFromCanvas(glMix, texB, deckB.canvas);

    glMix.useProgram(mixProgram!);
    glMix.viewport(0, 0, outputCanvas.width, outputCanvas.height);
    glMix.clearColor(0.05, 0.05, 0.08, 1);
    glMix.clear(glMix.COLOR_BUFFER_BIT);

    glMix.activeTexture(glMix.TEXTURE0);
    glMix.bindTexture(glMix.TEXTURE_2D, texA);
    glMix.activeTexture(glMix.TEXTURE1);
    glMix.bindTexture(glMix.TEXTURE_2D, texB);

    const crossfaderLoc = glMix.getUniformLocation(mixProgram!, 'crossfader');
    if (crossfaderLoc) glMix.uniform1f(crossfaderLoc, crossfader);
    const mixModeLoc = glMix.getUniformLocation(mixProgram!, 'mixMode');
    if (mixModeLoc) glMix.uniform1i(mixModeLoc, effectiveMixModeInt());
    const outputFlipVLoc = glMix.getUniformLocation(mixProgram!, 'outputFlipV');
    if (outputFlipVLoc) glMix.uniform1f(outputFlipVLoc, outputFlipV ? 1 : 0);
    const outputFlipHLoc = glMix.getUniformLocation(mixProgram!, 'outputFlipH');
    if (outputFlipHLoc) glMix.uniform1f(outputFlipHLoc, outputFlipH ? 1 : 0);
    const outputRotationLoc = glMix.getUniformLocation(mixProgram!, 'outputRotation');
    if (outputRotationLoc) glMix.uniform1i(outputRotationLoc, outputRotation === 0 ? 0 : outputRotation === 90 ? 1 : outputRotation === 180 ? 2 : 3);
    const texALoc = glMix.getUniformLocation(mixProgram!, 'texA');
    if (texALoc) glMix.uniform1i(texALoc, 0);
    const texBLoc = glMix.getUniformLocation(mixProgram!, 'texB');
    if (texBLoc) glMix.uniform1i(texBLoc, 1);

    const loc = glMix.getAttribLocation(mixProgram!, 'a_pos');
    glMix.enableVertexAttribArray(loc);
    glMix.vertexAttribPointer(loc, 2, glMix.FLOAT, false, 0, 0);
    glMix.drawArrays(glMix.TRIANGLE_STRIP, 0, 4);
  }

  function sendFullSync(): void {
    if (lastPreparedA) {
      sendVJMessage({
        type: 'shader', deck: 'A',
        preparedSource: lastPreparedA,
        meta: deckAMeta.map(m => ({ name: m.name, type: m.type }))
      });
    }
    if (lastPreparedB) {
      sendVJMessage({
        type: 'shader', deck: 'B',
        preparedSource: lastPreparedB,
        meta: deckBMeta.map(m => ({ name: m.name, type: m.type }))
      });
    }
  }

  vjChannel.onmessage = (ev: MessageEvent) => {
    if (ev.data && ev.data.type === 'output-ready') {
      sendFullSync();
    }
  };

  let lastBroadcast = 0;
  let lastFrameJson = '';
  let lastVjFrameTime = 0;

  const sendQrOverlayRelay = (): void => {
    sendVJMessage({
      type: 'qr-overlay',
      qrOverlay: getGigOutputQrFramePayload() ?? {
        enabled: false,
        streamEnabled: false,
        mix: 0,
        opacity: 0,
        streamOpacity: 0,
        layout: { ...getGigOutputQrLayout() },
      },
    });
  };
  setGigOutputQrFrameRelay(sendQrOverlayRelay);
  window.addEventListener('macroverse-gig-output-qr-visible', () => {
    lastFrameJson = '';
  });
  window.addEventListener('macroverse-gig-stream-qr-visible', () => {
    lastFrameJson = '';
  });
  window.addEventListener('macroverse-gig-output-qr-layout', () => {
    lastFrameJson = '';
  });

  function runVJFrame(): void {
    if (!vjDeckTabActive) {
      if (rafMix) {
        cancelAnimationFrame(rafMix);
        rafMix = 0;
      }
      return;
    }
    try {
    const now = performance.now();
    lastVjFrameTime = now;

    if (audioEngine.active) {
      audioEngine.update();
      audioEngine.bandParamMap = {};
      for (const [bi, name] of Object.entries(deckAFftMap)) {
        audioEngine.bandParamMap[Number(bi)] = name;
      }
      for (const [bi, name] of Object.entries(deckBFftMap)) {
        audioEngine.bandParamMap[Number(bi)] = name;
      }
      audioEngine.drawFFT(fftCanvas);
    } else {
      fftCanvas.getContext('2d')?.clearRect(0, 0, fftCanvas.width, fftCanvas.height);
    }
    runDeckALogic();
    runDeckBLogic();
    runMixLogic();

    const manualX = tickCrossfadeAnim(manualCrossfadeAnim, now);
    if (manualX !== null) {
      crossfader = manualX;
      updateCrossfaderUi();
      if (!applyingRemoteControl) publishVjControl({ crossfader });
    }
    trackRandomMixTransition();

    if (outputCanvas) {
      const codeState = getCodeOverlayState();
      if (codeState.visible && codeState.opacity > 0.01) {
        const octx = outputCanvas.getContext('2d');
        if (octx) {
          const codeCanvas = document.createElement('canvas');
          codeCanvas.width = outputCanvas.width;
          codeCanvas.height = outputCanvas.height;
          renderCodeOverlayFromState(codeCanvas);
          octx.save();
          octx.globalAlpha = codeState.opacity;
          octx.drawImage(codeCanvas, 0, 0);
          octx.restore();
        }
      }
      applyOutputFxCanvas(outputCanvas, getOutputFxState());
    }

    // Multi-device Roliblock LED dispatch
    if (outputCanvas) {
      const mgr = roliblockManager;
      if (mgr.ledDisplayMode === 'sharedOutput') {
        for (const dev of mgr.getDevices()) {
          if (dev.enabled) dev.sampleAndSendLed(outputCanvas);
        }
      } else if (mgr.ledDisplayMode === 'stretched' || mgr.ledDisplayMode === 'linked') {
        sendStretchedLed(mgr.getDevices(), outputCanvas);
      } else {
        const devA = mgr.getDeviceForDeck('A');
        const devB = mgr.getDeviceForDeck('B');
        if (devA && devA.enabled) devA.sampleAndSendLed(deckA.canvas);
        if (devB && devB.enabled) devB.sampleAndSendLed(deckB.canvas);
        for (const dev of mgr.getDevices()) {
          if (dev.enabled && dev.deckAssignment === 'shared') {
            dev.sampleAndSendLed(outputCanvas);
          }
        }
      }
    }

    if (autoVjEnabled) {
      const beatMs = 60000 / autoVjBpm;
      const barBeats = autoVjParamConfig.barBeats;
      while (now - autoVjLastBeatTime >= beatMs) {
        autoVjLastBeatTime += beatMs;
        autoVjBeatCount++;
        const paramKeys = [
          ...deckAMeta.filter((p) => p.type === 'float' || p.type === 'int').map((p) => p.name),
          ...deckBMeta.filter((p) => p.type === 'float' || p.type === 'int').map((p) => p.name),
        ];
        if (autoVjParamConfig.paramMove) {
          onAutoVjBeatTick(autoVjParamState, autoVjBeatCount, paramKeys, autoVjParamConfig);
          updateAutoVjParamHint();
        }
        if (autoVjBeatCount % barBeats === 0) {
          autoVjCrossfadeStart = autoVjLastBeatTime;
          autoVjCrossfadeStartVal = crossfader;
          autoVjCrossfadeTarget = autoVjCrossfadeTarget > 0.5 ? 0 : 1;
          if (mixMode === 'random') pickRandomMixForTransition();
          if (autoVjParamConfig.shaderSwap) {
            const list = getShaderList();
            if (list.length > 0) {
              loadDeckAByGlobalIndex(Math.floor(Math.random() * list.length));
              loadDeckBByGlobalIndex(Math.floor(Math.random() * list.length));
            }
          }
        }
      }
      const crossfadeDurationMs = barBeats * beatMs;
      const crossfadeElapsed = now - autoVjCrossfadeStart;
      if (crossfadeElapsed < crossfadeDurationMs && crossfadeElapsed >= 0) {
        const t = Math.min(1, crossfadeElapsed / crossfadeDurationMs);
        const smooth = 0.5 - 0.5 * Math.cos(t * Math.PI);
        crossfader = autoVjCrossfadeStartVal + (autoVjCrossfadeTarget - autoVjCrossfadeStartVal) * smooth;
        crossfaderInput.value = String(crossfader);
        updateSliderFill(crossfaderInput);
        crossfaderVal.textContent = (crossfader * 100).toFixed(1) + '%';
      }
      const beatClock = autoVjBeatClock(autoVjBeatCount, now - autoVjLastBeatTime, beatMs);
      if (autoVjParamConfig.paramMove) {
        const fftMappedA = new Set(Object.values(deckAFftMap));
        const fftMappedB = new Set(Object.values(deckBFftMap));
        tickAutoVjDeckParams({
          config: autoVjParamConfig,
          state: autoVjParamState,
          beatClock,
          meta: deckAMeta,
          valuesRef: deckAParamValues,
          fftMapped: fftMappedA,
          skipFftMapped: audioEngine.active,
          deckOffset: 0,
        });
        tickAutoVjDeckParams({
          config: autoVjParamConfig,
          state: autoVjParamState,
          beatClock,
          meta: deckBMeta,
          valuesRef: deckBParamValues,
          fftMapped: fftMappedB,
          skipFftMapped: audioEngine.active,
          deckOffset: 1,
        });
      }
    }

    trackRandomMixTransition();

    if (now - lastBroadcast > 33) {
      lastBroadcast = now;
      const frameData = {
        type: 'frame' as const,
        crossfader,
        mixModeInt: effectiveMixModeInt(),
        flipV: outputFlipV,
        flipH: outputFlipH,
        rotation: outputRotation,
        mouseX: _vjMouseXRef,
        mouseY: _vjMouseYRef,
        mouseAX: _vjMouseAX,
        mouseAY: _vjMouseAY,
        mouseBX: _vjMouseBX,
        mouseBY: _vjMouseBY,
        paramsA: { ...deckAParamValues },
        paramsB: { ...deckBParamValues },
        qrOverlay: getGigOutputQrFramePayload() ?? {
          enabled: false,
          streamEnabled: false,
          mix: 0,
          opacity: 0,
          streamOpacity: 0,
          layout: { ...getGigOutputQrLayout() },
        },
      };
      const json = JSON.stringify(frameData);
      if (json !== lastFrameJson) {
        lastFrameJson = json;
        sendVJMessage(frameData);
      }
    }
    } catch (err) {
      console.error('[VJ] frame loop error:', err);
    }
    if (!document.hidden && vjDeckTabActive) rafMix = requestAnimationFrame(runVJFrame);
    else rafMix = 0;
  }

  vjFrameLoopStop = () => {
    if (rafMix) {
      cancelAnimationFrame(rafMix);
      rafMix = 0;
    }
    if (bgVjInterval) {
      clearInterval(bgVjInterval);
      bgVjInterval = 0;
    }
  };
  vjFrameLoopStart = () => {
    if (!vjDeckTabActive) return;
    if (rafMix) {
      cancelAnimationFrame(rafMix);
      rafMix = 0;
    }
    if (bgVjInterval) {
      clearInterval(bgVjInterval);
      bgVjInterval = 0;
    }
    runVJFrame();
  };

  // Keep VJ frame loop running when tab is hidden (for LED streaming + parameter automation)
  document.addEventListener('visibilitychange', () => {
    if (!vjDeckTabActive) return;
    if (document.hidden) {
      if (!bgVjInterval) bgVjInterval = window.setInterval(() => runVJFrame(), 33);
    } else {
      if (bgVjInterval) { clearInterval(bgVjInterval); bgVjInterval = 0; }
      if (rafMix) cancelAnimationFrame(rafMix);
      rafMix = requestAnimationFrame(runVJFrame);
    }
  });

  async function applyShaderSourceToDeck(
    path: string,
    src: string,
    progRef: { current: WebGLProgram | null },
    gl: WebGLRenderingContext,
    entryRef: { value: IndexEntry | null },
    valuesRef: Record<string, number | boolean>,
    onParamsLoaded: (meta: ExposeItem[]) => void,
    deckId: 'A' | 'B'
  ): Promise<void> {
    try {
      const entry = getShaderList().find((e) => (e.path ?? '') === path) ?? { path } as IndexEntry;
      entryRef.value = entry;
      if (progRef.current) gl.deleteProgram(progRef.current);
      const prepared = prepareFragmentForOffscreenRender(stripLeadingGarbage(src || ''));
      progRef.current = createDeckProgram(gl, src);
      const meta = parseExposeFromSource(src);
      const samplerNames = getSampler2DNamesFromPrepared(prepared);
      for (const k of Object.keys(valuesRef)) delete valuesRef[k];
      for (const p of meta) valuesRef[p.name] = p.def;
      onParamsLoaded(meta);
      if (deckId === 'A') { lastPreparedA = prepared; deckASamplerNames = samplerNames; }
      else { lastPreparedB = prepared; deckBSamplerNames = samplerNames; }
      sendVJMessage({
        type: 'shader',
        deck: deckId,
        preparedSource: prepared,
        meta: meta.map(m => ({ name: m.name, type: m.type }))
      });
    } catch (e) {
      if (progRef.current) gl.deleteProgram(progRef.current);
      progRef.current = null;
      entryRef.value = null;
      for (const k of Object.keys(valuesRef)) delete valuesRef[k];
      onParamsLoaded([]);
      if (deckId === 'A') { lastPreparedA = ''; deckASamplerNames = []; }
      else { lastPreparedB = ''; deckBSamplerNames = []; }
      sendVJMessage({ type: 'clear', deck: deckId });
      const errMsg = e instanceof Error ? e.message : String(e);
      console.warn('[VJ Deck] live shader compile failed:', errMsg.slice(0, 200));
    }
  }

  async function loadShaderForDeck(
    path: string,
    progRef: { current: WebGLProgram | null },
    gl: WebGLRenderingContext,
    entryRef: { value: IndexEntry | null },
    valuesRef: Record<string, number | boolean>,
    onParamsLoaded: (meta: ExposeItem[]) => void,
    deckId: 'A' | 'B'
  ): Promise<void> {
    if (!path) {
      if (progRef.current) gl.deleteProgram(progRef.current);
      progRef.current = null;
      entryRef.value = null;
      for (const k of Object.keys(valuesRef)) delete valuesRef[k];
      onParamsLoaded([]);
      if (deckId === 'A') { lastPreparedA = ''; deckASamplerNames = []; }
      else { lastPreparedB = ''; deckBSamplerNames = []; }
      sendVJMessage({ type: 'clear', deck: deckId });
      return;
    }
    try {
      const src = await fetchShader(path);
      await applyShaderSourceToDeck(path, src, progRef, gl, entryRef, valuesRef, onParamsLoaded, deckId);
    } catch (e) {
      if (progRef.current) gl.deleteProgram(progRef.current);
      progRef.current = null;
      entryRef.value = null;
      for (const k of Object.keys(valuesRef)) delete valuesRef[k];
      onParamsLoaded([]);
      if (deckId === 'A') { lastPreparedA = ''; deckASamplerNames = []; }
      else { lastPreparedB = ''; deckBSamplerNames = []; }
      sendVJMessage({ type: 'clear', deck: deckId });
      const errMsg = e instanceof Error ? e.message : String(e);
      console.warn('[VJ Deck] shader compile failed:', errMsg.slice(0, 200));
    }
  }

  crossfaderInput.addEventListener('input', () => {
    manualCrossfadeAnim.active = false;
    crossfader = parseFloat(crossfaderInput.value);
    updateCrossfaderUi();
    if (!applyingRemoteControl) publishVjControl({ crossfader });
  });

  mixModeSelect.addEventListener('change', () => {
    mixMode = mixModeSelect.value as MixMode;
    if (mixMode === 'random') {
      randomMixTransitionArmed = true;
      pickRandomMixForTransition();
    } else {
      updateRandomMixHint();
    }
    if (!applyingRemoteControl) publishVjControl({ mixMode });
  });

  function setAutoVjEnabled(en: boolean): void {
    autoVjEnabled = en;
    autoVjToggleBtn.textContent = en ? 'On' : 'Off';
    autoVjToggleBtn.classList.toggle('is-on', en);
    if (en) {
      if (!audioEngine.active) {
        audioEngine.start().then(() => {
          updateAudioStatus();
          maybeAutoAssignDeckFft('A', deckAMeta);
          maybeAutoAssignDeckFft('B', deckBMeta);
          rebuildFftUI();
        }).catch(() => {});
      }
      maybeAutoAssignDeckFft('A', deckAMeta);
      maybeAutoAssignDeckFft('B', deckBMeta);
      rebuildFftUI();
      autoVjLastBeatTime = performance.now();
      autoVjBeatCount = 0;
      autoVjCrossfadeStart = performance.now();
      autoVjCrossfadeStartVal = crossfader;
      autoVjCrossfadeTarget = crossfader > 0.5 ? 0 : 1;
      if (mixMode === 'random') pickRandomMixForTransition();
      autoVjParamState.activeRandomMode = pickRandomAutoVjParamMode();
      updateAutoVjParamHint();
    }
    if (!applyingRemoteControl) {
      publishVjControl({ autoVjEnabled: en, autoVjBpm });
    }
  }
  autoVjToggleBtn.addEventListener('click', () => setAutoVjEnabled(!autoVjEnabled));
  autoVjBpmInput.addEventListener('change', () => {
    const v = parseFloat(autoVjBpmInput.value);
    if (!Number.isNaN(v) && v >= 30 && v <= 240) {
      autoVjBpm = v;
      if (!applyingRemoteControl) publishVjControl({ autoVjBpm: v });
    }
  });
  autoVjTapBtn.addEventListener('click', () => {
    const t = performance.now();
    autoVjTapTimes.push(t);
    if (autoVjTapTimes.length > 4) autoVjTapTimes.shift();
    if (autoVjTapTimes.length >= 2) {
      const intervals: number[] = [];
      for (let i = 1; i < autoVjTapTimes.length; i++) intervals.push(autoVjTapTimes[i] - autoVjTapTimes[i - 1]);
      const avgMs = intervals.reduce((a, x) => a + x, 0) / intervals.length;
      const bpm = Math.round(60000 / avgMs);
      if (bpm >= 30 && bpm <= 240) {
        autoVjBpm = bpm;
        autoVjBpmInput.value = String(bpm);
        if (!applyingRemoteControl) publishVjControl({ autoVjBpm: bpm });
      }
    }
  });

  initVjContextMenus(wrap, {
    deckNavigate: (deck, delta) => {
      if (deck === 'A') deckA.navigateShader(delta);
      else deckB.navigateShader(delta);
    },
    crossfaderInput: crossfaderInput,
    mixModeSelect,
    autoVjToggle: () => setAutoVjEnabled(!autoVjEnabled),
    autoVjTap: () => autoVjTapBtn.click(),
    autoVjEnabled: () => autoVjEnabled,
    oscToggle: () => oscToggleBtn.click(),
    oscActive: () => oscEngine.active,
    oscPortInput,
    midiEnable: () => midiEnableBtn.click(),
    midiActive: () => midiEngine.active,
    audioToggle: () => audioToggleBtn.click(),
    audioActive: () => audioEngine.active,
    popOut: () => popOutBtn.click(),
    copyOutputUrl: () => outputUrlCopy.click(),
    flipH: () => { flipHCheck.checked = !flipHCheck.checked; flipHCheck.dispatchEvent(new Event('change', { bubbles: true })); },
    flipV: () => { flipVCheck.checked = !flipVCheck.checked; flipVCheck.dispatchEvent(new Event('change', { bubbles: true })); },
    flipHState: () => flipHCheck.checked,
    flipVState: () => flipVCheck.checked,
  });

  function loadDeckABySlot(slot: number): void {
    const list = getShaderList();
    const idx = currentPageA * CLIPS_PER_PAGE + Math.max(0, Math.min(39, Math.floor(slot)));
    const entry = list[idx];
    if (!entry?.path) return;
    deckAEntry.value = entry;
    loadShaderForDeck(entry.path, deckAProgRef, glA!, deckAEntry, deckAParamValues, (meta) => {
      deckAMeta = meta;
      deckA.updateParams(meta);
      maybeAutoAssignDeckFft('A', meta);
      rebuildFftUI();
      buildVjOscMaps();
    }, 'A');
    deckA.setDisplayPath(entry.path);
  }
  function loadDeckBBySlot(slot: number): void {
    const list = getShaderList();
    const idx = currentPageB * CLIPS_PER_PAGE + Math.max(0, Math.min(39, Math.floor(slot)));
    const entry = list[idx];
    if (!entry?.path) return;
    deckBEntry.value = entry;
    loadShaderForDeck(entry.path, deckBProgRef, glB!, deckBEntry, deckBParamValues, (meta) => {
      deckBMeta = meta;
      deckB.updateParams(meta);
      maybeAutoAssignDeckFft('B', meta);
      rebuildFftUI();
      buildVjOscMaps();
    }, 'B');
    deckB.setDisplayPath(entry.path);
  }
  function loadDeckAByGlobalIndex(globalIndex: number): void {
    const list = getShaderList();
    if (list.length === 0) return;
    const idx = Math.max(0, Math.min(list.length - 1, Math.floor(globalIndex)));
    deckAGlobalIndex = idx;
    if (!applyingRemoteControl) publishVjControl({ deckAGlobalIndex: idx });
    const entry = list[idx];
    if (!entry?.path) return;
    deckAEntry.value = entry;
    loadShaderForDeck(entry.path, deckAProgRef, glA!, deckAEntry, deckAParamValues, (meta) => {
      deckAMeta = meta;
      deckA.updateParams(meta);
      maybeAutoAssignDeckFft('A', meta);
      rebuildFftUI();
      buildVjOscMaps();
    }, 'A');
    deckA.setDisplayPath(entry.path);
  }
  function loadDeckBByGlobalIndex(globalIndex: number): void {
    const list = getShaderList();
    if (list.length === 0) return;
    const idx = Math.max(0, Math.min(list.length - 1, Math.floor(globalIndex)));
    deckBGlobalIndex = idx;
    if (!applyingRemoteControl) publishVjControl({ deckBGlobalIndex: idx });
    const entry = list[idx];
    if (!entry?.path) return;
    deckBEntry.value = entry;
    loadShaderForDeck(entry.path, deckBProgRef, glB!, deckBEntry, deckBParamValues, (meta) => {
      deckBMeta = meta;
      deckB.updateParams(meta);
      maybeAutoAssignDeckFft('B', meta);
      rebuildFftUI();
      buildVjOscMaps();
    }, 'B');
    deckB.setDisplayPath(entry.path);
  }

  function loadDeckByPath(deck: 'A' | 'B', path: string): void {
    const trimmed = path.trim();
    if (!trimmed) return;
    const idx = getShaderList().findIndex((e) => (e.path ?? '') === trimmed);
    const entry = getShaderList().find((e) => (e.path ?? '') === trimmed) ?? ({ path: trimmed } as IndexEntry);
    if (deck === 'B') {
      if (idx >= 0) deckBGlobalIndex = idx;
      deckBEntry.value = entry;
      deckB.setDisplayPath(trimmed);
    } else {
      if (idx >= 0) deckAGlobalIndex = idx;
      deckAEntry.value = entry;
      deckA.setDisplayPath(trimmed);
    }
    shaderCarousel.updateActive();
    shaderCarousel.scrollToPath(trimmed);
    if (!glA || !glB) return;
    if (deck === 'B') {
      loadShaderForDeck(trimmed, deckBProgRef, glB, deckBEntry, deckBParamValues, (meta) => {
        deckBMeta = meta;
        deckB.updateParams(meta);
        maybeAutoAssignDeckFft('B', meta);
        rebuildFftUI();
        buildVjOscMaps();
      }, 'B');
      return;
    }
    loadShaderForDeck(trimmed, deckAProgRef, glA, deckAEntry, deckAParamValues, (meta) => {
      deckAMeta = meta;
      deckA.updateParams(meta);
      maybeAutoAssignDeckFft('A', meta);
      rebuildFftUI();
      buildVjOscMaps();
    }, 'A');
  }

  vjActiveLoadDeckByPath = loadDeckByPath;

  vjController.register('vj/crossfader', (v) => {
    crossfader = v;
    crossfaderInput.value = String(v);
    updateSliderFill(crossfaderInput);
    crossfaderVal.textContent = (v * 100).toFixed(1) + '%';
  });
  vjController.register('vj/autoVj', (v) => setAutoVjEnabled(v > 0.5));
  for (let i = 0; i < 8; i++) {
    vjController.register(`vj/deckA/param/${i}` as import('../engines/vjController.js').VJActionId, (v) => {
      const p = deckAMeta[i];
      if (p) {
        const val = p.type === 'bool' ? v > 0.5 : p.min + (p.max - p.min) * v;
        deckAParamValues[p.name] = p.type === 'bool' ? val : Math.max(p.min, Math.min(p.max, val));
      }
    });
    vjController.register(`vj/deckB/param/${i}` as import('../engines/vjController.js').VJActionId, (v) => {
      const p = deckBMeta[i];
      if (p) {
        const val = p.type === 'bool' ? v > 0.5 : p.min + (p.max - p.min) * v;
        deckBParamValues[p.name] = p.type === 'bool' ? val : Math.max(p.min, Math.min(p.max, val));
      }
    });
  }
  vjController.register('vj/loadClipA', (v) => loadDeckABySlot(v));
  vjController.register('vj/loadClipB', (v) => loadDeckBBySlot(v));
  vjController.register('vj/deckA/pageUp', () => {
    currentPageA++;
    if (!applyingRemoteControl) publishVjControl({ pageA: currentPageA });
  });
  vjController.register('vj/deckA/pageDown', () => {
    currentPageA = Math.max(0, currentPageA - 1);
    if (!applyingRemoteControl) publishVjControl({ pageA: currentPageA });
  });
  vjController.register('vj/deckB/pageLeft', () => {
    currentPageB = Math.max(0, currentPageB - 1);
    if (!applyingRemoteControl) publishVjControl({ pageB: currentPageB });
  });
  vjController.register('vj/deckB/pageRight', () => {
    currentPageB++;
    if (!applyingRemoteControl) publishVjControl({ pageB: currentPageB });
  });

  onAudienceMouse((mx, my) => {
    if (!getAudienceParticipationEnabled()) return;
    setVjMouseFromRolibblock(mx, my);
    lastBroadcast = 0;
    lastFrameJson = '';
  });
  onVjConfig((cfg) => {
    setAudienceParticipationEnabled(cfg.audienceParticipation);
  });

  void ensureVjTokens(getVjSessionId()).then(() => {
    syncOutputUrlInput();
    if (!isVjViewOnlyMode()) {
      connectVjSession();
      if (getAudienceParticipationEnabled()) {
        void pushAudienceParticipation(true);
      }
    }
  });
  onRemoteVjControl((ctrl) => {
    applyingRemoteControl = true;
    try {
      if (typeof ctrl.crossfader === 'number') {
        crossfader = ctrl.crossfader;
        crossfaderInput.value = String(crossfader);
        updateSliderFill(crossfaderInput);
        crossfaderVal.textContent = (crossfader * 100).toFixed(1) + '%';
      }
      if (ctrl.mixMode && isMixMode(ctrl.mixMode)) {
        mixMode = ctrl.mixMode;
        mixModeSelect.value = mixMode;
        if (mixMode === 'random') {
          randomMixTransitionArmed = true;
          pickRandomMixForTransition();
        } else {
          updateRandomMixHint();
        }
      }
      if (typeof ctrl.pageA === 'number') currentPageA = ctrl.pageA;
      if (typeof ctrl.pageB === 'number') currentPageB = ctrl.pageB;
      if (typeof ctrl.deckAGlobalIndex === 'number' && ctrl.deckAGlobalIndex >= 0 && ctrl.deckAGlobalIndex !== deckAGlobalIndex) {
        loadDeckAByGlobalIndex(ctrl.deckAGlobalIndex);
      }
      if (typeof ctrl.deckBGlobalIndex === 'number' && ctrl.deckBGlobalIndex >= 0 && ctrl.deckBGlobalIndex !== deckBGlobalIndex) {
        loadDeckBByGlobalIndex(ctrl.deckBGlobalIndex);
      }
      if (ctrl.paramsA) {
        for (const [k, v] of Object.entries(ctrl.paramsA)) deckAParamValues[k] = v;
      }
      if (ctrl.paramsB) {
        for (const [k, v] of Object.entries(ctrl.paramsB)) deckBParamValues[k] = v;
      }
      if (typeof ctrl.autoVjEnabled === 'boolean' && ctrl.autoVjEnabled !== autoVjEnabled) {
        setAutoVjEnabled(ctrl.autoVjEnabled);
      }
      if (typeof ctrl.autoVjBpm === 'number' && ctrl.autoVjBpm >= 30 && ctrl.autoVjBpm <= 240) {
        autoVjBpm = ctrl.autoVjBpm;
        autoVjBpmInput.value = String(Math.round(ctrl.autoVjBpm));
      }
    } finally {
      applyingRemoteControl = false;
    }
  });

  onVjShaderLive(({ deck, path, source }) => {
    if (deck === 'B') {
      void applyShaderSourceToDeck(path, source, deckBProgRef, glB!, deckBEntry, deckBParamValues, (meta) => {
        deckBMeta = meta;
        deckB.updateParams(meta);
        maybeAutoAssignDeckFft('B', meta);
        rebuildFftUI();
        buildVjOscMaps();
      }, 'B');
      deckB.setDisplayPath(path);
      return;
    }
    void applyShaderSourceToDeck(path, source, deckAProgRef, glA!, deckAEntry, deckAParamValues, (meta) => {
      deckAMeta = meta;
      deckA.updateParams(meta);
      maybeAutoAssignDeckFft('A', meta);
      rebuildFftUI();
      buildVjOscMaps();
    }, 'A');
    deckA.setDisplayPath(path);
  });

  if (vjDeckTabActive) runVJFrame();
}
