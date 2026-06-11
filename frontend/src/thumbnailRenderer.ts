import { stripLeadingGarbage, prepareFragmentForOffscreenRender } from './render.js';
import { fetchShader } from './api.js';
import { appSettings } from './state.js';

const THUMB_WIDTH = 256;
const THUMB_HEIGHT = 144;
/** Warmup frames so the shader is actually running before we capture (avoids black transition). */
const THUMB_WARMUP_FRAMES = 24;
/** Time at which to capture (many shaders need several seconds to show content). */
const THUMB_CAPTURE_TIME = 5;
const YIELD_MS = 48;
const IDLE_TIMEOUT_MS = 2500;

const vertSrc = `precision highp float;
attribute vec2 a_pos;
varying vec2 v_uv;
varying vec2 surfacePosition;
void main() {
  vec2 uv = a_pos * 0.5 + 0.5;
  v_uv = uv;
  surfacePosition = uv;
  gl_Position = vec4(a_pos, 0.0, 1.0);
}`;

const quadVerts = new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]);

let offCtx: WebGLRenderingContext | null = null;
let offCanvas: HTMLCanvasElement | null = null;

function getOffscreenGl(): WebGLRenderingContext | null {
  if (offCtx && offCanvas) return offCtx;
  offCanvas = document.createElement('canvas');
  offCanvas.width = THUMB_WIDTH;
  offCanvas.height = THUMB_HEIGHT;
  offCanvas.style.cssText = 'position:absolute;left:-9999px;top:0;width:1px;height:1px;visibility:hidden;';
  document.body.appendChild(offCanvas);
  const gl = offCanvas.getContext('webgl', {
    premultipliedAlpha: false,
    preserveDrawingBuffer: true,
    powerPreference: 'low-power'
  });
  if (!gl) {
    if (offCanvas.parentNode) offCanvas.parentNode.removeChild(offCanvas);
    offCanvas = null;
    return null;
  }
  offCtx = gl;
  return gl;
}

function compileShader(gl: WebGLRenderingContext, type: number, src: string): WebGLShader {
  const s = gl.createShader(type);
  if (!s) throw new Error('createShader failed');
  gl.shaderSource(s, src);
  gl.compileShader(s);
  if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) {
    const log = gl.getShaderInfoLog(s);
    gl.deleteShader(s);
    throw new Error(log || 'compile failed');
  }
  return s;
}

function createThumbProgram(gl: WebGLRenderingContext, fragSrc: string): WebGLProgram {
  const prepared = prepareFragmentForOffscreenRender(stripLeadingGarbage(fragSrc || ''));
  const v = compileShader(gl, gl.VERTEX_SHADER, vertSrc);
  const f = compileShader(gl, gl.FRAGMENT_SHADER, prepared);
  const p = gl.createProgram();
  if (!p) throw new Error('createProgram failed');
  gl.attachShader(p, v);
  gl.attachShader(p, f);
  gl.linkProgram(p);
  gl.deleteShader(v);
  gl.deleteShader(f);
  if (!gl.getProgramParameter(p, gl.LINK_STATUS)) {
    const log = gl.getProgramInfoLog(p);
    gl.deleteProgram(p);
    throw new Error(log || 'link failed');
  }
  return p;
}

function createDefaultTex(gl: WebGLRenderingContext): WebGLTexture {
  const tex = gl.createTexture();
  if (!tex) throw new Error('createTexture failed');
  gl.bindTexture(gl.TEXTURE_2D, tex);
  gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0, gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([128, 128, 128, 255]));
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
  return tex;
}

function setUniforms(gl: WebGLRenderingContext, prog: WebGLProgram, w: number, h: number, time: number, frameIndex: number, defaultTex: WebGLTexture): void {
  const timeLoc = gl.getUniformLocation(prog, 'TIME');
  if (timeLoc) gl.uniform1f(timeLoc, time);
  const timeLowerLoc = gl.getUniformLocation(prog, 'time');
  if (timeLowerLoc) gl.uniform1f(timeLowerLoc, time);
  const rendLoc = gl.getUniformLocation(prog, 'RENDERSIZE');
  if (rendLoc) gl.uniform2f(rendLoc, w, h);
  const resLoc = gl.getUniformLocation(prog, 'resolution');
  if (resLoc) gl.uniform2f(resLoc, w, h);
  const tsLoc = gl.getUniformLocation(prog, 'uTimeScale');
  if (tsLoc) gl.uniform1f(tsLoc, 1.0);
  const mouseLoc = gl.getUniformLocation(prog, 'uMouse');
  if (mouseLoc) gl.uniform2f(mouseLoc, 0.5, 0.5);
  const mouseLoc2 = gl.getUniformLocation(prog, 'mouse');
  if (mouseLoc2) gl.uniform2f(mouseLoc2, 0.5, 0.5);
  const iframeLoc = gl.getUniformLocation(prog, 'iFrame');
  if (iframeLoc) gl.uniform1f(iframeLoc, frameIndex);
  const frameIndexLoc = gl.getUniformLocation(prog, 'FRAMEINDEX');
  if (frameIndexLoc) gl.uniform1f(frameIndexLoc, frameIndex);
  const useFrameLoc = gl.getUniformLocation(prog, 'useFrameIndex');
  if (useFrameLoc) gl.uniform1i(useFrameLoc, 0);
  const fpsLoc = gl.getUniformLocation(prog, 'fps');
  if (fpsLoc) gl.uniform1f(fpsLoc, 60);
  const timeScaleLoc = gl.getUniformLocation(prog, 'timeScale');
  if (timeScaleLoc) gl.uniform1f(timeScaleLoc, 1.0);
  const mouseXLoc = gl.getUniformLocation(prog, 'mouseX');
  if (mouseXLoc) gl.uniform1f(mouseXLoc, 0.5);
  const mouseYLoc = gl.getUniformLocation(prog, 'mouseY');
  if (mouseYLoc) gl.uniform1f(mouseYLoc, 0.5);
  const n = gl.getProgramParameter(prog, gl.ACTIVE_UNIFORMS) as number;
  const SAMPLER2D = 35678;
  for (let i = 0; i < n; i++) {
    const u = gl.getActiveUniform(prog, i);
    if (!u || u.type !== SAMPLER2D) continue;
    gl.activeTexture(gl.TEXTURE0 + i);
    gl.bindTexture(gl.TEXTURE_2D, defaultTex);
    const loc = gl.getUniformLocation(prog, u.name);
    if (loc) gl.uniform1i(loc, i);
  }
  const n2 = gl.getProgramParameter(prog, gl.ACTIVE_UNIFORMS) as number;
  const skip = new Set(['time', 'mouse', 'resolution', 'TIME', 'RENDERSIZE', 'uTimeScale', 'uMouse', 'iFrame', 'mouseX', 'mouseY', 'timeScale', 'FRAMEINDEX', 'useFrameIndex', 'fps']);
  for (let i = 0; i < n2; i++) {
    const u = gl.getActiveUniform(prog, i);
    if (!u) continue;
    if (u.type === gl.FLOAT && !skip.has(u.name)) {
      const loc = gl.getUniformLocation(prog, u.name);
      if (loc) gl.uniform1f(loc, 0.5);
    } else if (u.type === gl.BOOL) {
      const loc = gl.getUniformLocation(prog, u.name);
      if (loc) gl.uniform1i(loc, 0);
    }
  }
}

export function renderThumbnailSync(fragSrc: string): string | null {
  const gl = getOffscreenGl();
  if (!gl) return null;
  try {
    const prog = createThumbProgram(gl, fragSrc);
    gl.useProgram(prog);
    const posLoc = gl.getAttribLocation(prog, 'a_pos');
    const buf = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, buf);
    gl.bufferData(gl.ARRAY_BUFFER, quadVerts, gl.STATIC_DRAW);
    gl.enableVertexAttribArray(posLoc);
    gl.vertexAttribPointer(posLoc, 2, gl.FLOAT, false, 0, 0);
    const defaultTex = createDefaultTex(gl);
    gl.viewport(0, 0, THUMB_WIDTH, THUMB_HEIGHT);
    gl.clearColor(0.1, 0.1, 0.1, 1);
    for (let i = 0; i < THUMB_WARMUP_FRAMES; i++) {
      const t = i < THUMB_WARMUP_FRAMES - 1 ? i * 0.12 + 0.5 : THUMB_CAPTURE_TIME;
      setUniforms(gl, prog, THUMB_WIDTH, THUMB_HEIGHT, t, i, defaultTex);
      gl.clear(gl.COLOR_BUFFER_BIT);
      gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
    }
    gl.finish();
    const w = THUMB_WIDTH;
    const h = THUMB_HEIGHT;
    const pixels = new Uint8Array(w * h * 4);
    gl.readPixels(0, 0, w, h, gl.RGBA, gl.UNSIGNED_BYTE, pixels);
    const flipped = new Uint8ClampedArray(w * h * 4);
    const rowBytes = w * 4;
    for (let y = 0; y < h; y++) {
      const srcRow = h - 1 - y;
      flipped.set(pixels.subarray(srcRow * rowBytes, (srcRow + 1) * rowBytes), y * rowBytes);
    }
    const readbackCanvas = document.createElement('canvas');
    readbackCanvas.width = w;
    readbackCanvas.height = h;
    const readbackCtx = readbackCanvas.getContext('2d');
    if (!readbackCtx) {
      gl.deleteProgram(prog);
      gl.deleteBuffer(buf);
      gl.deleteTexture(defaultTex);
      return null;
    }
    readbackCtx.putImageData(new ImageData(flipped, w, h), 0, 0);
    const quality = appSettings.thumbnailQuality ?? 0.5;
    const maxSize = appSettings.thumbnailMaxSize ?? 120;
    let targetCanvas: HTMLCanvasElement = readbackCanvas;
    if (maxSize > 0 && (w > maxSize || h > maxSize)) {
      const scale = Math.min(maxSize / w, maxSize / h);
      const tw = Math.max(1, Math.floor(w * scale));
      const th = Math.max(1, Math.floor(h * scale));
      const c2 = document.createElement('canvas');
      c2.width = tw;
      c2.height = th;
      const ctx = c2.getContext('2d');
      if (ctx) {
        ctx.drawImage(readbackCanvas, 0, 0, w, h, 0, 0, tw, th);
        targetCanvas = c2;
      }
    }
    const dataUrl = targetCanvas.toDataURL('image/jpeg', quality);
    gl.deleteProgram(prog);
    gl.deleteBuffer(buf);
    gl.deleteTexture(defaultTex);
    return dataUrl;
  } catch {
    return null;
  }
}

function yieldToMain(): Promise<void> {
  return new Promise((r) => {
    const scheduling = (navigator as Navigator & { scheduling?: { isInputPending?: () => boolean } }).scheduling;
    if (typeof requestIdleCallback === 'function') {
      requestIdleCallback(
        (deadline) => {
          if (scheduling?.isInputPending?.() || deadline.timeRemaining() < 8) {
            window.setTimeout(r, YIELD_MS);
            return;
          }
          r();
        },
        { timeout: IDLE_TIMEOUT_MS }
      );
    } else {
      setTimeout(r, YIELD_MS);
    }
  });
}

export type ThumbnailQueueEntry = { path: string; entry: { path?: string; fixedName?: string; name?: string } };
export type ThumbnailProgress = (done: number, total: number, path: string) => void;

if (typeof window !== 'undefined' && /[?&]bulk=1/.test(window.location.search)) {
  (window as unknown as { renderThumbnailSyncForBulk: (src: string) => string | null }).renderThumbnailSyncForBulk = renderThumbnailSync;
}

export async function generateThumbnailsInBackground(
  entries: ThumbnailQueueEntry[],
  onProgress: ThumbnailProgress,
  onThumbnail: (path: string, dataUrl: string) => void,
  signal?: AbortSignal
): Promise<void> {
  for (let i = 0; i < entries.length; i++) {
    if (signal?.aborted) return;
    await yieldToMain();
    if (signal?.aborted) return;
    const { path, entry } = entries[i];
    onProgress(i + 1, entries.length, entry.fixedName || entry.name || path);
    let src: string;
    try {
      src = await fetchShader(path);
    } catch {
      continue;
    }
    if (signal?.aborted) return;
    await yieldToMain();
    if (signal?.aborted) return;
    const dataUrl = renderThumbnailSync(src);
    if (dataUrl) {
      onThumbnail(path, dataUrl);
    }
  }
}
