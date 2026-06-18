import type { IndexEntry, Settings } from './types.js';

const DB_NAME = 'macroverse-cloud-local';
const DB_VERSION = 1;
const CHANGE_EVENT = 'macroverse-local-store-changed';

type StoreName = 'shaders' | 'meta' | 'created' | 'deleted' | 'thumbnails' | 'settings';

export type LocalStoreStats = {
  shaderEdits: number;
  created: number;
  deleted: number;
  thumbnails: number;
  hasSettings: boolean;
};

function normPath(path: string): string {
  return (path || '').replace(/[\\/]+/g, '|').replace(/^\|+|\|+$/g, '');
}

function entryNameFromPath(path: string): string {
  const np = normPath(path);
  const parts = np.split('|');
  return parts[parts.length - 1] || 'shader.fs';
}

function entryCategoryFromPath(path: string): string {
  const np = normPath(path);
  const parts = np.split('|');
  if (parts.length <= 1) return '';
  return parts.slice(0, -1).join('/');
}

let dbPromise: Promise<IDBDatabase> | null = null;

function openDb(): Promise<IDBDatabase> {
  if (typeof indexedDB === 'undefined') {
    return Promise.reject(new Error('IndexedDB unavailable'));
  }
  if (!dbPromise) {
    dbPromise = new Promise((resolve, reject) => {
      const req = indexedDB.open(DB_NAME, DB_VERSION);
      req.onupgradeneeded = () => {
        const db = req.result;
        if (!db.objectStoreNames.contains('shaders')) db.createObjectStore('shaders');
        if (!db.objectStoreNames.contains('meta')) db.createObjectStore('meta');
        if (!db.objectStoreNames.contains('created')) db.createObjectStore('created', { keyPath: 'id' });
        if (!db.objectStoreNames.contains('deleted')) db.createObjectStore('deleted');
        if (!db.objectStoreNames.contains('thumbnails')) db.createObjectStore('thumbnails');
        if (!db.objectStoreNames.contains('settings')) db.createObjectStore('settings');
      };
      req.onsuccess = () => resolve(req.result);
      req.onerror = () => reject(req.error ?? new Error('IndexedDB open failed'));
    });
  }
  return dbPromise;
}

async function idbTx<T>(
  store: StoreName,
  mode: IDBTransactionMode,
  fn: (os: IDBObjectStore) => IDBRequest<T> | void
): Promise<T | undefined> {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(store, mode);
    const os = tx.objectStore(store);
    let req: IDBRequest<T> | undefined;
    try {
      req = fn(os) as IDBRequest<T> | undefined;
    } catch (e) {
      reject(e);
      return;
    }
    if (!req) {
      tx.oncomplete = () => resolve(undefined);
      tx.onerror = () => reject(tx.error);
      return;
    }
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}

async function idbGet<T>(store: StoreName, key: IDBValidKey): Promise<T | undefined> {
  return idbTx<T>(store, 'readonly', (os) => os.get(key));
}

async function idbPut(store: StoreName, value: unknown, key?: IDBValidKey): Promise<void> {
  await idbTx(store, 'readwrite', (os) => (key !== undefined ? os.put(value, key) : os.put(value)));
}

async function idbDelete(store: StoreName, key: IDBValidKey): Promise<void> {
  await idbTx(store, 'readwrite', (os) => os.delete(key));
}

async function idbGetAll<T>(store: StoreName): Promise<T[]> {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(store, 'readonly');
    const req = tx.objectStore(store).getAll();
    req.onsuccess = () => resolve((req.result || []) as T[]);
    req.onerror = () => reject(req.error);
  });
}

async function idbGetAllKeys(store: StoreName): Promise<IDBValidKey[]> {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(store, 'readonly');
    const req = tx.objectStore(store).getAllKeys();
    req.onsuccess = () => resolve(req.result || []);
    req.onerror = () => reject(req.error);
  });
}

function notifyChanged(): void {
  window.dispatchEvent(new CustomEvent(CHANGE_EVENT));
}

export function onLocalStoreChange(fn: () => void): () => void {
  window.addEventListener(CHANGE_EVENT, fn);
  return () => window.removeEventListener(CHANGE_EVENT, fn);
}

async function nextLocalId(): Promise<number> {
  const cur = (await idbGet<number>('settings', '_nextLocalId')) ?? 0;
  const next = cur - 1;
  await idbPut('settings', next, '_nextLocalId');
  return next;
}

export async function getLocalShaderContent(path: string): Promise<string | null> {
  const v = await idbGet<{ content?: string }>('shaders', normPath(path));
  return v && typeof v.content === 'string' ? v.content : null;
}

export async function saveLocalShader(path: string, content: string): Promise<void> {
  const np = normPath(path);
  await idbPut('shaders', { content, updatedAt: Date.now() }, np);
  notifyChanged();
}

/** New shader created on cloud — add index row local to this browser. */
export async function registerLocalShaderEntry(path: string): Promise<IndexEntry> {
  const np = normPath(path);
  const created = await idbGetAll<IndexEntry>('created');
  const existing = created.find((e) => normPath(e.path || '') === np);
  if (existing) return existing;
  const id = await nextLocalId();
  const entry: IndexEntry = {
    id,
    path: np,
    name: entryNameFromPath(np),
    category: entryCategoryFromPath(np),
    tags: [],
    sets: [],
    format: /\.vert$/i.test(np) ? 'vert' : 'glsl',
  };
  await idbPut('created', entry);
  notifyChanged();
  return entry;
}
export async function hasLocalShaderOverride(path: string): Promise<boolean> {
  return (await getLocalShaderContent(path)) != null;
}

export async function getMetaOverride(id: number): Promise<Partial<IndexEntry> | null> {
  const v = await idbGet<Partial<IndexEntry>>('meta', id);
  return v || null;
}

export async function saveMetaOverride(id: number, patch: Partial<IndexEntry>): Promise<void> {
  const prev = (await getMetaOverride(id)) || {};
  await idbPut('meta', { ...prev, ...patch, id }, id);
  notifyChanged();
}

export async function mergeIndexWithLocal(serverEntries: IndexEntry[]): Promise<IndexEntry[]> {
  const deletedKeys = await idbGetAllKeys('deleted');
  const deleted = new Set(deletedKeys.map((k) => Number(k)));
  const metas = await idbGetAll<Partial<IndexEntry>>('meta');
  const metaById = new Map<number, Partial<IndexEntry>>();
  for (const m of metas) {
    if (typeof m.id === 'number') metaById.set(m.id, m);
  }
  const merged = serverEntries
    .filter((e) => !deleted.has(e.id))
    .map((e) => {
      const patch = metaById.get(e.id);
      return patch ? ({ ...e, ...patch, id: e.id } as IndexEntry) : e;
    });
  const created = await idbGetAll<IndexEntry>('created');
  const serverPaths = new Set(merged.map((e) => normPath(e.path || '')));
  for (const c of created) {
    const np = normPath(c.path || '');
    if (!np || serverPaths.has(np)) continue;
    merged.push(c);
  }
  return merged;
}

export async function applyLocalUpdate(payload: {
  id?: number;
  name?: string;
  tags?: string[];
  sets?: string[];
  notes?: string;
  category?: string;
  favorite?: boolean;
  color?: string;
  paramRanges?: IndexEntry['paramRanges'];
}): Promise<void> {
  if (payload.id == null) return;
  const id = payload.id;
  if (id < 0) {
    const created = await idbGet<IndexEntry>('created', id);
    if (created) {
      await idbPut('created', { ...created, ...payload, id } as IndexEntry);
    }
    notifyChanged();
    return;
  }
  await saveMetaOverride(id, payload);
}

export async function applyLocalRename(id: number, newName: string): Promise<{ path?: string; name?: string }> {
  const trimmed = (newName || '').trim();
  if (!trimmed) throw new Error('empty name');
  if (id < 0) {
    const created = await idbGet<IndexEntry>('created', id);
    if (!created?.path) throw new Error('entry not found');
    const dir = entryCategoryFromPath(created.path);
    const newPath = dir ? dir.replace(/\//g, '|') + '|' + trimmed : trimmed;
    const content = await getLocalShaderContent(created.path);
    if (content != null) {
      await idbDelete('shaders', normPath(created.path));
      await idbPut('shaders', { content, updatedAt: Date.now() }, normPath(newPath));
    }
    const thumb = await idbGet<string>('thumbnails', normPath(created.path));
    if (thumb) {
      await idbDelete('thumbnails', normPath(created.path));
      await idbPut('thumbnails', thumb, normPath(newPath));
    }
    await idbPut('created', { ...created, id, path: newPath, name: trimmed }, id);
    notifyChanged();
    return { path: newPath, name: trimmed };
  }
  await saveMetaOverride(id, { name: trimmed, fixedName: trimmed });
  return { name: trimmed };
}

export async function applyLocalMove(id: number, category: string): Promise<{ category?: string }> {
  const cat = (category || '').trim();
  if (id < 0) {
    const created = await idbGet<IndexEntry>('created', id);
    if (!created?.path) throw new Error('entry not found');
    const name = entryNameFromPath(created.path);
    const newPath = cat ? cat.replace(/\//g, '|') + '|' + name : name;
    const content = await getLocalShaderContent(created.path);
    if (content != null) {
      await idbDelete('shaders', normPath(created.path));
      await idbPut('shaders', { content, updatedAt: Date.now() }, normPath(newPath));
    }
    const thumb = await idbGet<string>('thumbnails', normPath(created.path));
    if (thumb) {
      await idbDelete('thumbnails', normPath(created.path));
      await idbPut('thumbnails', thumb, normPath(newPath));
    }
    await idbPut('created', { ...created, id, path: newPath, category: cat, name }, id);
    notifyChanged();
    return { category: cat };
  }
  await saveMetaOverride(id, { category: cat });
  return { category: cat };
}

export async function applyLocalDelete(id: number): Promise<void> {
  if (id < 0) {
    const created = await idbGet<IndexEntry>('created', id);
    if (created?.path) {
      await idbDelete('shaders', normPath(created.path));
      await idbDelete('thumbnails', normPath(created.path));
    }
    await idbDelete('created', id);
    notifyChanged();
    return;
  }
  await idbPut('deleted', true, id);
  notifyChanged();
}

export async function saveLocalThumbnail(path: string, dataUrl: string): Promise<void> {
  await idbPut('thumbnails', dataUrl, normPath(path));
  notifyChanged();
}

export async function getLocalThumbnails(paths: string[]): Promise<Record<string, string>> {
  const out: Record<string, string> = {};
  for (const p of paths) {
    const np = normPath(p);
    const v = await idbGet<string>('thumbnails', np);
    if (v) out[np] = v;
  }
  return out;
}

export async function getLocalSettings(): Promise<Partial<Settings>> {
  return (await idbGet<Partial<Settings>>('settings', 'app')) || {};
}

export async function saveLocalSettings(patch: Partial<Settings>): Promise<void> {
  const prev = await getLocalSettings();
  await idbPut('settings', { ...prev, ...patch }, 'app');
  notifyChanged();
}

export async function getLocalStoreStats(): Promise<LocalStoreStats> {
  const shaderKeys = await idbGetAllKeys('shaders');
  const created = await idbGetAll<IndexEntry>('created');
  const deleted = await idbGetAllKeys('deleted');
  const thumbKeys = await idbGetAllKeys('thumbnails');
  const settings = await getLocalSettings();
  return {
    shaderEdits: shaderKeys.length,
    created: created.length,
    deleted: deleted.length,
    thumbnails: thumbKeys.length,
    hasSettings: Object.keys(settings).length > 0,
  };
}

export async function exportLocalStore(): Promise<string> {
  const payload = {
    version: 1,
    exportedAt: new Date().toISOString(),
    shaders: {} as Record<string, { content: string; updatedAt: number }>,
    meta: await idbGetAll<Partial<IndexEntry>>('meta'),
    created: await idbGetAll<IndexEntry>('created'),
    deleted: await idbGetAllKeys('deleted'),
    thumbnails: {} as Record<string, string>,
    settings: await getLocalSettings(),
  };
  for (const key of await idbGetAllKeys('shaders')) {
    const row = await idbGet<{ content: string; updatedAt: number }>('shaders', key);
    if (row) payload.shaders[String(key)] = row;
  }
  for (const key of await idbGetAllKeys('thumbnails')) {
    const row = await idbGet<string>('thumbnails', key);
    if (row) payload.thumbnails[String(key)] = row;
  }
  return JSON.stringify(payload);
}

export async function importLocalStore(json: string, merge: boolean): Promise<void> {
  const data = JSON.parse(json) as {
    shaders?: Record<string, { content: string; updatedAt?: number }>;
    meta?: Partial<IndexEntry>[];
    created?: IndexEntry[];
    deleted?: number[];
    thumbnails?: Record<string, string>;
    settings?: Partial<Settings>;
  };
  if (!merge) await clearLocalStore(false);
  if (data.shaders) {
    for (const [path, row] of Object.entries(data.shaders)) {
      await idbPut('shaders', { content: row.content, updatedAt: row.updatedAt || Date.now() }, path);
    }
  }
  if (data.meta) {
    for (const m of data.meta) {
      if (typeof m.id === 'number') await idbPut('meta', m, m.id);
    }
  }
  if (data.created) {
    for (const c of data.created) await idbPut('created', c);
  }
  if (data.deleted) {
    for (const id of data.deleted) await idbPut('deleted', true, id);
  }
  if (data.thumbnails) {
    for (const [path, url] of Object.entries(data.thumbnails)) {
      await idbPut('thumbnails', url, path);
    }
  }
  if (data.settings) await idbPut('settings', data.settings, 'app');
  notifyChanged();
}

export async function clearLocalStore(notify = true): Promise<void> {
  const stores: StoreName[] = ['shaders', 'meta', 'created', 'deleted', 'thumbnails', 'settings'];
  const db = await openDb();
  await new Promise<void>((resolve, reject) => {
    const tx = db.transaction(stores, 'readwrite');
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
    for (const s of stores) tx.objectStore(s).clear();
  });
  if (notify) notifyChanged();
}

export function isLocalOnlyEntry(entry: IndexEntry): boolean {
  return typeof entry.id === 'number' && entry.id < 0;
}
