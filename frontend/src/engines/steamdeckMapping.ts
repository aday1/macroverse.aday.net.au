/**
 * Steam Deck / gamepad control mapping for the VJ deck: pure logic, no DOM.
 *
 * - Input normalisation from the Kefka native bridge (ops/kefka/deck-input.py SSE payload) and from
 *   the standard Gamepad API. Normalised frames use +x right and +y UP for sticks and trackpads.
 * - Auto mapping generated from what the VJ deck exposes (focused deck, its float params, param bank,
 *   gyro enabler), user overrides on top ("partially auto": overrides win, per field).
 * - Response shaping: deadzone, sensitivity, invert, curve.
 * - Gyro ratchet: relative yaw / pitch / roll from the bridge's orientation quaternion, only while an
 *   enabler is held, deadband from activation, frozen while the Deck rests.
 * - Learn capture: the first deliberate input after a baseline (held inputs must be released first).
 *
 * Everything here is deterministic so node:test can run it offline (frontend/tests/).
 * Keep this file free of imports so Node can load it with type stripping.
 */

// ---------------------------------------------------------------------------------------------
// Inputs

export const BUTTON_INPUTS = [
  'btn.a', 'btn.b', 'btn.x', 'btn.y', 'btn.lb', 'btn.rb', 'btn.view', 'btn.menu', 'btn.l3', 'btn.r3',
  'dpad.up', 'dpad.down', 'dpad.left', 'dpad.right',
] as const;
export const TRIGGER_INPUTS = ['trig.lt', 'trig.rt'] as const;
export const STICK_INPUTS = ['ls.x', 'ls.y', 'rs.x', 'rs.y'] as const;
export const PAD_INPUTS = ['lpad.x', 'lpad.y', 'rpad.x', 'rpad.y'] as const;
export const TOUCH_INPUTS = ['lpad.touch', 'rpad.touch'] as const;
export const GYRO_INPUTS = ['gyro.yaw', 'gyro.pitch', 'gyro.roll'] as const;

export type ButtonInput = (typeof BUTTON_INPUTS)[number];
export type InputId =
  | ButtonInput
  | (typeof TRIGGER_INPUTS)[number]
  | (typeof STICK_INPUTS)[number]
  | (typeof PAD_INPUTS)[number]
  | (typeof TOUCH_INPUTS)[number]
  | (typeof GYRO_INPUTS)[number];

export type InputKind = 'button' | 'trigger' | 'stick' | 'pad' | 'gyro';

export const ALL_INPUTS: InputId[] = [
  ...TRIGGER_INPUTS, 'btn.lb', 'btn.rb', ...STICK_INPUTS, 'btn.l3', 'btn.r3',
  'dpad.left', 'dpad.right', 'dpad.up', 'dpad.down', 'btn.a', 'btn.b', 'btn.x', 'btn.y',
  ...PAD_INPUTS, ...TOUCH_INPUTS, ...GYRO_INPUTS, 'btn.view', 'btn.menu',
];

/** View toggles the HUD and Menu the mapping editor; they are never remapped. */
export const RESERVED_INPUTS: ReadonlySet<InputId> = new Set<InputId>(['btn.view', 'btn.menu']);
export const BINDABLE_INPUTS: InputId[] = ALL_INPUTS.filter((i) => !RESERVED_INPUTS.has(i));

/** Standard Gamepad button index per button input (the Kefka bridge uses the same order). */
export const BUTTON_INDEX: Record<ButtonInput, number> = {
  'btn.a': 0, 'btn.b': 1, 'btn.x': 2, 'btn.y': 3, 'btn.lb': 4, 'btn.rb': 5,
  'btn.view': 8, 'btn.menu': 9, 'btn.l3': 10, 'btn.r3': 11,
  'dpad.up': 12, 'dpad.down': 13, 'dpad.left': 14, 'dpad.right': 15,
};

export const INPUT_LABELS: Record<InputId, string> = {
  'btn.a': 'A', 'btn.b': 'B', 'btn.x': 'X', 'btn.y': 'Y', 'btn.lb': 'LB', 'btn.rb': 'RB',
  'btn.view': 'View', 'btn.menu': 'Menu', 'btn.l3': 'L3', 'btn.r3': 'R3',
  'dpad.up': 'D-pad ▴', 'dpad.down': 'D-pad ▾', 'dpad.left': 'D-pad ◂', 'dpad.right': 'D-pad ▸',
  'trig.lt': 'LT', 'trig.rt': 'RT',
  'ls.x': 'L stick ↔', 'ls.y': 'L stick ↕', 'rs.x': 'R stick ↔', 'rs.y': 'R stick ↕',
  'lpad.x': 'L pad ↔', 'lpad.y': 'L pad ↕', 'rpad.x': 'R pad ↔', 'rpad.y': 'R pad ↕',
  'lpad.touch': 'L pad touch', 'rpad.touch': 'R pad touch',
  'gyro.yaw': 'Gyro yaw', 'gyro.pitch': 'Gyro pitch', 'gyro.roll': 'Gyro roll',
};

export function inputKind(id: InputId): InputKind {
  if (id.startsWith('btn.') || id.startsWith('dpad.') || id.endsWith('.touch')) return 'button';
  if (id.startsWith('trig.')) return 'trigger';
  if (id.startsWith('ls.') || id.startsWith('rs.')) return 'stick';
  if (id.startsWith('lpad.') || id.startsWith('rpad.')) return 'pad';
  return 'gyro';
}

export function isInputId(v: unknown): v is InputId {
  return typeof v === 'string' && (ALL_INPUTS as string[]).includes(v);
}

// ---------------------------------------------------------------------------------------------
// Normalised frames

export type Quat = [number, number, number, number];

export interface MotionSample {
  /** Bridge clock (seconds, monotonic). A repeated value means no new sample. */
  at: number;
  /** Body -> world orientation, [w, x, y, z]. Body: +X right, +Y up, +Z toward the user. World +Y up. */
  q: Quat;
  /** The bridge saw the Deck still long enough to learn gyro bias. */
  resting: boolean;
}

export interface PadFrame {
  /** Identity of the controller connection; a change re-arms the neutral gate. */
  source: string;
  kind: 'native' | 'gamepad';
  /** 16 standard buttons, 0..1; LT (6) and RT (7) analog. */
  buttons: number[];
  /** [lx, ly, rx, ry]: +x right, +y UP. */
  axes: [number, number, number, number];
  touch: [boolean, boolean];
  /** [[lx, ly], [rx, ry]]: -1..1, +x right, +y UP; 0,0 while untouched. */
  touchpads: [[number, number], [number, number]];
  hasTouchpads: boolean;
  motion: MotionSample | null;
}

const num = (v: unknown, fallback = 0): number => (typeof v === 'number' && Number.isFinite(v) ? v : fallback);
const clampUnit = (v: number): number => Math.max(-1, Math.min(1, v));
export const clamp01 = (v: number): number => Math.max(0, Math.min(1, Number.isFinite(v) ? v : 0));

function buttonsFrom(raw: unknown): number[] {
  const out = new Array<number>(16).fill(0);
  if (!Array.isArray(raw)) return out;
  for (let i = 0; i < 16 && i < raw.length; i++) {
    const b = raw[i] as unknown;
    const v = typeof b === 'number' ? b : num((b as { value?: unknown })?.value, (b as { pressed?: unknown })?.pressed ? 1 : 0);
    out[i] = clamp01(v);
  }
  return out;
}

function quatFrom(raw: unknown): Quat | null {
  if (!Array.isArray(raw) || raw.length !== 4 || !raw.every((v) => typeof v === 'number' && Number.isFinite(v))) return null;
  const n = Math.hypot(raw[0], raw[1], raw[2], raw[3]);
  if (n < 0.5 || n > 1.5) return null;
  return [raw[0] / n, raw[1] / n, raw[2] / n, raw[3] / n];
}

/**
 * Kefka bridge SSE payload -> frame. Returns null unless the bridge's own focus/ownership gate is open
 * (nativeAllowed), the pad is present, and the payload is well formed.
 * Bridge conventions (hid-steam): stick +y DOWN (driver-negated), trackpad +y UP (raw), touch 0,0 = no finger.
 */
export function normalizeBridgePayload(payload: unknown): PadFrame | null {
  if (!payload || typeof payload !== 'object') return null;
  const p = payload as Record<string, unknown>;
  if (p.nativeAllowed !== true || !p.pad || typeof p.pad !== 'object') return null;
  const pad = p.pad as Record<string, unknown>;
  if (!Array.isArray(pad.buttons)) return null;
  const ax = Array.isArray(pad.axes) ? (pad.axes as unknown[]) : [];
  const axes: [number, number, number, number] = [
    clampUnit(num(ax[0])), -clampUnit(num(ax[1])), clampUnit(num(ax[2])), -clampUnit(num(ax[3])),
  ];
  const tp = Array.isArray(pad.touchpads) ? (pad.touchpads as unknown[]) : null;
  const tpair = (i: number): [number, number] => {
    const v = tp && Array.isArray(tp[i]) ? (tp[i] as unknown[]) : [];
    return [clampUnit(num(v[0])), clampUnit(num(v[1]))];
  };
  const touchRaw = Array.isArray(pad.touch) ? (pad.touch as unknown[]) : [];
  const touchpads: [[number, number], [number, number]] = [tpair(0), tpair(1)];
  const touch: [boolean, boolean] = [touchRaw[0] === true, touchRaw[1] === true];
  let motion: MotionSample | null = null;
  const m = p.motion as Record<string, unknown> | null | undefined;
  if (m && typeof m === 'object') {
    const q = quatFrom(m.q);
    if (q && typeof m.at === 'number' && Number.isFinite(m.at)) motion = { at: m.at, q, resting: m.resting === true };
  }
  return {
    source: 'kefka:' + String(pad.connection ?? '0'),
    kind: 'native',
    buttons: buttonsFrom(pad.buttons),
    axes,
    touch,
    touchpads: [touch[0] ? touchpads[0] : [0, 0], touch[1] ? touchpads[1] : [0, 0]],
    hasTouchpads: tp !== null,
    motion,
  };
}

export interface GamepadLike {
  id: string;
  index: number;
  connected?: boolean;
  mapping?: string;
  axes: readonly number[];
  buttons: readonly ({ value: number; pressed: boolean } | number)[];
}

/** Standard-mapped Gamepad API pad -> frame (no trackpads, no gyro in the standard mapping). */
export function normalizeGamepad(gp: GamepadLike): PadFrame {
  const a = gp.axes || [];
  return {
    source: `gamepad:${gp.index}:${gp.id}`,
    kind: 'gamepad',
    buttons: buttonsFrom(gp.buttons),
    axes: [clampUnit(num(a[0])), -clampUnit(num(a[1])), clampUnit(num(a[2])), -clampUnit(num(a[3]))],
    touch: [false, false],
    touchpads: [[0, 0], [0, 0]],
    hasTouchpads: false,
    motion: null,
  };
}

/** Built-in Deck controller as seen by Chromium (hid-steam 28de:1205 and friends). */
export function isBuiltInDeckGamepad(id: string): boolean {
  return /steam.*(deck|controller)|valve|28de/i.test(id || '');
}

/** Raw (unshaped) value of an input in a frame; gyro inputs are produced by the ratchet instead. */
export function rawInputValue(frame: PadFrame, id: InputId): number {
  switch (id) {
    case 'trig.lt': return frame.buttons[6] ?? 0;
    case 'trig.rt': return frame.buttons[7] ?? 0;
    case 'ls.x': return frame.axes[0];
    case 'ls.y': return frame.axes[1];
    case 'rs.x': return frame.axes[2];
    case 'rs.y': return frame.axes[3];
    case 'lpad.x': return frame.touch[0] ? frame.touchpads[0][0] : 0;
    case 'lpad.y': return frame.touch[0] ? frame.touchpads[0][1] : 0;
    case 'rpad.x': return frame.touch[1] ? frame.touchpads[1][0] : 0;
    case 'rpad.y': return frame.touch[1] ? frame.touchpads[1][1] : 0;
    case 'lpad.touch': return frame.touch[0] ? 1 : 0;
    case 'rpad.touch': return frame.touch[1] ? 1 : 0;
    case 'gyro.yaw': case 'gyro.pitch': case 'gyro.roll': return 0;
    default: return frame.buttons[BUTTON_INDEX[id as ButtonInput]] ?? 0;
  }
}

/** Every button and trigger released and both sticks near centre. Trackpads may be touched. */
export function isNeutral(frame: PadFrame): boolean {
  for (let i = 0; i < 16; i++) if ((frame.buttons[i] ?? 0) > 0.35) return false;
  return frame.axes.every((a) => Math.abs(a) < 0.35);
}

// ---------------------------------------------------------------------------------------------
// Targets

export type DeckId = 'A' | 'B';
export type DeckRef = DeckId | 'focus';

export const ACTIONS = [
  'browse.prev', 'browse.next', 'browse.pageUp', 'browse.pageDown',
  'load.A', 'load.B', 'load.focus',
  'focus.A', 'focus.B', 'focus.toggle', 'bank.next',
  'deck.prev', 'deck.next',
  'cut.A', 'cut.B', 'cut.focus', 'autoVj.toggle',
  'mouse.center', 'hud.toggle',
] as const;
export type ActionId = (typeof ACTIONS)[number];

export type TargetId = string;

export const ACTION_LABELS: Record<ActionId, string> = {
  'browse.prev': 'Browse ◂', 'browse.next': 'Browse ▸',
  'browse.pageUp': 'Browse page ◂◂', 'browse.pageDown': 'Browse page ▸▸',
  'load.A': 'Load → A', 'load.B': 'Load → B', 'load.focus': 'Load → focused deck',
  'focus.A': 'Focus deck A', 'focus.B': 'Focus deck B', 'focus.toggle': 'Swap focus', 'bank.next': 'Next param bank',
  'deck.prev': 'Focused deck: prev shader', 'deck.next': 'Focused deck: next shader',
  'cut.A': 'Cut to A', 'cut.B': 'Cut to B', 'cut.focus': 'Cut to focused deck', 'autoVj.toggle': 'Auto VJ on/off',
  'mouse.center': 'Centre focused mouse', 'hud.toggle': 'Toggle HUD',
};

/** Actions an axis can fire in its negative direction. */
export const ACTION_OPPOSITE: Partial<Record<ActionId, ActionId>> = {
  'browse.next': 'browse.prev', 'browse.prev': 'browse.next',
  'browse.pageDown': 'browse.pageUp', 'browse.pageUp': 'browse.pageDown',
  'deck.next': 'deck.prev', 'deck.prev': 'deck.next',
  'focus.B': 'focus.A', 'focus.A': 'focus.B',
  'load.B': 'load.A', 'load.A': 'load.B',
  'cut.B': 'cut.A', 'cut.A': 'cut.B',
};

/** Held inputs bound to these repeat (D-pad browsing). */
export const REPEATABLE_ACTIONS: ReadonlySet<string> = new Set<string>([
  'browse.prev', 'browse.next', 'browse.pageUp', 'browse.pageDown', 'deck.prev', 'deck.next',
]);

export type ParsedTarget =
  | { kind: 'none' }
  | { kind: 'xfade' }
  | { kind: 'param'; deck: DeckRef; index: number }
  | { kind: 'mouse'; deck: DeckRef; axis: 'x' | 'y' }
  | { kind: 'action'; action: ActionId };

const PARAM_RE = /^(A|B|focus)\.param\.(\d{1,2})$/;
const MOUSE_RE = /^(A|B|focus)\.mouse\.(x|y)$/;

export function parseTarget(t: unknown): ParsedTarget | null {
  if (typeof t !== 'string') return null;
  if (t === 'none') return { kind: 'none' };
  if (t === 'xfade') return { kind: 'xfade' };
  let m = PARAM_RE.exec(t);
  if (m) return { kind: 'param', deck: m[1] as DeckRef, index: Number(m[2]) };
  m = MOUSE_RE.exec(t);
  if (m) return { kind: 'mouse', deck: m[1] as DeckRef, axis: m[2] as 'x' | 'y' };
  if ((ACTIONS as readonly string[]).includes(t)) return { kind: 'action', action: t as ActionId };
  return null;
}

export function isActionTarget(t: TargetId): boolean {
  return parseTarget(t)?.kind === 'action';
}

/** Replace "focus" with the focused deck. */
export function resolveFocusTarget(t: TargetId, focus: DeckId): TargetId {
  if (t.startsWith('focus.param.') || t.startsWith('focus.mouse.')) return focus + t.slice('focus'.length);
  return t;
}

export interface DeckParamInfo {
  name: string;
  type: string;
}

/** Continuous shader params a stick or gyro can sweep (bool, vec and sampler params are skipped). */
export function isContinuousParam(p: DeckParamInfo): boolean {
  const t = (p.type || '').toLowerCase();
  return t === 'float' || t === 'int' || t === 'long';
}

export interface LabelContext {
  focus: DeckId;
  params: Record<DeckId, DeckParamInfo[]>;
}

export function targetLabel(t: TargetId, ctx?: LabelContext): string {
  const p = parseTarget(t);
  if (!p || p.kind === 'none') return '—';
  if (p.kind === 'xfade') return 'Crossfader';
  if (p.kind === 'action') return ACTION_LABELS[p.action];
  const deck: DeckId = p.deck === 'focus' ? (ctx?.focus ?? 'A') : p.deck;
  const prefix = p.deck === 'focus' ? `Focused (${deck})` : deck;
  if (p.kind === 'mouse') return `${prefix} · mouse ${p.axis.toUpperCase()}`;
  const name = ctx?.params[deck]?.[p.index]?.name;
  return `${prefix} · ${name ?? 'param ' + (p.index + 1)}`;
}

export interface TargetGroup {
  label: string;
  targets: TargetId[];
}

/** Everything a binding can point at, grouped for the picker / learn list. */
export function listTargets(ctx: LabelContext): TargetGroup[] {
  const params = (deck: DeckRef): TargetId[] => {
    const d: DeckId = deck === 'focus' ? ctx.focus : deck;
    return (ctx.params[d] || []).map((_, i) => `${deck}.param.${i}`);
  };
  return [
    { label: 'Mixer', targets: ['xfade', 'cut.A', 'cut.B', 'cut.focus', 'autoVj.toggle'] },
    { label: 'Focused deck', targets: [...params('focus'), 'focus.mouse.x', 'focus.mouse.y', 'mouse.center', 'bank.next'] },
    { label: 'Deck A', targets: [...params('A'), 'A.mouse.x', 'A.mouse.y'] },
    { label: 'Deck B', targets: [...params('B'), 'B.mouse.x', 'B.mouse.y'] },
    { label: 'Library', targets: ['browse.prev', 'browse.next', 'browse.pageUp', 'browse.pageDown', 'load.A', 'load.B', 'load.focus', 'deck.prev', 'deck.next'] },
    { label: 'Focus', targets: ['focus.A', 'focus.B', 'focus.toggle', 'hud.toggle'] },
    { label: 'Unbind', targets: ['none'] },
  ];
}

// ---------------------------------------------------------------------------------------------
// Bindings, auto mapping and overrides

export type BindingMode = 'press' | 'toggle' | 'momentary' | 'relative' | 'absolute' | 'ratchet';
export type Curve = 'linear' | 'soft' | 'expo';
export const CURVES: Curve[] = ['linear', 'soft', 'expo'];
const CURVE_EXP: Record<Curve, number> = { linear: 1, soft: 1.6, expo: 2.4 };

export interface Binding {
  input: InputId;
  target: TargetId;
  mode: BindingMode;
  sensitivity: number;
  invert: boolean;
  curve: Curve;
  source: 'auto' | 'user';
}

export interface BindingOverride {
  target?: TargetId;
  mode?: BindingMode;
  sensitivity?: number;
  invert?: boolean;
  curve?: Curve;
}

export type GyroEnabler = 'rpad-touch' | 'lpad-touch' | 'lt-hold' | 'r3-toggle' | 'always' | 'off';
export const GYRO_ENABLERS: GyroEnabler[] = ['rpad-touch', 'lt-hold', 'r3-toggle', 'lpad-touch', 'always', 'off'];
export const GYRO_ENABLER_LABELS: Record<GyroEnabler, string> = {
  'rpad-touch': 'Finger on right trackpad',
  'lpad-touch': 'Finger on left trackpad',
  'lt-hold': 'Hold LT',
  'r3-toggle': 'Toggle with R3',
  always: 'Always on',
  off: 'Off',
};

/** Input that drives the gyro enabler (never auto-bound to anything else). */
export function enablerInput(e: GyroEnabler): InputId | null {
  switch (e) {
    case 'rpad-touch': return 'rpad.touch';
    case 'lpad-touch': return 'lpad.touch';
    case 'lt-hold': return 'trig.lt';
    case 'r3-toggle': return 'btn.r3';
    default: return null;
  }
}

export interface GyroSettings {
  enabler: GyroEnabler;
  /** Degrees of rotation since activation ignored before anything moves. */
  deadband: number;
}

export interface MappingConfig {
  version: 1;
  overrides: Partial<Record<InputId, BindingOverride>>;
  gyro: GyroSettings;
}

export const DEFAULT_GYRO: GyroSettings = { enabler: 'rpad-touch', deadband: 1.5 };

export function defaultConfig(): MappingConfig {
  return { version: 1, overrides: {}, gyro: { ...DEFAULT_GYRO } };
}

export function allowedModes(input: InputId, target: TargetId): BindingMode[] {
  if (isActionTarget(target) || target === 'none') return ['press'];
  switch (inputKind(input)) {
    case 'button': return ['toggle', 'momentary'];
    case 'trigger': return ['relative', 'absolute'];
    case 'stick': return ['relative', 'absolute'];
    case 'pad': return ['absolute', 'relative'];
    default: return ['ratchet'];
  }
}

export function defaultModeFor(input: InputId, target: TargetId): BindingMode {
  const modes = allowedModes(input, target);
  // A trigger squeezing a shader param reads as "amount"; the crossfader keeps a rate.
  if (inputKind(input) === 'trigger' && target !== 'xfade' && modes.includes('absolute')) return 'absolute';
  return modes[0];
}

export function defaultBinding(input: InputId, target: TargetId, source: 'auto' | 'user' = 'user'): Binding {
  return {
    input,
    target,
    mode: defaultModeFor(input, target),
    sensitivity: 1,
    invert: false,
    curve: inputKind(input) === 'stick' ? 'soft' : 'linear',
    source,
  };
}

export const PARAMS_PER_BANK = 4;

export interface AutoContext {
  focus: DeckId;
  /** Which group of four continuous params the sticks reach. */
  bank: number;
  /** Focused deck's exposed params, in deck order. */
  params: DeckParamInfo[];
  gyroEnabler: GyroEnabler;
}

export function bankCount(params: DeckParamInfo[]): number {
  return Math.max(1, Math.ceil(params.filter(isContinuousParam).length / PARAMS_PER_BANK));
}

/**
 * Sensible defaults generated from what the VJ deck exposes:
 * LT/RT and gyro roll ride the crossfader, the sticks sweep the focused deck's continuous params
 * (four per bank, L3 next bank), L pad and gyro yaw/pitch aim the focused deck's mouse, D-pad browses
 * the library, A/B load to deck A/B, X auto VJ, Y cuts to the focused deck, LB/RB focus A/B,
 * R3 centres the mouse. The gyro enabler's own input is left free.
 */
export function buildAutoBindings(ctx: AutoContext): Partial<Record<InputId, Binding>> {
  const out: Partial<Record<InputId, Binding>> = {};
  const put = (input: InputId, target: TargetId, extra: Partial<Binding> = {}): void => {
    out[input] = { ...defaultBinding(input, target, 'auto'), ...extra, input, target, source: 'auto' };
  };
  put('trig.lt', 'xfade', { mode: 'relative', invert: true });
  put('trig.rt', 'xfade', { mode: 'relative' });
  put('btn.lb', 'focus.A');
  put('btn.rb', 'focus.B');
  put('btn.a', 'load.A');
  put('btn.b', 'load.B');
  put('btn.x', 'autoVj.toggle');
  put('btn.y', 'cut.focus');
  put('dpad.left', 'browse.prev');
  put('dpad.right', 'browse.next');
  put('dpad.up', 'browse.pageUp');
  put('dpad.down', 'browse.pageDown');
  put('btn.l3', 'bank.next');
  put('btn.r3', 'mouse.center');

  const continuous: number[] = [];
  ctx.params.forEach((p, i) => { if (isContinuousParam(p)) continuous.push(i); });
  const banks = bankCount(ctx.params);
  const bank = ((Math.floor(ctx.bank) % banks) + banks) % banks;
  const stickInputs: InputId[] = ['ls.x', 'ls.y', 'rs.x', 'rs.y'];
  stickInputs.forEach((input, slot) => {
    const idx = continuous[bank * PARAMS_PER_BANK + slot];
    if (idx !== undefined) put(input, `${ctx.focus}.param.${idx}`, { mode: 'relative', curve: 'soft' });
  });

  put('lpad.x', `${ctx.focus}.mouse.x`, { mode: 'absolute' });
  put('lpad.y', `${ctx.focus}.mouse.y`, { mode: 'absolute' });
  put('gyro.roll', 'xfade', { mode: 'ratchet' });
  put('gyro.yaw', `${ctx.focus}.mouse.x`, { mode: 'ratchet', sensitivity: 1.5 });
  put('gyro.pitch', `${ctx.focus}.mouse.y`, { mode: 'ratchet', sensitivity: 1.5 });

  const enabler = enablerInput(ctx.gyroEnabler);
  if (enabler) {
    delete out[enabler];
    if (enabler === 'lpad.touch') { delete out['lpad.x']; delete out['lpad.y']; }
    if (enabler === 'rpad.touch') { delete out['rpad.x']; delete out['rpad.y']; }
  }
  if (ctx.gyroEnabler === 'off') {
    for (const g of GYRO_INPUTS) delete out[g];
  }
  return out;
}

const SENS_MIN = 0.1;
const SENS_MAX = 5;
export const clampSensitivity = (v: number): number =>
  Math.round(Math.max(SENS_MIN, Math.min(SENS_MAX, Number.isFinite(v) ? v : 1)) * 100) / 100;

/** Auto bindings with user overrides applied per field; an override's target replaces the auto one. */
export function resolveBindings(
  auto: Partial<Record<InputId, Binding>>,
  overrides: Partial<Record<InputId, BindingOverride>>,
): Partial<Record<InputId, Binding>> {
  const out: Partial<Record<InputId, Binding>> = {};
  for (const input of BINDABLE_INPUTS) {
    const a = auto[input];
    const o = overrides[input];
    if (!o) {
      if (a) out[input] = a;
      continue;
    }
    const target = o.target ?? a?.target ?? 'none';
    const base = o.target !== undefined || !a ? defaultBinding(input, target, 'user') : a;
    const modes = allowedModes(input, target);
    out[input] = {
      ...base,
      input,
      target,
      mode: o.mode && modes.includes(o.mode) ? o.mode : modes.includes(base.mode) ? base.mode : modes[0],
      sensitivity: o.sensitivity !== undefined ? clampSensitivity(o.sensitivity) : base.sensitivity,
      invert: o.invert ?? base.invert,
      curve: o.curve ?? base.curve,
      source: 'user',
    };
  }
  return out;
}

export function parseMappingConfig(raw: unknown): MappingConfig {
  const cfg = defaultConfig();
  if (!raw || typeof raw !== 'object') return cfg;
  const r = raw as Record<string, unknown>;
  const g = r.gyro as Record<string, unknown> | undefined;
  if (g && typeof g === 'object') {
    if (GYRO_ENABLERS.includes(g.enabler as GyroEnabler)) cfg.gyro.enabler = g.enabler as GyroEnabler;
    if (typeof g.deadband === 'number' && Number.isFinite(g.deadband)) cfg.gyro.deadband = Math.max(0, Math.min(10, g.deadband));
  }
  const ov = r.overrides as Record<string, unknown> | undefined;
  if (ov && typeof ov === 'object') {
    for (const [key, val] of Object.entries(ov)) {
      if (!isInputId(key) || RESERVED_INPUTS.has(key) || !val || typeof val !== 'object') continue;
      const v = val as Record<string, unknown>;
      const o: BindingOverride = {};
      if (v.target !== undefined && parseTarget(v.target)) o.target = v.target as string;
      if (typeof v.mode === 'string' && ['press', 'toggle', 'momentary', 'relative', 'absolute', 'ratchet'].includes(v.mode)) o.mode = v.mode as BindingMode;
      if (typeof v.sensitivity === 'number' && Number.isFinite(v.sensitivity)) o.sensitivity = clampSensitivity(v.sensitivity);
      if (typeof v.invert === 'boolean') o.invert = v.invert;
      if (typeof v.curve === 'string' && (CURVES as string[]).includes(v.curve)) o.curve = v.curve as Curve;
      if (Object.keys(o).length) cfg.overrides[key] = o;
    }
  }
  return cfg;
}

// ---------------------------------------------------------------------------------------------
// Response shaping

/** Rescaled deadzone: 0 inside, then 0..1 over the rest of the travel. */
export function applyDeadzone(v: number, dz: number): number {
  const a = Math.abs(v);
  if (a <= dz) return 0;
  return Math.sign(v) * Math.min(1, (a - dz) / (1 - dz));
}

/** Sign-preserving power curve; also defined beyond |x| > 1 (gyro sweeps). */
export function applyCurve(v: number, curve: Curve): number {
  const p = CURVE_EXP[curve] ?? 1;
  return p === 1 ? v : Math.sign(v) * Math.pow(Math.abs(v), p);
}

export const STICK_DEADZONE = 0.12;
export const TRIGGER_DEADZONE = 0.04;
/** Value units per second at full deflection, before sensitivity. */
export const RELATIVE_RATE: Record<'stick' | 'trigger', number> = { stick: 0.8, trigger: 0.9 };
/** Trackpad relative: value units per full pad width of finger travel. */
export const PAD_RELATIVE_GAIN = 0.5;
/** Degrees of gyro rotation for a full 0..1 sweep at sensitivity 1. */
export const GYRO_FULL_RANGE_DEG = 60;

// ---------------------------------------------------------------------------------------------
// Gyro orientation -> relative yaw / pitch / roll

export interface Ypr {
  /** Turn right about world up, degrees. */
  yaw: number;
  /** Tilt the Deck's back upward ("aim up"), degrees. */
  pitch: number;
  /** Steer clockwise as seen by the player, degrees. */
  roll: number;
}

const RAD = 180 / Math.PI;

/**
 * Body->world quaternion [w,x,y,z] to aiming angles (intrinsic Y-X-Z: heading about world up, then
 * pitch about the Deck's right axis, then roll about its screen normal), converted to
 * right / up / clockwise positive. The Deck's back points -Z, like a camera.
 */
export function quatToYpr(q: Quat): Ypr {
  const [w, x, y, z] = q;
  const m13 = 2 * (x * z + w * y);
  const m33 = 1 - 2 * (x * x + y * y);
  const m23 = 2 * (y * z - w * x);
  const m21 = 2 * (x * y + w * z);
  const m22 = 1 - 2 * (x * x + z * z);
  const pitch = Math.asin(Math.max(-1, Math.min(1, -m23))) * RAD;
  let yaw: number;
  let roll: number;
  if (Math.abs(m23) < 0.9999) {
    yaw = Math.atan2(m13, m33) * RAD;
    roll = Math.atan2(m21, m22) * RAD;
  } else {
    const m31 = 2 * (x * z - w * y);
    const m11 = 1 - 2 * (y * y + z * z);
    yaw = Math.atan2(-m31, m11) * RAD;
    roll = 0;
  }
  return { yaw: -yaw || 0, pitch: pitch || 0, roll: -roll || 0 };
}

export function wrapDeg(d: number): number {
  return d - 360 * Math.round(d / 360);
}

/** Deadband on a cumulative angle: nothing inside, linear (no jump) outside. */
export function deadband(v: number, db: number): number {
  const a = Math.abs(v);
  return a <= db ? 0 : Math.sign(v) * (a - db);
}

/** A jump this large between samples is a reconnect / gap, not a rotation. */
const GYRO_GAP_DEG = 45;

export interface RatchetUpdate {
  active: boolean;
  /** Raw rotation since activation (degrees). */
  total: Ypr;
  /** Deadbanded rotation since activation, now and before this update. */
  out: Ypr;
  prevOut: Ypr;
}

const zeroYpr = (): Ypr => ({ yaw: 0, pitch: 0, roll: 0 });

/**
 * Relative gyro aiming with a ratchet: while enabled, accumulate frame-to-frame orientation changes
 * since activation; releasing the enabler stops (the target keeps its value) and the next activation
 * starts a fresh stroke from wherever the Deck points. While the bridge reports the Deck resting, the
 * accumulation is frozen, so a Deck lying on a table never drifts a value.
 */
export class GyroRatchet {
  active = false;
  last: Ypr | null = null;
  lastAt = Number.NaN;
  total: Ypr = zeroYpr();
  out: Ypr = zeroYpr();

  reset(): void {
    this.active = false;
    this.last = null;
    this.lastAt = Number.NaN;
    this.total = zeroYpr();
    this.out = zeroYpr();
  }

  update(motion: MotionSample | null, enabled: boolean, db: number): RatchetUpdate {
    if (!enabled || !motion) {
      this.reset();
      return { active: false, total: zeroYpr(), out: zeroYpr(), prevOut: zeroYpr() };
    }
    const now = quatToYpr(motion.q);
    if (!this.active) {
      this.reset();
      this.active = true;
      this.last = now;
      this.lastAt = motion.at;
      return { active: true, total: zeroYpr(), out: zeroYpr(), prevOut: zeroYpr() };
    }
    const prevOut = { ...this.out };
    if (motion.at !== this.lastAt && this.last) {
      const d: Ypr = {
        yaw: wrapDeg(now.yaw - this.last.yaw),
        pitch: now.pitch - this.last.pitch,
        roll: wrapDeg(now.roll - this.last.roll),
      };
      const gap = Math.abs(d.yaw) > GYRO_GAP_DEG || Math.abs(d.pitch) > GYRO_GAP_DEG || Math.abs(d.roll) > GYRO_GAP_DEG;
      if (!gap && !motion.resting) {
        // Heading and roll are undefined with the back pointing straight up/down; fade them out there.
        const nearPole = Math.min(1, Math.cos(now.pitch / RAD) / 0.17);
        this.total.yaw += d.yaw * nearPole;
        this.total.pitch += d.pitch;
        this.total.roll += d.roll * nearPole;
      }
      this.last = now;
      this.lastAt = motion.at;
    }
    const dbv = Math.max(0, db);
    this.out = { yaw: deadband(this.total.yaw, dbv), pitch: deadband(this.total.pitch, dbv), roll: deadband(this.total.roll, dbv) };
    return { active: true, total: { ...this.total }, out: { ...this.out }, prevOut };
  }
}

// ---------------------------------------------------------------------------------------------
// Mapper: frames + bindings -> effects

export type MapperEffect =
  | { type: 'delta'; target: TargetId; amount: number; input: InputId }
  | { type: 'set'; target: TargetId; value: number; input: InputId }
  | { type: 'toggle'; target: TargetId; input: InputId }
  | { type: 'action'; target: TargetId; repeat: boolean; input: InputId };

export interface MapperResult {
  /** Neutral gate passed for this source (no effects until then). */
  ready: boolean;
  effects: MapperEffect[];
  /** Button-kind press edges and auto-repeats (editor navigation). */
  pressed: InputId[];
  released: InputId[];
  /** Live values for the HUD: buttons 0/1, triggers 0..1, sticks/pads -1..1 (+y up), gyro degrees. */
  values: Partial<Record<InputId, number>>;
  gyro: { available: boolean; enabled: boolean; active: boolean; total: Ypr; out: Ypr };
}

export interface MapperOptions {
  /** Milliseconds (performance.now()). */
  now: number;
  /** Seconds since the previous step. */
  dt: number;
  bindings: Partial<Record<InputId, Binding>>;
  gyro: GyroSettings;
}

interface DigitalState {
  down: boolean;
  /** Direction captured at press (axes). */
  dir: number;
  nextRepeat: number;
}

export const REPEAT_DELAY_MS = 380;
export const REPEAT_INTERVAL_MS = 110;
/** A longer gap between samples means the page stalled (e.g. a shader compile); it is not hold time. */
export const STALL_GAP_MS = 150;

function digitalThresholds(kind: InputKind): { on: number; off: number } {
  switch (kind) {
    case 'button': return { on: 0.5, off: 0.35 };
    case 'trigger': return { on: 0.55, off: 0.35 };
    case 'gyro': return { on: 20, off: 10 };
    default: return { on: 0.6, off: 0.4 };
  }
}

const VALUE_EPS = 1e-4;

export class DeckMapper {
  source: string | null = null;
  ready = false;
  digital = new Map<InputId, DigitalState>();
  lastAbsolute = new Map<InputId, number>();
  padPrev = new Map<InputId, number>();
  ratchet = new GyroRatchet();
  r3Toggle = false;
  lastNow: number | null = null;

  reset(): void {
    this.lastNow = null;
    this.source = null;
    this.ready = false;
    this.digital.clear();
    this.lastAbsolute.clear();
    this.padPrev.clear();
    this.ratchet.reset();
    this.r3Toggle = false;
  }

  /** Gyro enabler state for this frame (R3 toggle flips on its press edge, before gating). */
  gyroEnabled(frame: PadFrame, settings: GyroSettings, r3Edge: boolean): boolean {
    switch (settings.enabler) {
      case 'rpad-touch': return frame.touch[1];
      case 'lpad-touch': return frame.touch[0];
      case 'lt-hold': return (frame.buttons[6] ?? 0) > 0.5;
      case 'r3-toggle':
        if (r3Edge) this.r3Toggle = !this.r3Toggle;
        return this.r3Toggle;
      case 'always': return true;
      default: return false;
    }
  }

  step(frame: PadFrame | null, opts: MapperOptions): MapperResult {
    const empty: MapperResult = {
      ready: false, effects: [], pressed: [], released: [], values: {},
      gyro: { available: false, enabled: false, active: false, total: zeroYpr(), out: zeroYpr() },
    };
    if (!frame) {
      if (this.source !== null) this.reset();
      return empty;
    }
    if (frame.source !== this.source) {
      this.reset();
      this.source = frame.source;
    }
    const values: Partial<Record<InputId, number>> = {};
    for (const id of ALL_INPUTS) if (inputKind(id) !== 'gyro') values[id] = rawInputValue(frame, id);

    if (!this.ready) {
      this.ready = isNeutral(frame);
      if (!this.ready) {
        return { ...empty, values, gyro: { ...empty.gyro, available: !!frame.motion } };
      }
      // First neutral frame: seed digital state from it so nothing already held (a resting finger on a
      // trackpad) fires an edge.
      for (const id of ALL_INPUTS) {
        const kind = inputKind(id);
        if (kind === 'gyro') continue;
        const v = values[id] ?? 0;
        this.digital.set(id, { down: Math.abs(v) >= digitalThresholds(kind).on, dir: Math.sign(v), nextRepeat: Infinity });
      }
    }

    const effects: MapperEffect[] = [];
    const pressed: InputId[] = [];
    const released: InputId[] = [];
    const edges = new Map<InputId, { press: boolean; release: boolean; repeat: boolean; dir: number }>();

    // Samples queued during a page stall arrive late in a burst: do not let the stall count as
    // holding a D-pad direction (no phantom auto-repeat steps).
    const gap = this.lastNow === null ? 0 : opts.now - this.lastNow;
    this.lastNow = opts.now;
    if (gap > STALL_GAP_MS) {
      for (const st of this.digital.values()) if (st.down && Number.isFinite(st.nextRepeat)) st.nextRepeat += gap;
    }

    // Digital edges for every non-gyro input (axes use |v| with direction).
    for (const id of ALL_INPUTS) {
      const kind = inputKind(id);
      if (kind === 'gyro') continue;
      const v = values[id] ?? 0;
      const { on, off } = digitalThresholds(kind);
      const st = this.digital.get(id) ?? { down: false, dir: 0, nextRepeat: Infinity };
      let press = false;
      let release = false;
      let repeat = false;
      if (!st.down && Math.abs(v) >= on) {
        st.down = true;
        st.dir = Math.sign(v) || 1;
        st.nextRepeat = opts.now + REPEAT_DELAY_MS;
        press = true;
      } else if (st.down && Math.abs(v) <= off) {
        st.down = false;
        st.nextRepeat = Infinity;
        release = true;
      } else if (st.down && opts.now >= st.nextRepeat) {
        st.nextRepeat = opts.now + REPEAT_INTERVAL_MS;
        repeat = true;
      }
      this.digital.set(id, st);
      edges.set(id, { press, release, repeat, dir: st.dir });
      if (kind === 'button') {
        if (press || repeat) pressed.push(id);
        if (release) released.push(id);
      }
    }

    // Gyro.
    const r3Edge = !!edges.get('btn.r3')?.press;
    const gyroEnabled = this.gyroEnabled(frame, opts.gyro, r3Edge);
    const rat = this.ratchet.update(frame.motion, gyroEnabled, opts.gyro.deadband);
    values['gyro.yaw'] = rat.out.yaw;
    values['gyro.pitch'] = rat.out.pitch;
    values['gyro.roll'] = rat.out.roll;
    const gyroAxis = (id: InputId): keyof Ypr => (id === 'gyro.yaw' ? 'yaw' : id === 'gyro.pitch' ? 'pitch' : 'roll');

    for (const input of BINDABLE_INPUTS) {
      const b = opts.bindings[input];
      if (!b || b.target === 'none') continue;
      const kind = inputKind(input);
      const sign = b.invert ? -1 : 1;
      const sens = b.sensitivity;
      if (isActionTarget(b.target)) {
        if (kind === 'gyro') {
          // Tilt-to-fire: 20 degrees past the deadband, re-arms below 10.
          const st = this.digital.get(input) ?? { down: false, dir: 0, nextRepeat: Infinity };
          const v = rat.active ? rat.out[gyroAxis(input)] : 0;
          if (!st.down && Math.abs(v) >= 20) {
            st.down = true;
            st.dir = Math.sign(v);
            this.digital.set(input, st);
            const action = st.dir * sign < 0 ? ACTION_OPPOSITE[b.target as ActionId] : (b.target as ActionId);
            if (action) effects.push({ type: 'action', target: action, repeat: false, input });
          } else if (st.down && Math.abs(v) <= 10) {
            st.down = false;
            this.digital.set(input, st);
          }
          continue;
        }
        const e = edges.get(input);
        if (!e || !(e.press || (e.repeat && REPEATABLE_ACTIONS.has(b.target)))) continue;
        let action: ActionId | undefined = b.target as ActionId;
        if (kind === 'stick' || kind === 'pad') {
          if (e.dir * sign < 0) action = ACTION_OPPOSITE[b.target as ActionId];
        }
        if (action && (!e.repeat || REPEATABLE_ACTIONS.has(action))) {
          effects.push({ type: 'action', target: action, repeat: e.repeat, input });
        }
        continue;
      }

      // Continuous targets.
      if (kind === 'button') {
        const e = edges.get(input);
        if (!e) continue;
        if (b.mode === 'momentary') {
          if (e.press) effects.push({ type: 'set', target: b.target, value: b.invert ? 0 : 1, input });
          if (e.release) effects.push({ type: 'set', target: b.target, value: b.invert ? 1 : 0, input });
        } else if (e.press) {
          effects.push({ type: 'toggle', target: b.target, input });
        }
        continue;
      }
      if (kind === 'gyro') {
        if (!rat.active) continue;
        const ax = gyroAxis(input);
        const now = applyCurve(rat.out[ax] / GYRO_FULL_RANGE_DEG, b.curve);
        const before = applyCurve(rat.prevOut[ax] / GYRO_FULL_RANGE_DEG, b.curve);
        const amount = (now - before) * sens * sign;
        if (Math.abs(amount) > 1e-6) effects.push({ type: 'delta', target: b.target, amount, input });
        continue;
      }
      const raw = values[input] ?? 0;
      if (kind === 'pad') {
        const touched = input.startsWith('lpad') ? frame.touch[0] : frame.touch[1];
        if (!touched) {
          this.padPrev.delete(input);
          this.lastAbsolute.delete(input);
          continue;
        }
        if (b.mode === 'relative') {
          const prev = this.padPrev.get(input);
          this.padPrev.set(input, raw);
          if (prev === undefined) continue;
          const amount = applyCurve(raw - prev, b.curve) * PAD_RELATIVE_GAIN * sens * sign;
          if (Math.abs(amount) > 1e-6) effects.push({ type: 'delta', target: b.target, amount, input });
        } else {
          const value = clamp01(0.5 + 0.5 * applyCurve(raw, b.curve) * sens * sign);
          const last = this.lastAbsolute.get(input);
          if (last === undefined || Math.abs(last - value) > VALUE_EPS) {
            this.lastAbsolute.set(input, value);
            effects.push({ type: 'set', target: b.target, value, input });
          }
        }
        continue;
      }
      // Sticks and triggers.
      const shaped = kind === 'trigger'
        ? applyCurve(applyDeadzone(Math.max(0, raw), TRIGGER_DEADZONE), b.curve)
        : applyCurve(applyDeadzone(raw, STICK_DEADZONE), b.curve);
      if (b.mode === 'absolute') {
        const value = kind === 'trigger'
          ? clamp01(b.invert ? 1 - shaped * sens : shaped * sens)
          : clamp01(0.5 + 0.5 * shaped * sens * sign);
        const last = this.lastAbsolute.get(input);
        if (last === undefined ? Math.abs(shaped) > 0 : Math.abs(last - value) > VALUE_EPS) {
          this.lastAbsolute.set(input, value);
          effects.push({ type: 'set', target: b.target, value, input });
        }
        continue;
      }
      if (shaped === 0) continue;
      const rate = RELATIVE_RATE[kind === 'trigger' ? 'trigger' : 'stick'];
      const amount = shaped * rate * sens * sign * Math.max(0, Math.min(0.1, opts.dt));
      if (Math.abs(amount) > 1e-7) effects.push({ type: 'delta', target: b.target, amount, input });
    }

    return {
      ready: true,
      effects,
      pressed,
      released,
      values,
      gyro: { available: !!frame.motion, enabled: gyroEnabled, active: rat.active, total: rat.total, out: rat.out },
    };
  }
}

// ---------------------------------------------------------------------------------------------
// Learn capture

export const LEARN_GYRO_DEG = 25;

/**
 * Watches frames after a learn request and reports the first deliberate input. Inputs already active
 * when learning starts (the button that started it, a resting finger) must return to neutral first.
 * Reserved inputs (View/Menu) are ignored; the caller uses them to cancel.
 */
export class LearnCapture {
  busy = new Set<InputId>();
  padStart = new Map<number, { x: number; y: number; at: number; moved: boolean }>();
  gyroLast: Ypr | null = null;
  gyroTotal: Ypr = zeroYpr();

  constructor(baseline: PadFrame | null) {
    if (baseline) this.markBusy(baseline);
  }

  activeInputs(frame: PadFrame): InputId[] {
    const out: InputId[] = [];
    for (const id of BINDABLE_INPUTS) {
      const kind = inputKind(id);
      if (kind === 'gyro' || kind === 'pad') continue;
      const v = rawInputValue(frame, id);
      if (kind === 'button' && id.endsWith('.touch')) { if (v > 0.5) out.push(id); continue; }
      if (kind === 'button' && v > 0.35) out.push(id);
      else if (kind === 'trigger' && v > 0.3) out.push(id);
      else if (kind === 'stick' && Math.abs(v) > 0.3) out.push(id);
    }
    return out;
  }

  markBusy(frame: PadFrame): void {
    for (const id of this.activeInputs(frame)) this.busy.add(id);
  }

  update(frame: PadFrame | null, nowMs: number): InputId | null {
    if (!frame) return null;
    const active = new Set(this.activeInputs(frame));
    for (const id of [...this.busy]) if (!active.has(id)) this.busy.delete(id);

    // Buttons first (unambiguous), then triggers, then sticks by dominant axis.
    for (const id of BINDABLE_INPUTS) {
      if (this.busy.has(id)) continue;
      const kind = inputKind(id);
      const v = rawInputValue(frame, id);
      if (kind === 'button' && !id.endsWith('.touch') && v > 0.5) return id;
      if (kind === 'trigger' && v > 0.6) return id;
    }
    const sticks: [InputId, InputId][] = [['ls.x', 'ls.y'], ['rs.x', 'rs.y']];
    for (const [xId, yId] of sticks) {
      if (this.busy.has(xId) || this.busy.has(yId)) continue;
      const x = rawInputValue(frame, xId);
      const y = rawInputValue(frame, yId);
      if (Math.max(Math.abs(x), Math.abs(y)) > 0.6) return Math.abs(x) >= Math.abs(y) ? xId : yId;
    }

    // Trackpads: a swipe binds the dominant axis, a short tap binds the touch.
    for (const side of [0, 1]) {
      const touchId: InputId = side === 0 ? 'lpad.touch' : 'rpad.touch';
      const xId: InputId = side === 0 ? 'lpad.x' : 'rpad.x';
      const yId: InputId = side === 0 ? 'lpad.y' : 'rpad.y';
      const touched = frame.touch[side];
      const start = this.padStart.get(side);
      if (touched) {
        const [x, y] = frame.touchpads[side];
        if (!start) {
          if (!this.busy.has(touchId)) this.padStart.set(side, { x, y, at: nowMs, moved: false });
          continue;
        }
        const dx = x - start.x;
        const dy = y - start.y;
        if (Math.max(Math.abs(dx), Math.abs(dy)) > 0.35) return Math.abs(dx) >= Math.abs(dy) ? xId : yId;
        if (Math.max(Math.abs(dx), Math.abs(dy)) > 0.12) start.moved = true;
      } else if (start) {
        this.padStart.delete(side);
        if (!start.moved && nowMs - start.at < 600) return touchId;
      }
      if (!touched) this.busy.delete(touchId);
    }

    // Gyro: a deliberate rotation about one axis.
    if (frame.motion) {
      const cur = quatToYpr(frame.motion.q);
      if (this.gyroLast) {
        const d = { yaw: wrapDeg(cur.yaw - this.gyroLast.yaw), pitch: cur.pitch - this.gyroLast.pitch, roll: wrapDeg(cur.roll - this.gyroLast.roll) };
        if (Math.max(Math.abs(d.yaw), Math.abs(d.pitch), Math.abs(d.roll)) <= GYRO_GAP_DEG) {
          this.gyroTotal.yaw += d.yaw;
          this.gyroTotal.pitch += d.pitch;
          this.gyroTotal.roll += d.roll;
        }
      }
      this.gyroLast = cur;
      const t = this.gyroTotal;
      const axes: [InputId, number][] = [['gyro.yaw', Math.abs(t.yaw)], ['gyro.pitch', Math.abs(t.pitch)], ['gyro.roll', Math.abs(t.roll)]];
      axes.sort((a, b) => b[1] - a[1]);
      if (axes[0][1] >= LEARN_GYRO_DEG && axes[0][1] >= 1.6 * axes[1][1]) return axes[0][0];
    }
    return null;
  }
}
