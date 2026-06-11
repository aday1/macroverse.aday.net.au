import {
  buildGigVrAudienceUrl,
  buildGigVrControllerUrl,
} from './gigQr.js';
import { getVjSessionId, isVjViewOnlyMode } from './vjSession.js';
import { ensureVjTokens } from './vjTokens.js';

const VR_MODE_KEY = 'macroverse-vr-display-mode';

export type VrDisplayMode = 'dome' | 'screen';

export function getStoredVrDisplayMode(): VrDisplayMode {
  try {
    return localStorage.getItem(VR_MODE_KEY) === 'screen' ? 'screen' : 'dome';
  } catch {
    return 'dome';
  }
}

function saveVrDisplayMode(mode: VrDisplayMode): void {
  try {
    localStorage.setItem(VR_MODE_KEY, mode);
  } catch {
    /* ignore */
  }
}

function isQuestBrowser(): boolean {
  return /Quest|OculusBrowser/i.test(navigator.userAgent);
}

function openVrUrl(url: string): void {
  if (isQuestBrowser()) {
    window.location.href = url;
    return;
  }
  window.open(url, '_blank', 'noopener');
}

async function navigateVr(role: 'audience' | 'vj', mode: VrDisplayMode): Promise<void> {
  const sid = getVjSessionId();
  try {
    await ensureVjTokens(sid);
  } catch {
    /* fallback sessionId URLs */
  }
  const url =
    role === 'vj'
      ? buildGigVrControllerUrl(sid, mode)
      : buildGigVrAudienceUrl(sid, mode);
  openVrUrl(url);
}

export async function openVrRole(role: 'audience' | 'vj', mode: VrDisplayMode = getStoredVrDisplayMode()): Promise<void> {
  await navigateVr(role, mode);
}

export function openJumpIntoVrChooser(): void {
  if (isVjViewOnlyMode()) return;

  const existing = document.getElementById('jumpIntoVrOverlay');
  if (existing) existing.remove();

  let mode: VrDisplayMode = getStoredVrDisplayMode();

  const overlay = document.createElement('div');
  overlay.id = 'jumpIntoVrOverlay';
  overlay.className = 'jump-vr-overlay';
  overlay.setAttribute('role', 'dialog');
  overlay.setAttribute('aria-modal', 'true');
  overlay.setAttribute('aria-label', 'Jump into VR');

  const panel = document.createElement('div');
  panel.className = 'jump-vr-panel';

  const head = document.createElement('div');
  head.className = 'jump-vr-head';
  head.innerHTML =
    '<div class="jump-vr-title">Jump into VR</div>' +
    '<button type="button" class="jump-vr-close" aria-label="Close">Close</button>';

  const sessionLine = document.createElement('p');
  sessionLine.className = 'jump-vr-session';
  sessionLine.textContent = 'Session: ' + getVjSessionId();

  const hint = document.createElement('p');
  hint.className = 'jump-vr-hint';
  hint.textContent =
    'Host desk keeps running. Pick a role, then open vj-vr.html on this device or a headset.';

  const modeRow = document.createElement('div');
  modeRow.className = 'jump-vr-mode-row';
  const modeLabel = document.createElement('span');
  modeLabel.className = 'jump-vr-mode-label';
  modeLabel.textContent = 'Display';
  const domeBtn = document.createElement('button');
  domeBtn.type = 'button';
  domeBtn.className = 'jump-vr-mode-btn';
  domeBtn.textContent = '360 dome';
  const screenBtn = document.createElement('button');
  screenBtn.type = 'button';
  screenBtn.className = 'jump-vr-mode-btn';
  screenBtn.textContent = 'Cinema screen';

  const syncModeBtns = (): void => {
    domeBtn.classList.toggle('is-on', mode === 'dome');
    screenBtn.classList.toggle('is-on', mode === 'screen');
  };
  domeBtn.addEventListener('click', () => {
    mode = 'dome';
    saveVrDisplayMode(mode);
    syncModeBtns();
  });
  screenBtn.addEventListener('click', () => {
    mode = 'screen';
    saveVrDisplayMode(mode);
    syncModeBtns();
  });
  syncModeBtns();
  modeRow.appendChild(modeLabel);
  modeRow.appendChild(domeBtn);
  modeRow.appendChild(screenBtn);

  const actions = document.createElement('div');
  actions.className = 'jump-vr-actions';

  const audienceBtn = document.createElement('button');
  audienceBtn.type = 'button';
  audienceBtn.className = 'jump-vr-action jump-vr-action--primary';
  audienceBtn.textContent = 'Audience VR';
  audienceBtn.title = 'Immersive live mix — view what the crowd sees';
  audienceBtn.addEventListener('click', () => {
    overlay.remove();
    void openVrRole('audience', mode);
  });

  const vjBtn = document.createElement('button');
  vjBtn.type = 'button';
  vjBtn.className = 'jump-vr-action';
  vjBtn.textContent = 'VJ controller VR';
  vjBtn.title = 'Remote desk in headset — crossfader, decks, live code';
  vjBtn.addEventListener('click', () => {
    overlay.remove();
    void openVrRole('vj', mode);
  });

  actions.appendChild(audienceBtn);
  actions.appendChild(vjBtn);

  panel.appendChild(head);
  panel.appendChild(sessionLine);
  panel.appendChild(hint);
  panel.appendChild(modeRow);
  panel.appendChild(actions);
  overlay.appendChild(panel);

  const close = (): void => overlay.remove();
  head.querySelector('.jump-vr-close')?.addEventListener('click', close);
  overlay.addEventListener('click', (ev) => {
    if (ev.target === overlay) close();
  });
  document.addEventListener(
    'keydown',
    function onKey(ev) {
      if (ev.key === 'Escape') {
        close();
        document.removeEventListener('keydown', onKey);
      }
    },
    { once: false }
  );

  document.body.appendChild(overlay);
}
