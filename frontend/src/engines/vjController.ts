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
