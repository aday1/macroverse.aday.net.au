export type DeckId = 'A' | 'B';
export type BindMode = 'file' | 'live';

export interface DeckLiveBindEntry {
  mode: BindMode;
  path: string;
}

export interface VjLiveBindState {
  deckA: DeckLiveBindEntry;
  deckB: DeckLiveBindEntry;
}

const STORAGE_KEY = 'macroverse-vj-live-bind-v1';
const LIVE_SOURCE_EVENT = 'macroverse-vj-live-source';

function defaultEntry(): DeckLiveBindEntry {
  return { mode: 'file', path: '' };
}

function defaultState(): VjLiveBindState {
  return { deckA: defaultEntry(), deckB: defaultEntry() };
}

function deckKey(deck: DeckId): 'deckA' | 'deckB' {
  return deck === 'A' ? 'deckA' : 'deckB';
}

function normPath(path: string): string {
  return (path || '').replace(/\\/g, '|');
}

export function loadVjLiveBind(): VjLiveBindState {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return defaultState();
    const parsed = JSON.parse(raw) as Partial<VjLiveBindState>;
    return {
      deckA: {
        mode: parsed.deckA?.mode === 'live' ? 'live' : 'file',
        path: typeof parsed.deckA?.path === 'string' ? normPath(parsed.deckA.path) : '',
      },
      deckB: {
        mode: parsed.deckB?.mode === 'live' ? 'live' : 'file',
        path: typeof parsed.deckB?.path === 'string' ? normPath(parsed.deckB.path) : '',
      },
    };
  } catch {
    return defaultState();
  }
}

export function saveVjLiveBind(state: VjLiveBindState): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  } catch {
    /* ignore */
  }
}

export function isDeckLiveBound(deck: DeckId, path: string): boolean {
  const entry = loadVjLiveBind()[deckKey(deck)];
  return entry.mode === 'live' && entry.path === normPath(path);
}

export function setDeckLiveBind(deck: DeckId, mode: BindMode, path?: string): void {
  const state = loadVjLiveBind();
  const entry = state[deckKey(deck)];
  entry.mode = mode;
  if (path !== undefined) entry.path = normPath(path);
  saveVjLiveBind(state);
}

export function toggleDeckLiveBind(deck: DeckId, path: string): BindMode {
  const np = normPath(path);
  const state = loadVjLiveBind();
  const entry = state[deckKey(deck)];
  if (entry.mode === 'live' && entry.path === np) {
    entry.mode = 'file';
  } else {
    entry.mode = 'live';
    entry.path = np;
  }
  saveVjLiveBind(state);
  return entry.mode;
}

export function notifyVjLiveSource(path: string, source: string): void {
  if (typeof window === 'undefined') return;
  window.dispatchEvent(
    new CustomEvent(LIVE_SOURCE_EVENT, { detail: { path: normPath(path), source } })
  );
}
