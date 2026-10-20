// Deck controls end-to-end: a running local Macroverse (MV_BASE_URL, default http://127.0.0.1:18765)
// driven by a fake Kefka bridge in headless Chromium at 1280x800, with screenshots.
//   node tests/e2e/deck-controls.e2e.mjs        (needs a built backend serving this frontend build)
// Then a Gamepad API scenario (bridge off, fake navigator.getGamepads).
import { launch, sleep } from './cdp.mjs';
import { startFakeBridge } from './fake-deck-bridge.mjs';

const BASE = (process.env.MV_BASE_URL || 'http://127.0.0.1:18765').replace(/\/$/, '');
const BRIDGE_PORT = Number(process.env.FAKE_BRIDGE_PORT || 18799);

const BTN = { A: 0, B: 1, X: 2, Y: 3, LB: 4, RB: 5, LT: 6, RT: 7, VIEW: 8, MENU: 9, L3: 10, R3: 11, UP: 12, DOWN: 13, LEFT: 14, RIGHT: 15 };
const bridge = startFakeBridge(BRIDGE_PORT);
const b = await launch({ label: 'e2e', touch: true });
const results = {};
const check = (name, ok, detail) => {
  results[name] = { ok: !!ok, detail };
  console.log((ok ? 'PASS ' : 'FAIL ') + name + (detail !== undefined ? '  ' + JSON.stringify(detail) : ''));
};
const tap = async (i, ms = 90) => { bridge.press(i); await sleep(ms); bridge.release(i); await sleep(140); };
const ev = (expr) => b.evaluate(expr);
const xfade = () => ev(`Number(document.querySelector('.vj-crossfader')?.value)`);
const engine = (expr) => ev(`(() => { const e = window.__macroverseDeck; return ${expr}; })()`);
const hideChrome = () => ev(`(() => { for (const el of document.querySelectorAll('body > div')) { if ((el.textContent||'').includes('Switch site')) el.style.display='none'; } return 1; })()`);

try {
  await b.navigate(BASE + '/');
  await ev(`localStorage.clear(); localStorage.setItem('macroverse-quick-start-seen-v1','1'); localStorage.setItem('macroverse-backend-viewer-collapsed','1'); localStorage.setItem('macroverse-vj-session-id', 'e2e-${Date.now()}'); 1`);
  await b.navigate(`${BASE}/?view=vj&deckBridge=http://127.0.0.1:${BRIDGE_PORT}/events`);
  for (let i = 0; i < 80; i++) {
    if (await engine(`!!(e && e.result && e.result.ready && e.snap && e.snap.active)`)) break;
    await sleep(500);
  }
  check('engine ready on native bridge', await engine(`e.source.kind === 'native' && e.result.ready`), await engine(`e.source`));
  await hideChrome();

  // 1. View -> HUD with live inputs.
  await tap(BTN.VIEW);
  await sleep(300);
  check('View shows HUD', await ev(`!!document.querySelector('.mv-deckhud')`));
  bridge.state.axes = [0.62, -0.7, -0.35, 0]; // L stick right+up, R stick left
  bridge.press(BTN.RT, 0.0); bridge.press(BTN.LT, 0.0);
  bridge.state.touch = [true, false]; bridge.state.touchpads[0] = [0.4, 0.6];
  bridge.press(BTN.X, 0); // nothing
  let hot = [];
  for (let i = 0; i < 30; i++) {
    await sleep(100);
    hot = await ev(`[...document.querySelectorAll('.mv-deckhud__row.is-hot .mv-deckhud__key')].map(e => e.textContent)`);
    if (hot.includes('L stick ↔') && hot.includes('L pad ↔')) break;
  }
  check('HUD shows live inputs (hot rows)', hot.includes('L stick ↔') && hot.includes('L pad ↔'), hot);
  console.log(await b.shot('01-hud-live-inputs'));
  bridge.state.axes = [0, 0, 0, 0];
  bridge.state.touch = [false, false];
  await sleep(300);

  // 2. LB / RB focus.
  await tap(BTN.RB);
  await sleep(200);
  const focusB = await ev(`({ focus: window.__macroverseDeck.focus, b: document.querySelector('.vj-deck[data-vj-deck="B"]').classList.contains('is-pad-focus'), a: document.querySelector('.vj-deck[data-vj-deck="A"]').classList.contains('is-pad-focus') })`);
  check('RB focuses deck B', focusB.focus === 'B' && focusB.b && !focusB.a, focusB);
  await tap(BTN.LB);
  await sleep(200);
  check('LB focuses deck A', (await engine(`e.focus`)) === 'A');

  // 3. D-pad browse + A/B load.
  const base = await engine(`Math.max(0, e.snap.decks.A.libraryIndex)`);
  await tap(BTN.RIGHT); await tap(BTN.RIGHT); await tap(BTN.RIGHT);
  await sleep(200);
  const cur = await ev(`({ cursor: window.__macroverseDeck.cursor, card: document.querySelector('.vj-shader-carousel-card.is-pad-cursor')?.dataset.path })`);
  const count = await engine(`e.snap.libraryCount`);
  check('D-pad right x3 moves browse cursor (+3 from deck A shader)', cur.cursor === (base + 3) % count && !!cur.card, { base, ...cur });
  console.log(await b.shot('02-browse-cursor'));
  await tap(BTN.A);
  await sleep(1500);
  const deckA = await engine(`e.snap.decks.A`);
  check('A loads cursor shader to deck A', deckA.libraryIndex === cur.cursor, { name: deckA.name, idx: deckA.libraryIndex });
  // Back to chroma-warp (index 0) for deck B: it exposes float params for the learn step.
  for (let i = 0; i < 90 && (await engine(`e.cursor`)) !== 0; i++) await tap(BTN.LEFT, 60);
  await tap(BTN.B);
  await sleep(1500);
  const deckB = await engine(`e.snap.decks.B`);
  check('B loads cursor shader to deck B', deckB.libraryIndex === 0, { name: deckB.name, idx: deckB.libraryIndex, params: deckB.params.map(p => p.name) });
  // Page down / up.
  await tap(BTN.DOWN);
  check('D-pad down pages +10', (await engine(`e.cursor`)) === 10);
  await tap(BTN.UP);
  check('D-pad up pages -10', (await engine(`e.cursor`)) === 0);
  console.log(await b.shot('03-loaded-decks'));

  // 4. RT rides the crossfader toward B.
  const x0 = await xfade();
  bridge.press(BTN.RT, 1);
  await sleep(500);
  bridge.release(BTN.RT);
  await sleep(150);
  const x1 = await xfade();
  check('RT moves crossfader toward B', x1 > x0 + 0.2, { x0, x1 });
  console.log(await b.shot('04-trigger-crossfader'));
  await tap(BTN.VIEW); // hide HUD: show the page's own crossfader + deck focus
  await sleep(250);
  await ev(`document.querySelector('.vj-crossfader-row')?.scrollIntoView({ block: 'center' }); 1`);
  await sleep(250);
  check('View hides HUD again', !(await ev(`!!document.querySelector('.mv-deckhud')`)));
  console.log(await b.shot('04b-crossfader-ui-no-hud'));
  await ev(`document.getElementById('vjDeckContainer')?.querySelector('.vj-deck-wrap')?.scrollTo?.(0, 0); document.getElementById('vjDeckContainer')?.scrollTo?.(0, 0); 1`);
  await tap(BTN.VIEW);
  await sleep(200);
  bridge.press(BTN.LT, 1);
  await sleep(250);
  bridge.release(BTN.LT);
  await sleep(150);
  const x2 = await xfade();
  check('LT moves crossfader back toward A', x2 < x1 - 0.1, { x1, x2 });

  // 5. Gyro roll only while the right trackpad is touched. Start near B.
  bridge.press(BTN.RT, 1);
  await sleep(1300);
  bridge.release(BTN.RT);
  await sleep(200);
  const g0 = await xfade();
  for (let r = 0; r <= 30; r += 3) { bridge.state.pose = [0, -40, r]; await sleep(40); }
  await sleep(200);
  const g1 = await xfade();
  check('gyro roll without enabler does nothing', Math.abs(g1 - g0) < 1e-6, { g0, g1 });
  bridge.state.touch = [false, true]; bridge.state.touchpads[1] = [0.1, 0.1];
  await sleep(200);
  for (let r = 30; r >= -6; r -= 3) { bridge.state.pose = [0, -40, r]; await sleep(40); }
  await sleep(200);
  const g2 = await xfade();
  const gyroState = await engine(`({ active: e.result.gyro.active, roll: e.result.values['gyro.roll'] })`);
  check('gyro roll with finger on right pad moves crossfader (steer left -> toward A)', g2 < g1 - 0.4, { g1, g2, gyroState });
  console.log(await b.shot('05-gyro-active'));
  bridge.state.touch = [false, false];
  await sleep(150);
  for (let r = -6; r <= 40; r += 3) { bridge.state.pose = [0, -40, r]; await sleep(40); }
  await sleep(200);
  const g3 = await xfade();
  check('gyro stops when the finger lifts (ratchet)', Math.abs(g3 - g2) < 1e-6, { g2, g3 });
  bridge.state.touch = [false, true];
  await sleep(150);
  for (let r = 40; r <= 70; r += 3) { bridge.state.pose = [0, -40, r]; await sleep(40); }
  await sleep(200);
  const g4 = await xfade();
  check('re-engaging continues from current value (ratchet stroke 2)', g4 > g3 + 0.3, { g3, g4 });
  bridge.state.touch = [false, false];
  bridge.state.pose = [0, -40, 0];
  await sleep(300);

  // 6. Learn: Menu -> Learn tab -> pick "A . <param>" -> move R stick X -> binds.
  await tap(BTN.MENU);
  await sleep(300);
  check('Menu opens mapping editor', await ev(`!!document.querySelector('.mv-deckedit')`));
  console.log(await b.shot('06-editor-inputs'));
  await tap(BTN.RB); // -> Learn tab
  await sleep(200);
  const rows = await engine(`e.editorRows()`);
  const want = rows.findIndex((t) => /^B\.param\.\d+$/.test(t));
  check('learn tab lists deck B params', want >= 0, { want, first: rows.slice(0, 12) });
  for (let i = 0; i < want + 5 && (await engine(`e.editor.row`)) < want; i++) await tap(BTN.DOWN, 60);
  await sleep(150);
  const selTarget = rows[await engine(`e.editor.row`)];
  await tap(BTN.A);
  await sleep(250);
  check('A starts learn on selected target', (await engine(`e.learn && e.learn.target`)) === selTarget, selTarget);
  console.log(await b.shot('07-learn-waiting'));
  bridge.state.axes = [0, 0, 0.9, 0]; // R stick right
  await sleep(250);
  bridge.state.axes = [0, 0, 0, 0];
  await sleep(250);
  const bound = await engine(`({ learn: !!e.learn, rs: e.bindings['rs.x'], ov: e.config.overrides['rs.x'] })`);
  check('moving R stick binds rs.x to the target', !bound.learn && bound.rs?.target === selTarget && bound.rs?.source === 'user', bound);
  console.log(await b.shot('08-learned-binding'));
  await tap(BTN.MENU);
  await sleep(200);
  // Use the learned binding: R stick right raises the B param.
  const idx = Number(selTarget.split('.').pop());
  const p0 = await ev(`window.__macroverseDeck.snap.decks.B.params[${idx}]`);
  bridge.state.axes = [0, 0, 1, 0];
  await sleep(700);
  bridge.state.axes = [0, 0, 0, 0];
  await sleep(200);
  const p1 = await ev(`window.__macroverseDeck.snap.decks.B.params[${idx}]`);
  const slider = await ev(`(() => { const row = document.querySelector('.vj-deck[data-vj-deck="B"] [data-deck-target="B.param.${idx}"] input[type=range]'); return row ? Number(row.value) : null; })()`);
  check('learned stick drives the deck B param (and its slider)', Number(p1.value) > Number(p0.value) && Math.abs(slider - Number(p1.value)) < 0.011, { p0, p1, slider });
  await tap(BTN.VIEW); // hide HUD to show the deck B param slider
  await sleep(200);
  await ev(`document.querySelector('.vj-deck[data-vj-deck="B"] [data-deck-target="B.param.${idx}"]')?.scrollIntoView({ block: 'center' }); 1`);
  await sleep(300);
  console.log(await b.shot('09-learned-param-moved-slider'));
  const persisted = await ev(`localStorage.getItem('macroverse-deck-controls-v1')`);
  check('override persisted in localStorage', persisted && persisted.includes('rs.x'), persisted);

  // 6b. On-screen learn: pick a deck B param row on the page, then move L stick up.
  await ev(`window.__macroverseDeck.startScreenPick(); 1`);
  await sleep(300);
  console.log(await b.shot('10a-screen-pick'));
  const pickTarget = await ev(`(() => { const rows = [...document.querySelectorAll('.vj-deck[data-vj-deck="B"] [data-deck-target^="B.param."]')]; const row = rows[2] || rows[0]; row.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, cancelable: true })); return row.dataset.deckTarget; })()`);
  await sleep(250);
  check('on-screen pick starts learn for that param', (await engine(`e.learn && e.learn.target`)) === pickTarget, pickTarget);
  console.log(await b.shot('10b-screen-pick-learn-card'));
  bridge.state.axes = [0, -0.95, 0, 0]; // L stick up (bridge +y down)
  await sleep(250);
  bridge.state.axes = [0, 0, 0, 0];
  await sleep(250);
  const ls = await engine(`e.bindings['ls.y']`);
  check('L stick up binds ls.y to the picked param', ls && ls.target === pickTarget && ls.source === 'user', ls);
  const pIdx = Number(pickTarget.split('.').pop());
  const q0 = await engine(`e.snap.decks.B.params[${pIdx}].value`);
  bridge.state.axes = [0, -1, 0, 0];
  await sleep(600);
  bridge.state.axes = [0, 0, 0, 0];
  await sleep(250);
  const q1 = await engine(`e.snap.decks.B.params[${pIdx}].value`);
  check('L stick up raises the picked param', q1 > q0, { q0, q1 });
  await ev(`document.querySelector('.vj-deck[data-vj-deck="B"] [data-deck-target="${pickTarget}"]')?.scrollIntoView({ block: 'center' }); 1`);
  await sleep(250);
  console.log(await b.shot('10c-screen-pick-learned-slider'));
  await tap(BTN.VIEW);
  await sleep(200);

  // 6c. Editor: gyro tab (enabler cycling frees LT), picker, reset to auto.
  await tap(BTN.MENU);
  await sleep(250);
  await tap(BTN.LB); // Inputs <- Learn? tabs wrap: go to gyro via LB from inputs
  await sleep(200);
  check('LB cycles editor tabs (to Gyro & general)', (await engine(`e.editor.tab`)) === 'gyro', await engine(`e.editor.tab`));
  await tap(BTN.RIGHT); // enabler: rpad-touch -> lt-hold
  await sleep(250);
  const lt = await engine(`({ enabler: e.config.gyro.enabler, lt: e.bindings['trig.lt'] || null })`);
  check('D-pad right cycles gyro enabler to Hold LT; LT auto binding freed', lt.enabler === 'lt-hold' && lt.lt === null, lt);
  console.log(await b.shot('12-editor-gyro-tab'));
  await tap(BTN.LEFT);
  await sleep(200);
  check('D-pad left returns enabler to right trackpad', (await engine(`e.config.gyro.enabler`)) === 'rpad-touch');
  await tap(BTN.RB); // gyro -> inputs (wrap)
  await sleep(200);
  const rsRow = await engine(`e.editorRows().indexOf('rs.x')`);
  for (let i = 0; i < 40 && (await engine(`e.editor.row`)) < rsRow; i++) await tap(BTN.DOWN, 60);
  for (let i = 0; i < 40 && (await engine(`e.editor.row`)) > rsRow; i++) await tap(BTN.UP, 60);
  await tap(BTN.RIGHT); await tap(BTN.RIGHT); // sensitivity +0.2
  await tap(BTN.L3); // invert
  await sleep(200);
  const tuned = await engine(`e.bindings['rs.x']`);
  check('D-pad right / L3 tune sensitivity and invert on a row', tuned.sensitivity === 1.2 && tuned.invert === true, tuned);
  await tap(BTN.A); // picker
  await sleep(250);
  check('A opens the target picker', !!(await engine(`e.editor.picker`)));
  console.log(await b.shot('13-editor-picker'));
  await tap(BTN.B);
  await sleep(150);
  await tap(BTN.Y); // back to auto
  await sleep(200);
  const back = await engine(`({ b: e.bindings['rs.x'], ov: e.config.overrides['rs.x'] || null })`);
  check('Y resets the row to auto (focused deck param, no override)', back.ov === null && (!back.b || back.b.source === 'auto'), back);
  await tap(BTN.MENU);
  await sleep(200);

  // 7. Gate closed (another window focused) -> nothing acts, HUD explains.
  bridge.state.nativeAllowed = false;
  await sleep(800);
  const src = await engine(`e.source`);
  check('closed gate reported in HUD', src.kind === null && /Focus the Macroverse Deck/.test(src.note), src);
  console.log(await b.shot('11-gate-closed'));
  await ev(`window.__macroverseDeck.setHudVisible(false); 1`);
  await sleep(600);
  const idle = await engine(`({ raf: e.raf, frame: e.frame, kind: e.source.kind, hud: e.hudVisible, ed: e.editor.open, learn: !!e.learn, pick: e.screenPick, gp: !!e.findGamepad(), lna: e.lastNativeAt, run: e.shouldRun() })`);
  check('closed gate + HUD hidden: engine loop idles (no per-frame work)', idle.raf === 0 && idle.frame === null, idle);
  bridge.state.nativeAllowed = true;
  await sleep(400);
  check('gate reopens: native control resumes', (await engine(`e.source.kind === 'native' && e.result.ready`)));

  console.log('LOGS:\n' + b.logs.filter((l) => !/thumbnail|WebGL|GPU stall/i.test(l)).slice(-25).join('\n'));
} catch (err) {
  console.error('E2E ERROR', err);
  try { console.log(await b.shot('zz-error')); } catch {}
  console.log('LOGS:\n' + b.logs.slice(-25).join('\n'));
} finally {
  await b.close();
  bridge.close();
  const failed = Object.entries(results).filter(([, r]) => !r.ok).map(([n]) => n);
  console.log(`\n${Object.keys(results).length - failed.length}/${Object.keys(results).length} checks passed` + (failed.length ? '; FAILED: ' + failed.join(', ') : ''));
  if (failed.length) process.exitCode = 1;
}


// ---- Gamepad API scenario -------------------------------------------------------------------
const g = await launch({ label: 'gamepad', touch: false });
const gpResults = [];
const gcheck = (n, ok, d) => { gpResults.push(ok); console.log((ok ? 'PASS ' : 'FAIL ') + n + (d !== undefined ? '  ' + JSON.stringify(d) : '')); };
const gev = (x) => g.evaluate(x);
try {
  await g.send('Page.addScriptToEvaluateOnNewDocument', { source: `
    window.__pad = { buttons: Array.from({length: 17}, () => ({ value: 0, pressed: false })), axes: [0,0,0,0], present: false };
    navigator.getGamepads = () => {
      const deck = { id: 'Steam Deck (Vendor: 28de Product: 1205)', index: 0, connected: true, mapping: 'standard', axes: [0,0,0,0], buttons: Array.from({length: 17}, () => ({ value: 1, pressed: true })), timestamp: performance.now() };
      const xbox = { id: 'Xbox Wireless Controller (STANDARD GAMEPAD Vendor: 045e Product: 0b13)', index: 1, connected: true, mapping: 'standard', axes: window.__pad.axes.slice(), buttons: window.__pad.buttons.map(b => ({...b})), timestamp: performance.now() };
      return window.__pad.present ? [deck, xbox] : [deck];
    };
  ` });
  await g.navigate(BASE + '/');
  await gev(`localStorage.clear(); localStorage.setItem('macroverse-quick-start-seen-v1','1'); localStorage.setItem('macroverse-backend-viewer-collapsed','1'); localStorage.setItem('macroverse-vj-session-id','gp-${Date.now()}'); 1`);
  await g.navigate(BASE + '/?view=vj&deckBridge=off');
  for (let i = 0; i < 80 && !(await gev(`!!(window.__macroverseDeck && window.__macroverseDeck.snap && window.__macroverseDeck.snap.active)`)); i++) await sleep(500);
  await sleep(500);
  const idle = await gev(`({ raf: window.__macroverseDeck.raf, kind: window.__macroverseDeck.source.kind, bridge: window.__macroverseDeck.bridge, hud: !!document.querySelector('.mv-deckhud') })`);
  gcheck('no controller on a local page: built-in Deck ignored, loop idle, no HUD, bridge off', idle.raf === 0 && idle.kind === null && idle.bridge === null && !idle.hud, idle);
  await gev(`window.__pad.present = true; window.dispatchEvent(new Event('gamepadconnected')); 1`);
  await sleep(600);
  const src = await gev(`window.__macroverseDeck.source`);
  gcheck('external standard gamepad picked up', src.kind === 'gamepad' && /Xbox/.test(src.label), src);
  const x0 = await gev(`Number(document.querySelector('.vj-crossfader').value)`);
  // Gamepad API is polled per animation frame; headless software GL can run very few frames, so poll.
  await gev(`window.__pad.buttons[7] = { value: 1, pressed: true }; 1`);
  let x1 = x0;
  for (let i = 0; i < 40 && x1 <= x0 + 0.2; i++) {
    await sleep(100);
    x1 = await gev(`Number(document.querySelector('.vj-crossfader').value)`);
  }
  await gev(`window.__pad.buttons[7] = { value: 0, pressed: false }; 1`);
  await sleep(200);
  gcheck('gamepad RT moves crossfader', x1 > x0 + 0.2, { x0, x1 });
  await gev(`window.__pad.buttons[8] = { value: 1, pressed: true }; 1`);
  await sleep(250);
  await gev(`window.__pad.buttons[8] = { value: 0, pressed: false }; 1`);
  await sleep(400);
  gcheck('gamepad View shows HUD (no gyro / trackpads note)', await gev(`!!document.querySelector('.mv-deckhud') && document.querySelector('.mv-deckhud__source').textContent.includes('no trackpads')`));
  console.log(await g.shot('gamepad-hud'));
  // Mouse still drives the crossfader slider normally.
  const ok = await gev(`(() => { const i = document.querySelector('.vj-crossfader'); i.value = '0.25'; i.dispatchEvent(new Event('input', { bubbles: true })); return Number(i.value) === 0.25; })()`);
  await sleep(200);
  const after = await gev(`({ snap: window.__macroverseDeck.snap.crossfader, live: Number(document.querySelector('.vj-crossfader').value), raf: window.__macroverseDeck.raf, src: window.__macroverseDeck.source.kind })`);
  gcheck('mouse/keyboard crossfader input still works with a pad connected', ok && after.live === 0.25 && after.snap === 0.25, { ok, ...after });
  console.log('LOGS:\n' + g.logs.slice(-10).join('\n'));
} catch (e) { console.error('ERR', e); } finally {
  await g.close();
  console.log(`${gpResults.filter(Boolean).length}/${gpResults.length} gamepad checks passed`);
  if (gpResults.some((r) => !r)) process.exitCode = 1;
}
