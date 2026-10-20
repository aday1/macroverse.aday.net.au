// Minimal Chromium DevTools-protocol driver (Node built-ins only) for the Deck controls e2e.
// Profiles and screenshots go under $DECK_E2E_DIR (default: a temp dir); the profile is deleted on close.
import { spawn } from 'node:child_process';
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';

const OUT = process.env.DECK_E2E_DIR || mkdtempSync(path.join(tmpdir(), 'mv-deck-e2e-'));

export async function launch({ label, width = 1280, height = 800, touch = true }) {
  const root = path.join(OUT, label);
  const profile = path.join(root, 'profile');
  mkdirSync(profile, { recursive: true });
  const chrome = spawn(process.env.CHROMIUM || 'chromium', [
    '--headless=new', '--no-sandbox', '--disable-dev-shm-usage', '--use-angle=swiftshader', '--enable-unsafe-swiftshader',
    '--remote-debugging-port=0', '--no-first-run', '--no-default-browser-check', '--disable-extensions',
    '--disk-cache-size=1', '--media-cache-size=1',
    `--window-size=${width},${height}`, `--user-data-dir=${profile}`, 'about:blank',
  ], { stdio: ['ignore', 'ignore', 'pipe'] });
  const wsUrl = await new Promise((resolve, reject) => {
    let out = '';
    const t = setTimeout(() => reject(new Error('chromium did not start ' + out.slice(-800))), 20000);
    chrome.stderr.on('data', (c) => {
      out += c;
      const m = out.match(/DevTools listening on (ws:\/\/[^\s]+)/);
      if (m) { clearTimeout(t); resolve(m[1]); }
    });
  });
  const base = wsUrl.replace(/^ws:/, 'http:').split('/devtools/')[0];
  const list = await (await fetch(base + '/json/list')).json();
  const page = list.find((p) => p.type === 'page');
  const ws = new WebSocket(page.webSocketDebuggerUrl);
  await new Promise((r, j) => { ws.onopen = r; ws.onerror = j; });
  let id = 0;
  const pending = new Map();
  const listeners = [];
  const logs = [];
  ws.onmessage = (ev) => {
    const msg = JSON.parse(ev.data);
    if (msg.id && pending.has(msg.id)) {
      const { resolve, reject } = pending.get(msg.id);
      pending.delete(msg.id);
      if (msg.error) reject(new Error(JSON.stringify(msg.error)));
      else resolve(msg.result);
    } else if (msg.method) {
      if (msg.method === 'Runtime.consoleAPICalled') {
        logs.push(msg.params.type + ': ' + msg.params.args.map((a) => a.value ?? a.description ?? '').join(' '));
      } else if (msg.method === 'Runtime.exceptionThrown') {
        logs.push('EXC: ' + (msg.params.exceptionDetails?.exception?.description || msg.params.exceptionDetails?.text));
      }
      for (const l of listeners) l(msg);
    }
  };
  const send = (method, params = {}) => new Promise((resolve, reject) => {
    const mid = ++id;
    pending.set(mid, { resolve, reject });
    ws.send(JSON.stringify({ id: mid, method, params }));
  });
  await send('Runtime.enable');
  await send('Page.enable');
  await send('Emulation.setDeviceMetricsOverride', { width, height, deviceScaleFactor: 1, mobile: false });
  if (touch) await send('Emulation.setTouchEmulationEnabled', { enabled: true, maxTouchPoints: 10 });
  const evaluate = async (expression) => {
    const r = await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
    if (r.exceptionDetails) throw new Error('eval: ' + (r.exceptionDetails.exception?.description || r.exceptionDetails.text));
    return r.result.value;
  };
  const shot = async (name) => {
    const r = await send('Page.captureScreenshot', { format: 'png' });
    const p = path.join(root, name + '.png');
    writeFileSync(p, Buffer.from(r.data, 'base64'));
    return p;
  };
  const navigate = async (url) => {
    const loaded = new Promise((r) => {
      const l = (m) => { if (m.method === 'Page.loadEventFired') { listeners.splice(listeners.indexOf(l), 1); r(); } };
      listeners.push(l);
    });
    await send('Page.navigate', { url });
    await loaded;
  };
  const close = async () => {
    try { ws.close(); } catch {}
    chrome.kill('SIGKILL');
    await new Promise((r) => setTimeout(r, 300));
    rmSync(profile, { recursive: true, force: true });
  };
  return { send, evaluate, shot, navigate, close, logs, root };
}

export const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
