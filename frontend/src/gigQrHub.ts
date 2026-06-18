import { drawGigQr } from './gigQr.js';
import { copyGigUrl, GIG_QR_ITEMS } from './gigQrItems.js';
import { getVjSessionId, isVjViewOnlyMode } from './vjSession.js';
import { ensureVjTokens } from './vjTokens.js';

export { GIG_QR_ITEMS as GIG_QR_HUB_ITEMS } from './gigQrItems.js';
export type { GigQrItem as GigQrHubItem } from './gigQrItems.js';

let hubEl: HTMLElement | null = null;

async function refreshHubQrs(root: HTMLElement, sessionId: string): Promise<void> {
  try {
    await ensureVjTokens(sessionId);
  } catch {
    /* fallback URLs */
  }
  for (const item of GIG_QR_ITEMS) {
    const canvas = root.querySelector(`[data-gig-qr-id="${item.id}"]`) as HTMLCanvasElement | null;
    if (!canvas) continue;
    await drawGigQr(canvas, item.buildUrl(sessionId), 160);
  }
}

function closeHub(): void {
  if (!hubEl) return;
  hubEl.hidden = true;
  document.body.classList.remove('gig-qr-hub-open');
}

function buildHubDom(): HTMLElement {
  const overlay = document.createElement('div');
  overlay.id = 'gigQrHubOverlay';
  overlay.className = 'gig-qr-hub-overlay';
  overlay.hidden = true;
  overlay.setAttribute('role', 'dialog');
  overlay.setAttribute('aria-modal', 'true');
  overlay.setAttribute('aria-label', 'Show QR codes');

  const panel = document.createElement('div');
  panel.className = 'gig-qr-hub-panel';

  const head = document.createElement('div');
  head.className = 'gig-qr-hub-head';
  head.innerHTML =
    '<div class="gig-qr-hub-title">Show QR codes</div>' +
    '<button type="button" class="gig-qr-hub-close" aria-label="Close">Close</button>';

  const sessionLine = document.createElement('p');
  sessionLine.className = 'gig-qr-hub-session';
  sessionLine.id = 'gigQrHubSession';

  const grid = document.createElement('div');
  grid.className = 'gig-qr-hub-grid';

  for (const item of GIG_QR_ITEMS) {
    const card = document.createElement('div');
    card.className = 'gig-qr-hub-card';

    const label = document.createElement('div');
    label.className = 'gig-qr-hub-label';
    label.textContent = item.label;

    const hint = document.createElement('div');
    hint.className = 'gig-qr-hub-hint';
    hint.textContent = item.hint;

    if (item.id === 'vr-audience' || item.id === 'vr-vj') {
      const vrDev = document.createElement('div');
      vrDev.className = 'gig-qr-hub-vr-dev';
      vrDev.setAttribute('role', 'status');
      vrDev.textContent = 'VR SUPPORT UNDER DEVELOPMENT';
      card.appendChild(label);
      card.appendChild(vrDev);
      card.appendChild(hint);
    } else {
      card.appendChild(label);
      card.appendChild(hint);
    }

    const canvas = document.createElement('canvas');
    canvas.width = 160;
    canvas.height = 160;
    canvas.dataset.gigQrId = item.id;

    const copyBtn = document.createElement('button');
    copyBtn.type = 'button';
    copyBtn.className = 'gig-qr-hub-copy';
    copyBtn.textContent = 'Copy link';
    copyBtn.addEventListener('click', () => {
      copyGigUrl(item.buildUrl(getVjSessionId()), copyBtn);
    });

    card.appendChild(canvas);
    card.appendChild(copyBtn);
    grid.appendChild(card);
  }

  const foot = document.createElement('p');
  foot.className = 'gig-qr-hub-foot';
  foot.textContent =
    'Cannot scan? Use Copy link on each card. VJ tab: Jump into VR or stream QR. Settings: VJ Show Session ID.';

  panel.appendChild(head);
  panel.appendChild(sessionLine);
  panel.appendChild(grid);
  panel.appendChild(foot);
  overlay.appendChild(panel);

  head.querySelector('.gig-qr-hub-close')?.addEventListener('click', () => closeHub());
  overlay.addEventListener('click', (ev) => {
    if (ev.target === overlay) closeHub();
  });

  return overlay;
}

export function openGigQrHub(): void {
  if (isVjViewOnlyMode()) return;
  if (!hubEl) {
    hubEl = buildHubDom();
    document.body.appendChild(hubEl);
    document.addEventListener('keydown', (ev) => {
      if (ev.key === 'Escape' && hubEl && !hubEl.hidden) closeHub();
    });
    window.addEventListener('macroverse-vj-session-changed', () => {
      if (hubEl && !hubEl.hidden) void refreshHubQrs(hubEl, getVjSessionId());
    });
  }
  const sid = getVjSessionId();
  const sessionLine = hubEl.querySelector('#gigQrHubSession');
  if (sessionLine) sessionLine.textContent = 'Session: ' + sid;
  hubEl.hidden = false;
  document.body.classList.add('gig-qr-hub-open');
  void refreshHubQrs(hubEl, sid);
}
