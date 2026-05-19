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

export interface GigOutputQrFrame {
  enabled: boolean;
  mix: number;
  opacity: number;
  layout: GigOutputQrLayout;
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
let qrTile: HTMLCanvasElement | null = null;
let qrTileSession = '';

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
}

export function isGigOutputQrActive(): boolean {
  return targetVisible || opacity > 0.01;
}

export function isGigOutputQrVisible(): boolean {
  return targetVisible;
}

export function getGigOutputQrOpacity(): number {
  return opacity;
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

export function setGigOutputQrVisible(visible: boolean, fadeMs = DEFAULT_FADE_MS): void {
  if (visible === targetVisible && (visible ? opacity >= 0.99 : opacity <= 0.01)) return;
  targetVisible = visible;
  fadeFrom = opacity;
  fadeTo = visible ? 1 : 0;
  fadeStart = performance.now();
  fadeDurationMs = fadeMs;
  if (visible) void ensureVjTokens().then(() => ensureGigOutputQrTile());
  window.dispatchEvent(
    new CustomEvent('macroverse-gig-output-qr-visible', { detail: { visible } })
  );
}

export function tickGigOutputQrFade(): void {
  if (fadeDurationMs <= 0) {
    opacity = fadeTo;
    return;
  }
  const t = Math.min(1, (performance.now() - fadeStart) / fadeDurationMs);
  const eased = t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2;
  opacity = fadeFrom + (fadeTo - fadeFrom) * eased;
  if (t >= 1) fadeDurationMs = 0;
}

export function getGigOutputQrFramePayload(): GigOutputQrFrame | null {
  if (opacity <= 0.001 && !targetVisible) return null;
  return { enabled: targetVisible, mix, opacity, layout: { ...layout } };
}

export function applyGigOutputQrFramePayload(payload: GigOutputQrFrame | undefined | null): void {
  if (payload === undefined) return;
  if (!payload || (!payload.enabled && payload.opacity <= 0.001)) {
    targetVisible = false;
    opacity = 0;
    return;
  }
  targetVisible = payload.enabled;
  mix = clampMix(payload.mix);
  opacity = Math.max(0, Math.min(1, payload.opacity));
  fadeDurationMs = 0;
  if (payload.layout) {
    layout = {
      posX: clamp01(payload.layout.posX ?? DEFAULT_LAYOUT.posX),
      posY: clamp01(payload.layout.posY ?? DEFAULT_LAYOUT.posY),
      scale: clampScale(payload.layout.scale ?? DEFAULT_LAYOUT.scale),
      rotation: clampRotation(payload.layout.rotation ?? DEFAULT_LAYOUT.rotation),
      fx: clamp01(payload.layout.fx ?? DEFAULT_LAYOUT.fx),
      showLabel: payload.layout.showLabel !== false,
    };
  }
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

function paintGigOutputQrOn2d(
  ctx: CanvasRenderingContext2D,
  w: number,
  h: number,
  tile: HTMLCanvasElement
): void {
  const alpha = opacity * mix;
  if (alpha <= 0.001) return;

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
  const blockW = qrSize * pulse + pad * 2;

  ctx.save();
  ctx.globalAlpha = alpha;
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
  sessionId?: string
): Promise<void> {
  tickGigOutputQrFade();
  ctx.clearRect(0, 0, w, h);
  const alpha = opacity * mix;
  if (alpha <= 0.001) return;
  const tile = await ensureGigOutputQrTile(sessionId);
  paintGigOutputQrOn2d(ctx, w, h, tile);
}

export function drawGigOutputQrOnCanvas(
  canvas: HTMLCanvasElement,
  sessionId?: string
): void {
  const w = canvas.width;
  const h = canvas.height;
  if (w <= 0 || h <= 0) return;
  const ctx = canvas.getContext('2d');
  if (!ctx) return;
  const show = opacity > 0.001 || targetVisible;
  canvas.style.visibility = show ? 'visible' : 'hidden';
  if (!show) {
    ctx.clearRect(0, 0, w, h);
    return;
  }
  tickGigOutputQrFade();
  const sid = (sessionId ?? getVjSessionId()).trim().slice(0, 64) || 'default';
  if (qrTile && qrTileSession === sid) {
    ctx.clearRect(0, 0, w, h);
    paintGigOutputQrOn2d(ctx, w, h, qrTile);
    return;
  }
  if (!qrTileLoadPromise) {
    qrTileLoadPromise = ensureGigOutputQrTile(sessionId).finally(() => {
      qrTileLoadPromise = null;
    });
  }
  void qrTileLoadPromise.then((tile) => {
    ctx.clearRect(0, 0, w, h);
    paintGigOutputQrOn2d(ctx, w, h, tile);
  });
}
