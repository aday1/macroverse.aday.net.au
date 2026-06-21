import { fetchLocalStatus } from '../api.js';
import { midiEngine } from '../engines/midi.js';
import { pushMonitorEntry } from '../engines/midiOscMonitor.js';
import { roliblockManager, type RoliblockDevice } from '../engines/roliblock.js';
import { vjController } from '../engines/vjController.js';

export interface ControllerAutoMapStatus {
  active: boolean;
  localOnly: boolean;
  midiEnabled: boolean;
  midiInputs: number;
  midiOutputs: number;
  genericMappings: number;
  roliInputs: number;
  roliOutputs: number;
  roliDevices: number;
  roliReady: number;
  lastMessage: string;
}

let started = false;
let timer: ReturnType<typeof setInterval> | null = null;
let roliSysexAttempted = false;
let lastDeviceSignature = '';

const status: ControllerAutoMapStatus = {
  active: false,
  localOnly: false,
  midiEnabled: false,
  midiInputs: 0,
  midiOutputs: 0,
  genericMappings: 0,
  roliInputs: 0,
  roliOutputs: 0,
  roliDevices: 0,
  roliReady: 0,
  lastMessage: 'waiting'
};

function isLocalHost(): boolean {
  if (typeof window === 'undefined') return false;
  const host = window.location.hostname;
  return host === 'localhost' || host === '127.0.0.1' || host === '::1' || host === '';
}

function activeView(): string {
  return document.querySelector('.view-tab.active')?.getAttribute('data-view') || '';
}

async function routeRoliblockMouse(dev: RoliblockDevice, x: number, y: number): Promise<void> {
  const view = activeView();
  if (view === 'vj') {
    const mod = await import('../panels/vjDeck.js');
    if (roliblockManager.ledDisplayMode === 'linked') {
      const linked = roliblockManager.getLinkedMouse(dev);
      mod.setVjMouseFromRoliblock(linked.x, linked.y);
    } else if (dev.deckAssignment === 'deckA') {
      mod.setVjMouseForDeckA(x, y);
    } else if (dev.deckAssignment === 'deckB') {
      mod.setVjMouseForDeckB(x, y);
    } else {
      mod.setVjMouseFromRoliblock(x, y);
    }
    return;
  }
  if (view === 'gallery') {
    const mod = await import('../panels/gallery.js');
    if (roliblockManager.ledDisplayMode === 'linked') {
      const linked = roliblockManager.getLinkedMouse(dev);
      mod.setGalleryRoliblockMouse(linked.x, linked.y);
    } else {
      mod.setGalleryRoliblockMouse(x, y);
    }
    return;
  }
  const render = await import('../render.js');
  if (roliblockManager.ledDisplayMode === 'linked') {
    const linked = roliblockManager.getLinkedMouse(dev);
    render.setMouse(linked.x, linked.y);
  } else {
    render.setMouse(x, y);
  }
}

function wireRoliblocks(): void {
  for (const dev of roliblockManager.getDevices()) {
    if (!dev.enabled) {
      dev._onMouse = null;
      dev._onCrossfader = null;
      continue;
    }
    dev._onMouse = (x, y) => {
      routeRoliblockMouse(dev, x, y).catch(() => {});
    };
    dev._onCrossfader = dev.crossfaderEnabled ? (value) => vjController.dispatch('vj/crossfader', value) : null;
  }
}

function refreshStatus(message?: string, emit = true): void {
  status.midiEnabled = midiEngine.active;
  status.midiInputs = midiEngine.inputs.length;
  status.midiOutputs = midiEngine.getMidiOutputCount();
  status.genericMappings = midiEngine.getGenericAutoMapCount();
  status.roliInputs = roliblockManager.inputs.filter((i) => roliblockManager.isRoliblockLike(i.name)).length;
  status.roliOutputs = roliblockManager.outputs.filter((o) => roliblockManager.isRoliblockLike(o.name)).length;
  status.roliDevices = roliblockManager.getDevices().length;
  status.roliReady = roliblockManager.getDevices().filter((d) => d.isReady()).length;
  if (message) status.lastMessage = message;
  if (emit) window.dispatchEvent(new CustomEvent('macroverse:controller-automap'));
}

function deviceSignature(): string {
  const inputs = midiEngine.inputs.map((i) => i.id + ':' + i.name).sort();
  const outputs: string[] = [];
  midiEngine.access?.outputs.forEach((out) => outputs.push(out.id + ':' + (out.name || '')));
  return inputs.concat(outputs.sort()).join('|');
}

async function maybeMapRoliblocks(): Promise<void> {
  const roliSeen = midiEngine.hasRoliblockLikeInput() || midiEngine.hasRoliblockLikeOutput() || roliblockManager.getDevices().length > 0;
  if (!roliSeen || roliSysexAttempted) return;
  roliSysexAttempted = true;
  const ok = await roliblockManager.requestAccess();
  if (!ok) {
    refreshStatus('Roliblock sysex permission unavailable');
    return;
  }
  roliblockManager.reconnectSaved();
  roliblockManager.autoDetectDevices();
  wireRoliblocks();
  const devices = roliblockManager.getDevices().length;
  refreshStatus('Roliblock auto-detect: ' + devices + ' device(s)');
  pushMonitorEntry({ type: 'midi', device: 'Roliblock', text: 'Auto-detect ' + devices + ' device(s)' });
}

async function runAutoMapPass(): Promise<void> {
  if (!status.localOnly) return;
  try {
    await midiEngine.start();
    midiEngine.refreshInputs();
    midiEngine.listenAll();
    const sig = deviceSignature();
    if (sig !== lastDeviceSignature) {
      lastDeviceSignature = sig;
      roliSysexAttempted = false;
      refreshStatus('MIDI devices: ' + midiEngine.inputs.length + ' in / ' + midiEngine.getMidiOutputCount() + ' out');
    }
    await maybeMapRoliblocks();
    wireRoliblocks();
    refreshStatus();
  } catch (err) {
    refreshStatus((err as Error).message || 'MIDI unavailable');
  }
}

export function getControllerAutoMapStatus(): ControllerAutoMapStatus {
  refreshStatus(undefined, false);
  return { ...status };
}

export function initControllerAutoMap(): void {
  if (started || typeof window === 'undefined') return;
  started = true;
  status.active = true;

  void fetchLocalStatus()
    .then((localStatus) => {
      status.localOnly = isLocalHost() || !!localStatus?.privateLibrary || localStatus?.hostMode === 'desktop';
      if (!status.localOnly) {
        refreshStatus('cloud lane: controller automap disabled');
        return;
      }
      void runAutoMapPass();
      timer = setInterval(() => {
        void runAutoMapPass();
      }, 5000);
      window.addEventListener('focus', () => {
        void runAutoMapPass();
      });
    })
    .catch(() => {
      status.localOnly = isLocalHost();
      if (status.localOnly) void runAutoMapPass();
    });

  window.addEventListener('beforeunload', () => {
    if (timer) clearInterval(timer);
  });
}
