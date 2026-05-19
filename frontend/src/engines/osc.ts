import { pushMonitorEntry } from './midiOscMonitor.js';

export interface ParamMeta {
  id: string;
  min: number;
  max: number;
}

const OSC_ADDR_KEY = 'macroverse-osc-addr';
const OSC_VJ_ADDR_KEY = 'macroverse-osc-vj-addr';

const DEFAULT_VJ_OSC_ADDRESSES: Record<string, string> = {
  '/vj/crossfader': 'vj/crossfader',
  '/vj/deckA/param/0': 'vj/deckA/param/0', '/vj/deckA/param/1': 'vj/deckA/param/1', '/vj/deckA/param/2': 'vj/deckA/param/2', '/vj/deckA/param/3': 'vj/deckA/param/3',
  '/vj/deckA/param/4': 'vj/deckA/param/4', '/vj/deckA/param/5': 'vj/deckA/param/5', '/vj/deckA/param/6': 'vj/deckA/param/6', '/vj/deckA/param/7': 'vj/deckA/param/7',
  '/vj/deckB/param/0': 'vj/deckB/param/0', '/vj/deckB/param/1': 'vj/deckB/param/1', '/vj/deckB/param/2': 'vj/deckB/param/2', '/vj/deckB/param/3': 'vj/deckB/param/3',
  '/vj/deckB/param/4': 'vj/deckB/param/4', '/vj/deckB/param/5': 'vj/deckB/param/5', '/vj/deckB/param/6': 'vj/deckB/param/6', '/vj/deckB/param/7': 'vj/deckB/param/7',
  '/vj/deckA/loadClip': 'vj/deckA/loadClip', '/vj/deckB/loadClip': 'vj/deckB/loadClip',
  '/vj/deckA/pageUp': 'vj/deckA/pageUp', '/vj/deckA/pageDown': 'vj/deckA/pageDown',
  '/vj/deckB/pageLeft': 'vj/deckB/pageLeft', '/vj/deckB/pageRight': 'vj/deckB/pageRight',
  '/vj/autoVj': 'vj/autoVj'
};

export const oscEngine = {
  eventSource: null as EventSource | null,
  port: 9000,
  active: false,
  addressMap: {} as Record<string, string>,
  customAddresses: {} as Record<string, string>,
  pendingValues: {} as Record<string, number>,
  onSelectShader: null as ((index: number) => void) | null,

  vjDeckAAddressMap: {} as Record<string, string>,
  vjDeckBAddressMap: {} as Record<string, string>,
  vjDeckAPending: {} as Record<string, number>,
  vjDeckBPending: {} as Record<string, number>,
  vjOscAddressMap: {} as Record<string, string>,
  lastOscMessage: '',

  setLastOscReceived(addr: string, value: number): void {
    this.lastOscMessage = addr + ' = ' + (typeof value === 'number' && !isNaN(value) ? value.toFixed(3) : String(value));
    const el = document.getElementById('oscLastReceived');
    if (el) el.textContent = this.lastOscMessage;
  },

  setVjDeckAAddressMap(map: Record<string, string>): void {
    this.vjDeckAAddressMap = map;
  },
  setVjDeckBAddressMap(map: Record<string, string>): void {
    this.vjDeckBAddressMap = map;
  },

  loadVjOscAddresses(): void {
    try {
      const s = localStorage.getItem(OSC_VJ_ADDR_KEY);
      this.vjOscAddressMap = s ? { ...DEFAULT_VJ_OSC_ADDRESSES, ...JSON.parse(s) } : { ...DEFAULT_VJ_OSC_ADDRESSES };
    } catch (_) {
      this.vjOscAddressMap = { ...DEFAULT_VJ_OSC_ADDRESSES };
    }
  },
  setVjOscAddress(oscAddr: string, actionId: string | null): void {
    if (actionId) this.vjOscAddressMap[oscAddr] = actionId;
    else delete this.vjOscAddressMap[oscAddr];
    try {
      const custom: Record<string, string> = {};
      for (const [k, v] of Object.entries(this.vjOscAddressMap)) {
        if (DEFAULT_VJ_OSC_ADDRESSES[k] !== v) custom[k] = v;
      }
      localStorage.setItem(OSC_VJ_ADDR_KEY, JSON.stringify(custom));
    } catch (_) {}
  },

  async start(port: number): Promise<void> {
    this.port = port || this.port;
    const r = await fetch('/api/osc/start', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ port: this.port })
    });
    if (!r.ok) throw new Error(await r.text());
    this.loadVjOscAddresses();
    if (this.eventSource) this.eventSource.close();
    this.eventSource = new EventSource('/api/osc/stream');
    this.eventSource.onmessage = (e: MessageEvent) => {
      try {
        const msg = JSON.parse(e.data) as { address?: string; value?: number; type?: string };
        const addr = msg.address || '';
        const val = typeof msg.value === 'number' ? msg.value : parseFloat(String(msg.value));
        this.setLastOscReceived(addr, val);
        const oscText = addr + ' = ' + (typeof val === 'number' && !isNaN(val) ? val.toFixed(3) : String(val));
        pushMonitorEntry({ type: 'osc', text: oscText });
        if (msg.type === 'select' && this.onSelectShader && !isNaN(val)) {
          this.onSelectShader(Math.round(val));
          return;
        }
        const normVal = isNaN(val) ? 0 : Math.max(0, Math.min(1, val));
        const vjActionAddr = this.vjOscAddressMap[addr] || addr;
        if (vjActionAddr.startsWith('vj/')) {
          import('./vjController.js').then(({ vjController }) => {
            if (vjActionAddr === 'vj/crossfader') {
              vjController.dispatch('vj/crossfader', normVal);
            } else if (vjActionAddr.startsWith('vj/deckA/param/') || vjActionAddr.startsWith('vj/deckB/param/')) {
              vjController.dispatch(vjActionAddr as import('./vjController.js').VJActionId, normVal);
            } else if (vjActionAddr === 'vj/deckA/loadClip' || vjActionAddr === 'vj/deckB/loadClip') {
              const slot = Math.max(0, Math.min(39, Math.round(isNaN(val) ? 0 : val)));
              vjController.dispatchLoadClipBySlot(vjActionAddr === 'vj/deckA/loadClip' ? 'A' : 'B', slot);
            } else if (vjActionAddr === 'vj/deckA/pageUp' || vjActionAddr === 'vj/deckA/pageDown' || vjActionAddr === 'vj/deckB/pageLeft' || vjActionAddr === 'vj/deckB/pageRight') {
              vjController.dispatchPage(vjActionAddr as import('./vjController.js').VJActionId);
            } else if (vjActionAddr === 'vj/autoVj') {
              vjController.dispatch('vj/autoVj', normVal);
            }
          });
          return;
        }
        if (isNaN(val)) return;
        const vjA = this.vjDeckAAddressMap[addr];
        if (vjA !== undefined) {
          this.vjDeckAPending[vjA] = normVal;
          return;
        }
        const vjB = this.vjDeckBAddressMap[addr];
        if (vjB !== undefined) {
          this.vjDeckBPending[vjB] = normVal;
          return;
        }
        let paramName = this.addressMap[addr];
        if (!paramName) {
          for (const pn in this.customAddresses) {
            if (this.customAddresses[pn] === addr) {
              paramName = pn;
              break;
            }
          }
        }
        if (!paramName) {
          const parts = addr.split('/');
          paramName = parts[parts.length - 1] || '';
        }
        if (paramName) this.pendingValues[paramName] = normVal;
      } catch (_) {}
    };
    this.active = true;
  },

  async stop(): Promise<void> {
    if (this.eventSource) {
      this.eventSource.close();
      this.eventSource = null;
    }
    await fetch('/api/osc/stop', { method: 'POST' }).catch(() => {});
    this.active = false;
    this.pendingValues = {};
    this.vjDeckAPending = {};
    this.vjDeckBPending = {};
  },

  buildAddressMap(params: ParamMeta[]): void {
    this.addressMap = {};
    params.forEach((p) => {
      const addr = this.customAddresses[p.id] || '/shader/' + p.id;
      this.addressMap[addr] = p.id;
    });
  },

  setCustomAddress(paramName: string, addr: string | null): void {
    if (addr) this.customAddresses[paramName] = addr;
    else delete this.customAddresses[paramName];
    this.saveAddresses();
  },

  getAddress(paramName: string): string {
    return this.customAddresses[paramName] || '/shader/' + paramName;
  },

  saveAddresses(): void {
    try {
      localStorage.setItem(OSC_ADDR_KEY, JSON.stringify(this.customAddresses));
    } catch (_) {}
  },

  loadAddresses(): void {
    try {
      const s = localStorage.getItem(OSC_ADDR_KEY);
      if (s) this.customAddresses = JSON.parse(s);
    } catch (_) {}
  }
};
