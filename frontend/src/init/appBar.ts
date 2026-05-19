/**
 * Top app bar wiring.
 *
 * The app bar is the single permanent header on every device. It
 * provides:
 *   - Logo (M42)             -> opens Settings panel
 *   - Shader name + dirty    -> reflects current loaded shader
 *   - View dropdown          -> mirrors the existing .view-tabs
 *   - Save button            -> proxies to #codeSaveBtn (existing
 *                               save logic stays in codeEditor.ts)
 *   - Palette button (>_)    -> opens command palette
 *   - Menu button (hamburger)-> opens "More" sheet (Phase 4 wires
 *                               the bottom-tab "More" sheet too)
 */

import { el } from '../dom.js';
import type { IndexEntry } from '../types.js';
import { currentEntry } from '../state.js';
import { initVjAppBarSession } from './vjAppBarSession.js';

let appBarEl: HTMLElement | null = null;
let shaderEl: HTMLElement | null = null;
let saveBtn: HTMLButtonElement | null = null;
let logoBtn: HTMLButtonElement | null = null;
let paletteBtn: HTMLButtonElement | null = null;
let menuBtn: HTMLButtonElement | null = null;
let libBtn: HTMLButtonElement | null = null;
let paramsBtn: HTMLButtonElement | null = null;
let viewSelect: HTMLSelectElement | null = null;

function shortShaderLabel(entry: IndexEntry | null | undefined): string {
  if (!entry) return 'No shader loaded';
  const name = entry.fixedName || entry.name || entry.path || '';
  if (!name) return 'No shader loaded';
  // Strip leading folder paths so we show just the file name.
  const slashIdx = Math.max(name.lastIndexOf('/'), name.lastIndexOf('\\'));
  return slashIdx >= 0 ? name.slice(slashIdx + 1) : name;
}

function updateShaderLabel(entry: IndexEntry | null | undefined): void {
  if (!shaderEl) return;
  shaderEl.textContent = shortShaderLabel(entry);
  shaderEl.title = entry?.path || '';
}

function syncDirtyState(): void {
  if (!appBarEl) return;
  const codeSave = document.getElementById('codeSaveBtn');
  const dirty = !!codeSave?.classList.contains('btn-unsaved');
  appBarEl.classList.toggle('is-dirty', dirty);
}

/**
 * Open the command palette. The palette implementation lives in a
 * separate module (Phase 3); we lazy-import it the first time the
 * user asks for it.
 */
async function openCommandPalette(): Promise<void> {
  try {
    const m = await import('./commandPalette.js');
    m.openCommandPalette();
  } catch (_) {
    // Palette not yet available - fall back to nothing.
  }
}

/**
 * Open the "More" sheet. This routes to the bottom-tab-bar's More
 * implementation once Phase 4 lands. Until then, it just opens the
 * Settings panel as a sensible default.
 */
function openMoreMenu(): void {
  const more = (window as unknown as { openMoreSheet?: () => void }).openMoreSheet;
  if (typeof more === 'function') {
    more();
    return;
  }
  // Fallback: open Settings.
  document.getElementById('sidebarSettingsBtn')?.click();
}

export function initAppBar(): void {
  appBarEl = el('appBar');
  shaderEl = el('appBarShader');
  saveBtn = el('appBarSave') as HTMLButtonElement | null;
  logoBtn = el('appBarLogo') as HTMLButtonElement | null;
  paletteBtn = el('appBarPalette') as HTMLButtonElement | null;
  menuBtn = el('appBarMenu') as HTMLButtonElement | null;
  libBtn = el('appBarLibraryBtn') as HTMLButtonElement | null;
  paramsBtn = el('appBarParamsBtn') as HTMLButtonElement | null;
  viewSelect = el('viewSelect') as HTMLSelectElement | null;
  if (!appBarEl) return;

  initVjAppBarSession();

  // Initial label
  updateShaderLabel(currentEntry);

  // Listen for shader changes
  window.addEventListener('macroverse:shader-changed', (ev: Event) => {
    const detail = (ev as CustomEvent<{ entry: IndexEntry | null }>).detail;
    updateShaderLabel(detail?.entry || null);
  });

  // Save: forward click to existing #codeSaveBtn so all the existing
  // save logic in codeEditor.ts still runs.
  saveBtn?.addEventListener('click', () => {
    const real = document.getElementById('codeSaveBtn');
    if (real) (real as HTMLButtonElement).click();
  });

  // Logo opens Settings.
  logoBtn?.addEventListener('click', () => {
    document.getElementById('sidebarSettingsBtn')?.click();
  });

  // Palette
  paletteBtn?.addEventListener('click', () => { void openCommandPalette(); });

  // Menu
  menuBtn?.addEventListener('click', openMoreMenu);

  // Tablet-only Library/Params buttons -> dock toggles
  libBtn?.addEventListener('click', () => {
    void import('./dockSystem.js').then((m) => m.toggleDock('left'));
  });
  paramsBtn?.addEventListener('click', () => {
    void import('./dockSystem.js').then((m) => m.toggleDock('right'));
  });

  // Watch the existing #codeSaveBtn for dirty-state changes so our
  // app-bar Save button mirrors the flash.
  const codeSave = document.getElementById('codeSaveBtn');
  if (codeSave) {
    syncDirtyState();
    const obs = new MutationObserver(syncDirtyState);
    obs.observe(codeSave, { attributes: true, attributeFilter: ['class', 'textContent'], subtree: false });
    // Some code paths set textContent without class change; poll a
    // couple times to be safe (cheap).
    setInterval(syncDirtyState, 500);
  }

  // View dropdown -> click the matching .view-tab
  if (viewSelect) {
    viewSelect.addEventListener('change', () => {
      const v = viewSelect!.value;
      const tab = document.querySelector(`.view-tab[data-view="${v}"]`) as HTMLElement | null;
      tab?.click();
    });
    // Two-way sync: when a tab is activated by click or by code,
    // update the dropdown.
    const tabsRoot = document.querySelector('.view-tabs');
    if (tabsRoot) {
      const sync = () => {
        const active = document.querySelector('.view-tab.active') as HTMLElement | null;
        const v = active?.dataset.view;
        if (v && viewSelect && viewSelect.value !== v) viewSelect.value = v;
      };
      new MutationObserver(sync).observe(tabsRoot, {
        attributes: true,
        subtree: true,
        attributeFilter: ['class']
      });
      sync();
    }
  }

  // Global keyboard: Ctrl+K opens palette.
  document.addEventListener('keydown', (ev: KeyboardEvent) => {
    if ((ev.ctrlKey || ev.metaKey) && (ev.key === 'k' || ev.key === 'K')) {
      ev.preventDefault();
      void openCommandPalette();
    }
  });
}

/** Manually update the shader label (used by tests / fallbacks). */
export function setAppBarShader(entry: IndexEntry | null | undefined): void {
  updateShaderLabel(entry);
}
