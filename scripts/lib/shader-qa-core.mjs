import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export const VERT_SRC = `precision highp float;
attribute vec2 a_pos;
varying vec2 v_uv;
varying vec2 surfacePosition;
void main() {
  vec2 uv = a_pos * 0.5 + 0.5;
  v_uv = uv;
  surfacePosition = uv;
  gl_Position = vec4(a_pos, 0.0, 1.0);
}`;

export const THUMB_W = 256;
export const THUMB_H = 144;
export const THUMB_WARMUP = 24;
export const THUMB_TIME = 5;

export function loadPrepScript() {
  return fs.readFileSync(path.join(__dirname, 'shader-prep.js'), 'utf8');
}

export function walkShaders(dir) {
  const out = [];
  if (!fs.existsSync(dir)) return out;
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, ent.name);
    if (ent.isDirectory()) out.push(...walkShaders(full));
    else if (/\.(fs|frag|glsl|isf)$/i.test(ent.name)) out.push(full);
  }
  return out;
}

/** Pixel variance + blank detection (matches factory QA). */
export function analyzePixels(pixels, w, h) {
  const n = w * h;
  let sumR = 0;
  let sumG = 0;
  let sumB = 0;
  let nearBlack = 0;
  let nearGray = 0;
  for (let i = 0; i < n; i++) {
    const o = i * 4;
    const r = pixels[o];
    const g = pixels[o + 1];
    const b = pixels[o + 2];
    sumR += r;
    sumG += g;
    sumB += b;
    if (r < 12 && g < 12 && b < 12) nearBlack++;
    if (Math.abs(r - 128) < 6 && Math.abs(g - 128) < 6 && Math.abs(b - 128) < 6) nearGray++;
  }
  const meanR = sumR / n;
  const meanG = sumG / n;
  const meanB = sumB / n;
  let varSum = 0;
  for (let i = 0; i < n; i++) {
    const o = i * 4;
    const dr = pixels[o] - meanR;
    const dg = pixels[o + 1] - meanG;
    const db = pixels[o + 2] - meanB;
    varSum += dr * dr + dg * dg + db * db;
  }
  const variance = varSum / (n * 3);
  const blackRatio = nearBlack / n;
  const grayRatio = nearGray / n;
  const isBlank =
    variance < 80 ||
    blackRatio > 0.95 ||
    (grayRatio > 0.92 && variance < 200);
  return { variance, blackRatio, grayRatio, isBlank, meanR, meanG, meanB };
}

export function buildBrowserHarness(prepSrc) {
  return `<!doctype html><canvas id="c"></canvas><script>${prepSrc}<\/script>`;
}

/** Evaluate compile + render in browser; returns { error?, pixels?, dataUrl?, analysis? } */
export async function qaShaderInPage(page, fragment, opts = {}) {
  const w = opts.width ?? THUMB_W;
  const h = opts.height ?? THUMB_H;
  const captureTime = opts.time ?? THUMB_TIME;
  const warmup = opts.warmup ?? THUMB_WARMUP;
  const makeThumb = opts.thumbnail !== false;

  return page.evaluate(
    ({ fragment, vert, w, h, captureTime, warmup, makeThumb }) => {
      const prep = window.ShaderPrep;
      const prepared = prep.prepareFragmentForOffscreenRender(prep.stripLeadingGarbage(fragment || ''));
      const gl = document.createElement('canvas').getContext('webgl', {
        premultipliedAlpha: false,
        preserveDrawingBuffer: true,
      });
      if (!gl) return { error: 'WebGL unavailable' };

      function compile(type, source) {
        const s = gl.createShader(type);
        gl.shaderSource(s, source);
        gl.compileShader(s);
        if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) {
          const log = gl.getShaderInfoLog(s) || 'compile failed';
          gl.deleteShader(s);
          return { error: log };
        }
        return { shader: s };
      }
      const ve = compile(gl.VERTEX_SHADER, vert);
      if (ve.error) return ve;
      const fe = compile(gl.FRAGMENT_SHADER, prepared);
      if (fe.error) return fe;
      const p = gl.createProgram();
      gl.attachShader(p, ve.shader);
      gl.attachShader(p, fe.shader);
      gl.linkProgram(p);
      gl.deleteShader(ve.shader);
      gl.deleteShader(fe.shader);
      if (!gl.getProgramParameter(p, gl.LINK_STATUS)) {
        return { error: gl.getProgramInfoLog(p) || 'link failed' };
      }

      gl.useProgram(p);
      const posLoc = gl.getAttribLocation(p, 'a_pos');
      const buf = gl.createBuffer();
      gl.bindBuffer(gl.ARRAY_BUFFER, buf);
      gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]), gl.STATIC_DRAW);
      gl.enableVertexAttribArray(posLoc);
      gl.vertexAttribPointer(posLoc, 2, gl.FLOAT, false, 0, 0);

      const defaultTex = gl.createTexture();
      gl.bindTexture(gl.TEXTURE_2D, defaultTex);
      gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0, gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([128, 128, 128, 255]));
      const SAMPLER2D = 35678;
      const n = gl.getProgramParameter(p, gl.ACTIVE_UNIFORMS);
      for (let i = 0; i < n; i++) {
        const u = gl.getActiveUniform(p, i);
        if (!u || u.type !== SAMPLER2D) continue;
        gl.activeTexture(gl.TEXTURE0 + i);
        gl.bindTexture(gl.TEXTURE_2D, defaultTex);
        const loc = gl.getUniformLocation(p, u.name);
        if (loc) gl.uniform1i(loc, i);
      }

      function setU(name, fn) {
        const loc = gl.getUniformLocation(p, name);
        if (loc) fn(loc);
      }
      const skip = new Set(['time', 'mouse', 'resolution', 'TIME', 'RENDERSIZE', 'uTimeScale', 'uMouse', 'iFrame', 'mouseX', 'mouseY', 'timeScale', 'FRAMEINDEX', 'useFrameIndex', 'fps']);
      for (let i = 0; i < n; i++) {
        const u = gl.getActiveUniform(p, i);
        if (!u || skip.has(u.name) || u.type === SAMPLER2D) continue;
        const loc = gl.getUniformLocation(p, u.name);
        if (!loc) continue;
        if (u.type === gl.FLOAT) gl.uniform1f(loc, 0.5);
        else if (u.type === gl.BOOL) gl.uniform1i(loc, 0);
      }

      gl.viewport(0, 0, w, h);
      gl.clearColor(0.1, 0.1, 0.1, 1);
      for (let i = 0; i < warmup; i++) {
        const t = i < warmup - 1 ? i * 0.12 + 0.5 : captureTime;
        setU('TIME', (l) => gl.uniform1f(l, t));
        setU('time', (l) => gl.uniform1f(l, t));
        setU('RENDERSIZE', (l) => gl.uniform2f(l, w, h));
        setU('resolution', (l) => gl.uniform2f(l, w, h));
        setU('uTimeScale', (l) => gl.uniform1f(l, 1));
        setU('timeScale', (l) => gl.uniform1f(l, 1));
        setU('uMouse', (l) => gl.uniform2f(l, 0.5, 0.5));
        setU('mouse', (l) => gl.uniform2f(l, 0.5, 0.5));
        setU('mouseX', (l) => gl.uniform1f(l, 0.5));
        setU('mouseY', (l) => gl.uniform1f(l, 0.5));
        setU('iFrame', (l) => gl.uniform1f(l, i));
        setU('FRAMEINDEX', (l) => gl.uniform1f(l, i));
        setU('useFrameIndex', (l) => gl.uniform1i(l, 0));
        setU('fps', (l) => gl.uniform1f(l, 60));
        gl.clear(gl.COLOR_BUFFER_BIT);
        gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
      }
      gl.finish();
      const pixels = new Uint8Array(w * h * 4);
      gl.readPixels(0, 0, w, h, gl.RGBA, gl.UNSIGNED_BYTE, pixels);
      const flipped = new Uint8ClampedArray(w * h * 4);
      const rowBytes = w * 4;
      for (let y = 0; y < h; y++) {
        const srcRow = h - 1 - y;
        flipped.set(pixels.subarray(srcRow * rowBytes, (srcRow + 1) * rowBytes), y * rowBytes);
      }
      let dataUrl = null;
      if (makeThumb) {
        const c = document.createElement('canvas');
        c.width = w;
        c.height = h;
        const ctx = c.getContext('2d');
        ctx.putImageData(new ImageData(flipped, w, h), 0, 0);
        dataUrl = c.toDataURL('image/jpeg', 0.5);
      }
      return { pixels: Array.from(flipped), dataUrl };
    },
    { fragment, vert: VERT_SRC, w, h, captureTime, warmup, makeThumb }
  );
}

export async function launchBrowser(puppeteer) {
  const opts = { headless: true, args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'] };
  try {
    return await puppeteer.default.launch(opts);
  } catch (err) {
    if (String(err.message || err).includes('Could not find Chrome')) {
      const candidates = [
        process.env.LOCALAPPDATA && path.join(process.env.LOCALAPPDATA, 'Google', 'Chrome', 'Application', 'chrome.exe'),
        'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
        'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
      ].filter(Boolean);
      for (const exe of candidates) {
        if (fs.existsSync(exe)) {
          opts.executablePath = exe;
          return puppeteer.default.launch(opts);
        }
      }
    }
    throw err;
  }
}

export async function createQaPage(browser, prepSrc) {
  const page = await browser.newPage();
  await page.setContent(buildBrowserHarness(prepSrc));
  return page;
}