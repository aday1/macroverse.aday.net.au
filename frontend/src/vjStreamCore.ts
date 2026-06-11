import { VJ_MIX_FRAG_SRC } from './vjMixShader.js';

export const VJ_STREAM_VERT = `precision highp float;
attribute vec2 a_pos;
varying vec2 v_uv;
void main() {
  vec2 uv = a_pos * 0.5 + 0.5;
  v_uv = uv;
  gl_Position = vec4(a_pos, 0.0, 1.0);
}`;

export const VJ_STREAM_QUAD = new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]);
export const VJ_STREAM_DECK_W = 640;
export const VJ_STREAM_DECK_H = 360;

export interface VjParamMeta { name: string; type: string }

export interface VjShaderMsg {
  type: 'shader';
  deck: 'A' | 'B';
  preparedSource: string;
  meta: VjParamMeta[];
}

export interface VjFrameMsg {
  type: 'frame';
  crossfader: number;
  mixModeInt: number;
  flipV: boolean;
  flipH: boolean;
  rotation: number;
  mouseX: number;
  mouseY: number;
  mouseAX?: number;
  mouseAY?: number;
  mouseBX?: number;
  mouseBY?: number;
  paramsA: Record<string, number | boolean>;
  paramsB: Record<string, number | boolean>;
  qrOverlay?: unknown;
}

export interface VjClearMsg { type: 'clear'; deck: 'A' | 'B' }
export interface VjQrOverlayMsg { type: 'qr-overlay'; qrOverlay: unknown }

export type VjStreamMsg = VjShaderMsg | VjFrameMsg | VjClearMsg | VjQrOverlayMsg;

const GL_OPTS: WebGLContextAttributes = {
  preserveDrawingBuffer: true,
  premultipliedAlpha: false,
  powerPreference: 'default',
};

export function compileGlShader(gl: WebGLRenderingContext, type: number, src: string): WebGLShader {
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

export function linkGlProgram(gl: WebGLRenderingContext, vSrc: string, fSrc: string): WebGLProgram {
  const v = compileGlShader(gl, gl.VERTEX_SHADER, vSrc);
  const f = compileGlShader(gl, gl.FRAGMENT_SHADER, fSrc);
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

export function setVjDeckUniforms(
  gl: WebGLRenderingContext,
  prog: WebGLProgram,
  w: number,
  h: number,
  time: number,
  overrides: Record<string, number | boolean>,
  meta: VjParamMeta[],
  mx: number,
  my: number
): void {
  const timeLoc = gl.getUniformLocation(prog, 'TIME');
  if (timeLoc) gl.uniform1f(timeLoc, time);
  const resLoc = gl.getUniformLocation(prog, 'RENDERSIZE');
  if (resLoc) gl.uniform2f(resLoc, w, h);
  const tsLoc = gl.getUniformLocation(prog, 'uTimeScale');
  if (tsLoc) gl.uniform1f(tsLoc, 1.0);
  const mouseLoc = gl.getUniformLocation(prog, 'uMouse');
  if (mouseLoc) gl.uniform2f(mouseLoc, mx, my);
  const iFrameLoc = gl.getUniformLocation(prog, 'iFrame');
  if (iFrameLoc) gl.uniform1f(iFrameLoc, Math.floor(time * 60));
  for (const p of meta) {
    if (overrides[p.name] === undefined) continue;
    const loc = gl.getUniformLocation(prog, p.name);
    if (!loc) continue;
    if (p.type === 'bool') gl.uniform1i(loc, (overrides[p.name] as boolean) ? 1 : 0);
    else gl.uniform1f(loc, overrides[p.name] as number);
  }
  const aliases: [string, (l: WebGLUniformLocation) => void][] = [
    ['time', (l) => gl.uniform1f(l, time)],
    ['resolution', (l) => gl.uniform2f(l, w, h)],
    ['mouse', (l) => gl.uniform2f(l, mx, my)],
    ['iGlobalTime', (l) => gl.uniform1f(l, time)],
    ['iTime', (l) => gl.uniform1f(l, time)],
    ['iResolution', (l) => gl.uniform2f(l, w, h)],
    ['FRAMEINDEX', (l) => gl.uniform1f(l, Math.floor(time * 60))],
    ['timeScale', (l) => gl.uniform1f(l, 1.0)],
    ['mouseX', (l) => gl.uniform1f(l, mx)],
    ['mouseY', (l) => gl.uniform1f(l, my)],
  ];
  for (const [name, setter] of aliases) {
    const loc = gl.getUniformLocation(prog, name);
    if (loc) setter(loc);
  }
}

function initBuf(gl: WebGLRenderingContext): WebGLBuffer {
  const b = gl.createBuffer()!;
  gl.bindBuffer(gl.ARRAY_BUFFER, b);
  gl.bufferData(gl.ARRAY_BUFFER, VJ_STREAM_QUAD, gl.STATIC_DRAW);
  return b;
}

function createTex(gl: WebGLRenderingContext): WebGLTexture {
  const t = gl.createTexture()!;
  gl.bindTexture(gl.TEXTURE_2D, t);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
  return t;
}

export interface VjStreamRenderer {
  outputCanvas: HTMLCanvasElement;
  deckCanvasA: HTMLCanvasElement;
  deckCanvasB: HTMLCanvasElement;
  hasShader: boolean;
  applyMessage(msg: VjStreamMsg): void;
  renderFrame(): void;
  resizeOutput(w: number, h: number): void;
}

export function createVjStreamRenderer(outputCanvas: HTMLCanvasElement): VjStreamRenderer {
  const deckCanvasA = document.createElement('canvas');
  deckCanvasA.width = VJ_STREAM_DECK_W;
  deckCanvasA.height = VJ_STREAM_DECK_H;
  const deckCanvasB = document.createElement('canvas');
  deckCanvasB.width = VJ_STREAM_DECK_W;
  deckCanvasB.height = VJ_STREAM_DECK_H;

  const glA = deckCanvasA.getContext('webgl', GL_OPTS);
  const glB = deckCanvasB.getContext('webgl', GL_OPTS);
  const glMix = outputCanvas.getContext('webgl', GL_OPTS);
  if (!glA || !glB || !glMix) throw new Error('WebGL not available');

  for (const g of [glA, glB, glMix]) g.getExtension('OES_standard_derivatives');

  const bufA = initBuf(glA);
  const bufB = initBuf(glB);
  const bufMix = initBuf(glMix);
  const texA = createTex(glMix);
  const texB = createTex(glMix);
  const mixProg = linkGlProgram(glMix, VJ_STREAM_VERT, VJ_MIX_FRAG_SRC);

  let progA: WebGLProgram | null = null;
  let progB: WebGLProgram | null = null;
  let metaA: VjParamMeta[] = [];
  let metaB: VjParamMeta[] = [];
  let paramsA: Record<string, number | boolean> = {};
  let paramsB: Record<string, number | boolean> = {};
  let crossfader = 0;
  let mixModeInt = 0;
  let flipV = false;
  let flipH = false;
  let rotation = 0;
  let mouseAX = 0.5;
  let mouseAY = 0.5;
  let mouseBX = 0.5;
  let mouseBY = 0.5;
  const startTime = performance.now();

  function renderDeck(
    gl: WebGLRenderingContext,
    canvas: HTMLCanvasElement,
    prog: WebGLProgram | null,
    buf: WebGLBuffer,
    params: Record<string, number | boolean>,
    meta: VjParamMeta[],
    mx: number,
    my: number
  ): void {
    gl.viewport(0, 0, canvas.width, canvas.height);
    gl.clearColor(0.05, 0.05, 0.08, 1);
    gl.clear(gl.COLOR_BUFFER_BIT);
    if (!prog) return;
    gl.useProgram(prog);
    const t = (performance.now() - startTime) / 1000;
    setVjDeckUniforms(gl, prog, canvas.width, canvas.height, t, params, meta, mx, my);
    gl.bindBuffer(gl.ARRAY_BUFFER, buf);
    const loc = gl.getAttribLocation(prog, 'a_pos');
    gl.enableVertexAttribArray(loc);
    gl.vertexAttribPointer(loc, 2, gl.FLOAT, false, 0, 0);
    gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
  }

  function renderMix(): void {
    glMix.bindTexture(glMix.TEXTURE_2D, texA);
    glMix.texImage2D(glMix.TEXTURE_2D, 0, glMix.RGBA, glMix.RGBA, glMix.UNSIGNED_BYTE, deckCanvasA);
    glMix.bindTexture(glMix.TEXTURE_2D, texB);
    glMix.texImage2D(glMix.TEXTURE_2D, 0, glMix.RGBA, glMix.RGBA, glMix.UNSIGNED_BYTE, deckCanvasB);

    glMix.useProgram(mixProg);
    glMix.viewport(0, 0, outputCanvas.width, outputCanvas.height);
    glMix.clearColor(0, 0, 0, 1);
    glMix.clear(glMix.COLOR_BUFFER_BIT);

    glMix.activeTexture(glMix.TEXTURE0);
    glMix.bindTexture(glMix.TEXTURE_2D, texA);
    glMix.activeTexture(glMix.TEXTURE1);
    glMix.bindTexture(glMix.TEXTURE_2D, texB);

    const cf = glMix.getUniformLocation(mixProg, 'crossfader');
    if (cf) glMix.uniform1f(cf, crossfader);
    const mm = glMix.getUniformLocation(mixProg, 'mixMode');
    if (mm) glMix.uniform1i(mm, mixModeInt);
    const fv = glMix.getUniformLocation(mixProg, 'outputFlipV');
    if (fv) glMix.uniform1f(fv, flipV ? 1 : 0);
    const fh = glMix.getUniformLocation(mixProg, 'outputFlipH');
    if (fh) glMix.uniform1f(fh, flipH ? 1 : 0);
    const ro = glMix.getUniformLocation(mixProg, 'outputRotation');
    if (ro) glMix.uniform1i(ro, rotation === 0 ? 0 : rotation === 90 ? 1 : rotation === 180 ? 2 : 3);
    const tA = glMix.getUniformLocation(mixProg, 'texA');
    if (tA) glMix.uniform1i(tA, 0);
    const tB = glMix.getUniformLocation(mixProg, 'texB');
    if (tB) glMix.uniform1i(tB, 1);

    glMix.bindBuffer(glMix.ARRAY_BUFFER, bufMix);
    const loc = glMix.getAttribLocation(mixProg, 'a_pos');
    glMix.enableVertexAttribArray(loc);
    glMix.vertexAttribPointer(loc, 2, glMix.FLOAT, false, 0, 0);
    glMix.drawArrays(glMix.TRIANGLE_STRIP, 0, 4);
  }

  return {
    outputCanvas,
    deckCanvasA,
    deckCanvasB,
    get hasShader() {
      return progA !== null || progB !== null;
    },
    applyMessage(msg: VjStreamMsg): void {
      if (!msg || !msg.type) return;
      if (msg.type === 'shader') {
        const gl = msg.deck === 'A' ? glA : glB;
        const oldProg = msg.deck === 'A' ? progA : progB;
        if (oldProg) gl.deleteProgram(oldProg);
        try {
          const p = linkGlProgram(gl, VJ_STREAM_VERT, msg.preparedSource);
          if (msg.deck === 'A') {
            progA = p;
            metaA = msg.meta;
          } else {
            progB = p;
            metaB = msg.meta;
          }
        } catch (e) {
          if (msg.deck === 'A') progA = null;
          else progB = null;
          console.warn('[VJ stream] shader compile failed for deck ' + msg.deck + ':', e);
        }
      } else if (msg.type === 'frame') {
        crossfader = msg.crossfader;
        mixModeInt = msg.mixModeInt;
        flipV = msg.flipV;
        flipH = msg.flipH;
        rotation = msg.rotation;
        mouseAX = msg.mouseAX ?? msg.mouseX ?? 0.5;
        mouseAY = msg.mouseAY ?? msg.mouseY ?? 0.5;
        mouseBX = msg.mouseBX ?? msg.mouseX ?? 0.5;
        mouseBY = msg.mouseBY ?? msg.mouseY ?? 0.5;
        paramsA = msg.paramsA;
        paramsB = msg.paramsB;
      } else if (msg.type === 'clear') {
        const gl = msg.deck === 'A' ? glA : glB;
        const oldProg = msg.deck === 'A' ? progA : progB;
        if (oldProg) gl.deleteProgram(oldProg);
        if (msg.deck === 'A') {
          progA = null;
          metaA = [];
        } else {
          progB = null;
          metaB = [];
        }
      }
    },
    renderFrame(): void {
      renderDeck(glA, deckCanvasA, progA, bufA, paramsA, metaA, mouseAX, mouseAY);
      renderDeck(glB, deckCanvasB, progB, bufB, paramsB, metaB, mouseBX, mouseBY);
      renderMix();
    },
    resizeOutput(w: number, h: number): void {
      if (outputCanvas.width !== w || outputCanvas.height !== h) {
        outputCanvas.width = w;
        outputCanvas.height = h;
      }
    },
  };
}

export function connectVjRemoteStream(
  onMessage: (msg: VjStreamMsg) => void,
  streamQuery: string,
  onStatus?: (text: string) => void
): () => void {
  const url =
    typeof window !== 'undefined' && window.location.origin
      ? `${window.location.origin}/api/vj-output/stream?${streamQuery}`
      : `/api/vj-output/stream?${streamQuery}`;
  let es = new EventSource(url);
  let reconnectAttempts = 0;
  let closed = false;

  function attachHandlers(source: EventSource): void {
    source.onopen = () => {
      reconnectAttempts = 0;
      onStatus?.('Connected. Waiting for VJ signal...');
    };
    source.onmessage = (ev: MessageEvent<string>) => {
      try {
        onMessage(JSON.parse(ev.data) as VjStreamMsg);
      } catch {
        /* ignore */
      }
    };
    source.onerror = () => {
      source.close();
      if (closed) return;
      reconnectAttempts++;
      const delay = Math.min(reconnectAttempts * 1000, 5000);
      onStatus?.('Reconnecting...');
      setTimeout(() => {
        if (closed) return;
        es = new EventSource(url);
        attachHandlers(es);
      }, delay);
    };
  }

  onStatus?.('Connecting to stream...');
  attachHandlers(es);
  return () => {
    closed = true;
    es.close();
  };
}
