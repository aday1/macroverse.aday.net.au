// VJ Output - standalone pop-out renderer
import {
  applyGigOutputQrFramePayload,
  ensureGigOutputQrLoop,
  isGigStreamQrActive,
  isGigStreamQrVisible,
  isStreamLinkViewerPage,
  registerGigOutputQrCanvas,
  type GigOutputQrFrame,
} from './gigOutputQr.js';
import {
  audienceMouseQuery,
  fetchAudienceParticipationConfig,
} from './vjAudienceParticipation.js';
import { getViewTokenFromUrl, vjViewQuery } from './vjTokens.js';
import { VJ_MIX_FRAG_SRC } from './vjMixShader.js';
// Receives state from main window via BroadcastChannel and renders independently.
// This page has its own WebGL contexts and animation loop so it keeps rendering
// even when the main browser tab is in the background.

const VERT_SRC = `precision highp float;
attribute vec2 a_pos;
varying vec2 v_uv;
void main() {
  vec2 uv = a_pos * 0.5 + 0.5;
  v_uv = uv;
  gl_Position = vec4(a_pos, 0.0, 1.0);
}`;

const QUAD = new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]);
const DECK_W = 640;
const DECK_H = 360;

interface ParamMeta { name: string; type: string }
interface ShaderMsg { type: 'shader'; deck: 'A' | 'B'; preparedSource: string; meta: ParamMeta[] }
interface FrameMsg {
  type: 'frame';
  crossfader: number;
  mixModeInt: number;
  flipV: boolean;
  flipH: boolean;
  rotation: number;
  mouseX: number;
  mouseY: number;
  mouseAX?: number;
  mouseAY?: number;
  mouseBX?: number;
  mouseBY?: number;
  paramsA: Record<string, number | boolean>;
  paramsB: Record<string, number | boolean>;
  qrOverlay?: GigOutputQrFrame;
}
interface ClearMsg { type: 'clear'; deck: 'A' | 'B' }
interface QrOverlayMsg { type: 'qr-overlay'; qrOverlay: GigOutputQrFrame | null }
type VJMsg = ShaderMsg | FrameMsg | ClearMsg | QrOverlayMsg;

// ---- WebGL helpers ----

function compile(gl: WebGLRenderingContext, type: number, src: string): WebGLShader {
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

function link(gl: WebGLRenderingContext, vSrc: string, fSrc: string): WebGLProgram {
  const v = compile(gl, gl.VERTEX_SHADER, vSrc);
  const f = compile(gl, gl.FRAGMENT_SHADER, fSrc);
  const p = gl.createProgram()!;
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

function setDeckUniforms(
  gl: WebGLRenderingContext, prog: WebGLProgram,
  w: number, h: number, time: number,
  overrides: Record<string, number | boolean>,
  meta: ParamMeta[],
  mx: number,
  my: number
): void {
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
  for (const p of meta) {
    if (overrides[p.name] === undefined) continue;
    const loc = gl.getUniformLocation(prog, p.name);
    if (!loc) continue;
    if (p.type === 'bool') gl.uniform1i(loc, (overrides[p.name] as boolean) ? 1 : 0);
    else gl.uniform1f(loc, overrides[p.name] as number);
  }
  const aliases: [string, (l: WebGLUniformLocation) => void][] = [
    ['time', l => gl.uniform1f(l, time)],
    ['resolution', l => gl.uniform2f(l, w, h)],
    ['mouse', l => gl.uniform2f(l, mx, my)],
    ['iGlobalTime', l => gl.uniform1f(l, time)],
    ['iTime', l => gl.uniform1f(l, time)],
    ['iResolution', l => gl.uniform2f(l, w, h)],
    ['FRAMEINDEX', l => gl.uniform1f(l, Math.floor(time * 60))],
    ['timeScale', l => gl.uniform1f(l, 1.0)],
    ['mouseX', l => gl.uniform1f(l, mx)],
    ['mouseY', l => gl.uniform1f(l, my)],
  ];
  for (const [name, setter] of aliases) {
    const loc = gl.getUniformLocation(prog, name);
    if (loc) setter(loc);
  }
}

// ---- Setup ----

const glOpts: WebGLContextAttributes = {
  preserveDrawingBuffer: true,
  premultipliedAlpha: false,
  powerPreference: 'default'
};

const outputCanvas = document.getElementById('vjCanvas') as HTMLCanvasElement;
const qrOverlayCanvas = document.getElementById('vjQrOverlay') as HTMLCanvasElement | null;
const streamQrDismissBtn = document.getElementById('streamQrDismiss') as HTMLButtonElement | null;

const STREAM_QR_DISMISS_KEY = 'macroverse-stream-qr-dismiss';

function streamDismissStorageKey(): string {
  const viewToken = getViewTokenFromUrl();
  if (viewToken) return `${STREAM_QR_DISMISS_KEY}:${viewToken.slice(0, 48)}`;
  try {
    return `${STREAM_QR_DISMISS_KEY}:${window.location.pathname}${window.location.search}`;
  } catch {
    return STREAM_QR_DISMISS_KEY;
  }
}

let streamQrDismissed = false;

function loadStreamQrDismissed(): boolean {
  if (!isStreamLinkViewerPage()) return false;
  try {
    return sessionStorage.getItem(streamDismissStorageKey()) === '1';
  } catch {
    return false;
  }
}

function isStreamQrDismissed(): boolean {
  return streamQrDismissed;
}

function dismissStreamQr(): void {
  streamQrDismissed = true;
  try {
    sessionStorage.setItem(streamDismissStorageKey(), '1');
  } catch {
    /* ignore */
  }
  syncStreamQrDismissBtn();
  ensureGigOutputQrLoop();
  window.dispatchEvent(new CustomEvent('macroverse-stream-qr-dismissed'));
}

function syncStreamQrDismissBtn(): void {
  if (!streamQrDismissBtn || !isStreamLinkViewerPage()) return;
  const show =
    !streamQrDismissed &&
    (isGigStreamQrVisible() || isGigStreamQrActive());
  streamQrDismissBtn.hidden = !show;
}

streamQrDismissed = loadStreamQrDismissed();
if (qrOverlayCanvas) {
  registerGigOutputQrCanvas(qrOverlayCanvas, {
    surface: isStreamLinkViewerPage() ? 'stream' : 'output',
    isDismissed: isStreamLinkViewerPage() ? isStreamQrDismissed : undefined,
  });
}
if (streamQrDismissBtn) {
  streamQrDismissBtn.addEventListener('click', (e) => {
    e.preventDefault();
    e.stopPropagation();
    dismissStreamQr();
  });
}
window.addEventListener('macroverse-gig-stream-qr-visible', () => syncStreamQrDismissBtn());
window.addEventListener('macroverse-stream-qr-dismissed', () => {
  ensureGigOutputQrLoop();
});
syncStreamQrDismissBtn();
const statusEl = document.getElementById('status') as HTMLDivElement;
const audienceInteractHintEl = document.getElementById('audienceInteractHint') as HTMLDivElement | null;

const deckCanvasA = document.createElement('canvas');
deckCanvasA.width = DECK_W;
deckCanvasA.height = DECK_H;
const deckCanvasB = document.createElement('canvas');
deckCanvasB.width = DECK_W;
deckCanvasB.height = DECK_H;

const glA = deckCanvasA.getContext('webgl', glOpts);
const glB = deckCanvasB.getContext('webgl', glOpts);
const glMix = outputCanvas.getContext('webgl', glOpts);

if (!glA || !glB || !glMix) {
  statusEl.textContent = 'WebGL not available';
  throw new Error('WebGL not available');
}

for (const g of [glA, glB, glMix]) g.getExtension('OES_standard_derivatives');

function initBuf(gl: WebGLRenderingContext): WebGLBuffer {
  const b = gl.createBuffer()!;
  gl.bindBuffer(gl.ARRAY_BUFFER, b);
  gl.bufferData(gl.ARRAY_BUFFER, QUAD, gl.STATIC_DRAW);
  return b;
}

const bufA = initBuf(glA);
const bufB = initBuf(glB);
const bufMix = initBuf(glMix);

function createTex(gl: WebGLRenderingContext): WebGLTexture {
  const t = gl.createTexture()!;
  gl.bindTexture(gl.TEXTURE_2D, t);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
  return t;
}

const texA = createTex(glMix);
const texB = createTex(glMix);
const mixProg = link(glMix, VERT_SRC, VJ_MIX_FRAG_SRC);

// ---- State ----

let progA: WebGLProgram | null = null;
let progB: WebGLProgram | null = null;
let metaA: ParamMeta[] = [];
let metaB: ParamMeta[] = [];
let paramsA: Record<string, number | boolean> = {};
let paramsB: Record<string, number | boolean> = {};
let crossfader = 0;
let mixModeInt = 0;
let flipV = false;
let flipH = false;
let rotation = 0;
let mouseX = 0.5;
let mouseY = 0.5;
let mouseAX = 0.5;
let mouseAY = 0.5;
let mouseBX = 0.5;
let mouseBY = 0.5;

const startTime = performance.now();

// ---- Resize ----

function resize(): void {
  const dpr = window.devicePixelRatio || 1;
  const w = Math.round(window.innerWidth * dpr);
  const h = Math.round(window.innerHeight * dpr);
  if (outputCanvas.width !== w || outputCanvas.height !== h) {
    outputCanvas.width = w;
    outputCanvas.height = h;
  }
  if (qrOverlayCanvas && (qrOverlayCanvas.width !== w || qrOverlayCanvas.height !== h)) {
    qrOverlayCanvas.width = w;
    qrOverlayCanvas.height = h;
  }
}
window.addEventListener('resize', resize);
resize();

// ---- Message handling (shared by BroadcastChannel and EventSource) ----

function applyVJMsg(msg: VJMsg): void {
  if (!msg || !msg.type) return;
  if (msg.type === 'shader') {
    const gl = msg.deck === 'A' ? glA : glB;
    const oldProg = msg.deck === 'A' ? progA : progB;
    if (oldProg) gl.deleteProgram(oldProg);
    try {
      const p = link(gl, VERT_SRC, msg.preparedSource);
      if (msg.deck === 'A') { progA = p; metaA = msg.meta; }
      else { progB = p; metaB = msg.meta; }
      statusEl.style.opacity = '0';
    } catch (e) {
      if (msg.deck === 'A') progA = null;
      else progB = null;
      console.warn('[VJ Output] shader compile failed for deck ' + msg.deck + ':', e);
    }
  } else if (msg.type === 'frame') {
    crossfader = msg.crossfader;
    mixModeInt = msg.mixModeInt;
    flipV = msg.flipV;
    flipH = msg.flipH;
    rotation = msg.rotation;
    mouseX = msg.mouseX ?? 0.5;
    mouseY = msg.mouseY ?? 0.5;
    mouseAX = msg.mouseAX ?? msg.mouseX ?? 0.5;
    mouseAY = msg.mouseAY ?? msg.mouseY ?? 0.5;
    mouseBX = msg.mouseBX ?? msg.mouseX ?? 0.5;
    mouseBY = msg.mouseBY ?? msg.mouseY ?? 0.5;
    paramsA = msg.paramsA;
    paramsB = msg.paramsB;
    if ('qrOverlay' in msg) applyGigOutputQrFramePayload(msg.qrOverlay);
  } else if (msg.type === 'clear') {
    const gl = msg.deck === 'A' ? glA : glB;
    const oldProg = msg.deck === 'A' ? progA : progB;
    if (oldProg) gl.deleteProgram(oldProg);
    if (msg.deck === 'A') { progA = null; metaA = []; }
    else { progB = null; metaB = []; }
  } else if (msg.type === 'qr-overlay') {
    applyGigOutputQrFramePayload(msg.qrOverlay);
  }
}

function setStatus(text: string): void {
  if (statusEl) statusEl.textContent = text;
}

function streamQuery(): string {
  const viewToken = getViewTokenFromUrl();
  if (viewToken) {
    return `viewToken=${encodeURIComponent(viewToken)}`;
  }
  return vjViewQuery();
}

function connectRemoteStream(): void {
  setStatus('Connecting to stream...');
  const q = streamQuery();
  const url = (typeof window !== 'undefined' && window.location.origin)
    ? `${window.location.origin}/api/vj-output/stream?${q}`
    : `/api/vj-output/stream?${q}`;
  let es = new EventSource(url);
  let receivedAny = false;
  let reconnectAttempts = 0;

  function attachHandlers(source: EventSource): void {
    source.onopen = () => {
      reconnectAttempts = 0;
      if (!progA && !progB) setStatus('Connected. Waiting for signal...');
    };
    source.onmessage = (ev: MessageEvent<string>) => {
      try {
        const msg = JSON.parse(ev.data) as VJMsg;
        if (!receivedAny && (msg.type === 'shader' || msg.type === 'frame')) receivedAny = true;
        applyVJMsg(msg);
      } catch (_) {}
    };
    source.onerror = () => {
      source.close();
      reconnectAttempts++;
      const delay = Math.min(reconnectAttempts * 1000, 5000);
      if (!progA && !progB) setStatus('Reconnecting...');
      setTimeout(() => {
        es = new EventSource(url);
        attachHandlers(es);
      }, delay);
    };
  }

  attachHandlers(es);

  const noSignalTimer = setTimeout(() => {
    if (progA || progB) return;
    setStatus('No signal. On host: open the app, go to VJ deck, load a shader in deck A or B.');
  }, 8000);
  const checkDone = setInterval(() => {
    if (progA || progB) {
      clearInterval(checkDone);
      clearTimeout(noSignalTimer);
    }
  }, 500);
}

const useRemoteOnly = typeof window !== 'undefined' && new URLSearchParams(window.location.search).get('remote') === '1';
const isRemoteHost = typeof window !== 'undefined' && window.location.hostname !== 'localhost' && window.location.hostname !== '127.0.0.1';
const noOpener = typeof window !== 'undefined' && !window.opener;
const likelyRemote = noOpener;

if (useRemoteOnly || isRemoteHost || noOpener) {
  connectRemoteStream();
} else {
  const channel = new BroadcastChannel('macroverse-vj-output');
  let remoteFallbackScheduled = true;
  const fallbackDelay = likelyRemote ? 800 : 2500;
  const fallbackTimer = setTimeout(() => {
    remoteFallbackScheduled = false;
    if (!progA && !progB) connectRemoteStream();
  }, fallbackDelay);
  channel.onmessage = (ev: MessageEvent<VJMsg>) => {
    applyVJMsg(ev.data);
    if (remoteFallbackScheduled && (ev.data?.type === 'shader' || ev.data?.type === 'frame')) {
      clearTimeout(fallbackTimer);
      remoteFallbackScheduled = false;
    }
  };

  channel.postMessage({ type: 'output-ready' });
  let syncRetries = 0;
  const syncInterval = setInterval(() => {
    if (progA || progB || ++syncRetries > 15) {
      clearInterval(syncInterval);
      return;
    }
    channel.postMessage({ type: 'output-ready' });
  }, 500);
}

/** Audience member opened the phone QR (not projector / operator pop-out). */
function isAudiencePhonePage(): boolean {
  if (typeof window === 'undefined') return false;
  const params = new URLSearchParams(window.location.search);
  return params.get('audienceUi') === '1' && Boolean(getViewTokenFromUrl());
}

let audienceParticipation = false;
let lastAudiencePost = 0;
const AUDIENCE_POST_MS = 50;
let interactHintFadeTimer = 0;
let interactHintHideTimer = 0;

const INTERACT_HINT_HOLD_MS = 2800;
const INTERACT_HINT_FADE_MS = 1100;

function showAudienceInteractHint(): void {
  if (!audienceInteractHintEl || !isAudiencePhonePage() || !audienceParticipation) return;
  window.clearTimeout(interactHintFadeTimer);
  window.clearTimeout(interactHintHideTimer);
  audienceInteractHintEl.hidden = false;
  audienceInteractHintEl.classList.remove('is-visible');
  void audienceInteractHintEl.offsetWidth;
  audienceInteractHintEl.classList.add('is-visible');
  interactHintFadeTimer = window.setTimeout(() => {
    audienceInteractHintEl.classList.remove('is-visible');
    interactHintHideTimer = window.setTimeout(() => {
      audienceInteractHintEl.hidden = true;
    }, INTERACT_HINT_FADE_MS);
  }, INTERACT_HINT_HOLD_MS);
}

async function refreshAudienceParticipation(): Promise<void> {
  if (!isAudiencePhonePage()) return;
  const enabled = await fetchAudienceParticipationConfig();
  if (enabled === audienceParticipation) return;
  const wasOff = !audienceParticipation;
  audienceParticipation = enabled;
  outputCanvas.style.cursor = enabled ? 'crosshair' : 'default';
  if (enabled && wasOff) {
    showAudienceInteractHint();
  }
  if (!enabled && audienceInteractHintEl) {
    audienceInteractHintEl.classList.remove('is-visible');
    audienceInteractHintEl.hidden = true;
  }
}

function postAudienceMouse(mx: number, my: number): void {
  const now = performance.now();
  if (now - lastAudiencePost < AUDIENCE_POST_MS) return;
  lastAudiencePost = now;
  void fetch(`/api/vj-output/audience-mouse?${audienceMouseQuery()}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ mouseX: mx, mouseY: my }),
  }).catch(() => {});
}

function setOutputMouseFromClient(clientX: number, clientY: number): void {
  const rect = outputCanvas.getBoundingClientRect();
  const mx = Math.max(0, Math.min(1, (clientX - rect.left) / rect.width));
  const my = Math.max(0, Math.min(1, 1 - (clientY - rect.top) / rect.height));
  if (isAudiencePhonePage()) {
    if (audienceParticipation) {
      postAudienceMouse(mx, my);
      mouseX = mx;
      mouseY = my;
    }
    return;
  }
  mouseX = mx;
  mouseY = my;
  mouseAX = mx;
  mouseAY = my;
  mouseBX = mx;
  mouseBY = my;
}

if (isAudiencePhonePage()) {
  void refreshAudienceParticipation();
  window.setInterval(() => void refreshAudienceParticipation(), 4000);
} else {
  outputCanvas.style.cursor = 'crosshair';
}

outputCanvas.addEventListener('mousemove', (e) => setOutputMouseFromClient(e.clientX, e.clientY));
outputCanvas.addEventListener('mouseleave', () => {
  if (isAudiencePhonePage()) return;
  mouseX = 0.5;
  mouseY = 0.5;
  mouseAX = 0.5;
  mouseAY = 0.5;
  mouseBX = 0.5;
  mouseBY = 0.5;
});
outputCanvas.addEventListener('touchstart', (e) => {
  if (e.touches.length) setOutputMouseFromClient(e.touches[0].clientX, e.touches[0].clientY);
}, { passive: true });
outputCanvas.addEventListener('touchmove', (e) => {
  if (e.touches.length) {
    setOutputMouseFromClient(e.touches[0].clientX, e.touches[0].clientY);
    if (isAudiencePhonePage() && audienceParticipation) e.preventDefault();
  }
}, { passive: false });
outputCanvas.addEventListener('touchend', () => {
  if (isAudiencePhonePage()) return;
  mouseX = 0.5;
  mouseY = 0.5;
  mouseAX = 0.5;
  mouseAY = 0.5;
  mouseBX = 0.5;
  mouseBY = 0.5;
}, { passive: true });
outputCanvas.addEventListener('touchcancel', () => {
  if (isAudiencePhonePage()) return;
  mouseX = 0.5;
  mouseY = 0.5;
  mouseAX = 0.5;
  mouseAY = 0.5;
  mouseBX = 0.5;
  mouseBY = 0.5;
}, { passive: true });

// ---- Render loop ----

function renderDeck(
  gl: WebGLRenderingContext, canvas: HTMLCanvasElement,
  prog: WebGLProgram | null, buf: WebGLBuffer,
  params: Record<string, number | boolean>, meta: ParamMeta[],
  mx: number, my: number
): void {
  gl.viewport(0, 0, canvas.width, canvas.height);
  gl.clearColor(0.05, 0.05, 0.08, 1);
  gl.clear(gl.COLOR_BUFFER_BIT);
  if (!prog) return;
  gl.useProgram(prog);
  const t = (performance.now() - startTime) / 1000;
  setDeckUniforms(gl, prog, canvas.width, canvas.height, t, params, meta, mx, my);
  gl.bindBuffer(gl.ARRAY_BUFFER, buf);
  const loc = gl.getAttribLocation(prog, 'a_pos');
  gl.enableVertexAttribArray(loc);
  gl.vertexAttribPointer(loc, 2, gl.FLOAT, false, 0, 0);
  gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
}

function renderMix(): void {
  glMix.bindTexture(glMix.TEXTURE_2D, texA);
  glMix.texImage2D(glMix.TEXTURE_2D, 0, glMix.RGBA, glMix.RGBA, glMix.UNSIGNED_BYTE, deckCanvasA);
  glMix.bindTexture(glMix.TEXTURE_2D, texB);
  glMix.texImage2D(glMix.TEXTURE_2D, 0, glMix.RGBA, glMix.RGBA, glMix.UNSIGNED_BYTE, deckCanvasB);

  glMix.useProgram(mixProg);
  glMix.viewport(0, 0, outputCanvas.width, outputCanvas.height);
  glMix.clearColor(0, 0, 0, 1);
  glMix.clear(glMix.COLOR_BUFFER_BIT);

  glMix.activeTexture(glMix.TEXTURE0);
  glMix.bindTexture(glMix.TEXTURE_2D, texA);
  glMix.activeTexture(glMix.TEXTURE1);
  glMix.bindTexture(glMix.TEXTURE_2D, texB);

  const cf = glMix.getUniformLocation(mixProg, 'crossfader');
  if (cf) glMix.uniform1f(cf, crossfader);
  const mm = glMix.getUniformLocation(mixProg, 'mixMode');
  if (mm) glMix.uniform1i(mm, mixModeInt);
  const fv = glMix.getUniformLocation(mixProg, 'outputFlipV');
  if (fv) glMix.uniform1f(fv, flipV ? 1 : 0);
  const fh = glMix.getUniformLocation(mixProg, 'outputFlipH');
  if (fh) glMix.uniform1f(fh, flipH ? 1 : 0);
  const ro = glMix.getUniformLocation(mixProg, 'outputRotation');
  if (ro) glMix.uniform1i(ro, rotation === 0 ? 0 : rotation === 90 ? 1 : rotation === 180 ? 2 : 3);
  const tA = glMix.getUniformLocation(mixProg, 'texA');
  if (tA) glMix.uniform1i(tA, 0);
  const tB = glMix.getUniformLocation(mixProg, 'texB');
  if (tB) glMix.uniform1i(tB, 1);

  glMix.bindBuffer(glMix.ARRAY_BUFFER, bufMix);
  const loc = glMix.getAttribLocation(mixProg, 'a_pos');
  glMix.enableVertexAttribArray(loc);
  glMix.vertexAttribPointer(loc, 2, glMix.FLOAT, false, 0, 0);
  glMix.drawArrays(glMix.TRIANGLE_STRIP, 0, 4);
}

function frame(): void {
  renderDeck(glA, deckCanvasA, progA, bufA, paramsA, metaA, mouseAX, mouseAY);
  renderDeck(glB, deckCanvasB, progB, bufB, paramsB, metaB, mouseBX, mouseBY);
  renderMix();
  requestAnimationFrame(frame);
}

requestAnimationFrame(frame);
