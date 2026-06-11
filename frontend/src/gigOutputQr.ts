import { buildGigAudienceStreamUrl } from './gigQr.js';
import { getVjSessionId } from './vjSession.js';
import { ensureVjTokens } from './vjTokens.js';
import QRCode from 'qrcode';

const STORAGE_MIX = 'macroverse-gig-audience-mix';
const STORAGE_LAYOUT = 'macroverse-gig-output-qr-layout';
const DEFAULT_FADE_MS = 450;

export interface GigOutputQrLayout {
  /** Center X, 0–1 across output width */
  posX: number;
  /** Center Y, 0–1 across output height */
  posY: number;
  /** QR square side as fraction of min(output w, h) */
  scale: number;
  /** Degrees clockwise */
  rotation: number;
  /** 0–1 glow pulse + border shimmer (keeps QR modules clean for scan) */
  fx: number;
  showLabel: boolean;
}

export type GigOutputQrSurface = 'output' | 'stream';

export interface GigOutputQrFrame {
  /** Projector / preview / pop-out (vj-output without audienceUi). */
  enabled: boolean;
  /** Audience stream link pages (audienceUi=1). */
  streamEnabled: boolean;
  mix: number;
  opacity: number;
  streamOpacity: number;
  layout: GigOutputQrLayout;
}

export interface GigOutputQrCanvasOptions {
  surface?: GigOutputQrSurface;
  /** Stream viewers: return true to hide QR locally (dismiss button). */
  isDismissed?: () => boolean;
}

const DEFAULT_LAYOUT: GigOutputQrLayout = {
  posX: 0.5,
  posY: 0.5,
  scale: 0.42,
  rotation: 0,
  fx: 0.55,
  showLabel: true,
};

function clamp01(v: number): number {
  return Math.max(0, Math.min(1, v));
}

function clampScale(v: number): number {
  return Math.max(0.12, Math.min(0.72, v));
}

function clampRotation(v: number): number {
  let d = ((v % 360) + 360) % 360;
  if (d > 180) d -= 360;
  return d;
}

function loadStoredLayout(): GigOutputQrLayout {
  try {
    const raw = localStorage.getItem(STORAGE_LAYOUT);
    if (!raw) return { ...DEFAULT_LAYOUT };
    const p = JSON.parse(raw) as Partial<GigOutputQrLayout>;
    return {
      posX: clamp01(Number(p.posX ?? DEFAULT_LAYOUT.posX)),
      posY: clamp01(Number(p.posY ?? DEFAULT_LAYOUT.posY)),
      scale: clampScale(Number(p.scale ?? DEFAULT_LAYOUT.scale)),
      rotation: clampRotation(Number(p.rotation ?? DEFAULT_LAYOUT.rotation)),
      fx: clamp01(Number(p.fx ?? DEFAULT_LAYOUT.fx)),
      showLabel: p.showLabel !== false,
    };
  } catch {
    return { ...DEFAULT_LAYOUT };
  }
}

function saveLayout(layout: GigOutputQrLayout): void {
  try {
    localStorage.setItem(STORAGE_LAYOUT, JSON.stringify(layout));
  } catch {
    /* ignore */
  }
}

function clampMix(v: number): number {
  return Math.max(0, Math.min(1, v));
}

function loadStoredMix(): number {
  try {
    const v = parseFloat(localStorage.getItem(STORAGE_MIX) || '0.65');
    return Number.isFinite(v) ? clampMix(v) : 0.65;
  } catch {
    return 0.65;
  }
}

function saveMix(mix: number): void {
  try {
    localStorage.setItem(STORAGE_MIX, String(clampMix(mix)));
  } catch {
    /* ignore */
  }
}

let mix = loadStoredMix();
let layout = loadStoredLayout();
let targetVisible = false;
let opacity = 0;
let fadeFrom = 0;
let fadeTo = 0;
let fadeStart = 0;
let fadeDurationMs = 0;
let targetStreamVisible = false;
let streamOpacity = 0;
let streamFadeFrom = 0;
let streamFadeTo = 0;
let streamFadeStart = 0;
let streamFadeDurationMs = 0;
let qrTile: HTMLCanvasElement | null = null;
let qrTileSession = '';

interface QrCanvasEntry {
  canvas: HTMLCanvasElement;
  surface: GigOutputQrSurface;
  isDismissed?: () => boolean;
}
const qrCanvases = new Map<HTMLCanvasElement, QrCanvasEntry>();
let qrRafId = 0;
let qrFrameRelay: (() => void) | null = null;
let lastQrRelayAt = 0;
const QR_RELAY_MS = 33;

function dispatchLayoutChange(): void {
  window.dispatchEvent(new CustomEvent('macroverse-gig-output-qr-layout', { detail: { ...layout } }));
}

export function getGigOutputQrLayout(): Readonly<GigOutputQrLayout> {
  return layout;
}

export function setGigOutputQrLayout(patch: Partial<GigOutputQrLayout>): void {
  layout = {
    posX: patch.posX !== undefined ? clamp01(patch.posX) : layout.posX,
    posY: patch.posY !== undefined ? clamp01(patch.posY) : layout.posY,
    scale: patch.scale !== undefined ? clampScale(patch.scale) : layout.scale,
    rotation: patch.rotation !== undefined ? clampRotation(patch.rotation) : layout.rotation,
    fx: patch.fx !== undefined ? clamp01(patch.fx) : layout.fx,
    showLabel: patch.showLabel !== undefined ? patch.showLabel : layout.showLabel,
  };
  saveLayout(layout);
  dispatchLayoutChange();
  ensureGigOutputQrLoop();
}

export function resetGigOutputQrLayout(): void {
  layout = { ...DEFAULT_LAYOUT };
  saveLayout(layout);
  dispatchLayoutChange();
}

export function getGigOutputQrMix(): number {
  return mix;
}

export function setGigOutputQrMix(value: number): void {
  mix = clampMix(value);
  saveMix(mix);
  window.dispatchEvent(
    new CustomEvent('macroverse-gig-output-qr-mix', { detail: { mix } })
  );
  window.dispatchEvent(
    new CustomEvent('macroverse-gig-audience-mix', { detail: { mix } })
  );
  ensureGigOutputQrLoop();
}

export function isGigOutputQrActive(): boolean {
  return targetVisible || opacity > 0.01;
}

export function isGigOutputQrVisible(): boolean {
  return targetVisible;
}

export function isGigStreamQrActive(): boolean {
  return targetStreamVisible || streamOpacity > 0.01;
}

export function isGigStreamQrVisible(): boolean {
  return targetStreamVisible;
}

export function getGigOutputQrOpacity(): number {
  return opacity;
}

export function getGigStreamQrOpacity(): number {
  return streamOpacity;
}

/** True when this vj-output page should use the stream QR surface. */
export function isStreamLinkViewerPage(): boolean {
  if (typeof window === 'undefined') return false;
  return new URLSearchParams(window.location.search).get('audienceUi') === '1';
}

async function drawOutputQrTile(canvas: HTMLCanvasElement, url: string): Promise<void> {
  await QRCode.toCanvas(canvas, url, {
    width: canvas.width || 512,
    margin: 2,
    errorCorrectionLevel: 'H',
  });
}

export async function ensureGigOutputQrTile(sessionId?: string): Promise<HTMLCanvasElement> {
  const sid = (sessionId ?? getVjSessionId()).trim().slice(0, 64) || 'default';
  if (qrTile && qrTileSession === sid) return qrTile;
  qrTileSession = sid;
  if (!qrTile) {
    qrTile = document.createElement('canvas');
    qrTile.width = 512;
    qrTile.height = 512;
  }
  await drawOutputQrTile(qrTile, buildGigAudienceStreamUrl(sid));
  return qrTile;
}

export function refreshGigOutputQrSession(sessionId?: string): void {
  qrTileSession = '';
  void ensureVjTokens(sessionId).then(() => ensureGigOutputQrTile(sessionId));
}

export function registerGigOutputQrCanvas(
  canvas: HTMLCanvasElement,
  options?: GigOutputQrCanvasOptions
): () => void {
  qrCanvases.set(canvas, {
    canvas,
    surface: options?.surface ?? 'output',
    isDismissed: options?.isDismissed,
  });
  ensureGigOutputQrLoop();
  return () => {
    qrCanvases.delete(canvas);
  };
}

export function setGigOutputQrFrameRelay(relay: (() => void) | null): void {
  qrFrameRelay = relay;
}

function qrLoopNeedsRun(): boolean {
  return (
    targetVisible ||
    opacity > 0.001 ||
    fadeDurationMs > 0 ||
    targetStreamVisible ||
    streamOpacity > 0.001 ||
    streamFadeDurationMs > 0
  );
}

function runGigOutputQrLoopFrame(): void {
  tickGigOutputQrFade();
  tickGigStreamQrFade();
  for (const entry of qrCanvases.values()) {
    drawGigOutputQrOnCanvas(entry.canvas, undefined, entry);
  }
  const now = performance.now();
  if (qrFrameRelay && now - lastQrRelayAt >= QR_RELAY_MS) {
    lastQrRelayAt = now;
    qrFrameRelay();
  }
}

function gigOutputQrLoopTick(): void {
  qrRafId = 0;
  if (!qrLoopNeedsRun()) return;
  runGigOutputQrLoopFrame();
  if (qrLoopNeedsRun()) {
    qrRafId = requestAnimationFrame(gigOutputQrLoopTick);
  } else if (qrFrameRelay) {
    lastQrRelayAt = 0;
    qrFrameRelay();
  }
}

export function ensureGigOutputQrLoop(): void {
  runGigOutputQrLoopFrame();
  if (!qrRafId && qrLoopNeedsRun()) {
    qrRafId = requestAnimationFrame(gigOutputQrLoopTick);
  }
}

function bumpMixIfTooLow(): void {
  if (mix >= 0.05) return;
  const stored = loadStoredMix();
  mix = stored >= 0.05 ? stored : 0.65;
  saveMix(mix);
  window.dispatchEvent(
    new CustomEvent('macroverse-gig-output-qr-mix', { detail: { mix } })
  );
}

export function setGigOutputQrVisible(visible: boolean, fadeMs = DEFAULT_FADE_MS): void {
  const settled = visible ? opacity >= 0.99 : opacity <= 0.01;
  if (visible === targetVisible && settled && fadeDurationMs <= 0) return;
  targetVisible = visible;
  fadeFrom = opacity;
  fadeTo = visible ? 1 : 0;
  fadeStart = performance.now();
  fadeDurationMs = fadeMs;
  if (visible) bumpMixIfTooLow();
  if (visible) void ensureVjTokens().then(() => ensureGigOutputQrTile()).then(() => ensureGigOutputQrLoop());
  window.dispatchEvent(
    new CustomEvent('macroverse-gig-output-qr-visible', { detail: { visible } })
  );
  ensureGigOutputQrLoop();
}

export function setGigStreamQrVisible(visible: boolean, fadeMs = DEFAULT_FADE_MS): void {
  const settled = visible ? streamOpacity >= 0.99 : streamOpacity <= 0.01;
  if (visible === targetStreamVisible && settled && streamFadeDurationMs <= 0) return;
  targetStreamVisible = visible;
  streamFadeFrom = streamOpacity;
  streamFadeTo = visible ? 1 : 0;
  streamFadeStart = performance.now();
  streamFadeDurationMs = fadeMs;
  if (visible) bumpMixIfTooLow();
  if (visible) void ensureVjTokens().then(() => ensureGigOutputQrTile()).then(() => ensureGigOutputQrLoop());
  window.dispatchEvent(
    new CustomEvent('macroverse-gig-stream-qr-visible', { detail: { visible } })
  );
  ensureGigOutputQrLoop();
}

function tickQrFade(
  durationMs: number,
  from: number,
  to: number,
  start: number,
  onSet: (v: number) => void,
  onDone: () => void
): void {
  if (durationMs <= 0) {
    onSet(to);
    return;
  }
  const t = Math.min(1, (performance.now() - start) / durationMs);
  const eased = t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2;
  onSet(from + (to - from) * eased);
  if (t >= 1) onDone();
}

export function tickGigOutputQrFade(): void {
  tickQrFade(fadeDurationMs, fadeFrom, fadeTo, fadeStart, (v) => {
    opacity = v;
  }, () => {
    fadeDurationMs = 0;
  });
}

export function tickGigStreamQrFade(): void {
  tickQrFade(streamFadeDurationMs, streamFadeFrom, streamFadeTo, streamFadeStart, (v) => {
    streamOpacity = v;
  }, () => {
    streamFadeDurationMs = 0;
  });
}

export function getGigOutputQrFramePayload(): GigOutputQrFrame | null {
  const outputOn = targetVisible || opacity > 0.001;
  const streamOn = targetStreamVisible || streamOpacity > 0.001;
  if (!outputOn && !streamOn) return null;
  return {
    enabled: targetVisible,
    streamEnabled: targetStreamVisible,
    mix,
    opacity,
    streamOpacity,
    layout: { ...layout },
  };
}

export function applyGigOutputQrFramePayload(payload: GigOutputQrFrame | undefined | null): void {
  if (payload === undefined) return;
  const outputOff = !payload || (!payload.enabled && payload.opacity <= 0.001);
  const streamOn = payload?.streamEnabled === true;
  const streamOp = payload?.streamOpacity ?? (streamOn ? payload?.opacity ?? 0 : 0);
  const streamOff = !payload || (!streamOn && streamOp <= 0.001);
  if (outputOff && streamOff) {
    targetVisible = false;
    opacity = 0;
    targetStreamVisible = false;
    streamOpacity = 0;
    ensureGigOutputQrLoop();
    window.dispatchEvent(
      new CustomEvent('macroverse-gig-stream-qr-visible', { detail: { visible: false } })
    );
    return;
  }
  if (!outputOff) {
    targetVisible = payload!.enabled;
    opacity = Math.max(0, Math.min(1, payload!.opacity));
    fadeDurationMs = 0;
  } else {
    targetVisible = false;
    opacity = 0;
  }
  if (!streamOff) {
    targetStreamVisible = streamOn;
    streamOpacity = Math.max(0, Math.min(1, streamOp));
    streamFadeDurationMs = 0;
  } else {
    targetStreamVisible = false;
    streamOpacity = 0;
  }
  if (payload?.layout) {
    layout = {
      posX: clamp01(payload.layout.posX ?? DEFAULT_LAYOUT.posX),
      posY: clamp01(payload.layout.posY ?? DEFAULT_LAYOUT.posY),
      scale: clampScale(payload.layout.scale ?? DEFAULT_LAYOUT.scale),
      rotation: clampRotation(payload.layout.rotation ?? DEFAULT_LAYOUT.rotation),
      fx: clamp01(payload.layout.fx ?? DEFAULT_LAYOUT.fx),
      showLabel: payload.layout.showLabel !== false,
    };
  }
  if (payload?.mix !== undefined) mix = clampMix(payload.mix);
  ensureGigOutputQrLoop();
  window.dispatchEvent(
    new CustomEvent('macroverse-gig-stream-qr-visible', { detail: { visible: targetStreamVisible } })
  );
}

/** Map pointer on output canvas to normalized layout center. */
export function gigOutputQrLayoutFromPointer(
  clientX: number,
  clientY: number,
  canvas: HTMLCanvasElement
): Pick<GigOutputQrLayout, 'posX' | 'posY'> {
  const rect = canvas.getBoundingClientRect();
  if (rect.width <= 0 || rect.height <= 0) return { posX: layout.posX, posY: layout.posY };
  return {
    posX: clamp01((clientX - rect.left) / rect.width),
    posY: clamp01((clientY - rect.top) / rect.height),
  };
}

let qrTileLoadPromise: Promise<HTMLCanvasElement> | null = null;

function surfacePaintAlpha(entry: QrCanvasEntry): number {
  if (entry.isDismissed?.()) return 0;
  if (entry.surface === 'stream') {
    if (!targetStreamVisible && streamOpacity <= 0.001) return 0;
    return streamOpacity * mix;
  }
  if (!targetVisible && opacity <= 0.001) return 0;
  return opacity * mix;
}

function surfaceTargetVisible(entry: QrCanvasEntry): boolean {
  if (entry.isDismissed?.()) return false;
  return entry.surface === 'stream' ? targetStreamVisible : targetVisible;
}

function paintGigOutputQrOn2d(
  ctx: CanvasRenderingContext2D,
  w: number,
  h: number,
  tile: HTMLCanvasElement,
  paintAlpha: number
): void {
  if (paintAlpha <= 0.001) return;

  const minDim = Math.min(w, h);
  const qrSize = minDim * layout.scale;
  const cx = w * layout.posX;
  const cy = h * layout.posY;
  const fx = layout.fx;
  const t = performance.now() * 0.001;
  const pulse = 1 + Math.sin(t * 2.1) * 0.035 * fx;
  const glowA = 0.25 + 0.35 * fx * (0.5 + 0.5 * Math.sin(t * 1.6));
  const pad = Math.max(6, qrSize * 0.04);
  const labelGap = Math.max(14, h * 0.028);
  const fontSize = Math.max(12, Math.round(minDim * 0.028));
  const labelH = layout.showLabel ? fontSize + labelGap : 0;
  const blockH = labelH + qrSize * pulse + pad * 2;

  ctx.save();
  ctx.globalAlpha = paintAlpha;
  ctx.translate(cx, cy);
  ctx.rotate((layout.rotation * Math.PI) / 180);

  if (layout.showLabel) {
    ctx.textAlign = 'center';
    ctx.textBaseline = 'bottom';
    ctx.fillStyle = '#f8dd36';
    ctx.font = `600 ${fontSize}px system-ui, sans-serif`;
    ctx.shadowColor = 'rgba(0,0,0,0.9)';
    ctx.shadowBlur = 8 + 6 * fx;
    const labelY = -blockH / 2 + labelH;
    ctx.fillText('Scan to stream the viz', 0, labelY);
    ctx.shadowBlur = 0;
  }

  const qrTop = -blockH / 2 + labelH + pad;
  const half = (qrSize * pulse) / 2;

  ctx.shadowColor = `rgba(248, 221, 54, ${glowA})`;
  ctx.shadowBlur = 12 + 18 * fx;
  ctx.fillStyle = '#ffffff';
  ctx.fillRect(-half - pad, qrTop - pad, qrSize * pulse + pad * 2, qrSize * pulse + pad * 2);
  ctx.shadowBlur = 0;

  if (fx > 0.05) {
    ctx.strokeStyle = `rgba(136, 204, 255, ${0.15 + 0.25 * fx * (0.5 + 0.5 * Math.sin(t * 3))})`;
    ctx.lineWidth = 2;
    ctx.strokeRect(-half - pad, qrTop - pad, qrSize * pulse + pad * 2, qrSize * pulse + pad * 2);
  }

  ctx.imageSmoothingEnabled = true;
  ctx.imageSmoothingQuality = 'high';
  ctx.drawImage(tile, -half, qrTop, qrSize * pulse, qrSize * pulse);

  if (fx > 0.08) {
    ctx.save();
    ctx.globalCompositeOperation = 'source-atop';
    ctx.fillStyle = `rgba(255, 255, 255, ${0.04 + 0.06 * fx * (0.5 + 0.5 * Math.sin(t * 4))})`;
    const bandH = Math.max(2, qrSize * 0.08);
    const bandY = qrTop + ((t * 40) % (qrSize * pulse + bandH)) - bandH;
    ctx.fillRect(-half, bandY, qrSize * pulse, bandH);
    ctx.restore();
  }

  ctx.restore();
}

export async function drawGigOutputQrOn2d(
  ctx: CanvasRenderingContext2D,
  w: number,
  h: number,
  sessionId?: string,
  surface: GigOutputQrSurface = 'output'
): Promise<void> {
  tickGigOutputQrFade();
  tickGigStreamQrFade();
  ctx.clearRect(0, 0, w, h);
  const paintAlpha =
    surface === 'stream'
      ? (targetStreamVisible || streamOpacity > 0.001 ? streamOpacity * mix : 0)
      : (targetVisible || opacity > 0.001 ? opacity * mix : 0);
  if (paintAlpha <= 0.001) return;
  const tile = await ensureGigOutputQrTile(sessionId);
  paintGigOutputQrOn2d(ctx, w, h, tile, paintAlpha);
}

export function drawGigOutputQrOnCanvas(
  canvas: HTMLCanvasElement,
  sessionId?: string,
  entry?: QrCanvasEntry
): void {
  const resolved =
    entry ??
    qrCanvases.get(canvas) ?? {
      canvas,
      surface: 'output' as GigOutputQrSurface,
    };
  const w = canvas.width;
  const h = canvas.height;
  if (w <= 0 || h <= 0) return;
  const ctx = canvas.getContext('2d');
  if (!ctx) return;
  const paintAlpha = surfacePaintAlpha(resolved);
  const showTarget = surfaceTargetVisible(resolved);
  const show = paintAlpha > 0.001 || showTarget;
  canvas.style.visibility = show ? 'visible' : 'hidden';
  const allowPointer = showTarget && paintAlpha > 0.05 && resolved.surface === 'output';
  canvas.style.pointerEvents = allowPointer ? 'auto' : 'none';
  if (!show || paintAlpha <= 0.001) {
    ctx.clearRect(0, 0, w, h);
    return;
  }
  const sid = (sessionId ?? getVjSessionId()).trim().slice(0, 64) || 'default';
  if (qrTile && qrTileSession === sid) {
    ctx.clearRect(0, 0, w, h);
    paintGigOutputQrOn2d(ctx, w, h, qrTile, paintAlpha);
    return;
  }
  if (!qrTileLoadPromise) {
    qrTileLoadPromise = ensureGigOutputQrTile(sessionId).finally(() => {
      qrTileLoadPromise = null;
    });
  }
  void qrTileLoadPromise.then((tile) => {
    ctx.clearRect(0, 0, w, h);
    paintGigOutputQrOn2d(ctx, w, h, tile, surfacePaintAlpha(resolved));
  });
}
