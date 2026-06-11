export function el(id: string): HTMLElement | null {
  return document.getElementById(id) || null;
}

const VIEWPORT_PAD = 8;

/** Clamp a fixed-position context menu so it stays inside the viewport. Call after appending to body. */
export function clampContextMenuToViewport(menu: HTMLElement): void {
  requestAnimationFrame(() => {
    const r = menu.getBoundingClientRect();
    const w = r.width;
    const h = r.height;
    let left = r.left;
    let top = r.top;
    if (left + w > window.innerWidth - VIEWPORT_PAD) left = window.innerWidth - VIEWPORT_PAD - w;
    if (top + h > window.innerHeight - VIEWPORT_PAD) top = window.innerHeight - VIEWPORT_PAD - h;
    if (left < VIEWPORT_PAD) left = VIEWPORT_PAD;
    if (top < VIEWPORT_PAD) top = VIEWPORT_PAD;
    menu.style.left = left + 'px';
    menu.style.top = top + 'px';
  });
}

export function status(text: string, isError?: boolean): void {
  const elm = el('statusText');
  if (!elm) return;
  elm.textContent = text || '';
  elm.title = text || '';
  if (isError && elm.parentElement) {
    elm.parentElement.classList.add('status-error');
  } else if (elm.parentElement) {
    elm.parentElement.classList.remove('status-error');
  }
}

export function hideSplash(): void {
  const ov = el('splashOverlay');
  if (ov) ov.classList.add('hidden');
}

const pathsInfoStyles = {
  overlay: 'position:fixed;inset:0;z-index:10002;background:rgba(0,0,0,0.7);display:flex;align-items:center;justify-content:center;cursor:pointer;',
  box: 'background:var(--amiga-panel);border:2px solid var(--amiga-copper);padding:24px;max-width:90vw;max-height:80vh;font-family:inherit;cursor:default;min-width:360px;display:flex;flex-direction:column;overflow:hidden;',
  boxScroll: 'flex:1;min-height:0;overflow:auto;',
  row: 'display:flex;gap:8px;align-items:center;margin-bottom:8px;',
  input: 'flex:1;padding:8px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);font-size:12px;font-family:inherit;',
  btn: 'padding:6px 12px;font-size:11px;background:var(--amiga-surface);color:var(--amiga-accent);border:1px solid var(--bevel-dark);cursor:pointer;flex-shrink:0;',
  btnDanger: 'padding:6px 12px;font-size:11px;background:#5a2222;color:#ffaaaa;border:1px solid #8a4444;cursor:pointer;flex-shrink:0;',
  label: 'color:var(--amiga-copper);font-size:11px;text-transform:uppercase;margin-bottom:6px;display:block;',
  muted: 'font-size:11px;color:var(--crt-dim);margin-top:4px;',
};

export function showPathsInfo(paths: string[], indexPath: string, count: number): void {
  let existing = document.getElementById('pathsInfoOverlay');
  if (existing) existing.remove();

  const overlay = document.createElement('div');
  overlay.id = 'pathsInfoOverlay';
  overlay.style.cssText = pathsInfoStyles.overlay;
  overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };

  const box = document.createElement('div');
  box.style.cssText = pathsInfoStyles.box;
  box.onclick = (e) => e.stopPropagation();

  const pathList = (paths && paths.length) ? [...paths] : [];
  const pathRowsContainer = document.createElement('div');
  pathRowsContainer.id = 'pathsInfoPathRows';

  function addPathRow(value: string): void {
    const row = document.createElement('div');
    row.style.cssText = pathsInfoStyles.row;
    const input = document.createElement('input');
    input.type = 'text';
    input.className = 'paths-info-source-input';
    input.value = value;
    input.style.cssText = pathsInfoStyles.input;
    input.placeholder = 'Drive:\\path\\to\\shaders';
    const removeBtn = document.createElement('button');
    removeBtn.type = 'button';
    removeBtn.textContent = 'Remove';
    removeBtn.title = 'TLDR: Remove this source path';
    removeBtn.style.cssText = pathsInfoStyles.btnDanger;
    removeBtn.onclick = () => { row.remove(); };
    row.appendChild(input);
    row.appendChild(removeBtn);
    pathRowsContainer.appendChild(row);
  }

  pathList.forEach((p) => addPathRow(p));

  const addBtn = document.createElement('button');
  addBtn.type = 'button';
  addBtn.textContent = 'Add path...';
  addBtn.title = 'TLDR: Browse and add a source path';
  addBtn.style.cssText = pathsInfoStyles.btn;
  addBtn.style.marginBottom = '16px';
  addBtn.onclick = () => {
    import('./pathPicker.js').then(({ showPathPicker }) => {
      showPathPicker((selectedPath) => {
        if (selectedPath) addPathRow(selectedPath);
      });
    });
  };

  const indexLabel = document.createElement('label');
  indexLabel.style.cssText = pathsInfoStyles.label;
  indexLabel.textContent = 'Index (SQLite)';
  const indexInput = document.createElement('input');
  indexInput.type = 'text';
  indexInput.readOnly = true;
  indexInput.value = indexPath || 'macroverse.db (internal)';
  indexInput.style.cssText = pathsInfoStyles.input + ' margin-bottom:16px;';

  const countEl = document.createElement('div');
  countEl.style.cssText = pathsInfoStyles.muted + ' margin-bottom:16px;';
  countEl.textContent = 'Shaders indexed: ' + count;

  function collectPaths(): string[] {
    const inputs = pathRowsContainer.querySelectorAll('input.paths-info-source-input');
    const out: string[] = [];
    inputs.forEach((inp) => {
      const v = (inp as HTMLInputElement).value.trim();
      if (v) out.push(v);
    });
    return out;
  }

  const saveBtn = document.createElement('button');
  saveBtn.type = 'button';
  saveBtn.textContent = 'Save paths';
  saveBtn.title = 'TLDR: Save paths and index config';
  saveBtn.style.cssText = pathsInfoStyles.btn;
  saveBtn.onclick = async () => {
    const newPaths = collectPaths();
    const { postSources } = await import('./api.js');
    const { appSettings, setAppSettings } = await import('./state.js');
    const currentPaths = appSettings.sourcePaths || [];
    try {
      for (let i = currentPaths.length - 1; i >= 0; i--) {
        await postSources({ action: 'remove', index: i });
      }
      let lastRes: { paths?: string[] } = {};
      for (const p of newPaths) {
        lastRes = await postSources({ action: 'add', path: p });
      }
      const updatedPaths = lastRes.paths || newPaths;
      setAppSettings({ sourcePaths: updatedPaths });
      status('Paths saved.');
      overlay.remove();
      const { buildList } = await import('./list.js');
      buildList();
    } catch (e) {
      status('Save paths: ' + (e as Error).message, true);
    }
  };

  const reindexBtn = document.createElement('button');
  reindexBtn.type = 'button';
  reindexBtn.textContent = 'Reindex';
  reindexBtn.style.cssText = pathsInfoStyles.btn;
  reindexBtn.title = 'TLDR: Scan and reload shader list';
  reindexBtn.onclick = async () => {
    overlay.remove();
    status('Saving paths...');
    const { postSources, postNativeScan } = await import('./api.js');
    const { appSettings, setAppSettings } = await import('./state.js');
    const { loadSequence } = await import('./init/loadSequence.js');
    try {
      const newPaths = collectPaths();
      const currentPaths = appSettings.sourcePaths || [];
      for (let i = currentPaths.length - 1; i >= 0; i--) {
        await postSources({ action: 'remove', index: i });
      }
      let lastRes: { paths?: string[] } = {};
      for (const p of newPaths) {
        lastRes = await postSources({ action: 'add', path: p });
      }
      setAppSettings({ sourcePaths: lastRes.paths || newPaths });
      status('Scanning...');
      const result = await postNativeScan();
      status(`Scan complete — ${result.added} added, ${result.removed} removed`);
      await loadSequence();
    } catch (e) {
      status('Reindex: ' + (e as Error).message, true);
    }
  };

  const MASS_THUMB_CAP = 50;
  const thumbBtn = document.createElement('button');
  thumbBtn.type = 'button';
  thumbBtn.textContent = 'Generate thumbnails';
  thumbBtn.style.cssText = pathsInfoStyles.btn;
  thumbBtn.title = 'TLDR: Generate thumbnails for shaders without one (up to ' + MASS_THUMB_CAP + ')';
  thumbBtn.onclick = async () => {
    const { entries: stateEntries, getThumbnail: getThumb, setThumbnail } = await import('./state.js');
    const { buildList } = await import('./list.js');
    const { postThumbnailSave } = await import('./api.js');
    const { generateThumbnailsInBackground } = await import('./thumbnailRenderer.js');
    const allWithout = stateEntries.filter((e) => e.path && !getThumb(e.path));
    if (allWithout.length === 0) {
      status('All shaders already have thumbnails.');
      return;
    }
    const toGenerate = allWithout.slice(0, MASS_THUMB_CAP);
    const capMsg = allWithout.length > MASS_THUMB_CAP
      ? ' (first ' + MASS_THUMB_CAP + ' of ' + allWithout.length + ')'
      : '';
    const msg = 'Generate thumbnails for ' + toGenerate.length + ' shader(s)' + capMsg + ' in background? Main preview stays responsive.';
    if (!window.confirm(msg)) {
      status('Cancelled.');
      return;
    }
    status('Generating ' + toGenerate.length + ' thumbnails in background...');
    const entries = toGenerate.map((e) => ({ path: e.path!, entry: e }));
    await generateThumbnailsInBackground(
      entries,
      (done, total, name) => status('Thumbnail ' + done + '/' + total + ': ' + name),
      (path, dataUrl) => {
        setThumbnail(path, dataUrl);
        postThumbnailSave({ path, dataUrl }).catch(() => {});
        window.dispatchEvent(new CustomEvent('thumbnail-captured', { detail: { path } }));
        buildList();
      }
    );
    status('Thumbnails generated for ' + toGenerate.length + ' shaders.');
  };

  const closeBtn = document.createElement('button');
  closeBtn.type = 'button';
  closeBtn.textContent = 'Close';
  closeBtn.title = 'TLDR: Close paths dialog';
  closeBtn.style.cssText = pathsInfoStyles.btn + ';pointer-events:auto;position:relative;z-index:1;';
  closeBtn.onclick = () => overlay.remove();

  const header = document.createElement('div');
  header.style.cssText = 'color:var(--amiga-copper);font-weight:bold;margin-bottom:16px;font-size:14px;flex-shrink:0;';
  header.textContent = 'SHADER PATHS INFO';

  const pathLabel = document.createElement('label');
  pathLabel.style.cssText = pathsInfoStyles.label;
  pathLabel.textContent = 'Source path(s)';

  const scrollWrap = document.createElement('div');
  scrollWrap.style.cssText = pathsInfoStyles.boxScroll;
  scrollWrap.appendChild(pathLabel);
  scrollWrap.appendChild(pathRowsContainer);
  scrollWrap.appendChild(addBtn);
  scrollWrap.appendChild(indexLabel);
  scrollWrap.appendChild(indexInput);
  scrollWrap.appendChild(countEl);

  const btnRow = document.createElement('div');
  btnRow.style.cssText = 'display:flex;gap:8px;flex-wrap:wrap;margin-top:16px;flex-shrink:0;pointer-events:auto;position:relative;z-index:1;';
  btnRow.appendChild(saveBtn);
  btnRow.appendChild(reindexBtn);
  btnRow.appendChild(thumbBtn);
  btnRow.appendChild(closeBtn);

  box.appendChild(header);
  box.appendChild(scrollWrap);
  box.appendChild(btnRow);

  overlay.appendChild(box);
  document.body.appendChild(overlay);
}
