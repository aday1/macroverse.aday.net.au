#!/usr/bin/env node
// Render smoke test over the Chrome DevTools protocol (Node >= 22: global fetch + WebSocket, no deps).
// Loads Macroverse, dismisses the splash, loads the first library shader into the preview, then opens
// the VJ view. For each step it measures every visible WebGL canvas from two composited screenshots
// 700 ms apart (mean luma, colour count, whether it changed), records the compile-error overlay, console
// errors, failed requests and the WebGL renderer, and saves a 1280x800 screenshot.
//
//   node ops/kefka/render-smoke.mjs --cdp http://127.0.0.1:19339 --url http://127.0.0.1:8765/ --out DIR
//
// ops/kefka/render-smoke.sh runs it against a throwaway headless Chrome on Kefka.
import fs from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';

const args = Object.fromEntries(process.argv.slice(2).reduce((acc, a, i, all) => {
  if (a.startsWith('--')) acc.push([a.slice(2), all[i + 1]]);
  return acc;
}, []));
const cdp = (args.cdp || 'http://127.0.0.1:9222').replace(/\/$/, '');
const url = args.url || 'http://127.0.0.1:8765/';
const out = path.resolve(args.out || '.');
fs.mkdirSync(out, { recursive: true });
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

const target = await (await fetch(`${cdp}/json/new?about:blank`, { method: 'PUT' })).json();
const wsUrl = new URL(target.webSocketDebuggerUrl);
const cdpUrl = new URL(cdp);
wsUrl.host = cdpUrl.host; // the browser reports its own port; we may be tunnelled
const ws = new WebSocket(wsUrl);
await new Promise((resolve, reject) => { ws.onopen = resolve; ws.onerror = reject; });
let nextId = 1;
const pending = new Map();
const consoleLines = [];
const failedRequests = [];
ws.onmessage = (event) => {
  const msg = JSON.parse(event.data);
  if (msg.id && pending.has(msg.id)) {
    const { resolve, reject } = pending.get(msg.id);
    pending.delete(msg.id);
    if (msg.error) reject(new Error(JSON.stringify(msg.error)));
    else resolve(msg.result);
  } else if (msg.method === 'Runtime.consoleAPICalled') {
    const text = msg.params.args.map((a) => a.value ?? a.description ?? '').join(' ');
    consoleLines.push({ level: msg.params.type, text: String(text).slice(0, 400) });
  } else if (msg.method === 'Runtime.exceptionThrown') {
    const d = msg.params.exceptionDetails;
    consoleLines.push({ level: 'exception', text: String(d.exception?.description || d.text).slice(0, 400) });
  } else if (msg.method === 'Network.responseReceived' && msg.params.response.status >= 400) {
    failedRequests.push(`${msg.params.response.status} ${msg.params.response.url}`.slice(0, 300));
  } else if (msg.method === 'Log.entryAdded') {
    consoleLines.push({ level: msg.params.entry.level, text: String(msg.params.entry.text).slice(0, 400) });
  }
};
const send = (method, params = {}) => new Promise((resolve, reject) => {
  const id = nextId++;
  pending.set(id, { resolve, reject });
  ws.send(JSON.stringify({ id, method, params }));
});
const evaluate = async (expression) => {
  const r = await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
  if (r.exceptionDetails) throw new Error(r.exceptionDetails.exception?.description || r.exceptionDetails.text);
  return r.result.value;
};
const waitFor = async (expression, ms) => {
  const end = Date.now() + ms;
  while (Date.now() < end) {
    if (await evaluate(expression).catch(() => false)) return true;
    await sleep(250);
  }
  return false;
};
const screenshot = async (name) => {
  const { data } = await send('Page.captureScreenshot', { format: 'png' });
  const file = path.join(out, name);
  fs.writeFileSync(file, Buffer.from(data, 'base64'));
  return file;
};
// Minimal PNG decoder (8-bit RGB/RGBA, non-interlaced: what Page.captureScreenshot returns).
function decodePng(buf) {
  let pos = 8, width = 0, height = 0, channels = 4;
  const idat = [];
  while (pos < buf.length) {
    const len = buf.readUInt32BE(pos), type = buf.toString('ascii', pos + 4, pos + 8);
    const data = buf.subarray(pos + 8, pos + 8 + len);
    if (type === 'IHDR') { width = data.readUInt32BE(0); height = data.readUInt32BE(4); channels = data[9] === 6 ? 4 : 3; }
    else if (type === 'IDAT') idat.push(data);
    pos += 12 + len;
  }
  const raw = zlib.inflateSync(Buffer.concat(idat));
  const stride = width * channels, px = Buffer.alloc(stride * height);
  for (let y = 0; y < height; y++) {
    const f = raw[y * (stride + 1)], row = raw.subarray(y * (stride + 1) + 1, (y + 1) * (stride + 1));
    for (let x = 0; x < stride; x++) {
      const a = x >= channels ? px[y * stride + x - channels] : 0, b = y ? px[(y - 1) * stride + x] : 0;
      const c = x >= channels && y ? px[(y - 1) * stride + x - channels] : 0;
      let v = row[x];
      if (f === 1) v += a; else if (f === 2) v += b; else if (f === 3) v += (a + b) >> 1;
      else if (f === 4) { const p = a + b - c, pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c);
        v += pa <= pb && pa <= pc ? a : pb <= pc ? b : c; }
      px[y * stride + x] = v & 255;
    }
  }
  return { width, height, channels, px };
}
function stats(png) {
  let sum = 0, min = 255, max = 0; const colours = new Set();
  const n = png.width * png.height;
  for (let i = 0; i < n; i++) {
    const o = i * png.channels, r = png.px[o], g = png.px[o + 1], b = png.px[o + 2];
    const l = 0.2126 * r + 0.7152 * g + 0.0722 * b;
    sum += l; if (l < min) min = l; if (l > max) max = l;
    colours.add((r >> 4) << 8 | (g >> 4) << 4 | (b >> 4));
  }
  return { meanLuma: Math.round(sum / n), lumaRange: Math.round(max - min), colours: colours.size };
}
const CANVASES = `[...document.querySelectorAll('canvas')].map((c) => {
  const r = c.getBoundingClientRect();
  let kind = '2d';
  try { if (c.getContext('webgl2') || c.getContext('webgl')) kind = 'webgl'; } catch { kind = '?'; }
  return { id: c.id || c.className || '(canvas)', kind, x: r.x, y: r.y, w: r.width, h: r.height,
           shown: r.width > 40 && r.height > 40 && c.offsetParent !== null && getComputedStyle(c).visibility !== 'hidden' };
}).filter((c) => c.shown && c.kind === 'webgl' && c.y < innerHeight && c.x < innerWidth)`;
async function sampleCanvases() {
  const list = await evaluate(CANVASES);
  const shot = async (c) => {
    const clip = { x: Math.max(0, c.x), y: Math.max(0, c.y), width: Math.min(c.w, 1280 - c.x), height: Math.min(c.h, 800 - c.y), scale: 1 };
    const { data } = await send('Page.captureScreenshot', { format: 'png', clip });
    return Buffer.from(data, 'base64');
  };
  const first = [];
  for (const c of list) first.push(await shot(c));
  await sleep(700);
  const result = [];
  for (let i = 0; i < list.length; i++) {
    const second = await shot(list[i]);
    result.push({ id: list[i].id, size: [Math.round(list[i].w), Math.round(list[i].h)], ...stats(decodePng(second)),
                  animating: !first[i].equals(second) });
  }
  return result;
}
const STATUS = `(() => {
  const o = document.getElementById('previewCompileErrorOverlay');
  const visible = (el) => !!el && el.offsetParent !== null && getComputedStyle(el).display !== 'none' && !el.classList.contains('hidden');
  return { shader: document.getElementById('appBarShader')?.textContent?.trim(),
           view: document.getElementById('viewSelect')?.value,
           listItems: document.querySelectorAll('#list .list-item').length,
           splashVisible: visible(document.getElementById('splashOverlay')),
           compileErrorVisible: visible(o),
           compileError: visible(o) ? (document.getElementById('previewErrorMsg')?.textContent || '').slice(0, 300) : null };
})()`;
const RENDERER = `(() => {
  const gl = document.createElement('canvas').getContext('webgl');
  if (!gl) return 'no WebGL';
  const ext = gl.getExtension('WEBGL_debug_renderer_info');
  return ext ? gl.getParameter(ext.UNMASKED_RENDERER_WEBGL) : gl.getParameter(gl.RENDERER);
})()`;

await send('Runtime.enable');
await send('Log.enable');
await send('Page.enable');
await send('Network.enable');
await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 800, deviceScaleFactor: 1, mobile: false });
await send('Page.navigate', { url });
const report = { url, steps: [] };
report.listLoaded = await waitFor(`document.querySelectorAll('#list .list-item').length > 0`, 45000);
await sleep(1500);
report.renderer = await evaluate(RENDERER);
// Dismiss the splash with a real pointer press, as a user would.
for (const type of ['mousePressed', 'mouseReleased']) {
  await send('Input.dispatchMouseEvent', { type, x: 640, y: 400, button: 'left', clickCount: 1 });
}
await sleep(800);
report.steps.push({ step: 'loaded', status: await evaluate(STATUS), screenshot: await screenshot('01-loaded.png') });

const clicked = await evaluate(`(() => {
  const item = document.querySelector('#list .list-item .list-item-name') || document.querySelector('#list .list-item');
  if (!item) return null;
  item.click();
  return item.textContent.trim();
})()`);
await sleep(5000);
report.steps.push({ step: 'preview', clicked, status: await evaluate(STATUS), canvases: await sampleCanvases(),
                    screenshot: await screenshot('02-preview.png') });

await evaluate(`(() => { const s = document.getElementById('viewSelect'); s.value = 'vj';
  s.dispatchEvent(new Event('change', { bubbles: true })); return s.value; })()`);
await sleep(7000);
report.steps.push({ step: 'vj', status: await evaluate(STATUS), canvases: await sampleCanvases(),
                    screenshot: await screenshot('03-vj.png') });

const errors = consoleLines.filter((l) => ['error', 'exception', 'assert'].includes(l.level));
report.consoleErrors = errors.slice(0, 40);
report.consoleErrorCount = errors.length;
report.failedRequests = failedRequests.slice(0, 40);
report.shaderCompileMessages = consoleLines.filter((l) => /compile|shader error|GL_INVALID|link/i.test(l.text)).slice(0, 20);
fs.writeFileSync(path.join(out, 'report.json'), JSON.stringify(report, null, 2));
console.log(JSON.stringify(report, null, 2));
await fetch(`${cdp}/json/close/${target.id}`).catch(() => {});
ws.close();
