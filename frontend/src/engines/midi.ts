import { pushMonitorEntry } from './midiOscMonitor.js';
import type { VJActionId } from './vjController.js';

const MIDI_MAP_KEY = 'macroverse-midi-map';
const VJ_MIDI_MAP_KEY = 'macroverse-vj-midi-map';
const VJ_MIDI_TEMPLATE_KEY = 'macroverse-vj-midi-template';
const VJ_GENERIC_AUTO_MAP_KEY = 'macroverse-vj-generic-auto-map';

export interface MidiMapping {
  ch: number;
  cc: number;
  inputName: string;
}

export type VJMidiTemplateId = 'none' | 'apc40_mk2' | 'custom';

export interface VJActionMidiMap {
  ch: number;
  cc: number;
}

type VJDeckId = 'A' | 'B';

const APC40_MK2_DEFAULTS: Record<string, VJActionMidiMap> = {
  'vj/crossfader': { ch: 0, cc: 14 },
  'vj/deckA/param/0': { ch: 0, cc: 16 }, 'vj/deckA/param/1': { ch: 0, cc: 17 }, 'vj/deckA/param/2': { ch: 0, cc: 18 }, 'vj/deckA/param/3': { ch: 0, cc: 19 },
  'vj/deckA/param/4': { ch: 0, cc: 20 }, 'vj/deckA/param/5': { ch: 0, cc: 21 }, 'vj/deckA/param/6': { ch: 0, cc: 22 }, 'vj/deckA/param/7': { ch: 0, cc: 23 },
  'vj/deckB/param/0': { ch: 0, cc: 24 }, 'vj/deckB/param/1': { ch: 0, cc: 25 }, 'vj/deckB/param/2': { ch: 0, cc: 26 }, 'vj/deckB/param/3': { ch: 0, cc: 27 },
  'vj/deckB/param/4': { ch: 0, cc: 28 }, 'vj/deckB/param/5': { ch: 0, cc: 29 }, 'vj/deckB/param/6': { ch: 0, cc: 30 }, 'vj/deckB/param/7': { ch: 0, cc: 31 },
  'vj/deckA/pageUp': { ch: 0, cc: 104 }, 'vj/deckA/pageDown': { ch: 0, cc: 105 },
  'vj/deckB/pageLeft': { ch: 0, cc: 106 }, 'vj/deckB/pageRight': { ch: 0, cc: 107 },
  'vj/autoVj': { ch: 0, cc: 15 }
};

const APC40_SHIFT_NOTES = new Set([98, 122]);
const APC40_SCENE_BASE = 82;
const APC_MINI_MK2_SCENE_BASE = 112;
const APC40_PAGE_NOTE_ACTIONS: Record<number, VJActionId> = {
  94: 'vj/deckA/pageUp',
  95: 'vj/deckA/pageDown',
  96: 'vj/deckB/pageRight',
  97: 'vj/deckB/pageLeft'
};
const APC40_PAGE_CC_ACTIONS: Record<number, VJActionId> = {
  104: 'vj/deckA/pageUp',
  105: 'vj/deckA/pageDown',
  106: 'vj/deckB/pageLeft',
  107: 'vj/deckB/pageRight'
};

const GENERIC_AUTO_ACTION_ORDER: VJActionId[] = [
  'vj/crossfader',
  'vj/deckA/param/0',
  'vj/deckA/param/1',
  'vj/deckA/param/2',
  'vj/deckA/param/3',
  'vj/deckA/param/4',
  'vj/deckA/param/5',
  'vj/deckA/param/6',
  'vj/deckA/param/7',
  'vj/deckB/param/0',
  'vj/deckB/param/1',
  'vj/deckB/param/2',
  'vj/deckB/param/3',
  'vj/deckB/param/4',
  'vj/deckB/param/5',
  'vj/deckB/param/6',
  'vj/deckB/param/7',
  'vj/autoVj'
];

function clamp01(v: number): number {
  return Math.max(0, Math.min(1, v));
}

function isRoliblockLikeMidiName(name: string): boolean {
  const n = (name || '').toLowerCase();
  return n.includes('roli') || n.includes('lightpad') || n.includes('block') || n.includes('seaboard');
}

function friendlyActionLabel(actionId: VJActionId): string {
  return actionId.replace(/^vj\//, '').replace(/\//g, ' ');
}

function akaiClipSlotFromNote(note: number, ch: number): number | null {
  if (note >= 0 && note < 5 && ch >= 0 && ch < 8) return ch * 5 + note;
  if (note >= 0 && note < 40) return note;
  return null;
}

function akaiSceneFromNote(note: number): number | null {
  if (note >= APC40_SCENE_BASE && note < APC40_SCENE_BASE + 5) return note - APC40_SCENE_BASE;
  if (note >= APC_MINI_MK2_SCENE_BASE && note < APC_MINI_MK2_SCENE_BASE + 5) return note - APC_MINI_MK2_SCENE_BASE;
  return null;
}

export const midiEngine = {
  access: null as MIDIAccess | null,
  inputs: [] as { id: string; name: string; inp: MIDIInput }[],
  selectedInputId: null as string | null,
  mappings: {} as Record<string, MidiMapping>,
  learning: null as string | null,
  active: false,
  lastCC: null as { ch: number; cc: number; val: number; name: string } | null,
  pendingValues: {} as Record<string, number>,
  onLearnComplete: null as (() => void) | null,

  vjLearning: null as string | null,
  vjOnLearnComplete: null as (() => void) | null,
  vjTemplate: 'apc40_mk2' as VJMidiTemplateId,
  vjActionMap: {} as Record<string, VJActionMidiMap>,
  genericAutoActionMap: {} as Record<string, VJActionId>,
  genericAutoMapEnabled: true,
  onApc40Param: null as ((index: number, value: number) => void) | null,
  vjAkaiShiftDown: false,

  async start(): Promise<void> {
    if (!navigator.requestMIDIAccess) throw new Error('Web MIDI not supported');
    if (!this.access) {
      this.access = await navigator.requestMIDIAccess();
      this.access.onstatechange = () => {
        this.refreshInputs();
        this.listenAll();
      };
    }
    this.refreshInputs();
    this.active = true;
    this.loadMappings();
    this.loadVjMappings();
    this.loadGenericAutoMappings();
    this.listenAll();
  },

  refreshInputs(): void {
    this.inputs = [];
    if (this.access) {
      this.access.inputs.forEach((inp) => this.inputs.push({ id: inp.id, name: inp.name || 'Unnamed', inp }));
    }
  },

  listenAll(): void {
    if (!this.access) return;
    this.access.inputs.forEach((inp) => {
      inp.onmidimessage = (e: MIDIMessageEvent) => this.onMessage(e);
    });
  },

  setLastReceivedLabel(text: string): void {
    const ids = ['midiLastReceived', 'midiLastReceivedVJ'];
    for (const id of ids) {
      const el = document.getElementById(id);
      if (el) el.textContent = text;
    }
  },

  onMessage(evt: MIDIMessageEvent): void {
    const d = evt.data;
    if (!d || d.length < 3) return;
    const status = d[0] & 0xf0;
    const ch = d[0] & 0x0f;
    const target = evt.target as MIDIInput;
    const name = target.name || '?';
    const roliLikeInput = isRoliblockLikeMidiName(name);

    if (status === 0x90 || status === 0x80) {
      const note = d[1];
      const vel = d[2];
      const text = 'Note ' + note + ' Ch' + (ch + 1) + ' = ' + vel;
      this.setLastReceivedLabel(text + ' [' + name + ']');
      pushMonitorEntry({ type: 'midi', device: name, text });
      if (this.selectedInputId && target.id !== this.selectedInputId) return;
      if (roliLikeInput) return;
      if (this.vjTemplate === 'apc40_mk2' && this.handleAkaiNote(ch, note, vel, status === 0x90 && vel > 0, status === 0x80 || vel === 0)) return;
      return;
    }

    if (status !== 0xb0) return;
    const cc = d[1];
    const val = d[2];
    this.lastCC = { ch, cc, val, name };
    const text = 'CC' + cc + ' Ch' + (ch + 1) + ' = ' + val;
    this.setLastReceivedLabel(text + ' [' + name + ']');
    pushMonitorEntry({ type: 'midi', device: name, text });
    if (this.selectedInputId && target.id !== this.selectedInputId) return;
    if (roliLikeInput && !this.learning && !this.vjLearning) return;
    if (this.learning) {
      this.mappings[this.learning] = { ch, cc, inputName: target.name || '' };
      this.learning = null;
      this.saveMappings();
      this.onLearnComplete?.();
      return;
    }
    if (this.vjLearning) {
      this.vjActionMap[this.vjLearning] = { ch, cc };
      this.saveVjMappings();
      this.vjLearning = null;
      this.vjOnLearnComplete?.();
      return;
    }

    for (const actionId of Object.keys(this.vjActionMap)) {
      const m = this.vjActionMap[actionId];
      if (m.ch === ch && m.cc === cc) {
        const norm = val / 127;
        if (this.vjTemplate === 'apc40_mk2' && cc >= 16 && cc <= 23 && this.onApc40Param) {
          this.onApc40Param(cc - 16, norm);
        }
        const isPageAction = actionId.startsWith('vj/deckA/page') || actionId.startsWith('vj/deckB/page');
        if (!isPageAction || val > 0) this.dispatchVjAction(actionId as VJActionId, norm, isPageAction);
        return;
      }
    }

    if (this.vjTemplate === 'apc40_mk2' && this.handleAkaiCc(ch, cc, val)) return;

    for (const paramName of Object.keys(this.mappings)) {
      const m = this.mappings[paramName];
      if (m.cc === cc && m.ch === ch) this.pendingValues[paramName] = val / 127;
    }

    if (!roliLikeInput) this.handleGenericAutoCc(ch, cc, val, name);
  },

  dispatchVjAction(actionId: VJActionId, value: number, page = false): void {
    import('./vjController.js').then(({ vjController }) => {
      if (page) vjController.dispatchPage(actionId);
      else vjController.dispatch(actionId, clamp01(value));
    });
  },

  dispatchVjParam(deck: VJDeckId, index: number, value: number): void {
    if (index < 0 || index > 7) return;
    const actionId = `vj/deck${deck}/param/${index}` as VJActionId;
    this.dispatchVjAction(actionId, value);
  },

  dispatchVjClip(deck: VJDeckId, slot: number): void {
    import('./vjController.js').then(({ vjController }) => {
      vjController.dispatchLoadClipBySlot(deck, slot);
    });
  },

  handleAkaiNote(ch: number, note: number, _vel: number, isOn: boolean, isOff: boolean): boolean {
    if (APC40_SHIFT_NOTES.has(note)) {
      this.vjAkaiShiftDown = isOn && !isOff;
      return true;
    }
    if (!isOn) return false;

    const pageAction = APC40_PAGE_NOTE_ACTIONS[note];
    if (pageAction) {
      this.dispatchVjAction(pageAction, 1, true);
      return true;
    }

    const deck: VJDeckId = this.vjAkaiShiftDown ? 'B' : 'A';
    const scene = akaiSceneFromNote(note);
    if (scene !== null) {
      this.dispatchVjClip(deck, scene);
      return true;
    }

    const slot = akaiClipSlotFromNote(note, ch);
    if (slot !== null) {
      this.dispatchVjClip(deck, slot);
      return true;
    }

    return false;
  },

  handleAkaiCc(ch: number, cc: number, val: number): boolean {
    const norm = clamp01(val / 127);

    if (cc === 14) {
      this.dispatchVjAction('vj/crossfader', norm);
      return true;
    }
    if (cc === 15) {
      this.dispatchVjAction('vj/autoVj', norm);
      return true;
    }

    const pageAction = APC40_PAGE_CC_ACTIONS[cc];
    if (pageAction) {
      if (val > 0) this.dispatchVjAction(pageAction, 1, true);
      return true;
    }

    if (cc >= 16 && cc <= 23) {
      const index = cc - 16;
      this.onApc40Param?.(index, norm);
      this.dispatchVjParam('A', index, norm);
      return true;
    }
    if (cc >= 24 && cc <= 31) {
      this.dispatchVjParam('B', cc - 24, norm);
      return true;
    }

    if (cc === 7 && ch >= 0 && ch < 8) {
      this.dispatchVjParam(this.vjAkaiShiftDown ? 'B' : 'A', ch, norm);
      return true;
    }
    if (cc >= 48 && cc <= 55) {
      this.dispatchVjParam(this.vjAkaiShiftDown ? 'B' : 'A', cc - 48, norm);
      return true;
    }

    return false;
  },

  handleGenericAutoCc(ch: number, cc: number, val: number, inputName: string): boolean {
    if (!this.genericAutoMapEnabled || isRoliblockLikeMidiName(inputName)) return false;
    const key = ch + ':' + cc;
    let actionId = this.genericAutoActionMap[key];
    if (!actionId) {
      const used = new Set(Object.values(this.genericAutoActionMap));
      const nextAction = GENERIC_AUTO_ACTION_ORDER.find((id) => !used.has(id));
      if (!nextAction) return false;
      actionId = nextAction;
      this.genericAutoActionMap[key] = nextAction;
      this.saveGenericAutoMappings();
      pushMonitorEntry({
        type: 'midi',
        device: inputName,
        text: 'Auto-map CC' + cc + ' Ch' + (ch + 1) + ' -> ' + friendlyActionLabel(actionId)
      });
    }
    const value = clamp01(val / 127);
    const page = actionId.startsWith('vj/deckA/page') || actionId.startsWith('vj/deckB/page');
    if (!page || val > 0) this.dispatchVjAction(actionId, value, page);
    return true;
  },

  learn(paramName: string): void {
    this.learning = paramName;
  },

  cancelLearn(): void {
    this.learning = null;
  },

  forget(paramName: string): void {
    delete this.mappings[paramName];
    delete this.pendingValues[paramName];
    this.saveMappings();
  },

  stop(): void {
    if (this.access) this.access.inputs.forEach((inp) => (inp.onmidimessage = null));
    this.active = false;
    this.learning = null;
  },

  saveMappings(): void {
    try {
      localStorage.setItem(MIDI_MAP_KEY, JSON.stringify(this.mappings));
    } catch (_) {}
  },

  loadMappings(): void {
    try {
      const s = localStorage.getItem(MIDI_MAP_KEY);
      if (s) this.mappings = JSON.parse(s);
    } catch (_) {}
  },

  loadVjMappings(): void {
    try {
      const t = localStorage.getItem(VJ_MIDI_TEMPLATE_KEY) as VJMidiTemplateId | null;
      if (t) this.vjTemplate = t;
      const s = localStorage.getItem(VJ_MIDI_MAP_KEY);
      if (s) this.vjActionMap = JSON.parse(s);
      else this.vjActionMap = {};
    } catch (_) {
      this.vjActionMap = {};
    }
    if (this.vjTemplate === 'apc40_mk2' && Object.keys(this.vjActionMap).length === 0) {
      this.vjActionMap = { ...APC40_MK2_DEFAULTS };
      this.saveVjMappings();
    }
  },

  saveGenericAutoMappings(): void {
    try {
      localStorage.setItem(VJ_GENERIC_AUTO_MAP_KEY, JSON.stringify(this.genericAutoActionMap));
    } catch (_) {}
  },

  loadGenericAutoMappings(): void {
    try {
      const s = localStorage.getItem(VJ_GENERIC_AUTO_MAP_KEY);
      this.genericAutoActionMap = s ? JSON.parse(s) : {};
    } catch (_) {
      this.genericAutoActionMap = {};
    }
  },

  getGenericAutoMapCount(): number {
    return Object.keys(this.genericAutoActionMap || {}).length;
  },

  getMidiOutputCount(): number {
    let count = 0;
    this.access?.outputs.forEach(() => { count++; });
    return count;
  },

  hasRoliblockLikeInput(): boolean {
    return this.inputs.some((i) => isRoliblockLikeMidiName(i.name));
  },

  hasRoliblockLikeOutput(): boolean {
    let found = false;
    this.access?.outputs.forEach((out) => {
      if (isRoliblockLikeMidiName(out.name || '')) found = true;
    });
    return found;
  },

  saveVjMappings(): void {
    try {
      localStorage.setItem(VJ_MIDI_TEMPLATE_KEY, this.vjTemplate);
      localStorage.setItem(VJ_MIDI_MAP_KEY, JSON.stringify(this.vjActionMap));
    } catch (_) {}
  },

  setVjTemplate(id: VJMidiTemplateId): void {
    this.vjTemplate = id;
    if (id === 'apc40_mk2') this.vjActionMap = { ...APC40_MK2_DEFAULTS };
    else if (id === 'none') this.vjActionMap = {};
    this.saveVjMappings();
  },

  setVjActionMapping(actionId: string, ch: number, cc: number): void {
    this.vjActionMap[actionId] = { ch, cc };
    this.saveVjMappings();
  },

  getVjActionMapping(actionId: string): VJActionMidiMap | undefined {
    return this.vjActionMap[actionId];
  },

  learnVjAction(actionId: string, onComplete: () => void): void {
    this.vjLearning = actionId;
    this.vjOnLearnComplete = onComplete;
  },
  cancelVjLearn(): void {
    this.vjLearning = null;
    this.vjOnLearnComplete = null;
  }
};
