import type { IndexEntry, Settings } from './types.js';

export let entries: IndexEntry[] = [];
export let currentPath: string | null = null;
export let currentEntry: IndexEntry | null = null;
export let currentSource = '';
export let lastCompileError = '';
export let lastCompileErrorPath = '';
const _pendingCursorConfirm = { value: false };
let _pendingAgentReload = false;

export function getPendingCursorConfirm(): boolean {
  return _pendingCursorConfirm.value;
}

export function setPendingCursorConfirm(v: boolean): void {
  _pendingCursorConfirm.value = v;
}

export function getPendingAgentReload(): boolean {
  return _pendingAgentReload;
}

export function setPendingAgentReload(v: boolean): void {
  _pendingAgentReload = v;
}

export function setLastCompileError(err: string, path: string): void {
  lastCompileError = err;
  lastCompileErrorPath = path;
}

export function clearLastCompileError(): void {
  lastCompileError = '';
  lastCompileErrorPath = '';
}

/** Browser-only overrides (compile overlay quick entry). Server Settings used as fallback. */
const LS_CURSOR_API_KEY = 'macroverse-cursor-api-key-local';
const LS_GITHUB_TOKEN = 'macroverse-github-token-local';

export function getCursorApiKey(): string {
  try {
    const local = localStorage.getItem(LS_CURSOR_API_KEY)?.trim();
    if (local) return local;
  } catch (_) { /* ignore */ }
  const k = (appSettings as Record<string, unknown>).cursorApiKey as string | undefined;
  return (k && k !== '***') ? k : '';
}

export function getGithubToken(): string {
  try {
    const local = localStorage.getItem(LS_GITHUB_TOKEN)?.trim();
    if (local) return local;
  } catch (_) { /* ignore */ }
  const k = (appSettings as Record<string, unknown>).githubToken as string | undefined;
  return (k && k !== '***') ? k : '';
}

export function setLocalCursorApiKey(value: string): void {
  try {
    const t = value.trim();
    if (t) localStorage.setItem(LS_CURSOR_API_KEY, t);
    else localStorage.removeItem(LS_CURSOR_API_KEY);
  } catch (_) { /* ignore */ }
}

export function setLocalGithubToken(value: string): void {
  try {
    const t = value.trim();
    if (t) localStorage.setItem(LS_GITHUB_TOKEN, t);
    else localStorage.removeItem(LS_GITHUB_TOKEN);
  } catch (_) { /* ignore */ }
}

export function getLocalCursorKeyStored(): string {
  try {
    return localStorage.getItem(LS_CURSOR_API_KEY)?.trim() || '';
  } catch (_) {
    return '';
  }
}

export function getLocalGithubTokenStored(): string {
  try {
    return localStorage.getItem(LS_GITHUB_TOKEN)?.trim() || '';
  } catch (_) {
    return '';
  }
}

export const defaultAppSettings: Settings = {
  previewWidth: 854,
  previewHeight: 480,
  previewResolution: 'auto',
  previewQuality: 1,
  thumbnailQuality: 0.5,
  thumbnailMaxSize: 120,
  targetFps: 30,
  enablePipeline: true,
  enableOutput: true,
  enableGit: true,
  showThumbnails: true,
  listViewMode: 'list',
  sourcePaths: [],
  indexPath: '',
  enableSpout: false,
  enableNdi: false
};

export let appSettings: Settings = { ...defaultAppSettings };

export type ListFormatFilter = 'all' | 'glsl' | 'isf';
export let listFormatFilter: ListFormatFilter = 'all';
export let listSearchQuery = '';
export let listSetFilter: string | null = null;
export let showDeadShaders: boolean = localStorage.getItem('macroverse-show-dead') === 'true';
export let showTrashShaders: boolean = localStorage.getItem('macroverse-show-trash') === 'true';

export function setListFormatFilter(v: ListFormatFilter): void {
  listFormatFilter = v;
}
export function setListSearchQuery(q: string): void {
  listSearchQuery = (q || '').trim().toLowerCase();
}
export function setListSetFilter(v: string | null): void {
  listSetFilter = v && v.trim() ? v.trim() : null;
}
export function setShowDeadShaders(v: boolean): void {
  showDeadShaders = v;
  localStorage.setItem('macroverse-show-dead', String(v));
}
export function setShowTrashShaders(v: boolean): void {
  showTrashShaders = v;
  localStorage.setItem('macroverse-show-trash', String(v));
}

const thumbnailCache: Map<string, string> = new Map();

function normalizeThumbnailKey(path: string): string {
  return (path || '').replace(/\\/g, '|');
}

export function getThumbnail(path: string): string | undefined {
  return thumbnailCache.get(normalizeThumbnailKey(path));
}
export function setThumbnail(path: string, dataUrl: string): void {
  thumbnailCache.set(normalizeThumbnailKey(path), dataUrl);
}

const lastSavedByPath: Map<string, number> = new Map();
export function getLastSaved(path: string): number | undefined {
  return lastSavedByPath.get(path);
}
export function setLastSaved(path: string): void {
  lastSavedByPath.set(path, Date.now());
}

export function setEntries(arr: IndexEntry[] | unknown): void {
  entries = Array.isArray(arr) ? arr : [];
  if (typeof window !== 'undefined') {
    window.dispatchEvent(
      new CustomEvent('macroverse-shader-index-updated', { detail: { count: entries.length } }),
    );
  }
}

export function setCurrentEntry(entry: IndexEntry | null): void {
  currentEntry = entry;
  currentPath = entry ? (entry.path ?? null) : null;
}

export function setCurrentSource(src: string): void {
  currentSource = src || '';
}

export function setAppSettings(s: Partial<Settings> | null | undefined): void {
  if (s && typeof s === 'object') {
    appSettings = { ...appSettings, ...s };
  }
}

const GALLERY_STORAGE = 'macroverse-gallery';
export type GalleryAutoAdvanceMode = 'shader' | 'page';

export let galleryPage = 0;
export let galleryPerPage = 12;
export let galleryGridCols = 4;
export let galleryGridRows = 3;
export let galleryResolution = 320;
export let galleryQuality = 0.75;
export let galleryAutoAdvanceEnabled = false;
export let galleryAutoAdvanceIntervalSec = 8;
export let galleryAutoAdvanceMode: GalleryAutoAdvanceMode = 'shader';
export let galleryFocusedIndex = 0;
export let galleryPresetTags: string[] = ['favorite', 'keep', 'vj', 'test', 'dead', 'wip', 'done', 'export', 'backup'];
export const GALLERY_PRESET_SETS: string[] = [
  'vj-ambient',    // 1: slow, atmospheric, generative
  'vj-techno',     // 2: fast, harsh, rhythmic
  'vj-cosmic',     // 3: space, void, nebula, stars
  'vj-glitch',     // 4: digital, corrupted, databend
  'vj-geometric',  // 5: shapes, patterns, kaleidoscope
  'vj-organic',    // 6: fluid, liquid, biological
  'vj-wire-ready', // 7: ISF format, Resolume Wire ready
  'vj-dark',       // 8: dark/moody, low-energy sets
  'vj-colour',     // 9: colour-forward, palette-heavy
];

function loadGalleryState(): void {
  try {
    const raw = localStorage.getItem(GALLERY_STORAGE);
    if (!raw) return;
    const o = JSON.parse(raw) as Record<string, unknown>;
    if (typeof o.galleryPage === 'number') galleryPage = Math.max(0, o.galleryPage);
    if (typeof o.galleryPerPage === 'number') galleryPerPage = Math.max(1, Math.min(48, o.galleryPerPage));
    if (typeof o.galleryGridCols === 'number') galleryGridCols = Math.max(1, Math.min(6, o.galleryGridCols));
    if (typeof o.galleryGridRows === 'number') galleryGridRows = Math.max(1, Math.min(6, o.galleryGridRows));
    if (typeof o.galleryResolution === 'number') galleryResolution = Math.max(128, Math.min(640, o.galleryResolution));
    if (typeof o.galleryQuality === 'number') galleryQuality = Math.max(0.25, Math.min(1, o.galleryQuality));
    if (typeof o.galleryAutoAdvanceIntervalSec === 'number') galleryAutoAdvanceIntervalSec = Math.max(1, Math.min(120, o.galleryAutoAdvanceIntervalSec));
    if (o.galleryAutoAdvanceMode === 'shader' || o.galleryAutoAdvanceMode === 'page') galleryAutoAdvanceMode = o.galleryAutoAdvanceMode;
    if (Array.isArray(o.galleryPresetTags)) galleryPresetTags = o.galleryPresetTags.slice(0, 9).map(String);
  } catch {
    // ignore
  }
}

function saveGalleryState(): void {
  try {
    localStorage.setItem(GALLERY_STORAGE, JSON.stringify({
      galleryPage,
      galleryPerPage,
      galleryGridCols,
      galleryGridRows,
      galleryResolution,
      galleryQuality,
      galleryAutoAdvanceIntervalSec,
      galleryAutoAdvanceMode,
      galleryPresetTags
    }));
  } catch {
    // ignore
  }
}

export function setGalleryPage(v: number): void {
  galleryPage = Math.max(0, v);
  saveGalleryState();
}

export function setGalleryPerPage(v: number): void {
  galleryPerPage = Math.max(1, Math.min(24, v));
  // Recompute grid dims to match
  const LAYOUTS: Record<number, [number, number]> = { 1:[1,1], 4:[2,2], 8:[4,2], 14:[7,2], 24:[6,4] };
  const layout = LAYOUTS[v];
  if (layout) { galleryGridCols = layout[0]; galleryGridRows = layout[1]; }
  saveGalleryState();
}

export function setGalleryGridCols(v: number): void {
  galleryGridCols = Math.max(1, Math.min(6, v));
  saveGalleryState();
}

export function setGalleryGridRows(v: number): void {
  galleryGridRows = Math.max(1, Math.min(6, v));
  saveGalleryState();
}

export function setGalleryResolution(v: number): void {
  galleryResolution = Math.max(128, Math.min(640, v));
  saveGalleryState();
}

export function setGalleryQuality(v: number): void {
  galleryQuality = Math.max(0.25, Math.min(1, v));
  saveGalleryState();
}

export function setGalleryAutoAdvanceEnabled(v: boolean): void {
  galleryAutoAdvanceEnabled = v;
}

export function setGalleryAutoAdvanceIntervalSec(v: number): void {
  galleryAutoAdvanceIntervalSec = Math.max(1, Math.min(120, v));
  saveGalleryState();
}

export function setGalleryAutoAdvanceMode(v: GalleryAutoAdvanceMode): void {
  galleryAutoAdvanceMode = v;
  saveGalleryState();
}

export function setGalleryFocusedIndex(v: number): void {
  galleryFocusedIndex = Math.max(0, v);
}

export function setGalleryPresetTags(tags: string[]): void {
  galleryPresetTags = tags.slice(0, 9).map(String);
  saveGalleryState();
}

export function initGalleryStateFromStorage(): void {
  loadGalleryState();
}

/* ── Wire Pipeline Hub state ──────────────────────────────────────────── */
export const WIRE_PRESET_SETS: string[] = [
  'wire-effects',     // sampler2D post-processing shaders
  'wire-textures',    // good source textures for effects
  'wire-mixing-kit',  // curated crossfader-friendly mix
];

export type WireShaderTab = 'sources' | 'effects';
export let wireShaderTab: WireShaderTab = 'sources';
export let wireSelectedShaderIds: number[] = [];
export let wireTopology = 'feedback';
export let wireMidiPreset = 'apc40';
export let wireLibrarySearch = '';

export function setWireShaderTab(v: WireShaderTab): void { wireShaderTab = v; }
export function setWireSelectedShaderIds(ids: number[]): void { wireSelectedShaderIds = ids; }
export function toggleWireSelectedShader(id: number): void {
  const idx = wireSelectedShaderIds.indexOf(id);
  if (idx >= 0) wireSelectedShaderIds.splice(idx, 1);
  else wireSelectedShaderIds.push(id);
}
export function setWireTopology(v: string): void { wireTopology = v; }
export function setWireMidiPreset(v: string): void { wireMidiPreset = v; }
export function setWireLibrarySearch(v: string): void { wireLibrarySearch = (v || '').trim().toLowerCase(); }
