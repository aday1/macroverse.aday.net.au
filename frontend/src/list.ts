import { el, status, clampContextMenuToViewport } from './dom.js';
import { entries, setCurrentEntry, currentEntry, listFormatFilter, listSearchQuery, listSetFilter, setListFormatFilter, setListSearchQuery, setListSetFilter, appSettings, setAppSettings, getThumbnail, getLastSaved, setThumbnail, showDeadShaders, setShowDeadShaders, showTrashShaders, setShowTrashShaders, setEntries } from './state.js';
import { postUpdate, postShaderRename, postShaderMove, postOpenInCursor, postOpenInExplorer, postOpenInNotepad, fetchGitLog, postGitRevertVersion, postShaderDelete, fetchShader, postThumbnailSave, fetchIndex } from './api.js';
import { loadShader, clearSessionForNewShader } from './render.js';
import { renderThumbnailSync } from './thumbnailRenderer.js';
import { showCreateShaderModal } from './createShader.js';
import { stopThumbnailLoad, isThumbnailsLoading, resumeThumbnailLoad, hasMissingThumbnails, getThumbnailProgress } from './init/loadSequence.js';
import { generateThumbnailsInBackground } from './thumbnailRenderer.js';
import type { IndexEntry } from './types.js';

let stopThumbsListenersAttached = false;
let setFilterListenerAttached = false;

const COLOR_PRESETS = ['#e74c3c', '#3498db', '#2ecc71', '#f39c12', '#9b59b6', '#1abc9c', '#e67e22', '#34495e'];

const EMOJI_PRESETS = [
  '\u2B50', '\u2764\uFE0F', '\u26A1', '\u2728', '\u2705', '\u26D4', '\u267B\uFE0F', '\u2757',
  '\u{1F3B5}', '\u{1F3A8}', '\u{1F525}', '\u{1F30A}', '\u{1F300}', '\u{1F30C}', '\u{1F308}', '\u{1F3AD}',
  '\u{1F4A0}', '\u{1F48E}', '\u{1F52E}', '\u{1F4F7}', '\u{1F3AE}', '\u{1F680}', '\u{1F916}', '\u{1F47E}',
  '\u{2744}\uFE0F', '\u{1F4A5}', '\u{1F31F}', '\u{1F9CA}', '\u{1F4CC}', '\u{1F3AF}', '\u{1F9EA}', '\u{1F4DD}',
];

function isDeadShader(e: IndexEntry): boolean {
  return (e.tags || []).some((t) => t.toLowerCase() === 'dead');
}

export function filterEntries(): IndexEntry[] {
  let out = entries;
  if (!showDeadShaders) {
    out = out.filter((e) => !isDeadShader(e));
  }
  if (!showTrashShaders) {
    out = out.filter((e) => (e.category || '').toLowerCase() !== 'trash');
  }
  if (listFormatFilter !== 'all') {
    const wantIsf = listFormatFilter === 'isf';
    out = out.filter((e) => {
      const fmt = (e.format || '').toLowerCase();
      const isIsf = fmt === 'isf';
      return isIsf === wantIsf;
    });
  }
  if (listSearchQuery) {
    const q = listSearchQuery;
    out = out.filter((e) => {
      const name = (e.fixedName || e.name || e.path || '').toLowerCase();
      const tags = (e.tags || []).join(' ').toLowerCase();
      const cat = (e.category || '').toLowerCase();
      return name.includes(q) || tags.includes(q) || cat.includes(q);
    });
  }
  if (listSetFilter) {
    out = out.filter((e) => (e.sets || []).includes(listSetFilter!));
  }
  return out;
}

export function getDeadCount(): number {
  return entries.filter((e) => isDeadShader(e)).length;
}

export function getTrashCount(): number {
  return entries.filter((e) => (e.category || '').toLowerCase() === 'trash').length;
}

export async function refetchIndexAndBuildList(): Promise<void> {
  try {
    const arr = await fetchIndex();
    const prevId = currentEntry?.id;
    setEntries(arr);
    if (prevId != null) {
      const next = arr.find((e) => e.id === prevId);
      setCurrentEntry(next ?? null);
    }
    buildList();
  } catch (e) {
    status('Index refresh failed: ' + (e as Error).message, true);
  }
}

function getAllSetNames(): string[] {
  const names = new Set<string>();
  entries.forEach((e) => (e.sets || []).forEach((s) => names.add(s)));
  return [...names].sort();
}

function getAllTagNames(): string[] {
  const names = new Set<string>();
  entries.forEach((e) => (e.tags || []).forEach((t) => names.add(t)));
  return [...names].sort();
}

function getEntriesInSet(setName: string): IndexEntry[] {
  return entries.filter((e) => (e.sets || []).includes(setName));
}

function showManageSetsModal(): void {
  const overlay = document.createElement('div');
  overlay.style.cssText = 'position:fixed;inset:0;z-index:10002;background:rgba(0,0,0,0.7);display:flex;align-items:center;justify-content:center;';
  overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };

  const box = document.createElement('div');
  box.style.cssText = 'background:var(--amiga-panel);border:2px solid var(--amiga-copper);padding:20px;max-width:420px;width:90%;max-height:80vh;overflow:auto;font-family:inherit;';
  box.onclick = (e) => e.stopPropagation();

  const title = document.createElement('div');
  title.style.cssText = 'color:var(--amiga-copper);font-weight:bold;margin-bottom:12px;font-size:13px;';
  title.textContent = 'Manage sets';
  box.appendChild(title);

  const hint = document.createElement('p');
  hint.style.cssText = 'font-size:10px;color:var(--crt-dim);margin:0 0 12px 0;';
  hint.textContent = 'Filter the list by set using the dropdown above. Assign shaders via right-click > Edit sets or the + on a shader.';
  box.appendChild(hint);

  const setNames = getAllSetNames();
  const listWrap = document.createElement('div');
  listWrap.style.cssText = 'margin-bottom:16px;';
  if (setNames.length === 0) {
    const empty = document.createElement('div');
    empty.style.cssText = 'font-size:11px;color:var(--crt-dim);';
    empty.textContent = 'No sets yet. Add a set name below, then assign shaders via right-click > Edit sets.';
    listWrap.appendChild(empty);
  } else {
    setNames.forEach((name) => {
      const row = document.createElement('div');
      row.style.cssText = 'display:flex;align-items:center;gap:8px;margin-bottom:6px;';
      const label = document.createElement('span');
      label.style.cssText = 'flex:1;font-size:12px;color:var(--crt-fg);';
      const count = getEntriesInSet(name).length;
      label.textContent = name + ' (' + count + ' shader' + (count !== 1 ? 's' : '') + ')';
      const renameBtn = document.createElement('button');
      renameBtn.type = 'button';
      renameBtn.textContent = 'Rename';
      renameBtn.style.cssText = 'font-size:10px;padding:2px 8px;background:var(--amiga-surface);color:var(--amiga-copper);border:1px solid var(--bevel-dark);cursor:pointer;';
      const removeBtn = document.createElement('button');
      removeBtn.type = 'button';
      removeBtn.textContent = 'Remove from all';
      removeBtn.style.cssText = 'font-size:10px;padding:2px 8px;background:var(--amiga-surface);color:var(--amiga-copper);border:1px solid var(--bevel-dark);cursor:pointer;';
      renameBtn.onclick = () => {
        const newName = window.prompt('Rename set to', name);
        if (newName === null || newName.trim() === '' || newName.trim() === name) return;
        const n = newName.trim();
        const toUpdate = getEntriesInSet(name);
        let done = 0;
        const next = () => {
          if (done >= toUpdate.length) {
            buildList();
            overlay.remove();
            showManageSetsModal();
            status('Set renamed to: ' + n);
            return;
          }
          const e = toUpdate[done];
          const newSets = (e.sets || []).map((s) => s === name ? n : s);
          postUpdate({ id: e.id, sets: newSets })
            .then(() => {
              e.sets = newSets;
              if (currentEntry && currentEntry.id === e.id) currentEntry.sets = newSets;
              done++;
              next();
            })
            .catch((err) => { status('Rename set failed: ' + (err as Error).message, true); done = toUpdate.length; next(); });
        };
        next();
      };
      removeBtn.onclick = () => {
        const toUpdate = getEntriesInSet(name);
        let done = 0;
        const next = () => {
          if (done >= toUpdate.length) {
            buildList();
            overlay.remove();
            showManageSetsModal();
            status('Set removed from all shaders');
            return;
          }
          const e = toUpdate[done];
          const newSets = (e.sets || []).filter((s) => s !== name);
          postUpdate({ id: e.id, sets: newSets })
            .then(() => {
              e.sets = newSets;
              if (currentEntry && currentEntry.id === e.id) currentEntry.sets = newSets;
              done++;
              next();
            })
            .catch((err) => { status('Remove set failed: ' + (err as Error).message, true); done = toUpdate.length; next(); });
        };
        next();
      };
      row.appendChild(label);
      row.appendChild(renameBtn);
      row.appendChild(removeBtn);
      listWrap.appendChild(row);
    });
  }
  box.appendChild(listWrap);

  const addSection = document.createElement('div');
  addSection.style.cssText = 'border-top:1px solid var(--bevel-dark);padding-top:12px;';
  const addLabel = document.createElement('div');
  addLabel.style.cssText = 'font-size:10px;color:var(--crt-dim);margin-bottom:6px;';
  addLabel.textContent = 'New set name (assign shaders via right-click > Edit sets or + on a shader):';
  const addRow = document.createElement('div');
  addRow.style.cssText = 'display:flex;gap:6px;align-items:center;';
  const addInput = document.createElement('input');
  addInput.type = 'text';
  addInput.placeholder = 'e.g. Live Set A';
  addInput.style.cssText = 'flex:1;padding:6px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);font-size:12px;';
  const addBtn = document.createElement('button');
  addBtn.type = 'button';
  addBtn.textContent = 'Add set';
  addBtn.style.cssText = 'padding:6px 12px;background:var(--amiga-accent);color:#fff;border:none;cursor:pointer;font-size:11px;';
  addBtn.onclick = () => {
    const n = addInput.value.trim();
    if (!n) return;
    status('Set "' + n + '" added. Assign shaders via right-click > Edit sets or the + pill.');
    addInput.value = '';
    overlay.remove();
  };
  addRow.appendChild(addInput);
  addRow.appendChild(addBtn);
  addSection.appendChild(addLabel);
  addSection.appendChild(addRow);
  box.appendChild(addSection);

  const closeBtn = document.createElement('button');
  closeBtn.type = 'button';
  closeBtn.textContent = 'Close';
  closeBtn.style.cssText = 'margin-top:12px;padding:6px 12px;background:var(--amiga-surface);color:var(--crt-fg);border:1px solid var(--bevel-dark);cursor:pointer;font-size:11px;';
  closeBtn.onclick = () => overlay.remove();
  box.appendChild(closeBtn);

  overlay.appendChild(box);
  document.body.appendChild(overlay);
}

function formatLastSaved(ms: number | undefined): string {
  if (ms == null) return '';
  const sec = Math.floor((Date.now() - ms) / 1000);
  if (sec < 60) return 'saved just now';
  if (sec < 3600) return 'saved ' + Math.floor(sec / 60) + 'm ago';
  if (sec < 86400) return 'saved ' + Math.floor(sec / 3600) + 'h ago';
  return 'saved ' + Math.floor(sec / 86400) + 'd ago';
}

function slugForClass(s: string): string {
  return (s || '').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '') || 'none';
}

export function buildList(): void {
  initThumbnailCaptureListener();
  const listEl = el('list');
  if (!listEl) return;

  const trashLabel = document.getElementById('trashToggleLabel');
  const trashBadge = document.getElementById('trashCountBadge');
  const trashCount = getTrashCount();
  if (trashLabel) {
    trashLabel.style.display = trashCount > 0 ? '' : 'none';
    if (trashBadge) trashBadge.textContent = '(' + trashCount + ')';
  }
  const indexHeaderEl = document.getElementById('indexHeaderText');
  if (indexHeaderEl) indexHeaderEl.textContent = 'Index (' + entries.length + ')';

  const setSelect = document.getElementById('listSetFilter') as HTMLSelectElement | null;
  if (setSelect) {
    const currentVal = listSetFilter || '';
    const setNames = getAllSetNames();
    setSelect.innerHTML = '<option value="">All sets</option>' + setNames.map((s) => '<option value="' + escapeHtml(s) + '">' + escapeHtml(s) + '</option>').join('');
    if (setNames.includes(currentVal)) {
      setSelect.value = currentVal;
    } else {
      setSelect.value = '';
      setListSetFilter(null);
    }
  }

  const filtered = filterEntries();
  listEl.innerHTML = '';
  const listMode = appSettings.listViewMode || 'list';
  const isGrid = listMode === 'grid';
  const isCompact = listMode === 'compact';
  listEl.classList.toggle('list--grid', isGrid);
  listEl.classList.toggle('list--compact', isCompact && !isGrid);

  if (filtered.length === 0) {
    const empty = document.createElement('div');
    empty.className = 'list-empty';
    empty.textContent = entries.length === 0
      ? 'No shaders. Open Paths to add sources and Reindex.'
      : 'No shaders match this filter.';
    empty.style.cssText = 'padding:12px;color:var(--crt-dim);font-size:11px;';
    listEl.appendChild(empty);
    return;
  }

  const showThumbs = !!appSettings.showThumbnails;

  const VJ_DROP_TYPE = 'application/x-macroverse-shader-path';
  filtered.forEach((entry: IndexEntry, i: number) => {
    const item = document.createElement('div');
    item.className = 'list-item';
    item.draggable = !!entry.path;
    item.dataset.index = String(i);
    if (entry.path) item.dataset.path = entry.path;
    const id = entry.id != null ? entry.id : i;
    const indexNum = entries.indexOf(entry) + 1;
    let name = entry.fixedName || entry.name || entry.path || ('Shader ' + id);
    const pathFilename = entry.path ? entry.path.replace(/^.*[/\\]/, '') : '';
    const isGenericName = /^(new\s+(isf\s+)?shader|newshader)$/i.test(name.trim());
    if (isGenericName && pathFilename) name = name + ' (' + pathFilename + ')';
    const starCls = entry.favorite ? ' star-fav' : '';
    const swatchStyle = entry.color ? `background:${entry.color}` : 'background:transparent';
    const tagsList = (entry.tags || []).join(', ');
    const hasColor = !!entry.color;
    const isIsf = (entry.format || '').toLowerCase() === 'isf';
    const thumbUrl = entry.path ? getThumbnail(entry.path) : '';

    let thumbHtml = '';
    if (showThumbs) {
      if (thumbUrl) {
        thumbHtml = '<img class="list-item-thumb" src="' + escapeHtml(thumbUrl) + '" alt="">';
      } else {
        thumbHtml = '<span class="list-item-thumb-placeholder">' + (name.charAt(0) || '?') + '</span>';
      }
    }

    const isfBadge = isIsf ? '<span class="isf-wire-badge" title="ISF / Wire-ready">ISF</span>' : '';
    const lastSaved = entry.path ? getLastSaved(entry.path) : undefined;
    const metaStr = formatLastSaved(lastSaved);
    const metaHtml = metaStr ? '<span class="list-item-meta">' + escapeHtml(metaStr) + '</span>' : '';

    // Build tag pills HTML
    let tagsHtml = '';
    if (tagsList) {
      const tagPills = (entry.tags || []).map((t) =>
        '<span class="tag-pill" data-tag="' + escapeHtml(t) + '">' + escapeHtml(t) +
        '<span class="tag-pill-x" title="Remove tag">&times;</span></span>'
      ).join('');
      tagsHtml = '<span class="list-item-tags" data-entry-id="' + id + '">' + tagPills +
        '<span class="tag-pill tag-pill-add" title="Add tag">+</span></span>';
    } else {
      tagsHtml = '<span class="list-item-tags list-item-tags-empty" data-entry-id="' + id + '" title="Click to add tags">+</span>';
    }

    const setsList = (entry.sets || []).join(', ');
    let setsHtml = '';
    if (setsList) {
      const setPills = (entry.sets || []).map((s) =>
        '<span class="set-pill" data-set="' + escapeHtml(s) + '">' + escapeHtml(s) +
        '<span class="set-pill-x" title="Remove from set">&times;</span></span>'
      ).join('');
      setsHtml = '<span class="list-item-sets" data-entry-id="' + id + '">' + setPills +
        '<span class="set-pill set-pill-add" title="Add to set">+</span></span>';
    } else {
      setsHtml = '<span class="list-item-sets list-item-sets-empty" data-entry-id="' + id + '" title="Click to add to set">Set +</span>';
    }

    // Color highlight: apply to the entire row background
    if (hasColor) {
      item.style.borderLeft = '4px solid ' + (entry.color || '');
      item.style.background = (entry.color || '') + '18';
    }

    const emoji = entry.notes || '';
    const emojiBadge = emoji
      ? '<span class="emoji-badge" data-id="' + id + '" title="Click to change icon">' + emoji + '</span>'
      : '<span class="emoji-badge emoji-badge-empty" data-id="' + id + '" title="Click to set icon"></span>';

    item.innerHTML =
      '<span class="star' + starCls + '" data-id="' + id + '" title="Toggle favorite">*</span>' +
      '<span class="swatch" style="' + swatchStyle + '" data-id="' + id + '" title="Click: cycle color / Right-click: clear"></span>' +
      thumbHtml +
      emojiBadge +
      '<span class="list-item-index" title="Index number">' + indexNum + '.</span>' +
      '<span class="list-item-name"' + (hasColor ? ' style="color:' + escapeHtml(entry.color || '') + '"' : '') +
      ' title="Double-click to rename">' +
      escapeHtml(name) + '</span>' +
      isfBadge +
      tagsHtml +
      setsHtml +
      metaHtml;

    item.classList.toggle('has-color', hasColor);
    if (isDeadShader(entry)) item.classList.add('dead-shader');
    if (currentEntry && entry.id === currentEntry.id) item.classList.add('active');
    const catSlug = slugForClass(entry.category || '');
    if (catSlug) item.classList.add('list-item--cat-' + catSlug);
    const fmt = (entry.format || '').toLowerCase();
    if (fmt === 'isf') item.classList.add('list-item--format-isf');
    else if (fmt === 'glsl' || entry.path?.toLowerCase().endsWith('.glsl')) item.classList.add('list-item--format-glsl');
    (entry.tags || []).slice(0, 5).forEach((t) => {
      const tagSlug = slugForClass(t);
      if (tagSlug) item.classList.add('list-item--tag-' + tagSlug);
    });

    if (entry.path) {
      item.addEventListener('dragstart', (ev: DragEvent) => {
        if (!ev.dataTransfer) return;
        ev.dataTransfer.setData('text/plain', entry.path ?? '');
        ev.dataTransfer.setData(VJ_DROP_TYPE, entry.path ?? '');
        ev.dataTransfer.effectAllowed = 'copy';
      });
    }

    const starEl = item.querySelector('.star');
    starEl?.addEventListener('click', (ev) => {
      ev.stopPropagation();
      toggleFavorite(entry);
    });

    const swatchEl = item.querySelector('.swatch');
    swatchEl?.addEventListener('click', (ev) => {
      ev.stopPropagation();
      cycleColor(entry);
    });
    swatchEl?.addEventListener('contextmenu', (ev) => {
      ev.preventDefault();
      ev.stopPropagation();
      clearColor(entry);
    });

    // Emoji badge click
    const emojiBadgeEl = item.querySelector('.emoji-badge');
    if (emojiBadgeEl) {
      emojiBadgeEl.addEventListener('click', (ev) => {
        ev.stopPropagation();
        showEmojiPicker(ev as MouseEvent, entry);
      });
    }

    // Tag pill interactions
    item.querySelectorAll('.tag-pill-x').forEach((xBtn) => {
      xBtn.addEventListener('click', (ev) => {
        ev.stopPropagation();
        const pill = (ev.target as HTMLElement).parentElement;
        const tag = pill?.dataset.tag;
        if (tag) removeTag(entry, tag);
      });
    });
    const addPill = item.querySelector('.tag-pill-add');
    if (addPill) {
      addPill.addEventListener('click', (ev) => {
        ev.stopPropagation();
        promptAddTag(entry);
      });
    }
    const tagsEmptyEl = item.querySelector('.list-item-tags-empty');
    if (tagsEmptyEl) {
      tagsEmptyEl.addEventListener('click', (ev) => {
        ev.stopPropagation();
        promptAddTag(entry);
      });
    }

    item.querySelectorAll('.set-pill-x').forEach((xBtn) => {
      xBtn.addEventListener('click', (ev) => {
        ev.stopPropagation();
        const pill = (ev.target as HTMLElement).parentElement;
        const setName = pill?.dataset.set;
        if (setName) removeFromSet(entry, setName);
      });
    });
    const addSetPill = item.querySelector('.set-pill-add');
    if (addSetPill) {
      addSetPill.addEventListener('click', (ev) => {
        ev.stopPropagation();
        showSetEditor(entry, ev.currentTarget as HTMLElement);
      });
    }
    const setsEmptyEl = item.querySelector('.list-item-sets-empty');
    if (setsEmptyEl) {
      setsEmptyEl.addEventListener('click', (ev) => {
        ev.stopPropagation();
        showSetEditor(entry, ev.currentTarget as HTMLElement);
      });
    }

    // Double-click name to rename
    const nameEl = item.querySelector('.list-item-name');
    if (nameEl) {
      nameEl.addEventListener('dblclick', (ev) => {
        ev.stopPropagation();
        openInlineRename(entry, nameEl as HTMLElement);
      });
    }

    item.onclick = () => selectShader(entry, item);

    item.addEventListener('contextmenu', (ev) => {
      ev.preventDefault();
      showShaderContextMenu(ev, entry, item);
    });

    listEl.appendChild(item);
  });
}

function initThumbnailCaptureListener(): void {
  if ((window as unknown as { __thumbnailCaptureListener?: boolean }).__thumbnailCaptureListener) return;
  (window as unknown as { __thumbnailCaptureListener?: boolean }).__thumbnailCaptureListener = true;
  window.addEventListener('thumbnail-captured', (e: Event) => {
    const path = (e as CustomEvent).detail?.path;
    if (!path) return;
    const dataUrl = getThumbnail(path);
    if (!dataUrl) return;
    const listEl = el('list');
    if (!listEl) return;
    const item = [...listEl.querySelectorAll('.list-item')].find(
      (node) => (node as HTMLElement).dataset.path === path
    ) as HTMLElement | undefined;
    if (!item) return;
    const placeholder = item.querySelector('.list-item-thumb-placeholder');
    const img = item.querySelector('.list-item-thumb') as HTMLImageElement | null;
    if (placeholder && !img) {
      const imgEl = document.createElement('img');
      imgEl.className = 'list-item-thumb';
      imgEl.src = dataUrl;
      imgEl.alt = '';
      placeholder.replaceWith(imgEl);
    } else if (img) {
      img.src = dataUrl;
    }
  });
}

function removeTag(entry: IndexEntry, tag: string): void {
  const tags = (entry.tags || []).filter((t) => t !== tag);
  postUpdate({ id: entry.id, tags })
    .then(() => {
      entry.tags = tags;
      if (currentEntry && currentEntry.id === entry.id) currentEntry.tags = tags;
      buildList();
      status('Removed tag: ' + tag);
    })
    .catch((e) => status('Tag remove: ' + (e as Error).message, true));
}

let tagsModalOverlay: HTMLElement | null = null;

function closeTagsModal(): void {
  if (tagsModalOverlay) {
    tagsModalOverlay.remove();
    tagsModalOverlay = null;
  }
  document.removeEventListener('keydown', tagsModalKeydown);
}

function tagsModalKeydown(e: KeyboardEvent): void {
  if (e.key === 'Escape') closeTagsModal();
}

function showTagsModal(entry: IndexEntry): void {
  closeTagsModal();
  const overlay = document.createElement('div');
  tagsModalOverlay = overlay;
  overlay.style.cssText = 'position:fixed;inset:0;z-index:10002;background:rgba(0,0,0,0.6);display:flex;align-items:center;justify-content:center;';
  overlay.onclick = (e) => { if (e.target === overlay) closeTagsModal(); };

  const box = document.createElement('div');
  box.style.cssText = 'background:var(--amiga-panel);border:2px solid var(--bevel-dark);padding:16px;min-width:280px;max-width:400px;max-height:85vh;overflow:auto;box-shadow:0 8px 24px rgba(0,0,0,0.5);';
  box.onclick = (e) => e.stopPropagation();

  const name = entry.fixedName || entry.name || entry.path || 'Shader';
  const title = document.createElement('div');
  title.style.cssText = 'color:var(--amiga-copper);font-size:11px;text-transform:uppercase;margin-bottom:10px;';
  title.textContent = 'Manage tags: ' + escapeHtml(name.length > 40 ? name.slice(0, 37) + '...' : name);
  box.appendChild(title);

  const currentTags = [...(entry.tags || [])];
  const listWrap = document.createElement('div');
  listWrap.style.cssText = 'min-height:32px;margin-bottom:12px;display:flex;flex-wrap:wrap;gap:6px;align-items:center;';
  function renderTags(): void {
    listWrap.innerHTML = '';
    currentTags.forEach((tag) => {
      const pill = document.createElement('span');
      pill.style.cssText = 'display:inline-flex;align-items:center;gap:4px;padding:2px 8px;font-size:10px;background:var(--amiga-surface);border:1px solid var(--bevel-dark);border-radius:6px;';
      pill.textContent = tag;
      const x = document.createElement('span');
      x.style.cssText = 'cursor:pointer;color:var(--crt-dim);';
      x.textContent = '\u00D7';
      x.title = 'Remove tag';
      x.onclick = () => {
        currentTags.splice(currentTags.indexOf(tag), 1);
        applyTags();
        renderTags();
      };
      pill.appendChild(x);
      listWrap.appendChild(pill);
    });
    if (currentTags.length === 0) {
      const empty = document.createElement('span');
      empty.style.cssText = 'font-size:10px;color:var(--crt-dim);';
      empty.textContent = 'No tags yet. Add below or pick from index.';
      listWrap.appendChild(empty);
    }
  }
  function applyTags(): void {
    postUpdate({ id: entry.id, tags: currentTags })
      .then(() => {
        entry.tags = currentTags;
        if (currentEntry && currentEntry.id === entry.id) currentEntry.tags = currentTags;
        buildList();
        status('Tags updated');
      })
      .catch((e) => status('Tags: ' + (e as Error).message, true));
  }
  renderTags();

  const addRow = document.createElement('div');
  addRow.style.cssText = 'display:flex;gap:6px;align-items:center;margin-bottom:12px;';
  const addInput = document.createElement('input');
  addInput.type = 'text';
  addInput.placeholder = 'New tag name';
  addInput.style.cssText = 'flex:1;padding:6px 8px;font-size:11px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);font-family:inherit;';
  const addBtn = document.createElement('button');
  addBtn.type = 'button';
  addBtn.textContent = 'Add';
  addBtn.style.cssText = 'padding:6px 12px;font-size:10px;background:var(--amiga-copper);color:var(--amiga-bg);border:none;cursor:pointer;font-family:inherit;';
  addBtn.onclick = () => {
    const t = addInput.value.trim();
    if (!t || currentTags.includes(t)) { addInput.value = ''; return; }
    currentTags.push(t);
    currentTags.sort();
    addInput.value = '';
    applyTags();
    renderTags();
  };
  addInput.onkeydown = (ev) => { if (ev.key === 'Enter') { ev.preventDefault(); addBtn.click(); } };
  addRow.appendChild(addInput);
  addRow.appendChild(addBtn);
  box.appendChild(listWrap);
  box.appendChild(addRow);

  const allTagsLabel = document.createElement('div');
  allTagsLabel.style.cssText = 'font-size:9px;color:var(--crt-dim);text-transform:uppercase;margin-bottom:6px;';
  allTagsLabel.textContent = 'Click to add from index';
  box.appendChild(allTagsLabel);
  const allTagsWrap = document.createElement('div');
  allTagsWrap.style.cssText = 'display:flex;flex-wrap:wrap;gap:4px;margin-bottom:12px;';
  const allTags = getAllTagNames().filter((t) => !currentTags.includes(t));
  allTags.forEach((tag) => {
    const pill = document.createElement('button');
    pill.type = 'button';
    pill.textContent = tag;
    pill.style.cssText = 'padding:2px 8px;font-size:10px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);cursor:pointer;font-family:inherit;';
    pill.onclick = () => {
      currentTags.push(tag);
      currentTags.sort();
      applyTags();
      renderTags();
      pill.remove();
    };
    allTagsWrap.appendChild(pill);
  });
  if (allTags.length === 0) {
    const empty = document.createElement('span');
    empty.style.cssText = 'font-size:10px;color:var(--crt-dim);';
    empty.textContent = 'All index tags already on this shader.';
    allTagsWrap.appendChild(empty);
  }
  box.appendChild(allTagsWrap);

  const doneBtn = document.createElement('button');
  doneBtn.type = 'button';
  doneBtn.textContent = 'Done';
  doneBtn.style.cssText = 'width:100%;padding:8px;font-size:11px;background:var(--amiga-accent);color:var(--amiga-bg);border:none;cursor:pointer;font-family:inherit;';
  doneBtn.onclick = closeTagsModal;
  box.appendChild(doneBtn);

  overlay.appendChild(box);
  document.body.appendChild(overlay);
  document.addEventListener('keydown', tagsModalKeydown);
  addInput.focus();
}

function promptAddTag(entry: IndexEntry): void {
  showTagsModal(entry);
}

function removeFromSet(entry: IndexEntry, setName: string): void {
  const sets = (entry.sets || []).filter((s) => s !== setName);
  postUpdate({ id: entry.id, sets })
    .then(() => {
      entry.sets = sets;
      if (currentEntry && currentEntry.id === entry.id) currentEntry.sets = sets;
      buildList();
      status('Removed from set: ' + setName);
    })
    .catch((e) => status('Set remove: ' + (e as Error).message, true));
}

let setEditorOverlay: HTMLElement | null = null;

function closeSetEditor(): void {
  if (setEditorOverlay) {
    setEditorOverlay.remove();
    setEditorOverlay = null;
  }
  document.removeEventListener('keydown', setEditorKeydown);
}

function setEditorKeydown(e: KeyboardEvent): void {
  if (e.key === 'Escape') closeSetEditor();
}

function showSetEditor(entry: IndexEntry, _anchor?: HTMLElement | { x: number; y: number }): void {
  closeSetEditor();
  const overlay = document.createElement('div');
  setEditorOverlay = overlay;
  overlay.style.cssText = 'position:fixed;inset:0;z-index:10002;background:rgba(0,0,0,0.6);display:flex;align-items:center;justify-content:center;';
  overlay.onclick = (e) => { if (e.target === overlay) closeSetEditor(); };

  const box = document.createElement('div');
  box.className = 'set-editor-popover';
  box.style.cssText = 'background:var(--amiga-panel);border:2px solid var(--bevel-dark);padding:16px;min-width:260px;max-width:360px;max-height:85vh;overflow:auto;box-shadow:0 8px 24px rgba(0,0,0,0.5);';
  box.onclick = (e) => e.stopPropagation();

  const name = entry.fixedName || entry.name || entry.path || 'Shader';
  const title = document.createElement('div');
  title.style.cssText = 'color:var(--amiga-copper);font-size:11px;text-transform:uppercase;margin-bottom:8px;';
  title.textContent = 'Add to set(s): ' + (name.length > 35 ? name.slice(0, 32) + '...' : name);
  box.appendChild(title);

  const hint = document.createElement('div');
  hint.style.cssText = 'font-size:9px;color:var(--crt-dim);margin-bottom:10px;';
  hint.textContent = 'Check sets to add this shader to. Filter by set in the index dropdown.';
  box.appendChild(hint);

  const currentSets = new Set(entry.sets || []);
  const listWrap = document.createElement('div');
  listWrap.style.cssText = 'overflow-y:auto;max-height:200px;margin-bottom:12px;';
  listWrap.className = 'set-editor-list';

  function applySets(sets: string[]): void {
    postUpdate({ id: entry.id, sets })
      .then(() => {
        entry.sets = sets;
        if (currentEntry && currentEntry.id === entry.id) currentEntry.sets = sets;
        buildList();
        renderSetList();
        status('Sets updated');
      })
      .catch((e) => status('Sets: ' + (e as Error).message, true));
  }

  function renderSetList(): void {
    listWrap.innerHTML = '';
    const names = getAllSetNames();
    names.forEach((setName) => {
      const row = document.createElement('label');
      row.style.cssText = 'display:flex;align-items:center;gap:8px;padding:6px 0;cursor:pointer;font-size:11px;color:var(--crt-fg);';
      const cb = document.createElement('input');
      cb.type = 'checkbox';
      cb.checked = currentSets.has(setName);
      cb.style.cssText = 'margin:0;flex-shrink:0;';
      cb.addEventListener('change', () => {
        if (cb.checked) currentSets.add(setName);
        else currentSets.delete(setName);
        applySets([...currentSets]);
      });
      row.appendChild(cb);
      row.appendChild(document.createTextNode(setName));
      listWrap.appendChild(row);
    });
    if (names.length === 0) {
      const empty = document.createElement('div');
      empty.style.cssText = 'font-size:10px;color:var(--crt-dim);';
      empty.textContent = 'No sets yet. Create one below.';
      listWrap.appendChild(empty);
    }
  }
  renderSetList();

  box.appendChild(listWrap);

  const addRow = document.createElement('div');
  addRow.style.cssText = 'display:flex;gap:6px;align-items:center;border-top:1px solid var(--bevel-dark);padding-top:10px;margin-bottom:12px;';
  const addInput = document.createElement('input');
  addInput.type = 'text';
  addInput.placeholder = 'New set name';
  addInput.style.cssText = 'flex:1;padding:6px 8px;font-size:11px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);font-family:inherit;';
  const datalist = document.createElement('datalist');
  datalist.id = 'set-editor-datalist-' + entry.id;
  getAllSetNames().forEach((s) => {
    const opt = document.createElement('option');
    opt.value = s;
    datalist.appendChild(opt);
  });
  addInput.setAttribute('list', datalist.id);
  const addBtn = document.createElement('button');
  addBtn.type = 'button';
  addBtn.textContent = 'Add set';
  addBtn.style.cssText = 'padding:6px 12px;font-size:10px;background:var(--amiga-copper);color:var(--amiga-bg);border:none;cursor:pointer;font-family:inherit;';
  addBtn.onclick = () => {
    const setName = addInput.value.trim();
    if (!setName) return;
    if (currentSets.has(setName)) {
      addInput.value = '';
      return;
    }
    currentSets.add(setName);
    addInput.value = '';
    applySets([...currentSets]);
    renderSetList();
    const newOpt = document.createElement('option');
    newOpt.value = setName;
    datalist.appendChild(newOpt);
  };
  addInput.addEventListener('keydown', (ev) => {
    if (ev.key === 'Enter') { ev.preventDefault(); addBtn.click(); }
  });
  addRow.appendChild(addInput);
  addRow.appendChild(addBtn);
  box.appendChild(addRow);
  box.appendChild(datalist);

  const doneBtn = document.createElement('button');
  doneBtn.type = 'button';
  doneBtn.textContent = 'Done';
  doneBtn.style.cssText = 'width:100%;padding:8px;font-size:11px;background:var(--amiga-accent);color:var(--amiga-bg);border:none;cursor:pointer;font-family:inherit;';
  doneBtn.onclick = closeSetEditor;
  box.appendChild(doneBtn);

  overlay.appendChild(box);
  document.body.appendChild(overlay);
  document.addEventListener('keydown', setEditorKeydown);
  addInput.focus();
}

function openInlineRename(entry: IndexEntry, nameEl: HTMLElement): void {
  const currentName = entry.fixedName || entry.name || '';
  const stem = currentName.replace(/\.\w+$/, '');
  const input = document.createElement('input');
  input.type = 'text';
  input.value = stem;
  input.className = 'inline-rename-input';
  input.style.cssText = 'width:100%;background:var(--editor-bg);color:var(--crt-fg);border:1px solid var(--amiga-accent);font-size:12px;padding:2px 4px;outline:none;';
  nameEl.textContent = '';
  nameEl.appendChild(input);
  input.focus();
  input.select();

  const commit = () => {
    const newName = input.value.trim();
    if (!newName || newName === stem) {
      buildList();
      return;
    }
    postShaderRename({ id: entry.id, newName })
      .then((resp) => {
        if (resp.path) entry.path = resp.path;
        if (resp.name) {
          entry.name = resp.name;
          entry.fixedName = resp.name;
        }
        if (currentEntry && currentEntry.id === entry.id) {
          currentEntry.path = entry.path;
          currentEntry.name = entry.name;
          currentEntry.fixedName = entry.fixedName;
        }
        buildList();
        status('Renamed to: ' + newName);
      })
      .catch((e) => {
        status('Rename: ' + (e as Error).message, true);
        buildList();
      });
  };

  input.addEventListener('keydown', (ev) => {
    if (ev.key === 'Enter') { ev.preventDefault(); commit(); }
    if (ev.key === 'Escape') { buildList(); }
  });
  input.addEventListener('blur', commit);
}

const CATEGORIES = ['3d', 'abstract', 'color', 'fractal', 'geometric', 'grid', 'misc',
  'noise', 'particles', 'plasma', 'psychedelic', 'space', 'tunnel', 'water', 'trash'];

function showMoveMenu(ev: MouseEvent, entry: IndexEntry): void {
  hideCtxMenu();
  const menu = document.createElement('div');
  menu.className = 'ctx-menu';
  menu.style.cssText = 'position:fixed;left:' + ev.clientX + 'px;top:' + ev.clientY +
    'px;background:var(--amiga-panel);border:2px solid var(--bevel-dark);padding:4px 0;min-width:180px;z-index:10001;box-shadow:4px 4px 12px rgba(0,0,0,0.5);max-height:400px;overflow-y:auto;';
  ctxMenuEl = menu;

  const title = document.createElement('div');
  title.style.cssText = 'padding:6px 12px;font-size:11px;color:var(--crt-dim);border-bottom:1px solid var(--bevel-dark);';
  title.textContent = 'Move to category:';
  menu.appendChild(title);

  CATEGORIES.forEach((cat) => {
    if (cat === 'trash') {
      const sep = document.createElement('div');
      sep.style.cssText = 'height:1px;background:var(--bevel-dark);margin:6px 0;';
      menu.appendChild(sep);
    }
    const row = document.createElement('div');
    row.className = 'ctx-menu-item';
    const isCurrent = (entry.category || '').toLowerCase() === cat;
    const isTrash = cat === 'trash';
    row.style.cssText = 'padding:6px 12px;cursor:pointer;font-size:12px;color:' +
      (isCurrent ? 'var(--amiga-accent)' : isTrash ? '#e74c3c' : 'var(--crt-fg)') + ';';
    row.textContent = (isCurrent ? '> ' : '  ') + (isTrash ? '\u{1F5D1} trash' : cat);
    row.onmouseenter = () => { row.style.background = isTrash ? 'rgba(231,76,60,0.15)' : 'var(--amiga-surface)'; };
    row.onmouseleave = () => { row.style.background = ''; };
    row.onclick = (e) => {
      e.stopPropagation();
      hideCtxMenu();
      if (isCurrent) return;
      postShaderMove({ id: entry.id, category: cat })
        .then(() => {
          refetchIndexAndBuildList();
          status('Moved to: ' + cat);
        })
        .catch((e2) => status('Move: ' + (e2 as Error).message, true));
    };
    menu.appendChild(row);
  });

  document.body.appendChild(menu);
  clampContextMenuToViewport(menu);
  setTimeout(() => document.addEventListener('click', hideCtxMenu), 0);
}

let ctxMenuEl: HTMLElement | null = null;
function hideCtxMenu(): void {
  if (ctxMenuEl) {
    ctxMenuEl.remove();
    ctxMenuEl = null;
  }
  document.removeEventListener('click', hideCtxMenu);
}

function showShaderContextMenu(ev: MouseEvent, entry: IndexEntry, itemEl: HTMLElement): void {
  ev.preventDefault();
  hideCtxMenu();
  const path = (entry.path || '').replace(/\\/g, '|');
  if (!path) return;
  const pathNorm = path.replace(/\|/g, String.fromCharCode(92));
  const name = entry.fixedName || entry.name || entry.path || '';

  const menu = document.createElement('div');
  menu.className = 'ctx-menu';
  menu.style.cssText = 'position:fixed;left:' + ev.clientX + 'px;top:' + ev.clientY + 'px;background:var(--amiga-panel);border:2px solid var(--bevel-dark);padding:4px 0;min-width:200px;z-index:10001;box-shadow:4px 4px 12px rgba(0,0,0,0.5);';
  ctxMenuEl = menu;

  function addItem(label: string, onClick: () => void): void {
    const row = document.createElement('div');
    row.className = 'ctx-menu-item';
    row.style.cssText = 'padding:6px 12px;cursor:pointer;font-size:12px;color:var(--crt-fg);';
    row.textContent = label;
    row.onmouseenter = () => { row.style.background = 'var(--amiga-surface)'; };
    row.onmouseleave = () => { row.style.background = ''; };
    row.onclick = (e) => { e.stopPropagation(); onClick(); hideCtxMenu(); };
    menu.appendChild(row);
  }

  addItem('Rename shader...', () => {
    const stem = (entry.fixedName || entry.name || '').replace(/\.\w+$/, '');
    const newName = window.prompt('New name:', stem);
    if (newName && newName.trim() && newName.trim() !== stem) {
      postShaderRename({ id: entry.id, newName: newName.trim() })
        .then((resp) => {
          if (resp.path) entry.path = resp.path;
          if (resp.name) { entry.name = resp.name; entry.fixedName = resp.name; }
          if (currentEntry && currentEntry.id === entry.id) {
            currentEntry.path = entry.path; currentEntry.name = entry.name; currentEntry.fixedName = entry.fixedName;
          }
          buildList();
          status('Renamed to: ' + newName.trim());
        })
        .catch((e) => status('Rename: ' + (e as Error).message, true));
    }
  });
  addItem('Move to category...', () => {
    showMoveMenu(ev, entry);
  });
  addItem('Edit tags...', () => {
    hideCtxMenu();
    setTimeout(() => showTagsModal(entry), 0);
  });
  addItem('Edit sets...', () => {
    hideCtxMenu();
    const x = ev.clientX;
    const y = ev.clientY;
    setTimeout(() => showSetEditor(entry, { x, y }), 0);
  });
  addItem('Trash shader', () => {
    postShaderMove({ id: entry.id, category: 'trash' })
      .then(() => {
        refetchIndexAndBuildList();
        status('Moved to trash');
      })
      .catch((e2) => status('Trash: ' + (e2 as Error).message, true));
  });

  const sep0 = document.createElement('div');
  sep0.style.cssText = 'height:1px;background:var(--bevel-dark);margin:4px 0;';
  menu.appendChild(sep0);

  addItem('Set icon / emoji...', () => { showEmojiPicker(ev, entry); });
  addItem('Set color...', () => { cycleColor(entry); });
  addItem('Clear color', () => { clearColor(entry); });

  const sep1 = document.createElement('div');
  sep1.style.cssText = 'height:1px;background:var(--bevel-dark);margin:4px 0;';
  menu.appendChild(sep1);

  addItem('Copy path to clipboard', () => {
    navigator.clipboard.writeText(pathNorm).then(() => status('Path copied')).catch(() => status('Copy failed', true));
  });
  addItem('Generate thumbnail', () => {
    if (!entry.path) return;
    status('Generating thumbnail in background...');
    fetchShader(entry.path)
      .then((src) => {
        const dataUrl = renderThumbnailSync(src);
        if (dataUrl) {
          setThumbnail(entry.path!, dataUrl);
          postThumbnailSave({ path: entry.path!, dataUrl }).catch(() => {});
          window.dispatchEvent(new CustomEvent('thumbnail-captured', { detail: { path: entry.path } }));
          buildList();
          status('Thumbnail generated');
        } else {
          status('Failed to render thumbnail', true);
        }
      })
      .catch(() => status('Failed to load shader for thumbnail', true));
  });
  addItem('Open in Cursor', () => {
    postOpenInCursor({ path }).then(() => status('Opened in Cursor')).catch((e) => status('Cursor: ' + (e as Error).message, true));
  });
  addItem('Open in file explorer', () => {
    postOpenInExplorer({ path }).then(() => status('Opened in explorer')).catch((e) => status('Explorer: ' + (e as Error).message, true));
  });
  addItem('Open with Notepad', () => {
    postOpenInNotepad({ path }).then(() => status('Opened in Notepad')).catch((e) => status('Notepad: ' + (e as Error).message, true));
  });
  // "See versions" uses a special pattern: don't dismiss menu, replace contents inline
  const versionsRow = document.createElement('div');
  versionsRow.className = 'ctx-menu-item';
  versionsRow.style.cssText = 'padding:6px 12px;cursor:pointer;font-size:12px;color:var(--crt-fg);';
  versionsRow.textContent = 'See versions...';
  versionsRow.onmouseenter = () => { versionsRow.style.background = 'var(--amiga-surface)'; };
  versionsRow.onmouseleave = () => { versionsRow.style.background = ''; };
  versionsRow.onclick = (e) => {
    e.stopPropagation();
    // Do NOT call hideCtxMenu -- replace menu content in place
    versionsRow.textContent = 'Loading...';
    fetchGitLog(path).then((versions: { sha: string; date: string; subject: string }[]) => {
      menu.innerHTML = '';
      const header = document.createElement('div');
      header.style.cssText = 'padding:6px 12px;font-size:10px;color:var(--amiga-copper);border-bottom:1px solid var(--bevel-dark);text-transform:uppercase;';
      header.textContent = 'Version History (' + versions.length + ' revisions)';
      menu.appendChild(header);
      const back = document.createElement('div');
      back.className = 'ctx-menu-item';
      back.style.cssText = 'padding:6px 12px;cursor:pointer;font-size:12px;color:var(--amiga-copper);border-bottom:1px solid var(--bevel-dark);';
      back.textContent = '< Close';
      back.onclick = () => hideCtxMenu();
      menu.appendChild(back);
      if (versions.length === 0) {
        const row = document.createElement('div');
        row.style.cssText = 'padding:8px 12px;font-size:11px;color:var(--crt-dim);';
        row.textContent = 'No git history yet. Save the shader to create the first version.';
        menu.appendChild(row);
      } else {
        versions.slice(0, 20).forEach((v, i) => {
          const d = v.date.slice(0, 10) + ' ' + v.date.slice(11, 19);
          const vRow = document.createElement('div');
          vRow.className = 'ctx-menu-item';
          vRow.style.cssText = 'padding:6px 12px;cursor:pointer;font-size:11px;color:var(--crt-fg);display:flex;gap:6px;align-items:center;';
          const numSpan = document.createElement('span');
          numSpan.style.cssText = 'color:var(--amiga-accent);font-size:10px;min-width:20px;';
          numSpan.textContent = '#' + (versions.length - i);
          const dateSpan = document.createElement('span');
          dateSpan.style.cssText = 'color:var(--crt-dim);font-size:10px;min-width:120px;';
          dateSpan.textContent = d;
          const msgSpan = document.createElement('span');
          msgSpan.style.cssText = 'overflow:hidden;text-overflow:ellipsis;white-space:nowrap;';
          msgSpan.textContent = v.subject.length > 40 ? v.subject.slice(0, 40) + '...' : v.subject;
          vRow.appendChild(numSpan);
          vRow.appendChild(dateSpan);
          vRow.appendChild(msgSpan);
          vRow.onmouseenter = () => { vRow.style.background = 'var(--amiga-surface)'; };
          vRow.onmouseleave = () => { vRow.style.background = ''; };
          vRow.onclick = (ev) => {
            ev.stopPropagation();
            hideCtxMenu();
            postGitRevertVersion({ path, sha: v.sha })
              .then(() => {
                setCurrentEntry(entry);
                loadShader(entry);
                buildList();
                status('Reverted to version #' + (versions.length - i) + ' (' + d + ')');
              })
              .catch((err) => status('Revert: ' + (err as Error).message, true));
          };
          menu.appendChild(vRow);
        });
      }
    }).catch(() => {
      menu.innerHTML = '';
      const row = document.createElement('div');
      row.style.cssText = 'padding:8px 12px;font-size:11px;color:var(--crt-dim);';
      row.textContent = 'Failed to load version history';
      menu.appendChild(row);
    });
  };
  menu.appendChild(versionsRow);
  addItem('Delete shader file...', () => {
    if (!window.confirm('Delete shader file and remove from index? This cannot be undone.')) return;
    postShaderDelete({ id: entry.id, paths: [path], confirm: true })
      .then(() => {
        refetchIndexAndBuildList();
        status('Deleted');
      })
      .catch((e) => status('Delete: ' + (e as Error).message, true));
  });

  document.body.appendChild(menu);
  clampContextMenuToViewport(menu);
  setTimeout(() => document.addEventListener('click', hideCtxMenu), 0);
}

export function initListFilters(): void {
  const toolbar = document.querySelector('.index-panel .index-toolbar');
  if (toolbar) {
    let newShaderBtn = toolbar.querySelector('.new-shader-btn') as HTMLButtonElement | null;
    if (!newShaderBtn) {
      newShaderBtn = document.createElement('button');
      newShaderBtn.type = 'button';
      newShaderBtn.className = 'wire-btn new-shader-btn';
      newShaderBtn.textContent = 'New shader';
      newShaderBtn.title = 'TLDR: Create new ISF or GLSL (blank or paste from Shadertoy/GLSLSandbox)';
      newShaderBtn.style.marginBottom = '6px';
      newShaderBtn.onclick = () => showCreateShaderModal();
      toolbar.insertBefore(newShaderBtn, toolbar.firstChild);
    }
    let stopThumbsBtn = toolbar.querySelector('.stop-thumbs-btn') as HTMLButtonElement | null;
    if (!stopThumbsBtn) {
      stopThumbsBtn = document.createElement('button');
      stopThumbsBtn.type = 'button';
      stopThumbsBtn.className = 'wire-btn stop-thumbs-btn';
      stopThumbsBtn.textContent = 'Pause thumbnails';
      stopThumbsBtn.title = 'TLDR: Pause thumbnail loading to reduce CPU/memory use';
      stopThumbsBtn.style.cssText = 'display:none;margin-bottom:6px;';
      stopThumbsBtn.onclick = () => stopThumbnailLoad();
      toolbar.insertBefore(stopThumbsBtn, newShaderBtn.nextSibling);
    }
    let loadThumbsBtn = toolbar.querySelector('.load-thumbs-btn') as HTMLButtonElement | null;
    if (!loadThumbsBtn) {
      loadThumbsBtn = document.createElement('button');
      loadThumbsBtn.type = 'button';
      loadThumbsBtn.className = 'wire-btn load-thumbs-btn';
      loadThumbsBtn.textContent = 'Load thumbnails';
      loadThumbsBtn.title = 'TLDR: Load cached thumbnails from disk (opt-in; does not generate new ones). Use bulk script or right-click > Generate thumbnail for many.';
      loadThumbsBtn.style.cssText = 'display:none;margin-bottom:6px;';
      loadThumbsBtn.onclick = () => resumeThumbnailLoad();
      toolbar.insertBefore(loadThumbsBtn, stopThumbsBtn.nextSibling);
    }
    let generateThumbsBtn = toolbar.querySelector('.generate-thumbs-btn') as HTMLButtonElement | null;
    if (!generateThumbsBtn) {
      generateThumbsBtn = document.createElement('button');
      generateThumbsBtn.type = 'button';
      generateThumbsBtn.className = 'wire-btn generate-thumbs-btn';
      generateThumbsBtn.textContent = 'Generate thumbnails';
      generateThumbsBtn.title = 'Generate missing thumbnails in background';
      generateThumbsBtn.style.cssText = 'display:none;margin-bottom:6px;';
      generateThumbsBtn.onclick = async () => {
        const { entries: stateEntries, getThumbnail: getThumb, setThumbnail } = await import('./state.js');
        const { postThumbnailSave } = await import('./api.js');
        const allWithout = stateEntries.filter((e) => e.path && !getThumb(e.path));
        if (allWithout.length === 0) {
          status('All shaders already have thumbnails.');
          return;
        }
        const toGen = allWithout.slice(0, 50);
        status('Generating ' + toGen.length + ' thumbnails in background...');
        const queue = toGen.map((e) => ({ path: e.path!, entry: e }));
        await generateThumbnailsInBackground(
          queue,
          (done, total, name) => status('Thumbnail ' + done + '/' + total + ': ' + name),
          (path, dataUrl) => {
            setThumbnail(path, dataUrl);
            postThumbnailSave({ path, dataUrl }).catch(() => {});
            window.dispatchEvent(new CustomEvent('thumbnail-captured', { detail: { path } }));
            buildList();
          }
        );
        status('Thumbnails done.');
      };
      toolbar.insertBefore(generateThumbsBtn, loadThumbsBtn.nextSibling);
    }
    let thumbCounterEl = toolbar.querySelector('.thumb-counter') as HTMLSpanElement | null;
    if (!thumbCounterEl) {
      thumbCounterEl = document.createElement('span');
      thumbCounterEl.className = 'thumb-counter';
      thumbCounterEl.style.cssText = 'display:none;font-size:11px;color:#888;margin-bottom:6px;';
      toolbar.insertBefore(thumbCounterEl, generateThumbsBtn.nextSibling);
    }
    const updateThumbCounter = () => {
      const loading = stopThumbsBtn.classList.contains('loading') || isThumbnailsLoading();
      if (!loading) {
        thumbCounterEl.style.display = 'none';
        return;
      }
      const { loaded, total } = getThumbnailProgress();
      thumbCounterEl.textContent = loaded + ' / ' + total;
      thumbCounterEl.style.display = total > 0 ? 'block' : 'none';
    };
    const updateThumbBtns = () => {
      const loading = stopThumbsBtn.classList.contains('loading') || isThumbnailsLoading();
      const missing = hasMissingThumbnails();
      const showThumbs = !!appSettings.showThumbnails;
      stopThumbsBtn.style.display = loading ? 'block' : 'none';
      loadThumbsBtn.style.display = !loading && showThumbs && missing ? 'block' : 'none';
      generateThumbsBtn.style.display = !loading && showThumbs ? 'block' : 'none';
      updateThumbCounter();
    };
    if (!stopThumbsListenersAttached) {
      stopThumbsListenersAttached = true;
      window.addEventListener('thumbnails-loading-start', () => { stopThumbsBtn.classList.add('loading'); updateThumbBtns(); });
      window.addEventListener('thumbnails-loading-done', () => { stopThumbsBtn.classList.remove('loading'); updateThumbBtns(); });
      window.addEventListener('thumbnails-progress', updateThumbCounter);
    }
    updateThumbBtns();
  }
  const searchEl = el('search') as HTMLInputElement | null;
  const formatEl = el('listFormatFilter') as HTMLSelectElement | null;
  if (searchEl) {
    searchEl.addEventListener('input', () => {
      setListSearchQuery(searchEl.value);
      buildList();
    });
  }
  if (formatEl) {
    formatEl.value = listFormatFilter;
    formatEl.addEventListener('change', () => {
      const v = (formatEl.value || 'all') as 'all' | 'glsl' | 'isf';
      setListFormatFilter(v);
      buildList();
    });
  }
  const setFilterEl = document.getElementById('listSetFilter') as HTMLSelectElement | null;
  if (setFilterEl && !setFilterListenerAttached) {
    setFilterListenerAttached = true;
    setFilterEl.addEventListener('change', () => {
      const v = setFilterEl.value || null;
      setListSetFilter(v);
      buildList();
    });
  }
  const setManageBtn = document.getElementById('listSetManageBtn') as HTMLButtonElement | null;
  if (setManageBtn && !(setManageBtn as unknown as { _managed?: boolean })._managed) {
    (setManageBtn as unknown as { _managed?: boolean })._managed = true;
    setManageBtn.addEventListener('click', () => showManageSetsModal());
  }
  const deadToggle = document.getElementById('showDeadToggle') as HTMLInputElement | null;
  const deadToggleLabel = document.getElementById('deadToggleLabel') as HTMLElement | null;
  const deadCountBadge = document.getElementById('deadCountBadge') as HTMLElement | null;
  if (deadToggle && deadToggleLabel) {
    deadToggle.checked = showDeadShaders;
    const deadCount = getDeadCount();
    if (deadCount > 0) {
      deadToggleLabel.style.display = '';
      if (deadCountBadge) deadCountBadge.textContent = '(' + deadCount + ')';
    } else {
      deadToggleLabel.style.display = 'none';
    }
    if (!(deadToggle as unknown as { _deadInit?: boolean })._deadInit) {
      (deadToggle as unknown as { _deadInit?: boolean })._deadInit = true;
      deadToggle.addEventListener('change', () => {
        setShowDeadShaders(deadToggle.checked);
        buildList();
      });
    }
  }
  const trashToggle = document.getElementById('showTrashToggle') as HTMLInputElement | null;
  const trashToggleLabel = document.getElementById('trashToggleLabel') as HTMLElement | null;
  const trashCountBadge = document.getElementById('trashCountBadge') as HTMLElement | null;
  if (trashToggle && trashToggleLabel) {
    trashToggle.checked = showTrashShaders;
    const trashCount = getTrashCount();
    if (trashCount > 0) {
      trashToggleLabel.style.display = '';
      if (trashCountBadge) trashCountBadge.textContent = '(' + trashCount + ')';
    } else {
      trashToggleLabel.style.display = 'none';
    }
    if (!(trashToggle as unknown as { _trashInit?: boolean })._trashInit) {
      (trashToggle as unknown as { _trashInit?: boolean })._trashInit = true;
      trashToggle.addEventListener('change', () => {
        setShowTrashShaders(trashToggle.checked);
        buildList();
      });
    }
  }
  document.querySelectorAll('.list-view-btn').forEach((btn) => {
    btn.addEventListener('click', () => {
      const v = (btn as HTMLElement).dataset.view || 'list';
      setAppSettings({ listViewMode: v });
      document.querySelectorAll('.list-view-btn').forEach((b) => b.classList.remove('active'));
      btn.classList.add('active');
      buildList();
    });
  });
  const mode = appSettings.listViewMode || 'list';
  document.querySelectorAll('.list-view-btn').forEach((b) => {
    (b as HTMLElement).classList.toggle('active', (b as HTMLElement).dataset.view === mode);
  });
  window.addEventListener('thumbnail-captured', () => { buildList(); updateThumbBtns(); });
}

function escapeHtml(s: string): string {
  const div = document.createElement('div');
  div.textContent = s;
  return div.innerHTML;
}

function selectShader(entry: IndexEntry, itemEl: HTMLElement): void {
  setCurrentEntry(entry);
  document.querySelectorAll('.list-item').forEach((elem) => elem.classList.remove('active'));
  if (itemEl) itemEl.classList.add('active');
  clearSessionForNewShader();
  loadShader(entry);
  // Fetch and display git revision info
  if (entry.path) {
    import('./api.js').then(({ fetchGitInfo }) => {
      fetchGitInfo(entry.path!).then((info) => {
        const revEl = document.getElementById('shaderRevInfo');
        if (!revEl) return;
        if (info.tracked && info.revisions > 0) {
          const age = info.firstDate ? info.firstDate.slice(0, 10) : '?';
          const last = info.lastDate ? info.lastDate.slice(0, 10) : '?';
          revEl.textContent = info.revisions + ' rev' + (info.revisions !== 1 ? 's' : '') + ' | ' + age + ' - ' + last;
          revEl.style.display = '';
          revEl.title = 'First: ' + info.firstDate + ' (' + (info.firstSubject || '') + ')\nLast: ' + info.lastDate + ' (' + (info.lastSubject || '') + ')';
        } else {
          revEl.textContent = 'no history';
          revEl.style.display = '';
          revEl.title = 'No git commits for this shader yet';
        }
      }).catch(() => {
        const revEl = document.getElementById('shaderRevInfo');
        if (revEl) revEl.style.display = 'none';
      });
    });
  }
}

function toggleFavorite(entry: IndexEntry): void {
  const fav = !entry.favorite;
  postUpdate({ id: entry.id, favorite: fav })
    .then(() => {
      entry.favorite = fav;
      if (currentEntry && currentEntry.id === entry.id) currentEntry.favorite = fav;
      buildList();
    })
    .catch((e) => status('favorite: ' + (e as Error).message, true));
}

function cycleColor(entry: IndexEntry): void {
  const current = entry.color || '';
  const idx = COLOR_PRESETS.indexOf(current);
  const next = idx >= 0 && idx < COLOR_PRESETS.length - 1 ? COLOR_PRESETS[idx + 1] : COLOR_PRESETS[0];
  postUpdate({ id: entry.id, color: next })
    .then(() => {
      entry.color = next;
      if (currentEntry && currentEntry.id === entry.id) currentEntry.color = next;
      buildList();
    })
    .catch((e) => status('color: ' + (e as Error).message, true));
}

function showEmojiPicker(ev: MouseEvent, entry: IndexEntry): void {
  hideCtxMenu();
  const menu = document.createElement('div');
  menu.className = 'ctx-menu emoji-picker';
  menu.style.cssText = 'position:fixed;left:' + ev.clientX + 'px;top:' + ev.clientY +
    'px;background:var(--amiga-panel);border:2px solid var(--bevel-dark);padding:8px;z-index:10001;' +
    'box-shadow:4px 4px 12px rgba(0,0,0,0.5);display:grid;grid-template-columns:repeat(8,1fr);gap:4px;max-width:280px;';
  ctxMenuEl = menu;

  // Clear button
  const clearBtn = document.createElement('div');
  clearBtn.style.cssText = 'grid-column:1/-1;padding:4px;text-align:center;font-size:10px;color:var(--crt-dim);cursor:pointer;border-bottom:1px solid var(--bevel-dark);margin-bottom:4px;';
  clearBtn.textContent = 'Clear icon';
  clearBtn.onmouseenter = () => { clearBtn.style.color = 'var(--amiga-accent)'; };
  clearBtn.onmouseleave = () => { clearBtn.style.color = 'var(--crt-dim)'; };
  clearBtn.onclick = (e) => {
    e.stopPropagation();
    hideCtxMenu();
    const notes = '';
    postUpdate({ id: entry.id, notes })
      .then(() => { entry.notes = notes; if (currentEntry && currentEntry.id === entry.id) currentEntry.notes = notes; buildList(); status('Icon cleared'); })
      .catch((e2) => status('Icon: ' + (e2 as Error).message, true));
  };
  menu.appendChild(clearBtn);

  EMOJI_PRESETS.forEach((em) => {
    const btn = document.createElement('div');
    btn.style.cssText = 'width:28px;height:28px;display:flex;align-items:center;justify-content:center;cursor:pointer;font-size:18px;border-radius:4px;';
    btn.textContent = em;
    btn.onmouseenter = () => { btn.style.background = 'var(--amiga-surface)'; };
    btn.onmouseleave = () => { btn.style.background = ''; };
    btn.onclick = (e) => {
      e.stopPropagation();
      hideCtxMenu();
      const notes = em;
      postUpdate({ id: entry.id, notes })
        .then(() => { entry.notes = notes; if (currentEntry && currentEntry.id === entry.id) currentEntry.notes = notes; buildList(); status('Icon set: ' + em); })
        .catch((e2) => status('Icon: ' + (e2 as Error).message, true));
    };
    menu.appendChild(btn);
  });

  document.body.appendChild(menu);
  clampContextMenuToViewport(menu);
  setTimeout(() => document.addEventListener('click', hideCtxMenu), 0);
}

function clearColor(entry: IndexEntry): void {
  const noColor = '';
  postUpdate({ id: entry.id, color: noColor })
    .then(() => {
      entry.color = '';
      if (currentEntry && currentEntry.id === entry.id) currentEntry.color = '';
      buildList();
      status('Color cleared');
    })
    .catch((e) => status('color: ' + (e as Error).message, true));
}
