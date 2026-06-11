import { drawGigQr } from '../gigQr.js';
import { copyGigUrl, GIG_QR_ITEMS, SPLASH_QR_CANVAS_IDS } from '../gigQrItems.js';
import { hideSplash } from '../dom.js';
import { getVjSessionId, isVjViewOnlyMode } from '../vjSession.js';
import { ensureVjTokens } from '../vjTokens.js';
import { isCompactLayout } from './layoutTier.js';

const MIN_SPLASH_MS = 6500;
const MIN_SPLASH_MS_COMPACT = 2800;

let refreshTimer: ReturnType<typeof setTimeout> | null = null;
let resizeTimer: ReturnType<typeof setTimeout> | null = null;
let dismissTimer: ReturnType<typeof setTimeout> | null = null;
const splashStartedAt = performance.now();
let splashQrsAttempted = false;
let splashDismissRequested = false;
let lastSplashQrUrls = '';
let splashQrFoldInited = false;

function setSplashStatus(text: string): void {
  const el = document.getElementById('splashVjQrStatus');
  if (el) el.textContent = text;
}

function usesCompactSplashQr(): boolean {
  return isCompactLayout();
}

function isSplashQrExpanded(): boolean {
  const fold = document.getElementById('splashQrFold');
  return !fold?.classList.contains('splash-qr-fold--collapsed');
}

function splashQrPixelSize(): number {
  const canvas = document.getElementById('splashVjQrJoin') as HTMLCanvasElement | null;
  if (canvas) {
    const rect = canvas.getBoundingClientRect();
    if (rect.width > 8 && rect.height > 8) {
      return Math.max(128, Math.min(640, Math.round(Math.min(rect.width, rect.height) - 12)));
    }
  }
  const block = document.querySelector('.splash-vj-qr-block') as HTMLElement | null;
  if (block) {
    const bw = block.clientWidth;
    const bh = block.clientHeight - 28;
    if (bw > 0 && bh > 0) {
      return Math.max(128, Math.min(640, Math.round(Math.min(bw, bh))));
    }
  }
  const cap = usesCompactSplashQr() ? 0.28 : 0.36;
  return Math.max(
    128,
    Math.min(640, Math.round(Math.min(window.innerWidth * cap, window.innerHeight * 0.42)))
  );
}

function minSplashMs(): number {
  return usesCompactSplashQr() ? MIN_SPLASH_MS_COMPACT : MIN_SPLASH_MS;
}

function tryDismissSplash(force = false): void {
  if (!splashDismissRequested) return;
  if (!force && !splashQrsAttempted && (!usesCompactSplashQr() || isSplashQrExpanded())) return;
  const elapsed = performance.now() - splashStartedAt;
  const wait = force ? 0 : Math.max(0, minSplashMs() - elapsed);
  if (dismissTimer) clearTimeout(dismissTimer);
  if (wait <= 0) {
    hideSplash();
    return;
  }
  dismissTimer = setTimeout(() => hideSplash(), wait);
}

/** Auto-dismiss after index load — keeps splash up briefly so QRs can render. */
export function requestSplashDismiss(): void {
  splashDismissRequested = true;
  if (usesCompactSplashQr() && !isSplashQrExpanded()) {
    splashQrsAttempted = true;
  }
  tryDismissSplash(false);
}

/** User clicked the splash — dismiss immediately. */
export function dismissSplashByUser(): void {
  splashDismissRequested = true;
  if (dismissTimer) clearTimeout(dismissTimer);
  hideSplash();
}

function setSplashQrExpanded(expanded: boolean): void {
  const fold = document.getElementById('splashQrFold');
  const toggle = document.getElementById('splashVjQrToggle');
  if (!fold || !toggle) return;
  fold.classList.toggle('splash-qr-fold--collapsed', !expanded);
  toggle.setAttribute('aria-expanded', expanded ? 'true' : 'false');
  toggle.textContent = expanded ? 'Hide gig QR codes' : 'Show gig QR codes (4)';
  if (expanded) {
    void drawSplashQrs();
  }
}

function isSplashQrControlTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false;
  return !!target.closest('.splash-vj-qr-copy, .splash-qr-toggle');
}

/** Click/tap backdrop to dismiss; copy + fold toggle stay interactive. */
export function initSplashDismiss(): void {
  const splash = document.getElementById('splashOverlay');
  if (!splash || splash.dataset.dismissBound === '1') return;
  splash.dataset.dismissBound = '1';

  const onDismissIntent = (ev: Event): void => {
    if (splash.classList.contains('hidden')) return;
    if (isSplashQrControlTarget(ev.target)) return;
    ev.preventDefault();
    dismissSplashByUser();
  };

  splash.addEventListener('click', onDismissIntent, true);
  splash.addEventListener('pointerdown', onDismissIntent, true);
  splash.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      dismissSplashByUser();
    }
  });
}

function initSplashCopyButtons(): void {
  for (const item of GIG_QR_ITEMS) {
    const canvasId = SPLASH_QR_CANVAS_IDS[item.id];
    if (!canvasId) continue;
    const block = document.querySelector(`.splash-vj-qr-block[data-gig-qr-id="${item.id}"]`) as HTMLElement | null;
    if (!block) continue;
    let copyBtn = block.querySelector('.splash-vj-qr-copy') as HTMLButtonElement | null;
    if (!copyBtn) {
      copyBtn = document.createElement('button');
      copyBtn.type = 'button';
      copyBtn.className = 'splash-vj-qr-copy';
      copyBtn.textContent = 'Copy link';
      block.appendChild(copyBtn);
    }
    copyBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      copyGigUrl(item.buildUrl(getVjSessionId()), copyBtn!);
    });
  }
}

function initSplashQrFold(): void {
  if (splashQrFoldInited) return;
  const fold = document.getElementById('splashQrFold');
  const toggle = document.getElementById('splashVjQrToggle');
  const row = document.getElementById('splashVjQrRow');
  if (!fold || !toggle || !row) return;
  splashQrFoldInited = true;

  initSplashCopyButtons();

  if (usesCompactSplashQr()) {
    fold.classList.add('splash-qr-fold--collapsed');
    toggle.setAttribute('aria-expanded', 'false');
    toggle.textContent = 'Show gig QR codes (4)';
  } else {
    fold.classList.remove('splash-qr-fold--collapsed');
    toggle.setAttribute('aria-expanded', 'true');
    toggle.textContent = 'Hide gig QR codes';
  }

  toggle.addEventListener('click', (e) => {
    e.stopPropagation();
    setSplashQrExpanded(fold.classList.contains('splash-qr-fold--collapsed'));
  });
}

function splashUrlsKey(sid: string, px: number): string {
  return GIG_QR_ITEMS.map((item) => item.buildUrl(sid)).concat(String(px)).join('\n');
}

async function drawSplashQrs(options?: { mintTokens?: boolean }): Promise<boolean> {
  if (usesCompactSplashQr() && !isSplashQrExpanded()) {
    splashQrsAttempted = true;
    if (splashDismissRequested) tryDismissSplash(false);
    return false;
  }

  const row = document.getElementById('splashVjQrRow');
  if (!row) return false;

  const sid = getVjSessionId();
  const px = splashQrPixelSize();
  const urlKey = splashUrlsKey(sid, px);
  const resizeOnly = !options?.mintTokens && urlKey === lastSplashQrUrls;

  const canvases = GIG_QR_ITEMS.map((item) => {
    const id = SPLASH_QR_CANVAS_IDS[item.id];
    return id ? (document.getElementById(id) as HTMLCanvasElement | null) : null;
  });
  if (canvases.some((c) => !c)) return false;

  if (resizeOnly) {
    try {
      await Promise.all(
        GIG_QR_ITEMS.map((item, i) => drawGigQr(canvases[i]!, item.buildUrl(sid), px))
      );
      return true;
    } catch {
      return false;
    }
  }

  setSplashStatus('Preparing session links…');

  if (options?.mintTokens !== false) {
    try {
      await ensureVjTokens(sid);
    } catch {
      /* draw with sessionId fallback URLs */
    }
  }

  try {
    await Promise.all(
      GIG_QR_ITEMS.map((item, i) => drawGigQr(canvases[i]!, item.buildUrl(sid), px))
    );
    lastSplashQrUrls = splashUrlsKey(sid, px);
    for (const c of canvases) {
      if (c) {
        c.style.width = '';
        c.style.height = '';
      }
    }
    row.classList.remove('is-pending');
    row.classList.add('is-ready');
    setSplashStatus('');
    return true;
  } catch {
    row.classList.add('is-pending');
    row.classList.remove('is-ready');
    setSplashStatus('Could not draw QRs yet — retrying…');
    return false;
  } finally {
    splashQrsAttempted = true;
    if (splashDismissRequested) tryDismissSplash(false);
  }
}

function scheduleRetry(): void {
  if (refreshTimer) return;
  refreshTimer = setTimeout(() => {
    refreshTimer = null;
    void drawSplashQrs().then((ok) => {
      if (!ok) scheduleRetry();
    });
  }, 1500);
}

export function initSplashVjQr(): void {
  if (isVjViewOnlyMode()) return;
  initSplashDismiss();
  const row = document.getElementById('splashVjQrRow');
  if (!row) return;

  initSplashQrFold();

  const startDraw = () => {
    void drawSplashQrs().then((ok) => {
      if (!ok && (!usesCompactSplashQr() || isSplashQrExpanded())) scheduleRetry();
    });
  };
  requestAnimationFrame(() => requestAnimationFrame(startDraw));

  if (typeof ResizeObserver !== 'undefined') {
    const ro = new ResizeObserver(() => {
      if (document.getElementById('splashOverlay')?.classList.contains('hidden')) return;
      if (usesCompactSplashQr() && !isSplashQrExpanded()) return;
      if (resizeTimer) clearTimeout(resizeTimer);
      resizeTimer = setTimeout(() => {
        resizeTimer = null;
        void drawSplashQrs({ mintTokens: false });
      }, 200);
    });
    ro.observe(row);
  }

  window.addEventListener('macroverse-vj-session-changed', () => {
    if (document.getElementById('splashOverlay')?.classList.contains('hidden')) return;
    lastSplashQrUrls = '';
    void drawSplashQrs({ mintTokens: true });
  });
}

export function refreshSplashVjQr(): void {
  if (isVjViewOnlyMode()) return;
  void drawSplashQrs();
}
