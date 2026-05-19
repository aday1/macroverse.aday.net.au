import { buildGigJoinUrl, drawGigQr } from '../gigQr.js';
import { getVjSessionId } from '../vjSession.js';
import { ensureVjTokens } from '../vjTokens.js';
import { isVjWsConnected, reconnectVjSession } from '../vjWs.js';

let panelOpen = false;
let qrCanvas: HTMLCanvasElement | null = null;
let labelEl: HTMLElement | null = null;
let dotEl: HTMLElement | null = null;
let rootEl: HTMLElement | null = null;

async function refreshQr(): Promise<void> {
  const sid = getVjSessionId();
  if (labelEl) labelEl.textContent = sid;
  const idDisplay = document.getElementById('appBarVjSessionIdDisplay');
  if (idDisplay) idDisplay.textContent = sid;
  try {
    await ensureVjTokens(sid);
    if (qrCanvas) await drawGigQr(qrCanvas, buildGigJoinUrl(sid));
  } catch {
    if (qrCanvas) {
      const ctx = qrCanvas.getContext('2d');
      ctx?.clearRect(0, 0, qrCanvas.width, qrCanvas.height);
    }
  }
  if (dotEl) {
    dotEl.classList.toggle('is-live', isVjWsConnected());
  }
}

function setPanelOpen(open: boolean): void {
  panelOpen = open;
  const panel = document.getElementById('appBarVjSessionPanel');
  const toggle = document.getElementById('appBarVjSessionToggle');
  if (panel) panel.hidden = !open;
  if (rootEl) rootEl.classList.toggle('is-open', open);
  if (toggle) toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
  if (open) void refreshQr();
}

function closeOnOutsideClick(ev: MouseEvent): void {
  if (!panelOpen || !rootEl) return;
  if (!rootEl.contains(ev.target as Node)) setPanelOpen(false);
}

export function initVjAppBarSession(): void {
  rootEl = document.getElementById('appBarVjSession');
  const toggle = document.getElementById('appBarVjSessionToggle');
  labelEl = document.getElementById('appBarVjSessionLabel');
  dotEl = document.getElementById('appBarVjSessionDot');
  qrCanvas = document.getElementById('appBarVjJoinQr') as HTMLCanvasElement | null;
  const copyBtn = document.getElementById('appBarVjJoinCopy');

  if (!rootEl || !toggle) return;

  void refreshQr();

  toggle.addEventListener('click', (ev) => {
    ev.stopPropagation();
    setPanelOpen(!panelOpen);
  });

  copyBtn?.addEventListener('click', () => {
    const url = buildGigJoinUrl();
    void navigator.clipboard?.writeText(url).then(() => {
      const t = copyBtn.textContent;
      copyBtn.textContent = 'Copied';
      setTimeout(() => {
        copyBtn.textContent = t;
      }, 1500);
    });
  });

  document.addEventListener('click', closeOnOutsideClick);
  document.addEventListener('keydown', (ev) => {
    if (ev.key === 'Escape') setPanelOpen(false);
  });

  window.addEventListener('macroverse-vj-session-changed', () => {
    void refreshQr();
    if (!panelOpen) void reconnectVjSession();
  });

  setInterval(() => {
    if (dotEl) dotEl.classList.toggle('is-live', isVjWsConnected());
  }, 2000);
}
