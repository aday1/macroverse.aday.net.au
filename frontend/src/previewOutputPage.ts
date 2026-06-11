// Preview Output - standalone pop-out for the main shader preview.
// Receives prepared shader source + params from the main window via
// BroadcastChannel and renders independently with its own WebGL context.
// Mouse on this canvas acts as an XY pad (mouseX / mouseY uniforms).

const VERT_SRC = `precision highp float;
attribute vec2 a_pos;
varying vec2 v_uv;
varying vec2 surfacePosition;
void main() {
  vec2 uv = a_pos * 0.5 + 0.5;
  v_uv = uv;
  surfacePosition = uv;
  gl_Position = vec4(a_pos, 0.0, 1.0);
}`;

const QUAD = new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]);

interface ParamMeta { name: string; type: string }

interface ShaderMsg {
  type: 'shader';
  preparedSource: string;
  meta: ParamMeta[];
}
interface FrameMsg {
  type: 'frame';
  params: Record<string, number | boolean>;
  mouseX: number;
  mouseY: number;
}
interface ClearMsg { type: 'clear' }
type PVMsg = ShaderMsg | FrameMsg | ClearMsg;

function compile(gl: WebGLRenderingContext, type: number, src: string): WebGLShader {
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

function link(gl: WebGLRenderingContext, vSrc: string, fSrc: string): WebGLProgram {
  const v = compile(gl, gl.VERTEX_SHADER, vSrc);
  const f = compile(gl, gl.FRAGMENT_SHADER, fSrc);
  const p = gl.createProgram()!;
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

const canvas = document.getElementById('pvCanvas') as HTMLCanvasElement;
const statusEl = document.getElementById('status') as HTMLDivElement;

const glOpts: WebGLContextAttributes = {
  preserveDrawingBuffer: true,
  premultipliedAlpha: false,
  powerPreference: 'default'
};
const gl = canvas.getContext('webgl', glOpts);
if (!gl) {
  statusEl.textContent = 'WebGL not available';
  throw new Error('WebGL not available');
}
gl.getExtension('OES_standard_derivatives');

const buf = gl.createBuffer()!;
gl.bindBuffer(gl.ARRAY_BUFFER, buf);
gl.bufferData(gl.ARRAY_BUFFER, QUAD, gl.STATIC_DRAW);

let prog: WebGLProgram | null = null;
let meta: ParamMeta[] = [];
let params: Record<string, number | boolean> = {};
let mouseX = 0.5;
let mouseY = 0.5;
let localMouse = false;

const startTime = performance.now();

function resize(): void {
  const dpr = window.devicePixelRatio || 1;
  const w = Math.round(window.innerWidth * dpr);
  const h = Math.round(window.innerHeight * dpr);
  if (canvas.width !== w || canvas.height !== h) {
    canvas.width = w;
    canvas.height = h;
  }
}
window.addEventListener('resize', resize);
resize();

function setMouseFromClient(clientX: number, clientY: number): void {
  const rect = canvas.getBoundingClientRect();
  mouseX = Math.max(0, Math.min(1, (clientX - rect.left) / rect.width));
  mouseY = Math.max(0, Math.min(1, 1 - (clientY - rect.top) / rect.height));
  localMouse = true;
}

canvas.addEventListener('mousemove', (e) => setMouseFromClient(e.clientX, e.clientY));
canvas.addEventListener('mouseleave', () => {
  localMouse = false;
  mouseX = 0.5;
  mouseY = 0.5;
});

canvas.addEventListener('touchstart', (e) => {
  if (e.touches.length) setMouseFromClient(e.touches[0].clientX, e.touches[0].clientY);
}, { passive: true });
canvas.addEventListener('touchmove', (e) => {
  if (e.touches.length) {
    setMouseFromClient(e.touches[0].clientX, e.touches[0].clientY);
    e.preventDefault();
  }
}, { passive: false });
canvas.addEventListener('touchend', (e) => {
  if (e.touches.length === 0) {
    localMouse = false;
    mouseX = 0.5;
    mouseY = 0.5;
  }
}, { passive: true });
canvas.addEventListener('touchcancel', (e) => {
  if (e.touches.length === 0) {
    localMouse = false;
    mouseX = 0.5;
    mouseY = 0.5;
  }
}, { passive: true });

// BroadcastChannel
const channel = new BroadcastChannel('macroverse-preview-output');

channel.onmessage = (ev: MessageEvent<PVMsg>) => {
  const msg = ev.data;
  if (!msg || !msg.type) return;

  if (msg.type === 'shader') {
    if (prog) gl.deleteProgram(prog);
    try {
      prog = link(gl, VERT_SRC, msg.preparedSource);
      meta = msg.meta;
      statusEl.style.opacity = '0';
    } catch (e) {
      prog = null;
      console.warn('[Preview Output] shader compile failed:', e);
    }
  } else if (msg.type === 'frame') {
    params = msg.params;
    if (!localMouse) {
      mouseX = msg.mouseX ?? 0.5;
      mouseY = msg.mouseY ?? 0.5;
    }
  } else if (msg.type === 'clear') {
    if (prog) gl.deleteProgram(prog);
    prog = null;
    meta = [];
  }
};

channel.postMessage({ type: 'preview-ready' });
let syncRetries = 0;
const syncInterval = setInterval(() => {
  if (prog || ++syncRetries > 15) { clearInterval(syncInterval); return; }
  channel.postMessage({ type: 'preview-ready' });
}, 500);

// Render loop
let frameCount = 0;
function frame(): void {
  requestAnimationFrame(frame);
  if (!prog) return;
  frameCount++;

  const w = canvas.width;
  const h = canvas.height;
  const t = (performance.now() - startTime) / 1000;
  const ts = (params.timeScale as number) ?? 1;
  const mx = mouseX;
  const my = mouseY;

  gl.viewport(0, 0, w, h);
  gl.clearColor(0.05, 0.05, 0.08, 1);
  gl.clear(gl.COLOR_BUFFER_BIT);
  gl.useProgram(prog);

  const builtins: [string, () => void][] = [
    ['TIME', () => gl.uniform1f(gl.getUniformLocation(prog!, 'TIME')!, t)],
    ['RENDERSIZE', () => gl.uniform2f(gl.getUniformLocation(prog!, 'RENDERSIZE')!, w, h)],
    ['uTimeScale', () => gl.uniform1f(gl.getUniformLocation(prog!, 'uTimeScale')!, ts)],
    ['uMouse', () => gl.uniform2f(gl.getUniformLocation(prog!, 'uMouse')!, mx, my)],
    ['time', () => gl.uniform1f(gl.getUniformLocation(prog!, 'time')!, t * ts)],
    ['mouse', () => gl.uniform2f(gl.getUniformLocation(prog!, 'mouse')!, mx, my)],
    ['resolution', () => gl.uniform2f(gl.getUniformLocation(prog!, 'resolution')!, w, h)],
    ['iResolution', () => gl.uniform2f(gl.getUniformLocation(prog!, 'iResolution')!, w, h)],
    ['iGlobalTime', () => gl.uniform1f(gl.getUniformLocation(prog!, 'iGlobalTime')!, t)],
    ['iTime', () => gl.uniform1f(gl.getUniformLocation(prog!, 'iTime')!, t)],
    ['FRAMEINDEX', () => gl.uniform1f(gl.getUniformLocation(prog!, 'FRAMEINDEX')!, frameCount)],
    ['iFrame', () => gl.uniform1f(gl.getUniformLocation(prog!, 'iFrame')!, frameCount)],
    ['useFrameIndex', () => gl.uniform1i(gl.getUniformLocation(prog!, 'useFrameIndex')!, (params.useFrameIndex as boolean) ? 1 : 0)],
    ['fps', () => gl.uniform1f(gl.getUniformLocation(prog!, 'fps')!, 60)],
    ['timeScale', () => gl.uniform1f(gl.getUniformLocation(prog!, 'timeScale')!, ts)],
    ['mouseX', () => gl.uniform1f(gl.getUniformLocation(prog!, 'mouseX')!, mx)],
    ['mouseY', () => gl.uniform1f(gl.getUniformLocation(prog!, 'mouseY')!, my)],
  ];
  for (const [, setter] of builtins) {
    try { setter(); } catch (_) {}
  }

  for (const [k, v] of Object.entries(params)) {
    if (['timeScale', 'mouseX', 'mouseY'].includes(k)) continue;
    const loc = gl.getUniformLocation(prog, k);
    if (!loc) continue;
    if (typeof v === 'boolean') gl.uniform1i(loc, v ? 1 : 0);
    else if (typeof v === 'number') gl.uniform1f(loc, v);
  }

  gl.bindBuffer(gl.ARRAY_BUFFER, buf);
  const loc = gl.getAttribLocation(prog, 'a_pos');
  gl.enableVertexAttribArray(loc);
  gl.vertexAttribPointer(loc, 2, gl.FLOAT, false, 0, 0);
  gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
}

requestAnimationFrame(frame);
