import { fetchSources, fetchIndex, fetchThumbnailsBatch } from '../api.js';
import { status, showPathsInfo } from '../dom.js';
import { requestSplashDismiss } from './splashVjQr.js';
import * as stateModule from '../state.js';
import { setEntries, setAppSettings, setCurrentEntry, setCurrentSource, setThumbnail, appSettings, entries, getThumbnail, clearLastCompileError } from '../state.js';
import { buildList, initListFilters } from '../list.js';
import { loadShader, setPreviewCompileErrorOverlayVisible } from '../render.js';
import {
  cancelIdleThumbnailGeneration,
  enqueueIdleThumbnailGeneration,
} from './idleThumbnailGen.js';
import type { IndexEntry } from '../types.js';

export const CAROUSEL_THUMB_PRIORITY = 150;

const LAST_SHADER_KEY = 'macroverse-last-shader-path';

/** Remember the last successfully-loaded shader so the user lands on
 *  the same one next visit. */
function rememberLastShader(path: string | undefined | null): void {
  try { if (path) localStorage.setItem(LAST_SHADER_KEY, path); } catch (_) {}
}
function recallLastShader(): string | null {
  try { return localStorage.getItem(LAST_SHADER_KEY); } catch (_) { return null; }
}

// Listen for shader changes triggered by the user (clicking a list
// item, drag-drop, etc.). When the shader compiles cleanly, remember
// it. Listener is attached once at module load.
if (typeof window !== 'undefined') {
  window.addEventListener('macroverse:shader-changed', (ev: Event) => {
    const detail = (ev as CustomEvent<{ path?: string }>).detail;
    // Defer: the compile error state is set synchronously inside
    // render() which runs immediately before the event dispatch, so
    // a microtask is enough to read the latest value.
    queueMicrotask(() => {
      if (stateModule.lastCompileError) return;
      rememberLastShader(detail?.path);
    });
  });
}

/** Score a shader for "is this likely to compile in WebGL 1.0?". Higher
 *  is better. We want the FIRST landing to be a simple, reliable shader
 *  (gradient, plasma, solid red, noise) rather than a debug/test
 *  shader that happens to come first alphabetically and may use
 *  GLSL ES 3.00 features. */
function landingScore(e: IndexEntry): number {
  const name = ((e.fixedName ?? e.name ?? e.path ?? '')).toLowerCase();
  let s = 0;
  // Strong positive: known-simple visual shaders
  if (/\b(gradient|plasma|solid|simple|hello|basic|minimal)\b/.test(name)) s += 100;
  // Mild positive: classic generative shaders
  if (/\b(noise|spiral|rainbow|tunnel)\b/.test(name)) s += 30;
  // Strong negative: debug/test/sampler shaders are usually written
  // for WebGL 2 / GLSL ES 3.00 with bitwise/modulo on ints, integer
  // texture lookups, etc.
  if (/\b(debug|test|sampler|checker|scratch|broken|dead|wip)\b/.test(name)) s -= 100;
  // Negative for items currently flagged dead in the index
  if ((e.tags || []).some((t) => /^dead$/i.test(t))) s -= 1000;
  return s;
}

export interface LoadSequenceOpts {
  signal?: AbortSignal;
}

const BATCH_SIZE = 12;
const BATCH_DELAY_MS = 600;
const REBUILD_DEBOUNCE_MS = 400;

let thumbnailsAbortController: AbortController | null = null;
let thumbLoadMoreTimer: ReturnType<typeof setTimeout> | null = null;
let thumbPipelineStarted = false;

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
  cancelIdleThumbnailGeneration();
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

export function resumeThumbnailLoad(opts?: { force?: boolean }): void {
  if (thumbnailsAbortController) return;
  if (!opts?.force && !appSettings.showThumbnails) return;
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

function pathsMissingThumbnails(limit?: number): string[] {
  const missing = entries.filter((e) => e.path && !getThumbnail(e.path!)).map((e) => e.path!);
  if (limit != null && limit >= 0) return missing.slice(0, limit);
  return missing;
}

/** Manual / bulk generate — still runs through the idle queue (one thumb per spare cycle). */
export function enqueueAllMissingThumbnails(front = false): void {
  enqueueIdleThumbnailGeneration(pathsMissingThumbnails(), { front });
}

/** Load cached thumbs from server/IDB. Visible carousel previews are generated on idle via vjDeck. */
export function startThumbnailPipeline(): void {
  if (thumbPipelineStarted) return;
  thumbPipelineStarted = true;

  if (appSettings.thumbnailLoadingPaused) return;

  const missing = pathsMissingThumbnails();
  if (missing.length === 0) return;

  if (isThumbnailsLoading()) return;

  resumeThumbnailLoad({ force: true });
}

export function scheduleThumbnailPipeline(delayMs = 1200): void {
  window.setTimeout(() => startThumbnailPipeline(), delayMs);
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
  thumbPipelineStarted = false;

  if (arr.length === 0) {
    status('No shaders in index. Use Scan in the index panel to scan.');
  } else {
    status('Rendering ' + arr.length + ' shaders...');
  }

  buildList();
  initListFilters();
  updateIndexIndicator(arr, src);

  requestSplashDismiss();

  if (arr.length > 0) {
    status(arr.length + ' shaders loaded');

    // Build the candidate list:
    // 1. The user's last successfully-loaded shader, if it still exists.
    // 2. Then everything else, sorted by landingScore (simple visual
    //    shaders ahead of debug / test / sampler shaders that often
    //    require GLSL ES 3.00 and break on plain WebGL 1.0).
    const remembered = recallLastShader();
    const rememberedEntry = remembered
      ? arr.find((e) => e.path === remembered)
      : null;
    const others = arr
      .filter((e) => e.path && e !== rememberedEntry)
      .slice()
      .sort((a, b) => landingScore(b) - landingScore(a));
    const candidates = (rememberedEntry ? [rememberedEntry, ...others] : others).slice(0, 25);

    let loaded = false;
    for (const entry of candidates) {
      setCurrentEntry(entry);
      try {
        clearLastCompileError();
        await loadShader(entry);
        // render() catches its own compile errors and shows the
        // overlay without throwing. Inspect lastCompileError to
        // detect silent failures and try the next candidate.
        if (stateModule.lastCompileError) continue;
        loaded = true;
        rememberLastShader(entry.path);
        break;
      } catch (_) {
        /* fetch / abort failure - try next */
      }
    }

    if (!loaded) {
      // Every candidate either failed to fetch or failed to compile.
      // Don't leave the user staring at a broken shader: clear the
      // overlay, show a friendly message, and let them pick from the
      // library.
      clearLastCompileError();
      setPreviewCompileErrorOverlayVisible(false);
      const fixBtn = document.getElementById('fixBtn');
      if (fixBtn) (fixBtn as HTMLElement).style.display = 'none';
      setCurrentEntry(null);
      setCurrentSource('');
      const sync = (globalThis as unknown as { syncCodeFromState?: () => void }).syncCodeFromState;
      if (typeof sync === 'function') sync();
      status('No shader auto-loaded. Pick one from the library to start.');
    }
    scheduleThumbnailPipeline();
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
