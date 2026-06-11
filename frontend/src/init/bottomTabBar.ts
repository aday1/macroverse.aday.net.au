/**
 * Bottom tab bar (phone only).
 *
 * Five fixed tabs at the bottom of the viewport:
 *   Library | Preview | Code | Params | More
 *
 * The bar is always in the DOM; CSS hides it on viewports above
 * 640 px. "More" opens a sheet of secondary actions.
 */

import { openDock, closeAllDocks, toggleDock, isDockOpen } from './dockSystem.js';
import { openCommandPalette } from './commandPalette.js';
import { onLayoutTierChange } from './layoutTier.js';

let barEl: HTMLElement | null = null;
let moreSheetEl: HTMLElement | null = null;

function clickViewTab(view: string): void {
  const tab = document.querySelector(`.view-tab[data-view="${view}"]`) as HTMLElement | null;
  tab?.click();
}

function getActiveView(): string {
  const active = document.querySelector('.view-tab.active') as HTMLElement | null;
  return active?.dataset.view || '';
}

function syncActive(): void {
  if (!barEl) return;
  const buttons = barEl.querySelectorAll('.bottom-tab[data-tab]');
  const activeView = getActiveView();
  buttons.forEach((b) => {
    const tab = b as HTMLElement;
    const t = tab.dataset.tab;
    let on = false;
    if (t === 'library') on = isDockOpen('left');
    else if (t === 'params') on = isDockOpen('right');
    else if (t === 'preview') on = activeView === 'preview' || activeView === 'split';
    else if (t === 'code')    on = activeView === 'code';
    else on = false;
    tab.classList.toggle('active', on);
  });
}

function buildMoreSheet(): HTMLElement {
  if (moreSheetEl) return moreSheetEl;
  moreSheetEl = document.createElement('div');
  moreSheetEl.className = 'more-sheet';
  moreSheetEl.id = 'moreSheet';
  moreSheetEl.innerHTML = `
    <div class="more-sheet-scrim" data-close="1"></div>
    <div class="more-sheet-card" role="dialog" aria-modal="true" aria-label="More menu">
      <div class="more-sheet-handle" aria-hidden="true"></div>
      <div class="more-sheet-title">More</div>
      <div class="more-sheet-grid">
        <button type="button" class="more-sheet-btn" data-act="vj">&#127899; VJ deck</button>
        <button type="button" class="more-sheet-btn" data-act="gallery">&#128444; Gallery</button>
        <button type="button" class="more-sheet-btn desktop-only" data-act="pipeline">&#9889; Pipeline</button>
        <button type="button" class="more-sheet-btn desktop-only" data-act="wire">&#128268; Wire Hub</button>
        <button type="button" class="more-sheet-btn" data-act="split">&#128256; Split view</button>
        <button type="button" class="more-sheet-btn" data-act="palette">Commands &gt;_</button>
        <button type="button" class="more-sheet-btn" data-act="settings">Settings</button>
        <button type="button" class="more-sheet-btn" data-act="help">Help</button>
        <button type="button" class="more-sheet-btn desktop-only" data-act="paths">Paths</button>
        <button type="button" class="more-sheet-btn desktop-only" data-act="rescan">Rescan</button>
      </div>
      <div class="more-sheet-footer">
        <button type="button" class="more-sheet-cancel" data-close="1">Close</button>
      </div>
    </div>
  `;
  document.body.appendChild(moreSheetEl);

  moreSheetEl.addEventListener('click', (e) => {
    const t = e.target as HTMLElement | null;
    if (!t) return;
    if (t.dataset.close === '1') {
      closeMoreSheet();
      return;
    }
    const act = t.dataset.act;
    if (!act) return;
    closeMoreSheet();
    switch (act) {
      case 'vj':       clickViewTab('vj'); break;
      case 'gallery':  clickViewTab('gallery'); break;
      case 'pipeline': clickViewTab('pipeline'); break;
      case 'wire':     clickViewTab('wire'); break;
      case 'split':    clickViewTab('split'); break;
      case 'palette':  openCommandPalette(); break;
      case 'settings': document.getElementById('sidebarSettingsBtn')?.click(); break;
      case 'help':     document.getElementById('sidebarHelpBtn')?.click(); break;
      case 'paths':    document.getElementById('sidebarPathsBtn')?.click(); break;
      case 'rescan':   document.getElementById('sidebarRescanBtn')?.click(); break;
    }
  });

  return moreSheetEl;
}

export function openMoreSheet(): void {
  buildMoreSheet().classList.add('open');
}

export function closeMoreSheet(): void {
  moreSheetEl?.classList.remove('open');
}

export function initBottomTabBar(): void {
  barEl = document.createElement('nav');
  barEl.className = 'bottom-tab-bar';
  barEl.id = 'bottomTabBar';
  barEl.setAttribute('aria-label', 'Bottom navigation');
  barEl.innerHTML = `
    <button type="button" class="bottom-tab" data-tab="library" aria-label="Library">
      <span class="bottom-tab-icon" aria-hidden="true">&#9776;</span>
      <span class="bottom-tab-label">Library</span>
    </button>
    <button type="button" class="bottom-tab" data-tab="preview" aria-label="Preview">
      <span class="bottom-tab-icon" aria-hidden="true">&#9654;</span>
      <span class="bottom-tab-label">Preview</span>
    </button>
    <button type="button" class="bottom-tab" data-tab="code" aria-label="Code">
      <span class="bottom-tab-icon" aria-hidden="true">{}</span>
      <span class="bottom-tab-label">Code</span>
    </button>
    <button type="button" class="bottom-tab" data-tab="params" aria-label="Parameters">
      <span class="bottom-tab-icon" aria-hidden="true">&#8801;</span>
      <span class="bottom-tab-label">Params</span>
    </button>
    <button type="button" class="bottom-tab" data-tab="more" aria-label="More">
      <span class="bottom-tab-icon" aria-hidden="true">&#8943;</span>
      <span class="bottom-tab-label">More</span>
    </button>
  `;
  document.body.appendChild(barEl);

  // Expose openMoreSheet globally so app bar's hamburger can use it
  // without a direct import (avoids a cycle).
  (window as unknown as { openMoreSheet?: () => void }).openMoreSheet = openMoreSheet;

  barEl.addEventListener('click', (e) => {
    const tab = (e.target as HTMLElement).closest('.bottom-tab') as HTMLElement | null;
    if (!tab) return;
    const t = tab.dataset.tab;
    switch (t) {
      case 'library':
        toggleDock('left');
        break;
      case 'preview':
        closeAllDocks();
        // If we're already in code-only, switch to split for context;
        // otherwise go to preview-only.
        if (getActiveView() === 'code') clickViewTab('split');
        else clickViewTab('preview');
        break;
      case 'code':
        closeAllDocks();
        clickViewTab('code');
        break;
      case 'params':
        toggleDock('right');
        break;
      case 'more':
        openMoreSheet();
        break;
    }
    // Defer sync so view-tab class flips first.
    setTimeout(syncActive, 30);
  });

  // Sync on view changes.
  const tabsRoot = document.querySelector('.view-tabs');
  if (tabsRoot) {
    new MutationObserver(syncActive).observe(tabsRoot, {
      attributes: true,
      subtree: true,
      attributeFilter: ['class']
    });
  }
  // Sync on dock open/close.
  new MutationObserver(syncActive).observe(document.body, {
    attributes: true,
    attributeFilter: ['class']
  });

  syncActive();

  onLayoutTierChange(() => syncActive());
  window.addEventListener('macroverse:dock-change', () => syncActive());
}
