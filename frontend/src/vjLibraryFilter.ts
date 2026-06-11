import type { IndexEntry } from './types.js';
import {
  galleryPresetTags,
  GALLERY_PRESET_SETS,
  listFormatFilter,
  listSetFilter,
} from './state.js';

export type VjFormatFilter = 'glsl' | 'isf';
export type VjTagSetMode = 'any' | 'all';

export interface VjLibraryFilter {
  text: string;
  formats: VjFormatFilter[];
  tagsInclude: string[];
  tagsExclude: string[];
  tagMode: VjTagSetMode;
  categories: string[];
  setsInclude: string[];
  setsExclude: string[];
  setMode: VjTagSetMode;
  favoritesOnly: boolean;
  hideDead: boolean;
  hideTrash: boolean;
  syncWithMainList: boolean;
}

const STORAGE_KEY = 'macroverse-vj-library-filter-v1';

const DEFAULT_FILTER: VjLibraryFilter = {
  text: '',
  formats: [],
  tagsInclude: [],
  tagsExclude: [],
  tagMode: 'any',
  categories: [],
  setsInclude: [],
  setsExclude: [],
  setMode: 'any',
  favoritesOnly: false,
  hideDead: true,
  hideTrash: true,
  syncWithMainList: false,
};

let filterState: VjLibraryFilter = { ...DEFAULT_FILTER, formats: [], tagsInclude: [], tagsExclude: [], categories: [], setsInclude: [], setsExclude: [] };

function cloneFilter(f: VjLibraryFilter): VjLibraryFilter {
  return {
    ...f,
    formats: f.formats.slice(),
    tagsInclude: f.tagsInclude.slice(),
    tagsExclude: f.tagsExclude.slice(),
    categories: f.categories.slice(),
    setsInclude: f.setsInclude.slice(),
    setsExclude: f.setsExclude.slice(),
  };
}

function normalizeTagSetMode(v: unknown): VjTagSetMode {
  return v === 'all' ? 'all' : 'any';
}

function normalizeFormats(raw: unknown): VjFormatFilter[] {
  if (!Array.isArray(raw)) return [];
  const out: VjFormatFilter[] = [];
  for (const x of raw) {
    const s = String(x).toLowerCase();
    if (s === 'glsl' || s === 'isf') out.push(s);
  }
  return out;
}

function normalizeStringList(raw: unknown): string[] {
  if (!Array.isArray(raw)) return [];
  return raw.map((x) => String(x).trim()).filter(Boolean);
}

function mergeLoadedFilter(raw: Record<string, unknown>): VjLibraryFilter {
  return {
    text: typeof raw.text === 'string' ? raw.text : DEFAULT_FILTER.text,
    formats: normalizeFormats(raw.formats),
    tagsInclude: normalizeStringList(raw.tagsInclude),
    tagsExclude: normalizeStringList(raw.tagsExclude),
    tagMode: normalizeTagSetMode(raw.tagMode),
    categories: normalizeStringList(raw.categories),
    setsInclude: normalizeStringList(raw.setsInclude),
    setsExclude: normalizeStringList(raw.setsExclude),
    setMode: normalizeTagSetMode(raw.setMode),
    favoritesOnly: raw.favoritesOnly === true,
    hideDead: raw.hideDead !== false,
    hideTrash: raw.hideTrash !== false,
    syncWithMainList: raw.syncWithMainList === true,
  };
}

export function fuzzyMatch(query: string, text: string): boolean {
  const q = query.trim().toLowerCase();
  if (!q) return true;
  const t = text.toLowerCase();
  let i = 0;
  for (let j = 0; j < t.length && i < q.length; j++) {
    if (t[j] === q[i]) i++;
  }
  return i === q.length;
}

function entryTagsLower(e: IndexEntry): string[] {
  return (e.tags || []).map((t) => t.toLowerCase());
}

function hasDeadTag(e: IndexEntry): boolean {
  return (e.tags || []).some((t) => t.toLowerCase().includes('dead'));
}

function isTrashEntry(e: IndexEntry): boolean {
  return (e.category || '').toLowerCase() === 'trash';
}

function entryMatchesFormat(e: IndexEntry, want: Set<VjFormatFilter>): boolean {
  const fmt = (e.format || '').toLowerCase();
  const isIsf = fmt === 'isf';
  if (want.has('isf') && isIsf) return true;
  if (want.has('glsl') && !isIsf) return true;
  return false;
}

function tagsMatch(e: IndexEntry, include: string[], exclude: string[], mode: VjTagSetMode): boolean {
  const tags = entryTagsLower(e);
  for (const ex of exclude) {
    const x = ex.toLowerCase();
    if (tags.some((t) => t === x || t.includes(x))) return false;
  }
  if (include.length === 0) return true;
  const want = include.map((t) => t.toLowerCase());
  if (mode === 'all') {
    return want.every((w) => tags.some((t) => t === w || t.includes(w)));
  }
  return want.some((w) => tags.some((t) => t === w || t.includes(w)));
}

function setsMatch(e: IndexEntry, include: string[], exclude: string[], mode: VjTagSetMode): boolean {
  const sets = (e.sets || []).map((s) => s.toLowerCase());
  for (const ex of exclude) {
    const x = ex.toLowerCase();
    if (sets.includes(x)) return false;
  }
  if (include.length === 0) return true;
  const want = include.map((s) => s.toLowerCase());
  if (mode === 'all') {
    return want.every((w) => sets.includes(w));
  }
  return want.some((w) => sets.includes(w));
}

export function loadVjLibraryFilter(): VjLibraryFilter {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const o = JSON.parse(raw) as Record<string, unknown>;
      filterState = mergeLoadedFilter(o);
    }
  } catch {
    filterState = cloneFilter(DEFAULT_FILTER);
  }
  return getVjLibraryFilter();
}

export function saveVjLibraryFilter(f?: VjLibraryFilter): void {
  if (f) filterState = cloneFilter(f);
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(filterState));
  } catch {
    /* ignore */
  }
}

export function getVjLibraryFilter(): VjLibraryFilter {
  return cloneFilter(filterState);
}

export function setVjLibraryFilter(patch: Partial<VjLibraryFilter>): void {
  filterState = cloneFilter({ ...filterState, ...patch });
  if (patch.formats) filterState.formats = patch.formats.slice();
  if (patch.tagsInclude) filterState.tagsInclude = patch.tagsInclude.slice();
  if (patch.tagsExclude) filterState.tagsExclude = patch.tagsExclude.slice();
  if (patch.categories) filterState.categories = patch.categories.slice();
  if (patch.setsInclude) filterState.setsInclude = patch.setsInclude.slice();
  if (patch.setsExclude) filterState.setsExclude = patch.setsExclude.slice();
  saveVjLibraryFilter();
}

export function toggleVjFilterTag(tag: string, exclude = false): void {
  const t = tag.trim();
  if (!t) return;
  const f = getVjLibraryFilter();
  if (exclude) {
    const ii = f.tagsInclude.indexOf(t);
    if (ii >= 0) f.tagsInclude.splice(ii, 1);
    const ei = f.tagsExclude.indexOf(t);
    if (ei >= 0) f.tagsExclude.splice(ei, 1);
    else f.tagsExclude.push(t);
  } else {
    const ei = f.tagsExclude.indexOf(t);
    if (ei >= 0) f.tagsExclude.splice(ei, 1);
    const ii = f.tagsInclude.indexOf(t);
    if (ii >= 0) f.tagsInclude.splice(ii, 1);
    else f.tagsInclude.push(t);
  }
  setVjLibraryFilter(f);
}

export function toggleVjFilterSet(setName: string, exclude = false): void {
  const s = setName.trim();
  if (!s) return;
  const f = getVjLibraryFilter();
  if (exclude) {
    const ii = f.setsInclude.indexOf(s);
    if (ii >= 0) f.setsInclude.splice(ii, 1);
    const ei = f.setsExclude.indexOf(s);
    if (ei >= 0) f.setsExclude.splice(ei, 1);
    else f.setsExclude.push(s);
  } else {
    const ei = f.setsExclude.indexOf(s);
    if (ei >= 0) f.setsExclude.splice(ei, 1);
    const ii = f.setsInclude.indexOf(s);
    if (ii >= 0) f.setsInclude.splice(ii, 1);
    else f.setsInclude.push(s);
  }
  setVjLibraryFilter(f);
}

function toggleVjFilterFormat(fmt: VjFormatFilter): void {
  const f = getVjLibraryFilter();
  const i = f.formats.indexOf(fmt);
  if (i >= 0) f.formats.splice(i, 1);
  else f.formats.push(fmt);
  setVjLibraryFilter(f);
}

function toggleVjFilterCategory(category: string): void {
  const c = category.trim();
  if (!c) return;
  const f = getVjLibraryFilter();
  const i = f.categories.indexOf(c);
  if (i >= 0) f.categories.splice(i, 1);
  else f.categories.push(c);
  setVjLibraryFilter(f);
}

export function applyVjLibraryFilter(source: IndexEntry[]): IndexEntry[] {
  const f = filterState;
  let out = source.slice();

  if (f.hideDead) {
    out = out.filter((e) => !hasDeadTag(e));
  }
  if (f.hideTrash) {
    out = out.filter((e) => !isTrashEntry(e));
  }
  if (f.favoritesOnly) {
    out = out.filter((e) => !!e.favorite);
  }

  if (f.formats.length > 0) {
    const want = new Set(f.formats);
    out = out.filter((e) => entryMatchesFormat(e, want));
  }

  if (f.categories.length > 0) {
    const want = new Set(f.categories.map((c) => c.toLowerCase()));
    out = out.filter((e) => want.has((e.category || '').toLowerCase()));
  }

  out = out.filter((e) => tagsMatch(e, f.tagsInclude, f.tagsExclude, f.tagMode));
  out = out.filter((e) => setsMatch(e, f.setsInclude, f.setsExclude, f.setMode));

  if (f.syncWithMainList) {
    if (listFormatFilter !== 'all') {
      const wantIsf = listFormatFilter === 'isf';
      out = out.filter((e) => {
        const fmt = (e.format || '').toLowerCase();
        const isIsf = fmt === 'isf';
        return isIsf === wantIsf;
      });
    }
    if (f.setsInclude.length === 1 && listSetFilter) {
      out = out.filter((e) => (e.sets || []).includes(listSetFilter));
    }
  }

  const q = f.text.trim();
  if (q) {
    out = out.filter((e) => {
      const name = (e.fixedName || e.name || e.path || '');
      if (fuzzyMatch(q, name) || fuzzyMatch(q, e.path ?? '')) return true;
      const tags = e.tags || [];
      if (tags.length && (fuzzyMatch(q, tags.join(' ')) || tags.some((t) => fuzzyMatch(q, t)))) return true;
      const cat = e.category || '';
      if (cat && fuzzyMatch(q, cat)) return true;
      return false;
    });
  }

  return out;
}

loadVjLibraryFilter();

/** Gallery preset tag names (for UI chip ordering). */
export function vjFilterTagChipNames(limit: number, entryTags: string[]): string[] {
  const counts = new Map<string, number>();
  for (const t of entryTags) {
    const key = t.trim();
    if (!key) continue;
    counts.set(key, (counts.get(key) || 0) + 1);
  }
  const top = [...counts.entries()]
    .sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0]))
    .map(([tag]) => tag);
  const order: string[] = [];
  for (const t of galleryPresetTags) {
    if (counts.has(t) && !order.includes(t)) order.push(t);
  }
  for (const t of top) {
    if (!order.includes(t)) order.push(t);
  }
  return order.slice(0, limit);
}

/** Gallery preset set names first, then others. */
export function vjFilterSetChipNames(extraSets: string[]): string[] {
  const names = new Set<string>();
  GALLERY_PRESET_SETS.forEach((s) => names.add(s));
  extraSets.forEach((s) => names.add(s));
  const preset = GALLERY_PRESET_SETS.filter((s) => names.has(s));
  const rest = [...names].filter((s) => !GALLERY_PRESET_SETS.includes(s)).sort();
  return [...preset, ...rest];
}

// Used by vjLibraryFilterUI (format/category chips).
export { toggleVjFilterFormat, toggleVjFilterCategory };
