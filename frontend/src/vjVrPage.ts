import { fetchIndex, fetchShader, postShaderSave } from './api.js';
import { audienceMouseQuery, fetchAudienceParticipationConfig } from './vjAudienceParticipation.js';
import { connectVjRemoteStream, type VjStreamMsg } from './vjStreamCore.js';
import {
  createVjVrImmersive,
  createVjVrStreamView,
  type VjVrDisplayMode,
} from './vjVrImmersive.js';
import {
  applyControlTokenFromUrl,
  ensureVjTokens,
  getControlTokenFromUrl,
  getViewTokenFromUrl,
  vjViewQuery,
} from './vjTokens.js';
import {
  connectVjSession,
  onRemoteVjControl,
  publishVjControl,
  publishVjShaderLive,
  type VjControlState,
} from './vjWs.js';
import type { IndexEntry } from './types.js';

function parseDisplayMode(): VjVrDisplayMode {
  const m = new URLSearchParams(window.location.search).get('mode');
  return m === 'screen' ? 'screen' : 'dome';
}

function isVjControllerMode(): boolean {
  const params = new URLSearchParams(window.location.search);
  if (params.get('role') === 'vj') return true;
  return Boolean(getControlTokenFromUrl());
}

function streamQuery(): string {
  const viewToken = getViewTokenFromUrl();
  if (viewToken) return `viewToken=${encodeURIComponent(viewToken)}`;
  return vjViewQuery();
}

const previewCanvas = document.getElementById('vjCanvas') as HTMLCanvasElement;
const statusEl = document.getElementById('status') as HTMLDivElement;
const roleBadge = document.getElementById('roleBadge') as HTMLSpanElement;
const xrBadge = document.getElementById('xrBadge') as HTMLSpanElement;
const interactHint = document.getElementById('interactHint') as HTMLDivElement;
const controllerPanel = document.getElementById('controllerPanel') as HTMLDivElement;
const audiencePanel = document.getElementById('audiencePanel') as HTMLDivElement;
const enterVrBtn = document.getElementById('enterVr') as HTMLButtonElement;
const exitVrBtn = document.getElementById('exitVr') as HTMLButtonElement;
const modeLabel = document.getElementById('modeLabel') as HTMLSpanElement;
const modeDomeBtn = document.getElementById('modeDome') as HTMLButtonElement;
const modeScreenBtn = document.getElementById('modeScreen') as HTMLButtonElement;
const vrHud = document.getElementById('vrHud') as HTMLDivElement;
const hudModeDomeBtn = document.getElementById('hudModeDome') as HTMLButtonElement;
const hudModeScreenBtn = document.getElementById('hudModeScreen') as HTMLButtonElement;

let currentDisplayMode: VjVrDisplayMode = parseDisplayMode();
const controllerMode = isVjControllerMode();
document.body.classList.toggle('vr-controller-mode', controllerMode);
document.body.classList.toggle('vr-audience-mode', !controllerMode);

function syncModeUi(): void {
  modeLabel.textContent = currentDisplayMode === 'dome' ? '360 dome' : 'cinema screen';
  modeDomeBtn?.classList.toggle('is-on', currentDisplayMode === 'dome');
  modeScreenBtn?.classList.toggle('is-on', currentDisplayMode === 'screen');
  hudModeDomeBtn?.classList.toggle('is-on', currentDisplayMode === 'dome');
  hudModeScreenBtn?.classList.toggle('is-on', currentDisplayMode === 'screen');
}

syncModeUi();
roleBadge.textContent = controllerMode ? 'VJ Controller VR' : 'Audience VR';

if (controllerMode) {
  controllerPanel.hidden = false;
  applyControlTokenFromUrl();
} else {
  audiencePanel.hidden = false;
}

const stream = createVjVrStreamView();
let immersive = createVjVrImmersive(stream, currentDisplayMode, vrHud);

function setDisplayMode(mode: VjVrDisplayMode): void {
  if (mode === currentDisplayMode) return;
  currentDisplayMode = mode;
  immersive.setDisplayMode(mode);
  syncModeUi();
  const params = new URLSearchParams(window.location.search);
  if (mode === 'screen') params.set('mode', 'screen');
  else params.delete('mode');
  const qs = params.toString();
  history.replaceState(null, '', qs ? `${window.location.pathname}?${qs}` : window.location.pathname);
}

modeDomeBtn?.addEventListener('click', () => setDisplayMode('dome'));
modeScreenBtn?.addEventListener('click', () => setDisplayMode('screen'));
hudModeDomeBtn?.addEventListener('click', () => setDisplayMode('dome'));
hudModeScreenBtn?.addEventListener('click', () => setDisplayMode('screen'));

async function probeWebXr(): Promise<void> {
  if (!navigator.xr) {
    xrBadge.textContent = 'No WebXR — flat preview only';
    xrBadge.classList.add('is-warn');
    enterVrBtn.disabled = true;
    return;
  }
  try {
    const supported = await navigator.xr.isSessionSupported('immersive-vr');
    if (supported) {
      xrBadge.textContent = 'WebXR ready';
      xrBadge.classList.add('is-ready');
    } else {
      xrBadge.textContent = 'Use Quest Browser for VR';
      xrBadge.classList.add('is-warn');
      enterVrBtn.disabled = true;
    }
  } catch {
    xrBadge.textContent = 'WebXR check failed';
    xrBadge.classList.add('is-warn');
    enterVrBtn.disabled = true;
  }
}

void probeWebXr();

function setStatus(text: string): void {
  statusEl.textContent = text;
  statusEl.style.opacity = '1';
}

function hideStatus(): void {
  statusEl.style.opacity = '0';
}

connectVjRemoteStream(
  (msg: VjStreamMsg) => {
    stream.applyStreamMessage(msg);
    if (stream.renderer.hasShader) hideStatus();
    if (msg.type === 'frame') syncControlFromFrame(msg);
  },
  streamQuery(),
  setStatus
);

let shaderList: IndexEntry[] = [];
let deckAIndex = 0;
let deckBIndex = 0;

function syncControlFromFrame(msg: Extract<VjStreamMsg, { type: 'frame' }>): void {
  const xfade = document.getElementById('xfade') as HTMLInputElement | null;
  const hudXfade = document.getElementById('hudXfade') as HTMLInputElement | null;
  if (xfade) xfade.value = String(msg.crossfader);
  if (hudXfade) hudXfade.value = String(msg.crossfader);
}

function deckLabel(entry: IndexEntry | undefined): string {
  return (entry?.fixedName ?? entry?.name ?? entry?.path ?? '—').slice(0, 40);
}

async function loadShaderList(): Promise<void> {
  try {
    shaderList = await fetchIndex();
    updateDeckLabels();
  } catch {
    shaderList = [];
  }
}

function updateDeckLabels(): void {
  const aEl = document.getElementById('deckALabel');
  const bEl = document.getElementById('deckBLabel');
  const hudAEl = document.getElementById('hudDeckALabel');
  const hudBEl = document.getElementById('hudDeckBLabel');
  if (aEl) aEl.textContent = deckLabel(shaderList[deckAIndex]);
  if (bEl) bEl.textContent = deckLabel(shaderList[deckBIndex]);
  if (hudAEl) hudAEl.textContent = deckLabel(shaderList[deckAIndex]);
  if (hudBEl) hudBEl.textContent = deckLabel(shaderList[deckBIndex]);
}

function publishDeckIndices(): void {
  publishVjControl({ deckAGlobalIndex: deckAIndex, deckBGlobalIndex: deckBIndex });
}

function bindControllerUi(): void {
  const xfade = document.getElementById('xfade') as HTMLInputElement;
  const hudXfade = document.getElementById('hudXfade') as HTMLInputElement;
  const mixMode = document.getElementById('mixMode') as HTMLSelectElement;
  const hudMixMode = document.getElementById('hudMixMode') as HTMLSelectElement;
  const autoVjBtn = document.getElementById('autoVjBtn') as HTMLButtonElement;
  const hudAutoVjBtn = document.getElementById('hudAutoVjBtn') as HTMLButtonElement;
  const autoVjBpm = document.getElementById('autoVjBpm') as HTMLInputElement;
  const hudAutoVjBpm = document.getElementById('hudAutoVjBpm') as HTMLInputElement;
  const codeEditor = document.getElementById('codeEditor') as HTMLTextAreaElement;
  const codePath = document.getElementById('codePath') as HTMLInputElement;
  const codeDeck = document.getElementById('codeDeck') as HTMLSelectElement;

  const setXfade = (v: number): void => {
    xfade.value = String(v);
    hudXfade.value = String(v);
    publishVjControl({ crossfader: v });
  };

  xfade.addEventListener('input', () => setXfade(parseFloat(xfade.value)));
  hudXfade.addEventListener('input', () => setXfade(parseFloat(hudXfade.value)));
  document.getElementById('xfadeA')?.addEventListener('click', () => setXfade(0));
  document.getElementById('xfadeB')?.addEventListener('click', () => setXfade(1));
  document.getElementById('hudXfadeA')?.addEventListener('click', () => setXfade(0));
  document.getElementById('hudXfadeB')?.addEventListener('click', () => setXfade(1));

  const setMixMode = (value: string): void => {
    mixMode.value = value;
    hudMixMode.value = value;
    publishVjControl({ mixMode: value });
  };

  mixMode.addEventListener('change', () => setMixMode(mixMode.value));
  hudMixMode.addEventListener('change', () => setMixMode(hudMixMode.value));

  const stepDeck = (deck: 'A' | 'B', delta: number): void => {
    if (shaderList.length === 0) return;
    if (deck === 'A') deckAIndex = (deckAIndex + delta + shaderList.length) % shaderList.length;
    else deckBIndex = (deckBIndex + delta + shaderList.length) % shaderList.length;
    updateDeckLabels();
    publishDeckIndices();
  };

  document.getElementById('deckAPrev')?.addEventListener('click', () => stepDeck('A', -1));
  document.getElementById('deckANext')?.addEventListener('click', () => stepDeck('A', 1));
  document.getElementById('deckBPrev')?.addEventListener('click', () => stepDeck('B', -1));
  document.getElementById('deckBNext')?.addEventListener('click', () => stepDeck('B', 1));
  document.getElementById('hudDeckAPrev')?.addEventListener('click', () => stepDeck('A', -1));
  document.getElementById('hudDeckANext')?.addEventListener('click', () => stepDeck('A', 1));
  document.getElementById('hudDeckBPrev')?.addEventListener('click', () => stepDeck('B', -1));
  document.getElementById('hudDeckBNext')?.addEventListener('click', () => stepDeck('B', 1));

  let autoVjOn = false;
  const syncAutoVjBtn = (): void => {
    autoVjBtn.textContent = autoVjOn ? 'Auto VJ On' : 'Auto VJ Off';
    hudAutoVjBtn.textContent = autoVjOn ? 'Auto VJ On' : 'Auto VJ Off';
    autoVjBtn.classList.toggle('is-on', autoVjOn);
    hudAutoVjBtn.classList.toggle('is-on', autoVjOn);
  };

  const setAutoVj = (on: boolean): void => {
    autoVjOn = on;
    syncAutoVjBtn();
    publishVjControl({ autoVjEnabled: autoVjOn, autoVjBpm: parseFloat(autoVjBpm.value) || 120 });
  };

  const setBpm = (raw: string): void => {
    const bpm = parseFloat(raw);
    if (bpm >= 30 && bpm <= 240) {
      const text = String(Math.round(bpm));
      autoVjBpm.value = text;
      hudAutoVjBpm.value = text;
      publishVjControl({ autoVjBpm: bpm });
    }
  };

  autoVjBtn.addEventListener('click', () => setAutoVj(!autoVjOn));
  hudAutoVjBtn.addEventListener('click', () => setAutoVj(!autoVjOn));
  autoVjBpm.addEventListener('change', () => setBpm(autoVjBpm.value));
  hudAutoVjBpm.addEventListener('change', () => setBpm(hudAutoVjBpm.value));

  const tapTimes: number[] = [];
  const tapTempo = (): void => {
    const t = performance.now();
    tapTimes.push(t);
    if (tapTimes.length > 4) tapTimes.shift();
    if (tapTimes.length < 2) return;
    const intervals: number[] = [];
    for (let i = 1; i < tapTimes.length; i++) intervals.push(tapTimes[i] - tapTimes[i - 1]);
    const avgMs = intervals.reduce((a, x) => a + x, 0) / intervals.length;
    const bpm = Math.round(60000 / avgMs);
    if (bpm >= 30 && bpm <= 240) {
      setBpm(String(bpm));
    }
  };
  document.getElementById('autoVjTap')?.addEventListener('click', tapTempo);
  document.getElementById('hudAutoVjTap')?.addEventListener('click', tapTempo);

  let codeDebounce: ReturnType<typeof setTimeout> | null = null;
  codeEditor.addEventListener('input', () => {
    if (codeDebounce) clearTimeout(codeDebounce);
    codeDebounce = setTimeout(() => {
      const path = codePath.value.trim();
      const deck = codeDeck.value === 'B' ? 'B' : 'A';
      if (path && codeEditor.value.trim()) {
        publishVjShaderLive(deck, path, codeEditor.value);
      }
    }, 350);
  });

  document.getElementById('codeLoad')?.addEventListener('click', () => {
    void (async () => {
      const path = codePath.value.trim();
      if (!path) return;
      try {
        codeEditor.value = await fetchShader(path);
      } catch {
        setStatus('Load failed');
      }
    })();
  });

  document.getElementById('codePush')?.addEventListener('click', () => {
    const path = codePath.value.trim();
    const deck = codeDeck.value === 'B' ? 'B' : 'A';
    if (path && codeEditor.value.trim()) {
      publishVjShaderLive(deck, path, codeEditor.value);
    }
  });

  document.getElementById('codeSave')?.addEventListener('click', () => {
    void (async () => {
      const path = codePath.value.trim();
      if (!path) return;
      try {
        await postShaderSave({ path: path.replace(/\\/g, '|'), content: codeEditor.value });
        publishVjShaderLive(codeDeck.value === 'B' ? 'B' : 'A', path, codeEditor.value);
      } catch {
        setStatus('Save failed');
      }
    })();
  });

  onRemoteVjControl((ctrl: VjControlState) => {
    if (typeof ctrl.crossfader === 'number') {
      xfade.value = String(ctrl.crossfader);
      hudXfade.value = String(ctrl.crossfader);
    }
    if (ctrl.mixMode) {
      mixMode.value = ctrl.mixMode;
      hudMixMode.value = ctrl.mixMode;
    }
    if (typeof ctrl.deckAGlobalIndex === 'number') {
      deckAIndex = ctrl.deckAGlobalIndex;
      updateDeckLabels();
    }
    if (typeof ctrl.deckBGlobalIndex === 'number') {
      deckBIndex = ctrl.deckBGlobalIndex;
      updateDeckLabels();
    }
    if (typeof ctrl.autoVjEnabled === 'boolean') {
      autoVjOn = ctrl.autoVjEnabled;
      syncAutoVjBtn();
    }
    if (typeof ctrl.autoVjBpm === 'number') {
      const text = String(Math.round(ctrl.autoVjBpm));
      autoVjBpm.value = text;
      hudAutoVjBpm.value = text;
    }
  });

  syncAutoVjBtn();

  const codeToggle = document.getElementById('codeSectionToggle');
  const codeBody = document.getElementById('codeSectionBody');
  let codeOpen = window.innerWidth >= 640;
  const syncCodeSection = (): void => {
    if (!codeToggle || !codeBody) return;
    codeBody.classList.toggle('is-collapsed', !codeOpen);
    codeToggle.textContent = codeOpen ? 'Live code -' : 'Live code +';
  };
  syncCodeSection();
  codeToggle?.addEventListener('click', () => {
    codeOpen = !codeOpen;
    syncCodeSection();
  });
}

function bindAudienceInteraction(): void {
  let participation = false;
  let lastPost = 0;

  async function refreshParticipation(): Promise<void> {
    participation = await fetchAudienceParticipationConfig();
    interactHint.classList.toggle('is-on', participation);
    previewCanvas.style.cursor = participation ? 'crosshair' : 'default';
  }

  void refreshParticipation();
  window.setInterval(() => void refreshParticipation(), 4000);

  function postMouse(mx: number, my: number): void {
    const now = performance.now();
    if (now - lastPost < 50) return;
    lastPost = now;
    void fetch(`/api/vj-output/audience-mouse?${audienceMouseQuery()}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ mouseX: mx, mouseY: my }),
    }).catch(() => {});
  }

  function setMouseFromClient(clientX: number, clientY: number): void {
    if (!participation) return;
    const rect = previewCanvas.getBoundingClientRect();
    const mx = Math.max(0, Math.min(1, (clientX - rect.left) / rect.width));
    const my = Math.max(0, Math.min(1, 1 - (clientY - rect.top) / rect.height));
    postMouse(mx, my);
  }

  previewCanvas.addEventListener('mousemove', (e) => setMouseFromClient(e.clientX, e.clientY));
  previewCanvas.addEventListener('touchmove', (e) => {
    if (e.touches.length) {
      setMouseFromClient(e.touches[0].clientX, e.touches[0].clientY);
      if (participation) e.preventDefault();
    }
  }, { passive: false });
  previewCanvas.addEventListener('touchstart', (e) => {
    if (e.touches.length) setMouseFromClient(e.touches[0].clientX, e.touches[0].clientY);
  }, { passive: true });
}

function resizePreview(): void {
  const dpr = window.devicePixelRatio || 1;
  const w = Math.round(previewCanvas.clientWidth * dpr);
  const h = Math.round(previewCanvas.clientHeight * dpr);
  if (w > 0 && h > 0) {
    previewCanvas.width = w;
    previewCanvas.height = h;
    stream.resizeMix(w, h);
  }
}
window.addEventListener('resize', resizePreview);
resizePreview();

function syncImmersiveUi(active: boolean): void {
  document.body.classList.toggle('immersive', active);
  enterVrBtn.hidden = active;
  exitVrBtn.hidden = !active;
}

function previewLoop(): void {
  if (immersive.consumeEndedBySession()) {
    syncImmersiveUi(false);
  }
  if (!immersive.isActive()) {
    stream.renderPreviewTo(previewCanvas);
  }
  requestAnimationFrame(previewLoop);
}
requestAnimationFrame(previewLoop);

async function enterImmersive(): Promise<void> {
  if (!stream.renderer.hasShader) {
    setStatus('Waiting for VJ signal...');
    return;
  }
  const ok = await immersive.enter();
  if (!ok) {
    setStatus('Could not enter VR. Use Quest Browser or a WebXR headset.');
    return;
  }
  syncImmersiveUi(true);
  hideStatus();
}

function exitImmersive(): void {
  immersive.exit();
  syncImmersiveUi(false);
}

enterVrBtn.addEventListener('click', () => void enterImmersive());
exitVrBtn.addEventListener('click', exitImmersive);
document.getElementById('hudExit')?.addEventListener('click', exitImmersive);

void (async () => {
  if (controllerMode) {
    await ensureVjTokens().catch(() => {});
    connectVjSession();
    bindControllerUi();
    await loadShaderList();
  } else {
    bindAudienceInteraction();
  }
})();
