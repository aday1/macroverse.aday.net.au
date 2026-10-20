/**
 * Deck controls HUD (View) and mapping editor (Menu) for the VJ deck. Sized for the Steam Deck's
 * 1280x800 screen (16px+ text, 40px touch rows) and usable with a mouse on a laptop.
 * The HUD is display-only (pointer-events pass through except its two buttons); the editor is a modal
 * driven by D-pad + A/B (+ X/Y/L3/R3/LB/RB shortcuts) or touch.
 */

import {
  BINDABLE_INPUTS,
  GYRO_ENABLER_LABELS,
  INPUT_LABELS,
  allowedModes,
  enablerInput,
  inputKind,
  listTargets,
  targetLabel,
  type Binding,
  type InputId,
  type TargetId,
} from '../engines/steamdeckMapping.js';
import { EDITOR_TABS, type DeckHudApi, type EditorTab, type SteamDeckEngine } from '../engines/steamdeck.js';

const LEFT_INPUTS: InputId[] = ['trig.lt', 'btn.lb', 'ls.x', 'ls.y', 'btn.l3', 'dpad.left', 'dpad.right', 'dpad.up', 'dpad.down', 'lpad.x', 'lpad.y', 'lpad.touch'];
const RIGHT_INPUTS: InputId[] = ['trig.rt', 'btn.rb', 'rs.x', 'rs.y', 'btn.r3', 'btn.a', 'btn.b', 'btn.x', 'btn.y', 'rpad.x', 'rpad.y', 'rpad.touch'];
const GYRO_ROWS: InputId[] = ['gyro.yaw', 'gyro.pitch', 'gyro.roll'];

const TAB_LABELS: Record<EditorTab, string> = { inputs: 'Inputs', learn: 'Learn', gyro: 'Gyro & general' };

const MODE_SHORT: Record<string, string> = {
  press: '', toggle: 'toggle', momentary: 'hold', relative: 'rate', absolute: 'abs', ratchet: 'gyro',
};

function h<K extends keyof HTMLElementTagNameMap>(tag: K, cls?: string, text?: string): HTMLElementTagNameMap[K] {
  const el = document.createElement(tag);
  if (cls) el.className = cls;
  if (text !== undefined) el.textContent = text;
  return el;
}

function button(cls: string, text: string, onClick: () => void, title?: string): HTMLButtonElement {
  const b = h('button', cls, text);
  b.type = 'button';
  if (title) b.title = title;
  b.addEventListener('click', (ev) => {
    ev.preventDefault();
    ev.stopPropagation();
    onClick();
  });
  return b;
}

/** Normalised meter value: [-1..1] bipolar or [0..1] unipolar. */
function meterValue(input: InputId, v: number): { v: number; bipolar: boolean } {
  const kind = inputKind(input);
  if (kind === 'gyro') return { v: Math.max(-1, Math.min(1, v / 60)), bipolar: true };
  if (kind === 'stick' || kind === 'pad') return { v: Math.max(-1, Math.min(1, v)), bipolar: true };
  return { v: Math.max(0, Math.min(1, v)), bipolar: false };
}

function setMeter(fill: HTMLElement, input: InputId, raw: number): void {
  const { v, bipolar } = meterValue(input, raw);
  if (bipolar) {
    const w = Math.abs(v) * 50;
    fill.style.left = (v < 0 ? 50 - w : 50) + '%';
    fill.style.width = w + '%';
  } else {
    fill.style.left = '0%';
    fill.style.width = v * 100 + '%';
  }
}

function isHot(input: InputId, raw: number): boolean {
  const kind = inputKind(input);
  if (kind === 'gyro') return Math.abs(raw) > 0.5;
  if (kind === 'stick') return Math.abs(raw) > 0.15;
  if (kind === 'pad') return Math.abs(raw) > 0.02;
  return raw > 0.3;
}

function bindingTag(b: Binding | undefined): string {
  if (!b || b.target === 'none') return '';
  const parts: string[] = [];
  const m = MODE_SHORT[b.mode];
  if (m) parts.push(m);
  if (b.invert) parts.push('inv');
  if (b.sensitivity !== 1) parts.push('×' + b.sensitivity.toFixed(1));
  if (b.curve !== 'linear' && b.mode !== 'press') parts.push(b.curve);
  return parts.join(' · ');
}

interface Row {
  input: InputId;
  root: HTMLElement;
  fill: HTMLElement;
  target: HTMLElement;
  tag: HTMLElement;
}

export function createDeckHud(engine: SteamDeckEngine): DeckHudApi {
  // ---------------------------------------------------------------- HUD (View)
  const hud = h('div', 'mv-deckhud');
  hud.setAttribute('aria-label', 'Deck controls HUD');
  const head = h('div', 'mv-deckhud__head');
  const title = h('span', 'mv-deckhud__title', 'Deck controls');
  const sourceEl = h('span', 'mv-deckhud__source');
  const focusEl = h('span', 'mv-deckhud__focus');
  const gyroEl = h('span', 'mv-deckhud__gyro');
  const headBtns = h('span', 'mv-deckhud__btns');
  headBtns.appendChild(button('mv-deckhud__btn', 'Mapping ☰', () => engine.setEditorOpen(true), 'Mapping editor (Menu)'));
  headBtns.appendChild(button('mv-deckhud__btn', 'Hide ⧉', () => engine.setHudVisible(false), 'Hide HUD (View)'));
  head.append(title, sourceEl, focusEl, gyroEl, headBtns);

  const cols = h('div', 'mv-deckhud__cols');
  const left = h('div', 'mv-deckhud__col');
  const mid = h('div', 'mv-deckhud__col mv-deckhud__col--mid');
  const right = h('div', 'mv-deckhud__col');
  cols.append(left, mid, right);
  hud.append(head, cols);

  // Middle column: mixer state.
  const deckLine = h('div', 'mv-deckhud__decks');
  const deckAEl = h('div', 'mv-deckhud__deck mv-deckhud__deck--a');
  const deckBEl = h('div', 'mv-deckhud__deck mv-deckhud__deck--b');
  deckLine.append(deckAEl, deckBEl);
  const xfadeWrap = h('div', 'mv-deckhud__xfade');
  const xfadeA = h('span', 'mv-deckhud__xfade-end mv-deckhud__xfade-end--a', 'A');
  const xfadeTrack = h('span', 'mv-deckhud__xfade-track');
  const xfadeKnob = h('i', 'mv-deckhud__xfade-knob');
  xfadeTrack.appendChild(xfadeKnob);
  const xfadeB = h('span', 'mv-deckhud__xfade-end mv-deckhud__xfade-end--b', 'B');
  const xfadeVal = h('span', 'mv-deckhud__xfade-val');
  xfadeWrap.append(xfadeA, xfadeTrack, xfadeB, xfadeVal);
  const browseEl = h('div', 'mv-deckhud__browse');
  const autoEl = h('div', 'mv-deckhud__auto');
  const gyroBox = h('div', 'mv-deckhud__gyrobox');
  const flashEl = h('div', 'mv-deckhud__flash');
  const noteEl = h('div', 'mv-deckhud__note');
  mid.append(deckLine, xfadeWrap, browseEl, autoEl, gyroBox, flashEl, noteEl);

  let rows: Row[] = [];

  function makeRow(input: InputId, compact = false): Row {
    const root = h('div', 'mv-deckhud__row' + (compact ? ' mv-deckhud__row--gyro' : ''));
    const key = h('span', 'mv-deckhud__key', INPUT_LABELS[input]);
    const meter = h('span', 'mv-deckhud__meter');
    const fill = h('i');
    meter.appendChild(fill);
    const target = h('span', 'mv-deckhud__tgt');
    const tag = h('span', 'mv-deckhud__tag');
    root.append(key, meter, target, tag);
    return { input, root, fill, target, tag };
  }

  function buildHudRows(): void {
    left.replaceChildren();
    right.replaceChildren();
    gyroBox.replaceChildren();
    rows = [];
    for (const input of LEFT_INPUTS) { const r = makeRow(input); rows.push(r); left.appendChild(r.root); }
    for (const input of RIGHT_INPUTS) { const r = makeRow(input); rows.push(r); right.appendChild(r.root); }
    const gyroHead = h('div', 'mv-deckhud__subhead', 'Gyro');
    gyroBox.appendChild(gyroHead);
    for (const input of GYRO_ROWS) { const r = makeRow(input, true); rows.push(r); gyroBox.appendChild(r.root); }
  }

  function rowTargetText(input: InputId): { text: string; source: string } {
    const enabler = enablerInput(engine.config.gyro.enabler);
    if (input === enabler) return { text: 'Gyro enable' + (engine.config.gyro.enabler === 'r3-toggle' ? ' (toggle)' : ''), source: 'gyro' };
    const b = engine.bindings[input];
    if (!b) return { text: '—', source: '' };
    let text = engine.bindingText(input);
    if (b.target === 'xfade' && b.mode === 'relative') text += b.invert ? ' ◂ A' : ' ▸ B';
    return { text, source: b.source };
  }

  // ---------------------------------------------------------------- Editor (Menu)
  const edit = h('div', 'mv-deckedit');
  edit.setAttribute('role', 'dialog');
  edit.setAttribute('aria-label', 'Deck controls mapping');
  const panel = h('div', 'mv-deckedit__panel');
  edit.appendChild(panel);
  edit.addEventListener('pointerdown', (ev) => {
    if (ev.target === edit) engine.setEditorOpen(false);
  });

  // Learn / screen-pick overlays.
  const learnCard = h('div', 'mv-decklearn');
  const pickBanner = h('div', 'mv-deckpick');

  let editRows: HTMLElement[] = [];
  let editLive: { input: InputId; fill: HTMLElement }[] = [];
  let lastSel = -1;
  let lastRevision = -1;

  function legend(text: string): HTMLElement {
    return h('div', 'mv-deckedit__legend', text);
  }

  function buildEditor(): void {
    panel.replaceChildren();
    editRows = [];
    editLive = [];
    lastSel = -1;
    const ed = engine.editor;
    const headEl = h('div', 'mv-deckedit__head');
    headEl.appendChild(h('span', 'mv-deckedit__title', 'Deck mapping'));
    const tabs = h('div', 'mv-deckedit__tabs');
    for (const t of EDITOR_TABS) {
      const b = button('mv-deckedit__tab' + (t === ed.tab && !ed.picker ? ' is-active' : ''), TAB_LABELS[t], () => engine.setEditorTab(t));
      tabs.appendChild(b);
    }
    headEl.appendChild(tabs);
    headEl.appendChild(h('span', 'mv-deckedit__hint', 'LB / RB tabs'));
    headEl.appendChild(button('mv-deckedit__close', '✕', () => engine.setEditorOpen(false), 'Close (Menu / B)'));
    panel.appendChild(headEl);

    const body = h('div', 'mv-deckedit__body');
    panel.appendChild(body);
    const ctx = engine.labelContext();

    if (ed.picker) {
      const input = ed.picker.input;
      const ph = h('div', 'mv-deckedit__pickhead');
      ph.appendChild(h('span', '', `Bind ${INPUT_LABELS[input]} to…`));
      ph.appendChild(button('mv-deckedit__chip', '← Back (B)', () => { engine.editor.picker = null; rebuild(); }));
      body.appendChild(ph);
      const current = engine.bindings[input]?.target ?? 'none';
      let i = 0;
      for (const g of listTargets(ctx)) {
        body.appendChild(h('div', 'mv-deckedit__group', g.label));
        for (const t of g.targets) {
          const idx = i++;
          const r = button('mv-deckedit__row mv-deckedit__row--target' + (t === current ? ' is-current' : ''), targetLabel(t, ctx), () => engine.editorActivate(idx));
          body.appendChild(r);
          editRows.push(r);
        }
      }
      panel.appendChild(legend('D-pad ▴▾ move · ◂▸ jump · A bind · B back'));
      return;
    }

    if (ed.tab === 'inputs') {
      const colHead = h('div', 'mv-deckedit__row mv-deckedit__row--colhead');
      for (const label of ['Control', 'Live', 'Target', 'Mode', 'Sensitivity', 'Invert', 'Curve', '', '']) colHead.appendChild(h('span', '', label));
      body.appendChild(colHead);
      BINDABLE_INPUTS.forEach((input, idx) => {
        const b = engine.bindings[input];
        const row = h('div', 'mv-deckedit__row mv-deckedit__row--input' + (b?.source === 'user' ? ' is-user' : ''));
        row.addEventListener('click', () => { engine.editor.row = idx; });
        row.appendChild(h('span', 'mv-deckedit__input', INPUT_LABELS[input]));
        const meter = h('span', 'mv-deckhud__meter');
        const fill = h('i');
        meter.appendChild(fill);
        row.appendChild(meter);
        editLive.push({ input, fill });
        const { text } = rowTargetText(input);
        row.appendChild(button('mv-deckedit__field mv-deckedit__field--target', text, () => { engine.editor.row = idx; engine.editorActivate(idx); }, 'Choose target (A)'));
        const modes = b ? allowedModes(input, b.target) : [];
        row.appendChild(button('mv-deckedit__field', b && b.target !== 'none' ? (MODE_SHORT[b.mode] || 'press') : '—', () => engine.cycleMode(input), modes.length > 1 ? 'Cycle mode (X)' : 'Fixed for this target'));
        const sens = h('span', 'mv-deckedit__sens');
        sens.appendChild(button('mv-deckedit__step', '−', () => engine.adjustSensitivity(input, -0.1), 'Less (D-pad ◂)'));
        const tunable = !!b && b.target !== 'none' && b.mode !== 'press';
        sens.appendChild(h('span', 'mv-deckedit__sensval', tunable ? b.sensitivity.toFixed(2) : '—'));
        sens.appendChild(button('mv-deckedit__step', '+', () => engine.adjustSensitivity(input, 0.1), 'More (D-pad ▸)'));
        row.appendChild(sens);
        row.appendChild(button('mv-deckedit__field' + (b?.invert ? ' is-on' : ''), b?.invert ? 'inv' : 'norm', () => engine.toggleInvert(input), 'Invert (L3)'));
        row.appendChild(button('mv-deckedit__field', tunable ? b.curve : '—', () => engine.cycleCurve(input), 'Curve (R3)'));
        row.appendChild(h('span', 'mv-deckedit__src', b?.source === 'user' ? 'USER' : 'AUTO'));
        row.appendChild(button('mv-deckedit__step', '↺', () => engine.setOverride(input, null), 'Back to auto (Y)'));
        body.appendChild(row);
        editRows.push(row);
      });
      panel.appendChild(legend('D-pad ▴▾ row · ◂▸ sensitivity · A target · X mode · L3 invert · R3 curve · Y auto · B close'));
      return;
    }

    if (ed.tab === 'learn') {
      const top = h('div', 'mv-deckedit__pickhead');
      top.appendChild(h('span', '', 'Pick a target, then move a Deck control to bind it.'));
      top.appendChild(button('mv-deckedit__chip', 'Pick on screen…', () => engine.startScreenPick(), 'Tap a param slider, the crossfader or Auto VJ'));
      body.appendChild(top);
      const boundBy = new Map<TargetId, string[]>();
      for (const input of BINDABLE_INPUTS) {
        const b = engine.bindings[input];
        if (!b || b.target === 'none') continue;
        const list = boundBy.get(b.target) || [];
        list.push(INPUT_LABELS[input]);
        boundBy.set(b.target, list);
      }
      let i = 0;
      for (const g of listTargets(ctx)) {
        const targets = g.targets.filter((t) => t !== 'none');
        if (!targets.length) continue;
        body.appendChild(h('div', 'mv-deckedit__group', g.label));
        for (const t of targets) {
          const idx = i++;
          const r = button('mv-deckedit__row mv-deckedit__row--target', '', () => { engine.editor.row = idx; engine.editorActivate(idx); });
          r.appendChild(h('span', 'mv-deckedit__tl', targetLabel(t, ctx)));
          r.appendChild(h('span', 'mv-deckedit__bound', (boundBy.get(t) || []).join(', ')));
          r.appendChild(h('span', 'mv-deckedit__learnbtn', 'Learn'));
          body.appendChild(r);
          editRows.push(r);
        }
      }
      panel.appendChild(legend('D-pad ▴▾ move · A learn · then move a stick, trigger, pad, button or tilt · View / Menu cancel'));
      return;
    }

    // Gyro & general.
    const g = engine.config.gyro;
    const settings: { label: string; value: string; act: () => void; dec?: () => void; inc?: () => void }[] = [
      { label: 'Gyro enabler', value: GYRO_ENABLER_LABELS[g.enabler], act: () => engine.cycleEnabler(1), dec: () => engine.cycleEnabler(-1), inc: () => engine.cycleEnabler(1) },
      { label: 'Gyro deadband', value: g.deadband.toFixed(1) + '°', act: () => engine.adjustDeadband(0.5), dec: () => engine.adjustDeadband(-0.5), inc: () => engine.adjustDeadband(0.5) },
      { label: 'Reset every control to auto', value: Object.keys(engine.config.overrides).length + ' overrides', act: () => engine.resetAll() },
      { label: 'Show HUD', value: engine.hudVisible ? 'on' : 'off', act: () => engine.setHudVisible(!engine.hudVisible) },
    ];
    settings.forEach((s, idx) => {
      const row = h('div', 'mv-deckedit__row mv-deckedit__row--setting');
      row.addEventListener('click', () => { engine.editor.row = idx; });
      row.appendChild(h('span', 'mv-deckedit__input', s.label));
      row.appendChild(s.dec ? button('mv-deckedit__step', '◂', () => { s.dec!(); rebuild(); }) : h('span'));
      row.appendChild(button('mv-deckedit__field mv-deckedit__field--wide', s.value, () => { s.act(); rebuild(); }));
      row.appendChild(s.inc ? button('mv-deckedit__step', '▸', () => { s.inc!(); rebuild(); }) : h('span'));
      body.appendChild(row);
      editRows.push(row);
    });
    const live = h('div', 'mv-deckedit__gyrolive');
    for (const input of GYRO_ROWS) {
      const r = h('div', 'mv-deckedit__row mv-deckedit__row--gyro');
      r.appendChild(h('span', 'mv-deckedit__input', INPUT_LABELS[input]));
      const meter = h('span', 'mv-deckhud__meter');
      const fill = h('i');
      meter.appendChild(fill);
      r.appendChild(meter);
      r.appendChild(h('span', 'mv-deckedit__tl', engine.bindingText(input)));
      editLive.push({ input, fill });
      live.appendChild(r);
    }
    body.appendChild(live);
    panel.appendChild(legend('D-pad ▴▾ row · ◂▸ change · A apply · B close · the gyro only moves things while the enabler is held'));
  }

  // ---------------------------------------------------------------- screen pick (learn from the page)
  const onPickPointer = (ev: PointerEvent): void => {
    if (!engine.screenPick) return;
    const el = (ev.target as Element | null)?.closest?.('[data-deck-target]') as HTMLElement | null;
    if (!el || el.closest('.mv-deckpick, .mv-deckhud, .mv-deckedit')) return;
    ev.preventDefault();
    ev.stopPropagation();
    engine.startLearn(el.dataset.deckTarget || 'none');
  };
  let pickBound = false;

  function rebuild(): void {
    lastRevision = engine.revision;
    buildHudRows();
    if (engine.editor.open) buildEditor();
    // Learn card.
    learnCard.replaceChildren();
    if (engine.learn) {
      learnCard.appendChild(h('div', 'mv-decklearn__k', 'Learn'));
      learnCard.appendChild(h('div', 'mv-decklearn__t', targetLabel(engine.learn.target, engine.labelContext())));
      learnCard.appendChild(h('div', 'mv-decklearn__s', 'Move a stick, trigger, trackpad, button or tilt the Deck'));
      learnCard.appendChild(button('mv-deckedit__chip', 'Cancel (View / Menu)', () => engine.cancelLearn()));
    }
    pickBanner.replaceChildren();
    if (engine.screenPick) {
      pickBanner.appendChild(h('span', '', 'Tap a deck param, the crossfader or Auto VJ to learn it'));
      pickBanner.appendChild(button('mv-deckedit__chip', 'Cancel', () => engine.cancelLearn()));
    }
    if (engine.screenPick && !pickBound) {
      document.addEventListener('pointerdown', onPickPointer, true);
      pickBound = true;
    } else if (!engine.screenPick && pickBound) {
      document.removeEventListener('pointerdown', onPickPointer, true);
      pickBound = false;
    }
    document.body.classList.toggle('mv-deck-picking', engine.screenPick);
  }

  function attach(el: HTMLElement, on: boolean): void {
    if (on && !el.isConnected) document.body.appendChild(el);
    else if (!on && el.isConnected) el.remove();
  }

  function update(e: SteamDeckEngine): void {
    if (e.revision !== lastRevision) rebuild();
    // While picking a target on the page the HUD steps aside so the deck controls can be seen and tapped.
    attach(hud, e.hudVisible && !e.screenPick);
    attach(edit, e.editor.open);
    attach(learnCard, !!e.learn);
    attach(pickBanner, e.screenPick);
    const values = e.result?.values ?? {};
    const now = performance.now();

    if (e.hudVisible) {
      const src = e.source;
      sourceEl.textContent = (src.kind ? '● ' : '○ ') + src.label + (src.note ? ' · ' + src.note : '');
      sourceEl.classList.toggle('is-live', !!src.kind);
      const ready = e.result?.ready ?? false;
      const snap = e.snap;
      focusEl.replaceChildren();
      focusEl.appendChild(document.createTextNode('Focus '));
      const fa = h('b', 'mv-deckhud__fdeck mv-deckhud__fdeck--a' + (e.focus === 'A' ? ' is-on' : ''), 'A');
      const fb = h('b', 'mv-deckhud__fdeck mv-deckhud__fdeck--b' + (e.focus === 'B' ? ' is-on' : ''), 'B');
      focusEl.append(fa, fb, document.createTextNode(' ' + e.bankText()));
      const gy = e.result?.gyro;
      const enabler = GYRO_ENABLER_LABELS[e.config.gyro.enabler];
      gyroEl.textContent = 'Gyro · ' + (gy?.active ? 'ACTIVE' : gy?.available ? enabler : 'no gyro');
      gyroEl.classList.toggle('is-on', !!gy?.active);

      for (const r of rows) {
        const raw = values[r.input] ?? 0;
        setMeter(r.fill, r.input, raw);
        const { text, source } = rowTargetText(r.input);
        if (r.target.textContent !== text) r.target.textContent = text;
        let tag = source === 'gyro' ? 'GYRO' : source === 'user' ? 'USER' : source === 'auto' ? bindingTag(e.bindings[r.input]) || 'auto' : '';
        if (inputKind(r.input) === 'gyro' && e.result?.gyro.active) tag = (raw >= 0 ? '+' : '−') + Math.abs(raw).toFixed(1) + '°';
        const kind = inputKind(r.input);
        const na = !!e.frame && ((kind === 'pad' || r.input.endsWith('.touch')) ? !e.frame.hasTouchpads : kind === 'gyro' ? !e.frame.motion : false);
        if (na) tag = 'n/a';
        if (r.tag.textContent !== tag) r.tag.textContent = tag;
        r.root.classList.toggle('is-na', na);
        r.root.classList.toggle('is-user', source === 'user');
        r.root.classList.toggle('is-unbound', text === '—');
        r.root.classList.toggle('is-hot', isHot(r.input, raw));
      }

      if (snap) {
        deckAEl.textContent = 'A · ' + (snap.decks.A.name || '—');
        deckBEl.textContent = 'B · ' + (snap.decks.B.name || '—');
        deckAEl.classList.toggle('is-focus', e.focus === 'A');
        deckBEl.classList.toggle('is-focus', e.focus === 'B');
        xfadeKnob.style.left = snap.crossfader * 100 + '%';
        xfadeVal.textContent = (snap.crossfader * 100).toFixed(0) + '%';
        const count = snap.libraryCount;
        const cur = e.cursor >= 0 && e.cursor < count ? e.cursor : -1;
        const entry = cur >= 0 ? e.cursorName : '';
        browseEl.textContent = cur >= 0 ? `Browse ▸ ${cur + 1}/${count} · ${entry}` : `Library · ${count} shaders · D-pad browses`;
        autoEl.textContent = 'Auto VJ ' + (snap.autoVj ? 'ON' : 'off');
        autoEl.classList.toggle('is-on', snap.autoVj);
      } else {
        deckAEl.textContent = 'A · —';
        deckBEl.textContent = 'B · —';
        browseEl.textContent = 'Open the VJ view to drive the decks';
        autoEl.textContent = '';
      }
      const flashAge = now - e.flash.at;
      flashEl.textContent = flashAge < 2500 ? '▸ ' + e.flash.text : '';
      let note = '';
      if (src.kind && !ready) note = 'Release all buttons and sticks to start';
      else if (snap && !snap.active) note = 'VJ view hidden: controls paused';
      noteEl.textContent = note;
    }

    if (e.editor.open) {
      for (const l of editLive) setMeter(l.fill, l.input, values[l.input] ?? 0);
      const sel = e.editor.picker ? e.editor.picker.row : e.editor.row;
      if (sel !== lastSel) {
        editRows.forEach((r, i) => r.classList.toggle('is-sel', i === sel));
        editRows[sel]?.scrollIntoView({ block: 'nearest' });
        lastSel = sel;
      }
    }
  }

  return { update, rebuild };
}
