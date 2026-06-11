import { buildGigJoinUrl, drawGigQr } from '../gigQr.js';
import { openGigQrHub } from '../gigQrHub.js';
import { openVrRole } from '../jumpIntoVr.js';
import { getVjSessionId } from '../vjSession.js';
import { ensureVjTokens } from '../vjTokens.js';
import { isVjWsConnected, reconnectVjSession } from '../vjWs.js';

let panelOpen = false;
let qrCanvas: HTMLCanvasElement | null = null;
let labelEl: HTMLElement | null = null;
let dotEl: HTMLElement | null = null;
let rootEl: HTMLElement | null = null;

function viewportMargin(): number {
  return 8;
}

function resetPanelPosition(panel: HTMLElement): void {
  panel.classList.remove('is-fixed', 'is-flip-up');
  panel.style.top = '';
  panel.style.right = '';
  panel.style.left = '';
  panel.style.bottom = '';
  panel.style.maxHeight = '';
}

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
  if (panelOpen) {
    window.requestAnimationFrame(() => positionPanel());
  }
}

function positionPanel(): void {
  const panel = document.getElementById('appBarVjSessionPanel');
  const toggle = document.getElementById('appBarVjSessionToggle');
  if (!panel || !toggle || panel.hidden) return;

  const margin = viewportMargin();
  const vh = window.innerHeight;
  const vw = window.innerWidth;
  const tr = toggle.getBoundingClientRect();

  resetPanelPosition(panel);
  panel.classList.add('is-fixed');

  panel.style.right = `${Math.max(margin, vw - tr.right)}px`;
  panel.style.left = 'auto';

  const openBelowTop = tr.bottom + 4;
  panel.style.top = `${openBelowTop}px`;

  let rect = panel.getBoundingClientRect();
  const spaceBelow = vh - margin - openBelowTop;
  const spaceAbove = tr.top - margin - 4;

  if (rect.bottom > vh - margin) {
    const flipTop = tr.top - rect.height - 4;
    if (flipTop >= margin && rect.height <= spaceAbove + 4) {
      panel.style.top = `${Math.max(margin, flipTop)}px`;
      panel.classList.add('is-flip-up');
    } else if (spaceBelow >= spaceAbove && spaceBelow >= 120) {
      panel.style.maxHeight = `${Math.max(120, spaceBelow)}px`;
    } else if (spaceAbove >= 120) {
      panel.style.top = `${margin}px`;
      panel.style.maxHeight = `${Math.max(120, tr.top - margin - 4)}px`;
      panel.classList.add('is-flip-up');
    } else {
      panel.style.top = `${margin}px`;
      panel.style.maxHeight = `${Math.max(120, vh - margin * 2)}px`;
    }
  } else {
    panel.style.maxHeight = `${Math.max(120, vh - openBelowTop - margin)}px`;
  }

  rect = panel.getBoundingClientRect();
  if (rect.left < margin) {
    panel.style.right = `${Math.max(margin, vw - rect.width - margin)}px`;
  }
  if (rect.right > vw - margin) {
    panel.style.right = `${margin}px`;
  }
}

function setPanelOpen(open: boolean): void {
  panelOpen = open;
  const panel = document.getElementById('appBarVjSessionPanel');
  const toggle = document.getElementById('appBarVjSessionToggle');
  if (panel) {
    panel.hidden = !open;
    if (!open) resetPanelPosition(panel);
  }
  if (rootEl) rootEl.classList.toggle('is-open', open);
  if (toggle) toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
  if (open) {
    void refreshQr().then(() => {
      window.requestAnimationFrame(() => positionPanel());
    });
  }
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
  const allQrBtn = document.getElementById('appBarVjAllQrBtn');
  const vrAudienceBtn = document.getElementById('appBarVjVrAudience');
  const vrControllerBtn = document.getElementById('appBarVjVrController');

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

  allQrBtn?.addEventListener('click', (ev) => {
    ev.stopPropagation();
    setPanelOpen(false);
    openGigQrHub();
  });

  vrAudienceBtn?.addEventListener('click', (ev) => {
    ev.stopPropagation();
    setPanelOpen(false);
    void openVrRole('audience');
  });

  vrControllerBtn?.addEventListener('click', (ev) => {
    ev.stopPropagation();
    setPanelOpen(false);
    void openVrRole('vj');
  });

  document.addEventListener('click', closeOnOutsideClick);
  document.addEventListener('keydown', (ev) => {
    if (ev.key === 'Escape') setPanelOpen(false);
  });
  window.addEventListener('resize', () => {
    if (panelOpen) positionPanel();
  });
  window.addEventListener('scroll', () => {
    if (panelOpen) positionPanel();
  }, true);

  window.addEventListener('macroverse-vj-session-changed', () => {
    void refreshQr();
    if (!panelOpen) void reconnectVjSession();
  });

  setInterval(() => {
    if (dotEl) dotEl.classList.toggle('is-live', isVjWsConnected());
  }, 2000);
}
