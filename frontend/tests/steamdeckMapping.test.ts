// Offline tests for the pure Deck mapping logic. Run: npm run test:deck (node --test, type stripping).
import test from 'node:test';
import assert from 'node:assert/strict';
import {
  DeckMapper,
  GyroRatchet,
  LearnCapture,
  applyCurve,
  applyDeadzone,
  bankCount,
  buildAutoBindings,
  defaultConfig,
  isBuiltInDeckGamepad,
  isNeutral,
  listTargets,
  normalizeBridgePayload,
  normalizeGamepad,
  parseMappingConfig,
  parseTarget,
  quatToYpr,
  resolveBindings,
  resolveFocusTarget,
  targetLabel,
  type Binding,
  type InputId,
  type MapperResult,
  type PadFrame,
  type Quat,
} from '../src/engines/steamdeckMapping.ts';

const near = (a: number, b: number, eps = 1e-6, msg?: string): void => {
  assert.ok(Math.abs(a - b) <= eps, `${msg ?? ''} expected ${b}, got ${a}`);
};

/** Quaternion for a rotation of deg degrees about a unit axis. */
function axisAngle(axis: [number, number, number], deg: number): Quat {
  const h = (deg * Math.PI) / 360;
  const s = Math.sin(h);
  return [Math.cos(h), axis[0] * s, axis[1] * s, axis[2] * s];
}
function mul(a: Quat, b: Quat): Quat {
  return [
    a[0] * b[0] - a[1] * b[1] - a[2] * b[2] - a[3] * b[3],
    a[0] * b[1] + a[1] * b[0] + a[2] * b[3] - a[3] * b[2],
    a[0] * b[2] - a[1] * b[3] + a[2] * b[0] + a[3] * b[1],
    a[0] * b[3] + a[1] * b[2] - a[2] * b[1] + a[3] * b[0],
  ];
}
/** Deck held tilted back by `tilt` degrees, turned `yaw` (right +), steered `roll` (clockwise +). */
function pose(yawRight: number, pitchUp: number, rollCw: number): Quat {
  return mul(mul(axisAngle([0, 1, 0], -yawRight), axisAngle([1, 0, 0], pitchUp)), axisAngle([0, 0, 1], -rollCw));
}

function frame(over: Partial<PadFrame> = {}): PadFrame {
  return {
    source: 'test:1',
    kind: 'native',
    buttons: new Array(16).fill(0),
    axes: [0, 0, 0, 0],
    touch: [false, false],
    touchpads: [[0, 0], [0, 0]],
    hasTouchpads: true,
    motion: null,
    ...over,
  };
}
function withButton(i: number, v = 1, base: PadFrame = frame()): PadFrame {
  const buttons = base.buttons.slice();
  buttons[i] = v;
  return { ...base, buttons };
}

const PARAMS = [
  { name: 'speed', type: 'float' },
  { name: 'invert', type: 'bool' },
  { name: 'zoom', type: 'float' },
  { name: 'hue', type: 'float' },
  { name: 'tint', type: 'vec3' },
  { name: 'amount', type: 'float' },
  { name: 'warp', type: 'float' },
];

// ------------------------------------------------------------------------------------------------
test('bridge payload: gate closed or malformed -> null', () => {
  assert.equal(normalizeBridgePayload(null), null);
  assert.equal(normalizeBridgePayload({ nativeAllowed: false, pad: { buttons: [] } }), null);
  assert.equal(normalizeBridgePayload({ nativeAllowed: true, pad: null }), null);
  assert.equal(normalizeBridgePayload({ nativeAllowed: true, pad: { axes: [0, 0, 0, 0] } }), null);
});

test('bridge payload: sticks flipped to +y up, trackpads kept (+y up), touch gates pad values', () => {
  const buttons = new Array(16).fill(0);
  buttons[0] = 1;
  buttons[7] = 0.42;
  const f = normalizeBridgePayload({
    nativeAllowed: true,
    connection: 'x',
    pad: {
      buttons,
      connection: '3:7',
      axes: [0.5, -0.8, -0.25, 0.4],
      touch: [false, true],
      touchpads: [[0.9, 0.9], [0.3, 0.6]],
    },
    motion: { at: 12.5, q: [1.0002, 0, 0, 0], resting: true, rate: [0, 0, 0] },
  });
  assert.ok(f);
  assert.equal(f.source, 'kefka:3:7');
  assert.deepEqual(f.axes, [0.5, 0.8, -0.25, -0.4]);
  assert.equal(f.buttons[0], 1);
  near(f.buttons[7], 0.42);
  assert.deepEqual(f.touchpads[0], [0, 0], 'untouched pad reads zero');
  assert.deepEqual(f.touchpads[1], [0.3, 0.6]);
  assert.deepEqual(f.motion?.q, [1, 0, 0, 0], 'quaternion normalised');
  assert.equal(f.motion?.resting, true);
});

test('bridge payload: bad quaternion drops motion only', () => {
  const f = normalizeBridgePayload({ nativeAllowed: true, pad: { buttons: [] }, motion: { at: 1, q: [0, 0, 0] } });
  assert.ok(f);
  assert.equal(f.motion, null);
});

test('gamepad API: standard mapping, +y up, built-in Deck detection', () => {
  const f = normalizeGamepad({
    id: 'Xbox Wireless Controller', index: 2,
    axes: [0.1, 0.9, 0, -1],
    buttons: Array.from({ length: 17 }, (_, i) => ({ value: i === 6 ? 0.7 : 0, pressed: i === 6 })),
  });
  assert.equal(f.source, 'gamepad:2:Xbox Wireless Controller');
  assert.deepEqual(f.axes, [0.1, -0.9, 0, 1]);
  near(f.buttons[6], 0.7);
  assert.equal(f.motion, null);
  assert.ok(isBuiltInDeckGamepad('Steam Deck (Vendor: 28de Product: 1205)'));
  assert.ok(!isBuiltInDeckGamepad('Xbox Wireless Controller (STANDARD GAMEPAD Vendor: 045e)'));
});

// ------------------------------------------------------------------------------------------------
test('curves and deadzone', () => {
  assert.equal(applyDeadzone(0.1, 0.12), 0);
  near(applyDeadzone(1, 0.12), 1);
  near(applyDeadzone(-0.56, 0.12), -0.5);
  near(applyCurve(0.5, 'linear'), 0.5);
  assert.ok(applyCurve(0.5, 'soft') < 0.5 && applyCurve(0.5, 'expo') < applyCurve(0.5, 'soft'));
  near(applyCurve(-1, 'expo'), -1);
  assert.ok(applyCurve(2, 'soft') > 2, 'curve extends past 1 for gyro sweeps');
});

// ------------------------------------------------------------------------------------------------
test('auto mapping follows what the deck exposes (focus, continuous params, banks)', () => {
  const auto = buildAutoBindings({ focus: 'B', bank: 0, params: PARAMS, gyroEnabler: 'rpad-touch' });
  assert.equal(auto['ls.x']?.target, 'B.param.0', 'speed');
  assert.equal(auto['ls.y']?.target, 'B.param.2', 'bool skipped -> zoom');
  assert.equal(auto['rs.x']?.target, 'B.param.3');
  assert.equal(auto['rs.y']?.target, 'B.param.5', 'vec3 skipped -> amount');
  assert.equal(auto['trig.lt']?.target, 'xfade');
  assert.equal(auto['trig.lt']?.invert, true);
  assert.equal(auto['trig.rt']?.mode, 'relative');
  assert.equal(auto['gyro.roll']?.target, 'xfade');
  assert.equal(auto['gyro.roll']?.mode, 'ratchet');
  assert.equal(auto['gyro.yaw']?.target, 'B.mouse.x');
  assert.equal(auto['btn.lb']?.target, 'focus.A');
  assert.equal(auto['btn.rb']?.target, 'focus.B');
  assert.equal(auto['btn.a']?.target, 'load.A');
  assert.equal(auto['btn.b']?.target, 'load.B');
  assert.equal(auto['btn.x']?.target, 'autoVj.toggle');
  assert.equal(auto['dpad.right']?.target, 'browse.next');
  assert.equal(auto['dpad.down']?.target, 'browse.pageDown');
  assert.equal(auto['btn.view'], undefined, 'View is reserved');
  assert.equal(auto['rpad.x'], undefined, 'enabler pad stays free');

  assert.equal(bankCount(PARAMS), 2);
  const bank1 = buildAutoBindings({ focus: 'A', bank: 1, params: PARAMS, gyroEnabler: 'rpad-touch' });
  assert.equal(bank1['ls.x']?.target, 'A.param.6', 'bank 2 starts at the 5th continuous param');
  assert.equal(bank1['ls.y'], undefined, 'no 6th continuous param');
  const wrapped = buildAutoBindings({ focus: 'A', bank: 2, params: PARAMS, gyroEnabler: 'rpad-touch' });
  assert.equal(wrapped['ls.x']?.target, 'A.param.0', 'bank wraps');

  const empty = buildAutoBindings({ focus: 'A', bank: 0, params: [], gyroEnabler: 'rpad-touch' });
  assert.equal(empty['ls.x'], undefined);
  assert.equal(empty['trig.rt']?.target, 'xfade');
});

test('auto mapping frees the gyro enabler input', () => {
  const lt = buildAutoBindings({ focus: 'A', bank: 0, params: PARAMS, gyroEnabler: 'lt-hold' });
  assert.equal(lt['trig.lt'], undefined);
  assert.equal(lt['trig.rt']?.target, 'xfade');
  const r3 = buildAutoBindings({ focus: 'A', bank: 0, params: PARAMS, gyroEnabler: 'r3-toggle' });
  assert.equal(r3['btn.r3'], undefined);
  const lpad = buildAutoBindings({ focus: 'A', bank: 0, params: PARAMS, gyroEnabler: 'lpad-touch' });
  assert.equal(lpad['lpad.x'], undefined);
  const off = buildAutoBindings({ focus: 'A', bank: 0, params: PARAMS, gyroEnabler: 'off' });
  assert.equal(off['gyro.roll'], undefined);
});

test('overrides win per field; untouched inputs stay auto', () => {
  const auto = buildAutoBindings({ focus: 'A', bank: 0, params: PARAMS, gyroEnabler: 'rpad-touch' });
  const res = resolveBindings(auto, {
    'ls.x': { target: 'xfade' },
    'ls.y': { invert: true, sensitivity: 2.5 },
    'btn.y': { target: 'none' },
    'trig.rt': { target: 'A.param.2' },
    'btn.view': { target: 'xfade' },
  });
  assert.equal(res['ls.x']?.target, 'xfade');
  assert.equal(res['ls.x']?.mode, 'relative');
  assert.equal(res['ls.x']?.source, 'user');
  assert.equal(res['ls.y']?.target, 'A.param.2', 'tuning-only override keeps the auto target');
  assert.equal(res['ls.y']?.invert, true);
  assert.equal(res['ls.y']?.sensitivity, 2.5);
  assert.equal(res['btn.y']?.target, 'none');
  assert.equal(res['trig.rt']?.mode, 'absolute', 'trigger -> param defaults to amount');
  assert.equal(res['btn.view'], undefined, 'reserved inputs cannot be overridden');
  assert.equal(res['rs.x']?.source, 'auto');
});

test('config parsing validates and clamps', () => {
  const cfg = parseMappingConfig({
    gyro: { enabler: 'lt-hold', deadband: 99 },
    overrides: {
      'ls.x': { target: 'A.param.3', sensitivity: 50, curve: 'expo', invert: true },
      'nope': { target: 'xfade' },
      'btn.menu': { target: 'xfade' },
      'rs.x': { target: 'rm -rf /' },
    },
  });
  assert.equal(cfg.gyro.enabler, 'lt-hold');
  assert.equal(cfg.gyro.deadband, 10);
  assert.deepEqual(cfg.overrides['ls.x'], { target: 'A.param.3', sensitivity: 5, curve: 'expo', invert: true });
  assert.equal(Object.keys(cfg.overrides).length, 1);
  assert.deepEqual(parseMappingConfig('garbage'), defaultConfig());
});

test('targets: parse, focus resolution, labels, picker groups', () => {
  assert.deepEqual(parseTarget('focus.param.3'), { kind: 'param', deck: 'focus', index: 3 });
  assert.deepEqual(parseTarget('B.mouse.y'), { kind: 'mouse', deck: 'B', axis: 'y' });
  assert.equal(parseTarget('A.param.x'), null);
  assert.equal(resolveFocusTarget('focus.param.2', 'B'), 'B.param.2');
  assert.equal(resolveFocusTarget('cut.focus', 'B'), 'cut.focus');
  const ctx = { focus: 'B' as const, params: { A: PARAMS, B: PARAMS.slice(0, 2) } };
  assert.equal(targetLabel('A.param.2', ctx), 'A · zoom');
  assert.equal(targetLabel('focus.param.0', ctx), 'Focused (B) · speed');
  assert.equal(targetLabel('xfade', ctx), 'Crossfader');
  const groups = listTargets(ctx);
  assert.equal(groups.find((g) => g.label === 'Focused deck')?.targets.filter((t) => t.includes('.param.')).length, 2);
  assert.equal(groups.find((g) => g.label === 'Deck A')?.targets.filter((t) => t.includes('.param.')).length, PARAMS.length);
});

// ------------------------------------------------------------------------------------------------
test('quaternion -> yaw/pitch/roll signs (right, up, clockwise positive)', () => {
  const r = quatToYpr(pose(30, 0, 0));
  near(r.yaw, 30, 1e-6, 'turn right');
  near(r.pitch, 0, 1e-6);
  near(r.roll, 0, 1e-6);
  near(quatToYpr(pose(0, 20, 0)).pitch, 20, 1e-6, 'aim up');
  near(quatToYpr(pose(0, 0, 15)).roll, 15, 1e-6, 'steer clockwise');
  const c = quatToYpr(pose(-40, -35, 10));
  near(c.yaw, -40, 1e-6);
  near(c.pitch, -35, 1e-6);
  near(c.roll, 10, 1e-6);
  // Clockwise steer seen by the player puts the right side DOWN: rotate body +X into world.
  const q = pose(0, 0, 30);
  const [w, x, y, z] = q;
  const rightY = 2 * (x * y + w * z);
  assert.ok(rightY < 0, 'right edge goes down');
});

test('gyro ratchet: only while enabled, deadband from activation, ratchets, frozen at rest', () => {
  const g = new GyroRatchet();
  const base = pose(10, -40, 0);
  // Disabled: nothing accumulates.
  let u = g.update({ at: 1, q: pose(25, -40, 0), resting: false }, false, 1.5);
  assert.equal(u.active, false);
  // Activate at the base pose.
  u = g.update({ at: 2, q: base, resting: false }, true, 1.5);
  assert.equal(u.active, true);
  near(u.out.roll, 0);
  // Inside the deadband: no output.
  u = g.update({ at: 3, q: pose(10, -40, 1.2), resting: false }, true, 1.5);
  near(u.total.roll, 1.2, 1e-6);
  near(u.out.roll, 0);
  // Past it: linear beyond the deadband, no jump.
  u = g.update({ at: 4, q: pose(10, -40, 11.5), resting: false }, true, 1.5);
  near(u.out.roll, 10, 1e-6);
  near(u.prevOut.roll, 0);
  // Same sample time again: no change.
  u = g.update({ at: 4, q: pose(10, -40, 30), resting: false }, true, 1.5);
  near(u.out.roll, 10, 1e-6);
  // Resting: frozen even if q drifts.
  u = g.update({ at: 5, q: pose(10, -40, 14), resting: true }, true, 1.5);
  near(u.out.roll, 10, 1e-6);
  // Release, rotate a lot, re-engage: new stroke starts at zero (ratchet).
  g.update({ at: 6, q: pose(10, -40, 60), resting: false }, false, 1.5);
  u = g.update({ at: 7, q: pose(10, -40, 60), resting: false }, true, 1.5);
  near(u.out.roll, 0);
  u = g.update({ at: 8, q: pose(10, -40, 50), resting: false }, true, 1.5);
  near(u.out.roll, -8.5, 1e-6);
  // A huge jump between samples is a gap, not a rotation.
  u = g.update({ at: 9, q: pose(100, -40, 50), resting: false }, true, 1.5);
  near(u.out.yaw, 0, 1e-6);
});

test('gyro ratchet: yaw wraps across +-180', () => {
  const g = new GyroRatchet();
  g.update({ at: 1, q: pose(170, -30, 0), resting: false }, true, 0);
  g.update({ at: 2, q: pose(-170, -30, 0), resting: false }, true, 0);
  const u = g.update({ at: 3, q: pose(-160, -30, 0), resting: false }, true, 0);
  near(u.out.yaw, 30, 1e-6);
});

// ------------------------------------------------------------------------------------------------
const AUTO = buildAutoBindings({ focus: 'A', bank: 0, params: PARAMS, gyroEnabler: 'rpad-touch' });
const GYRO = { enabler: 'rpad-touch' as const, deadband: 1.5 };

function run(m: DeckMapper, f: PadFrame | null, now: number, bindings = AUTO, dt = 1 / 60): MapperResult {
  return m.step(f, { now, dt, bindings, gyro: GYRO });
}
const targets = (r: MapperResult): string[] => r.effects.map((e) => e.type + ':' + e.target);

test('mapper: neutral gate on connect, then edges fire once', () => {
  const m = new DeckMapper();
  const held = withButton(0);
  let r = run(m, held, 0);
  assert.equal(r.ready, false, 'A held at connect');
  assert.deepEqual(r.effects, []);
  r = run(m, held, 16);
  assert.deepEqual(r.effects, [], 'still held: nothing');
  r = run(m, frame(), 32);
  assert.equal(r.ready, true);
  assert.deepEqual(r.effects, []);
  r = run(m, withButton(0), 48);
  assert.deepEqual(targets(r), ['action:load.A']);
  assert.deepEqual(r.pressed, ['btn.a']);
  r = run(m, withButton(0), 64);
  assert.deepEqual(r.effects, [], 'held A does not repeat (not a browse action)');
  // A different source re-arms the gate.
  r = run(m, { ...withButton(1), source: 'test:2' }, 80);
  assert.equal(r.ready, false);
});

/** Step a held frame every 20 ms (the bridge's SSE cadence) and collect action times. */
function holdActions(m: DeckMapper, f: PadFrame, from: number, to: number): number[] {
  const at: number[] = [];
  for (let t = from; t <= to; t += 20) if (run(m, f, t).effects.some((e) => e.type === 'action')) at.push(t);
  return at;
}

test('mapper: D-pad browse auto-repeats while held', () => {
  const m = new DeckMapper();
  run(m, frame(), -20);
  const right = withButton(15);
  assert.deepEqual(targets(run(m, right, 0)), ['action:browse.next']);
  assert.deepEqual(holdActions(m, right, 20, 600), [380, 500], 'first repeat after 380 ms, then every 110 ms');
  run(m, frame(), 620);
  assert.deepEqual(targets(run(m, withButton(13), 640)), ['action:browse.pageDown']);
});

test('mapper: a page stall while a D-pad is held does not count as hold time', () => {
  const m = new DeckMapper();
  run(m, frame(), -20);
  const right = withButton(15);
  assert.deepEqual(targets(run(m, right, 0)), ['action:browse.next']);
  assert.deepEqual(holdActions(m, right, 20, 100), []);
  // Main thread blocked 600 ms; queued "still held" samples then arrive late.
  assert.deepEqual(holdActions(m, right, 700, 960), [], 'no phantom repeat right after the stall');
  assert.deepEqual(holdActions(m, right, 980, 980), [980], 'repeat resumes on real hold time (380 ms + stall)');
});

test('mapper: triggers ride the crossfader at a rate, LT toward A', () => {
  const m = new DeckMapper();
  run(m, frame(), 0);
  // Start pulling after the gate: a light pull below the press threshold is still analog.
  const r = run(m, withButton(7, 1), 16, AUTO, 0.1);
  const d = r.effects.find((e) => e.type === 'delta' && e.target === 'xfade');
  assert.ok(d && d.type === 'delta');
  near(d.amount, 0.09, 1e-9, 'full RT for 100 ms');
  const l = run(m, withButton(6, 1, frame()), 32, AUTO, 0.1).effects.find((e) => e.type === 'delta');
  assert.ok(l && l.type === 'delta' && l.amount < 0, 'LT moves toward A');
});

test('mapper: stick sweeps focused param relatively; up increases', () => {
  const m = new DeckMapper();
  run(m, frame(), 0);
  const r = run(m, frame({ axes: [0, 1, 0, 0] }), 16, AUTO, 0.05);
  const d = r.effects.find((e) => e.target === 'A.param.2');
  assert.ok(d && d.type === 'delta' && d.amount > 0);
  near((d as { amount: number }).amount, 0.8 * 0.05, 1e-9);
  // Inside the deadzone: nothing.
  assert.deepEqual(run(m, frame({ axes: [0.05, 0.1, 0, 0] }), 32).effects, []);
});

test('mapper: left pad aims focused mouse absolutely while touched', () => {
  const m = new DeckMapper();
  run(m, frame(), 0);
  const r = run(m, frame({ touch: [true, false], touchpads: [[1, -0.5], [0, 0]] }), 16);
  const sets = r.effects.filter((e) => e.type === 'set');
  assert.deepEqual(sets.map((e) => e.target), ['A.mouse.x', 'A.mouse.y']);
  near((sets[0] as { value: number }).value, 1);
  near((sets[1] as { value: number }).value, 0.25);
  assert.deepEqual(run(m, frame({ touch: [true, false], touchpads: [[1, -0.5], [0, 0]] }), 32).effects, [], 'unchanged -> no spam');
  assert.deepEqual(run(m, frame(), 48).effects, [], 'lift: mouse stays');
});

test('mapper: gyro roll moves crossfader only while the right pad is touched', () => {
  const m = new DeckMapper();
  const rest = pose(0, -40, 0);
  const tilt = pose(0, -40, 31.5);
  const at = (t: number, q: Quat, touched: boolean): PadFrame =>
    frame({ motion: { at: t, q, resting: false }, touch: [false, touched], touchpads: [[0, 0], [touched ? 0.2 : 0, 0]] });
  run(m, at(1, rest, false), 0);
  // Rolling without the enabler: nothing.
  let r = run(m, at(2, tilt, false), 16);
  assert.equal(r.effects.filter((e) => e.target === 'xfade').length, 0);
  assert.equal(r.gyro.enabled, false);
  // Touch (activation at current pose), then roll back to level: -31.5 deg, deadband 1.5 -> -30 deg = -0.5.
  r = run(m, at(3, tilt, true), 32);
  assert.equal(r.gyro.active, true);
  r = run(m, at(4, rest, true), 48);
  const d = r.effects.find((e) => e.target === 'xfade');
  assert.ok(d && d.type === 'delta');
  near(d.amount, -0.5, 1e-6);
  // Lift: further rotation ignored.
  r = run(m, at(5, tilt, false), 64);
  assert.equal(r.effects.filter((e) => e.target === 'xfade').length, 0);
});

test('mapper: R3 toggle enabler flips gyro on/off', () => {
  const m = new DeckMapper();
  const gyro = { enabler: 'r3-toggle' as const, deadband: 0 };
  const bindings = buildAutoBindings({ focus: 'A', bank: 0, params: PARAMS, gyroEnabler: 'r3-toggle' });
  const f = (r3: boolean, t: number, roll: number) => withButton(11, r3 ? 1 : 0, frame({ motion: { at: t, q: pose(0, -30, roll), resting: false } }));
  m.step(f(false, 1, 0), { now: 0, dt: 0.016, bindings, gyro });
  assert.equal(m.step(f(true, 2, 0), { now: 16, dt: 0.016, bindings, gyro }).gyro.enabled, true);
  assert.equal(m.step(f(false, 3, 0), { now: 32, dt: 0.016, bindings, gyro }).gyro.enabled, true, 'stays on after release');
  const r = m.step(f(false, 4, 12), { now: 48, dt: 0.016, bindings, gyro });
  near((r.effects.find((e) => e.target === 'xfade') as { amount: number }).amount, 12 / 60, 1e-6);
  m.step(f(true, 5, 12), { now: 64, dt: 0.016, bindings, gyro });
  assert.equal(m.step(f(false, 6, 40), { now: 80, dt: 0.016, bindings, gyro }).gyro.enabled, false);
});

test('mapper: stick bound to an action fires the opposite on negative deflection', () => {
  const m = new DeckMapper();
  const b: Partial<Record<InputId, Binding>> = resolveBindings({}, { 'rs.x': { target: 'browse.next' } });
  run(m, frame(), 0, b);
  assert.deepEqual(targets(run(m, frame({ axes: [0, 0, -0.9, 0] }), 10, b)), ['action:browse.prev']);
  run(m, frame(), 20, b);
  assert.deepEqual(targets(run(m, frame({ axes: [0, 0, 0.9, 0] }), 30, b)), ['action:browse.next']);
});

test('mapper: button on a continuous target toggles or holds', () => {
  const m = new DeckMapper();
  const b = resolveBindings({}, { 'btn.y': { target: 'A.param.1' }, 'btn.x': { target: 'xfade', mode: 'momentary' } });
  run(m, frame(), 0, b);
  assert.deepEqual(targets(run(m, withButton(3), 10, b)), ['toggle:A.param.1']);
  run(m, frame(), 20, b);
  const press = run(m, withButton(2), 30, b).effects[0];
  assert.deepEqual(press, { type: 'set', target: 'xfade', value: 1, input: 'btn.x' });
  const release = run(m, frame(), 40, b).effects[0];
  assert.deepEqual(release, { type: 'set', target: 'xfade', value: 0, input: 'btn.x' });
});

test('isNeutral ignores a resting trackpad finger', () => {
  assert.ok(isNeutral(frame({ touch: [false, true], touchpads: [[0, 0], [0.3, 0.3]] })));
  assert.ok(!isNeutral(frame({ axes: [0, 0, 0.5, 0] })));
  assert.ok(!isNeutral(withButton(6, 0.5)));
});

// ------------------------------------------------------------------------------------------------
test('learn: ignores the held button that started learning, then binds the next input', () => {
  const start = withButton(0);
  const cap = new LearnCapture(start);
  assert.equal(cap.update(withButton(0), 0), null, 'A still held');
  assert.equal(cap.update(frame({ axes: [0.3, 0.2, 0, 0] }), 10), null, 'small stick wobble');
  assert.equal(cap.update(frame({ axes: [0.2, 0.9, 0, 0] }), 20), 'ls.y', 'dominant stick axis');
  const again = new LearnCapture(start);
  again.update(frame(), 0);
  assert.equal(again.update(withButton(0), 10), 'btn.a', 'released and pressed again: binds');
});

test('learn: triggers, trackpad swipe vs tap, gyro axis', () => {
  assert.equal(new LearnCapture(frame()).update(withButton(7, 0.8), 0), 'trig.rt');
  const pad = new LearnCapture(frame());
  assert.equal(pad.update(frame({ touch: [true, false], touchpads: [[0, 0.1], [0, 0]] }), 0), null);
  assert.equal(pad.update(frame({ touch: [true, false], touchpads: [[0.5, 0.15], [0, 0]] }), 50), 'lpad.x');
  const tap = new LearnCapture(frame());
  tap.update(frame({ touch: [false, true], touchpads: [[0, 0], [0.4, 0.4]] }), 0);
  assert.equal(tap.update(frame(), 200), 'rpad.touch');
  const gyro = new LearnCapture(frame());
  for (let i = 0; i <= 30; i += 5) {
    const r = gyro.update(frame({ motion: { at: i, q: pose(0, -35, i), resting: false } }), i);
    if (i < 25) assert.equal(r, null);
    else { assert.equal(r, 'gyro.roll'); break; }
  }
});
