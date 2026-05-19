import { fetchListDirs } from './api.js';

// Cross-platform fuzzy path picker modal
// Replaces the old PowerShell FolderBrowserDialog

export function showPathPicker(onSelect: (path: string) => void): void {
  const overlay = document.createElement('div');
  overlay.style.cssText = 'position:fixed;inset:0;z-index:10005;background:rgba(0,0,0,0.7);display:flex;align-items:center;justify-content:center;';
  overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };

  const box = document.createElement('div');
  box.style.cssText = 'background:var(--amiga-panel);border:2px solid var(--amiga-copper);padding:16px;min-width:400px;max-width:90vw;max-height:80vh;font-family:inherit;display:flex;flex-direction:column;';
  box.onclick = (e) => e.stopPropagation();

  const header = document.createElement('div');
  header.style.cssText = 'color:var(--amiga-copper);font-weight:bold;margin-bottom:12px;font-size:13px;';
  header.textContent = 'Select Folder';

  const pathRow = document.createElement('div');
  pathRow.style.cssText = 'display:flex;gap:6px;margin-bottom:8px;';
  const pathInput = document.createElement('input');
  pathInput.type = 'text';
  pathInput.style.cssText = 'flex:1;padding:8px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);font-family:inherit;font-size:12px;';
  pathInput.placeholder = 'Type a path or browse below...';
  pathInput.value = '/';

  const upBtn = document.createElement('button');
  upBtn.type = 'button';
  upBtn.textContent = '..';
  upBtn.title = 'Go to parent directory';
  upBtn.style.cssText = 'padding:6px 12px;background:var(--amiga-surface);color:var(--amiga-accent);border:1px solid var(--bevel-dark);cursor:pointer;font-size:12px;';

  pathRow.appendChild(pathInput);
  pathRow.appendChild(upBtn);

  const filterInput = document.createElement('input');
  filterInput.type = 'text';
  filterInput.style.cssText = 'width:100%;padding:6px 8px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);font-family:inherit;font-size:11px;margin-bottom:6px;';
  filterInput.placeholder = 'Filter directories...';

  const dirList = document.createElement('div');
  dirList.style.cssText = 'flex:1;min-height:200px;max-height:400px;overflow-y:auto;background:var(--amiga-bg);border:1px solid var(--bevel-dark);padding:4px;';

  const btnRow = document.createElement('div');
  btnRow.style.cssText = 'display:flex;gap:8px;margin-top:10px;justify-content:flex-end;';

  const selectBtn = document.createElement('button');
  selectBtn.type = 'button';
  selectBtn.textContent = 'Select This Folder';
  selectBtn.style.cssText = 'padding:8px 16px;background:var(--amiga-accent);color:#fff;border:none;cursor:pointer;font-size:12px;';

  const cancelBtn = document.createElement('button');
  cancelBtn.type = 'button';
  cancelBtn.textContent = 'Cancel';
  cancelBtn.style.cssText = 'padding:6px 12px;background:var(--amiga-surface);color:var(--amiga-accent);border:1px solid var(--bevel-dark);cursor:pointer;font-size:12px;';
  cancelBtn.onclick = () => overlay.remove();

  btnRow.appendChild(selectBtn);
  btnRow.appendChild(cancelBtn);

  let currentPath = '';
  let allDirs: string[] = [];

  function renderDirs(dirs: string[], filter: string): void {
    dirList.innerHTML = '';
    const lower = (filter || '').toLowerCase();
    const filtered = lower ? dirs.filter((d) => d.toLowerCase().includes(lower)) : dirs;
    if (filtered.length === 0) {
      const empty = document.createElement('div');
      empty.style.cssText = 'padding:8px;color:var(--crt-dim);font-size:11px;';
      empty.textContent = dirs.length === 0 ? 'No subdirectories' : 'No matches for "' + filter + '"';
      dirList.appendChild(empty);
      return;
    }
    for (const d of filtered) {
      const item = document.createElement('div');
      item.style.cssText = 'padding:6px 8px;cursor:pointer;font-size:12px;color:var(--crt-fg);border-bottom:1px solid var(--bevel-dark);display:flex;align-items:center;gap:6px;';
      item.onmouseenter = () => { item.style.background = 'var(--amiga-surface)'; item.style.color = 'var(--amiga-accent)'; };
      item.onmouseleave = () => { item.style.background = ''; item.style.color = 'var(--crt-fg)'; };
      const icon = document.createElement('span');
      icon.style.cssText = 'color:var(--amiga-copper);font-size:10px;flex-shrink:0;';
      icon.textContent = '[DIR]';
      const name = document.createElement('span');
      name.style.cssText = 'min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;';
      name.textContent = d;
      item.appendChild(icon);
      item.appendChild(name);
      item.ondblclick = () => {
        const sep = currentPath.includes('\\') ? '\\' : '/';
        let newPath: string;
        if (currentPath === '' || currentPath === '/') {
          newPath = currentPath + d;
        } else {
          newPath = currentPath.replace(/[/\\]$/, '') + sep + d;
        }
        navigate(newPath);
      };
      item.onclick = () => {
        const sep = currentPath.includes('\\') ? '\\' : '/';
        let full: string;
        if (currentPath === '' || currentPath === '/') {
          full = currentPath + d;
        } else {
          full = currentPath.replace(/[/\\]$/, '') + sep + d;
        }
        pathInput.value = full;
      };
      dirList.appendChild(item);
    }
  }

  function navigate(path: string): void {
    currentPath = path;
    pathInput.value = path;
    filterInput.value = '';
    dirList.innerHTML = '<div style="padding:8px;color:var(--crt-dim);font-size:11px;">Loading...</div>';
    fetchListDirs(path).then((r) => {
      allDirs = r.dirs;
      renderDirs(allDirs, '');
    }).catch(() => {
      dirList.innerHTML = '<div style="padding:8px;color:#ff8888;font-size:11px;">Failed to list directory</div>';
      allDirs = [];
    });
  }

  filterInput.addEventListener('input', () => {
    renderDirs(allDirs, filterInput.value);
  });

  pathInput.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') {
      navigate(pathInput.value.trim());
    }
  });

  upBtn.onclick = () => {
    const p = pathInput.value.trim();
    if (!p || p === '/' || /^[A-Z]:\\?$/i.test(p)) {
      navigate('');
      return;
    }
    const parts = p.replace(/[/\\]$/, '').split(/[/\\]/);
    parts.pop();
    const parent = parts.join('/') || '/';
    navigate(parent);
  };

  selectBtn.onclick = () => {
    const selected = pathInput.value.trim();
    if (selected) {
      overlay.remove();
      onSelect(selected);
    }
  };

  box.appendChild(header);
  box.appendChild(pathRow);
  box.appendChild(filterInput);
  box.appendChild(dirList);
  box.appendChild(btnRow);
  overlay.appendChild(box);
  document.body.appendChild(overlay);

  pathInput.focus();
  navigate('');
}
