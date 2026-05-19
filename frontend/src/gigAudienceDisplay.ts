import { buildGigOutputUrl, drawGigQr } from './gigQr.js';
import { getVjSessionId } from './vjSession.js';

const STORAGE_MIX = 'macroverse-gig-audience-mix';
const OVERLAY_ID = 'macroverse-gig-audience-display';
const DEFAULT_FADE_MS = 450;

export interface GigAudienceDisplayOptions {
  sessionId?: string;
  mix?: number;
  fadeMs?: number;
  qrVisible?: boolean;
}

function clampMix(v: number): number {
  return Math.max(0, Math.min(1, v));
}

function loadStoredMix(): number {
  try {
    const v = parseFloat(localStorage.getItem(STORAGE_MIX) || '0.65');
    return Number.isFinite(v) ? clampMix(v) : 0.65;
  } catch {
    return 0.65;
  }
}

function saveMix(mix: number): void {
  try {
    localStorage.setItem(STORAGE_MIX, String(clampMix(mix)));
  } catch {
    /* ignore */
  }
}

function animateOpacity(
  el: HTMLElement,
  from: number,
  to: number,
  durationMs: number,
  onDone?: () => void
): void {
  const start = performance.now();
  el.style.transition = 'none';
  el.style.opacity = String(from);
  const tick = (now: number) => {
    const t = durationMs <= 0 ? 1 : Math.min(1, (now - start) / durationMs);
    const eased = t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2;
    el.style.opacity = String(from + (to - from) * eased);
    if (t < 1) {
      requestAnimationFrame(tick);
    } else {
      el.style.transition = '';
      onDone?.();
    }
  };
  requestAnimationFrame(tick);
}

type VizSource = 'canvas' | 'iframe';

class GigAudienceDisplayController {
  readonly root: HTMLElement;
  private readonly bgCanvas: HTMLCanvasElement;
  private readonly bgIframe: HTMLIFrameElement | null;
  private readonly vizLayer: HTMLElement;
  private readonly qrLayer: HTMLElement;
  private readonly mixSlider: HTMLInputElement;
  private sessionId: string;
  private mix = loadStoredMix();
  qrVisible = false;
  private rafId = 0;
  private onKey: ((e: KeyboardEvent) => void) | null = null;
  private vizSource: VizSource;
  private disposed = false;

  constructor(host: HTMLElement, sessionId: string, vizSource: VizSource) {
    this.sessionId = sessionId;
    this.vizSource = vizSource;

    this.root = document.createElement('div');
    this.root.id = OVERLAY_ID;
    this.root.style.cssText =
      'position:fixed;inset:0;z-index:200000;background:#000;overflow:hidden;';

    this.vizLayer = document.createElement('div');
    this.vizLayer.style.cssText = 'position:absolute;inset:0;';

    this.bgCanvas = document.createElement('canvas');
    this.bgCanvas.style.cssText = 'width:100%;height:100%;display:block;object-fit:cover;';

    if (vizSource === 'iframe') {
      this.bgIframe = document.createElement('iframe');
      this.bgIframe.src = buildGigOutputUrl(sessionId);
      this.bgIframe.style.cssText = 'position:absolute;inset:0;width:100%;height:100%;border:0;';
      this.bgIframe.setAttribute('title', 'VJ viz');
      this.vizLayer.appendChild(this.bgIframe);
    } else {
      this.bgIframe = null;
      this.vizLayer.appendChild(this.bgCanvas);
    }

    this.qrLayer = document.createElement('div');
    this.qrLayer.style.cssText = [
      'position:absolute',
      'inset:0',
      'display:flex',
      'flex-direction:column',
      'align-items:center',
      'justify-content:center',
      'gap:20px',
      'padding:24px',
      'box-sizing:border-box',
      'pointer-events:auto',
    ].join(';');

    const title = document.createElement('h1');
    title.textContent = 'Scan to stream the viz';
    title.style.cssText =
      'margin:0;font-size:clamp(1.5rem,4vw,2.5rem);color:#f8dd36;font-weight:600;text-align:center;font-family:system-ui,sans-serif;text-shadow:0 2px 12px rgba(0,0,0,0.85);';

    const sessionEl = document.createElement('p');
    sessionEl.className = 'gig-audience-session';
    sessionEl.textContent = `Session: ${sessionId}`;
    sessionEl.style.cssText =
      'margin:0;font-size:clamp(0.9rem,2vw,1.2rem);color:#e2e8f0;font-family:monospace;text-shadow:0 1px 8px rgba(0,0,0,0.9);';

    const qrCanvas = document.createElement('canvas');
    qrCanvas.className = 'gig-audience-qr';
    qrCanvas.width = 420;
    qrCanvas.height = 420;
    qrCanvas.style.cssText =
      'background:#fff;padding:12px;border-radius:8px;max-width:min(90vw,420px);height:auto;box-shadow:0 8px 32px rgba(0,0,0,0.5);';
    void drawGigQr(qrCanvas, buildGigOutputUrl(sessionId));

    const hint = document.createElement('p');
    hint.textContent = 'Point your phone camera at the code';
    hint.style.cssText =
      'margin:0;font-size:1rem;color:#e2e8f0;text-align:center;text-shadow:0 1px 8px rgba(0,0,0,0.9);';

    const controls = document.createElement('div');
    controls.style.cssText =
      'display:flex;flex-wrap:wrap;align-items:center;justify-content:center;gap:12px;margin-top:8px;padding:12px 16px;background:rgba(0,0,0,0.55);border-radius:8px;border:1px solid #334155;';

    const mixLabel = document.createElement('label');
    mixLabel.style.cssText =
      'display:flex;align-items:center;gap:8px;font-size:13px;color:#cbd5e1;font-family:system-ui,sans-serif;';
    mixLabel.textContent = 'Mix';

    this.mixSlider = document.createElement('input');
    this.mixSlider.type = 'range';
    this.mixSlider.min = '0';
    this.mixSlider.max = '100';
    this.mixSlider.value = String(Math.round(this.mix * 100));
    this.mixSlider.style.cssText = 'width:140px;cursor:pointer;';
    this.mixSlider.title = '0 = shader/viz only, 100 = QR only';

    const mixValue = document.createElement('span');
    mixValue.className = 'gig-audience-mix-value';
    mixValue.style.cssText = 'font-size:12px;color:#94a3b8;min-width:4.5rem;font-variant-numeric:tabular-nums;';
    mixValue.textContent = `${Math.round(this.mix * 100)}% QR`;

    this.mixSlider.addEventListener('input', () => {
      this.setMix(Number(this.mixSlider.value) / 100);
      mixValue.textContent = `${Math.round(this.mix * 100)}% QR`;
    });

    mixLabel.appendChild(document.createTextNode('Shader '));
    mixLabel.appendChild(this.mixSlider);
    mixLabel.appendChild(mixValue);

    const hideQrBtn = document.createElement('button');
    hideQrBtn.type = 'button';
    hideQrBtn.className = 'gig-audience-hide-qr';
    hideQrBtn.textContent = 'Hide QR';
    hideQrBtn.style.cssText =
      'padding:8px 14px;font-size:13px;cursor:pointer;background:#1e293b;color:#e2e8f0;border:1px solid #475569;border-radius:6px;';

    const closeBtn = document.createElement('button');
    closeBtn.type = 'button';
    closeBtn.textContent = 'Close (Esc)';
    closeBtn.style.cssText = hideQrBtn.style.cssText;

    hideQrBtn.addEventListener('click', () => {
      if (this.qrVisible) {
        setGigAudienceQrVisible(false);
      } else {
        setGigAudienceQrVisible(true);
      }
    });

    closeBtn.addEventListener('click', () => dismissGigAudienceDisplay());

    controls.appendChild(mixLabel);
    controls.appendChild(hideQrBtn);
    controls.appendChild(closeBtn);

    this.qrLayer.appendChild(title);
    this.qrLayer.appendChild(sessionEl);
    this.qrLayer.appendChild(qrCanvas);
    this.qrLayer.appendChild(hint);
    this.qrLayer.appendChild(controls);

    this.root.appendChild(this.vizLayer);
    this.root.appendChild(this.qrLayer);
    host.appendChild(this.root);

    this.applyMixInstant();
    this.qrLayer.style.opacity = '0';
    this.qrVisible = false;

    if (vizSource === 'canvas') {
      this.startCanvasCopyLoop();
    }

    this.onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') dismissGigAudienceDisplay();
    };
    document.addEventListener('keydown', this.onKey);
  }

  private startCanvasCopyLoop(): void {
    const draw = () => {
      if (this.disposed) return;
      void import('./panels/vjDeck.js').then((mod) => {
        const src = mod.vjOutputCanvasRef;
        if (src && src.width > 0 && src.height > 0) {
          const w = this.root.clientWidth || window.innerWidth;
          const h = this.root.clientHeight || window.innerHeight;
          if (this.bgCanvas.width !== w || this.bgCanvas.height !== h) {
            this.bgCanvas.width = w;
            this.bgCanvas.height = h;
          }
          const ctx = this.bgCanvas.getContext('2d');
          if (ctx) {
            ctx.fillStyle = '#000';
            ctx.fillRect(0, 0, w, h);
            const scale = Math.max(w / src.width, h / src.height);
            const dw = src.width * scale;
            const dh = src.height * scale;
            const dx = (w - dw) / 2;
            const dy = (h - dh) / 2;
            ctx.drawImage(src, dx, dy, dw, dh);
          }
        }
      });
      this.rafId = requestAnimationFrame(draw);
    };
    this.rafId = requestAnimationFrame(draw);
  }

  private applyMixInstant(): void {
    const qrOpacity = this.qrVisible ? this.mix : 0;
    const vizOpacity = 1 - (this.qrVisible ? this.mix * 0.85 : 0);
    this.qrLayer.style.opacity = String(qrOpacity);
    this.vizLayer.style.opacity = String(Math.max(0.15, vizOpacity));
  }

  setMix(mix: number): void {
    this.mix = clampMix(mix);
    this.mixSlider.value = String(Math.round(this.mix * 100));
    saveMix(this.mix);
    this.applyMixInstant();
    const val = document.querySelector('.gig-audience-mix-value');
    if (val) val.textContent = `${Math.round(this.mix * 100)}% QR`;
    window.dispatchEvent(
      new CustomEvent('macroverse-gig-audience-mix', { detail: { mix: this.mix } })
    );
  }

  getMix(): number {
    return this.mix;
  }

  fadeQrIn(fadeMs = DEFAULT_FADE_MS): void {
    this.qrVisible = true;
    const hideBtn = this.root.querySelector('.gig-audience-hide-qr') as HTMLButtonElement | null;
    if (hideBtn) hideBtn.textContent = 'Hide QR';
    const targetQr = this.mix;
    const targetViz = Math.max(0.15, 1 - this.mix * 0.85);
    animateOpacity(this.qrLayer, Number(this.qrLayer.style.opacity) || 0, targetQr, fadeMs);
    animateOpacity(
      this.vizLayer,
      Number(this.vizLayer.style.opacity) || 1,
      targetViz,
      fadeMs
    );
    window.dispatchEvent(new CustomEvent('macroverse-gig-audience-qr-visible', { detail: { visible: true } }));
  }

  fadeQrOut(fadeMs = DEFAULT_FADE_MS, onDone?: () => void): void {
    this.qrVisible = false;
    const hideBtn = this.root.querySelector('.gig-audience-hide-qr') as HTMLButtonElement | null;
    if (hideBtn) hideBtn.textContent = 'Show QR';
    animateOpacity(this.qrLayer, Number(this.qrLayer.style.opacity) || 1, 0, fadeMs);
    animateOpacity(this.vizLayer, Number(this.vizLayer.style.opacity) || 0.2, 1, fadeMs, onDone);
    window.dispatchEvent(new CustomEvent('macroverse-gig-audience-qr-visible', { detail: { visible: false } }));
  }

  refreshQr(sessionId?: string): void {
    if (sessionId) this.sessionId = sessionId;
    const qr = this.root.querySelector('.gig-audience-qr') as HTMLCanvasElement | null;
    const sess = this.root.querySelector('.gig-audience-session');
    if (sess) sess.textContent = `Session: ${this.sessionId}`;
    if (qr) void drawGigQr(qr, buildGigOutputUrl(this.sessionId));
    if (this.bgIframe) {
      this.bgIframe.src = buildGigOutputUrl(this.sessionId);
    }
  }

  dispose(): void {
    this.disposed = true;
    if (this.rafId) cancelAnimationFrame(this.rafId);
    if (this.onKey) document.removeEventListener('keydown', this.onKey);
    this.root.remove();
  }
}

let controller: GigAudienceDisplayController | null = null;
let pageController: GigAudienceDisplayController | null = null;

export function isGigAudienceDisplayActive(): boolean {
  return controller !== null || pageController !== null;
}

export function isGigAudienceQrVisible(): boolean {
  return controller?.qrVisible ?? pageController?.qrVisible ?? false;
}

export function getGigAudienceMix(): number {
  return controller?.getMix() ?? pageController?.getMix() ?? loadStoredMix();
}

export function setGigAudienceMix(mix: number): void {
  controller?.setMix(mix);
  pageController?.setMix(mix);
}

export function setGigAudienceQrVisible(visible: boolean, fadeMs = DEFAULT_FADE_MS): void {
  const c = controller ?? pageController;
  if (!c) return;
  if (visible) c.fadeQrIn(fadeMs);
  else c.fadeQrOut(fadeMs);
}

/** Fullscreen audience display on the current window (shader + QR mix). */
export function showGigAudienceDisplay(options: GigAudienceDisplayOptions = {}): void {
  if (controller) {
    if (options.sessionId) controller.refreshQr(options.sessionId);
    if (options.mix !== undefined) controller.setMix(options.mix);
    if (options.qrVisible !== false) controller.fadeQrIn(options.fadeMs);
    return;
  }

  dismissGigAudienceDisplay(false);

  const sid = options.sessionId ?? getVjSessionId();
  controller = new GigAudienceDisplayController(document.body, sid, 'canvas');
  if (options.mix !== undefined) controller.setMix(options.mix);
  else controller.setMix(loadStoredMix());

  void document.documentElement.requestFullscreen?.().catch(() => {});

  if (options.qrVisible !== false) {
    controller.fadeQrIn(options.fadeMs ?? DEFAULT_FADE_MS);
  }
}

/** Mount compositor into a host element (gig-join-qr.html page). */
export function mountGigAudienceDisplayPage(
  host: HTMLElement,
  sessionId: string,
  options: GigAudienceDisplayOptions = {}
): GigAudienceDisplayController {
  pageController = new GigAudienceDisplayController(host, sessionId, 'iframe');
  if (options.mix !== undefined) pageController.setMix(options.mix);
  else pageController.setMix(loadStoredMix());
  if (options.qrVisible !== false) {
    pageController.fadeQrIn(options.fadeMs ?? DEFAULT_FADE_MS);
  }
  return pageController;
}

export function dismissGigAudienceDisplay(fadeOut = true): void {
  const fadeMs = DEFAULT_FADE_MS;
  const done = () => {
    controller?.dispose();
    pageController?.dispose();
    controller = null;
    pageController = null;
    if (document.fullscreenElement) {
      void document.exitFullscreen?.().catch(() => {});
    }
  };

  if (fadeOut && (controller?.qrVisible || pageController?.qrVisible)) {
    const c = controller ?? pageController;
    c?.fadeQrOut(fadeMs, () => {
      setTimeout(done, 80);
    });
    if (controller && pageController) {
      pageController.fadeQrOut(fadeMs);
    }
  } else if (fadeOut && (controller || pageController)) {
    const root = (controller ?? pageController)!.root;
    animateOpacity(root, 1, 0, fadeMs, done);
  } else {
    done();
  }
}

/** @deprecated use showGigAudienceDisplay */
export function showGigJoinDisplayOverlay(sessionId?: string): void {
  showGigAudienceDisplay({ sessionId, qrVisible: true });
}

/** @deprecated use dismissGigAudienceDisplay */
export function dismissGigJoinDisplayOverlay(): void {
  dismissGigAudienceDisplay(true);
}

export function openGigJoinDisplayWindow(sessionId?: string): Window | null {
  const sid = encodeURIComponent((sessionId ?? getVjSessionId()).trim().slice(0, 64) || 'default');
  const mix = Math.round(getGigAudienceMix() * 100);
  const url = `${window.location.origin}/gig-join-qr.html?sessionId=${sid}&mix=${mix}`;
  const w = window.open(url, 'macroverse-gig-join-qr', 'noopener,noreferrer,width=1280,height=720');
  if (w) {
    w.addEventListener('load', () => {
      try {
        w.document.documentElement.requestFullscreen?.();
      } catch {
        /* ignore */
      }
    });
  }
  return w;
}
