import { status, clampContextMenuToViewport } from '../dom.js';
import { postOutputSpout, postOutputNdi, postOutputMacroCam, setMacroCamBaseUrl } from '../api.js';
import { appSettings, setAppSettings } from '../state.js';
import { currentParamsMeta, resetParamToDefault } from '../panels/params.js';
import { setMacroCamEnabled, isMacroCamEnabled } from '../render.js';
import { hasVideoOutput } from '../hostCapabilities.js';

let menuEl: HTMLElement | null = null;

function getMenuEl(): HTMLElement {
  if (menuEl) return menuEl;
  menuEl = document.createElement('div');
  menuEl.id = 'previewContextMenu';
  menuEl.style.cssText = `
    position: fixed; z-index: 10000;
    background: var(--amiga-panel);
    border: 2px solid var(--bevel-dark);
    box-shadow: 4px 4px 12px rgba(0,0,0,0.5);
    min-width: 160px;
    padding: 4px 0;
    display: none;
    font-size: 12px;
  `;
  document.body.appendChild(menuEl);
  return menuEl;
}

function addMenuItem(
  parent: HTMLElement,
  label: string,
  onClick: () => void,
  opts?: { checked?: boolean }
): void {
  const item = document.createElement('div');
  item.style.cssText = `
    padding: 6px 12px; cursor: pointer;
    color: var(--crt-fg);
    display: flex; align-items: center; justify-content: space-between;
  `;
  item.style.background = 'transparent';
  item.onmouseenter = () => { item.style.background = 'var(--amiga-surface)'; };
  item.onmouseleave = () => { item.style.background = 'transparent'; };
  const labelSpan = document.createElement('span');
  labelSpan.textContent = label;
  item.appendChild(labelSpan);
  if (opts?.checked) {
    const check = document.createElement('span');
    check.textContent = ' [X]';
    check.style.color = 'var(--amiga-accent)';
    item.appendChild(check);
  }
  item.onclick = (e) => {
    e.stopPropagation();
    onClick();
  };
  parent.appendChild(item);
}

function hideMenu(): void {
  getMenuEl().style.display = 'none';
}

function showMenu(x: number, y: number): void {
  if (!hasVideoOutput()) return;

  const menu = getMenuEl();
  menu.innerHTML = '';
  menu.style.left = String(x) + 'px';
  menu.style.top = String(y) + 'px';
  menu.style.display = 'block';
  menu.onclick = (e) => e.stopPropagation();

  const spoutOn = !!appSettings.enableSpout;
  const ndiOn = !!appSettings.enableNdi;

  addMenuItem(menu, 'Output to Spout', () => {
    const next = !spoutOn;
    setAppSettings({ enableSpout: next });
    status(next ? 'Starting Spout output...' : 'Stopping Spout output...');
    postOutputSpout(next).then((r) => {
      if (!r.ok) {
        setAppSettings({ enableSpout: false });
        const err = (r as { error?: string }).error || 'wire-output binary not found';
        status('Spout: ' + err, true);
      } else {
        status(next ? 'Spout output active' : 'Spout output stopped');
      }
    }).catch((e) => {
      setAppSettings({ enableSpout: false });
      status('Spout: ' + ((e as Error)?.message || 'failed'), true);
    });
    hideMenu();
  }, { checked: spoutOn });

  addMenuItem(menu, 'Output to NDI', () => {
    const next = !ndiOn;
    setAppSettings({ enableNdi: next });
    status(next ? 'Starting NDI output...' : 'Stopping NDI output...');
    postOutputNdi(next).then((r) => {
      if (!r.ok) {
        setAppSettings({ enableNdi: false });
        const err = (r as { error?: string }).error || 'wire-output binary not found';
        status('NDI: ' + err, true);
      } else {
        status(next ? 'NDI output active' : 'NDI output stopped');
      }
    }).catch((e) => {
      setAppSettings({ enableNdi: false });
      status('NDI: ' + ((e as Error)?.message || 'failed'), true);
    });
    hideMenu();
  }, { checked: ndiOn });

  const macroCamOn = isMacroCamEnabled();
  addMenuItem(menu, 'Output to MacroCam (MJPEG)', () => {
    const next = !macroCamOn;
    status(next ? 'Starting MacroCam...' : 'Stopping MacroCam...');
    postOutputMacroCam(next).then((r) => {
      if (!r.ok) {
        setMacroCamEnabled(false);
        status('MacroCam: failed to toggle', true);
      } else {
        setMacroCamEnabled(next);
        if (next) {
          const name = (r as { name?: string }).name || 'MacroCam';
          const url = (r as { streamUrl?: string }).streamUrl || '';
          if (url) setMacroCamBaseUrl(url);
          status(name + ' active - stream at ' + url);
        } else {
          status('MacroCam stopped');
        }
      }
    }).catch((e) => {
      setMacroCamEnabled(false);
      status('MacroCam: ' + ((e as Error)?.message || 'failed'), true);
    });
    hideMenu();
  }, { checked: macroCamOn });

  clampContextMenuToViewport(menu);

  const closeOnOutside = () => {
    document.removeEventListener('click', closeOnOutside);
    document.removeEventListener('contextmenu', closeOnOutside);
    hideMenu();
  };
  setTimeout(() => {
    document.addEventListener('click', closeOnOutside);
    document.addEventListener('contextmenu', closeOnOutside);
  }, 0);
}

function showParamContextMenu(x: number, y: number, paramId: string): void {
  const meta = currentParamsMeta.find((p) => p.id === paramId);
  if (!meta) return;
  const menu = getMenuEl();
  menu.innerHTML = '';
  menu.style.left = String(x) + 'px';
  menu.style.top = String(y) + 'px';
  menu.style.display = 'block';
  menu.onclick = (e) => e.stopPropagation();
  clampContextMenuToViewport(menu);

  addMenuItem(menu, 'Reset to initial value', () => {
    if (resetParamToDefault(paramId)) {
      const defStr = typeof meta.def === 'boolean' ? (meta.def ? '1' : '0') : String(meta.def);
      status(paramId + ' reset to ' + defStr);
    }
    hideMenu();
  });

  clampContextMenuToViewport(menu);

  const closeOnOutside = () => {
    document.removeEventListener('click', closeOnOutside);
    document.removeEventListener('contextmenu', closeOnOutside);
    hideMenu();
  };
  setTimeout(() => {
    document.addEventListener('click', closeOnOutside);
    document.addEventListener('contextmenu', closeOnOutside);
  }, 0);
}

export function initPreviewContextMenu(): void {
  const area = document.querySelector('.preview-area');
  if (!area) return;
  area.addEventListener('contextmenu', (e: Event) => {
    if (!hasVideoOutput()) return;
    const ev = e as MouseEvent;
    ev.preventDefault();
    ev.stopPropagation();
    showMenu(ev.clientX, ev.clientY);
  });

  document.addEventListener('contextmenu', (e: Event) => {
    const target = e.target as HTMLElement;
    if (target.closest('.preview-area')) return;
    if (target.closest('.vj-deck-wrap')) return;
    if (target.closest('.ctx-menu')) return;
    const paramRow = target.closest('.param-row') as HTMLElement | null;
    if (paramRow) {
      const paramId = paramRow.dataset.param;
      if (paramId) {
        e.preventDefault();
        showParamContextMenu((e as MouseEvent).clientX, (e as MouseEvent).clientY, paramId);
      }
      return;
    }
    if (target.closest('.list-item')) return;
    if (target.closest('.code-wrap') || target.closest('#codeEditorContainer')) return;
    if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA') return;
    e.preventDefault();
  });
}
