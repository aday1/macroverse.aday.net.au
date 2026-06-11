const SCANLINE_KEY = 'macroverse-vj-output-scanline';
const VIGNETTE_KEY = 'macroverse-vj-output-vignette';

export interface OutputFxState {
  scanlines: boolean;
  vignette: boolean;
}

function loadBool(key: string, fallback = false): boolean {
  try {
    const v = localStorage.getItem(key);
    if (v === null) return fallback;
    return v === 'true' || v === '1';
  } catch {
    return fallback;
  }
}

function saveBool(key: string, value: boolean): void {
  try {
    localStorage.setItem(key, value ? 'true' : 'false');
  } catch {
    /* ignore */
  }
}

let fxState: OutputFxState = {
  scanlines: loadBool(SCANLINE_KEY),
  vignette: loadBool(VIGNETTE_KEY),
};

function dispatchChange(): void {
  if (typeof window === 'undefined') return;
  window.dispatchEvent(
    new CustomEvent('macroverse-vj-output-fx-changed', {
      detail: { state: { ...fxState } },
    })
  );
}

export function getOutputFxState(): Readonly<OutputFxState> {
  return fxState;
}

export function setOutputScanlines(enabled: boolean): void {
  fxState = { ...fxState, scanlines: enabled };
  saveBool(SCANLINE_KEY, enabled);
  dispatchChange();
}

export function setOutputVignette(enabled: boolean): void {
  fxState = { ...fxState, vignette: enabled };
  saveBool(VIGNETTE_KEY, enabled);
  dispatchChange();
}

export function toggleOutputScanlines(): boolean {
  setOutputScanlines(!fxState.scanlines);
  return fxState.scanlines;
}

export function toggleOutputVignette(): boolean {
  setOutputVignette(!fxState.vignette);
  return fxState.vignette;
}

/** Full-screen quad pass: sample input texture, apply scanlines + vignette. */
export const OUTPUT_FX_FRAG_SRC = `precision highp float;
uniform sampler2D u_input;
uniform vec2 u_resolution;
uniform float u_scanlines;
uniform float u_vignette;
varying vec2 v_uv;

void main() {
  vec2 uv = vec2(v_uv.x, 1.0 - v_uv.y);
  vec3 col = texture2D(u_input, uv).rgb;

  if (u_scanlines > 0.5) {
    float line = mod(gl_FragCoord.y, 4.0);
    col *= 0.88 + 0.12 * step(2.0, line);
  }

  if (u_vignette > 0.5) {
    vec2 p = uv - 0.5;
    float vig = 1.0 - dot(p, p) * 1.6;
    col *= clamp(vig, 0.35, 1.0);
  }

  gl_FragColor = vec4(col, 1.0);
}`;

export interface OutputFxGlslUniforms {
  scanlines: number;
  vignette: number;
}

export function outputFxGlslUniforms(state?: OutputFxState): OutputFxGlslUniforms {
  const s = state ?? fxState;
  return {
    scanlines: s.scanlines ? 1 : 0,
    vignette: s.vignette ? 1 : 0,
  };
}

export function applyOutputFxGlslUniforms(
  gl: WebGLRenderingContext,
  program: WebGLProgram,
  state?: OutputFxState
): void {
  const u = outputFxGlslUniforms(state);
  const scanLoc = gl.getUniformLocation(program, 'u_scanlines');
  const vigLoc = gl.getUniformLocation(program, 'u_vignette');
  if (scanLoc) gl.uniform1f(scanLoc, u.scanlines);
  if (vigLoc) gl.uniform1f(vigLoc, u.vignette);
}

/** Canvas 2D post-process on an already-rendered frame (destination = source canvas). */
export function applyOutputFxCanvas(
  canvas: HTMLCanvasElement,
  state?: OutputFxState
): void {
  const s = state ?? fxState;
  if (!s.scanlines && !s.vignette) return;

  const w = canvas.width;
  const h = canvas.height;
  if (w <= 0 || h <= 0) return;

  const ctx = canvas.getContext('2d');
  if (!ctx) return;

  ctx.save();

  if (s.vignette) {
    const gr = ctx.createRadialGradient(w / 2, h / 2, w * 0.25, w / 2, h / 2, w * 0.72);
    gr.addColorStop(0, 'rgba(0,0,0,0)');
    gr.addColorStop(0.65, 'rgba(0,0,0,0.15)');
    gr.addColorStop(1, 'rgba(0,0,0,0.55)');
    ctx.fillStyle = gr;
    ctx.fillRect(0, 0, w, h);
  }

  if (s.scanlines) {
    ctx.fillStyle = 'rgba(0,0,0,0.12)';
    for (let y = 0; y < h; y += 4) {
      ctx.fillRect(0, y, w, 2);
    }
  }

  ctx.restore();
}

/** Composite output FX onto a separate overlay canvas (non-destructive). */
export function paintOutputFxOverlay(
  ctx: CanvasRenderingContext2D,
  w: number,
  h: number,
  state?: OutputFxState
): void {
  const s = state ?? fxState;
  if (!s.scanlines && !s.vignette) return;

  ctx.save();

  if (s.vignette) {
    const gr = ctx.createRadialGradient(w / 2, h / 2, w * 0.25, w / 2, h / 2, w * 0.72);
    gr.addColorStop(0, 'rgba(0,0,0,0)');
    gr.addColorStop(0.65, 'rgba(0,0,0,0.15)');
    gr.addColorStop(1, 'rgba(0,0,0,0.55)');
    ctx.fillStyle = gr;
    ctx.fillRect(0, 0, w, h);
  }

  if (s.scanlines) {
    ctx.fillStyle = 'rgba(0,0,0,0.12)';
    for (let y = 0; y < h; y += 4) {
      ctx.fillRect(0, y, w, 2);
    }
  }

  ctx.restore();
}
