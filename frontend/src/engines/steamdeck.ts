/**
 * Steam Deck / gamepad controls for the VJ deck.
 *
 * Sources, in priority order:
 *  1. Kefka native bridge (ops/kefka/deck-input.py): SSE at http://127.0.0.1:18783/events, used only
 *     when this page is served from 127.0.0.1 / localhost. It carries sticks, triggers, trackpads,
 *     buttons and the gyro orientation, and only while the bridge's own gate says the Macroverse Deck
 *     window has focus with deckhypr in game mode.
 *  2. A standard external gamepad through the Gamepad API (no trackpads / gyro). On a local page the
 *     built-in Deck controller is never read this way: deckhypr owns it outside game mode.
 *
 * Dev override (loopback URLs only, local pages only): ?deckBridge=http://127.0.0.1:PORT/events or
 * localStorage "macroverse-deck-bridge-url"; "off" disables the bridge.
 *
 * Nothing runs until a source appears: no loop, no listeners beyond gamepadconnected and the bridge
 * probe on local pages. Mapping logic lives in steamdeckMapping.ts (pure, tested with node:test).
 */

import '../ui/steamdeckHud.css';
import {
  getVjDeckSurface,
  type VjDeckKey,
  type VjDeckSurface,
  type VjSurfaceSnapshot,
} from './vjController.js';
import {
  ACTION_LABELS,
  BINDABLE_INPUTS,
  CURVES,
  DeckMapper,
  GYRO_ENABLERS,
  INPUT_LABELS,
  LearnCapture,
  allowedModes,
  bankCount,
  buildAutoBindings,
  clamp01,
  clampSensitivity,
  isBuiltInDeckGamepad,
  isContinuousParam,
  listTargets,
  normalizeBridgePayload,
  normalizeGamepad,
  parseMappingConfig,
  parseTarget,
  resolveBindings,
  resolveFocusTarget,
  targetLabel,
  type ActionId,
  type Binding,
  type BindingOverride,
  type DeckId,
  type InputId,
  type LabelContext,
  type MapperEffect,
  type MapperResult,
  type MappingConfig,
  type PadFrame,
  type TargetId,
} from './steamdeckMapping.js';

const CONFIG_KEY = 'macroverse-deck-controls-v1';
const HUD_KEY = 'macroverse-deck-hud-visible';
const BRIDGE_OVERRIDE_KEY = 'macroverse-deck-bridge-url';
const DEFAULT_BRIDGE_URL = 'http://127.0.0.1:18783/events';
/** Native samples older than this are not used. */
const BRIDGE_STALE_MS = 400;
/**
 * The native source is only dropped (re-arming the neutral gate) after this long without a sample.
 * A shader compile can block the page for a second; samples queued meanwhile arrive afterwards and
 * must not find the pad reset with a button half pressed.
 */
const BRIDGE_LOST_MS = 1500;
const BROWSE_PAGE = 10;
/** deckhypr's GAME-mode back paddles type these keys; they must not type or trigger shortcuts here. */
const PADDLE_KEYS = new Set(['F2', 'F4', 'r', 'R', 'l', 'L']);

export type EditorTab = 'inputs' | 'learn' | 'gyro';
export const EDITOR_TABS: EditorTab[] = ['inputs', 'learn', 'gyro'];

export interface EditorState {
  open: boolean;
  tab: EditorTab;
  row: number;
  /** Target picker for one input (Inputs tab, A). */
  picker: { input: InputId; row: number } | null;
}

export interface LearnState {
  target: TargetId;
  capture: LearnCapture;
  startedAt: number;
}

export interface SourceStatus {
  kind: 'native' | 'gamepad' | null;
  label: string;
  note: string;
}

const isLocalPage = (): boolean =>
  typeof location !== 'undefined' && (location.hostname === '127.0.0.1' || location.hostname === 'localhost');

function bridgeUrl(): string | null {
  if (!isLocalPage()) return null;
  let override: string | null = null;
  try {
    override = new URLSearchParams(location.search).get('deckBridge');
    if (override === null) override = localStorage.getItem(BRIDGE_OVERRIDE_KEY);
  } catch {
    override = null;
  }
  if (override === 'off') return null;
  if (override && /^http:\/\/(127\.0\.0\.1|localhost)(:\d{1,5})?\/[\w./-]*$/.test(override)) return override;
  return DEFAULT_BRIDGE_URL;
}

/** EventSource with exponential backoff (EventSource's own 3 s retry would spam a laptop console). */
class BridgeClient {
  url: string;
  es: EventSource | null = null;
  payload: Record<string, unknown> | null = null;
  at = 0;
  state: 'connecting' | 'open' | 'error' = 'connecting';
  backoff = 2000;
  timer: ReturnType<typeof setTimeout> | null = null;
  onData: () => void;

  constructor(url: string, onData: () => void) {
    this.url = url;
    this.onData = onData;
    this.connect();
  }

  connect(): void {
    this.timer = null;
    this.state = 'connecting';
    let es: EventSource;
    try {
      es = new EventSource(this.url);
    } catch {
      this.retry();
      return;
    }
    this.es = es;
    es.onopen = () => {
      this.state = 'open';
      this.backoff = 2000;
    };
    es.onmessage = (ev) => {
      try {
        const data = JSON.parse(ev.data);
        this.payload = data && typeof data === 'object' ? data : null;
        this.at = performance.now();
        this.onData();
      } catch {
        this.payload = null;
      }
    };
    es.onerror = () => {
      es.close();
      if (this.es === es) this.es = null;
      this.payload = null;
      this.state = 'error';
      this.retry();
    };
  }

  retry(): void {
    if (this.timer) return;
    this.timer = setTimeout(() => this.connect(), this.backoff);
    this.backoff = Math.min(30000, this.backoff * 2);
  }

  fresh(now: number): boolean {
    return this.payload !== null && now - this.at < BRIDGE_STALE_MS;
  }
}

export interface DeckHudApi {
  update(engine: SteamDeckEngine): void;
  rebuild(engine: SteamDeckEngine): void;
}

export class SteamDeckEngine {
  config: MappingConfig = loadConfig();
  mapper = new DeckMapper();
  focus: DeckId = 'A';
  bank = 0;
  cursor = -1;
  cursorName = '';
  hudVisible = loadHudVisible();
  editor: EditorState = { open: false, tab: 'inputs', row: 0, picker: null };
  learn: LearnState | null = null;
  screenPick = false;
  bindings: Partial<Record<InputId, Binding>> = {};
  autoKey = '';
  frame: PadFrame | null = null;
  result: MapperResult | null = null;
  snap: VjSurfaceSnapshot | null = null;
  source: SourceStatus = { kind: null, label: 'No controller', note: '' };
  bridge: BridgeClient | null = null;
  flash = { text: '', at: 0 };
  raf = 0;
  lastStepAt = 0;
  lastNativeAt = 0;
  snapAt = 0;
  hud: DeckHudApi | null = null;
  focusShown = false;
  /** Bumped when bindings / config / editor structure change (HUD rebuilds its rows). */
  revision = 0;

  start(): void {
    const url = bridgeUrl();
    if (url) this.bridge = new BridgeClient(url, () => this.onBridgeMessage());
    window.addEventListener('gamepadconnected', () => this.ensureLoop());
    window.addEventListener('macroverse-vj-surface', () => this.ensureLoop());
    // Losing focus re-arms the neutral gate: nothing held across a window switch fires on return.
    window.addEventListener('blur', () => this.mapper.reset());
    window.addEventListener('keydown', (ev) => {
      if (this.source.kind !== 'native' || !PADDLE_KEYS.has(ev.key)) return;
      if (ev.ctrlKey || ev.metaKey || ev.altKey) return;
      const tag = (ev.target as HTMLElement | null)?.tagName || '';
      if (tag === 'INPUT' || tag === 'SELECT' || tag === 'TEXTAREA') return;
      ev.preventDefault();
    }, true);
    window.addEventListener('keydown', (ev) => {
      if (ev.key !== 'Escape') return;
      if (this.learn || this.screenPick) { this.cancelLearn(); ev.stopPropagation(); }
      else if (this.editor.open) { this.setEditorOpen(false); ev.stopPropagation(); }
    }, true);
    this.ensureLoop();
  }

  // ---- loop -------------------------------------------------------------------------------

  shouldRun(): boolean {
    const now = performance.now();
    // A bridge that only reports a closed gate (Deck window in the background) does not keep it awake.
    return (
      (this.lastNativeAt > 0 && now - this.lastNativeAt < BRIDGE_LOST_MS) ||
      !!this.findGamepad() ||
      this.hudVisible ||
      this.editor.open ||
      !!this.learn ||
      this.screenPick
    );
  }

  ensureLoop(): void {
    if (this.raf || typeof requestAnimationFrame === 'undefined' || !this.shouldRun()) return;
    this.raf = requestAnimationFrame((t) => this.tick(t));
  }

  findGamepad(): Gamepad | null {
    if (typeof navigator === 'undefined' || !navigator.getGamepads) return null;
    const local = isLocalPage();
    for (const gp of Array.from(navigator.getGamepads())) {
      if (gp && gp.connected && gp.mapping === 'standard' && !(local && isBuiltInDeckGamepad(gp.id))) return gp;
    }
    return null;
  }

  /** Native frame from the latest bridge payload, if fresh and the bridge's gate is open. */
  nativeFrame(now: number): PadFrame | null {
    const payload = this.bridge?.fresh(now) ? this.bridge.payload : null;
    const native = payload ? normalizeBridgePayload(payload) : null;
    if (native) {
      this.source = {
        kind: 'native',
        label: 'Kefka Steam Deck · native',
        note: payload?.motionConnected === true ? 'gyro live' : 'no gyro data',
      };
    }
    return native;
  }

  /**
   * Every bridge sample (50 Hz) goes through the mapper as it arrives, so a quick tap is never lost
   * between animation frames and rates integrate over real time even if the page renders slowly.
   */
  onBridgeMessage(): void {
    if (typeof document !== 'undefined' && document.hidden) return;
    const now = performance.now();
    const frame = this.nativeFrame(now);
    if (frame) {
      this.lastNativeAt = now;
      this.process(frame, now);
    } else {
      // The bridge is alive and says the gate is closed (another window focused): drop at once.
      this.lastNativeAt = 0;
      if (!this.readFrame(now) && this.frame) this.process(null, now);
    }
    this.ensureLoop();
  }

  readFrame(now: number): PadFrame | null {
    const payload = this.bridge?.fresh(now) ? this.bridge.payload : null;
    const gp = this.findGamepad();
    if (gp) {
      this.source = { kind: 'gamepad', label: 'Controller · ' + gp.id.replace(/\s*\(.*$/, '').slice(0, 40), note: 'no trackpads / gyro' };
      return normalizeGamepad(gp);
    }
    let note = '';
    if (payload && payload.connected === true) note = String(payload.nativeReason || 'Deck input not allowed for this window');
    else if (this.bridge?.state === 'open') note = 'Deck bridge up, no controller';
    else if (this.bridge?.state === 'connecting') note = 'Deck bridge connecting';
    else if (this.bridge) note = 'Deck bridge not reachable';
    this.source = { kind: null, label: 'No controller', note };
    return null;
  }

  /** One controller sample through the mapper, then HUD/editor/learn or the VJ deck. */
  process(frame: PadFrame | null, now: number): void {
    const dt = Math.max(0, Math.min(0.1, (now - this.lastStepAt) / 1000));
    this.lastStepAt = now;
    const surface = getVjDeckSurface();
    if (now - this.snapAt > 30 || !this.snap) this.refreshSnapshot(now);
    this.frame = frame;
    this.refreshBindings();
    const result = this.mapper.step(frame, { now, dt, bindings: this.bindings, gyro: this.config.gyro });
    this.result = result;

    const hasPad = !!frame;
    if (surface && (hasPad || this.hudVisible) !== this.focusShown) {
      this.focusShown = hasPad || this.hudVisible;
      surface.setPadFocus(this.focusShown ? this.focus : null);
      if (!this.focusShown) surface.setBrowseCursor(null);
    }

    if (result.ready) this.handleReserved(result);
    if (this.learn) this.stepLearn(frame, now);
    else if (this.editor.open) this.navigateEditor(result);
    else if (surface && this.snap?.active) {
      for (const e of result.effects) this.applyEffect(e, surface);
      // Discrete actions read deck state (auto VJ, loaded shader): keep it current within a burst.
      if (result.effects.some((e) => e.type === 'action')) this.refreshSnapshot(now);
    }
  }

  refreshSnapshot(now = performance.now()): void {
    const surface = getVjDeckSurface();
    this.snap = surface ? surface.snapshot() : null;
    this.snapAt = now;
  }

  tick(_t: number): void {
    this.raf = 0;
    const now = performance.now();
    try {
      const hidden = typeof document !== 'undefined' && document.hidden;
      const holding = this.lastNativeAt > 0 && now - this.lastNativeAt < BRIDGE_LOST_MS;
      if (!hidden && (this.nativeFrame(now) || holding)) {
        // Native frames are processed as they arrive (or are about to, after a stall); keep the
        // HUD's view of the deck current.
        if (now - this.snapAt > 30) this.refreshSnapshot(now);
      } else {
        this.lastNativeAt = 0;
        this.process(hidden ? null : this.readFrame(now), now);
      }
      this.hud?.update(this);
    } catch (err) {
      console.error('[Deck] frame error:', err);
    }
    if (this.shouldRun()) this.raf = requestAnimationFrame((tt) => this.tick(tt));
    else this.hud?.update(this);
  }

  // ---- bindings ---------------------------------------------------------------------------

  labelContext(): LabelContext {
    const params = (d: DeckId) => (this.snap?.decks[d].params ?? []).map((p) => ({ name: p.name, type: p.type }));
    return { focus: this.focus, params: { A: params('A'), B: params('B') } };
  }

  focusedParams() {
    return (this.snap?.decks[this.focus].params ?? []).map((p) => ({ name: p.name, type: p.type }));
  }

  banks(): number {
    return bankCount(this.focusedParams());
  }

  refreshBindings(force = false): void {
    const params = this.focusedParams();
    const banks = bankCount(params);
    if (this.bank >= banks) this.bank = 0;
    const key = [this.focus, this.bank, this.config.gyro.enabler, params.map((p) => p.name + ':' + p.type).join(',')].join('|');
    if (!force && key === this.autoKey) return;
    this.autoKey = key;
    const auto = buildAutoBindings({ focus: this.focus, bank: this.bank, params, gyroEnabler: this.config.gyro.enabler });
    this.bindings = resolveBindings(auto, this.config.overrides);
    this.changed();
  }

  setOverride(input: InputId, patch: BindingOverride | null): void {
    if (patch === null) delete this.config.overrides[input];
    else this.config.overrides[input] = { ...(this.config.overrides[input] || {}), ...patch };
    this.saveConfig();
    this.refreshBindings(true);
  }

  resetAll(): void {
    this.config.overrides = {};
    this.saveConfig();
    this.refreshBindings(true);
    this.say('All controls back to auto');
  }

  saveConfig(): void {
    try {
      localStorage.setItem(CONFIG_KEY, JSON.stringify(this.config));
    } catch {
      /* private mode: mapping lives for this session */
    }
  }

  // ---- effects ----------------------------------------------------------------------------

  /** Structure changed (bindings, editor view, learn state): the HUD rebuilds and repaints together. */
  changed(): void {
    this.revision++;
    this.hud?.update(this);
  }

  say(text: string): void {
    this.flash = { text, at: performance.now() };
  }

  getContinuous(surface: VjDeckSurface, target: TargetId): number | null {
    const p = parseTarget(resolveFocusTarget(target, this.focus));
    if (!p) return null;
    if (p.kind === 'xfade') return surface.getCrossfader();
    if (p.kind === 'param' && p.deck !== 'focus') return surface.getParamNorm(p.deck, p.index);
    if (p.kind === 'mouse' && p.deck !== 'focus') return surface.getMouse(p.deck)[p.axis];
    return null;
  }

  setContinuous(surface: VjDeckSurface, target: TargetId, v: number): void {
    const p = parseTarget(resolveFocusTarget(target, this.focus));
    if (!p) return;
    const nv = clamp01(v);
    if (p.kind === 'xfade') surface.setCrossfader(nv);
    else if (p.kind === 'param' && p.deck !== 'focus') surface.setParamNorm(p.deck, p.index, nv);
    else if (p.kind === 'mouse' && p.deck !== 'focus') surface.setMouse(p.deck, p.axis === 'x' ? nv : null, p.axis === 'y' ? nv : null);
  }

  applyEffect(e: MapperEffect, surface: VjDeckSurface): void {
    if (e.type === 'action') {
      this.runAction(e.target as ActionId, surface);
      return;
    }
    const cur = this.getContinuous(surface, e.target);
    if (cur === null) return;
    if (e.type === 'delta') this.setContinuous(surface, e.target, cur + e.amount);
    else if (e.type === 'set') this.setContinuous(surface, e.target, e.value);
    else if (e.type === 'toggle') {
      if (e.target === 'xfade') surface.cutTo(cur < 0.5 ? 'B' : 'A');
      else this.setContinuous(surface, e.target, cur < 0.5 ? 1 : 0);
    }
  }

  setFocus(deck: DeckId, surface: VjDeckSurface | null = getVjDeckSurface()): void {
    if (this.focus !== deck) this.bank = 0;
    this.focus = deck;
    surface?.setPadFocus(deck);
    this.focusShown = true;
    this.refreshBindings();
    this.say(`Focus deck ${deck}`);
  }

  ensureCursor(): number {
    const count = this.snap?.libraryCount ?? 0;
    if (count <= 0) return -1;
    if (this.cursor < 0 || this.cursor >= count) {
      const idx = this.snap?.decks[this.focus].libraryIndex ?? -1;
      this.cursor = idx >= 0 ? idx : 0;
      this.cursorName = getVjDeckSurface()?.libraryEntry(this.cursor)?.name ?? '';
    }
    return this.cursor;
  }

  moveCursor(delta: number, surface: VjDeckSurface): void {
    const count = this.snap?.libraryCount ?? 0;
    if (count <= 0) return;
    const base = this.ensureCursor();
    this.cursor = (((base + delta) % count) + count) % count;
    surface.setBrowseCursor(this.cursor);
    this.cursorName = surface.libraryEntry(this.cursor)?.name ?? '';
    this.say(`Browse ${this.cursor + 1}/${count} · ${this.cursorName}`);
  }

  runAction(action: ActionId, surface: VjDeckSurface): void {
    const deckFor = (a: string): VjDeckKey => (a.endsWith('.A') ? 'A' : a.endsWith('.B') ? 'B' : this.focus);
    switch (action) {
      case 'browse.prev': this.moveCursor(-1, surface); break;
      case 'browse.next': this.moveCursor(1, surface); break;
      case 'browse.pageUp': this.moveCursor(-BROWSE_PAGE, surface); break;
      case 'browse.pageDown': this.moveCursor(BROWSE_PAGE, surface); break;
      case 'load.A': case 'load.B': case 'load.focus': {
        const idx = this.ensureCursor();
        if (idx < 0) break;
        const deck = deckFor(action);
        surface.loadLibraryIndex(deck, idx);
        surface.setBrowseCursor(idx);
        this.say(`Load → ${deck} · ${surface.libraryEntry(idx)?.name ?? ''}`);
        break;
      }
      case 'focus.A': this.setFocus('A', surface); break;
      case 'focus.B': this.setFocus('B', surface); break;
      case 'focus.toggle': this.setFocus(this.focus === 'A' ? 'B' : 'A', surface); break;
      case 'bank.next': {
        const banks = this.banks();
        this.bank = (this.bank + 1) % banks;
        this.refreshBindings();
        this.say(`Param bank ${this.bank + 1}/${banks}`);
        break;
      }
      case 'deck.prev': case 'deck.next':
        surface.navigateDeck(this.focus, action === 'deck.next' ? 1 : -1);
        this.say(`Deck ${this.focus} ${action === 'deck.next' ? 'next' : 'prev'} shader`);
        break;
      case 'cut.A': case 'cut.B': case 'cut.focus': {
        const deck = deckFor(action);
        surface.cutTo(deck);
        this.say(`Cut to ${deck}`);
        break;
      }
      case 'autoVj.toggle': {
        const on = !(this.snap?.autoVj ?? false);
        surface.setAutoVj(on);
        this.say(`Auto VJ ${on ? 'on' : 'off'}`);
        break;
      }
      case 'mouse.center':
        surface.setMouse(this.focus, 0.5, 0.5);
        this.say(`Deck ${this.focus} mouse centred`);
        break;
      case 'hud.toggle': this.setHudVisible(!this.hudVisible); break;
    }
  }

  // ---- HUD / editor / learn ---------------------------------------------------------------

  async ensureHud(): Promise<void> {
    if (this.hud) return;
    const m = await import('../ui/steamdeckHud.js');
    this.hud = m.createDeckHud(this);
    this.hud.rebuild(this);
    this.hud.update(this);
  }

  setHudVisible(on: boolean): void {
    this.hudVisible = on;
    try { localStorage.setItem(HUD_KEY, on ? '1' : '0'); } catch { /* ignore */ }
    void this.ensureHud().then(() => this.hud?.update(this));
    this.ensureLoop();
  }

  setEditorOpen(open: boolean): void {
    this.editor = { ...this.editor, open, picker: null };
    if (!open) this.cancelLearn();
    this.revision++;
    void this.ensureHud().then(() => this.hud?.update(this));
    this.ensureLoop();
  }

  setEditorTab(tab: EditorTab): void {
    this.editor = { ...this.editor, tab, row: 0, picker: null };
    this.changed();
  }

  /** Rows of the current editor view (inputs, targets or settings). */
  editorRows(): string[] {
    if (this.editor.picker) return this.pickerTargets();
    if (this.editor.tab === 'inputs') return BINDABLE_INPUTS.slice();
    if (this.editor.tab === 'learn') return this.pickerTargets().filter((t) => t !== 'none');
    return ['gyro.enabler', 'gyro.deadband', 'reset.all', 'hud.start'];
  }

  pickerTargets(): TargetId[] {
    return listTargets(this.labelContext()).flatMap((g) => g.targets);
  }

  startLearn(target: TargetId): void {
    this.screenPick = false;
    this.learn = { target, capture: new LearnCapture(this.frame), startedAt: performance.now() };
    this.say(`Learn: move a control for ${targetLabel(target, this.labelContext())}`);
    this.revision++;
    void this.ensureHud().then(() => this.hud?.update(this));
    this.ensureLoop();
  }

  startScreenPick(): void {
    this.screenPick = true;
    this.editor = { ...this.editor, open: false, picker: null };
    this.revision++;
    void this.ensureHud().then(() => this.hud?.update(this));
    this.ensureLoop();
  }

  cancelLearn(): void {
    if (!this.learn && !this.screenPick) return;
    this.learn = null;
    this.screenPick = false;
    this.changed();
  }

  stepLearn(frame: PadFrame | null, now: number): void {
    if (!this.learn) return;
    const input = this.learn.capture.update(frame, now);
    if (!input) return;
    const target = this.learn.target;
    this.learn = null;
    this.setOverride(input, null);
    this.setOverride(input, { target });
    this.say(`Learned ${INPUT_LABELS[input]} → ${targetLabel(target, this.labelContext())}`);
    if (this.editor.open) {
      this.editor = { ...this.editor, tab: 'inputs', row: Math.max(0, BINDABLE_INPUTS.indexOf(input)), picker: null };
    }
    this.changed();
  }

  /** View / Menu: HUD and editor toggles (also cancel learning). */
  handleReserved(r: MapperResult): void {
    if (r.pressed.includes('btn.view')) {
      if (this.learn || this.screenPick) this.cancelLearn();
      else this.setHudVisible(!this.hudVisible);
    }
    if (r.pressed.includes('btn.menu')) {
      if (this.learn || this.screenPick) this.cancelLearn();
      else this.setEditorOpen(!this.editor.open);
    }
  }

  editorActivate(row: number): void {
    const rows = this.editorRows();
    const item = rows[row];
    if (item === undefined) return;
    this.editor.row = row;
    if (this.editor.picker) {
      const input = this.editor.picker.input;
      this.editor.picker = null;
      this.setOverride(input, null);
      this.setOverride(input, { target: item });
      this.say(`${INPUT_LABELS[input]} → ${targetLabel(item, this.labelContext())}`);
      this.changed();
      return;
    }
    if (this.editor.tab === 'inputs') {
      const input = item as InputId;
      const cur = this.bindings[input]?.target ?? 'none';
      const targets = this.pickerTargets();
      this.editor.picker = { input, row: Math.max(0, targets.indexOf(cur)) };
      this.changed();
      return;
    }
    if (this.editor.tab === 'learn') {
      this.startLearn(item);
      return;
    }
    if (item === 'gyro.enabler') this.cycleEnabler(1);
    else if (item === 'gyro.deadband') this.adjustDeadband(0.5);
    else if (item === 'reset.all') this.resetAll();
    else if (item === 'hud.start') this.setHudVisible(!this.hudVisible);
    this.changed();
  }

  cycleEnabler(dir: number): void {
    const i = GYRO_ENABLERS.indexOf(this.config.gyro.enabler);
    this.config.gyro.enabler = GYRO_ENABLERS[(i + dir + GYRO_ENABLERS.length) % GYRO_ENABLERS.length];
    this.mapper.ratchet.reset();
    this.mapper.r3Toggle = false;
    this.saveConfig();
    this.refreshBindings(true);
  }

  adjustDeadband(delta: number): void {
    const v = Math.round((this.config.gyro.deadband + delta) * 10) / 10;
    this.config.gyro.deadband = v > 5 ? 0 : v < 0 ? 5 : v;
    this.saveConfig();
  }

  adjustSensitivity(input: InputId, delta: number): void {
    const b = this.bindings[input];
    if (!b || b.target === 'none') return;
    this.setOverride(input, { sensitivity: clampSensitivity(b.sensitivity + delta) });
  }

  cycleMode(input: InputId): void {
    const b = this.bindings[input];
    if (!b) return;
    const modes = allowedModes(input, b.target);
    if (modes.length < 2) return;
    this.setOverride(input, { mode: modes[(modes.indexOf(b.mode) + 1) % modes.length] });
  }

  toggleInvert(input: InputId): void {
    const b = this.bindings[input];
    if (b) this.setOverride(input, { invert: !b.invert });
  }

  cycleCurve(input: InputId): void {
    const b = this.bindings[input];
    if (b) this.setOverride(input, { curve: CURVES[(CURVES.indexOf(b.curve) + 1) % CURVES.length] });
  }

  navigateEditor(r: MapperResult): void {
    const rows = this.editorRows();
    const p = (id: InputId) => r.pressed.includes(id);
    const picker = this.editor.picker;
    let row = picker ? picker.row : this.editor.row;
    const move = (d: number) => {
      row = Math.max(0, Math.min(rows.length - 1, row + d));
      if (picker) picker.row = row;
      else this.editor.row = row;
    };
    if (p('dpad.up')) move(-1);
    if (p('dpad.down')) move(1);
    if (picker) {
      if (p('dpad.left')) move(-8);
      if (p('dpad.right')) move(8);
      if (p('btn.a')) this.editorActivate(picker.row);
      else if (p('btn.b')) { this.editor.picker = null; this.changed(); }
      return;
    }
    const item = rows[this.editor.row];
    if (p('btn.lb') || p('btn.rb')) {
      const i = EDITOR_TABS.indexOf(this.editor.tab);
      this.setEditorTab(EDITOR_TABS[(i + (p('btn.rb') ? 1 : EDITOR_TABS.length - 1)) % EDITOR_TABS.length]);
      return;
    }
    if (p('btn.b')) { this.setEditorOpen(false); return; }
    if (p('btn.a')) { this.editorActivate(this.editor.row); return; }
    if (this.editor.tab === 'inputs' && item) {
      const input = item as InputId;
      if (p('dpad.left')) this.adjustSensitivity(input, -0.1);
      if (p('dpad.right')) this.adjustSensitivity(input, 0.1);
      if (p('btn.x')) this.cycleMode(input);
      if (p('btn.y')) { this.setOverride(input, null); this.say(`${INPUT_LABELS[input]} back to auto`); }
      if (p('btn.l3')) this.toggleInvert(input);
      if (p('btn.r3')) this.cycleCurve(input);
    } else if (this.editor.tab === 'gyro' && item) {
      const dir = p('dpad.left') ? -1 : p('dpad.right') ? 1 : 0;
      if (dir && item === 'gyro.enabler') this.cycleEnabler(dir);
      if (dir && item === 'gyro.deadband') this.adjustDeadband(dir * 0.5);
      if (dir) this.changed();
    }
  }

  /** Short human description of an input's binding for the HUD. */
  bindingText(input: InputId): string {
    const b = this.bindings[input];
    if (!b || b.target === 'none') return '—';
    return targetLabel(b.target, this.labelContext());
  }

  bankText(): string {
    const params = this.focusedParams();
    const banks = bankCount(params);
    const count = params.filter(isContinuousParam).length;
    return banks > 1 ? `bank ${this.bank + 1}/${banks}` : count ? `${count} params` : 'no params';
  }

  actionLabel(id: ActionId): string {
    return ACTION_LABELS[id];
  }
}

function loadConfig(): MappingConfig {
  try {
    return parseMappingConfig(JSON.parse(localStorage.getItem(CONFIG_KEY) || 'null'));
  } catch {
    return parseMappingConfig(null);
  }
}

function loadHudVisible(): boolean {
  try {
    return localStorage.getItem(HUD_KEY) === '1';
  } catch {
    return false;
  }
}

let engine: SteamDeckEngine | null = null;

/** Idempotent. Called when the VJ deck is built; cheap when no controller is ever connected. */
export function initSteamDeckControls(): SteamDeckEngine {
  if (engine) {
    engine.ensureLoop();
    return engine;
  }
  engine = new SteamDeckEngine();
  // Console handle for on-device diagnosis (window.__macroverseDeck.result / .source / .bindings).
  (window as unknown as { __macroverseDeck?: SteamDeckEngine }).__macroverseDeck = engine;
  engine.start();
  if (engine.hudVisible) void engine.ensureHud();
  void import('../init/commandRegistry.js').then(({ registerCommand }) => {
    registerCommand({
      id: 'vj.deckControls.hud',
      label: 'Deck controls: show / hide HUD',
      category: 'VJ',
      keywords: ['steam deck', 'gamepad', 'controller', 'gyro', 'kefka'],
      run: () => engine?.setHudVisible(!engine.hudVisible),
    });
    registerCommand({
      id: 'vj.deckControls.editor',
      label: 'Deck controls: mapping editor',
      category: 'VJ',
      keywords: ['steam deck', 'gamepad', 'controller', 'learn', 'mapping', 'gyro'],
      run: () => engine?.setEditorOpen(true),
    });
  }).catch(() => {});
  return engine;
}

export function getSteamDeckEngine(): SteamDeckEngine | null {
  return engine;
}
