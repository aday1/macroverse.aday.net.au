import { entries, listSetFilter } from '../state.js';
import { fetchShader } from '../api.js';
import { prepareFragmentForOffscreenRender, stripLeadingGarbage } from '../render.js';
import { parseExposeFromSource, type ExposeItem } from './params.js';
import { audioEngine, FFT_BAND_LABELS, FFT_BAND_COUNT } from '../engines/audio.js';
import { oscEngine } from '../engines/osc.js';
import { midiEngine } from '../engines/midi.js';
import { vjController } from '../engines/vjController.js';
import { roliblockManager, sendStretchedLed } from '../engines/roliblock.js';
import type { IndexEntry } from '../types.js';
import {
  getAudienceParticipationEnabled,
  pushAudienceParticipation,
  setAudienceParticipationEnabled,
} from '../vjAudienceParticipation.js';
import { getVjSessionId, isVjViewOnlyMode } from '../vjSession.js';
import { onAudienceMouse, onVjConfig } from '../vjWs.js';
import { ensureVjTokens, vjControlQuery, vjViewQuery } from '../vjTokens.js';
import {
  drawGigOutputQrOnCanvas,
  gigOutputQrLayoutFromPointer,
  getGigOutputQrFramePayload,
  getGigOutputQrLayout,
  isGigOutputQrVisible,
  refreshGigOutputQrSession,
  setGigOutputQrLayout,
  setGigOutputQrVisible,
} from '../gigOutputQr.js';
import { createVjPreviewGigQrBlock } from '../gigQrUi.js';
import { connectVjSession, onRemoteVjControl, publishVjControl } from '../vjWs.js';

const CLIPS_PER_PAGE = 40;

const VJ_FFT_A_KEY = 'macroverse-vj-fft-a';
const VJ_FFT_B_KEY = 'macroverse-vj-fft-b';
const VJ_OSC_A_KEY = 'macroverse-vj-osc-a';
const VJ_OSC_B_KEY = 'macroverse-vj-osc-b';

const vjChannel = new BroadcastChannel('macroverse-vj-output');

let lastRelayPost = 0;
const RELAY_THROTTLE_MS = 250;

/** True while the VJ view tab is active; pauses WebGL loop and API relay when false. */
let vjDeckTabActive = false;
let vjFrameLoopStart: (() => void) | null = null;
let vjFrameLoopStop: (() => void) | null = null;

export function setVjDeckTabActive(active: boolean): void {
  vjDeckTabActive = active;
  if (active) vjFrameLoopStart?.();
  else vjFrameLoopStop?.();
}

function sendVJMessage(msg: unknown): void {
  vjChannel.postMessage(msg);
  const isFrame = typeof msg === 'object' && msg !== null && (msg as { type?: string }).type === 'frame';
  if (isFrame && !vjDeckTabActive && !isGigOutputQrVisible()) return;
  const now = performance.now();
  if (isFrame) {
    if (now - lastRelayPost < RELAY_THROTTLE_MS) return;
    lastRelayPost = now;
  }
  fetch(`/api/vj-output/state?${vjControlQuery()}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(msg)
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

const DECK_CANVAS_W = 320;
const DECK_CANVAS_H = 180;
const OUTPUT_CANVAS_W = 640;
const OUTPUT_CANVAS_H = 360;

const VERT_SRC = `precision highp float;
attribute vec2 a_pos;
varying vec2 v_uv;
void main() {
  vec2 uv = a_pos * 0.5 + 0.5;
  v_uv = uv;
  gl_Position = vec4(a_pos, 0.0, 1.0);
}`;

const MIX_FRAG_SRC = `precision highp float;
uniform sampler2D texA;
uniform sampler2D texB;
uniform float crossfader;
uniform int mixMode;
uniform float outputFlipV;
uniform float outputFlipH;
uniform int outputRotation;
varying vec2 v_uv;
void main() {
  vec2 uv = vec2(v_uv.x, 1.0 - v_uv.y);
  if (outputFlipV > 0.5) uv.y = 1.0 - uv.y;
  if (outputFlipH > 0.5) uv.x = 1.0 - uv.x;
  if (outputRotation == 1) uv = vec2(1.0 - uv.y, uv.x);
  else if (outputRotation == 2) uv = vec2(1.0 - uv.x, 1.0 - uv.y);
  else if (outputRotation == 3) uv = vec2(uv.y, 1.0 - uv.x);
  vec4 a = texture2D(texA, uv);
  vec4 b = texture2D(texB, uv);
  if (mixMode == 1) {
    gl_FragColor = a * (1.0 - b.a * crossfader) + b * b.a * crossfader;
  } else if (mixMode == 2) {
    gl_FragColor = clamp(a + b * crossfader, 0.0, 1.0);
  } else if (mixMode == 3) {
    gl_FragColor = mix(a, a * b, crossfader);
  } else if (mixMode == 4) {
    float luma = dot(b.rgb, vec3(0.299, 0.587, 0.114));
    gl_FragColor = mix(a, b, luma * crossfader);
  } else {
    gl_FragColor = mix(a, b, crossfader);
  }
}`;

const QUAD_VERTS = new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]);

function getSampler2DNamesFromPrepared(preparedFrag: string): string[] {
  const re = /uniform\s+sampler2D\s+(\w+)\s*[;=]/gi;
  const out: string[] = [];
  let m: RegExpExecArray | null;
  while ((m = re.exec(preparedFrag)) !== null) out.push(m[1]);
  return out;
}

type MixMode = 'crossfade' | 'alpha' | 'add' | 'multiply' | 'luma';
const MIX_MODES: { value: MixMode; label: string; modeInt: number }[] = [
  { value: 'crossfade', label: 'Crossfade', modeInt: 0 },
  { value: 'alpha', label: 'Alpha Layer', modeInt: 1 },
  { value: 'add', label: 'Add', modeInt: 2 },
  { value: 'multiply', label: 'Multiply', modeInt: 3 },
  { value: 'luma', label: 'Luma Key', modeInt: 4 }
];

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
  const f = compileShader(gl, gl.FRAGMENT_SHADER, MIX_FRAG_SRC);
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
let _perDeckMouseEnabled = false;

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
  let mx: number, my: number;
  if (_perDeckMouseEnabled && deckKey === 'A') { mx = _vjMouseAX; my = _vjMouseAY; }
  else if (_perDeckMouseEnabled && deckKey === 'B') { mx = _vjMouseBX; my = _vjMouseBY; }
  else { mx = _vjMouseXRef; my = _vjMouseYRef; }
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

export function initVJDeck(): void {
  const container = document.getElementById('vjDeckContainer');
  if (!container) return;

  if (container.querySelector('.vj-deck-wrap')) {
    if (vjDeckTabActive) vjFrameLoopStart?.();
    return;
  }
  if (container.querySelector('.vj-deck-loading')) return;

  container.replaceChildren();
  const loading = document.createElement('div');
  loading.className = 'vj-deck-loading';
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
      errEl.textContent = 'VJ deck failed to load. Try refreshing the page or check the browser console.';
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

  const shaderList = listSetFilter ? entries.filter((e) => (e.sets || []).includes(listSetFilter)) : entries;
  function getShaderList(): IndexEntry[] {
    return listSetFilter ? entries.filter((e) => (e.sets || []).includes(listSetFilter)) : entries;
  }
  const deckAEntry: { value: IndexEntry | null } = { value: null };
  const deckBEntry: { value: IndexEntry | null } = { value: null };
  let crossfader = 0;
  let mixMode: MixMode = 'crossfade';
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
  let autoVjParamPhase = 0;
  const deckAProgRef: { current: WebGLProgram | null } = { current: null };
  const deckBProgRef: { current: WebGLProgram | null } = { current: null };
  let mixProgram: WebGLProgram | null = null;
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
  wrap.style.cssText = `
    display: flex;
    flex-direction: column;
    gap: 6px;
    padding: 6px;
    background: var(--amiga-bg);
    color: var(--crt-fg);
    font-family: inherit;
    font-size: 10px;
    width: 100%;
    box-sizing: border-box;
  `;

  const decksRow = document.createElement('div');
  decksRow.className = 'vj-decks-row';
  decksRow.style.cssText = `
    display: flex;
    gap: 8px;
    justify-content: center;
    flex-wrap: wrap;
  `;

  function buildDeckUI(
    label: string,
    onSelect: (path: string) => void,
    valuesRef: Record<string, number | boolean>,
    deckKey: 'A' | 'B',
    oscAddressesRef: Record<string, string>,
    onOscAddressChange: () => void
  ): { el: HTMLElement; canvas: HTMLCanvasElement; setDisplayPath: (path: string) => void; updateParams: (meta: ExposeItem[]) => void } {
    const deck = document.createElement('div');
    deck.className = 'vj-deck panel-section';
    deck.title = 'Drag a shader from the list and drop here';

    const searchWrap = document.createElement('div');
    searchWrap.style.cssText = 'position: relative;';

    const head = document.createElement('div');
    head.className = 'panel-section-head';
    head.textContent = label;

    const input = document.createElement('input');
    input.type = 'text';
    input.placeholder = 'Search...';
    input.autocomplete = 'off';
    input.style.cssText = 'width: 100%; padding: 2px 6px; font-size: 10px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); font-family: inherit; box-sizing: border-box;';

    const dropdown = document.createElement('div');
    dropdown.style.cssText = `
      display: none;
      position: absolute;
      left: 0;
      right: 0;
      top: 100%;
      margin-top: 2px;
      max-height: 200px;
      overflow-y: auto;
      background: var(--amiga-bg);
      border: 1px solid var(--bevel-dark);
      z-index: 100;
      box-shadow: 0 4px 12px rgba(0,0,0,0.4);
    `;

    function getDisplayName(e: IndexEntry): string {
      return (e.fixedName ?? e.name ?? e.path ?? '').slice(0, 48);
    }

    function getDisplayNameWithIndex(e: IndexEntry): string {
      const idx = shaderList.indexOf(e) + 1;
      const name = getDisplayName(e);
      return name ? idx + '. ' + name : String(idx);
    }

    function renderFiltered(): void {
      const query = input.value.trim();
      const filtered = query
        ? shaderList.filter((e) => {
            if (fuzzyMatch(query, getDisplayName(e)) || fuzzyMatch(query, (e.path ?? ''))) return true;
            const tags = e.tags || [];
            if (tags.length && (fuzzyMatch(query, tags.join(' ')) || tags.some((t) => fuzzyMatch(query, t)))) return true;
            return false;
          })
        : shaderList.slice(0, 80);
      dropdown.innerHTML = '';
      if (filtered.length === 0) {
        const empty = document.createElement('div');
        empty.style.cssText = 'padding: 6px 8px; font-size: 10px; color: var(--crt-dim);';
        empty.textContent = 'No matches';
        dropdown.appendChild(empty);
      } else {
        for (const e of filtered) {
          const row = document.createElement('div');
          row.style.cssText = `
            padding: 6px 8px;
            font-size: 11px;
            cursor: pointer;
            border-bottom: 1px solid var(--bevel-dark);
          `;
          row.textContent = getDisplayNameWithIndex(e);
          row.title = e.path ?? '';
          row.addEventListener('mouseenter', () => { row.style.background = 'var(--amiga-surface)'; });
          row.addEventListener('mouseleave', () => { row.style.background = ''; });
          row.addEventListener('click', () => {
            const path = e.path ?? '';
            input.value = path ? getDisplayNameWithIndex(e) : '';
            dropdown.style.display = 'none';
            input.blur();
            onSelect(path);
          });
          dropdown.appendChild(row);
        }
      }
    }

    input.addEventListener('focus', () => {
      renderFiltered();
      dropdown.style.display = 'block';
    });
    input.addEventListener('input', () => renderFiltered());
    input.addEventListener('keydown', (ev) => {
      if (ev.key === 'Escape') {
        dropdown.style.display = 'none';
        input.blur();
      }
    });

    function closeDropdown(): void {
      dropdown.style.display = 'none';
    }
    deck.addEventListener('focusin', (ev) => {
      if (dropdown.contains(ev.target as Node) || input.contains(ev.target as Node)) return;
      closeDropdown();
    });
    deck.addEventListener('focusout', (ev) => {
      const next = ev.relatedTarget as Node | null;
      if (next && (dropdown.contains(next) || input.contains(next))) return;
      setTimeout(closeDropdown, 120);
    });

    function setDisplayPath(path: string): void {
      if (!path) {
        input.value = '';
        return;
      }
      const entry = shaderList.find((e) => (e.path ?? '') === path);
      input.value = entry ? getDisplayNameWithIndex(entry) : path.slice(0, 48);
    }

    searchWrap.appendChild(input);
    searchWrap.appendChild(dropdown);

    deck.appendChild(head);
    deck.appendChild(searchWrap);

    const canvas = document.createElement('canvas');
    canvas.width = DECK_CANVAS_W;
    canvas.height = DECK_CANVAS_H;
    canvas.style.cssText = `
      width: 100%;
      max-width: ${DECK_CANVAS_W}px;
      height: auto;
      aspect-ratio: ${DECK_CANVAS_W} / ${DECK_CANVAS_H};
      background: var(--amiga-bg);
      border: 1px solid var(--bevel-dark);
      display: block;
    `;

    deck.appendChild(canvas);

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

    const paramsContainer = document.createElement('div');
    paramsContainer.style.cssText = 'display:flex;flex-direction:column;gap:2px;max-height:120px;overflow-y:auto;';
    deck.appendChild(paramsContainer);

    const prefix = '/vj/' + deckKey.toLowerCase() + '/';
    function updateParams(meta: ExposeItem[]): void {
      paramsContainer.innerHTML = '';
      for (const p of meta) {
        const row = document.createElement('div');
        row.style.cssText = 'display:flex;align-items:center;gap:2px;font-size:9px;';
        const lab = document.createElement('span');
        lab.style.cssText = 'min-width:36px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;color:var(--crt-dim);';
        lab.textContent = p.name;
        lab.title = p.name;
        row.appendChild(lab);
        if (p.type === 'bool') {
          const cb = document.createElement('input');
          cb.type = 'checkbox';
          cb.checked = !!(valuesRef[p.name] as boolean);
          cb.style.cssText = 'flex-shrink:0;';
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
          sl.style.cssText = 'flex:1;min-width:36px;accent-color:var(--amiga-accent);';
          sl.addEventListener('input', () => {
            valuesRef[p.name] = parseFloat(sl.value);
          });
          row.appendChild(sl);
        }
        const oscInp = document.createElement('input');
        oscInp.type = 'text';
        oscInp.value = oscAddressesRef[p.name] || prefix + p.name;
        oscInp.title = 'OSC address (0-1); clear for default';
        oscInp.style.cssText = 'width:72px;flex-shrink:0;padding:1px 4px;font-size:8px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);font-family:inherit;';
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

    return { el: deck, canvas, setDisplayPath, updateParams };
  }

  const deckA = buildDeckUI('Deck A', (path) => {
    deckAEntry.value = shaderList.find((e) => (e.path ?? '') === path) ?? null;
    loadShaderForDeck(path, deckAProgRef, glA, deckAEntry, deckAParamValues, (meta) => {
      deckAMeta = meta;
      deckA.updateParams(meta);
      rebuildFftUI();
      buildVjOscMaps();
    }, 'A');
    deckA.setDisplayPath(path);
  }, deckAParamValues, 'A', deckAOscAddresses, () => { saveVjOscAddresses(); buildVjOscMaps(); });
  const deckB = buildDeckUI('Deck B', (path) => {
    deckBEntry.value = shaderList.find((e) => (e.path ?? '') === path) ?? null;
    loadShaderForDeck(path, deckBProgRef, glB, deckBEntry, deckBParamValues, (meta) => {
      deckBMeta = meta;
      deckB.updateParams(meta);
      rebuildFftUI();
      buildVjOscMaps();
    }, 'B');
    deckB.setDisplayPath(path);
  }, deckBParamValues, 'B', deckBOscAddresses, () => { saveVjOscAddresses(); buildVjOscMaps(); });

  decksRow.appendChild(deckA.el);
  decksRow.appendChild(deckB.el);

  const controlsRow = document.createElement('div');
  controlsRow.style.cssText = `
    display: flex;
    flex-direction: column;
    gap: 4px;
    background: var(--amiga-surface);
    border: 1px solid var(--bevel-dark);
    padding: 4px 6px;
    border-radius: 3px;
  `;

  const crossfaderRow = document.createElement('div');
  crossfaderRow.className = 'vj-crossfader-row';
  crossfaderRow.style.cssText = 'display: flex; align-items: center; gap: 6px;';
  const crossfaderLabel = document.createElement('label');
  crossfaderLabel.textContent = 'Xfade';
  crossfaderLabel.style.cssText = 'color: var(--crt-dim); min-width: 36px; font-size: 9px;';
  const crossfaderInput = document.createElement('input');
  crossfaderInput.type = 'range';
  crossfaderInput.className = 'vj-crossfader';
  crossfaderInput.min = '0';
  crossfaderInput.max = '1';
  crossfaderInput.step = '0.01';
  crossfaderInput.value = '0';
  crossfaderInput.style.cssText = 'flex: 1; min-width: 80px; accent-color: var(--amiga-accent);';
  const crossfaderVal = document.createElement('span');
  crossfaderVal.style.cssText = 'color: var(--crt-dim); font-size: 9px; min-width: 28px;';
  crossfaderVal.textContent = '0%';
  crossfaderRow.appendChild(crossfaderLabel);
  crossfaderRow.appendChild(crossfaderInput);
  crossfaderRow.appendChild(crossfaderVal);

  const mixModeRow = document.createElement('div');
  mixModeRow.style.cssText = 'display: flex; align-items: center; gap: 6px;';
  const mixModeLabel = document.createElement('label');
  mixModeLabel.textContent = 'Mix';
  mixModeLabel.style.cssText = 'color: var(--crt-dim); min-width: 36px; font-size: 9px;';
  const mixModeSelect = document.createElement('select');
  mixModeSelect.style.cssText = 'padding: 2px 6px; font-size: 9px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); font-family: inherit; cursor: pointer;';
  for (const m of MIX_MODES) {
    const opt = document.createElement('option');
    opt.value = m.value;
    opt.textContent = m.label;
    mixModeSelect.appendChild(opt);
  }
  mixModeRow.appendChild(mixModeLabel);
  mixModeRow.appendChild(mixModeSelect);

  const textureRow = document.createElement('div');
  textureRow.style.cssText = 'display: flex; align-items: center; gap: 6px; flex-wrap: wrap;';
  const textureLabel = document.createElement('label');
  textureLabel.textContent = 'Texture';
  textureLabel.style.cssText = 'color: var(--crt-dim); min-width: 48px; font-size: 9px;';
  const textureStatus = document.createElement('span');
  textureStatus.style.cssText = 'font-size: 9px; color: var(--crt-dim);';
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
    textureBtns.forEach((b) => { b.el.style.fontWeight = b.value === s ? 'bold' : 'normal'; });
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
          textureBtns.forEach((b) => { b.el.style.fontWeight = b.value === 'smpte' ? 'bold' : 'normal'; });
        });
    }
  }
  textureRow.appendChild(textureLabel);
  for (const val of ['none', 'webcam', 'smpte', 'test1', 'test2', 'test3'] as VjTextureSource[]) {
    const btn = document.createElement('button');
    btn.type = 'button';
    btn.textContent = val === 'none' ? 'None' : val === 'test1' ? 'Test 1' : val === 'test2' ? 'Test 2' : val === 'test3' ? 'Test 3' : val;
    btn.style.cssText = 'font-size: 9px; padding: 2px 6px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); cursor: pointer;';
    if (val === vjTextureSource) btn.style.fontWeight = 'bold';
    btn.addEventListener('click', () => setVjTextureSource(val));
    textureBtns.push({ value: val, el: btn });
    textureRow.appendChild(btn);
  }
  textureRow.appendChild(textureStatus);

  controlsRow.appendChild(crossfaderRow);
  controlsRow.appendChild(mixModeRow);
  controlsRow.appendChild(textureRow);

  const autoVjSection = document.createElement('div');
  autoVjSection.className = 'panel-section';
  const autoVjHead = document.createElement('div');
  autoVjHead.className = 'panel-section-head';
  autoVjHead.textContent = 'Auto VJ';
  autoVjSection.appendChild(autoVjHead);
  const autoVjRow = document.createElement('div');
  autoVjRow.style.cssText = 'display: flex; align-items: center; gap: 8px; flex-wrap: wrap;';
  const autoVjToggleBtn = document.createElement('button');
  autoVjToggleBtn.type = 'button';
  autoVjToggleBtn.textContent = 'Off';
  autoVjToggleBtn.title = 'Auto-pilot: cycle shaders, crossfade, automate params, FFT. Toggle via click, MIDI CC15, or OSC /vj/autoVj';
  autoVjToggleBtn.style.cssText = 'font-size: 9px; padding: 4px 10px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); cursor: pointer;';
  const autoVjBpmLabel = document.createElement('label');
  autoVjBpmLabel.style.cssText = 'font-size: 9px; color: var(--crt-dim);';
  autoVjBpmLabel.textContent = 'BPM';
  const autoVjBpmInput = document.createElement('input');
  autoVjBpmInput.type = 'number';
  autoVjBpmInput.min = '30';
  autoVjBpmInput.max = '240';
  autoVjBpmInput.value = '120';
  autoVjBpmInput.style.cssText = 'width: 48px; font-size: 9px; padding: 2px 4px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); font-family: inherit;';
  const autoVjTapBtn = document.createElement('button');
  autoVjTapBtn.type = 'button';
  autoVjTapBtn.textContent = 'Tap';
  autoVjTapBtn.title = 'Tap to set BPM from tempo';
  autoVjTapBtn.style.cssText = 'font-size: 9px; padding: 2px 8px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); cursor: pointer;';
  autoVjRow.appendChild(autoVjToggleBtn);
  autoVjRow.appendChild(autoVjBpmLabel);
  autoVjRow.appendChild(autoVjBpmInput);
  autoVjRow.appendChild(autoVjTapBtn);
  autoVjSection.appendChild(autoVjRow);

  // --- MIDI section ---
  const midiSection = document.createElement('div');
  midiSection.className = 'panel-section';
  const midiHead = document.createElement('div');
  midiHead.className = 'panel-section-head';
  midiHead.style.cssText = 'display: flex; align-items: center; gap: 8px;';
  midiHead.textContent = 'MIDI';
  const midiStatusDot = document.createElement('span');
  midiStatusDot.style.cssText = 'width: 6px; height: 6px; border-radius: 50%; background: #aa3333; display: inline-block;';
  midiHead.appendChild(midiStatusDot);
  midiSection.appendChild(midiHead);

  const midiControlRow = document.createElement('div');
  midiControlRow.style.cssText = 'display: flex; align-items: center; gap: 4px; flex-wrap: wrap;';
  const midiEnableBtn = document.createElement('button');
  midiEnableBtn.type = 'button';
  midiEnableBtn.textContent = 'Enable';
  midiEnableBtn.style.cssText = 'font-size: 9px; padding: 2px 8px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); cursor: pointer;';
  const midiTemplateSelect = document.createElement('select');
  midiTemplateSelect.style.cssText = 'font-size: 9px; padding: 1px 4px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); font-family: inherit;';
  midiTemplateSelect.innerHTML = '<option value="apc40_mk2">APC40 MK2 / Akai</option><option value="custom">Custom</option><option value="none">None</option>';
  midiTemplateSelect.value = midiEngine.vjTemplate;
  const midiDeviceSelect = document.createElement('select');
  midiDeviceSelect.style.cssText = 'font-size: 9px; padding: 1px 4px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); font-family: inherit; max-width: 120px;';
  midiDeviceSelect.innerHTML = '<option value="">All devices</option>';
  const midiLastLabel = document.createElement('span');
  midiLastLabel.id = 'midiLastReceivedVJ';
  midiLastLabel.style.cssText = 'font-size: 8px; color: var(--crt-dim); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; max-width: 140px;';
  midiControlRow.appendChild(midiEnableBtn);
  midiControlRow.appendChild(midiTemplateSelect);
  midiControlRow.appendChild(midiDeviceSelect);
  midiControlRow.appendChild(midiLastLabel);
  midiSection.appendChild(midiControlRow);

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
        label: 'Deck A',
        actions: Array.from({ length: 8 }, (_, i) => ({
          id: 'vj/deckA/param/' + i,
          name: 'A' + i + (deckAMeta[i] ? ' ' + deckAMeta[i].name.slice(0, 6) : '')
        }))
      },
      {
        label: 'Deck B',
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
    midiEnableBtn.style.color = midiEngine.active ? '#22cc44' : 'var(--crt-fg)';
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
  oscSection.className = 'panel-section';
  const oscHead = document.createElement('div');
  oscHead.className = 'panel-section-head';
  oscHead.style.cssText = 'display: flex; align-items: center; gap: 8px;';
  oscHead.textContent = 'OSC';
  const oscStatusDot = document.createElement('span');
  oscStatusDot.style.cssText = 'width: 6px; height: 6px; border-radius: 50%; background: #aa3333; display: inline-block;';
  oscHead.appendChild(oscStatusDot);
  oscSection.appendChild(oscHead);

  const oscControlRow = document.createElement('div');
  oscControlRow.style.cssText = 'display: flex; align-items: center; gap: 4px; flex-wrap: wrap;';
  const oscToggleBtn = document.createElement('button');
  oscToggleBtn.type = 'button';
  oscToggleBtn.textContent = oscEngine.active ? 'Stop' : 'Listen';
  oscToggleBtn.style.cssText = 'font-size: 9px; padding: 2px 8px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); cursor: pointer;';
  const oscPortInput = document.createElement('input');
  oscPortInput.type = 'number';
  oscPortInput.value = String(oscEngine.port || 9000);
  oscPortInput.style.cssText = 'width: 50px; font-size: 9px; padding: 1px 4px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); font-family: inherit;';
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
    oscToggleBtn.style.color = oscEngine.active ? '#22cc44' : 'var(--crt-fg)';
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
  audioSection.className = 'panel-section';
  const audioHead = document.createElement('div');
  audioHead.className = 'panel-section-head';
  audioHead.style.cssText = 'display: flex; align-items: center; gap: 8px;';
  audioHead.textContent = 'Audio FFT';
  const audioStatusDot = document.createElement('span');
  audioStatusDot.style.cssText = 'width: 6px; height: 6px; border-radius: 50%; background: #aa3333; display: inline-block;';
  audioHead.appendChild(audioStatusDot);
  audioSection.appendChild(audioHead);

  const audioControlRow = document.createElement('div');
  audioControlRow.style.cssText = 'display: flex; align-items: center; gap: 4px; flex-wrap: wrap;';
  const audioToggleBtn = document.createElement('button');
  audioToggleBtn.type = 'button';
  audioToggleBtn.textContent = audioEngine.active ? 'Stop' : 'Start';
  audioToggleBtn.style.cssText = 'font-size: 9px; padding: 2px 8px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); cursor: pointer;';
  const audioGainLabel = document.createElement('span');
  audioGainLabel.style.cssText = 'font-size: 9px; color: var(--crt-dim);';
  audioGainLabel.textContent = 'Gain';
  const audioGainSlider = document.createElement('input');
  audioGainSlider.type = 'range';
  audioGainSlider.min = '0';
  audioGainSlider.max = '3';
  audioGainSlider.step = '0.1';
  audioGainSlider.value = String(audioEngine.gain);
  audioGainSlider.style.cssText = 'width: 60px; accent-color: var(--amiga-accent);';
  const audioGainVal = document.createElement('span');
  audioGainVal.style.cssText = 'font-size: 9px; color: var(--crt-dim); min-width: 24px;';
  audioGainVal.textContent = audioEngine.gain.toFixed(1);
  audioControlRow.appendChild(audioToggleBtn);
  audioControlRow.appendChild(audioGainLabel);
  audioControlRow.appendChild(audioGainSlider);
  audioControlRow.appendChild(audioGainVal);
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
    audioToggleBtn.style.color = audioEngine.active ? '#22cc44' : 'var(--crt-fg)';
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
  const fftHead = document.createElement('div');
  fftHead.style.cssText = 'font-size: 9px; color: var(--crt-dim); margin-bottom: 2px;';
  fftHead.textContent = 'Band Mapping (both decks)';
  fftSection.appendChild(fftHead);
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
    autoA.textContent = 'Auto';
    autoA.style.cssText = 'font-size: 9px; padding: 2px 6px; margin-bottom: 4px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); cursor: pointer;';
    autoA.title = 'Assign bands 0-' + (FFT_BAND_COUNT - 1) + ' to first params in order';
    autoA.onclick = () => {
      if (deckAMeta.length === 0) {
        import('../dom.js').then(({ status }) => status('Load a shader in Deck A first, then click Auto to assign FFT bands.', true));
        return;
      }
      for (let b = 0; b < FFT_BAND_COUNT; b++) {
        if (b < deckAMeta.length) deckAFftMap[b] = deckAMeta[b].name;
        else delete deckAFftMap[b];
      }
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
      sel.style.cssText = 'flex: 1; font-size: 9px; padding: 1px 4px; background: var(--amiga-bg); border: 1px solid var(--bevel-dark); color: var(--crt-fg); font-family: inherit;';
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
    autoB.textContent = 'Auto';
    autoB.style.cssText = 'font-size: 9px; padding: 2px 6px; margin-bottom: 4px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); cursor: pointer;';
    autoB.title = 'Assign bands 0-' + (FFT_BAND_COUNT - 1) + ' to first params in order';
    autoB.onclick = () => {
      if (deckBMeta.length === 0) {
        import('../dom.js').then(({ status }) => status('Load a shader in Deck B first, then click Auto to assign FFT bands.', true));
        return;
      }
      for (let b = 0; b < FFT_BAND_COUNT; b++) {
        if (b < deckBMeta.length) deckBFftMap[b] = deckBMeta[b].name;
        else delete deckBFftMap[b];
      }
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
      sel.style.cssText = 'flex: 1; font-size: 9px; padding: 1px 4px; background: var(--amiga-bg); border: 1px solid var(--bevel-dark); color: var(--crt-fg); font-family: inherit;';
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
  outputSection.style.cssText = `display: flex; flex-direction: column; gap: 6px;`;
  const outputHead = document.createElement('div');
  outputHead.style.cssText = `display: flex; align-items: center; justify-content: space-between; gap: 8px;`;
  const outputLabel = document.createElement('div');
  outputLabel.style.cssText = 'font-size: 9px; text-transform: uppercase; color: var(--amiga-copper); letter-spacing: 0.08em;';
  outputLabel.textContent = 'VJ Preview';
  const popOutBtn = document.createElement('button');
  popOutBtn.type = 'button';
  popOutBtn.textContent = 'Pop out';
  popOutBtn.title = 'Open output on another screen; reacts to MIDI/OSC/FFT';
  popOutBtn.style.cssText = 'font-size: 9px; padding: 2px 8px; background: var(--amiga-surface); color: var(--amiga-copper); border: 1px solid var(--bevel-dark); cursor: pointer;';
  const audienceQrBtn = document.createElement('button');
  audienceQrBtn.type = 'button';
  audienceQrBtn.textContent = 'Audience QR';
  audienceQrBtn.title = 'Show or hide join QR on the VJ output (preview, pop-out, HDMI)';
  audienceQrBtn.style.cssText = popOutBtn.style.cssText;
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
  outputHead.appendChild(audienceQrBtn);
  outputHead.appendChild(popOutBtn);
  outputSection.appendChild(outputHead);

  const transformSection = document.createElement('div');
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
  rotSelect.style.cssText = 'padding: 2px 4px; font-size: 9px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); font-family: inherit;';
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
  outputSection.appendChild(transformSection);

  const previewAndQrRow = document.createElement('div');
  previewAndQrRow.style.cssText =
    'display:flex;flex-wrap:wrap;gap:12px;align-items:flex-start;';

  const previewCol = document.createElement('div');
  previewCol.style.cssText = 'flex:1 1 280px;min-width:0;position:relative;';

  const outputCanvas = document.createElement('canvas');
  outputCanvas.width = OUTPUT_CANVAS_W;
  outputCanvas.height = OUTPUT_CANVAS_H;
  outputCanvas.style.cssText = `
    width: 100%;
    max-width: ${OUTPUT_CANVAS_W}px;
    height: auto;
    aspect-ratio: ${OUTPUT_CANVAS_W} / ${OUTPUT_CANVAS_H};
    background: var(--amiga-bg);
    border: 1px solid var(--bevel-dark);
    display: block;
  `;

  const outputQrOverlay = document.createElement('canvas');
  outputQrOverlay.width = OUTPUT_CANVAS_W;
  outputQrOverlay.height = OUTPUT_CANVAS_H;
  outputQrOverlay.style.cssText = `
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    max-width: ${OUTPUT_CANVAS_W}px;
    height: auto;
    aspect-ratio: ${OUTPUT_CANVAS_W} / ${OUTPUT_CANVAS_H};
    pointer-events: none;
    visibility: hidden;
  `;

  previewCol.appendChild(outputCanvas);
  previewCol.appendChild(outputQrOverlay);

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
  syncOutputQrOverlayPointer();

  const gigQrBlock = createVjPreviewGigQrBlock();
  previewAndQrRow.appendChild(previewCol);
  previewAndQrRow.appendChild(gigQrBlock.root);
  outputSection.appendChild(previewAndQrRow);
  window.addEventListener('macroverse-vj-session-changed', () => {
    void ensureVjTokens(getVjSessionId()).then(() => {
      gigQrBlock.refresh();
      refreshGigOutputQrSession();
    });
  });

  vjOutputCanvasRef = outputCanvas;
  outputCanvas.style.cursor = 'crosshair';
  outputCanvas.addEventListener('mousemove', (e) => {
    const rect = outputCanvas.getBoundingClientRect();
    _vjMouseXRef = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
    _vjMouseYRef = Math.max(0, Math.min(1, 1 - (e.clientY - rect.top) / rect.height));
  });
  outputCanvas.addEventListener('mouseleave', () => {
    _vjMouseXRef = 0.5;
    _vjMouseYRef = 0.5;
  });

  const outputUrlRow = document.createElement('div');
  outputUrlRow.style.cssText = 'display: flex; align-items: center; gap: 6px; flex-wrap: wrap; font-size: 9px;';
  const outputUrlLabel = document.createElement('span');
  outputUrlLabel.style.cssText = 'color: var(--crt-dim); white-space: nowrap;';
  outputUrlLabel.textContent = 'URL (pop-out / OBS Browser Source):';
  const outputUrlInput = document.createElement('input');
  outputUrlInput.type = 'text';
  outputUrlInput.readOnly = true;
  const vjOutputUrl = typeof window !== 'undefined'
    ? `${window.location.origin}/vj-output.html?remote=1&${vjViewQuery()}`
    : '';
  outputUrlInput.value = vjOutputUrl;
  outputUrlInput.title = 'Pi HDMI / OBS: open this URL on the projector machine for this gig session';
  outputUrlInput.style.cssText = 'flex: 1; min-width: 120px; font-size: 9px; padding: 2px 6px; background: var(--amiga-bg); color: var(--crt-fg); border: 1px solid var(--bevel-dark); font-family: inherit;';
  const outputUrlCopy = document.createElement('button');
  outputUrlCopy.type = 'button';
  outputUrlCopy.textContent = 'Copy';
  outputUrlCopy.style.cssText = 'font-size: 9px; padding: 2px 8px; background: var(--amiga-surface); color: var(--amiga-copper); border: 1px solid var(--bevel-dark); cursor: pointer;';
  outputUrlRow.appendChild(outputUrlLabel);
  outputUrlRow.appendChild(outputUrlInput);
  outputUrlRow.appendChild(outputUrlCopy);
  outputSection.appendChild(outputUrlRow);
  outputUrlCopy.addEventListener('click', () => {
    outputUrlInput.select();
    navigator.clipboard.writeText(outputUrlInput.value).then(() => {
      const t = outputUrlCopy.textContent;
      outputUrlCopy.textContent = 'Copied';
      setTimeout(() => { outputUrlCopy.textContent = t; }, 1500);
    }).catch(() => {});
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
  wrap.appendChild(decksRow);
  wrap.appendChild(controlsRow);
  wrap.appendChild(autoVjSection);
  wrap.appendChild(midiSection);
  wrap.appendChild(oscSection);
  wrap.appendChild(audioSection);
  wrap.appendChild(outputSection);
  container.appendChild(wrap);
  vjDeckInitialized = true;

  const glOpts: WebGLContextAttributes = {
    preserveDrawingBuffer: true,
    premultipliedAlpha: false,
    powerPreference: 'default'
  };

  const glA = deckA.canvas.getContext('webgl', glOpts);
  const glB = deckB.canvas.getContext('webgl', glOpts);
  const glMix = outputCanvas.getContext('webgl', glOpts);

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

    let mx: number, my: number;
    if (_perDeckMouseEnabled && deckId === 'A') { mx = _vjMouseAX; my = _vjMouseAY; }
    else if (_perDeckMouseEnabled && deckId === 'B') { mx = _vjMouseBX; my = _vjMouseBY; }
    else { mx = _vjMouseXRef; my = _vjMouseYRef; }
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
    if (mixModeLoc) glMix.uniform1i(mixModeLoc, MIX_MODES.find((m) => m.value === mixMode)!.modeInt);
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
  function runVJFrame(): void {
    if (!vjDeckTabActive) {
      if (rafMix) {
        cancelAnimationFrame(rafMix);
        rafMix = 0;
      }
      return;
    }
    const now = performance.now();
    const dt = lastVjFrameTime ? (now - lastVjFrameTime) / 1000 : 0.016;
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
    drawGigOutputQrOnCanvas(outputQrOverlay);

    // Multi-device Roliblock LED dispatch
    if (outputCanvas) {
      const mgr = roliblockManager;
      if (mgr.ledDisplayMode === 'stretched' || mgr.ledDisplayMode === 'linked') {
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
      while (now - autoVjLastBeatTime >= beatMs) {
        autoVjLastBeatTime += beatMs;
        autoVjBeatCount++;
        if (autoVjBeatCount % 4 === 0) {
          autoVjCrossfadeStart = autoVjLastBeatTime;
          autoVjCrossfadeStartVal = crossfader;
          autoVjCrossfadeTarget = autoVjCrossfadeTarget > 0.5 ? 0 : 1;
          const list = getShaderList();
          if (list.length > 0) {
            loadDeckAByGlobalIndex(Math.floor(Math.random() * list.length));
            loadDeckBByGlobalIndex(Math.floor(Math.random() * list.length));
          }
        }
      }
      const crossfadeDurationMs = 4 * beatMs;
      const crossfadeElapsed = now - autoVjCrossfadeStart;
      if (crossfadeElapsed < crossfadeDurationMs && crossfadeElapsed >= 0) {
        const t = Math.min(1, crossfadeElapsed / crossfadeDurationMs);
        const smooth = 0.5 - 0.5 * Math.cos(t * Math.PI);
        crossfader = autoVjCrossfadeStartVal + (autoVjCrossfadeTarget - autoVjCrossfadeStartVal) * smooth;
        crossfaderInput.value = String(crossfader);
        crossfaderVal.textContent = Math.round(crossfader * 100) + '%';
      }
      autoVjParamPhase += dt * (autoVjBpm / 60) * Math.PI * 0.5;
      for (let i = 0; i < deckAMeta.length; i++) {
        const m = deckAMeta[i];
        if (m.type !== 'float' && m.type !== 'int') continue;
        const range = (m.max ?? 1) - (m.min ?? 0);
        const center = (m.min ?? 0) + range * 0.5;
        const wave = 0.5 + 0.4 * Math.sin(autoVjParamPhase + i * 0.7);
        const v = center + (wave - 0.5) * range;
        deckAParamValues[m.name] = Math.max(m.min ?? 0, Math.min(m.max ?? 1, v));
      }
      for (let i = 0; i < deckBMeta.length; i++) {
        const m = deckBMeta[i];
        if (m.type !== 'float' && m.type !== 'int') continue;
        const range = (m.max ?? 1) - (m.min ?? 0);
        const center = (m.min ?? 0) + range * 0.5;
        const wave = 0.5 + 0.4 * Math.sin(autoVjParamPhase + 0.5 + i * 0.7);
        const v = center + (wave - 0.5) * range;
        deckBParamValues[m.name] = Math.max(m.min ?? 0, Math.min(m.max ?? 1, v));
      }
    }

    if (now - lastBroadcast > 33) {
      lastBroadcast = now;
      const frameData = {
        type: 'frame' as const,
        crossfader,
        mixModeInt: MIX_MODES.find((m) => m.value === mixMode)!.modeInt,
        flipV: outputFlipV,
        flipH: outputFlipH,
        rotation: outputRotation,
        mouseX: _vjMouseXRef,
        mouseY: _vjMouseYRef,
        paramsA: { ...deckAParamValues },
        paramsB: { ...deckBParamValues },
        qrOverlay: getGigOutputQrFramePayload() ?? {
          enabled: false,
          mix: 0,
          opacity: 0,
        },
      };
      const json = JSON.stringify(frameData);
      if (json !== lastFrameJson) {
        lastFrameJson = json;
        sendVJMessage(frameData);
      }
    }
    if (!document.hidden && vjDeckTabActive) rafMix = requestAnimationFrame(runVJFrame);
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
    if (rafMix || bgVjInterval) return;
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
      const entry = shaderList.find((e) => (e.path ?? '') === path) ?? null;
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
      console.warn('[VJ Deck] shader compile failed:', errMsg.slice(0, 200));
    }
  }

  crossfaderInput.addEventListener('input', () => {
    crossfader = parseFloat(crossfaderInput.value);
    crossfaderVal.textContent = Math.round(crossfader * 100) + '%';
    if (!applyingRemoteControl) publishVjControl({ crossfader });
  });

  mixModeSelect.addEventListener('change', () => {
    mixMode = mixModeSelect.value as MixMode;
    if (!applyingRemoteControl) publishVjControl({ mixMode });
  });

  function setAutoVjEnabled(en: boolean): void {
    autoVjEnabled = en;
    autoVjToggleBtn.textContent = en ? 'On' : 'Off';
    autoVjToggleBtn.style.background = en ? 'var(--amiga-copper)' : 'var(--amiga-bg)';
    if (en) {
      if (!audioEngine.active) {
        audioEngine.start().then(() => {
          updateAudioStatus();
        }).catch(() => {});
      }
      for (let b = 0; b < FFT_BAND_COUNT; b++) {
        if (b < deckAMeta.length) deckAFftMap[b] = deckAMeta[b].name;
        if (b < deckBMeta.length) deckBFftMap[b] = deckBMeta[b].name;
      }
      saveVjFftMaps();
      rebuildFftUI();
      autoVjLastBeatTime = performance.now();
      autoVjBeatCount = 0;
      autoVjCrossfadeStart = performance.now();
      autoVjCrossfadeStartVal = crossfader;
      autoVjCrossfadeTarget = crossfader > 0.5 ? 0 : 1;
    }
  }
  autoVjToggleBtn.addEventListener('click', () => setAutoVjEnabled(!autoVjEnabled));
  autoVjBpmInput.addEventListener('change', () => {
    const v = parseFloat(autoVjBpmInput.value);
    if (!Number.isNaN(v) && v >= 30 && v <= 240) autoVjBpm = v;
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
      }
    }
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
      if (autoVjEnabled) {
        for (let b = 0; b < FFT_BAND_COUNT; b++) if (b < meta.length) deckAFftMap[b] = meta[b].name;
        saveVjFftMaps();
      }
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
      if (autoVjEnabled) {
        for (let b = 0; b < FFT_BAND_COUNT; b++) if (b < meta.length) deckBFftMap[b] = meta[b].name;
        saveVjFftMaps();
      }
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
      if (autoVjEnabled) {
        for (let b = 0; b < FFT_BAND_COUNT; b++) if (b < meta.length) deckAFftMap[b] = meta[b].name;
        saveVjFftMaps();
      }
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
      if (autoVjEnabled) {
        for (let b = 0; b < FFT_BAND_COUNT; b++) if (b < meta.length) deckBFftMap[b] = meta[b].name;
        saveVjFftMaps();
      }
      rebuildFftUI();
      buildVjOscMaps();
    }, 'B');
    deckB.setDisplayPath(entry.path);
  }

  vjController.register('vj/crossfader', (v) => {
    crossfader = v;
    crossfaderInput.value = String(v);
    crossfaderVal.textContent = Math.round(v * 100) + '%';
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
        crossfaderVal.textContent = Math.round(crossfader * 100) + '%';
      }
      if (ctrl.mixMode && MIX_MODES.some((m) => m.value === ctrl.mixMode)) {
        mixMode = ctrl.mixMode as MixMode;
        mixModeSelect.value = mixMode;
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
    } finally {
      applyingRemoteControl = false;
    }
  });

  if (vjDeckTabActive) runVJFrame();
}
