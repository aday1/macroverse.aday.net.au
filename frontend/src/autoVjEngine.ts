import type { ExposeItem } from './panels/params.js';

export type AutoVjParamMode =
  | 'sine'
  | 'triangle'
  | 'saw'
  | 'pulse'
  | 'step'
  | 'multiply'
  | 'divide'
  | 'subtract'
  | 'add'
  | 'bounce'
  | 'random';

export type AutoVjParamModeDef = { value: AutoVjParamMode; label: string };

export const AUTO_VJ_PARAM_MODES: AutoVjParamModeDef[] = [
  { value: 'sine', label: 'Sine' },
  { value: 'triangle', label: 'Triangle' },
  { value: 'saw', label: 'Saw' },
  { value: 'pulse', label: 'Pulse' },
  { value: 'step', label: 'Step (beat)' },
  { value: 'multiply', label: 'Multiply' },
  { value: 'divide', label: 'Divide' },
  { value: 'subtract', label: 'Subtract' },
  { value: 'add', label: 'Add' },
  { value: 'bounce', label: 'Bounce' },
  { value: 'random', label: 'Random' },
];

export const AUTO_VJ_BEAT_CYCLES = [1, 2, 4, 8, 16] as const;
export const AUTO_VJ_BAR_BEATS = [2, 4, 8, 16] as const;

const RANDOMIZABLE_PARAM_MODES = AUTO_VJ_PARAM_MODES.filter((m) => m.value !== 'random').map((m) => m.value);

export interface AutoVjParamConfig {
  paramMode: AutoVjParamMode;
  beatsPerCycle: number;
  depth: number;
  barBeats: number;
  /** Pick new shaders on each bar when Auto VJ is on. */
  shaderSwap: boolean;
  /** Automate deck params (depth LFO) when Auto VJ is on. */
  paramMove: boolean;
}

export interface AutoVjParamState {
  activeRandomMode: AutoVjParamMode;
  stepNorms: Map<string, number>;
  lastStepBeat: number;
}

export function createAutoVjParamState(): AutoVjParamState {
  return {
    activeRandomMode: 'sine',
    stepNorms: new Map(),
    lastStepBeat: -1,
  };
}

export function isAutoVjParamMode(value: string): value is AutoVjParamMode {
  return AUTO_VJ_PARAM_MODES.some((m) => m.value === value);
}

export function autoVjParamModeLabel(mode: AutoVjParamMode): string {
  return AUTO_VJ_PARAM_MODES.find((m) => m.value === mode)?.label ?? mode;
}

export function pickRandomAutoVjParamMode(): AutoVjParamMode {
  return RANDOMIZABLE_PARAM_MODES[Math.floor(Math.random() * RANDOMIZABLE_PARAM_MODES.length)] ?? 'sine';
}

export function resolveAutoVjParamMode(config: AutoVjParamConfig, state: AutoVjParamState): AutoVjParamMode {
  return config.paramMode === 'random' ? state.activeRandomMode : config.paramMode;
}

/** Beat clock: integer beats plus fractional progress through current beat. */
export function autoVjBeatClock(beatCount: number, msSinceLastBeat: number, beatMs: number): number {
  if (beatMs <= 0) return beatCount;
  const frac = Math.max(0, Math.min(1, msSinceLastBeat / beatMs));
  return beatCount + frac;
}

function waveNorm(mode: AutoVjParamMode, phaseFrac: number, paramIndex: number): number {
  const p = ((phaseFrac % 1) + 1) % 1;
  const detune = paramIndex * 0.11;
  switch (mode) {
    case 'sine':
    case 'random':
      return 0.5 + 0.5 * Math.sin((p + detune) * Math.PI * 2);
    case 'triangle': {
      const t = (p + detune) % 1;
      return t < 0.5 ? t * 2 : 2 - t * 2;
    }
    case 'saw':
      return ((p + detune) % 1);
    case 'pulse':
      return ((p + detune) % 1) < 0.5 ? 0.08 : 0.92;
    case 'bounce':
      return Math.abs(Math.sin((p + detune) * Math.PI));
    case 'multiply':
      return p * p;
    case 'divide': {
      const x = 0.12 + p * 0.88;
      return Math.min(1, x / (0.35 + 0.65 * x));
    }
    case 'subtract':
      return 1 - p;
    case 'add':
      return p;
    case 'step':
      return 0.5;
    default:
      return 0.5 + 0.5 * Math.sin((p + detune) * Math.PI * 2);
  }
}

function clampParam(v: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, v));
}

function applyNormToParam(norm: number, mode: AutoVjParamMode, meta: ExposeItem, depth: number): number {
  const min = meta.min ?? 0;
  const max = meta.max ?? 1;
  const range = max - min;
  const center = min + range * 0.5;
  const n = Math.max(0, Math.min(1, norm));
  const d = Math.max(0.05, Math.min(1, depth));

  switch (mode) {
    case 'subtract':
      return clampParam(max - range * n * d, min, max);
    case 'add':
      return clampParam(min + range * n * d, min, max);
    default:
      return clampParam(center + (n - 0.5) * range * d, min, max);
  }
}

export function onAutoVjBeatTick(
  state: AutoVjParamState,
  beatCount: number,
  paramKeys: string[],
  config: AutoVjParamConfig
): void {
  if (config.paramMode === 'random' && beatCount > 0 && beatCount % config.barBeats === 0) {
    state.activeRandomMode = pickRandomAutoVjParamMode();
  }
  if (beatCount !== state.lastStepBeat) {
    state.lastStepBeat = beatCount;
    for (const key of paramKeys) {
      state.stepNorms.set(key, Math.random());
    }
  }
}

export function tickAutoVjDeckParams(opts: {
  config: AutoVjParamConfig;
  state: AutoVjParamState;
  beatClock: number;
  meta: ExposeItem[];
  valuesRef: Record<string, number | boolean>;
  fftMapped: Set<string>;
  skipFftMapped: boolean;
  deckOffset: number;
}): void {
  const mode = resolveAutoVjParamMode(opts.config, opts.state);
  const phaseFrac = opts.beatClock / Math.max(1, opts.config.beatsPerCycle);

  for (let i = 0; i < opts.meta.length; i++) {
    const m = opts.meta[i];
    if (m.type !== 'float' && m.type !== 'int') continue;
    if (opts.skipFftMapped && opts.fftMapped.has(m.name)) continue;

    let norm: number;
    if (mode === 'step') {
      norm = opts.state.stepNorms.get(m.name) ?? 0.5;
    } else {
      norm = waveNorm(mode, phaseFrac, i + opts.deckOffset * 3.7);
    }

    opts.valuesRef[m.name] = applyNormToParam(norm, mode, m, opts.config.depth);
  }
}

export function readAutoVjStorage(key: string, fallback: string): string {
  try {
    return localStorage.getItem(key) ?? fallback;
  } catch {
    return fallback;
  }
}

export function saveAutoVjStorage(key: string, value: string): void {
  try {
    localStorage.setItem(key, value);
  } catch (_) {}
}

export const AUTO_VJ_STORE = {
  paramMode: 'macroverse-autovj-param-mode',
  beatsPerCycle: 'macroverse-autovj-beats-cycle',
  depth: 'macroverse-autovj-param-depth',
  barBeats: 'macroverse-autovj-bar-beats',
  shaderSwap: 'macroverse-autovj-shader-swap',
  paramMove: 'macroverse-autovj-param-move',
} as const;

function readAutoVjBool(key: string, fallback: boolean): boolean {
  const raw = readAutoVjStorage(key, fallback ? '1' : '0');
  return raw === '1' || raw === 'true';
}

export function loadAutoVjParamConfig(): AutoVjParamConfig {
  const paramModeRaw = readAutoVjStorage(AUTO_VJ_STORE.paramMode, 'sine');
  const beatsRaw = parseInt(readAutoVjStorage(AUTO_VJ_STORE.beatsPerCycle, '4'), 10);
  const depthRaw = parseFloat(readAutoVjStorage(AUTO_VJ_STORE.depth, '0.85'));
  const barRaw = parseInt(readAutoVjStorage(AUTO_VJ_STORE.barBeats, '4'), 10);
  return {
    paramMode: isAutoVjParamMode(paramModeRaw) ? paramModeRaw : 'sine',
    beatsPerCycle: AUTO_VJ_BEAT_CYCLES.includes(beatsRaw as (typeof AUTO_VJ_BEAT_CYCLES)[number]) ? beatsRaw : 4,
    depth: Number.isFinite(depthRaw) ? Math.max(0.1, Math.min(1, depthRaw)) : 0.85,
    barBeats: AUTO_VJ_BAR_BEATS.includes(barRaw as (typeof AUTO_VJ_BAR_BEATS)[number]) ? barRaw : 4,
    shaderSwap: readAutoVjBool(AUTO_VJ_STORE.shaderSwap, true),
    paramMove: readAutoVjBool(AUTO_VJ_STORE.paramMove, true),
  };
}
