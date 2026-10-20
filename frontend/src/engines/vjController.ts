/**
 * VJ controller action layer. MIDI and OSC dispatch to these actions.
 * vjDeck registers callbacks when it inits; templates map hardware to action ids.
 */

export type VJActionId =
  | 'vj/crossfader'
  | 'vj/deckA/param/0' | 'vj/deckA/param/1' | 'vj/deckA/param/2' | 'vj/deckA/param/3'
  | 'vj/deckA/param/4' | 'vj/deckA/param/5' | 'vj/deckA/param/6' | 'vj/deckA/param/7'
  | 'vj/deckB/param/0' | 'vj/deckB/param/1' | 'vj/deckB/param/2' | 'vj/deckB/param/3'
  | 'vj/deckB/param/4' | 'vj/deckB/param/5' | 'vj/deckB/param/6' | 'vj/deckB/param/7'
  | 'vj/loadClipA' | 'vj/loadClipB'
  | 'vj/deckA/pageUp' | 'vj/deckA/pageDown' | 'vj/deckB/pageLeft' | 'vj/deckB/pageRight'
  | 'vj/autoVj';

export type VJActionHandler = (value: number) => void;

const handlers: Partial<Record<VJActionId, VJActionHandler>> = {};

export const vjController = {
  register(actionId: VJActionId, handler: VJActionHandler): void {
    handlers[actionId] = handler;
  },

  unregister(actionId: VJActionId): void {
    delete handlers[actionId];
  },

  dispatch(actionId: VJActionId, value: number): void {
    const h = handlers[actionId];
    if (h) {
      const v = Math.max(0, Math.min(1, value));
      h(v);
    }
  },

  dispatchLoadClip(noteOrSlot: number): void {
    const slot = Math.max(0, Math.min(39, Math.floor(noteOrSlot)));
    const track = Math.floor(slot / 5);
    const isDeckA = track % 2 === 0;
    const actionId: VJActionId = isDeckA ? 'vj/loadClipA' : 'vj/loadClipB';
    const h = handlers[actionId];
    if (h) h(slot);
  },

  dispatchLoadClipBySlot(deck: 'A' | 'B', slot: number): void {
    const s = Math.max(0, Math.min(39, Math.floor(slot)));
    const actionId: VJActionId = deck === 'A' ? 'vj/loadClipA' : 'vj/loadClipB';
    const h = handlers[actionId];
    if (h) h(s);
  },

  dispatchPage(actionId: VJActionId): void {
    const h = handlers[actionId];
    if (h) h(1);
  }
};

export const VJ_ACTION_IDS: VJActionId[] = [
  'vj/crossfader',
  'vj/deckA/param/0', 'vj/deckA/param/1', 'vj/deckA/param/2', 'vj/deckA/param/3',
  'vj/deckA/param/4', 'vj/deckA/param/5', 'vj/deckA/param/6', 'vj/deckA/param/7',
  'vj/deckB/param/0', 'vj/deckB/param/1', 'vj/deckB/param/2', 'vj/deckB/param/3',
  'vj/deckB/param/4', 'vj/deckB/param/5', 'vj/deckB/param/6', 'vj/deckB/param/7',
  'vj/loadClipA', 'vj/loadClipB',
  'vj/deckA/pageUp', 'vj/deckA/pageDown', 'vj/deckB/pageLeft', 'vj/deckB/pageRight',
  'vj/autoVj'
];

/**
 * Richer VJ deck surface for continuous controllers (Steam Deck / gamepads): read live state and drive
 * focus-relative actions that the numeric action ids above cannot express (browse cursor, load by
 * library index, mouse, per-deck params by index). vjDeck registers it when the deck is built.
 */
export type VjDeckKey = 'A' | 'B';

export interface VjDeckParamSnapshot {
  name: string;
  type: string;
  min: number;
  max: number;
  value: number | boolean;
}

export interface VjDeckStateSnapshot {
  path: string;
  name: string;
  /** Index in the (filtered) VJ library list, -1 when not in it. */
  libraryIndex: number;
  params: VjDeckParamSnapshot[];
  mouse: { x: number; y: number };
}

export interface VjSurfaceSnapshot {
  /** VJ view visible and deck built; controllers should only act while true. */
  active: boolean;
  crossfader: number;
  autoVj: boolean;
  libraryCount: number;
  decks: Record<VjDeckKey, VjDeckStateSnapshot>;
}

export interface VjDeckSurface {
  snapshot(): VjSurfaceSnapshot;
  libraryEntry(index: number): { path: string; name: string } | null;
  getCrossfader(): number;
  setCrossfader(v: number): void;
  /** Animated crossfade to one deck (tap feel). */
  cutTo(deck: VjDeckKey): void;
  /** Param value normalised to 0..1 (bool 0/1); null if the deck has no such param. */
  getParamNorm(deck: VjDeckKey, index: number): number | null;
  setParamNorm(deck: VjDeckKey, index: number, v: number): void;
  getMouse(deck: VjDeckKey): { x: number; y: number };
  setMouse(deck: VjDeckKey, x: number | null, y: number | null): void;
  loadLibraryIndex(deck: VjDeckKey, index: number): void;
  navigateDeck(deck: VjDeckKey, delta: number): void;
  setAutoVj(on: boolean): void;
  /** Visual focus for pad control (null clears). */
  setPadFocus(deck: VjDeckKey | null): void;
  /** Highlight the library browse cursor (null clears). */
  setBrowseCursor(index: number | null): void;
}

let deckSurface: VjDeckSurface | null = null;

export function registerVjDeckSurface(surface: VjDeckSurface | null): void {
  deckSurface = surface;
  if (typeof window !== 'undefined') window.dispatchEvent(new CustomEvent('macroverse-vj-surface'));
}

export function getVjDeckSurface(): VjDeckSurface | null {
  return deckSurface;
}
