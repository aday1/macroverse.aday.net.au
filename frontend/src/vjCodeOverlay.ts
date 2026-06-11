const STORAGE_KEY = 'macroverse-vj-code-overlay';

export interface CodeOverlayState {
  visible: boolean;
  opacity: number;
  scrollLine: number;
  source: string;
}

const DEFAULT_STATE: CodeOverlayState = {
  visible: false,
  opacity: 0.85,
  scrollLine: 0,
  source: '',
};

function clamp01(v: number): number {
  return Math.max(0, Math.min(1, v));
}

function loadState(): CodeOverlayState {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return { ...DEFAULT_STATE };
    const p = JSON.parse(raw) as Partial<CodeOverlayState>;
    return {
      visible: p.visible === true,
      opacity: clamp01(Number(p.opacity ?? DEFAULT_STATE.opacity)),
      scrollLine: Math.max(0, Math.floor(Number(p.scrollLine ?? 0))),
      source: typeof p.source === 'string' ? p.source : '',
    };
  } catch {
    return { ...DEFAULT_STATE };
  }
}

function saveState(state: CodeOverlayState): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  } catch {
    /* ignore */
  }
}

let overlayState = loadState();

function dispatchChange(): void {
  if (typeof window === 'undefined') return;
  window.dispatchEvent(
    new CustomEvent('macroverse-vj-code-overlay-changed', {
      detail: { state: { ...overlayState } },
    })
  );
}

export function getCodeOverlayState(): Readonly<CodeOverlayState> {
  return overlayState;
}

export function setCodeOverlayVisible(visible: boolean): void {
  overlayState = { ...overlayState, visible };
  saveState(overlayState);
  dispatchChange();
}

export function setCodeOverlayOpacity(opacity: number): void {
  overlayState = { ...overlayState, opacity: clamp01(opacity) };
  saveState(overlayState);
  dispatchChange();
}

export function setCodeOverlayScrollLine(scrollLine: number): void {
  overlayState = { ...overlayState, scrollLine: Math.max(0, Math.floor(scrollLine)) };
  saveState(overlayState);
  dispatchChange();
}

export function setCodeOverlaySource(source: string): void {
  overlayState = { ...overlayState, source };
  saveState(overlayState);
  dispatchChange();
}

const FONT = '14px "Consolas", "Courier New", monospace';
const LINE_HEIGHT = 18;
const PAD = 12;

/** Green monospace CRT-style code overlay on black. */
export function renderCodeOverlayCanvas(
  canvas: HTMLCanvasElement,
  text: string,
  scrollLine: number,
  opacity: number
): void {
  const w = canvas.width;
  const h = canvas.height;
  if (w <= 0 || h <= 0) return;

  const ctx = canvas.getContext('2d');
  if (!ctx) return;

  ctx.clearRect(0, 0, w, h);
  if (opacity <= 0.001 || !text.trim()) return;

  ctx.save();
  ctx.globalAlpha = clamp01(opacity);
  ctx.fillStyle = '#000000';
  ctx.fillRect(0, 0, w, h);

  const lines = text.replace(/\r\n/g, '\n').split('\n');
  const start = Math.max(0, Math.min(scrollLine, Math.max(0, lines.length - 1)));
  const visibleLines = Math.ceil((h - PAD * 2) / LINE_HEIGHT) + 1;

  ctx.font = FONT;
  ctx.textBaseline = 'top';
  ctx.fillStyle = '#33ff66';
  ctx.shadowColor = 'rgba(51, 255, 102, 0.35)';
  ctx.shadowBlur = 4;

  for (let i = 0; i < visibleLines; i++) {
    const lineIdx = start + i;
    if (lineIdx >= lines.length) break;
    const y = PAD + i * LINE_HEIGHT;
    ctx.fillText(lines[lineIdx], PAD, y);
  }

  ctx.shadowBlur = 0;
  ctx.fillStyle = 'rgba(51, 255, 102, 0.08)';
  for (let y = 0; y < h; y += 4) {
    ctx.fillRect(0, y, w, 2);
  }

  ctx.restore();
}

export function renderCodeOverlayFromState(canvas: HTMLCanvasElement): void {
  const s = overlayState;
  if (!s.visible) {
    const ctx = canvas.getContext('2d');
    ctx?.clearRect(0, 0, canvas.width, canvas.height);
    return;
  }
  renderCodeOverlayCanvas(canvas, s.source, s.scrollLine, s.opacity);
}
