/**
 * Command palette - a Ctrl+K modal that lists every registered
 * command, fuzzy-filters as you type, and runs the selected one.
 *
 * Wires no real commands itself - that's commandRegistry's job.
 * The palette stays cheap: it's only built once on first open
 * and reuses the same DOM thereafter.
 */

import { allCommands, runCommand, scoreCommand, type Command } from './commandRegistry.js';

let overlayEl: HTMLDivElement | null = null;
let inputEl: HTMLInputElement | null = null;
let listEl: HTMLDivElement | null = null;
let hintEl: HTMLDivElement | null = null;
let activeIndex = 0;
let filteredCmds: Command[] = [];

function ensureBuilt(): void {
  if (overlayEl) return;
  overlayEl = document.createElement('div');
  overlayEl.className = 'command-palette-overlay';
  overlayEl.id = 'commandPaletteOverlay';
  overlayEl.setAttribute('role', 'dialog');
  overlayEl.setAttribute('aria-modal', 'true');
  overlayEl.setAttribute('aria-label', 'Command palette');

  const box = document.createElement('div');
  box.className = 'command-palette';

  const inputWrap = document.createElement('div');
  inputWrap.className = 'command-palette-input-wrap';
  const prompt = document.createElement('span');
  prompt.className = 'command-palette-prompt';
  prompt.textContent = '>';
  inputEl = document.createElement('input');
  inputEl.type = 'text';
  inputEl.className = 'command-palette-input';
  inputEl.placeholder = 'Type a command... (Esc to close)';
  inputEl.autocomplete = 'off';
  inputEl.spellcheck = false;
  inputEl.setAttribute('aria-label', 'Command query');
  inputWrap.appendChild(prompt);
  inputWrap.appendChild(inputEl);

  listEl = document.createElement('div');
  listEl.className = 'command-palette-list';
  listEl.setAttribute('role', 'listbox');

  hintEl = document.createElement('div');
  hintEl.className = 'command-palette-hint';
  hintEl.innerHTML = 'Up/Down to move &nbsp;|&nbsp; Enter to run &nbsp;|&nbsp; Esc to close';

  box.appendChild(inputWrap);
  box.appendChild(listEl);
  box.appendChild(hintEl);
  overlayEl.appendChild(box);
  document.body.appendChild(overlayEl);

  overlayEl.addEventListener('click', (e) => {
    if (e.target === overlayEl) closeCommandPalette();
  });

  inputEl.addEventListener('input', () => {
    activeIndex = 0;
    refreshList();
  });

  inputEl.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
      e.preventDefault();
      closeCommandPalette();
    } else if (e.key === 'ArrowDown') {
      e.preventDefault();
      activeIndex = Math.min(filteredCmds.length - 1, activeIndex + 1);
      renderActiveSelection();
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      activeIndex = Math.max(0, activeIndex - 1);
      renderActiveSelection();
    } else if (e.key === 'Home') {
      e.preventDefault();
      activeIndex = 0;
      renderActiveSelection();
    } else if (e.key === 'End') {
      e.preventDefault();
      activeIndex = Math.max(0, filteredCmds.length - 1);
      renderActiveSelection();
    } else if (e.key === 'Enter') {
      e.preventDefault();
      runActive();
    }
  });
}

function refreshList(): void {
  if (!inputEl || !listEl) return;
  const query = inputEl.value.trim();
  const cmds = allCommands();
  if (!query) {
    filteredCmds = cmds.slice(0, 200);
  } else {
    const scored = cmds.map((c) => ({ c, s: scoreCommand(c, query) }))
      .filter((x) => x.s > 0)
      .sort((a, b) => b.s - a.s);
    filteredCmds = scored.slice(0, 200).map((x) => x.c);
  }
  if (activeIndex >= filteredCmds.length) {
    activeIndex = Math.max(0, filteredCmds.length - 1);
  }

  listEl.innerHTML = '';
  if (filteredCmds.length === 0) {
    const empty = document.createElement('div');
    empty.className = 'command-palette-empty';
    empty.textContent = 'No commands match "' + query + '"';
    listEl.appendChild(empty);
    return;
  }

  let lastCategory: string | null = null;
  filteredCmds.forEach((cmd, i) => {
    const cat = cmd.category || '';
    if (!query && cat !== lastCategory) {
      lastCategory = cat;
      const sep = document.createElement('div');
      sep.className = 'command-palette-cat';
      sep.textContent = cat || 'General';
      listEl!.appendChild(sep);
    }
    const item = document.createElement('div');
    item.className = 'command-palette-item';
    item.dataset.index = String(i);
    if (i === activeIndex) item.classList.add('active');
    item.setAttribute('role', 'option');

    const left = document.createElement('div');
    left.className = 'command-palette-item-left';
    const labelEl = document.createElement('div');
    labelEl.className = 'command-palette-item-label';
    labelEl.textContent = cmd.label;
    const descEl = document.createElement('div');
    descEl.className = 'command-palette-item-desc';
    descEl.textContent = cmd.description || '';
    left.appendChild(labelEl);
    if (cmd.description) left.appendChild(descEl);

    const right = document.createElement('div');
    right.className = 'command-palette-item-right';
    if (cmd.shortcut) {
      const sc = document.createElement('span');
      sc.className = 'command-palette-shortcut';
      sc.textContent = cmd.shortcut;
      right.appendChild(sc);
    }

    item.appendChild(left);
    item.appendChild(right);

    item.addEventListener('mouseenter', () => {
      activeIndex = i;
      renderActiveSelection();
    });
    item.addEventListener('click', () => {
      activeIndex = i;
      runActive();
    });

    listEl!.appendChild(item);
  });
  renderActiveSelection();
}

function renderActiveSelection(): void {
  if (!listEl) return;
  const items = listEl.querySelectorAll('.command-palette-item');
  items.forEach((it) => it.classList.remove('active'));
  const active = listEl.querySelector(
    `.command-palette-item[data-index="${activeIndex}"]`
  ) as HTMLElement | null;
  if (active) {
    active.classList.add('active');
    active.scrollIntoView({ block: 'nearest' });
  }
}

function runActive(): void {
  const cmd = filteredCmds[activeIndex];
  if (!cmd) return;
  closeCommandPalette();
  // Defer the run so the close animation completes first.
  setTimeout(() => { void runCommand(cmd.id); }, 50);
}

export function openCommandPalette(): void {
  ensureBuilt();
  if (!overlayEl || !inputEl) return;
  overlayEl.classList.add('open');
  inputEl.value = '';
  activeIndex = 0;
  refreshList();
  // Defer focus to next frame so iOS Safari raises the keyboard
  // reliably.
  requestAnimationFrame(() => {
    inputEl?.focus();
    inputEl?.select();
  });
}

export function closeCommandPalette(): void {
  if (!overlayEl) return;
  overlayEl.classList.remove('open');
  // Return focus to the previously-focused element where possible.
  // Using blur is safer than restoring an unknown last-focused.
  inputEl?.blur();
}

export function isCommandPaletteOpen(): boolean {
  return !!overlayEl && overlayEl.classList.contains('open');
}

export function initCommandPalette(): void {
  // Build the palette lazily; nothing to do at boot. Just confirm
  // Ctrl+K binding exists. The app bar already binds Ctrl+K, so
  // this init is a no-op placeholder for symmetry.
  // Bind it again here defensively in case appBar didn't load.
  if ((window as unknown as { __mvPaletteBound?: boolean }).__mvPaletteBound) return;
  (window as unknown as { __mvPaletteBound?: boolean }).__mvPaletteBound = true;
  document.addEventListener('keydown', (e) => {
    if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
      e.preventDefault();
      if (isCommandPaletteOpen()) closeCommandPalette();
      else openCommandPalette();
    }
    // "/" focuses the palette (Slack/Discord style) when no input
    // is focused.
    if (e.key === '/' && !isCommandPaletteOpen()) {
      const t = e.target as HTMLElement | null;
      const tag = (t?.tagName || '').toLowerCase();
      const isInput = tag === 'input' || tag === 'textarea' ||
                      (t?.isContentEditable ?? false) ||
                      tag === 'select';
      if (!isInput) {
        e.preventDefault();
        openCommandPalette();
      }
    }
  });
}
