import {
  createVjStreamRenderer,
  linkGlProgram,
  VJ_STREAM_QUAD,
  type VjStreamMsg,
} from './vjStreamCore.js';

export type VjVrDisplayMode = 'dome' | 'screen';

const VR_FS_VERT = `precision highp float;
attribute vec2 a_pos;
varying vec2 v_ndc;
void main() {
  v_ndc = a_pos;
  gl_Position = vec4(a_pos, 0.0, 1.0);
}`;

const VR_DOME_FRAG = `precision highp float;
uniform sampler2D uMix;
uniform mat4 uInvProjection;
uniform mat4 uViewTransform;
varying vec2 v_ndc;
vec2 dirToEquirect(vec3 d) {
  d = normalize(d);
  float lon = atan(d.z, d.x);
  float lat = asin(clamp(d.y, -1.0, 1.0));
  return vec2(lon / 6.2831853 + 0.5, 0.5 - lat / 3.14159265);
}
void main() {
  vec4 viewRay = uInvProjection * vec4(v_ndc, 1.0, 1.0);
  viewRay /= max(viewRay.w, 0.00001);
  vec3 dir = normalize((uViewTransform * vec4(viewRay.xyz, 0.0)).xyz);
  gl_FragColor = texture2D(uMix, dirToEquirect(dir));
}`;

const VR_SCREEN_VERT = `precision highp float;
attribute vec3 a_pos;
attribute vec2 a_uv;
uniform mat4 uViewProj;
varying vec2 v_uv;
void main() {
  v_uv = a_uv;
  gl_Position = uViewProj * vec4(a_pos, 1.0);
}`;

const VR_SCREEN_FRAG = `precision highp float;
uniform sampler2D uMix;
varying vec2 v_uv;
void main() {
  gl_FragColor = texture2D(uMix, v_uv);
}`;

function buildScreenQuad(w: number, h: number, dist: number): {
  positions: Float32Array;
  uvs: Float32Array;
} {
  const hw = w * 0.5;
  const hh = h * 0.5;
  return {
    positions: new Float32Array([-hw, -hh, -dist, hw, -hh, -dist, -hw, hh, -dist, hw, hh, -dist]),
    uvs: new Float32Array([0, 0, 1, 0, 0, 1, 1, 1]),
  };
}

function mat4Multiply(out: Float32Array, a: Float32Array, b: Float32Array): void {
  const o = new Float32Array(16);
  for (let c = 0; c < 4; c++) {
    for (let r = 0; r < 4; r++) {
      o[c * 4 + r] =
        a[0 * 4 + r] * b[c * 4 + 0] +
        a[1 * 4 + r] * b[c * 4 + 1] +
        a[2 * 4 + r] * b[c * 4 + 2] +
        a[3 * 4 + r] * b[c * 4 + 3];
    }
  }
  out.set(o);
}

function mat4Invert(out: Float32Array, a: Float32Array): boolean {
  const a00 = a[0];
  const a01 = a[1];
  const a02 = a[2];
  const a03 = a[3];
  const a10 = a[4];
  const a11 = a[5];
  const a12 = a[6];
  const a13 = a[7];
  const a20 = a[8];
  const a21 = a[9];
  const a22 = a[10];
  const a23 = a[11];
  const a30 = a[12];
  const a31 = a[13];
  const a32 = a[14];
  const a33 = a[15];

  const b00 = a00 * a11 - a01 * a10;
  const b01 = a00 * a12 - a02 * a10;
  const b02 = a00 * a13 - a03 * a10;
  const b03 = a01 * a12 - a02 * a11;
  const b04 = a01 * a13 - a03 * a11;
  const b05 = a02 * a13 - a03 * a12;
  const b06 = a20 * a31 - a21 * a30;
  const b07 = a20 * a32 - a22 * a30;
  const b08 = a20 * a33 - a23 * a30;
  const b09 = a21 * a32 - a22 * a31;
  const b10 = a21 * a33 - a23 * a31;
  const b11 = a22 * a33 - a23 * a32;

  const det = b00 * b11 - b01 * b10 + b02 * b09 + b03 * b08 - b04 * b07 + b05 * b06;
  if (!det) return false;
  const invDet = 1.0 / det;

  out[0] = (a11 * b11 - a12 * b10 + a13 * b09) * invDet;
  out[1] = (a02 * b10 - a01 * b11 - a03 * b09) * invDet;
  out[2] = (a31 * b05 - a32 * b04 + a33 * b03) * invDet;
  out[3] = (a22 * b04 - a21 * b05 - a23 * b03) * invDet;
  out[4] = (a12 * b08 - a10 * b11 - a13 * b07) * invDet;
  out[5] = (a00 * b11 - a02 * b08 + a03 * b07) * invDet;
  out[6] = (a32 * b02 - a30 * b05 - a33 * b01) * invDet;
  out[7] = (a20 * b05 - a22 * b02 + a23 * b01) * invDet;
  out[8] = (a10 * b10 - a11 * b08 + a13 * b06) * invDet;
  out[9] = (a01 * b08 - a00 * b10 - a03 * b06) * invDet;
  out[10] = (a30 * b04 - a31 * b02 + a33 * b00) * invDet;
  out[11] = (a21 * b02 - a20 * b04 - a23 * b00) * invDet;
  out[12] = (a11 * b07 - a10 * b09 - a12 * b06) * invDet;
  out[13] = (a00 * b09 - a01 * b07 + a02 * b06) * invDet;
  out[14] = (a31 * b01 - a30 * b03 - a32 * b00) * invDet;
  out[15] = (a20 * b03 - a21 * b01 + a22 * b00) * invDet;
  return true;
}

function mat4FromXrRigidTransform(t: XRRigidTransform): Float32Array {
  if (t.matrix) return new Float32Array(t.matrix);
  const m = new Float32Array(16);
  const o = t.orientation;
  const p = t.position;
  const x = o.x;
  const y = o.y;
  const z = o.z;
  const w = o.w;
  const x2 = x + x;
  const y2 = y + y;
  const z2 = z + z;
  const xx = x * x2;
  const xy = x * y2;
  const xz = x * z2;
  const yy = y * y2;
  const yz = y * z2;
  const zz = z * z2;
  const wx = w * x2;
  const wy = w * y2;
  const wz = w * z2;
  m[0] = 1 - (yy + zz);
  m[1] = xy + wz;
  m[2] = xz - wy;
  m[3] = 0;
  m[4] = xy - wz;
  m[5] = 1 - (xx + zz);
  m[6] = yz + wx;
  m[7] = 0;
  m[8] = xz + wy;
  m[9] = yz - wx;
  m[10] = 1 - (xx + yy);
  m[11] = 0;
  m[12] = p.x;
  m[13] = p.y;
  m[14] = p.z;
  m[15] = 1;
  return m;
}

function getXrViewMatrix(view: XRView): Float32Array {
  if (view.transform.inverse?.matrix) return new Float32Array(view.transform.inverse.matrix);
  const transform = mat4FromXrRigidTransform(view.transform);
  const inverse = new Float32Array(16);
  if (mat4Invert(inverse, transform)) return inverse;
  return transform;
}

export interface VjVrStreamView {
  renderer: ReturnType<typeof createVjStreamRenderer>;
  mixCanvas: HTMLCanvasElement;
  applyStreamMessage(msg: VjStreamMsg): void;
  renderPreviewTo(canvas: HTMLCanvasElement): void;
  resizeMix(w: number, h: number): void;
}

export function createVjVrStreamView(): VjVrStreamView {
  const mixCanvas = document.createElement('canvas');
  mixCanvas.width = 1280;
  mixCanvas.height = 720;
  const renderer = createVjStreamRenderer(mixCanvas);
  return {
    renderer,
    mixCanvas,
    applyStreamMessage(msg: VjStreamMsg): void {
      renderer.applyMessage(msg);
    },
    renderPreviewTo(canvas: HTMLCanvasElement): void {
      renderer.renderFrame();
      const ctx = canvas.getContext('2d');
      if (ctx) ctx.drawImage(mixCanvas, 0, 0, canvas.width, canvas.height);
    },
    resizeMix(w: number, h: number): void {
      renderer.resizeOutput(w, h);
    },
  };
}

export interface VjVrImmersive {
  isActive(): boolean;
  enter(): Promise<boolean>;
  exit(): void;
  setDisplayMode(mode: VjVrDisplayMode): void;
  consumeEndedBySession(): boolean;
}

export function createVjVrImmersive(
  stream: VjVrStreamView,
  displayMode: VjVrDisplayMode,
  domOverlayRoot?: HTMLElement | null
): VjVrImmersive {
  let xrSession: XRSession | null = null;
  let xrRefSpace: XRReferenceSpace | null = null;
  let xrGl: WebGLRenderingContext | null = null;
  let xrMixTex: WebGLTexture | null = null;
  let xrProg: WebGLProgram | null = null;
  let xrPosBuf: WebGLBuffer | null = null;
  let xrUvBuf: WebGLBuffer | null = null;
  let xrFsBuf: WebGLBuffer | null = null;
  let xrRaf = 0;
  let endedBySession = false;
  let activeDisplayMode = displayMode;

  function initXrScene(gl: WebGLRenderingContext): void {
    if (xrProg) gl.deleteProgram(xrProg);
    if (xrMixTex) gl.deleteTexture(xrMixTex);
    if (xrPosBuf) gl.deleteBuffer(xrPosBuf);
    if (xrUvBuf) gl.deleteBuffer(xrUvBuf);
    if (xrFsBuf) gl.deleteBuffer(xrFsBuf);
    xrProg = null;
    xrMixTex = null;
    xrPosBuf = null;
    xrUvBuf = null;
    xrFsBuf = null;

    if (activeDisplayMode === 'dome') {
      xrProg = linkGlProgram(gl, VR_FS_VERT, VR_DOME_FRAG);
      xrFsBuf = gl.createBuffer();
      gl.bindBuffer(gl.ARRAY_BUFFER, xrFsBuf);
      gl.bufferData(gl.ARRAY_BUFFER, VJ_STREAM_QUAD, gl.STATIC_DRAW);
    } else {
      const screen = buildScreenQuad(4.8, 2.7, 2.2);
      xrProg = linkGlProgram(gl, VR_SCREEN_VERT, VR_SCREEN_FRAG);
      xrPosBuf = gl.createBuffer();
      gl.bindBuffer(gl.ARRAY_BUFFER, xrPosBuf);
      gl.bufferData(gl.ARRAY_BUFFER, screen.positions, gl.STATIC_DRAW);
      xrUvBuf = gl.createBuffer();
      gl.bindBuffer(gl.ARRAY_BUFFER, xrUvBuf);
      gl.bufferData(gl.ARRAY_BUFFER, screen.uvs, gl.STATIC_DRAW);
    }
    xrMixTex = gl.createTexture()!;
    gl.bindTexture(gl.TEXTURE_2D, xrMixTex);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
  }

  function drawXrView(gl: WebGLRenderingContext, view: XRView): void {
    if (!xrProg || !xrMixTex) return;
    stream.renderer.renderFrame();
    gl.bindTexture(gl.TEXTURE_2D, xrMixTex);
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, stream.mixCanvas);
    gl.useProgram(xrProg);
    gl.uniform1i(gl.getUniformLocation(xrProg, 'uMix'), 0);
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, xrMixTex);

    if (activeDisplayMode === 'dome' && xrFsBuf) {
      const invProjection = new Float32Array(16);
      if (!mat4Invert(invProjection, new Float32Array(view.projectionMatrix))) return;
      const viewTransform = mat4FromXrRigidTransform(view.transform);
      gl.uniformMatrix4fv(gl.getUniformLocation(xrProg, 'uInvProjection'), false, invProjection);
      gl.uniformMatrix4fv(gl.getUniformLocation(xrProg, 'uViewTransform'), false, viewTransform);
      gl.bindBuffer(gl.ARRAY_BUFFER, xrFsBuf);
      const posLoc = gl.getAttribLocation(xrProg, 'a_pos');
      gl.enableVertexAttribArray(posLoc);
      gl.vertexAttribPointer(posLoc, 2, gl.FLOAT, false, 0, 0);
      gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
      return;
    }

    if (activeDisplayMode === 'screen' && xrPosBuf && xrUvBuf) {
      const proj = new Float32Array(view.projectionMatrix);
      const invView = getXrViewMatrix(view);
      const viewProj = new Float32Array(16);
      mat4Multiply(viewProj, proj, invView);
      gl.uniformMatrix4fv(gl.getUniformLocation(xrProg, 'uViewProj'), false, viewProj);
      gl.bindBuffer(gl.ARRAY_BUFFER, xrPosBuf);
      const posLoc = gl.getAttribLocation(xrProg, 'a_pos');
      gl.enableVertexAttribArray(posLoc);
      gl.vertexAttribPointer(posLoc, 3, gl.FLOAT, false, 0, 0);
      gl.bindBuffer(gl.ARRAY_BUFFER, xrUvBuf);
      const uvLoc = gl.getAttribLocation(xrProg, 'a_uv');
      gl.enableVertexAttribArray(uvLoc);
      gl.vertexAttribPointer(uvLoc, 2, gl.FLOAT, false, 0, 0);
      gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
    }
  }

  function onXrFrame(_time: number, frame: XRFrame): void {
    if (!xrSession || !xrRefSpace || !xrGl) return;
    const pose = frame.getViewerPose(xrRefSpace);
    const layer = xrSession.renderState.baseLayer as XRWebGLLayer | null;
    if (!pose || !layer) {
      xrRaf = xrSession.requestAnimationFrame(onXrFrame);
      return;
    }
    xrGl.bindFramebuffer(xrGl.FRAMEBUFFER, layer.framebuffer);
    xrGl.disable(xrGl.SCISSOR_TEST);
    xrGl.viewport(0, 0, layer.framebufferWidth, layer.framebufferHeight);
    xrGl.clearColor(0, 0, 0, 1);
    xrGl.clear(xrGl.COLOR_BUFFER_BIT | xrGl.DEPTH_BUFFER_BIT);
    for (const view of pose.views) {
      const vp = layer.getViewport(view);
      if (!vp) continue;
      xrGl.viewport(vp.x, vp.y, vp.width, vp.height);
      drawXrView(xrGl, view);
    }
    xrRaf = xrSession.requestAnimationFrame(onXrFrame);
  }

  return {
    isActive(): boolean {
      return xrSession !== null;
    },
    async enter(): Promise<boolean> {
      if (!navigator.xr || xrSession) return false;
      const supported = await navigator.xr.isSessionSupported('immersive-vr');
      if (!supported) return false;

      const canvas = document.createElement('canvas');
      const gl = canvas.getContext('webgl', { xrCompatible: true, preserveDrawingBuffer: true });
      if (!gl) return false;
      await gl.makeXRCompatible();

      const optionalFeatures = ['local-floor', 'local'];
      const sessionInit: XRSessionInit = { optionalFeatures };
      if (domOverlayRoot) {
        optionalFeatures.push('dom-overlay');
        sessionInit.domOverlay = { root: domOverlayRoot };
      }

      const session = await navigator.xr.requestSession('immersive-vr', sessionInit);
      xrSession = session;
      xrGl = gl;
      const layer = new XRWebGLLayer(session, gl);
      session.updateRenderState({ baseLayer: layer });
      initXrScene(gl);

      xrRefSpace = await session.requestReferenceSpace('local').catch(() =>
        session.requestReferenceSpace('local-floor')
      );

      session.addEventListener('end', () => {
        if (xrRaf) session.cancelAnimationFrame(xrRaf);
        endedBySession = true;
        xrSession = null;
        xrRefSpace = null;
        xrGl = null;
      });

      xrRaf = session.requestAnimationFrame(onXrFrame);
      return true;
    },
    exit(): void {
      xrSession?.end();
    },
    setDisplayMode(mode: VjVrDisplayMode): void {
      if (mode === activeDisplayMode) return;
      activeDisplayMode = mode;
      if (xrGl) initXrScene(xrGl);
    },
    consumeEndedBySession(): boolean {
      const ended = endedBySession;
      endedBySession = false;
      return ended;
    },
  };
}
