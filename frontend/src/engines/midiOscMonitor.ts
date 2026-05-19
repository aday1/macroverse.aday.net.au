/**
 * In-memory store for last N MIDI and OSC messages, used by the collapsible monitor panel.
 */

export interface MonitorEntry {
  type: 'midi' | 'osc';
  device?: string;
  text: string;
  time: number;
}

const MAX_ENTRIES = 25;
const entries: MonitorEntry[] = [];
let onUpdate: (() => void) | null = null;

export function pushMonitorEntry(entry: Omit<MonitorEntry, 'time'>): void {
  entries.unshift({ ...entry, time: Date.now() });
  if (entries.length > MAX_ENTRIES) entries.length = MAX_ENTRIES;
  onUpdate?.();
}

export function getMonitorEntries(): MonitorEntry[] {
  return entries.slice();
}

export function setMonitorUpdateCallback(cb: (() => void) | null): void {
  onUpdate = cb;
}
