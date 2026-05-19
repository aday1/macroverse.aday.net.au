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

export async function drawGigQr(canvas: HTMLCanvasElement, url: string): Promise<void> {
  await QRCode.toCanvas(canvas, url, {
    width: canvas.width || 180,
    margin: 1,
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
