/* ── Pipeline Dashboard ────────────────────────────────────────────────────
   Interactive wizard + signal-flow diagram + status dashboard.
   Clickable nodes navigate to the relevant view tab.
   ──────────────────────────────────────────────────────────────────────── */

import { entries } from '../state.js';
import { fetchIndex, fetchWireLibrary } from '../api.js';

let rendered = false;

export function initPipeline(): void {
  if (rendered) return;
  const root = document.getElementById('pipelineContainer');
  if (!root) return;
  rendered = true;
  root.innerHTML = buildDashboard();
  wireEvents(root);
  refreshStatus();
}

/* ── View switching helper ─────────────────────────────────────────── */

function switchView(viewName: string): void {
  const tab = document.querySelector(`.view-tab[data-view="${viewName}"]`) as HTMLElement | null;
  if (tab) tab.click();
}

/* ── HTML helpers ──────────────────────────────────────────────────── */

function actionBtn(label: string, id: string, cls: string, title: string): string {
  return `<button type="button" class="pipe-action-btn ${cls}" id="${id}" title="${title}">${label}</button>`;
}

function stepItem(num: string, title: string, desc: string, actions: string): string {
  return `<div class="pipe-step">
    <div class="pipe-step-num">${num}</div>
    <div class="pipe-step-body">
      <div class="pipe-step-title">${title}</div>
      <div class="pipe-step-desc">${desc}</div>
      ${actions ? `<div class="pipe-step-actions">${actions}</div>` : ''}
    </div>
  </div>`;
}

function clickNode(title: string, lines: string[], viewTarget: string, accent?: string): string {
  const border = accent || 'var(--bevel-dark)';
  const titleColor = accent || 'var(--amiga-copper)';
  const cursor = viewTarget ? 'cursor:pointer;' : '';
  const dataAttr = viewTarget ? `data-goto-view="${viewTarget}"` : '';
  const hoverClass = viewTarget ? 'pipe-node-clickable' : '';
  return `<div class="pipe-node ${hoverClass}" ${dataAttr} style="border-color:${border};${cursor}">
    <div class="pipe-node-title" style="color:${titleColor}">${title}</div>
    ${lines.map(l => `<div class="pipe-node-line">${l}</div>`).join('')}
    ${viewTarget ? `<div class="pipe-node-goto">Click to open</div>` : ''}
  </div>`;
}

function arrow(): string {
  return `<div class="pipe-arrow">&#x25BC;</div>`;
}

function arrowRight(): string {
  return `<div class="pipe-arrow-right">&#x25B6;</div>`;
}

function sectionBox(title: string, inner: string, borderColor?: string): string {
  const bc = borderColor || 'var(--bevel-dark)';
  return `<div class="pipe-section" style="border-color:${bc}">
    <div class="pipe-section-label">${title}</div>
    ${inner}
  </div>`;
}

function row(items: string): string {
  return `<div class="pipe-row">${items}</div>`;
}

/* ── Dashboard Build ───────────────────────────────────────────────── */

function buildDashboard(): string {
  let h = '';

  /* ─── Getting Started Wizard ───────────────────────────────────── */
  h += `<div class="pipe-wizard">
    <div class="pipe-wizard-header">Getting Started &mdash; Shader to Wire in 4 Steps</div>
    <div class="pipe-steps">
      ${stepItem('1', 'Pick a Shader',
        'Browse the Index panel (left) &mdash; select any ISF or GLSL shader to load it.',
        '')}
      ${stepItem('2', 'Edit &amp; Preview',
        'Use Split view to see your shader running while you edit the code.',
        actionBtn('Go to Split View', 'pipeGoSplit', 'pipe-btn-blue', 'Switch to Split view'))}
      ${stepItem('3', 'Export to Wire',
        'Copy the ISF source for Wire, or validate Wire compatibility.',
        actionBtn('Copy for Wire', 'pipeCopyWire', 'pipe-btn-default', 'Copy current shader ISF to clipboard')
        + actionBtn('Check ISF', 'pipeCheckIsf', 'pipe-btn-default', 'Validate ISF Wire compatibility'))}
      ${stepItem('4', 'Batch Generate',
        'Open Wire Hub to build .wire patches from multiple shaders and generate Resolume compositions.',
        actionBtn('Open Wire Hub', 'pipeGoWire', 'pipe-btn-green', 'Switch to Wire Hub view'))}
    </div>
    <div class="pipe-quick-actions">
      <span class="pipe-quick-label">Quick Actions</span>
      ${actionBtn('Tag &amp; Classify', 'pipeTagAll', 'pipe-btn-default', 'Auto-classify shaders as source or effect')}
      ${actionBtn('Generate All Wire', 'pipeGenAll', 'pipe-btn-green', 'Generate Wire patches for all VJ source sets')}
      ${actionBtn('Rebuild Avenue', 'pipeRebuildAvc', 'pipe-btn-default', 'Rebuild Resolume Avenue composition')}
      ${actionBtn('Open Settings', 'pipeGoSettings', 'pipe-btn-default', 'Open Wire/Resolume settings')}
    </div>
  </div>`;

  /* ─── Interactive Signal Flow Diagram ──────────────────────────── */
  h += `<div class="pipe-diagram-wrap">
    <div class="pipe-diagram-title">Signal Flow</div>`;

  /* ZONE 1: INPUTS */
  const inputNodes = [
    clickNode('ISF / GLSL Shader', [
      'Fragment shader source',
      'ISF JSON metadata',
      'Uniform declarations',
    ], 'split'),
    clickNode('MIDI', [
      'Web MIDI API (USB)',
      'CC learn &amp; templates',
      'APC40 / Roli defaults',
    ], '', 'var(--amiga-accent)'),
    clickNode('FFT Audio', [
      'Web Audio API',
      '16 bands: Sub &rarr; Air',
    ], ''),
    clickNode('Webcam / Video', [
      'getUserMedia capture',
      'Bound as sampler2D',
    ], ''),
  ];

  h += sectionBox('Inputs', row(inputNodes.join('')));
  h += arrow();

  /* ZONE 2: ENGINE */
  const engineNodes = [
    clickNode('WebGL Compiler', [
      'Vertex + Fragment',
      'ISF wrapping layer',
      'Hot-reload on edit',
    ], 'code'),
    arrowRight(),
    clickNode('Render Loop', [
      'requestAnimationFrame',
      '60fps, multi-pass',
    ], ''),
    arrowRight(),
    clickNode('Uniform Binding', [
      'TIME, RENDERSIZE',
      'MIDI CC &rarr; params',
      'FFT bands mapped',
    ], ''),
  ];

  const vjBox = `<div class="pipe-row" style="margin-top:8px">
    ${clickNode('VJ Dual-Deck', ['Deck A / B mixer', 'Crossfader blend modes', 'Per-deck parameters'], 'vj', 'var(--amiga-copper)')}
    ${arrowRight()}
    ${clickNode('Gallery', ['Multi-shader audition', 'BPM automation', 'Tag &amp; set mgmt'], 'gallery')}
  </div>`;

  h += sectionBox('Macroverse 42 Engine', row(engineNodes.join('')) + vjBox, 'var(--amiga-copper)');
  h += arrow();

  /* ZONE 3: WIRE OUTPUT */
  const wireNodes = [
    clickNode('Wire Builder', [
      'Select shaders &rarr; nodes',
      'Topology selection',
      'MIDI preset mapping',
    ], 'wire', '#44ffaa'),
    arrowRight(),
    clickNode('.wire JSON', [
      'Complete Wire patch',
      'feedback / beat / glitch',
      'Node graph + connections',
    ], 'wire', 'var(--amiga-copper)'),
    arrowRight(),
    clickNode('.avc XML', [
      'Resolume Avenue comp',
      '9 decks &times; 3 layers',
      'Auto-generated layout',
    ], 'wire', 'var(--amiga-copper)'),
  ];

  h += sectionBox('Resolume Wire Output', row(wireNodes.join('')), '#44ffaa');

  h += `</div>`; /* .pipe-diagram-wrap */

  /* ─── Status Dashboard ─────────────────────────────────────────── */
  h += `<div class="pipe-status-dashboard">
    <div class="pipe-status-title">Status</div>
    <div class="pipe-status-grid">
      <div class="pipe-stat">
        <div class="pipe-stat-value" id="pipeTotalShaders">--</div>
        <div class="pipe-stat-label">Total Shaders</div>
      </div>
      <div class="pipe-stat">
        <div class="pipe-stat-value" id="pipeIsfCount">--</div>
        <div class="pipe-stat-label">ISF Shaders</div>
      </div>
      <div class="pipe-stat">
        <div class="pipe-stat-value" id="pipeGlslCount">--</div>
        <div class="pipe-stat-label">GLSL Shaders</div>
      </div>
      <div class="pipe-stat">
        <div class="pipe-stat-value" id="pipeWireCount">--</div>
        <div class="pipe-stat-label">Wire Patches</div>
      </div>
    </div>
  </div>`;

  return `<div class="pipe-dashboard">${h}</div>`;
}

/* ── Events ────────────────────────────────────────────────────────── */

function wireEvents(root: HTMLElement): void {
  /* Clickable diagram nodes */
  root.addEventListener('click', (ev) => {
    const node = (ev.target as HTMLElement).closest('[data-goto-view]') as HTMLElement | null;
    if (node) {
      const view = node.dataset.gotoView || '';
      if (view) switchView(view);
      return;
    }
  });

  /* Wizard buttons */
  const goSplit = root.querySelector('#pipeGoSplit');
  if (goSplit) goSplit.addEventListener('click', () => switchView('split'));

  const goWire = root.querySelector('#pipeGoWire');
  if (goWire) goWire.addEventListener('click', () => switchView('wire'));

  const goSettings = root.querySelector('#pipeGoSettings');
  if (goSettings) goSettings.addEventListener('click', () => {
    /* Settings is in the right column; click the settings tab if it exists */
    const settingsTab = document.querySelector('.right-tab[data-rtab="settings"]') as HTMLElement
      || document.querySelector('#settingsTabBtn') as HTMLElement;
    if (settingsTab) settingsTab.click();
  });

  /* Copy current shader for Wire */
  const copyWire = root.querySelector('#pipeCopyWire');
  if (copyWire) copyWire.addEventListener('click', async () => {
    const codeEl = document.getElementById('codeEditorContainer');
    if (!codeEl) return;
    const cm = (codeEl as unknown as { cmView?: { state: { doc: { toString(): string } } } }).cmView;
    const text = cm ? cm.state.doc.toString() : codeEl.textContent || '';
    if (text) {
      try {
        await navigator.clipboard.writeText(text);
        statusFlash(copyWire as HTMLElement, 'Copied!');
      } catch { /* ignore */ }
    }
  });

  /* Check ISF compatibility (simple client-side check) */
  const checkIsf = root.querySelector('#pipeCheckIsf');
  if (checkIsf) checkIsf.addEventListener('click', () => {
    const codeEl = document.getElementById('codeEditorContainer');
    const cm = (codeEl as unknown as { cmView?: { state: { doc: { toString(): string } } } }).cmView;
    const text = cm ? cm.state.doc.toString() : (codeEl?.textContent || '');
    if (!text.trim()) {
      statusFlash(checkIsf as HTMLElement, 'No shader loaded');
      return;
    }
    const hasIsfHeader = /\/\*\{[\s\S]*?"INPUTS"[\s\S]*?\}\*\//.test(text);
    const hasMainImage = /void\s+main\s*\(/.test(text);
    if (hasIsfHeader && hasMainImage) {
      statusFlash(checkIsf as HTMLElement, 'ISF OK');
    } else {
      const issues: string[] = [];
      if (!hasIsfHeader) issues.push('missing ISF JSON header');
      if (!hasMainImage) issues.push('missing main()');
      statusFlash(checkIsf as HTMLElement, issues.join(', '));
    }
  });

  /* Quick actions - these call the Wire Pipeline API endpoints */
  const tagAll = root.querySelector('#pipeTagAll');
  if (tagAll) tagAll.addEventListener('click', async () => {
    const btn = tagAll as HTMLButtonElement;
    btn.disabled = true;
    btn.textContent = 'Classifying...';
    try {
      const { postWireClassifyEffects } = await import('../api.js');
      const r = await postWireClassifyEffects();
      btn.textContent = `Tagged ${r.effectsTagged} effects, ${r.sourcesTagged} sources`;
      setTimeout(() => { btn.textContent = 'Tag & Classify'; btn.disabled = false; }, 3000);
      refreshStatus();
    } catch (err) {
      btn.textContent = 'Failed: ' + (err as Error).message;
      setTimeout(() => { btn.textContent = 'Tag & Classify'; btn.disabled = false; }, 3000);
    }
  });

  const genAll = root.querySelector('#pipeGenAll');
  if (genAll) genAll.addEventListener('click', async () => {
    const btn = genAll as HTMLButtonElement;
    btn.disabled = true;
    btn.textContent = 'Generating...';
    try {
      const { postSeedWire } = await import('../api.js');
      const r = await postSeedWire({ autoSeed: true });
      btn.textContent = `Generated ${r.generated.length} patches`;
      setTimeout(() => { btn.innerHTML = 'Generate All Wire'; btn.disabled = false; }, 3000);
      refreshStatus();
    } catch (err) {
      btn.textContent = 'Failed: ' + (err as Error).message;
      setTimeout(() => { btn.innerHTML = 'Generate All Wire'; btn.disabled = false; }, 3000);
    }
  });

  const rebuildAvc = root.querySelector('#pipeRebuildAvc');
  if (rebuildAvc) rebuildAvc.addEventListener('click', async () => {
    const btn = rebuildAvc as HTMLButtonElement;
    btn.disabled = true;
    btn.textContent = 'Building...';
    try {
      const { postSeedAvenue } = await import('../api.js');
      const msg = await postSeedAvenue({});
      btn.textContent = msg.slice(0, 50);
      setTimeout(() => { btn.textContent = 'Rebuild Avenue'; btn.disabled = false; }, 3000);
    } catch (err) {
      btn.textContent = 'Failed: ' + (err as Error).message;
      setTimeout(() => { btn.textContent = 'Rebuild Avenue'; btn.disabled = false; }, 3000);
    }
  });
}

function statusFlash(btn: HTMLElement, msg: string): void {
  const orig = btn.textContent;
  btn.textContent = msg;
  setTimeout(() => { btn.textContent = orig; }, 2500);
}

/* ── Status refresh ────────────────────────────────────────────────── */

async function refreshStatus(): Promise<void> {
  try {
    const [freshEntries, lib] = await Promise.all([fetchIndex(), fetchWireLibrary()]);
    entries.length = 0;
    entries.push(...freshEntries);

    const total = freshEntries.length;
    const isf = freshEntries.filter((e) => ((e as unknown as { format?: string }).format || '').toLowerCase() === 'isf').length;
    const glsl = total - isf;

    const setVal = (id: string, v: string) => {
      const el = document.getElementById(id);
      if (el) el.textContent = v;
    };

    setVal('pipeTotalShaders', String(total));
    setVal('pipeIsfCount', String(isf));
    setVal('pipeGlslCount', String(glsl));
    setVal('pipeWireCount', String(lib.length));
  } catch { /* silent */ }
}
