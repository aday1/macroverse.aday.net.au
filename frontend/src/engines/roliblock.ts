/**
 * Roliblock (Roli Lightpad Block) integration — multi-device support.
 * Uses the BLOCKS protocol (7-bit packed DataChangeList) for LED output,
 * matching the blocks-playground-master reference implementation.
 * Touch input via MPE MIDI (pitch bend = X, CC74 = Y, pressure = Z).
 *
 * Supports N Roli Lightpad Blocks simultaneously. Each device has its own
 * MIDI input/output, handshake state, LED buffer, LED settings, and callbacks.
 * Default VJ rig mirrors the master output to every enabled block. Manual
 * modes can split blocks by Deck A/B or stretch the output across two blocks.
 */

import { pushMonitorEntry } from './midiOscMonitor.js';

export const GRID_COLS = 15;
export const GRID_ROWS = 15;
const DEVICE_INDEX = 0x00;
const LED_DATA_OFFSET = 113;
const LED_PIXEL_COUNT = GRID_COLS * GRID_ROWS;
const LED_BYTE_COUNT = LED_PIXEL_COUNT * 2;
const CROSSFADER_CC = 73;
const LED_SEND_INTERVAL_MS = 40;
const BLE_LED_SEND_INTERVAL_MS = 80;
const MAX_PACKET_SIZE = 200;
const PACKET_COUNTER_MAX = 0x03ff;

export type RoliblockMode = 'preview' | 'vj';
export type DeckAssignment = 'auto' | 'deckA' | 'deckB' | 'shared';
export type LedDisplayMode = 'sharedOutput' | 'independent' | 'stretched' | 'linked';
export type ChannelIsolation = 'all' | 'r' | 'g' | 'b';

export interface LedSettings {
  contrast: number;
  brightness: number;
  saturation: number;
  gamma: number;
  grayscale: boolean;
  invert: boolean;
  posterize: number;
  channelIsolation: ChannelIsolation;
}

export interface RoliblockState {
  enabled: boolean;
  mode: RoliblockMode;
  crossfaderEnabled: boolean;
  midiInputId: string | null;
  midiOutputId: string | null;
  inputs: { id: string; name: string }[];
  outputs: { id: string; name: string }[];
}

export interface RoliblockDeviceState {
  id: string;
  label: string;
  enabled: boolean;
  midiInputId: string | null;
  midiOutputId: string | null;
  deckAssignment: DeckAssignment;
  crossfaderEnabled: boolean;
  ledSettings: LedSettings;
}

export type RoliblockMouseCallback = (x: number, y: number) => void;
export type RoliblockCrossfaderCallback = (value: number) => void;

function defaultLedSettings(): LedSettings {
  return {
    contrast: 1.0, brightness: 0.0, saturation: 1.0, gamma: 1.0,
    grayscale: false, invert: false, posterize: 0, channelIsolation: 'all'
  };
}

// -- 7-bit packed array builder (ported from blocks-playground BitConversionUtils.js) --
class Packed7BitBuilder {
  _data: number[] = [];
  _written = 0;
  _bits = 0;

  clone(): Packed7BitBuilder {
    const c = new Packed7BitBuilder();
    c._data = this._data.slice();
    c._written = this._written;
    c._bits = this._bits;
    return c;
  }
  size(): number { return this._written + (this._bits > 0 ? 1 : 0); }
  getData(): number[] { return this._data.slice(0, this.size()); }

  writeBits(value: number, numBits: number): void {
    while (numBits > 0) {
      if (this._bits === 0) {
        if (numBits < 7) {
          this._data[this._written] = value & 0x7f;
          this._bits = numBits;
          return;
        }
        if (numBits === 7) {
          this._data[this._written++] = value & 0x7f;
          return;
        }
        this._data[this._written++] = value & 0x7f;
        value >>>= 7;
        numBits -= 7;
      } else {
        const todo = Math.min(7 - this._bits, numBits);
        this._data[this._written] = (this._data[this._written] || 0) | ((value & ((1 << todo) - 1)) << this._bits);
        value >>>= todo;
        numBits -= todo;
        this._bits += todo;
        if (this._bits === 7) { this._bits = 0; this._written++; }
      }
    }
  }
}

// -- BLOCKS SysEx wrapper with checksum (from Block.js) --
function buildBlockSysEx(deviceIndex: number, payload: number[]): Uint8Array {
  const len = payload.length + 8;
  const d = new Uint8Array(len);
  d[0] = 0xf0; d[1] = 0x00; d[2] = 0x21; d[3] = 0x10; d[4] = 0x77;
  d[5] = deviceIndex & 0x7f;
  for (let i = 0; i < payload.length; i++) d[6 + i] = payload[i] & 0x7f;
  d[len - 1] = 0xf7;
  let ck = (len - 8) & 0xff;
  for (let i = 6; i < len - 2; i++) { ck = (ck + ck * 2 + d[i]) & 0xff; }
  d[len - 2] = ck & 0x7f;
  return d;
}

// -- RGBA to 16-bit BGR565 (matching BitmapLED.js to16bitColor) --
function rgbaToBgr565(r: number, g: number, b: number, a: number): number {
  const af = a / 255;
  const r5 = ((r * af) >> 3) & 0x1f;
  const g6 = ((g * af) >> 2) & 0x3f;
  const b5 = ((b * af) >> 3) & 0x1f;
  return (b5 << 11) | (g6 << 5) | r5;
}

function parseDump(hex: string): number[] {
  return hex.trim().split(/\s+/).map(h => parseInt(h, 16));
}

// -- Handshake constants --
const BITMAP_LED_DUMP_1 = '02 01 00 30 5A 3E 47 0B 20 01 3A 00 10 71 01 12 4B 31 09 08 60 46 5F 25 11 40 05 02 28 61 01 17 54 11 40 10 36 78 21 12 6D 1C 30 5B 00 2E 28 63 00 23 6C 70 43 24 5A 39 60 32 01 28 09 41 0D 3E 28 24 10 1B 04 51 48 1A 0A 08 22 09 1B 2C 30 45 0D 2E 08 24 20 1B 1C 00 5B 6C 50 41 16 36 58 20 10 01 6D 50 40 2D 36 58 60 0B 01 6D 70 40 2D 3A 78 3F 00 0F 1C 78 4F 07 2E 28 78 08 19 04 52 06 15 01 48 24 00 21 64 10 48 1A 02 18 60 0C 01 4C 70 40 05 7C 3F 00 7F 0F 60 7F 03 78 7F 00 7E 1F 40 7F 07 70 7F 01 7C 3F 00 7F 0F 60 7F 03 78 7F 00 7E 1F 40 7F 07 70 7F 01 7C 3F 00 7F 0F 00';
const BITMAP_LED_DUMP_2 = '02 02 00 0C 5C 7F 07 70 7F 01 7C 3F 00 7F 0F 60 7F 03 78 7F 00 7E 1F 40 7F 07 70 7F 01 7C 3F 00 7F 0F 60 7F 03 78 7F 00 7E 1F 40 7F 07 70 7F 01 7C 3F 00 7F 0F 60 7F 03 78 7F 00 7E 1F 40 7F 07 70 7F 01 7C 3F 00 7F 0F 60 7F 03 78 7F 00 1E 19 00 4B';

function read7BitBits(data: Uint8Array, bitPos: number, numBits: number): number {
  let v = 0;
  let rd = 0;
  while (rd < numBits) {
    const byteIdx = Math.floor((bitPos + rd) / 7);
    const bitInByte = (bitPos + rd) % 7;
    const avail = 7 - bitInByte;
    const toRead = Math.min(numBits - rd, avail);
    v |= ((data[byteIdx] >>> bitInByte) & ((1 << toRead) - 1)) << rd;
    rd += toRead;
  }
  return v;
}

function blendRgba(a: Uint8ClampedArray, b: Uint8ClampedArray, t: number): Uint8ClampedArray {
  const out = new Uint8ClampedArray(a.length);
  for (let i = 0; i < a.length; i++) out[i] = Math.round(a[i] * (1 - t) + b[i] * t);
  return out;
}

function isRoliblockLike(name: string): boolean {
  const n = (name || '').toLowerCase();
  return n.includes('roli') || n.includes('lightpad') || n.includes('block') || n.includes('seaboard');
}

// ---------------------------------------------------------------------------
// BLE MIDI connection — Web Bluetooth API for Bluetooth Low Energy MIDI
// ---------------------------------------------------------------------------
const BLE_MIDI_SERVICE = '03b80e5a-ede8-4b33-a751-6ce34ec4c700';
const BLE_MIDI_CHAR    = '7772e5db-3868-4112-a1a9-f2669d106bf3';

export type BleConnectionState = 'disconnected' | 'connecting' | 'connected';

class BleMidiConnection {
  device: BluetoothDevice | null = null;
  characteristic: BluetoothRemoteGATTCharacteristic | null = null;
  connectionState: BleConnectionState = 'disconnected';
  mtu = 20;
  onMidiMessage: ((data: Uint8Array) => void) | null = null;
  onDisconnect: (() => void) | null = null;
  private _sysexBuf: number[] = [];

  async pair(): Promise<boolean> {
    if (typeof navigator === 'undefined' || !navigator.bluetooth) return false;
    try {
      this.connectionState = 'connecting';
      this.device = await navigator.bluetooth.requestDevice({
        filters: [{ services: [BLE_MIDI_SERVICE] }],
        optionalServices: [BLE_MIDI_SERVICE]
      });
      return await this._connectGatt();
    } catch (_) {
      this.connectionState = 'disconnected';
      return false;
    }
  }

  private async _connectGatt(): Promise<boolean> {
    if (!this.device || !this.device.gatt) return false;
    try {
      const server = await this.device.gatt.connect();
      const service = await server.getPrimaryService(BLE_MIDI_SERVICE);
      this.characteristic = await service.getCharacteristic(BLE_MIDI_CHAR);
      await this.characteristic.startNotifications();
      this.characteristic.addEventListener('characteristicvaluechanged', this._handleNotification);
      this.device.addEventListener('gattserverdisconnected', this._handleDisconnect);
      this.connectionState = 'connected';
      console.log('[BLE MIDI] Connected: ' + (this.device.name || this.device.id));
      return true;
    } catch (e) {
      console.warn('[BLE MIDI] GATT connect failed:', e);
      this.connectionState = 'disconnected';
      return false;
    }
  }

  private _handleNotification = (event: Event): void => {
    const target = event.target as BluetoothRemoteGATTCharacteristic;
    const dv = target.value;
    if (!dv || dv.byteLength < 3) return;
    const messages = this._parseBlePacket(dv);
    for (const msg of messages) {
      // Accumulate SysEx fragments
      if (msg[0] === 0xF0) {
        this._sysexBuf = Array.from(msg);
      } else if (this._sysexBuf.length > 0) {
        this._sysexBuf.push(...Array.from(msg));
      }
      if (this._sysexBuf.length > 0 && this._sysexBuf[this._sysexBuf.length - 1] === 0xF7) {
        this.onMidiMessage?.(new Uint8Array(this._sysexBuf));
        this._sysexBuf = [];
        continue;
      }
      if (this._sysexBuf.length === 0) {
        this.onMidiMessage?.(msg);
      }
    }
  };

  private _handleDisconnect = (): void => {
    this.connectionState = 'disconnected';
    this.characteristic = null;
    console.log('[BLE MIDI] Disconnected: ' + (this.device?.name || this.device?.id || '?'));
    this.onDisconnect?.();
  };

  private _parseBlePacket(dv: DataView): Uint8Array[] {
    const messages: Uint8Array[] = [];
    let i = 1; // skip header byte
    while (i < dv.byteLength) {
      // Timestamp byte (bit 7 set)
      if (dv.getUint8(i) & 0x80) { i++; }
      const start = i;
      // Read MIDI data bytes (bit 7 clear), but include status bytes (bit 7 set) that are MIDI status
      while (i < dv.byteLength) {
        const b = dv.getUint8(i);
        if (b === 0xF7) { i++; break; } // SysEx end — include it
        if (b & 0x80) {
          // Could be a new timestamp or a MIDI status byte
          // If followed by another byte with bit 7 clear, it's a MIDI status
          // If it's a timestamp (part of packed messages), next byte would also have bit 7 set or be MIDI
          // Heuristic: if we haven't read any data yet, treat as MIDI status
          if (i === start) {
            // MIDI status byte — include and continue
            i++;
            continue;
          }
          // Otherwise it's probably a new timestamp — stop here
          break;
        }
        i++;
      }
      if (i > start) {
        messages.push(new Uint8Array(dv.buffer.slice(dv.byteOffset + start, dv.byteOffset + i)));
      }
    }
    return messages;
  }

  async sendSysEx(sysexBytes: Uint8Array): Promise<void> {
    if (!this.characteristic || this.connectionState !== 'connected') return;
    const payloadMax = this.mtu - 2;
    let offset = 0;
    const now = Date.now();
    const tsHigh = ((now >> 7) & 0x3F) | 0x80;
    const tsLow = (now & 0x7F) | 0x80;

    while (offset < sysexBytes.length) {
      const remaining = sysexBytes.length - offset;
      const chunkSize = Math.min(remaining, payloadMax);
      const pkt = new Uint8Array(chunkSize + 2);
      pkt[0] = tsHigh;
      pkt[1] = tsLow;
      pkt.set(sysexBytes.subarray(offset, offset + chunkSize), 2);
      offset += chunkSize;
      try {
        await this.characteristic.writeValueWithoutResponse(pkt);
      } catch (_) { return; }
    }
  }

  async sendMidi(midiBytes: Uint8Array): Promise<void> {
    if (!this.characteristic || this.connectionState !== 'connected') return;
    const now = Date.now();
    const pkt = new Uint8Array(midiBytes.length + 2);
    pkt[0] = ((now >> 7) & 0x3F) | 0x80;
    pkt[1] = (now & 0x7F) | 0x80;
    pkt.set(midiBytes, 2);
    try {
      await this.characteristic.writeValueWithoutResponse(pkt);
    } catch (_) { /* ignore */ }
  }

  async reconnect(): Promise<boolean> {
    if (this.device && this.device.gatt) {
      try {
        this.connectionState = 'connecting';
        return await this._connectGatt();
      } catch (_) {
        this.connectionState = 'disconnected';
        return false;
      }
    }
    return false;
  }

  disconnect(): void {
    if (this.characteristic) {
      try { this.characteristic.removeEventListener('characteristicvaluechanged', this._handleNotification); } catch (_) {}
      this.characteristic = null;
    }
    if (this.device) {
      try { this.device.removeEventListener('gattserverdisconnected', this._handleDisconnect); } catch (_) {}
      if (this.device.gatt?.connected) {
        try { this.device.gatt.disconnect(); } catch (_) {}
      }
    }
    this.connectionState = 'disconnected';
  }
}

// ---------------------------------------------------------------------------
// RoliblockDevice — one per physical Roli Lightpad Block
// ---------------------------------------------------------------------------
let _nextDeviceNum = 1;

export class RoliblockDevice {
  id: string;
  label: string;
  enabled = false;
  midiInputId: string | null = null;
  midiOutputId: string | null = null;
  deckAssignment: DeckAssignment = 'shared';
  crossfaderEnabled = true;
  ledSettings: LedSettings;

  // Internal per-device state
  connectionType: 'usb' | 'ble' = 'usb';
  bleConnection: BleMidiConnection | null = null;
  bleDeviceId: string | null = null;
  midiInput: MIDIInput | null = null;
  handshakeDone = false;
  packetCounter = 0;
  prevLedData = new Uint8Array(LED_BYTE_COUNT);
  ledSendFailCount = 0;
  lastLedSend = 0;
  ledLogCount = 0;
  mpeMouseX = 0.5;
  mpeMouseY = 0.5;
  sysexBuf: number[] = [];
  _onMouse: RoliblockMouseCallback | null = null;
  _onCrossfader: RoliblockCrossfaderCallback | null = null;
  scratchCanvas: HTMLCanvasElement | null = null;
  scratchCtx: CanvasRenderingContext2D | null = null;

  constructor(id?: string) {
    this.id = id || 'roli-' + Date.now() + '-' + Math.random().toString(36).slice(2, 6);
    this.label = 'Roli ' + _nextDeviceNum++;
    this.ledSettings = defaultLedSettings();
  }

  get state(): RoliblockDeviceState {
    return {
      id: this.id,
      label: this.label,
      enabled: this.enabled,
      midiInputId: this.midiInputId,
      midiOutputId: this.midiOutputId,
      deckAssignment: this.deckAssignment,
      crossfaderEnabled: this.crossfaderEnabled,
      ledSettings: { ...this.ledSettings }
    };
  }

  // -- SysEx send --
  private sendSysEx(payload: number[]): void {
    if (this.connectionType === 'ble' && this.bleConnection) {
      const sysex = buildBlockSysEx(DEVICE_INDEX, payload);
      this.bleConnection.sendSysEx(sysex).catch(() => { this.ledSendFailCount++; });
      return;
    }
    const ma = roliblockManager.midiAccess;
    if (!ma || !this.midiOutputId) return;
    const out = ma.outputs.get(this.midiOutputId);
    if (!out) return;
    try { out.send(buildBlockSysEx(DEVICE_INDEX, payload)); } catch (_) { this.ledSendFailCount++; }
  }

  // -- Handshake --
  doHandshake(): void {
    if (this.connectionType === 'ble') {
      if (!this.bleConnection || this.bleConnection.connectionState !== 'connected') return;
    } else {
      if (!roliblockManager.midiAccess || !this.midiOutputId) return;
    }
    const delay = this.connectionType === 'ble' ? 250 : 100;
    this.sendSysEx([0x01, 0x02, 0x00]);
    this.sendSysEx([0x01, 0x00, 0x00]);
    setTimeout(() => {
      this.sendSysEx([0x01, 0x00, 0x00]);
      this.sendSysEx([0x01, 0x03, 0x00]);
      this.sendSysEx([0x10, 0x02]);
      setTimeout(() => {
        this.sendSysEx([0x01, 0x03, 0x00]);
        this.sendSysEx(parseDump(BITMAP_LED_DUMP_1));
        this.sendSysEx(parseDump(BITMAP_LED_DUMP_2));
        setTimeout(() => {
          this.sendSysEx([0x01, 0x05, 0x00]);
          this.handshakeDone = true;
          this.packetCounter = 1;
          this.prevLedData = new Uint8Array(LED_BYTE_COUNT);
          console.log('[Roliblock:' + this.label + '] Handshake complete (' + this.connectionType + ')');
        }, delay);
      }, delay);
    }, delay);
  }

  // -- DataChangeList builder (per-device packet counter) --
  private buildDataChangeMessages(newData: Uint8Array, oldData: Uint8Array): number[][] {
    const b = new Packed7BitBuilder();
    const queued: number[][] = [];
    let pktIdx = this.packetCounter;

    function initPacket(): void {
      b._data = []; b._written = 0; b._bits = 0;
      b.writeBits(0x02, 7);
      b.writeBits(pktIdx & PACKET_COUNTER_MAX, 16);
    }
    function flushPacket(endOfChanges: boolean): void {
      const fin = b.clone();
      fin.writeBits(endOfChanges ? 1 : 0, 3);
      queued.push(fin.getData());
      pktIdx++;
      if (!endOfChanges) initPacket();
    }
    let currentOffset = 0;
    function appendSkipToOffset(): void { skipBytes(LED_DATA_OFFSET + currentOffset); }
    function skipBytes(count: number): void {
      while (count > 0) {
        if (b.size() >= MAX_PACKET_SIZE - 3) { flushPacket(false); appendSkipToOffset(); }
        if (count > 15) {
          const chunk = Math.min(255, count);
          b.writeBits(3, 3); b.writeBits(chunk, 8);
          count -= chunk;
        } else {
          b.writeBits(2, 3); b.writeBits(count, 4);
          count = 0;
        }
      }
    }

    initPacket();
    skipBytes(LED_DATA_OFFSET);

    let i = 0;
    while (i < LED_BYTE_COUNT) {
      if (newData[i] === oldData[i]) { i++; currentOffset = i; continue; }
      let runEnd = i;
      while (runEnd < LED_BYTE_COUNT && newData[runEnd] !== oldData[runEnd]) runEnd++;
      const seq = newData.subarray(i, runEnd);
      if (i > currentOffset) skipBytes(i - currentOffset);
      let written = 0;
      while (written < seq.length) {
        if (MAX_PACKET_SIZE - b.size() < 4) {
          currentOffset = i + written;
          flushPacket(false);
          appendSkipToOffset();
        }
        let chunk = Math.min(seq.length - written, Math.floor(((MAX_PACKET_SIZE - b.size()) - 1) * 7 / 9));
        if (chunk < 1) chunk = 1;
        b.writeBits(4, 3);
        for (let j = 0; j < chunk; j++) {
          b.writeBits(seq[written + j], 8);
          b.writeBits((j < chunk - 1) ? 1 : 0, 1);
        }
        written += chunk;
      }
      i = runEnd;
      currentOffset = i;
    }

    flushPacket(true);
    this.packetCounter = pktIdx;
    return queued;
  }

  // -- LED send --
  sendLedToDevice(data: Uint8ClampedArray): void {
    if (!this.enabled) return;
    if (this.connectionType === 'ble') {
      if (!this.bleConnection || this.bleConnection.connectionState !== 'connected') return;
    } else {
      const ma = roliblockManager.midiAccess;
      if (!ma || !this.midiOutputId) {
        if (this.ledLogCount++ < 3) console.log('[Roliblock:' + this.label + '] sendLed: no MIDI');
        return;
      }
    }
    if (!this.handshakeDone) {
      if (this.ledLogCount++ < 3) console.log('[Roliblock:' + this.label + '] sendLed: no handshake');
      return;
    }
    if (this.ledSendFailCount > 20) return;

    const newLed = new Uint8Array(LED_BYTE_COUNT);
    for (let i = 0; i < LED_PIXEL_COUNT; i++) {
      const r = data[i * 4], g = data[i * 4 + 1], b = data[i * 4 + 2], a = data[i * 4 + 3];
      const c16 = rgbaToBgr565(r, g, b, a);
      newLed[i * 2] = c16 & 0xff;
      newLed[i * 2 + 1] = (c16 >> 8) & 0xff;
    }

    const messages = this.buildDataChangeMessages(newLed, this.prevLedData);
    if (this.connectionType === 'ble' && this.bleConnection) {
      for (const msg of messages) {
        const sysex = buildBlockSysEx(DEVICE_INDEX, msg);
        this.bleConnection.sendSysEx(sysex).catch(() => { this.ledSendFailCount++; });
      }
    } else {
      const ma = roliblockManager.midiAccess;
      const out = ma?.outputs.get(this.midiOutputId!);
      if (!out) return;
      for (const msg of messages) {
        try { out.send(buildBlockSysEx(DEVICE_INDEX, msg)); } catch (_) { this.ledSendFailCount++; return; }
      }
    }
    this.ledSendFailCount = 0;
    this.prevLedData = newLed;
  }

  // -- Canvas sampling --
  sampleCanvasToRgba(canvas: HTMLCanvasElement): Uint8ClampedArray {
    if (!this.scratchCanvas) {
      this.scratchCanvas = document.createElement('canvas');
      this.scratchCanvas.width = GRID_COLS;
      this.scratchCanvas.height = GRID_ROWS;
      this.scratchCtx = this.scratchCanvas.getContext('2d');
      if (this.scratchCtx) {
        this.scratchCtx.imageSmoothingEnabled = true;
        this.scratchCtx.imageSmoothingQuality = 'high';
      }
    }
    if (!this.scratchCtx) return new Uint8ClampedArray(LED_PIXEL_COUNT * 4);
    this.scratchCtx.clearRect(0, 0, GRID_COLS, GRID_ROWS);
    this.scratchCtx.drawImage(canvas, 0, 0, canvas.width, canvas.height, 0, 0, GRID_COLS, GRID_ROWS);
    return this.scratchCtx.getImageData(0, 0, GRID_COLS, GRID_ROWS).data;
  }

  // -- LED pixel processing (per-device settings) --
  processLedPixels(data: Uint8ClampedArray): Uint8ClampedArray {
    const s = this.ledSettings;
    const isNeutral = s.contrast === 1.0 && s.brightness === 0.0 && s.saturation === 1.0
      && s.gamma === 1.0 && !s.grayscale && !s.invert && s.posterize === 0
      && s.channelIsolation === 'all';
    if (isNeutral) return data;

    const out = new Uint8ClampedArray(data.length);
    const invGamma = s.gamma !== 1.0 ? 1.0 / s.gamma : 1.0;
    for (let i = 0; i < LED_PIXEL_COUNT; i++) {
      const off = i * 4;
      let r: number = data[off];
      let g: number = data[off + 1];
      let b: number = data[off + 2];

      // brightness
      r += s.brightness * 255;
      g += s.brightness * 255;
      b += s.brightness * 255;
      // contrast
      r = (r - 128) * s.contrast + 128;
      g = (g - 128) * s.contrast + 128;
      b = (b - 128) * s.contrast + 128;
      // saturation
      const luma = 0.299 * r + 0.587 * g + 0.114 * b;
      r = luma + (r - luma) * s.saturation;
      g = luma + (g - luma) * s.saturation;
      b = luma + (b - luma) * s.saturation;
      // gamma
      if (s.gamma !== 1.0) {
        r = 255 * Math.pow(Math.max(0, Math.min(1, r / 255)), invGamma);
        g = 255 * Math.pow(Math.max(0, Math.min(1, g / 255)), invGamma);
        b = 255 * Math.pow(Math.max(0, Math.min(1, b / 255)), invGamma);
      }
      // channel isolation
      if (s.channelIsolation === 'r') { g = 0; b = 0; }
      else if (s.channelIsolation === 'g') { r = 0; b = 0; }
      else if (s.channelIsolation === 'b') { r = 0; g = 0; }
      // grayscale
      if (s.grayscale) {
        const gray = 0.299 * r + 0.587 * g + 0.114 * b;
        r = gray; g = gray; b = gray;
      }
      // posterize
      if (s.posterize >= 2) {
        const lv = s.posterize - 1;
        r = Math.round(Math.max(0, Math.min(255, r)) / 255 * lv) / lv * 255;
        g = Math.round(Math.max(0, Math.min(255, g)) / 255 * lv) / lv * 255;
        b = Math.round(Math.max(0, Math.min(255, b)) / 255 * lv) / lv * 255;
      }
      // invert
      if (s.invert) { r = 255 - r; g = 255 - g; b = 255 - b; }

      out[off] = Math.max(0, Math.min(255, Math.round(r)));
      out[off + 1] = Math.max(0, Math.min(255, Math.round(g)));
      out[off + 2] = Math.max(0, Math.min(255, Math.round(b)));
      out[off + 3] = data[off + 3];
    }
    return out;
  }

  // -- Rate-limited sample + process + send --
  sampleAndSendLed(srcCanvas: HTMLCanvasElement, srcCanvasB?: HTMLCanvasElement, crossfader?: number): void {
    if (!this.enabled) return;
    if (this.connectionType === 'ble') {
      if (!this.bleConnection || this.bleConnection.connectionState !== 'connected') return;
    } else {
      if (!roliblockManager.midiAccess || !this.midiOutputId) return;
    }
    const now = Date.now();
    const interval = this.connectionType === 'ble' ? BLE_LED_SEND_INTERVAL_MS : LED_SEND_INTERVAL_MS;
    if (now - this.lastLedSend < interval) return;
    this.lastLedSend = now;

    let data: Uint8ClampedArray;
    if (srcCanvasB !== undefined && crossfader !== undefined) {
      const dataA = this.sampleCanvasToRgba(srcCanvas);
      const dataB = this.sampleCanvasToRgba(srcCanvasB);
      data = blendRgba(dataA, dataB, crossfader);
    } else {
      data = this.sampleCanvasToRgba(srcCanvas);
    }
    this.sendLedToDevice(this.processLedPixels(data));
  }

  // -- Touch SysEx parser --
  private parseRoliTouchSysex(data: Uint8Array): void {
    if (data.length < 8) return;
    const msgData = data.subarray(5, data.length - 2);
    let bitPos = 39;
    while (bitPos + 7 <= msgData.length * 7) {
      const msgType = read7BitBits(msgData, bitPos, 7);
      bitPos += 7;
      if (msgType === 0x11) {
        bitPos += 5 + 5;
        const x = read7BitBits(msgData, bitPos, 12); bitPos += 12;
        const y = read7BitBits(msgData, bitPos, 12); bitPos += 12 + 8;
        this.mpeMouseX = x / 4095;
        this.mpeMouseY = y / 4095;
        pushMonitorEntry({ type: 'midi', device: this.label, text: 'Touch ' + this.mpeMouseX.toFixed(2) + ' ' + this.mpeMouseY.toFixed(2) });
        this._onMouse?.(this.mpeMouseX, this.mpeMouseY);
        return;
      }
      if (msgType === 0x13 || msgType === 0x15) {
        bitPos += 5 + 5;
        const x = read7BitBits(msgData, bitPos, 12); bitPos += 12;
        const y = read7BitBits(msgData, bitPos, 12); bitPos += 8;
        if (msgType === 0x13) bitPos += 24;
        this.mpeMouseX = x / 4095;
        this.mpeMouseY = y / 4095;
        pushMonitorEntry({ type: 'midi', device: this.label, text: 'Touch ' + this.mpeMouseX.toFixed(2) + ' ' + this.mpeMouseY.toFixed(2) });
        this._onMouse?.(this.mpeMouseX, this.mpeMouseY);
        return;
      }
      break;
    }
  }

  // -- MIDI message handler (bound to this device's input) --
  onMidiMessage = (event: MIDIMessageEvent): void => {
    const d = event.data;
    if (!d || d.length < 1) return;

    if (d[0] === 0xf0) {
      this.sysexBuf = Array.from(d);
    } else if (this.sysexBuf.length > 0) {
      this.sysexBuf.push(...Array.from(d));
    }
    if (this.sysexBuf.length > 0 && this.sysexBuf[this.sysexBuf.length - 1] === 0xf7) {
      const full = new Uint8Array(this.sysexBuf);
      if (full.length >= 8 && full[1] === 0x00 && full[2] === 0x21 && full[3] === 0x10 && full[4] === 0x77) {
        this.parseRoliTouchSysex(full);
      }
      this.sysexBuf = [];
    }

    if (d[0] !== 0xf0 && this.sysexBuf.length === 0) {
      const cmd = d[0] >> 4;
      if (cmd === 0x09 && d.length >= 3 && d[2] > 0) {
        this._onMouse?.(this.mpeMouseX, this.mpeMouseY);
      } else if (cmd === 0x0b && d.length >= 3) {
        const cc = d[1], val = d[2];
        if (cc === 74) {
          this.mpeMouseY = val / 127;
          this._onMouse?.(this.mpeMouseX, this.mpeMouseY);
        } else if (cc === CROSSFADER_CC && this.crossfaderEnabled) {
          this._onCrossfader?.(val / 127);
        }
      } else if (cmd === 0x0e && d.length >= 3) {
        this.mpeMouseX = (d[2] * 128 + d[1]) / 16383;
        this._onMouse?.(this.mpeMouseX, this.mpeMouseY);
      } else if (cmd === 0x0d && d.length >= 2) {
        this._onMouse?.(this.mpeMouseX, this.mpeMouseY);
      }
    }
  };

  // -- Connect --
  connectInput(inputId: string): void {
    const ma = roliblockManager.midiAccess;
    if (!ma) return;
    if (this.midiInput) { this.midiInput.onmidimessage = null; this.midiInput = null; }
    const inp = ma.inputs.get(inputId);
    if (inp) {
      this.midiInput = inp;
      this.midiInput.onmidimessage = this.onMidiMessage;
      this.midiInputId = inputId;
      console.log('[Roliblock:' + this.label + '] Connected input: ' + inp.name);
    }
  }

  connectOutput(outputId: string): void {
    const ma = roliblockManager.midiAccess;
    if (!ma) return;
    if (this.midiOutputId !== outputId) {
      this.handshakeDone = false;
      this.ledSendFailCount = 0;
    }
    if (ma.outputs.has(outputId)) {
      this.midiOutputId = outputId;
      console.log('[Roliblock:' + this.label + '] Connected output, handshaking...');
      this.doHandshake();
    }
  }

  // -- BLE connect --
  async connectBle(): Promise<boolean> {
    const ble = new BleMidiConnection();
    ble.onMidiMessage = (data: Uint8Array) => {
      this.onMidiMessage({ data } as MIDIMessageEvent);
    };
    ble.onDisconnect = () => {
      this.enabled = false;
      this.handshakeDone = false;
      this.bleConnection = null;
      console.log('[Roliblock:' + this.label + '] BLE disconnected');
    };
    const ok = await ble.pair();
    if (!ok) return false;
    this.bleConnection = ble;
    this.bleDeviceId = ble.device?.id || null;
    this.connectionType = 'ble';
    this.midiInput = null;
    this.midiOutputId = null;
    this.midiInputId = null;
    console.log('[Roliblock:' + this.label + '] BLE connected: ' + (ble.device?.name || ble.device?.id));
    this.doHandshake();
    return true;
  }

  enable(): void {
    this.enabled = true;
    console.log('[Roliblock:' + this.label + '] Enabled. handshake=' + this.handshakeDone + ' type=' + this.connectionType);
    if (this.connectionType === 'ble') {
      if (this.bleConnection && this.bleConnection.connectionState === 'connected' && !this.handshakeDone) this.doHandshake();
    } else {
      if (this.midiOutputId && !this.handshakeDone) this.doHandshake();
    }
  }

  disable(): void {
    this.enabled = false;
  }

  disconnectBle(): void {
    if (this.bleConnection) {
      this.bleConnection.disconnect();
      this.bleConnection = null;
    }
    this.connectionType = 'usb';
    this.bleDeviceId = null;
    this.handshakeDone = false;
  }

  isReady(): boolean {
    if (this.connectionType === 'ble') {
      return this.enabled && !!this.bleConnection
        && this.bleConnection.connectionState === 'connected' && this.handshakeDone;
    }
    return this.enabled && !!this.midiInput && !!this.midiOutputId && this.handshakeDone;
  }

  hasTouchInput(): boolean {
    if (this.connectionType === 'ble') {
      return !!this.bleConnection && this.bleConnection.connectionState === 'connected';
    }
    return !!this.midiInput;
  }

  hasLedOutput(): boolean {
    if (this.connectionType === 'ble') {
      return !!this.bleConnection && this.bleConnection.connectionState === 'connected';
    }
    return !!this.midiOutputId;
  }
}

// ---------------------------------------------------------------------------
// Stretched LED: sample 30x15 from output canvas, split to two devices
// ---------------------------------------------------------------------------
let _stretchedCanvas: HTMLCanvasElement | null = null;
let _stretchedCtx: CanvasRenderingContext2D | null = null;

export function sendStretchedLed(devices: RoliblockDevice[], canvas: HTMLCanvasElement): void {
  if (devices.length < 2) return;
  if (!_stretchedCanvas) {
    _stretchedCanvas = document.createElement('canvas');
    _stretchedCanvas.width = GRID_COLS * 2;
    _stretchedCanvas.height = GRID_ROWS;
    _stretchedCtx = _stretchedCanvas.getContext('2d');
    if (_stretchedCtx) {
      _stretchedCtx.imageSmoothingEnabled = true;
      _stretchedCtx.imageSmoothingQuality = 'high';
    }
  }
  if (!_stretchedCtx) return;
  _stretchedCtx.clearRect(0, 0, GRID_COLS * 2, GRID_ROWS);
  _stretchedCtx.drawImage(canvas, 0, 0, canvas.width, canvas.height, 0, 0, GRID_COLS * 2, GRID_ROWS);
  const full = _stretchedCtx.getImageData(0, 0, GRID_COLS * 2, GRID_ROWS).data;

  // Left half → device 0, Right half → device 1
  for (let d = 0; d < 2 && d < devices.length; d++) {
    const dev = devices[d];
    if (!dev.enabled) continue;
    const now = Date.now();
    const interval = dev.connectionType === 'ble' ? BLE_LED_SEND_INTERVAL_MS : LED_SEND_INTERVAL_MS;
    if (now - dev.lastLedSend < interval) continue;
    dev.lastLedSend = now;

    const half = new Uint8ClampedArray(LED_PIXEL_COUNT * 4);
    const xOff = d * GRID_COLS;
    for (let row = 0; row < GRID_ROWS; row++) {
      for (let col = 0; col < GRID_COLS; col++) {
        const srcIdx = (row * GRID_COLS * 2 + xOff + col) * 4;
        const dstIdx = (row * GRID_COLS + col) * 4;
        half[dstIdx] = full[srcIdx];
        half[dstIdx + 1] = full[srcIdx + 1];
        half[dstIdx + 2] = full[srcIdx + 2];
        half[dstIdx + 3] = full[srcIdx + 3];
      }
    }
    dev.sendLedToDevice(dev.processLedPixels(half));
  }
}

// ---------------------------------------------------------------------------
// roliblockManager — global manager + backward-compatible roliblockEngine alias
// ---------------------------------------------------------------------------
const STORAGE_KEY = 'macroverse-roliblocks';

export const roliblockManager = {
  midiAccess: null as MIDIAccess | null,
  devices: new Map<string, RoliblockDevice>(),
  inputs: [] as { id: string; name: string }[],
  outputs: [] as { id: string; name: string }[],
  ledDisplayMode: 'sharedOutput' as LedDisplayMode,

  // -- Linked mode: remap per-device touch to unified 30x15 coordinate space --
  getLinkedMouse(dev: RoliblockDevice): { x: number; y: number } {
    const devs = this.getDevices().filter(d => d.enabled);
    const idx = devs.indexOf(dev);
    if (idx === 0) return { x: dev.mpeMouseX * 0.5, y: dev.mpeMouseY };
    if (idx === 1) return { x: 0.5 + dev.mpeMouseX * 0.5, y: dev.mpeMouseY };
    return { x: dev.mpeMouseX, y: dev.mpeMouseY };
  },

  // -- Device management --
  addDevice(): RoliblockDevice {
    const dev = new RoliblockDevice();
    this.devices.set(dev.id, dev);
    this.saveAll();
    return dev;
  },

  removeDevice(id: string): void {
    const dev = this.devices.get(id);
    if (dev) {
      dev.disable();
      if (dev.bleConnection) dev.bleConnection.disconnect();
      if (dev.midiInput) { dev.midiInput.onmidimessage = null; }
      this.devices.delete(id);
      this.saveAll();
    }
  },

  getDevice(id: string): RoliblockDevice | undefined {
    return this.devices.get(id);
  },

  getDevices(): RoliblockDevice[] {
    return Array.from(this.devices.values());
  },

  getActiveDeviceCount(): number {
    let n = 0;
    for (const d of this.devices.values()) if (d.enabled) n++;
    return n;
  },

  getDeviceForDeck(deck: 'A' | 'B'): RoliblockDevice | undefined {
    const target = deck === 'A' ? 'deckA' : 'deckB';
    // Explicit assignment first
    for (const d of this.devices.values()) {
      if (d.enabled && d.deckAssignment === target) return d;
    }
    // Auto-assignment: first enabled device → A, second → B
    const enabled = this.getDevices().filter(d => d.enabled && d.deckAssignment === 'auto');
    if (deck === 'A' && enabled.length >= 1) return enabled[0];
    if (deck === 'B' && enabled.length >= 2) return enabled[1];
    return undefined;
  },

  // -- MIDI access --
  async requestAccess(): Promise<boolean> {
    if (!navigator.requestMIDIAccess) return false;
    try {
      this.midiAccess = await navigator.requestMIDIAccess({ sysex: true });
      this.refreshDeviceList();
      this.midiAccess.onstatechange = () => {
        this.refreshDeviceList();
        // Handle disconnections
        for (const dev of this.devices.values()) {
          if (dev.midiInput && dev.midiInput.state === 'disconnected') {
            dev.midiInput = null;
            dev.midiInputId = null;
          }
          if (dev.midiOutputId && this.midiAccess && !this.midiAccess.outputs.has(dev.midiOutputId)) {
            dev.midiOutputId = null;
            dev.handshakeDone = false;
          }
        }
      };
      return true;
    } catch (_) { return false; }
  },

  refreshDeviceList(): void {
    this.inputs = [];
    this.outputs = [];
    if (this.midiAccess) {
      this.midiAccess.inputs.forEach((inp) => this.inputs.push({ id: inp.id, name: inp.name || 'Unnamed' }));
      this.midiAccess.outputs.forEach((out) => this.outputs.push({ id: out.id, name: out.name || 'Unnamed' }));
    }
    if (this.inputs.length > 0 || this.outputs.length > 0) {
      console.log('[Roliblock] MIDI devices: ' + this.inputs.length + ' in, ' + this.outputs.length + ' out');
    }
  },

  autoDetectDevices(): void {
    if (!this.midiAccess) return;
    const roliInputs = this.inputs.filter(i => isRoliblockLike(i.name));
    const roliOutputs = this.outputs.filter(o => isRoliblockLike(o.name));

    // Find unassigned Roli pairs
    const usedInputs = new Set<string>();
    const usedOutputs = new Set<string>();
    for (const dev of this.devices.values()) {
      if (dev.midiInputId) usedInputs.add(dev.midiInputId);
      if (dev.midiOutputId) usedOutputs.add(dev.midiOutputId);
    }

    const hadDevices = this.devices.size > 0;

    // Create devices for new Roli pairs
    for (let idx = 0; idx < Math.max(roliInputs.length, roliOutputs.length); idx++) {
      const inp = roliInputs[idx];
      const outp = roliOutputs[idx];
      if ((!inp || usedInputs.has(inp.id)) && (!outp || usedOutputs.has(outp.id))) continue;

      const dev = this.addDevice();
      dev.deckAssignment = 'shared';
      if (inp && !usedInputs.has(inp.id)) {
        dev.connectInput(inp.id);
        usedInputs.add(inp.id);
      }
      if (outp && !usedOutputs.has(outp.id)) {
        dev.connectOutput(outp.id);
        usedOutputs.add(outp.id);
      }
      if (dev.midiInput || dev.midiOutputId) dev.enable();
    }

    // Auto-rig defaults: detected blocks mirror the master output unless changed manually.
    if (!hadDevices) this.ledDisplayMode = 'sharedOutput';
    for (const dev of this.getDevices()) {
      if (dev.deckAssignment === 'auto') dev.deckAssignment = 'shared';
    }
    this.saveAll();
  },

  // -- Persistence --
  saveAll(): void {
    try {
      const data = {
        ledDisplayMode: this.ledDisplayMode,
        devices: this.getDevices().map(d => ({
          id: d.id, label: d.label,
          midiInputId: d.midiInputId, midiOutputId: d.midiOutputId,
          deckAssignment: d.deckAssignment, crossfaderEnabled: d.crossfaderEnabled,
          ledSettings: d.ledSettings,
          connectionType: d.connectionType,
          bleDeviceId: d.bleDeviceId
        }))
      };
      localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
    } catch (_) {}
  },

  loadAll(): void {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return;
      const data = JSON.parse(raw);
      if (data.ledDisplayMode === 'sharedOutput' || data.ledDisplayMode === 'independent' ||
          data.ledDisplayMode === 'stretched' || data.ledDisplayMode === 'linked') {
        this.ledDisplayMode = data.ledDisplayMode;
      }
      if (Array.isArray(data.devices)) {
        for (const saved of data.devices) {
          const dev = new RoliblockDevice(saved.id);
          dev.label = saved.label || dev.label;
          dev.deckAssignment = saved.deckAssignment || 'auto';
          dev.crossfaderEnabled = saved.crossfaderEnabled !== false;
          if (saved.ledSettings) {
            dev.ledSettings = { ...defaultLedSettings(), ...saved.ledSettings };
          }
          // Don't auto-connect yet — wait for requestAccess or BLE re-pair
          dev.midiInputId = saved.midiInputId;
          dev.midiOutputId = saved.midiOutputId;
          dev.connectionType = saved.connectionType || 'usb';
          dev.bleDeviceId = saved.bleDeviceId || null;
          this.devices.set(dev.id, dev);
        }
      }
    } catch (_) {}
  },

  reconnectSaved(): void {
    if (!this.midiAccess) return;
    for (const dev of this.devices.values()) {
      // Skip BLE devices — they need user-initiated re-pairing
      if (dev.connectionType === 'ble') {
        if (!dev.bleConnection) {
          console.log('[Roliblock:' + dev.label + '] BLE device saved — click Pair BLE to reconnect');
        }
        continue;
      }
      if (dev.midiInputId && !dev.midiInput) {
        const inp = this.midiAccess.inputs.get(dev.midiInputId);
        if (inp) dev.connectInput(dev.midiInputId);
      }
      if (dev.midiOutputId && !dev.handshakeDone) {
        if (this.midiAccess.outputs.has(dev.midiOutputId)) dev.connectOutput(dev.midiOutputId);
      }
      if (dev.midiInput || dev.midiOutputId) dev.enable();
    }
  },

  // -----------------------------------------------------------------------
  // Backward compatibility — roliblockEngine-compatible API
  // Delegates to first device for single-device usage
  // -----------------------------------------------------------------------
  get state(): RoliblockState {
    const first = this.getDevices()[0];
    return {
      enabled: first?.enabled || false,
      mode: 'preview' as RoliblockMode,
      crossfaderEnabled: first?.crossfaderEnabled || true,
      midiInputId: first?.midiInputId || null,
      midiOutputId: first?.midiOutputId || null,
      inputs: this.inputs,
      outputs: this.outputs
    };
  },

  setMode(_mode: RoliblockMode): void {
    // Mode is now managed by syncRoliblockFromView per-device
  },

  setCrossfaderEnabled(enabled: boolean): void {
    const first = this.getDevices()[0];
    if (first) { first.crossfaderEnabled = enabled; this.saveAll(); }
  },

  setOnMouse(cb: RoliblockMouseCallback | null): void {
    const first = this.getDevices()[0];
    if (first) first._onMouse = cb;
  },

  setOnCrossfader(cb: RoliblockCrossfaderCallback | null): void {
    const first = this.getDevices()[0];
    if (first) first._onCrossfader = cb;
  },

  get ledSettings(): LedSettings {
    const first = this.getDevices()[0];
    return first ? { ...first.ledSettings } : defaultLedSettings();
  },

  setLedContrast(v: number): void { const d = this.getDevices()[0]; if (d) { d.ledSettings.contrast = v; this.saveAll(); } },
  setLedBrightness(v: number): void { const d = this.getDevices()[0]; if (d) { d.ledSettings.brightness = v; this.saveAll(); } },
  setLedSaturation(v: number): void { const d = this.getDevices()[0]; if (d) { d.ledSettings.saturation = v; this.saveAll(); } },
  setLedGamma(v: number): void { const d = this.getDevices()[0]; if (d) { d.ledSettings.gamma = v; this.saveAll(); } },

  isRoliblockLike,

  enable(): void {
    const first = this.getDevices()[0];
    if (first) first.enable();
  },

  disable(): void {
    const first = this.getDevices()[0];
    if (first) first.disable();
  },

  isReady(): boolean {
    return this.getDevices().some(d => d.isReady());
  },

  // Legacy single-device connect methods
  connectInput(inputId: string): void {
    const first = this.getDevices()[0];
    if (first) first.connectInput(inputId);
  },

  connectOutput(outputId: string): void {
    const first = this.getDevices()[0];
    if (first) first.connectOutput(outputId);
  },

  autoMapRoliblock(): boolean {
    this.autoDetectDevices();
    return this.devices.size > 0;
  }
};

// Load saved config on module init
roliblockManager.loadAll();

// Backward-compatible export alias
export const roliblockEngine = roliblockManager;

// Legacy re-exports for direct callers
export function sampleCanvasToRgba(canvas: HTMLCanvasElement): Uint8ClampedArray {
  const dev = roliblockManager.getDevices()[0];
  if (dev) return dev.sampleCanvasToRgba(canvas);
  return new Uint8ClampedArray(LED_PIXEL_COUNT * 4);
}

export function sendLedToDevice(data: Uint8ClampedArray): void {
  const dev = roliblockManager.getDevices()[0];
  if (dev) dev.sendLedToDevice(data);
}

export function sampleAndSendLed(
  srcCanvas: HTMLCanvasElement,
  srcCanvasB?: HTMLCanvasElement,
  crossfader?: number
): void {
  const dev = roliblockManager.getDevices()[0];
  if (dev) dev.sampleAndSendLed(srcCanvas, srcCanvasB, crossfader);
}
