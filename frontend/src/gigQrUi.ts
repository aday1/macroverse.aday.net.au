import { buildGigAudienceStreamUrl, buildGigJoinUrl, drawGigQr } from './gigQr.js';
import { getVjSessionId } from './vjSession.js';
import {
  dismissGigAudienceDisplay,
  isGigAudienceDisplayActive,
  openGigJoinDisplayWindow,
  setGigAudienceMix,
  showGigAudienceDisplay,
} from './gigAudienceDisplay.js';
import {
  getAudienceParticipationEnabled,
  pushAudienceParticipation,
} from './vjAudienceParticipation.js';
import { publishVjConfig } from './vjWs.js';
import {
  getGigOutputQrLayout,
  getGigOutputQrMix,
  isGigOutputQrVisible,
  resetGigOutputQrLayout,
  setGigOutputQrLayout,
  setGigOutputQrMix,
  setGigOutputQrVisible,
} from './gigOutputQr.js';

export interface GigQrUiBlock {
  root: HTMLElement;
  refresh: (sessionId?: string) => void;
}

function addLayoutSlider(
  parent: HTMLElement,
  labelText: string,
  min: number,
  max: number,
  step: number,
  getVal: () => number,
  onInput: (v: number) => void,
  format: (v: number) => string
): HTMLInputElement {
  const row = document.createElement('label');
  row.style.cssText =
    'display:flex;flex-direction:column;gap:2px;width:100%;font-size:8px;color:var(--crt-dim);';
  const head = document.createElement('span');
  const slider = document.createElement('input');
  slider.type = 'range';
  slider.min = String(min);
  slider.max = String(max);
  slider.step = String(step);
  slider.value = String(getVal());
  slider.style.cssText = 'width:100%;cursor:pointer;';
  const syncLabel = () => {
    head.textContent = `${labelText}: ${format(Number(slider.value))}`;
  };
  syncLabel();
  slider.addEventListener('input', () => {
    onInput(Number(slider.value));
    syncLabel();
  });
  row.appendChild(head);
  row.appendChild(slider);
  parent.appendChild(row);
  return slider;
}

/** Compact join QR + display controls for the VJ preview area. */
export function createVjPreviewGigQrBlock(): GigQrUiBlock {
  const root = document.createElement('div');
  root.style.cssText =
    'display:flex;flex-direction:column;align-items:center;gap:6px;flex:0 0 auto;min-width:148px;max-width:220px;';

  const label = document.createElement('div');
  label.style.cssText =
    'font-size:9px;text-transform:uppercase;color:var(--amiga-copper);letter-spacing:0.08em;text-align:center;line-height:1.3;';
  label.textContent = 'Scan to stream the viz';

  const canvas = document.createElement('canvas');
  canvas.width = 148;
  canvas.height = 148;
  canvas.style.cssText =
    'background:#fff;padding:4px;border:1px solid var(--bevel-dark);border-radius:4px;width:148px;height:148px;';

  const mixRow = document.createElement('label');
  mixRow.style.cssText =
    'display:flex;flex-direction:column;gap:2px;width:100%;font-size:8px;color:var(--crt-dim);';
  mixRow.title = 'Blend shader vs QR on the VJ output canvas';
  const mixLabel = document.createElement('span');
  mixLabel.textContent = 'Mix (shader / QR)';
  const mixSlider = document.createElement('input');
  mixSlider.type = 'range';
  mixSlider.min = '0';
  mixSlider.max = '100';
  mixSlider.value = String(Math.round(getGigOutputQrMix() * 100));
  mixSlider.style.cssText = 'width:100%;cursor:pointer;';
  mixRow.appendChild(mixLabel);
  mixRow.appendChild(mixSlider);

  const audienceRow = document.createElement('label');
  audienceRow.style.cssText =
    'display:flex;align-items:flex-start;gap:6px;width:100%;font-size:8px;color:var(--crt-dim);cursor:pointer;line-height:1.35;';
  audienceRow.title =
    'When on, phones on the stream link can drag X/Y on the preview (mouse uniforms only). No deck control.';
  const audienceCheck = document.createElement('input');
  audienceCheck.type = 'checkbox';
  audienceCheck.checked = getAudienceParticipationEnabled();
  const audienceText = document.createElement('span');
  audienceText.textContent = 'Audience participation (touch X/Y on stream)';
  audienceRow.appendChild(audienceCheck);
  audienceRow.appendChild(audienceText);

  audienceCheck.addEventListener('change', () => {
    const on = audienceCheck.checked;
    void pushAudienceParticipation(on).then(() => {
      publishVjConfig({ audienceParticipation: on });
    });
  });

  mixSlider.addEventListener('input', () => {
    const v = Number(mixSlider.value) / 100;
    setGigOutputQrMix(v);
    setGigAudienceMix(v);
  });

  const onMixEvent = ((e: CustomEvent<{ mix: number }>) => {
    mixSlider.value = String(Math.round((e.detail?.mix ?? getGigOutputQrMix()) * 100));
  }) as EventListener;
  window.addEventListener('macroverse-gig-output-qr-mix', onMixEvent);
  window.addEventListener('macroverse-gig-audience-mix', onMixEvent);

  const layoutSection = document.createElement('div');
  layoutSection.style.cssText =
    'display:flex;flex-direction:column;gap:4px;width:100%;padding-top:4px;border-top:1px solid var(--bevel-dark);';
  const layoutTitle = document.createElement('div');
  layoutTitle.style.cssText =
    'font-size:8px;text-transform:uppercase;color:var(--amiga-copper);letter-spacing:0.06em;';
  layoutTitle.textContent = 'QR on projector output';
  const layoutHint = document.createElement('div');
  layoutHint.style.cssText = 'font-size:7px;color:var(--crt-dim);line-height:1.3;';
  layoutHint.textContent = 'Drag the preview output to move. Sliders: size, rotate, FX.';
  layoutSection.appendChild(layoutTitle);
  layoutSection.appendChild(layoutHint);

  const sliders: HTMLInputElement[] = [];
  sliders.push(
    addLayoutSlider(
      layoutSection,
      'X',
      0,
      100,
      1,
      () => Math.round(getGigOutputQrLayout().posX * 100),
      (v) => setGigOutputQrLayout({ posX: v / 100 }),
      (v) => `${v}%`
    )
  );
  sliders.push(
    addLayoutSlider(
      layoutSection,
      'Y',
      0,
      100,
      1,
      () => Math.round(getGigOutputQrLayout().posY * 100),
      (v) => setGigOutputQrLayout({ posY: v / 100 }),
      (v) => `${v}%`
    )
  );
  sliders.push(
    addLayoutSlider(
      layoutSection,
      'Size',
      12,
      72,
      1,
      () => Math.round(getGigOutputQrLayout().scale * 100),
      (v) => setGigOutputQrLayout({ scale: v / 100 }),
      (v) => `${v}%`
    )
  );
  sliders.push(
    addLayoutSlider(
      layoutSection,
      'Rotate',
      -180,
      180,
      1,
      () => Math.round(getGigOutputQrLayout().rotation),
      (v) => setGigOutputQrLayout({ rotation: v }),
      (v) => `${v}°`
    )
  );
  sliders.push(
    addLayoutSlider(
      layoutSection,
      'FX',
      0,
      100,
      1,
      () => Math.round(getGigOutputQrLayout().fx * 100),
      (v) => setGigOutputQrLayout({ fx: v / 100 }),
      (v) => `${v}%`
    )
  );

  const resetLayoutBtn = document.createElement('button');
  resetLayoutBtn.type = 'button';
  resetLayoutBtn.textContent = 'Reset QR layout';
  resetLayoutBtn.style.cssText =
    'font-size:9px;padding:4px 6px;background:var(--amiga-surface);color:var(--crt-dim);border:1px solid var(--bevel-dark);cursor:pointer;width:100%;';
  resetLayoutBtn.addEventListener('click', () => resetGigOutputQrLayout());
  layoutSection.appendChild(resetLayoutBtn);

  const sliderLabels = ['X', 'Y', 'Size', 'Rotate', 'FX'] as const;
  const sliderFormats = [
    (v: number) => `${v}%`,
    (v: number) => `${v}%`,
    (v: number) => `${v}%`,
    (v: number) => `${v}°`,
    (v: number) => `${v}%`,
  ];
  const syncLayoutSliders = () => {
    const L = getGigOutputQrLayout();
    const vals = [
      Math.round(L.posX * 100),
      Math.round(L.posY * 100),
      Math.round(L.scale * 100),
      Math.round(L.rotation),
      Math.round(L.fx * 100),
    ];
    sliders.forEach((s, i) => {
      s.value = String(vals[i]);
      const head = s.parentElement?.querySelector('span');
      if (head) head.textContent = `${sliderLabels[i]}: ${sliderFormats[i](vals[i])}`;
    });
  };
  window.addEventListener('macroverse-gig-output-qr-layout', syncLayoutSliders as EventListener);
  syncLayoutSliders();

  const btnRow = document.createElement('div');
  btnRow.style.cssText = 'display:flex;flex-direction:column;gap:4px;width:100%;';

  const btnStyle =
    'font-size:9px;padding:4px 6px;background:var(--amiga-surface);color:var(--amiga-copper);border:1px solid var(--bevel-dark);cursor:pointer;width:100%;';

  const outputQrBtn = document.createElement('button');
  outputQrBtn.type = 'button';
  outputQrBtn.textContent = 'Show QR on output';
  outputQrBtn.title = 'Burn QR into VJ output (preview, pop-out, Pi HDMI, OBS). Drag preview to reposition.';
  outputQrBtn.style.cssText = btnStyle;

  const fullscreenBtn = document.createElement('button');
  fullscreenBtn.type = 'button';
  fullscreenBtn.textContent = 'Fullscreen display';
  fullscreenBtn.title = 'Optional fullscreen shader + QR on this monitor';
  fullscreenBtn.style.cssText = btnStyle;

  const displayWindowBtn = document.createElement('button');
  displayWindowBtn.type = 'button';
  displayWindowBtn.textContent = 'Open display window';
  displayWindowBtn.title = 'Separate window: live viz + QR';
  displayWindowBtn.style.cssText = btnStyle;

  const copyBtn = document.createElement('button');
  copyBtn.type = 'button';
  copyBtn.textContent = 'Copy stream link';
  copyBtn.title = 'VJ output URL (same as QR on output)';
  copyBtn.style.cssText = btnStyle;

  const syncOutputQrBtn = () => {
    outputQrBtn.textContent = isGigOutputQrVisible() ? 'Hide QR on output' : 'Show QR on output';
    fullscreenBtn.textContent = isGigAudienceDisplayActive() ? 'Close fullscreen' : 'Fullscreen display';
  };

  outputQrBtn.addEventListener('click', () => {
    if (isGigOutputQrVisible()) setGigOutputQrVisible(false);
    else setGigOutputQrVisible(true, 450);
    syncOutputQrBtn();
  });

  fullscreenBtn.addEventListener('click', () => {
    if (isGigAudienceDisplayActive()) {
      dismissGigAudienceDisplay(true);
    } else {
      showGigAudienceDisplay({
        sessionId: getVjSessionId(),
        mix: Number(mixSlider.value) / 100,
        qrVisible: true,
      });
    }
    syncOutputQrBtn();
  });

  displayWindowBtn.addEventListener('click', () => {
    const w = openGigJoinDisplayWindow(getVjSessionId());
    if (!w) {
      showGigAudienceDisplay({ sessionId: getVjSessionId(), mix: Number(mixSlider.value) / 100 });
    }
    syncOutputQrBtn();
  });

  copyBtn.addEventListener('click', () => {
    const url = buildGigAudienceStreamUrl(getVjSessionId());
    void navigator.clipboard?.writeText(url).then(() => {
      const t = copyBtn.textContent;
      copyBtn.textContent = 'Copied';
      setTimeout(() => {
        copyBtn.textContent = t;
      }, 1500);
    });
  });

  window.addEventListener('macroverse-gig-output-qr-visible', () => syncOutputQrBtn());

  btnRow.appendChild(outputQrBtn);
  btnRow.appendChild(fullscreenBtn);
  btnRow.appendChild(displayWindowBtn);
  btnRow.appendChild(copyBtn);

  window.addEventListener('macroverse-vj-audience-participation', ((e: CustomEvent<{ enabled: boolean }>) => {
    audienceCheck.checked = Boolean(e.detail?.enabled);
  }) as EventListener);

  root.appendChild(label);
  root.appendChild(canvas);
  root.appendChild(mixRow);
  root.appendChild(audienceRow);
  root.appendChild(layoutSection);
  root.appendChild(btnRow);

  syncOutputQrBtn();

  const refresh = (sessionId?: string) => {
    void drawGigQr(canvas, buildGigAudienceStreamUrl(sessionId));
  };

  window.setTimeout(() => refresh(), 0);

  return { root, refresh };
}
