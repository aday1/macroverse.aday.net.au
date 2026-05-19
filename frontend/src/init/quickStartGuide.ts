import { isVjViewOnlyMode } from '../vjSession.js';

const STORAGE_KEY = 'macroverse-quick-start-seen-v1';

function hasSeenQuickStart(): boolean {
  try {
    return localStorage.getItem(STORAGE_KEY) === '1';
  } catch {
    return false;
  }
}

function markQuickStartSeen(): void {
  try {
    localStorage.setItem(STORAGE_KEY, '1');
  } catch {
    /* ignore */
  }
}

function shouldOfferQuickStart(): boolean {
  if (typeof window === 'undefined') return false;
  const params = new URLSearchParams(window.location.search);
  if (params.get('quickstart') === '0') return false;
  if (params.get('quickstart') === '1') return true;
  if (isVjViewOnlyMode()) return false;
  if (hasSeenQuickStart()) return false;
  return true;
}

function openFullHelp(): void {
  document.getElementById('sidebarHelpBtn')?.click();
}

export function showQuickStartGuide(force = false): void {
  if (!force && !shouldOfferQuickStart()) return;

  const existing = document.getElementById('quickStartOverlay');
  if (existing) existing.remove();

  const overlay = document.createElement('div');
  overlay.id = 'quickStartOverlay';
  overlay.className = 'quick-start-overlay';
  overlay.setAttribute('role', 'dialog');
  overlay.setAttribute('aria-modal', 'true');
  overlay.setAttribute('aria-labelledby', 'quickStartTitle');

  const box = document.createElement('div');
  box.className = 'quick-start-box';
  box.onclick = (e) => e.stopPropagation();

  const header = document.createElement('div');
  header.className = 'quick-start-header';
  const title = document.createElement('h2');
  title.id = 'quickStartTitle';
  title.className = 'quick-start-title';
  title.textContent = 'Quick start';
  const subtitle = document.createElement('p');
  subtitle.className = 'quick-start-subtitle';
  subtitle.textContent = 'New here? This machine has not opened Macroverse before.';
  header.appendChild(title);
  header.appendChild(subtitle);

  const body = document.createElement('div');
  body.className = 'quick-start-body';
  body.innerHTML = `
<ol class="quick-start-steps">
  <li><b>Shader library</b> (left) — click any shader to preview it in the center.</li>
  <li><b>Views</b> — Preview, Code, Split, <b>VJ</b> deck, or Gallery from the tab bar or More menu.</li>
  <li><b>Parameters</b> (right) — sliders for the active shader. <kbd>Ctrl+S</kbd> saves.</li>
  <li><b>Command palette</b> — <kbd>Ctrl+K</kbd> (or <kbd>/</kbd>) jumps to actions, views, and tools.</li>
  <li><b>VJ shows</b> — VJ tab for live mixing. Settings: <b>VJ Show Session ID</b> for multi-device sync; optional audience stream QR and co-VJ collaboration links.</li>
  <li><b>Wire export</b> — expose magic numbers as ISF sliders, then Clipboard to Wire for Resolume.</li>
</ol>
<p class="quick-start-note">Tip: M42 logo opens Settings (shader folders, themes, session). Sidebar <b>?</b> or More → Help has the full manual.</p>`;

  const footer = document.createElement('div');
  footer.className = 'quick-start-footer';

  const primaryBtn = document.createElement('button');
  primaryBtn.type = 'button';
  primaryBtn.className = 'quick-start-btn quick-start-btn-primary';
  primaryBtn.textContent = 'Got it — start exploring';
  primaryBtn.onclick = () => {
    markQuickStartSeen();
    overlay.remove();
  };

  const helpBtn = document.createElement('button');
  helpBtn.type = 'button';
  helpBtn.className = 'quick-start-btn';
  helpBtn.textContent = 'Open full help';
  helpBtn.onclick = () => {
    markQuickStartSeen();
    overlay.remove();
    openFullHelp();
  };

  const skipBtn = document.createElement('button');
  skipBtn.type = 'button';
  skipBtn.className = 'quick-start-btn quick-start-btn-ghost';
  skipBtn.textContent = 'Dismiss without saving';
  skipBtn.onclick = () => overlay.remove();

  footer.appendChild(primaryBtn);
  footer.appendChild(helpBtn);
  footer.appendChild(skipBtn);

  box.appendChild(header);
  box.appendChild(body);
  box.appendChild(footer);
  overlay.appendChild(box);

  overlay.onclick = (e) => {
    if (e.target === overlay) overlay.remove();
  };

  document.body.appendChild(overlay);
  primaryBtn.focus();
}

export function maybeShowQuickStartGuide(): void {
  if (!shouldOfferQuickStart()) return;
  window.setTimeout(() => showQuickStartGuide(false), 400);
}
