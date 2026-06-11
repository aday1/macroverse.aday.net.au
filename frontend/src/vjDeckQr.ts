import {
  buildGigAudienceStreamUrl,
  buildGigJoinUrl,
  drawGigQr,
} from './gigQr.js';
import { getVjSessionId } from './vjSession.js';

export type VjDeckQrId = 'a' | 'b';
export type DeckQrPreset = 'join' | 'stream' | 'custom';

export interface DeckQrLayout {
  posX: number;
  posY: number;
  scale: number;
  opacity: number;
}

export interface DeckQrState {
  enabled: boolean;
  preset: DeckQrPreset;
  customUrl: string;
  layout: DeckQrLayout;
  mirror: boolean;
}

const STORAGE_A = 'macroverse-vj-deck-qr-a';
const STORAGE_B = 'macroverse-vj-deck-qr-b';

const DEFAULT_LAYOUT: DeckQrLayout = {
  posX: 0.88,
  posY: 0.12,
  scale: 0.2,
  opacity: 0.92,
};

const DEFAULT_STATE: DeckQrState = {
  enabled: false,
  preset: 'join',
  customUrl: '',
  layout: { ...DEFAULT_LAYOUT },
  mirror: false,
};

function clamp01(v: number): number {
  return Math.max(0, Math.min(1, v));
}

function clampScale(v: number): number {
  return Math.max(0.08, Math.min(0.55, v));
}

function storageKey(deck: VjDeckQrId): string {
  return deck === 'a' ? STORAGE_A : STORAGE_B;
}

function normalizeDeck(deck: VjDeckQrId | 'A' | 'B'): VjDeckQrId {
  return deck === 'A' || deck === 'a' ? 'a' : 'b';
}

function parsePreset(v: unknown): DeckQrPreset {
  if (v === 'stream' || v === 'custom') return v;
  return 'join';
}

function loadDeckQr(deck: VjDeckQrId): DeckQrState {
  try {
    const raw = localStorage.getItem(storageKey(deck));
    if (!raw) return { ...DEFAULT_STATE, layout: { ...DEFAULT_LAYOUT } };
    const p = JSON.parse(raw) as Partial<DeckQrState> & { layout?: Partial<DeckQrLayout> };
    return {
      enabled: p.enabled === true,
      preset: parsePreset(p.preset),
      customUrl: typeof p.customUrl === 'string' ? p.customUrl : '',
      layout: {
        posX: clamp01(Number(p.layout?.posX ?? DEFAULT_LAYOUT.posX)),
        posY: clamp01(Number(p.layout?.posY ?? DEFAULT_LAYOUT.posY)),
        scale: clampScale(Number(p.layout?.scale ?? DEFAULT_LAYOUT.scale)),
        opacity: clamp01(Number(p.layout?.opacity ?? DEFAULT_LAYOUT.opacity)),
      },
      mirror: p.mirror === true,
    };
  } catch {
    return { ...DEFAULT_STATE, layout: { ...DEFAULT_LAYOUT } };
  }
}

function saveDeckQr(deck: VjDeckQrId, state: DeckQrState): void {
  try {
    localStorage.setItem(storageKey(deck), JSON.stringify(state));
  } catch {
    /* ignore */
  }
}

const deckState: Record<VjDeckQrId, DeckQrState> = {
  a: loadDeckQr('a'),
  b: loadDeckQr('b'),
};

const qrTiles = new Map<string, HTMLCanvasElement>();
const qrTileLoads = new Map<string, Promise<HTMLCanvasElement>>();

function dispatchDeckQrChange(deck: VjDeckQrId): void {
  if (typeof window === 'undefined') return;
  window.dispatchEvent(
    new CustomEvent('macroverse-vj-deck-qr-changed', {
      detail: { deck, state: { ...deckState[deck] } },
    })
  );
}

function deckUrl(state: DeckQrState, sessionId?: string): string {
  const sid = sessionId ?? getVjSessionId();
  if (state.preset === 'stream') return buildGigAudienceStreamUrl(sid);
  if (state.preset === 'custom') return state.customUrl.trim();
  return buildGigJoinUrl(sid);
}

async function ensureDeckQrTile(deck: VjDeckQrId, sessionId?: string): Promise<HTMLCanvasElement> {
  const state = deckState[deck];
  const url = deckUrl(state, sessionId);
  const cacheKey = `${deck}:${url}`;
  const existing = qrTiles.get(cacheKey);
  if (existing) return existing;

  let pending = qrTileLoads.get(cacheKey);
  if (!pending) {
    pending = (async () => {
      const canvas = document.createElement('canvas');
      canvas.width = 256;
      canvas.height = 256;
      await drawGigQr(canvas, url, 256);
      qrTiles.set(cacheKey, canvas);
      qrTileLoads.delete(cacheKey);
      return canvas;
    })();
    qrTileLoads.set(cacheKey, pending);
  }
  return pending;
}

export function getDeckQrState(deck: VjDeckQrId | 'A' | 'B'): Readonly<DeckQrState> {
  const id = normalizeDeck(deck);
  return deckState[id];
}

export function setDeckQrEnabled(deck: VjDeckQrId | 'A' | 'B', enabled: boolean): void {
  const id = normalizeDeck(deck);
  deckState[id] = { ...deckState[id], enabled };
  saveDeckQr(id, deckState[id]);
  dispatchDeckQrChange(id);
}

export function setDeckQrPreset(deck: VjDeckQrId | 'A' | 'B', preset: DeckQrPreset): void {
  const id = normalizeDeck(deck);
  deckState[id] = { ...deckState[id], preset };
  saveDeckQr(id, deckState[id]);
  dispatchDeckQrChange(id);
}

export function setDeckQrCustomUrl(deck: VjDeckQrId | 'A' | 'B', customUrl: string): void {
  const id = normalizeDeck(deck);
  deckState[id] = { ...deckState[id], customUrl };
  saveDeckQr(id, deckState[id]);
  dispatchDeckQrChange(id);
}

export function setDeckQrLayout(deck: VjDeckQrId | 'A' | 'B', patch: Partial<DeckQrLayout>): void {
  const id = normalizeDeck(deck);
  const layout = deckState[id].layout;
  deckState[id] = {
    ...deckState[id],
    layout: {
      posX: patch.posX !== undefined ? clamp01(patch.posX) : layout.posX,
      posY: patch.posY !== undefined ? clamp01(patch.posY) : layout.posY,
      scale: patch.scale !== undefined ? clampScale(patch.scale) : layout.scale,
      opacity: patch.opacity !== undefined ? clamp01(patch.opacity) : layout.opacity,
    },
  };
  saveDeckQr(id, deckState[id]);
  dispatchDeckQrChange(id);
}

export function setDeckQrMirror(deck: VjDeckQrId | 'A' | 'B', mirror: boolean): void {
  const id = normalizeDeck(deck);
  deckState[id] = { ...deckState[id], mirror };
  saveDeckQr(id, deckState[id]);
  dispatchDeckQrChange(id);
}

export function invalidateDeckQrTiles(): void {
  qrTiles.clear();
  qrTileLoads.clear();
}

export async function renderDeckQrOverlay(
  ctx: CanvasRenderingContext2D,
  w: number,
  h: number,
  deck: VjDeckQrId | 'A' | 'B',
  sessionId?: string
): Promise<void> {
  const id = normalizeDeck(deck);
  const state = deckState[id];
  if (!state.enabled || state.layout.opacity <= 0.001) return;

  const url = deckUrl(state, sessionId);
  if (!url.trim()) return;

  const tile = await ensureDeckQrTile(id, sessionId);
  const minDim = Math.min(w, h);
  const qrSize = minDim * state.layout.scale;
  const cx = w * state.layout.posX;
  const cy = h * state.layout.posY;

  ctx.save();
  ctx.globalAlpha = state.layout.opacity;
  ctx.translate(cx, cy);
  if (state.mirror) ctx.scale(-1, 1);

  ctx.fillStyle = '#ffffff';
  const pad = Math.max(4, qrSize * 0.04);
  ctx.fillRect(-qrSize / 2 - pad, -qrSize / 2 - pad, qrSize + pad * 2, qrSize + pad * 2);
  ctx.imageSmoothingEnabled = false;
  ctx.drawImage(tile, -qrSize / 2, -qrSize / 2, qrSize, qrSize);
  ctx.restore();
}
