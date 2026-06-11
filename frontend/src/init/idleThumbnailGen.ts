import { postThumbnailSave, fetchShader } from '../api.js';
import { entries, getThumbnail, setThumbnail } from '../state.js';
import { renderThumbnailSync } from '../thumbnailRenderer.js';

const IDLE_TIMEOUT_MS = 3500;
const IDLE_MIN_BUDGET_MS = 10;
const BETWEEN_THUMBS_MS = 120;

let queue: string[] = [];
const queued = new Set<string>();
let pumping = false;
let pumpTimer: ReturnType<typeof setTimeout> | null = null;
let abortController: AbortController | null = null;
let generating = false;

function waitForSpareCycle(): Promise<boolean> {
  return new Promise((resolve) => {
    if (document.hidden) {
      resolve(false);
      return;
    }
    const scheduling = (navigator as Navigator & { scheduling?: { isInputPending?: () => boolean } }).scheduling;
    if (typeof requestIdleCallback !== 'function') {
      window.setTimeout(() => resolve(!document.hidden), BETWEEN_THUMBS_MS);
      return;
    }
    requestIdleCallback(
      (deadline) => {
        if (document.hidden) {
          resolve(false);
          return;
        }
        if (scheduling?.isInputPending?.()) {
          resolve(false);
          return;
        }
        if (deadline.timeRemaining() < IDLE_MIN_BUDGET_MS) {
          resolve(false);
          return;
        }
        resolve(true);
      },
      { timeout: IDLE_TIMEOUT_MS }
    );
  });
}

function schedulePump(delayMs = 0): void {
  if (pumpTimer) return;
  pumpTimer = window.setTimeout(() => {
    pumpTimer = null;
    void runPump();
  }, delayMs);
}

async function generateOne(path: string, signal: AbortSignal): Promise<void> {
  if (signal.aborted || getThumbnail(path)) return;
  const entry = entries.find((e) => e.path === path);
  if (!entry?.path) return;

  let src: string;
  try {
    src = await fetchShader(path);
  } catch {
    return;
  }
  if (signal.aborted || getThumbnail(path)) return;

  const ready = await waitForSpareCycle();
  if (!ready || signal.aborted) {
    enqueueIdleThumbnailGeneration([path], { front: true });
    return;
  }

  const dataUrl = renderThumbnailSync(src);
  if (!dataUrl || signal.aborted) return;

  setThumbnail(path, dataUrl);
  void postThumbnailSave({ path, dataUrl }).catch(() => {});
  window.dispatchEvent(new CustomEvent('thumbnail-captured', { detail: { path } }));
  window.dispatchEvent(new CustomEvent('thumbnails-progress'));
}

async function runPump(): Promise<void> {
  if (pumping) return;
  pumping = true;

  if (!abortController) {
    abortController = new AbortController();
    generating = true;
    window.dispatchEvent(new CustomEvent('thumbnails-generating-start'));
  }
  const signal = abortController.signal;

  try {
    while (queue.length > 0 && !signal.aborted) {
      if (document.hidden) {
        schedulePump(600);
        return;
      }

      const ready = await waitForSpareCycle();
      if (!ready) {
        schedulePump(BETWEEN_THUMBS_MS);
        return;
      }

      const path = queue.shift()!;
      queued.delete(path);
      if (!path || getThumbnail(path)) continue;

      await generateOne(path, signal);
      if (signal.aborted) break;

      schedulePump(BETWEEN_THUMBS_MS);
      return;
    }
  } finally {
    pumping = false;
    if (queue.length > 0 && !signal.aborted) {
      schedulePump(BETWEEN_THUMBS_MS);
      return;
    }
    if (queue.length === 0 && !signal.aborted) {
      abortController = null;
      generating = false;
      window.dispatchEvent(new CustomEvent('thumbnails-generating-done'));
    }
  }
}

/** Queue thumbnail renders for spare CPU cycles. Use front:true for on-screen carousel cards. */
export function enqueueIdleThumbnailGeneration(paths: string[], opts?: { front?: boolean }): void {
  const missing = paths.filter((p) => p && !getThumbnail(p));
  if (missing.length === 0) return;

  if (opts?.front) {
    const toFront: string[] = [];
    for (const p of missing) {
      if (queued.has(p)) {
        const idx = queue.indexOf(p);
        if (idx >= 0) queue.splice(idx, 1);
      } else {
        queued.add(p);
      }
      toFront.push(p);
    }
    queue.unshift(...toFront);
  } else {
    for (const p of missing) {
      if (queued.has(p)) continue;
      queued.add(p);
      queue.push(p);
    }
  }

  schedulePump(BETWEEN_THUMBS_MS);
}

export function cancelIdleThumbnailGeneration(): void {
  if (pumpTimer) {
    clearTimeout(pumpTimer);
    pumpTimer = null;
  }
  queue = [];
  queued.clear();
  if (abortController) {
    abortController.abort();
    abortController = null;
  }
  pumping = false;
  if (generating) {
    generating = false;
    window.dispatchEvent(new CustomEvent('thumbnails-generating-done'));
  }
}

export function isIdleThumbnailGenerating(): boolean {
  return generating || queue.length > 0 || pumping;
}

if (typeof document !== 'undefined') {
  document.addEventListener('visibilitychange', () => {
    if (!document.hidden && queue.length > 0) schedulePump(200);
  });
}
