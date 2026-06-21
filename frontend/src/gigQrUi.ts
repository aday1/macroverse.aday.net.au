import { buildGigAudienceStreamUrl, buildGigJoinUrl, buildGigVrAudienceUrl, buildGigVrControllerUrl, drawGigQr } from './gigQr.js';
import { copyGigUrl, GIG_MANUAL_URL_ITEMS } from './gigQrItems.js';
import { refreshGigUrlOriginOverride } from './gigUrlOrigin.js';
import { openJumpIntoVrChooser } from './jumpIntoVr.js';
import { getVjSessionId } from './vjSession.js';
import { ensureVjTokens } from './vjTokens.js';
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
  isGigStreamQrVisible,
  resetGigOutputQrLayout,
  setGigOutputQrLayout,
  setGigOutputQrMix,
  setGigOutputQrVisible,
  setGigStreamQrVisible,
} from './gigOutputQr.js';
import { createRichSlider, updateSliderFill } from './ui/richSlider.js';

export interface GigQrUiBlock {
  root: HTMLElement;
  refresh: (sessionId?: string) => void;
  isPanelVisible: () => boolean;
}

const VJ_PREVIEW_QR_HIDDEN_KEY = 'macroverse-vj-preview-qr-hidden';
const VJ_PREVIEW_QR_CANVAS_MIN = 120;
const VJ_PREVIEW_QR_CANVAS_MAX = 360;

function readQrPanelHidden(): boolean {
  try {
    return localStorage.getItem(VJ_PREVIEW_QR_HIDDEN_KEY) === '1';
  } catch {
    return false;
  }
}

function saveQrPanelHidden(hidden: boolean): void {
  try {
    localStorage.setItem(VJ_PREVIEW_QR_HIDDEN_KEY, hidden ? '1' : '0');
  } catch (_) {}
}

function canvasSizeForPanel(panelWidth: number): number {
  const inner = Math.max(0, panelWidth - 16);
  return Math.max(VJ_PREVIEW_QR_CANVAS_MIN, Math.min(VJ_PREVIEW_QR_CANVAS_MAX, Math.round(inner)));
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
  const rich = createRichSlider({
    label: labelText,
    min,
    max,
    step,
    value: getVal(),
    colorKey: 'gig-qr-' + labelText,
    formatValue: (v) => format(v),
    onInput: (v) => onInput(v)
  });
  parent.appendChild(rich.root);
  return rich.input;
}

/** Compact join QR + display controls for the VJ preview area. */
export function createVjPreviewGigQrBlock(): GigQrUiBlock {
  let panelVisible = !readQrPanelHidden();

  const root = document.createElement('div');
  root.className = 'vj-preview-gig-qr';
  root.style.cssText =
    'display:flex;flex-direction:column;align-items:stretch;gap:6px;flex:0 0 auto;min-width:160px;max-width:400px;overflow:hidden;';

  const headerRow = document.createElement('div');
  headerRow.style.cssText =
    'display:flex;align-items:center;justify-content:space-between;gap:6px;width:100%;';

  const label = document.createElement('div');
  label.style.cssText =
    'font-size:9px;text-transform:uppercase;color:var(--amiga-copper);letter-spacing:0.08em;line-height:1.3;flex:1;text-align:left;';
  label.textContent = 'Scan to stream the viz';

  const hideBtn = document.createElement('button');
  hideBtn.type = 'button';
  hideBtn.style.cssText =
    'font-size:8px;padding:2px 6px;background:var(--amiga-surface);color:var(--crt-dim);border:1px solid var(--bevel-dark);cursor:pointer;white-space:nowrap;';
  hideBtn.title = 'Hide or show the stream QR panel';

  const body = document.createElement('div');
  body.className = 'vj-preview-gig-qr__body';
  body.style.cssText = 'display:flex;flex-direction:column;align-items:center;gap:6px;width:100%;';

  const canvasWrap = document.createElement('div');
  canvasWrap.style.cssText = 'width:100%;display:flex;justify-content:center;';

  const canvas = document.createElement('canvas');
  canvas.width = 200;
  canvas.height = 200;
  canvas.style.cssText =
    'background:#fff;padding:4px;border:1px solid var(--bevel-dark);border-radius:4px;display:block;max-width:100%;height:auto;';
  canvasWrap.appendChild(canvas);

  const mixRow = document.createElement('div');
  mixRow.style.cssText = 'width:100%;';
  mixRow.title = 'Blend shader vs QR on the VJ output canvas';
  const mixRich = createRichSlider({
    label: 'Mix (shader / QR)',
    min: 0,
    max: 100,
    step: 1,
    value: Math.round(getGigOutputQrMix() * 100),
    colorKey: 'gig-qr-mix',
    formatValue: (v) => v + '% QR'
  });
  const mixSlider = mixRich.input;
  mixRow.appendChild(mixRich.root);

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
    mixRich.setValue(Math.round((e.detail?.mix ?? getGigOutputQrMix()) * 100));
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
      updateSliderFill(s);
      const valEl = s.closest('.mv-rich-slider')?.querySelector('.mv-rich-slider__value');
      if (valEl) valEl.textContent = sliderFormats[i](vals[i]);
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

  const streamQrBtn = document.createElement('button');
  streamQrBtn.type = 'button';
  streamQrBtn.textContent = 'Show QR on stream link';
  streamQrBtn.title =
    'Show join QR on audience stream pages (phones/tablets watching the live link). Viewers can hide it on their device.';
  streamQrBtn.style.cssText = btnStyle;

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

  const jumpVrBtn = document.createElement('button');
  jumpVrBtn.type = 'button';
  jumpVrBtn.textContent = 'Jump into VR';
  jumpVrBtn.title = 'Audience or VJ controller WebXR for this session';
  jumpVrBtn.style.cssText = btnStyle + 'font-weight:bold;border-color:var(--amiga-copper);';

  const copyBtn = document.createElement('button');
  copyBtn.type = 'button';
  copyBtn.textContent = 'Copy stream link';
  copyBtn.title = 'VJ output URL (same as QR on output)';
  copyBtn.style.cssText = btnStyle;

  const copyVrBtn = document.createElement('button');
  copyVrBtn.type = 'button';
  copyVrBtn.textContent = 'Copy VR audience';
  copyVrBtn.title = 'WebXR audience URL — immersive view + optional mouse steer';
  copyVrBtn.style.cssText = btnStyle;

  const copyVrVjBtn = document.createElement('button');
  copyVrVjBtn.type = 'button';
  copyVrVjBtn.textContent = 'Copy VR VJ';
  copyVrVjBtn.title = 'WebXR VJ controller — decks, mix, Auto VJ, live code';
  copyVrVjBtn.style.cssText = btnStyle;

  const manualUrls = document.createElement('details');
  manualUrls.open = true;
  manualUrls.style.cssText =
    'width:100%;padding:6px;border:1px solid var(--bevel-dark);background:rgba(0,0,0,0.18);box-sizing:border-box;';
  const manualSummary = document.createElement('summary');
  manualSummary.textContent = 'Manual stream URLs';
  manualSummary.style.cssText =
    'font-size:8px;text-transform:uppercase;color:var(--amiga-copper);letter-spacing:0.06em;cursor:pointer;';
  const manualList = document.createElement('div');
  manualList.style.cssText = 'display:flex;flex-direction:column;gap:6px;margin-top:6px;';
  manualUrls.appendChild(manualSummary);
  manualUrls.appendChild(manualList);

  const manualUrlRows: Array<{ input: HTMLInputElement; buildUrl: (sid: string) => string }> = [];
  const manualInputStyle =
    'width:100%;min-width:0;box-sizing:border-box;font-size:8px;padding:3px 5px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);font-family:ui-monospace,SFMono-Regular,Consolas,monospace;';
  for (const item of GIG_MANUAL_URL_ITEMS) {
    const row = document.createElement('div');
    row.style.cssText = 'display:flex;flex-direction:column;gap:3px;width:100%;';
    const rowHead = document.createElement('div');
    rowHead.style.cssText = 'display:flex;align-items:center;justify-content:space-between;gap:4px;';
    const rowLabel = document.createElement('span');
    rowLabel.textContent = item.label;
    rowLabel.title = item.hint;
    rowLabel.style.cssText = 'font-size:8px;color:var(--crt-dim);line-height:1.2;';
    const actionWrap = document.createElement('span');
    actionWrap.style.cssText = 'display:flex;gap:3px;flex:0 0 auto;';
    const copyUrlBtn = document.createElement('button');
    copyUrlBtn.type = 'button';
    copyUrlBtn.textContent = 'Copy';
    copyUrlBtn.style.cssText =
      'font-size:7px;padding:2px 4px;background:var(--amiga-surface);color:var(--amiga-copper);border:1px solid var(--bevel-dark);cursor:pointer;';
    const openUrlBtn = document.createElement('button');
    openUrlBtn.type = 'button';
    openUrlBtn.textContent = 'Open';
    openUrlBtn.style.cssText = copyUrlBtn.style.cssText;
    const input = document.createElement('input');
    input.type = 'text';
    input.readOnly = true;
    input.title = `${item.label}: ${item.hint}`;
    input.style.cssText = manualInputStyle;
    input.addEventListener('focus', () => input.select());
    input.addEventListener('click', () => input.select());
    copyUrlBtn.addEventListener('click', () => copyGigUrl(input.value || item.buildUrl(getVjSessionId()), copyUrlBtn));
    openUrlBtn.addEventListener('click', () => {
      window.open(input.value || item.buildUrl(getVjSessionId()), '_blank', 'noopener,noreferrer');
    });
    actionWrap.appendChild(copyUrlBtn);
    actionWrap.appendChild(openUrlBtn);
    rowHead.appendChild(rowLabel);
    rowHead.appendChild(actionWrap);
    row.appendChild(rowHead);
    row.appendChild(input);
    manualList.appendChild(row);
    manualUrlRows.push({ input, buildUrl: item.buildUrl });
  }

  const syncManualUrls = (sessionId = getVjSessionId()) => {
    for (const row of manualUrlRows) row.input.value = row.buildUrl(sessionId);
  };

  const syncOutputQrBtn = () => {
    outputQrBtn.textContent = isGigOutputQrVisible() ? 'Hide QR on output' : 'Show QR on output';
    streamQrBtn.textContent = isGigStreamQrVisible()
      ? 'Hide QR on stream link'
      : 'Show QR on stream link';
    fullscreenBtn.textContent = isGigAudienceDisplayActive() ? 'Close fullscreen' : 'Fullscreen display';
  };

  outputQrBtn.addEventListener('click', () => {
    if (isGigOutputQrVisible()) setGigOutputQrVisible(false);
    else setGigOutputQrVisible(true, 450);
    syncOutputQrBtn();
  });

  streamQrBtn.addEventListener('click', () => {
    if (isGigStreamQrVisible()) setGigStreamQrVisible(false);
    else setGigStreamQrVisible(true, 450);
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

  copyVrBtn.addEventListener('click', () => {
    const url = buildGigVrAudienceUrl(getVjSessionId(), 'dome');
    void navigator.clipboard?.writeText(url).then(() => {
      const t = copyVrBtn.textContent;
      copyVrBtn.textContent = 'Copied';
      setTimeout(() => {
        copyVrBtn.textContent = t;
      }, 1500);
    });
  });

  copyVrVjBtn.addEventListener('click', () => {
    const url = buildGigVrControllerUrl(getVjSessionId(), 'dome');
    void navigator.clipboard?.writeText(url).then(() => {
      const t = copyVrVjBtn.textContent;
      copyVrVjBtn.textContent = 'Copied';
      setTimeout(() => {
        copyVrVjBtn.textContent = t;
      }, 1500);
    });
  });

  jumpVrBtn.addEventListener('click', () => openJumpIntoVrChooser());

  window.addEventListener('macroverse-gig-output-qr-visible', () => syncOutputQrBtn());
  window.addEventListener('macroverse-gig-stream-qr-visible', () => syncOutputQrBtn());

  btnRow.appendChild(outputQrBtn);
  btnRow.appendChild(streamQrBtn);
  btnRow.appendChild(fullscreenBtn);
  btnRow.appendChild(displayWindowBtn);
  btnRow.appendChild(jumpVrBtn);
  btnRow.appendChild(copyBtn);
  btnRow.appendChild(copyVrBtn);
  btnRow.appendChild(copyVrVjBtn);

  window.addEventListener('macroverse-vj-audience-participation', ((e: CustomEvent<{ enabled: boolean }>) => {
    audienceCheck.checked = Boolean(e.detail?.enabled);
  }) as EventListener);

  root.appendChild(headerRow);
  headerRow.appendChild(label);
  headerRow.appendChild(hideBtn);
  body.appendChild(canvasWrap);
  body.appendChild(mixRow);
  body.appendChild(audienceRow);
  body.appendChild(layoutSection);
  body.appendChild(manualUrls);
  body.appendChild(btnRow);
  root.appendChild(body);

  const syncHideBtn = () => {
    hideBtn.textContent = panelVisible ? 'Hide' : 'Show QR';
    body.style.display = panelVisible ? '' : 'none';
    root.style.minWidth = panelVisible ? '160px' : '0';
    root.style.maxWidth = panelVisible ? '400px' : '72px';
    root.style.flex = panelVisible ? root.style.flex || '0 0 220px' : '0 0 auto';
  };

  const emitVisibility = () => {
    window.dispatchEvent(
      new CustomEvent('macroverse-vj-preview-qr-visible', { detail: { visible: panelVisible } })
    );
  };

  hideBtn.addEventListener('click', () => {
    panelVisible = !panelVisible;
    saveQrPanelHidden(!panelVisible);
    syncHideBtn();
    emitVisibility();
    if (panelVisible) void redrawQr();
  });

  syncHideBtn();
  emitVisibility();

  syncOutputQrBtn();

  let redrawPending = false;
  const redrawQr = async (sessionId?: string) => {
    if (!panelVisible) return;
    const sid = sessionId ?? getVjSessionId();
    const px = canvasSizeForPanel(root.offsetWidth || 220);
    try {
      await ensureVjTokens(sid);
    } catch {
      /* fallback URLs still work */
    }
    syncManualUrls(sid);
    await drawGigQr(canvas, buildGigAudienceStreamUrl(sid), px);
  };

  const refresh = (sessionId?: string) => {
    if (redrawPending) return;
    redrawPending = true;
    void redrawQr(sessionId).finally(() => {
      redrawPending = false;
    });
  };

  if (typeof ResizeObserver !== 'undefined') {
    const ro = new ResizeObserver(() => refresh());
    ro.observe(root);
  }

  window.setTimeout(() => refresh(), 0);
  syncManualUrls();
  void refreshGigUrlOriginOverride().then(() => {
    syncManualUrls();
    void refresh();
  });

  return { root, refresh, isPanelVisible: () => panelVisible };
}
