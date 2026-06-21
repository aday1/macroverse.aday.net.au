import { fetchLocalStatus } from '../api.js';
import { audioEngine } from '../engines/audio.js';
import { midiEngine } from '../engines/midi.js';
import { getMonitorEntries, setMonitorUpdateCallback } from '../engines/midiOscMonitor.js';
import { oscEngine } from '../engines/osc.js';
import * as state from '../state.js';
import type { LocalStatusResponse } from '../types.js';

let panel: HTMLElement | null = null;
let bodyEl: HTMLElement | null = null;
let statusCache: LocalStatusResponse | null = null;
let refreshTimer: ReturnType<typeof setInterval> | null = null;

function esc(value: unknown): string {
  return String(value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function compactPath(path: string): string {
  if (!path) return '';
  if (path.length <= 58) return path;
  return path.slice(0, 24) + '...' + path.slice(-30);
}

function fmtBool(value: boolean | undefined, yes = 'on', no = 'off'): string {
  return value ? yes : no;
}

function currentShaderLabel(): string {
  const entry = state.currentEntry;
  if (!entry) return 'none';
  return entry.name || entry.fixedName || entry.path || 'shader #' + entry.id;
}

function fftSummary(): string {
  if (!audioEngine.active) return 'off';
  audioEngine.update();
  const bands = Array.from(audioEngine.bands || []);
  const peak = bands.reduce((m, v) => Math.max(m, v), 0);
  const avg = bands.length ? bands.reduce((s, v) => s + v, 0) / bands.length : 0;
  const device = audioEngine.deviceLabel ? ' ' + audioEngine.deviceLabel : '';
  return `${Math.round(avg * 100)}% avg / ${Math.round(peak * 100)}% peak${device}`;
}

function midiSummary(): string {
  if (!midiEngine.active) return 'off';
  const last = midiEngine.lastCC
    ? ` last CC${midiEngine.lastCC.cc}=${midiEngine.lastCC.val} ch${midiEngine.lastCC.ch + 1}`
    : '';
  return `${midiEngine.inputs.length} input(s)${last}`;
}

function oscSummary(): string {
  const server = statusCache?.osc;
  const serverText = server ? `server ${fmtBool(server.running)}:${server.port || 9000}` : 'server pending';
  const browserText = oscEngine.active ? `browser on:${oscEngine.port}` : 'browser off';
  const last = oscEngine.lastOscMessage ? ` ${oscEngine.lastOscMessage}` : '';
  return `${serverText} / ${browserText}${last}`;
}

function linkSummary(): string {
  const sessions = statusCache?.sessions || [];
  const bridged = sessions.find((s) => s.bridgeConnected);
  if (!bridged) return 'bridge not connected';
  return `bridge connected (${bridged.id || 'default'})`;
}

function renderBackendViewer(): void {
  if (!bodyEl) return;
  const st = statusCache;
  const lane = st?.lane || 'local';
  const privateOk = !st?.privateLibrary || st.privateAuthorized;
  const bind = st?.bindHost || 'pending';
  const sourceStatus = st?.sourceStatus || [];
  const validSources = sourceStatus.filter((s) => s.valid).length;
  const sourceLine = sourceStatus.length
    ? `${validSources}/${sourceStatus.length} valid`
    : `${(st?.sourcePaths || []).length} path(s)`;
  const sourcePath = (st?.sourcePaths || [])[0] || '';
  const recent = getMonitorEntries().slice(0, 5);

  if (panel) {
    panel.dataset.lane = lane;
    panel.dataset.privateOk = privateOk ? '1' : '0';
  }

  bodyEl.innerHTML = `
    <div class="backend-viewer-row backend-viewer-row--primary">
      <span>Lane</span><strong>${esc(lane)}${st?.privateLibrary ? ' private' : ''}</strong>
    </div>
    <div class="backend-viewer-row">
      <span>Bind</span><code>${esc(bind)}</code>
    </div>
    <div class="backend-viewer-row">
      <span>Library</span><strong>${esc(st?.shaderCount ?? state.entries.length)} shaders</strong>
    </div>
    <div class="backend-viewer-row" title="${esc(sourcePath)}">
      <span>Source</span><code>${esc(sourceLine)}${sourcePath ? ' - ' + esc(compactPath(sourcePath)) : ''}</code>
    </div>
    <div class="backend-viewer-row" title="${esc(state.currentPath || '')}">
      <span>Shader</span><strong>${esc(currentShaderLabel())}</strong>
    </div>
    <div class="backend-viewer-row">
      <span>MIDI</span><code>${esc(midiSummary())}</code>
    </div>
    <div class="backend-viewer-row">
      <span>OSC</span><code>${esc(oscSummary())}</code>
    </div>
    <div class="backend-viewer-row">
      <span>FFT</span><code>${esc(fftSummary())}</code>
    </div>
    <div class="backend-viewer-row">
      <span>Link</span><code>${esc(linkSummary())}</code>
    </div>
    <div class="backend-viewer-row">
      <span>Version</span><code>${esc(st?.gitRev || st?.version || 'dev')}${st?.gitDirty ? '+' : ''}</code>
    </div>
    ${st?.privateLibrary && !privateOk ? '<div class="backend-viewer-warning">Private source blocked: Aday marker not confirmed.</div>' : ''}
    ${st?.privateLibrary && bind !== '127.0.0.1' ? '<div class="backend-viewer-warning">Private lane is not loopback-bound.</div>' : ''}
    <div class="backend-viewer-log">
      ${recent.length ? recent.map((e) => `<div><span>${esc(e.type.toUpperCase())}</span> ${esc(e.text)}${e.device ? ' [' + esc(e.device) + ']' : ''}</div>`).join('') : '<div><span>MON</span> waiting</div>'}
    </div>
  `;
}

async function refreshServerStatus(): Promise<void> {
  try {
    statusCache = await fetchLocalStatus();
  } catch {
    statusCache = null;
  }
  renderBackendViewer();
}

function setCollapsed(collapsed: boolean): void {
  if (!panel) return;
  panel.classList.toggle('backend-viewer--collapsed', collapsed);
  try {
    localStorage.setItem('macroverse-backend-viewer-collapsed', collapsed ? '1' : '0');
  } catch {
    // ignore
  }
}

export function initBackendViewer(): void {
  if (panel || typeof document === 'undefined') return;

  panel = document.createElement('section');
  panel.id = 'backendViewer';
  panel.className = 'backend-viewer';
  panel.setAttribute('aria-label', 'Backend viewer');
  panel.innerHTML = `
    <button type="button" class="backend-viewer-toggle" id="backendViewerToggle" title="Backend viewer" aria-expanded="true">Backend</button>
    <div class="backend-viewer-body" id="backendViewerBody"></div>
  `;
  document.body.appendChild(panel);
  bodyEl = document.getElementById('backendViewerBody');

  const toggle = document.getElementById('backendViewerToggle') as HTMLButtonElement | null;
  toggle?.addEventListener('click', () => {
    const collapsed = !panel!.classList.contains('backend-viewer--collapsed');
    setCollapsed(collapsed);
    toggle.setAttribute('aria-expanded', String(!collapsed));
  });

  const stored = localStorage.getItem('macroverse-backend-viewer-collapsed');
  setCollapsed(stored === '1');
  setMonitorUpdateCallback(renderBackendViewer);
  window.addEventListener('macroverse:shader-changed', renderBackendViewer);
  window.addEventListener('macroverse-shader-index-updated', renderBackendViewer);
  refreshServerStatus();
  refreshTimer = setInterval(refreshServerStatus, 2500);
  window.addEventListener('beforeunload', () => {
    if (refreshTimer) clearInterval(refreshTimer);
  });
}
