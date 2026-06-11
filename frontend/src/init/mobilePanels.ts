/**
 * Touch / compact layout panel UX: drawer close buttons, scroll lock,
 * index toolbar defaults.
 */

import { closeAllDocks, closeDock, isDockOpen } from './dockSystem.js';
import { isCompactLayout, onLayoutTierChange } from './layoutTier.js';

function syncBodyDockScrollLock(): void {
  const open = document.body.classList.contains('dock-any-open');
  document.body.classList.toggle('mv-panel-scroll-lock', open && isCompactLayout());
}

function wireDrawerCloseButtons(): void {
  document.querySelectorAll<HTMLButtonElement>('[data-dock-close]').forEach((btn) => {
    if (btn.dataset.wired === '1') return;
    btn.dataset.wired = '1';
    btn.addEventListener('click', (e) => {
      e.stopPropagation();
      const side = btn.dataset.dockClose;
      if (side === 'left' || side === 'right') closeDock(side);
    });
  });
}

function applyIndexToolbarCompactDefault(): void {
  const toolbar = document.querySelector('.index-toolbar') as HTMLElement | null;
  const btn = document.getElementById('indexToolsToggle');
  if (!toolbar || !btn) return;
  if (!isCompactLayout()) {
    toolbar.classList.remove('expanded');
    btn.textContent = 'More tools \u2193';
    return;
  }
  if (!toolbar.classList.contains('expanded')) {
    btn.textContent = 'Filters & settings \u2193';
  }
}

export function initMobilePanels(): void {
  wireDrawerCloseButtons();

  window.addEventListener('macroverse:dock-change', syncBodyDockScrollLock);

  onLayoutTierChange(() => {
    applyIndexToolbarCompactDefault();
    if (!isCompactLayout()) {
      document.body.classList.remove('mv-panel-scroll-lock');
    } else {
      syncBodyDockScrollLock();
    }
  });

  applyIndexToolbarCompactDefault();
  syncBodyDockScrollLock();
}

/** Close any compact overlay panel (drawer, more sheet, etc.). */
export function closeCompactOverlays(): void {
  if (isDockOpen('left') || isDockOpen('right')) closeAllDocks();
  document.getElementById('moreSheet')?.classList.remove('open');
  document.body.classList.remove('gig-qr-hub-open');
  const gigHub = document.querySelector('.gig-qr-hub-overlay') as HTMLElement | null;
  if (gigHub) gigHub.hidden = true;
}
