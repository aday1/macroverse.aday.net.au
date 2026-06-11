import QRCode from 'qrcode';
import { getVjSessionId } from './vjSession.js';
import { getStoredViewerToken, vjJoinQuery, vjViewQuery } from './vjTokens.js';

function normalizeSessionId(sessionId: string): string {
  return sessionId.trim().slice(0, 64) || 'default';
}

/** Main app URL — scan to join and co-VJ this gig (signed controlToken, not raw sessionId). */
export function buildGigJoinUrl(sessionId?: string): string {
  const base = typeof window !== 'undefined' ? window.location.origin : '';
  const query = vjJoinQuery(sessionId);
  return `${base}/?${query}`;
}

/** Projector / Pi HDMI / operator pop-out — view-only stream (no audience UI overlay). */
export function buildGigOutputUrl(sessionId?: string): string {
  const base = typeof window !== 'undefined' ? window.location.origin : '';
  const query = getStoredViewerToken(sessionId)
    ? vjViewQuery(sessionId)
    : `sessionId=${encodeURIComponent(normalizeSessionId(sessionId ?? getVjSessionId()))}`;
  return `${base}/vj-output.html?remote=1&${query}`;
}

/** Phone QR — same stream plus audienceUi=1 (interactive hint + touch X/Y on their device only). */
export function buildGigAudienceStreamUrl(sessionId?: string): string {
  const url = buildGigOutputUrl(sessionId);
  return url.includes('?') ? `${url}&audienceUi=1` : `${url}?audienceUi=1`;
}

/** WebXR headset — live VJ mix inside a 360 dome or cinema screen. Requires remote=1 + viewToken. */
export function buildGigVrUrl(sessionId?: string, mode: 'dome' | 'screen' = 'dome'): string {
  const base = typeof window !== 'undefined' ? window.location.origin : '';
  const query = getStoredViewerToken(sessionId)
    ? vjViewQuery(sessionId)
    : `sessionId=${encodeURIComponent(normalizeSessionId(sessionId ?? getVjSessionId()))}`;
  const modeQ = mode === 'screen' ? '&mode=screen' : '';
  return `${base}/vj-vr.html?remote=1&${query}${modeQ}`;
}

/** WebXR audience — viewToken stream; enable audience participation on host for mouse X/Y. */
export function buildGigVrAudienceUrl(sessionId?: string, mode: 'dome' | 'screen' = 'dome'): string {
  const url = buildGigVrUrl(sessionId, mode);
  return url.includes('?') ? `${url}&role=audience` : `${url}?role=audience`;
}

/** WebXR VJ controller — controlToken drives host via WebSocket; same live mix stream. */
export function buildGigVrControllerUrl(sessionId?: string, mode: 'dome' | 'screen' = 'dome'): string {
  const base = typeof window !== 'undefined' ? window.location.origin : '';
  const cq = vjJoinQuery(sessionId);
  const vq = getStoredViewerToken(sessionId) ? vjViewQuery(sessionId) : '';
  const modeQ = mode === 'screen' ? '&mode=screen' : '';
  const join = vq ? `${cq}&${vq}` : cq;
  return `${base}/vj-vr.html?role=vj&remote=1&${join}${modeQ}`;
}

export async function drawGigQr(canvas: HTMLCanvasElement, url: string, sizePx?: number): Promise<void> {
  const px = Math.max(64, Math.round(sizePx ?? canvas.width ?? 180));
  canvas.width = px;
  canvas.height = px;
  canvas.style.width = `${px}px`;
  canvas.style.height = `${px}px`;
  if (!url || !url.trim()) {
    const ctx = canvas.getContext('2d');
    ctx?.clearRect(0, 0, px, px);
    return;
  }
  await QRCode.toCanvas(canvas, url, {
    width: px,
    margin: 2,
    errorCorrectionLevel: 'M',
  });
}

export async function refreshGigQrPair(
  joinCanvas: HTMLCanvasElement | null,
  outputCanvas: HTMLCanvasElement | null,
  joinUrlEl: HTMLElement | null,
  sessionId?: string
): Promise<void> {
  const joinUrl = buildGigJoinUrl(sessionId);
  const outputUrl = buildGigAudienceStreamUrl(sessionId);
  if (joinCanvas) await drawGigQr(joinCanvas, joinUrl);
  if (outputCanvas) await drawGigQr(outputCanvas, outputUrl);
  if (joinUrlEl) joinUrlEl.textContent = joinUrl;
}
