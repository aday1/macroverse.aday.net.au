interface XRSystem {
  isSessionSupported(mode: XRSessionMode): Promise<boolean>;
  requestSession(mode: XRSessionMode, options?: XRSessionInit): Promise<XRSession>;
}

interface Navigator {
  xr?: XRSystem;
}

type XRSessionMode = 'inline' | 'immersive-vr' | 'immersive-ar';

interface XRSession extends EventTarget {
  renderState: XRRenderState;
  domOverlayState?: unknown;
  requestReferenceSpace(type: XRReferenceSpaceType): Promise<XRReferenceSpace>;
  requestAnimationFrame(callback: XRFrameRequestCallback): number;
  cancelAnimationFrame(handle: number): void;
  updateRenderState(state: XRRenderStateInit): void;
  addEventListener(type: 'end', listener: () => void): void;
}

type XRReferenceSpaceType = 'viewer' | 'local' | 'local-floor' | 'bounded-floor' | 'unbounded';

interface XRReferenceSpace extends XRSpace {}

interface XRSpace {}

interface XRRenderState {
  baseLayer?: XRWebGLLayer | null;
}

interface XRRenderStateInit {
  baseLayer?: XRWebGLLayer | null;
  domOverlay?: { root: Element };
}

interface XRWebGLLayer {
  framebuffer: WebGLFramebuffer;
  framebufferWidth: number;
  framebufferHeight: number;
  getViewport(view: XRView): XRViewport | undefined;
}

interface XRViewport {
  x: number;
  y: number;
  width: number;
  height: number;
}

interface XRFrame {
  getViewerPose(referenceSpace: XRReferenceSpace): XRViewerPose | null;
}

interface XRViewerPose {
  views: readonly XRView[];
}

interface XRView {
  eye: XREye;
  projectionMatrix: Float32Array;
  transform: XRRigidTransform;
}

type XREye = 'none' | 'left' | 'right';

interface XRRigidTransform {
  position: DOMPointReadOnly;
  orientation: DOMPointReadOnly;
  matrix: Float32Array;
  inverse?: XRRigidTransform;
}

interface XRSessionInit {
  optionalFeatures?: string[];
  domOverlay?: { root: Element };
}

type XRFrameRequestCallback = (time: DOMHighResTimeStamp, frame: XRFrame) => void;

interface WebGLRenderingContext {
  makeXRCompatible(): Promise<void>;
}

interface WebGLContextAttributes {
  xrCompatible?: boolean;
}

declare var XRWebGLLayer: {
  prototype: XRWebGLLayer;
  new(session: XRSession, context: WebGLRenderingContext): XRWebGLLayer;
};
