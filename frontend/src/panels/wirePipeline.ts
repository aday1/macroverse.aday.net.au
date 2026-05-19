/* ── Wire Pipeline Hub ────────────────────────────────────────────────────
   3-column layout: Shader Browser | Wire Builder | Wire Library
   Quick Actions toolbar for bulk operations.
   ──────────────────────────────────────────────────────────────────────── */

import {
  entries,
  wireShaderTab, setWireShaderTab,
  wireSelectedShaderIds, setWireSelectedShaderIds, toggleWireSelectedShader,
  wireTopology, setWireTopology,
  wireMidiPreset, setWireMidiPreset,
  wireLibrarySearch, setWireLibrarySearch,
} from '../state.js';

import type { WireShaderTab as WShaderTab } from '../state.js';
import type { IndexEntry } from '../types.js';
import type { WireLibraryEntry } from '../api.js';

import {
  fetchIndex,
  fetchWireLibrary,
  fetchWirePatch,
  postWireClassifyEffects,
  postWireGenerate,
  postWireGenerateEffects,
  postWireDelete,
  postSeedWire,
  postSeedAvenue,
  postOpenInWire,
  postOpenInExplorer,
  postWireCompile,
  postOpenInResolume,
} from '../api.js';

let rendered = false;
let libraryEntries: WireLibraryEntry[] = [];
let libraryTab: 'templates' | 'vj' | 'all' = 'templates';
let refreshBrowser: (() => void) | null = null;
let refreshBuilder: (() => void) | null = null;
let refreshLibrary: (() => void) | null = null;

/* ── Topology + MIDI descriptions ─────────────────────────────────── */

const TOPO_DESC: Record<string, string> = {
  feedback: 'Loops output back as input for evolving, self-modifying visuals.',
  beat: 'Rhythm-driven transitions between sources, synced to BPM.',
  glitch: 'Databend-style distortion with random parameter jumps.',
  geometric: 'Kaleidoscopic transforms and symmetry effects.',
  colour: 'Colour grading and palette manipulation chains.',
  mixer: 'A/B source mixing with crossfader and blend modes.',
};

const MIDI_DESC: Record<string, string> = {
  apc40: 'APC40 MK II: 8 faders + 8 knobs + transport + crossfader.',
  'roli-blocks': 'Roli Blocks: MPE pressure, slide, glide for expressive control.',
  generic: 'Generic: CC1-CC8 mapped to first 8 parameters.',
  none: 'No MIDI mapping. Parameters controlled manually.',
};

/* ── Public init ─────────────────────────────────────────────────────── */

export function initWirePipeline(): void {
  if (rendered) return;
  const root = document.getElementById('wirePipelineContainer');
  if (!root) return;
  rendered = true;
  root.innerHTML = '';
  root.style.display = 'flex';
  root.style.flexDirection = 'column';
  root.style.overflow = 'hidden';

  /* Quick Actions toolbar */
  const toolbar = el('div', 'wire-hub-toolbar');
  toolbar.innerHTML = `
    <div class="wire-hub-toolbar-inner">
      <span class="wire-hub-title">Wire Pipeline Hub</span>
      <div class="wire-toolbar-section">
        <span class="wire-toolbar-section-label">Classify</span>
        <button type="button" class="wire-btn wire-action-btn btn-wire-highlight" id="wireTagEffects" title="Scan shaders for sampler2D inputs and tag as source or texture-effect">Tag Effects</button>
      </div>
      <div class="wire-toolbar-section">
        <span class="wire-toolbar-section-label">Generate</span>
        <button type="button" class="wire-btn wire-action-btn" id="wireGenSources" title="Generate Wire patches for all VJ source sets">Gen Sources</button>
        <button type="button" class="wire-btn wire-action-btn" id="wireGenEffects" title="Generate Wire effect patches from texture-effect tagged shaders">Gen Effects</button>
        <button type="button" class="wire-btn wire-action-btn" id="wireRebuildAvenue" title="Rebuild Resolume Avenue composition from current Wire patches">Rebuild Avenue</button>
        <button type="button" class="wire-btn wire-action-btn" id="wireCompileAll" title="Bulk compile all .wire patches with Macroverse metadata (Author, Vendor, URL)" style="color:#ff44ff">Compile All</button>
      </div>
      <div class="wire-toolbar-section wire-seed-options">
        <span class="wire-toolbar-section-label">Seed Options</span>
        <label class="wire-seed-label">Set
          <select id="wireSeedSet" class="wire-seed-select">
            <option value="">All</option>
            <option value="vj-ambient">vj-ambient</option>
            <option value="vj-techno">vj-techno</option>
            <option value="vj-cosmic">vj-cosmic</option>
            <option value="vj-glitch">vj-glitch</option>
            <option value="vj-geometric">vj-geometric</option>
            <option value="vj-organic">vj-organic</option>
            <option value="vj-wire-ready">vj-wire-ready</option>
            <option value="vj-dark">vj-dark</option>
            <option value="vj-colour">vj-colour</option>
          </select>
        </label>
        <label class="wire-seed-label">Mode
          <select id="wireSeedMode" class="wire-seed-select">
            <option value="standard">Standard</option>
            <option value="enhanced">Enhanced</option>
            <option value="all">All</option>
          </select>
        </label>
        <label class="wire-seed-label">FX
          <select id="wireSeedFxLevel" class="wire-seed-select">
            <option value="advanced">Advanced</option>
            <option value="basic">Basic</option>
          </select>
        </label>
        <label class="wire-seed-check"><input type="checkbox" id="wireSeedFFT" checked autocomplete="off"> FFT</label>
        <label class="wire-seed-check"><input type="checkbox" id="wireSeedWebcam" checked autocomplete="off"> Webcam</label>
        <label class="wire-seed-check"><input type="checkbox" id="wireSeedGlitch" checked autocomplete="off"> Glitch</label>
        <label class="wire-seed-check"><input type="checkbox" id="wireSeedMidi" checked autocomplete="off"> MIDI</label>
        <label class="wire-seed-check"><input type="checkbox" id="wireSeedDryRun" autocomplete="off"> Dry Run</label>
      </div>
      <div class="wire-toolbar-section">
        <span class="wire-toolbar-section-label">Compile</span>
        <button type="button" class="wire-btn wire-action-btn btn-wire-highlight" id="wireBulkCompile" title="Bulk compile all .wire patches via Resolume Wire CLI (sets author, vendor, email metadata)">Bulk Compile</button>
      </div>
      <div class="wire-toolbar-section">
        <span class="wire-toolbar-section-label">Export</span>
        <button type="button" class="wire-btn wire-action-btn" id="wireExportLib" title="Export Wire library metadata as JSON">Export Library</button>
      </div>
      <span class="wire-hub-status" id="wireHubStatus"></span>
    </div>
  `;
  root.appendChild(toolbar);

  /* 3-column container */
  const columns = el('div', 'wire-hub-columns');
  root.appendChild(columns);

  /* Left: Shader Browser */
  const browserCol = el('div', 'wire-hub-col wire-browser-col');
  browserCol.innerHTML = buildShaderBrowser();
  columns.appendChild(browserCol);

  /* Center: Wire Builder */
  const builderCol = el('div', 'wire-hub-col wire-builder-col');
  builderCol.innerHTML = buildWireBuilder();
  columns.appendChild(builderCol);

  /* Right: Wire Library */
  const libraryCol = el('div', 'wire-hub-col wire-library-col');
  libraryCol.innerHTML = buildWireLibrary();
  columns.appendChild(libraryCol);

  /* Wire up events */
  wireToolbarEvents(toolbar);
  wireBrowserEvents(browserCol);
  wireBuilderEvents(builderCol);
  wireLibraryEvents(libraryCol);

  /* Initial data load */
  loadData();
}

/* ── Helpers ──────────────────────────────────────────────────────────── */

function el(tag: string, className?: string): HTMLElement {
  const e = document.createElement(tag);
  if (className) e.className = className;
  return e;
}

function status(msg: string, isError = false): void {
  const s = document.getElementById('wireHubStatus');
  if (s) {
    s.textContent = msg;
    s.style.color = isError ? '#ff6666' : 'var(--crt-dim)';
  }
}

/* ── Data loading ────────────────────────────────────────────────────── */

async function loadData(): Promise<void> {
  status('Loading...');
  try {
    const [freshEntries, lib] = await Promise.all([fetchIndex(), fetchWireLibrary()]);
    entries.length = 0;
    entries.push(...freshEntries);
    libraryEntries = lib;
    if (refreshBrowser) refreshBrowser();
    if (refreshLibrary) refreshLibrary();
    if (refreshBuilder) refreshBuilder();
    status(entries.length + ' shaders, ' + libraryEntries.length + ' wire patches');
  } catch (err) {
    status('Load failed: ' + (err as Error).message, true);
  }
}

/* ── Shader classification helpers ───────────────────────────────────── */

function isEffect(e: IndexEntry): boolean {
  const tags: string[] = (e as unknown as { tags?: string[] }).tags || [];
  if (tags.includes('texture-effect')) return true;
  const uniforms: string = (e as unknown as { uniforms?: string }).uniforms || '';
  if (/sampler2D|inputImage|texture\d/i.test(uniforms)) return true;
  return false;
}

function isSource(e: IndexEntry): boolean {
  return !isEffect(e);
}

function filteredBrowserEntries(): IndexEntry[] {
  const search = (document.getElementById('wireBrowserSearch') as HTMLInputElement)?.value?.trim().toLowerCase() || '';
  const tab = wireShaderTab;
  return entries.filter((e) => {
    if (tab === 'effects' && !isEffect(e)) return false;
    if (tab === 'sources' && !isSource(e)) return false;
    if (search) {
      const name = ((e as unknown as { name?: string }).name || '').toLowerCase();
      const cat = ((e as unknown as { category?: string }).category || '').toLowerCase();
      const tags = ((e as unknown as { tags?: string[] }).tags || []).join(' ').toLowerCase();
      if (!name.includes(search) && !cat.includes(search) && !tags.includes(search)) return false;
    }
    return true;
  });
}

/* ── Shader Browser (left column) ────────────────────────────────────── */

function buildShaderBrowser(): string {
  return `
    <div class="wire-col-header">Shader Browser</div>
    <div class="wire-browser-tabs">
      <button type="button" class="wire-btn wire-browser-tab ${wireShaderTab === 'sources' ? 'active' : ''}" data-wbtab="sources">Sources</button>
      <button type="button" class="wire-btn wire-browser-tab ${wireShaderTab === 'effects' ? 'active' : ''}" data-wbtab="effects">Effects</button>
    </div>
    <div class="wire-browser-search-row">
      <input type="text" id="wireBrowserSearch" class="search" placeholder="filter name, tags..." style="font-size:11px;padding:4px 8px">
    </div>
    <div class="wire-browser-count" id="wireBrowserCount">0 shaders</div>
    <div class="wire-browser-list" id="wireBrowserList"></div>
    <div class="wire-browser-footer">
      <button type="button" class="wire-btn" id="wireBrowserSelectAll" style="font-size:10px">Select All</button>
      <button type="button" class="wire-btn" id="wireBrowserClearSel" style="font-size:10px">Clear</button>
    </div>
  `;
}

function renderBrowserList(): void {
  const list = document.getElementById('wireBrowserList');
  const countEl = document.getElementById('wireBrowserCount');
  if (!list) return;

  const items = filteredBrowserEntries();
  if (countEl) {
    const sourceCount = entries.filter(isSource).length;
    const effectCount = entries.filter(isEffect).length;
    countEl.textContent = wireShaderTab === 'sources'
      ? items.length + ' of ' + sourceCount + ' sources'
      : items.length + ' of ' + effectCount + ' effects';
  }

  const visible = items.slice(0, 200);
  list.innerHTML = visible.map((e) => {
    const id = (e as unknown as { id?: number }).id ?? 0;
    const name = (e as unknown as { name?: string }).name || 'unnamed';
    const cat = (e as unknown as { category?: string }).category || '';
    const checked = wireSelectedShaderIds.includes(id) ? 'checked' : '';
    const eff = isEffect(e);
    return `<label class="wire-browser-row" data-sid="${id}">
      <input type="checkbox" ${checked} data-shader-id="${id}" autocomplete="off">
      <span class="wire-browser-name">${esc(name)}</span>
      <span class="wire-browser-badge ${eff ? 'badge-effect' : 'badge-source'}">${esc(cat || (eff ? 'effect' : 'source'))}</span>
    </label>`;
  }).join('');

  if (items.length > 200) {
    list.innerHTML += `<div style="padding:6px 8px;font-size:10px;color:var(--crt-dim)">...and ${items.length - 200} more</div>`;
  }
}

function wireBrowserEvents(col: HTMLElement): void {
  col.querySelectorAll('.wire-browser-tab').forEach((btn) => {
    btn.addEventListener('click', () => {
      const tab = (btn as HTMLElement).dataset.wbtab as WShaderTab;
      setWireShaderTab(tab);
      col.querySelectorAll('.wire-browser-tab').forEach((b) => b.classList.remove('active'));
      btn.classList.add('active');
      renderBrowserList();
    });
  });

  const searchInput = col.querySelector('#wireBrowserSearch');
  if (searchInput) {
    searchInput.addEventListener('input', () => renderBrowserList());
  }

  const list = col.querySelector('#wireBrowserList');
  if (list) {
    list.addEventListener('change', (ev) => {
      const target = ev.target as HTMLInputElement;
      if (target.dataset.shaderId) {
        toggleWireSelectedShader(Number(target.dataset.shaderId));
        if (refreshBuilder) refreshBuilder();
      }
    });
  }

  const selAllBtn = col.querySelector('#wireBrowserSelectAll');
  if (selAllBtn) {
    selAllBtn.addEventListener('click', () => {
      const items = filteredBrowserEntries();
      const ids = items.map((e) => (e as unknown as { id?: number }).id ?? 0).filter(Boolean);
      setWireSelectedShaderIds([...new Set([...wireSelectedShaderIds, ...ids])]);
      renderBrowserList();
      if (refreshBuilder) refreshBuilder();
    });
  }

  const clearBtn = col.querySelector('#wireBrowserClearSel');
  if (clearBtn) {
    clearBtn.addEventListener('click', () => {
      setWireSelectedShaderIds([]);
      renderBrowserList();
      if (refreshBuilder) refreshBuilder();
    });
  }

  refreshBrowser = renderBrowserList;
}

/* ── Wire Builder (center column) ────────────────────────────────────── */

function buildWireBuilder(): string {
  const topoDesc = TOPO_DESC[wireTopology] || '';
  const midiDesc = MIDI_DESC[wireMidiPreset] || '';

  return `
    <div class="wire-col-header">Wire Builder Studio</div>
    <div class="wire-builder-nodes" id="wireBuilderNodes"></div>
    <div class="wire-builder-controls">
      <div class="wire-builder-control-row">
        <label>
          <span class="wire-ctrl-label">Topology</span>
          <select id="wireTopologySelect" class="list-filter-format" style="width:auto;min-width:100px">
            <option value="feedback" ${wireTopology === 'feedback' ? 'selected' : ''}>Feedback</option>
            <option value="beat" ${wireTopology === 'beat' ? 'selected' : ''}>Beat</option>
            <option value="glitch" ${wireTopology === 'glitch' ? 'selected' : ''}>Glitch</option>
            <option value="geometric" ${wireTopology === 'geometric' ? 'selected' : ''}>Geometric</option>
            <option value="colour" ${wireTopology === 'colour' ? 'selected' : ''}>Colour</option>
            <option value="mixer" ${wireTopology === 'mixer' ? 'selected' : ''}>Mixer</option>
          </select>
        </label>
        <label>
          <span class="wire-ctrl-label">MIDI</span>
          <select id="wireMidiSelect" class="list-filter-format" style="width:auto;min-width:100px">
            <option value="apc40" ${wireMidiPreset === 'apc40' ? 'selected' : ''}>APC40</option>
            <option value="roli-blocks" ${wireMidiPreset === 'roli-blocks' ? 'selected' : ''}>Roli Blocks</option>
            <option value="generic" ${wireMidiPreset === 'generic' ? 'selected' : ''}>Generic</option>
            <option value="none" ${wireMidiPreset === 'none' ? 'selected' : ''}>None</option>
          </select>
        </label>
      </div>
      <div class="wire-topo-desc" id="wireTopoDesc">${esc(topoDesc)}</div>
      <div class="wire-midi-desc" id="wireMidiDesc">${esc(midiDesc)}</div>
      <div class="wire-builder-control-row">
        <label class="wire-feature-check"><input type="checkbox" id="wireFeatureFFT" checked autocomplete="off"> FFT</label>
        <label class="wire-feature-check"><input type="checkbox" id="wireFeatureWebcam" autocomplete="off"> Webcam</label>
        <label class="wire-feature-check"><input type="checkbox" id="wireFeatureMidi" checked autocomplete="off"> MIDI</label>
        <span class="wire-selected-count" id="wireSelectedCount">0 selected</span>
      </div>
    </div>
    <div class="wire-builder-actions">
      <button type="button" class="wire-btn btn-wire-primary" id="wireGenerateBtn" title="Generate .wire patch from selected shaders">Generate .wire</button>
      <button type="button" class="wire-btn" id="wireGenAvcBtn" title="Rebuild Resolume Avenue composition">Gen .avc</button>
      <button type="button" class="wire-btn btn-wire-open" id="wireOpenLastBtn" title="Open last generated patch in Wire" style="display:none">Open in Wire</button>
      <button type="button" class="wire-btn" id="wireClearSelBtn" title="Clear selection">Clear</button>
    </div>
    <div id="wireSuccessArea"></div>
  `;
}

function buildWelcomeGuide(): string {
  return `<div class="wire-welcome">
    <div class="wire-welcome-title">How to Use Wire Hub</div>
    <div class="wire-welcome-step">
      <div class="wire-welcome-num">1</div>
      <div class="wire-welcome-text"><strong>Tag your shaders</strong> &mdash; Click <strong>Tag Effects</strong> in the toolbar above to auto-classify shaders as Sources or Effects based on sampler2D usage.</div>
    </div>
    <div class="wire-welcome-step">
      <div class="wire-welcome-num">2</div>
      <div class="wire-welcome-text"><strong>Select shaders</strong> (left panel) &mdash; Check the shaders you want in your Wire patch. Use the Sources tab for generative shaders, Effects tab for post-processing.</div>
    </div>
    <div class="wire-welcome-step">
      <div class="wire-welcome-num">3</div>
      <div class="wire-welcome-text"><strong>Configure</strong> (below) &mdash; Pick a topology (Feedback, Beat, etc.) and MIDI preset for parameter mapping.</div>
    </div>
    <div class="wire-welcome-step">
      <div class="wire-welcome-num">4</div>
      <div class="wire-welcome-text"><strong>Generate</strong> &mdash; Click the green <strong>Generate .wire</strong> button below, then <strong>Open in Wire</strong> to launch it in Resolume.</div>
    </div>
    <div class="wire-welcome-divider"></div>
    <div style="font-size:11px;color:var(--amiga-text-dim);margin-bottom:8px">Or use Quick Generate from the toolbar:</div>
    <div class="wire-welcome-quick">
      <span style="font-size:10px;color:var(--amiga-text-dim)"><strong>Gen Sources</strong> = all VJ sets</span>
      <span style="font-size:10px;color:var(--amiga-text-dim)"><strong>Gen Effects</strong> = all effect patches</span>
      <span style="font-size:10px;color:var(--amiga-text-dim)"><strong>Rebuild Avenue</strong> = .avc composition</span>
    </div>
  </div>`;
}

function renderBuilderNodes(): void {
  const container = document.getElementById('wireBuilderNodes');
  const countEl = document.getElementById('wireSelectedCount');
  if (!container) return;

  const selected = wireSelectedShaderIds
    .map((id) => entries.find((e) => (e as unknown as { id?: number }).id === id))
    .filter(Boolean) as IndexEntry[];

  if (countEl) countEl.textContent = selected.length + ' selected';

  if (selected.length === 0) {
    container.innerHTML = buildWelcomeGuide();
    return;
  }

  container.innerHTML = selected.map((e) => {
    const id = (e as unknown as { id?: number }).id ?? 0;
    const name = (e as unknown as { name?: string }).name || 'unnamed';
    const cat = (e as unknown as { category?: string }).category || '';
    const format = (e as unknown as { format?: string }).format || '';
    const eff = isEffect(e);
    const type = eff ? 'effect' : 'source';
    return `<div class="wire-node-card ${type}" data-nid="${id}">
      <div class="wire-node-title">${esc(name)}</div>
      <div class="wire-node-meta">${esc(cat)} &middot; ${esc(format || 'glsl')} &middot; ${type}</div>
      <button type="button" class="wire-node-remove" data-remove-id="${id}" title="Remove from selection">&times;</button>
    </div>`;
  }).join('');
}

let lastGeneratedFile = '';

function wireBuilderEvents(col: HTMLElement): void {
  // Topology & MIDI with live descriptions
  const topoSel = col.querySelector('#wireTopologySelect') as HTMLSelectElement | null;
  const topoDescEl = col.querySelector('#wireTopoDesc') as HTMLElement | null;
  if (topoSel) topoSel.addEventListener('change', () => {
    setWireTopology(topoSel.value);
    if (topoDescEl) topoDescEl.textContent = TOPO_DESC[topoSel.value] || '';
  });

  const midiSel = col.querySelector('#wireMidiSelect') as HTMLSelectElement | null;
  const midiDescEl = col.querySelector('#wireMidiDesc') as HTMLElement | null;
  if (midiSel) midiSel.addEventListener('change', () => {
    setWireMidiPreset(midiSel.value);
    if (midiDescEl) midiDescEl.textContent = MIDI_DESC[midiSel.value] || '';
  });

  // Node remove delegation
  const nodes = col.querySelector('#wireBuilderNodes');
  if (nodes) {
    nodes.addEventListener('click', (ev) => {
      const btn = (ev.target as HTMLElement).closest('[data-remove-id]') as HTMLElement | null;
      if (btn) {
        toggleWireSelectedShader(Number(btn.dataset.removeId));
        renderBuilderNodes();
        if (refreshBrowser) refreshBrowser();
      }
    });
  }

  // Generate .wire
  const genBtn = col.querySelector('#wireGenerateBtn') as HTMLButtonElement | null;
  if (genBtn) {
    genBtn.addEventListener('click', async () => {
      if (wireSelectedShaderIds.length === 0) { status('Select shaders first', true); return; }
      genBtn.disabled = true;
      genBtn.textContent = 'Generating...';
      status('Generating Wire patch...');
      try {
        const fft = (col.querySelector('#wireFeatureFFT') as HTMLInputElement)?.checked ?? true;
        const webcam = (col.querySelector('#wireFeatureWebcam') as HTMLInputElement)?.checked ?? false;
        const midi = (col.querySelector('#wireFeatureMidi') as HTMLInputElement)?.checked ?? true;
        const result = await postWireGenerate({
          shaderIds: wireSelectedShaderIds,
          topology: wireTopology,
          midiPreset: wireMidiPreset,
          features: { fft, webcam, midi },
        });
        lastGeneratedFile = result.file || '';
        const openBtn = col.querySelector('#wireOpenLastBtn') as HTMLElement;
        if (openBtn && lastGeneratedFile) openBtn.style.display = '';
        status('Generated: ' + result.file + ' (' + result.shaderCount + ' shaders)');

        // Show success card
        const successArea = col.querySelector('#wireSuccessArea');
        if (successArea) {
          successArea.innerHTML = `<div class="wire-success-card">
            <div class="wire-success-title">Wire patch generated</div>
            <div class="wire-success-meta">${esc(result.file || '')} &mdash; ${result.shaderCount || 0} shader nodes</div>
            <div class="wire-success-path" style="font-size:10px;color:var(--crt-dim);word-break:break-all;margin:4px 0">${esc(result.file || '')}</div>
            <div class="wire-success-actions">
              <button type="button" class="wire-btn btn-wire-open" id="wireSuccessOpen" title="Open in Resolume Wire">Open in Wire</button>
              <button type="button" class="wire-btn" id="wireSuccessExplore" title="Show in Explorer">Show in Explorer</button>
              <button type="button" class="wire-btn" id="wireSuccessCopy" title="Copy Wire JSON">Copy JSON</button>
            </div>
          </div>`;
          const soBtn = successArea.querySelector('#wireSuccessOpen');
          if (soBtn) soBtn.addEventListener('click', async () => {
            try { await postOpenInWire({ path: lastGeneratedFile }); } catch (err) { status('Open failed: ' + (err as Error).message, true); }
          });
          const seBtn = successArea.querySelector('#wireSuccessExplore');
          if (seBtn) seBtn.addEventListener('click', async () => {
            try { await postOpenInExplorer({ path: lastGeneratedFile }); } catch (err) { status('Explorer: ' + (err as Error).message, true); }
          });
          const scBtn = successArea.querySelector('#wireSuccessCopy');
          if (scBtn) scBtn.addEventListener('click', async () => {
            try {
              const json = await fetchWirePatch(result.file || '');
              await navigator.clipboard.writeText(json);
              status('Copied to clipboard');
            } catch (err) {
              status('Copy failed: ' + (err as Error).message, true);
            }
          });
        }

        // Refresh library
        libraryEntries = await fetchWireLibrary();
        if (refreshLibrary) refreshLibrary();
      } catch (err) {
        status('Generate failed: ' + (err as Error).message, true);
      } finally {
        genBtn.disabled = false;
        genBtn.textContent = 'Generate .wire';
      }
    });
  }

  // Generate .avc
  const avcBtn = col.querySelector('#wireGenAvcBtn') as HTMLButtonElement | null;
  if (avcBtn) {
    avcBtn.addEventListener('click', async () => {
      avcBtn.disabled = true;
      status('Building Avenue composition...');
      try {
        const msg = await postSeedAvenue({});
        status('Avenue: ' + msg.slice(0, 100));
        // Show success card with AVC path
        const successArea = col.querySelector('#wireSuccessArea');
        if (successArea) {
          // Extract path from the output message (printed by script)
          const pathMatch = msg.match(/Full path:\s*(.+)/);
          const avcPath = pathMatch ? pathMatch[1].trim() : '';
          successArea.innerHTML = `<div class="wire-success-card">
            <div class="wire-success-title">Avenue composition generated</div>
            <div class="wire-success-meta">${esc(msg.slice(0, 200))}</div>
            ${avcPath ? `<div class="wire-success-path" style="font-size:10px;color:var(--crt-dim);word-break:break-all;margin:4px 0">${esc(avcPath)}</div>` : ''}
            <div class="wire-success-actions">
              ${avcPath ? `<button type="button" class="wire-btn btn-wire-open" id="wireAvcOpen" title="Open AVC in Resolume">Open in Resolume</button>` : ''}
              ${avcPath ? `<button type="button" class="wire-btn" id="wireAvcExplore" title="Show in Explorer">Show in Explorer</button>` : ''}
            </div>
          </div>`;
          if (avcPath) {
            const openBtn = successArea.querySelector('#wireAvcOpen');
            if (openBtn) openBtn.addEventListener('click', async () => {
              try { await postOpenInResolume({ path: avcPath }); status('Opened in Resolume'); } catch (err) { status('Open failed: ' + (err as Error).message, true); }
            });
            const exploreBtn = successArea.querySelector('#wireAvcExplore');
            if (exploreBtn) exploreBtn.addEventListener('click', async () => {
              try { await postOpenInExplorer({ path: avcPath }); } catch (err) { status('Explorer: ' + (err as Error).message, true); }
            });
          }
        }
      } catch (err) {
        status('Avenue failed: ' + (err as Error).message, true);
      } finally {
        avcBtn.disabled = false;
      }
    });
  }

  // Open last in Wire
  const openBtn = col.querySelector('#wireOpenLastBtn') as HTMLButtonElement | null;
  if (openBtn) {
    openBtn.addEventListener('click', async () => {
      if (lastGeneratedFile) {
        try {
          await postOpenInWire({ path: lastGeneratedFile });
        } catch (err) {
          status('Open failed: ' + (err as Error).message, true);
        }
      }
    });
  }

  // Clear selection
  const clearBtn = col.querySelector('#wireClearSelBtn') as HTMLButtonElement | null;
  if (clearBtn) {
    clearBtn.addEventListener('click', () => {
      setWireSelectedShaderIds([]);
      renderBuilderNodes();
      if (refreshBrowser) refreshBrowser();
      // Clear success card
      const successArea = col.querySelector('#wireSuccessArea');
      if (successArea) successArea.innerHTML = '';
    });
  }

  refreshBuilder = renderBuilderNodes;
  // Initial render
  renderBuilderNodes();
}

/* ── Wire Library (right column) ─────────────────────────────────────── */

const TMPL_TYPE_LABELS: Record<string, string> = {
  gen: 'Generator', beat: 'Beat Sync', feedback: 'Feedback', glitch: 'Glitch',
  colordrift: 'Color Drift', mix: 'Mixer', fx: 'Effect', midi: 'MIDI', fft: 'FFT',
};

function extractTemplateType(name: string): string {
  // tmpl-gen-xxx, tmpl-beat-xxx, etc.
  const parts = name.replace('tmpl-', '').split('-');
  const key = parts[0];
  return TMPL_TYPE_LABELS[key] || key;
}

function buildWireLibrary(): string {
  return `
    <div class="wire-col-header">Wire Library</div>
    <div class="wire-library-tabs">
      <button type="button" class="wire-btn wire-lib-tab active" data-libtab="templates">Templates</button>
      <button type="button" class="wire-btn wire-lib-tab" data-libtab="vj">VJ Sets</button>
      <button type="button" class="wire-btn wire-lib-tab" data-libtab="all">All</button>
    </div>
    <div class="wire-library-filters">
      <input type="text" id="wireLibSearch" class="search" placeholder="filter patches..." style="font-size:11px;padding:4px 8px">
      <select id="wireLibSetFilter" class="list-filter-format" style="font-size:11px;width:auto;min-width:80px">
        <option value="">All sets</option>
      </select>
    </div>
    <div class="wire-library-count" id="wireLibCount">0 patches</div>
    <div class="wire-library-list" id="wireLibList"></div>
  `;
}

function filteredLibraryEntries(): WireLibraryEntry[] {
  const search = wireLibrarySearch;
  const setFilter = (document.getElementById('wireLibSetFilter') as HTMLSelectElement)?.value || '';
  return libraryEntries.filter((e) => {
    // Tab filter
    if (libraryTab === 'templates' && !e.name.startsWith('tmpl-')) return false;
    if (libraryTab === 'vj' && e.name.startsWith('tmpl-')) return false;
    if (setFilter && e.setName !== setFilter) return false;
    if (search) {
      const haystack = (e.name + ' ' + e.displayName + ' ' + e.setName + ' ' + e.category).toLowerCase();
      if (!haystack.includes(search)) return false;
    }
    return true;
  });
}

function renderLibrary(): void {
  const list = document.getElementById('wireLibList');
  const countEl = document.getElementById('wireLibCount');
  const setFilter = document.getElementById('wireLibSetFilter') as HTMLSelectElement | null;
  if (!list) return;

  if (setFilter && setFilter.options.length <= 1) {
    const sets = [...new Set(libraryEntries.map((e) => e.setName).filter(Boolean))].sort();
    sets.forEach((s) => {
      const opt = document.createElement('option');
      opt.value = s;
      opt.textContent = s;
      setFilter.appendChild(opt);
    });
  }

  const items = filteredLibraryEntries();
  if (countEl) countEl.textContent = items.length + ' of ' + libraryEntries.length + ' patches';

  const visible = items.slice(0, 200);
  list.innerHTML = visible.map((e) => {
    const sizeKB = Math.round(e.fileSizeBytes / 1024);
    const isTemplate = e.name.startsWith('tmpl-');
    const typeTag = isTemplate ? extractTemplateType(e.name) : (e.setName || 'custom');
    return `<div class="wire-library-row ${isTemplate ? 'wire-lib-template' : ''}" data-wl-name="${esc(e.name)}" data-wl-path="${esc(e.path)}">
      <div class="wire-lib-name">${esc(e.displayName || e.name)}</div>
      <div class="wire-lib-meta"><span class="wire-lib-tag">${esc(typeTag)}</span> &middot; ${e.shaderCount} nodes &middot; ${sizeKB}KB</div>
      <div class="wire-lib-actions">
        <button type="button" class="wire-btn wire-lib-copy btn-wire-primary" title="Copy Wire JSON to clipboard" data-copy-name="${esc(e.name)}" style="font-size:10px;padding:2px 8px">Copy</button>
        <button type="button" class="wire-btn wire-lib-open" title="Open in Resolume Wire" data-open-path="${esc(e.path)}">Open</button>
        <button type="button" class="wire-btn wire-lib-explore" title="Show in Explorer" data-explore-path="${esc(e.path)}">Explore</button>
        <button type="button" class="wire-btn wire-lib-del" title="Delete this Wire patch" data-del-path="${esc(e.path)}" data-del-name="${esc(e.name)}">Del</button>
      </div>
    </div>`;
  }).join('');

  if (items.length > 200) {
    list.innerHTML += `<div style="padding:6px 8px;font-size:10px;color:var(--crt-dim)">...and ${items.length - 200} more (use search to filter)</div>`;
  }
}

function wireLibraryEvents(col: HTMLElement): void {
  // Library tab switching (Templates / VJ Sets / All)
  const tabBtns = col.querySelectorAll('.wire-lib-tab');
  tabBtns.forEach((btn) => {
    btn.addEventListener('click', () => {
      tabBtns.forEach((b) => b.classList.remove('active'));
      btn.classList.add('active');
      libraryTab = (btn as HTMLElement).dataset.libtab as typeof libraryTab || 'all';
      renderLibrary();
    });
  });

  const searchInput = col.querySelector('#wireLibSearch') as HTMLInputElement | null;
  if (searchInput) {
    searchInput.addEventListener('input', () => {
      setWireLibrarySearch(searchInput.value);
      renderLibrary();
    });
  }

  const setFilter = col.querySelector('#wireLibSetFilter') as HTMLSelectElement | null;
  if (setFilter) {
    setFilter.addEventListener('change', () => renderLibrary());
  }

  const list = col.querySelector('#wireLibList');
  if (list) {
    list.addEventListener('click', async (ev) => {
      const target = ev.target as HTMLElement;

      if (target.classList.contains('wire-lib-copy')) {
        const name = target.dataset.copyName || '';
        try {
          status('Copying ' + name + '...');
          const json = await fetchWirePatch(name);
          await navigator.clipboard.writeText(json);
          status('Copied ' + name + ' to clipboard');
        } catch (err) {
          status('Copy failed: ' + (err as Error).message, true);
        }
        return;
      }

      if (target.classList.contains('wire-lib-open')) {
        const path = target.dataset.openPath || '';
        try {
          await postOpenInWire({ path });
          status('Opened in Wire');
        } catch (err) {
          status('Open failed: ' + (err as Error).message, true);
        }
        return;
      }

      if (target.classList.contains('wire-lib-explore')) {
        const path = target.dataset.explorePath || '';
        try {
          await postOpenInExplorer({ path });
          status('Opened in Explorer');
        } catch (err) {
          status('Explorer: ' + (err as Error).message, true);
        }
        return;
      }

      if (target.classList.contains('wire-lib-del')) {
        const path = target.dataset.delPath || '';
        const name = target.dataset.delName || '';
        if (!confirm('Delete ' + name + '? This cannot be undone.')) return;
        try {
          await postWireDelete({ path });
          libraryEntries = libraryEntries.filter((e) => e.path !== path);
          renderLibrary();
          status('Deleted ' + name);
        } catch (err) {
          status('Delete failed: ' + (err as Error).message, true);
        }
        return;
      }
    });

    // Right-click context menu on library rows
    list.addEventListener('contextmenu', (ev) => {
      const target = (ev as MouseEvent).target as HTMLElement;
      const row = target.closest('.wire-library-row') as HTMLElement | null;
      if (!row) return;
      ev.preventDefault();
      const filePath = row.dataset.wlPath || '';
      const fileName = row.dataset.wlName || '';
      if (!filePath) return;

      // Remove any existing context menu
      document.querySelectorAll('.wire-ctx-menu').forEach((m) => m.remove());

      const menu = document.createElement('div');
      menu.className = 'wire-ctx-menu';
      menu.style.cssText = `position:fixed;left:${(ev as MouseEvent).clientX}px;top:${(ev as MouseEvent).clientY}px;z-index:9999;background:var(--crt-bg,#1a1a2e);border:1px solid var(--crt-accent,#ff6a00);border-radius:4px;padding:4px 0;min-width:180px;font-size:12px;box-shadow:0 4px 12px rgba(0,0,0,0.5)`;

      const items = [
        { label: 'Open in Wire', action: () => postOpenInWire({ path: filePath }).then(() => status('Opened in Wire')).catch((e: Error) => status('Open: ' + e.message, true)) },
        { label: 'Show in Explorer', action: () => postOpenInExplorer({ path: filePath }).then(() => status('Opened in Explorer')).catch((e: Error) => status('Explorer: ' + e.message, true)) },
        { label: 'Copy Wire JSON', action: async () => { try { const json = await fetchWirePatch(fileName); await navigator.clipboard.writeText(json); status('Copied to clipboard'); } catch (e) { status('Copy: ' + (e as Error).message, true); } } },
        { label: 'Copy path to clipboard', action: () => { navigator.clipboard.writeText(filePath); status('Path copied'); } },
        { label: 'Delete', action: async () => { if (!confirm('Delete ' + fileName + '?')) return; try { await postWireDelete({ path: filePath }); libraryEntries = libraryEntries.filter((e) => e.path !== filePath); renderLibrary(); status('Deleted ' + fileName); } catch (e) { status('Delete: ' + (e as Error).message, true); } } },
      ];

      for (const item of items) {
        const el = document.createElement('div');
        el.textContent = item.label;
        el.style.cssText = 'padding:6px 14px;cursor:pointer;color:var(--crt-green,#44ff44)';
        el.addEventListener('mouseenter', () => { el.style.background = 'var(--crt-accent,#ff6a00)'; el.style.color = '#000'; });
        el.addEventListener('mouseleave', () => { el.style.background = ''; el.style.color = 'var(--crt-green,#44ff44)'; });
        el.addEventListener('click', () => { item.action(); menu.remove(); });
        menu.appendChild(el);
      }

      document.body.appendChild(menu);
      const dismiss = (e: Event) => { if (!(e.target as HTMLElement).closest('.wire-ctx-menu')) { menu.remove(); document.removeEventListener('click', dismiss); } };
      setTimeout(() => document.addEventListener('click', dismiss), 0);
    });
  }

  refreshLibrary = renderLibrary;
}

/* ── Quick Actions toolbar ───────────────────────────────────────────── */

function wireToolbarEvents(toolbar: HTMLElement): void {
  const tagBtn = toolbar.querySelector('#wireTagEffects') as HTMLButtonElement | null;
  if (tagBtn) {
    tagBtn.addEventListener('click', async () => {
      tagBtn.disabled = true;
      tagBtn.textContent = 'Classifying...';
      status('Classifying shaders...');
      try {
        const r = await postWireClassifyEffects();
        status('Classified: ' + r.scanned + ' scanned, ' + r.effectsTagged + ' effects, ' + r.sourcesTagged + ' sources');
        tagBtn.textContent = 'Tagged ' + r.effectsTagged + 'E / ' + r.sourcesTagged + 'S';
        tagBtn.classList.remove('btn-wire-highlight');
        const fresh = await fetchIndex();
        entries.length = 0;
        entries.push(...fresh);
        if (refreshBrowser) refreshBrowser();
        setTimeout(() => { tagBtn.textContent = 'Tag Effects'; tagBtn.disabled = false; }, 3000);
      } catch (err) {
        status('Classify failed: ' + (err as Error).message, true);
        tagBtn.textContent = 'Tag Effects';
        tagBtn.disabled = false;
      }
    });
  }

  const genSrcBtn = toolbar.querySelector('#wireGenSources') as HTMLButtonElement | null;
  if (genSrcBtn) {
    genSrcBtn.addEventListener('click', async () => {
      genSrcBtn.disabled = true;
      genSrcBtn.textContent = 'Generating...';
      status('Generating source Wire patches...');
      try {
        const seedSet = (toolbar.querySelector('#wireSeedSet') as HTMLSelectElement)?.value || '';
        const seedMode = (toolbar.querySelector('#wireSeedMode') as HTMLSelectElement)?.value || 'standard';
        const seedFxLevel = (toolbar.querySelector('#wireSeedFxLevel') as HTMLSelectElement)?.value || 'advanced';
        const seedFFT = (toolbar.querySelector('#wireSeedFFT') as HTMLInputElement)?.checked ?? true;
        const seedWebcam = (toolbar.querySelector('#wireSeedWebcam') as HTMLInputElement)?.checked ?? true;
        const seedGlitch = (toolbar.querySelector('#wireSeedGlitch') as HTMLInputElement)?.checked ?? true;
        const seedMidi = (toolbar.querySelector('#wireSeedMidi') as HTMLInputElement)?.checked ?? true;
        const seedDryRun = (toolbar.querySelector('#wireSeedDryRun') as HTMLInputElement)?.checked ?? false;
        const r = await postSeedWire({
          autoSeed: true,
          set: seedSet || undefined,
          mode: seedMode,
          features: { fft: seedFFT, webcam: seedWebcam, glitch: seedGlitch, midi: seedMidi },
          fxLevel: seedFxLevel,
          dryRun: seedDryRun,
        });
        status('Generated ' + r.generated.length + ' Wire patches, ' + r.skipped.length + ' skipped');
        genSrcBtn.textContent = r.generated.length + ' generated';
        libraryEntries = await fetchWireLibrary();
        if (refreshLibrary) refreshLibrary();
        setTimeout(() => { genSrcBtn.textContent = 'Gen Sources'; genSrcBtn.disabled = false; }, 3000);
      } catch (err) {
        status('Gen sources failed: ' + (err as Error).message, true);
        genSrcBtn.textContent = 'Gen Sources';
        genSrcBtn.disabled = false;
      }
    });
  }

  const genEffBtn = toolbar.querySelector('#wireGenEffects') as HTMLButtonElement | null;
  if (genEffBtn) {
    genEffBtn.addEventListener('click', async () => {
      genEffBtn.disabled = true;
      genEffBtn.textContent = 'Generating...';
      status('Generating effect Wire patches...');
      try {
        const r = await postWireGenerateEffects();
        status('Generated ' + r.generated + ' effect patches' + (r.errors.length ? ', ' + r.errors.length + ' errors' : ''));
        genEffBtn.textContent = r.generated + ' generated';
        libraryEntries = await fetchWireLibrary();
        if (refreshLibrary) refreshLibrary();
        setTimeout(() => { genEffBtn.textContent = 'Gen Effects'; genEffBtn.disabled = false; }, 3000);
      } catch (err) {
        status('Gen effects failed: ' + (err as Error).message, true);
        genEffBtn.textContent = 'Gen Effects';
        genEffBtn.disabled = false;
      }
    });
  }

  const avcBtn = toolbar.querySelector('#wireRebuildAvenue') as HTMLButtonElement | null;
  if (avcBtn) {
    avcBtn.addEventListener('click', async () => {
      avcBtn.disabled = true;
      avcBtn.textContent = 'Building...';
      status('Rebuilding Avenue composition...');
      try {
        const msg = await postSeedAvenue({});
        status('Avenue: ' + msg.slice(0, 120));
        avcBtn.textContent = 'Done';
        setTimeout(() => { avcBtn.textContent = 'Rebuild Avenue'; avcBtn.disabled = false; }, 3000);
      } catch (err) {
        status('Avenue failed: ' + (err as Error).message, true);
        avcBtn.textContent = 'Rebuild Avenue';
        avcBtn.disabled = false;
      }
    });
  }

  const compileAllBtn = toolbar.querySelector('#wireCompileAll') as HTMLButtonElement | null;
  if (compileAllBtn) {
    compileAllBtn.addEventListener('click', async () => {
      compileAllBtn.disabled = true;
      compileAllBtn.textContent = 'Compiling...';
      status('Bulk compiling Wire patches with Macroverse metadata...');
      try {
        const r = await postWireCompile({ author: 'Macroverse', vendor: 'aday.net.au', url: 'aday@aday.net.au', mail: '' });
        const msg = `Updated ${r.updated}/${r.total} metadata, compiled ${r.compiled}`;
        status(msg + (r.errors.length ? ' (' + r.errors.length + ' errors)' : ''));
        compileAllBtn.textContent = r.updated + ' updated';
        if (r.errors.length > 0) {
          console.warn('Compile errors:', r.errors);
        }
        libraryEntries = await fetchWireLibrary();
        if (refreshLibrary) refreshLibrary();
        setTimeout(() => { compileAllBtn.textContent = 'Compile All'; compileAllBtn.disabled = false; }, 3000);
      } catch (err) {
        status('Compile failed: ' + (err as Error).message, true);
        compileAllBtn.textContent = 'Compile All';
        compileAllBtn.disabled = false;
      }
    });
  }

  const exportBtn = toolbar.querySelector('#wireExportLib') as HTMLButtonElement | null;
  if (exportBtn) {
    exportBtn.addEventListener('click', () => {
      const json = JSON.stringify(libraryEntries, null, 2);
      const blob = new Blob([json], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'wire-library-export.json';
      a.click();
      URL.revokeObjectURL(url);
      status('Exported ' + libraryEntries.length + ' entries');
    });
  }

  const compileBtn = toolbar.querySelector('#wireBulkCompile') as HTMLButtonElement | null;
  if (compileBtn) {
    compileBtn.addEventListener('click', async () => {
      compileBtn.disabled = true;
      compileBtn.textContent = 'Compiling...';
      status('Bulk compiling Wire patches...');
      try {
        const res = await fetch('/api/wire/bulk-compile', { method: 'POST' });
        if (!res.ok) throw new Error(await res.text() || res.statusText);
        const r = await res.json() as { compiled: number; errors: string[]; paths: string[] };
        status('Compiled ' + r.compiled + ' patches' + (r.errors.length ? ', ' + r.errors.length + ' errors' : ''));
        compileBtn.textContent = r.compiled + ' compiled';
        if (r.errors.length > 0) {
          console.warn('Compile errors:', r.errors);
        }
        libraryEntries = await fetchWireLibrary();
        if (refreshLibrary) refreshLibrary();
        setTimeout(() => { compileBtn.textContent = 'Bulk Compile'; compileBtn.disabled = false; }, 3000);
      } catch (err) {
        status('Compile failed: ' + (err as Error).message, true);
        compileBtn.textContent = 'Bulk Compile';
        compileBtn.disabled = false;
      }
    });
  }
}

/* ── Escape HTML ─────────────────────────────────────────────────────── */

function esc(s: string): string {
  return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}
