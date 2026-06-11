import { clampContextMenuToViewport } from '../dom.js';

export type ContextMenuItem =
  | { type?: 'item'; label: string; action: () => void; disabled?: boolean; checked?: boolean }
  | { type: 'sep' };

let activeMenu: HTMLElement | null = null;

function hideActiveMenu(): void {
  if (activeMenu) {
    activeMenu.remove();
    activeMenu = null;
  }
  document.removeEventListener('click', onOutsideClick);
  document.removeEventListener('contextmenu', onOutsideClick);
}

function onOutsideClick(): void {
  hideActiveMenu();
}

export function hideContextMenu(): void {
  hideActiveMenu();
}

export function showContextMenu(x: number, y: number, items: ContextMenuItem[], opts?: { className?: string }): void {
  hideActiveMenu();
  const menu = document.createElement('div');
  menu.className = 'ctx-menu' + (opts?.className ? ' ' + opts.className : '');
  menu.style.cssText =
    'position:fixed;left:' + x + 'px;top:' + y + 'px;background:var(--amiga-panel);border:2px solid var(--bevel-dark);padding:4px 0;min-width:200px;z-index:10001;box-shadow:4px 4px 12px rgba(0,0,0,0.5);font-size:12px;';
  activeMenu = menu;

  for (const item of items) {
    if (item.type === 'sep') {
      const sep = document.createElement('div');
      sep.style.cssText = 'height:1px;background:var(--bevel-dark);margin:4px 0;';
      menu.appendChild(sep);
      continue;
    }
    const row = document.createElement('div');
    row.className = 'ctx-menu-item';
    row.style.cssText =
      'padding:6px 12px;cursor:pointer;color:var(--crt-fg);display:flex;align-items:center;justify-content:space-between;gap:8px;';
    if (item.disabled) {
      row.style.opacity = '0.45';
      row.style.cursor = 'default';
    }
    const labelSpan = document.createElement('span');
    labelSpan.textContent = item.label;
    row.appendChild(labelSpan);
    if (item.checked) {
      const mark = document.createElement('span');
      mark.textContent = '[x]';
      mark.style.color = 'var(--amiga-accent)';
      mark.style.fontSize = '10px';
      row.appendChild(mark);
    }
    if (!item.disabled) {
      row.onmouseenter = () => { row.style.background = 'var(--amiga-surface)'; };
      row.onmouseleave = () => { row.style.background = ''; };
      row.onclick = (e) => {
        e.stopPropagation();
        hideActiveMenu();
        item.action();
      };
    }
    menu.appendChild(row);
  }

  document.body.appendChild(menu);
  clampContextMenuToViewport(menu);
  setTimeout(() => {
    document.addEventListener('click', onOutsideClick);
    document.addEventListener('contextmenu', onOutsideClick);
  }, 0);
}
