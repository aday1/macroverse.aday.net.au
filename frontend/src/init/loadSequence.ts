import { fetchSources, fetchIndex, fetchThumbnailsBatch } from '../api.js';
import { status, hideSplash, showPathsInfo } from '../dom.js';
import { setEntries, setAppSettings, setCurrentEntry, setCurrentSource, setThumbnail, appSettings, entries, getThumbnail } from '../state.js';
import { buildList, initListFilters } from '../list.js';
import { loadShader } from '../render.js';
import type { IndexEntry } from '../types.js';

export interface LoadSequenceOpts {
  signal?: AbortSignal;
}

const BATCH_SIZE = 12;
const BATCH_DELAY_MS = 600;
const REBUILD_DEBOUNCE_MS = 400;

let thumbnailsAbortController: AbortController | null = null;
let thumbLoadMoreTimer: ReturnType<typeof setTimeout> | null = null;

export function isThumbnailsLoading(): boolean {
  return !!thumbnailsAbortController;
}

export function stopThumbnailLoad(): void {
  if (thumbLoadMoreTimer) {
    clearTimeout(thumbLoadMoreTimer);
    thumbLoadMoreTimer = null;
  }
  if (thumbnailsAbortController) {
    thumbnailsAbortController.abort();
    thumbnailsAbortController = null;
    status('Thumbnail load paused');
    window.dispatchEvent(new CustomEvent('thumbnails-loading-done'));
  }
}

export function hasMissingThumbnails(): boolean {
  if (!appSettings.showThumbnails) return false;
  const arr = entries;
  if (arr.length === 0) return false;
  return arr.some((e) => e.path && !getThumbnail(e.path));
}

export function getThumbnailProgress(): { loaded: number; total: number } {
  const arr = entries.filter((e) => e.path);
  const total = arr.length;
  const loaded = arr.filter((e) => getThumbnail(e.path!)).length;
  return { loaded, total };
}

export function resumeThumbnailLoad(): void {
  if (thumbnailsAbortController) return;
  if (!appSettings.showThumbnails) return;
  const arr = entries;
  const without = arr.filter((e) => e.path && !getThumbnail(e.path)).map((e) => e.path!);
  if (without.length === 0) {
    status('All thumbnails loaded');
    return;
  }
  thumbnailsAbortController = new AbortController();
  const thumbSignal = thumbnailsAbortController.signal;
  window.dispatchEvent(new CustomEvent('thumbnails-loading-start'));
  let loadInProgress = false;
  let thumbRebuildTimer: ReturnType<typeof setTimeout> | null = null;
  const scheduleThumbRebuild = () => {
    if (thumbRebuildTimer) return;
    thumbRebuildTimer = setTimeout(() => {
      thumbRebuildTimer = null;
      requestAnimationFrame(() => buildList());
    }, REBUILD_DEBOUNCE_MS);
  };
  const doneThumbLoad = () => {
    thumbnailsAbortController = null;
    window.dispatchEvent(new CustomEvent('thumbnails-loading-done'));
    if (thumbRebuildTimer) {
      clearTimeout(thumbRebuildTimer);
      thumbRebuildTimer = null;
      buildList();
    }
  };
  let offset = 0;
  const loadMore = () => {
    if (thumbSignal?.aborted) { doneThumbLoad(); return; }
    const slice = without.slice(offset, offset + BATCH_SIZE);
    if (slice.length === 0) { doneThumbLoad(); return; }
    offset += slice.length;
    loadInProgress = true;
    fetchThumbnailsBatch(slice, { signal: thumbSignal })
      .then((thumbs) => {
        if (thumbSignal?.aborted) { loadInProgress = false; return; }
        for (const [path, dataUrl] of Object.entries(thumbs)) {
          if (path && dataUrl) setThumbnail(path, dataUrl);
        }
        scheduleThumbRebuild();
        window.dispatchEvent(new CustomEvent('thumbnails-progress'));
      })
      .catch(() => {})
      .finally(() => {
        loadInProgress = false;
        if (thumbSignal?.aborted) return;
        if (offset < without.length) {
          thumbLoadMoreTimer = setTimeout(loadMore, BATCH_DELAY_MS);
        } else {
          doneThumbLoad();
        }
      });
  };
  loadMore();
}


export async function loadSequence(opts?: LoadSequenceOpts): Promise<void> {
  const signal = opts?.signal;
  status('Step 1/4: Connecting to server, fetching /api/sources...');

  const src = await fetchSources({ signal }).catch((e) => {
    status('Server not reachable - is Macroverse running? Open http://localhost:8765', true);
    throw e;
  });

  const indexPath = (src && src.indexPath) ? src.indexPath : 'shader-index.json';
  status('Step 2/4: Got paths, index at ' + String(indexPath).replace(/^.*[\\\/]/, '') + '...');

  if (src && src.paths) {
    setAppSettings({ sourcePaths: src.paths, indexPath: src.indexPath || indexPath });
  }

  status('Step 3/4: Fetching shader index...');

  const data = await fetchIndex({ signal }).catch((e) => {
    status('Index load failed: ' + ((e as Error).message || 'unknown'), true);
    throw e;
  });

  status('Step 4/4: Parsing index...');

  const arr: IndexEntry[] = Array.isArray(data) ? data : [];
  setEntries(arr);

  if (thumbnailsAbortController) {
    thumbnailsAbortController.abort();
    thumbnailsAbortController = null;
  }
  // Do not auto-load or auto-generate thumbnails on startup (saves CPU/memory).
  // User can click "Load thumbnails" in sidebar to fetch cached thumbs, or use the bulk script.
  // Thumbnails are generated on-demand when opening a shader (if missing) or on save.

  if (arr.length === 0) {
    status('No shaders in index. Use Scan in the index panel to scan.');
  } else {
    status('Rendering ' + arr.length + ' shaders...');
  }

  buildList();
  initListFilters();
  updateIndexIndicator(arr, src);

  hideSplash();

  if (arr.length > 0) {
    status(arr.length + ' shaders loaded');
    const first = arr.find((x) => /sorted_txt/i.test(x.path || '')) || arr[0];
    if (first && first.path) {
      const rest = arr.filter((e) => e !== first && e.path);
      const candidates = [first, ...rest].slice(0, 15);
      let loaded = false;
      for (const entry of candidates) {
        setCurrentEntry(entry);
        try {
          await loadShader(entry);
          loaded = true;
          break;
        } catch (_) {
          /* compile or load failed, try next */
        }
      }
      if (!loaded) {
        setCurrentEntry(first);
        loadShader(first);
      }
    }
  } else {
    setCurrentSource('');
    const sync = (globalThis as unknown as { syncCodeFromState?: () => void }).syncCodeFromState;
    if (typeof sync === 'function') sync();
  }
}

function updateIndexIndicator(arr: IndexEntry[], src: { paths?: string[]; indexPath?: string } | null): void {
  const headerEl = document.getElementById('indexHeaderText');
  if (!headerEl) return;
  const count = arr.length;
  const glslCount = arr.filter((e) => (e.format || '').toLowerCase() !== 'isf').length;
  const isfCount = arr.filter((e) => (e.format || '').toLowerCase() === 'isf').length;
  const paths = (src && src.paths) || [];
  const indexFile = ((src && src.indexPath) || 'shader-index.json').replace(/^.*[/\\]/, '');
  let folderHint = '';
  if (paths.length === 1) {
    folderHint = paths[0].replace(/^.*[/\\]/, '');
  } else if (paths.length > 1) {
    folderHint = paths.length + ' folders';
  }
  headerEl.textContent = 'Index (' + count + ')';
  headerEl.setAttribute('title', 'Shaders: ' + count + ' total (' + glslCount + ' GLSL, ' + isfCount + ' ISF)\n' +
    'Index: ' + indexFile + '\n' +
    'Source: ' + (folderHint || 'none'));

  let infoEl = document.getElementById('indexInfoLine');
  if (!infoEl) {
    infoEl = document.createElement('div');
    infoEl.id = 'indexInfoLine';
    infoEl.style.cssText = 'font-size:9px;color:var(--crt-dim);padding:2px 12px;background:var(--amiga-surface);border-bottom:1px solid var(--bevel-dark);white-space:nowrap;overflow:hidden;text-overflow:ellipsis;';
    const toolbar = document.querySelector('.index-panel .index-toolbar');
    if (toolbar && toolbar.parentElement) {
      toolbar.parentElement.insertBefore(infoEl, toolbar);
    }
  }
  infoEl.textContent = count + ' shaders | ' + indexFile + (folderHint ? ' | ' + folderHint : '');
  infoEl.title = 'Source paths: ' + (paths.length > 0 ? paths.join(', ') : '(none)') + '\nIndex file: ' + ((src && src.indexPath) || 'shader-index.json');
}
