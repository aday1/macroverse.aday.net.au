import { pushMonitorEntry } from './midiOscMonitor.js';

const MIDI_MAP_KEY = 'macroverse-midi-map';
const VJ_MIDI_MAP_KEY = 'macroverse-vj-midi-map';
const VJ_MIDI_TEMPLATE_KEY = 'macroverse-vj-midi-template';

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
  onApc40Param: null as ((index: number, value: number) => void) | null,

  async start(): Promise<void> {
    if (!navigator.requestMIDIAccess) throw new Error('Web MIDI not supported');
    this.access = await navigator.requestMIDIAccess();
    this.refreshInputs();
    this.access.onstatechange = () => this.refreshInputs();
    this.active = true;
    this.loadVjMappings();
    this.listenAll();
  },

  refreshInputs(): void {
    this.inputs = [];
    if (this.access) {
      this.access.inputs.forEach((inp) => this.inputs.push({ id: inp.id, name: inp.name, inp }));
    }
  },

  listenAll(): void {
    if (!this.access) return;
    this.access.inputs.forEach((inp) => {
      inp.onmidimessage = (e: MIDIMessageEvent) => this.onMessage(e);
    });
  },

  setLastReceivedLabel(text: string): void {
    const el = document.getElementById('midiLastReceived');
    if (el) el.textContent = text;
  },

  onMessage(evt: MIDIMessageEvent): void {
    const d = evt.data;
    if (!d || d.length < 3) return;
    const status = d[0] & 0xf0;
    const ch = d[0] & 0x0f;
    const target = evt.target as MIDIInput;
    const name = target.name || '?';

    if (status === 0x90) {
      const note = d[1];
      const vel = d[2];
      const text = 'Note ' + note + ' Ch' + (ch + 1) + ' = ' + vel;
      this.setLastReceivedLabel(text + ' [' + name + ']');
      pushMonitorEntry({ type: 'midi', device: name, text });
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
      const aid = this.vjLearning;
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
        import('./vjController.js').then(({ vjController }) => {
          if (actionId.startsWith('vj/deckA/page') || actionId.startsWith('vj/deckB/page')) {
            vjController.dispatchPage(actionId as import('./vjController.js').VJActionId);
          } else {
            vjController.dispatch(actionId as import('./vjController.js').VJActionId, norm);
          }
        });
        return;
      }
    }

    for (const paramName of Object.keys(this.mappings)) {
      const m = this.mappings[paramName];
      if (m.cc === cc && m.ch === ch) this.pendingValues[paramName] = val / 127;
    }
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
