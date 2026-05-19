import { status } from '../dom.js';
import { entries, listFormatFilter, listSetFilter, listSearchQuery, setListFormatFilter, setListSetFilter, setListSearchQuery, galleryPage, galleryPerPage, galleryGridCols, galleryGridRows, galleryResolution, galleryQuality, galleryAutoAdvanceEnabled, galleryAutoAdvanceIntervalSec, galleryAutoAdvanceMode, galleryFocusedIndex, setGalleryPage, setGalleryPerPage, setGalleryResolution, setGalleryQuality, setGalleryAutoAdvanceEnabled, setGalleryAutoAdvanceIntervalSec, setGalleryAutoAdvanceMode, setGalleryFocusedIndex, initGalleryStateFromStorage, galleryPresetTags, GALLERY_PRESET_SETS } from '../state.js';
import { filterEntries } from '../list.js';
import { fetchShader, postUpdate, postOpenInCursor, postOpenInExplorer, postOpenInNotepad, fetchGitLog, postGitRevertVersion, postSeedWire, postSeedAvenue, postBulkRename, postTagScan, postWireClassifyEffects, postOpenInResolume } from '../api.js';
import { stripLeadingGarbage, prepareFragmentForOffscreenRender } from '../render.js';
import { clampContextMenuToViewport } from '../dom.js';
import { roliblockManager } from '../engines/roliblock.js';
import type { IndexEntry } from '../types.js';

const GALLERY_FPS = 15;
const GALLERY_LED_SEND_INTERVAL_MS = 40;
const vertSrc = `precision highp float;
attribute vec2 a_pos;
varying vec2 v_uv;
varying vec2 surfacePosition;
void main() {
  vec2 uv = a_pos * 0.5 + 0.5;
  v_uv = uv;
  surfacePosition = uv;
  gl_Position = vec4(a_pos, 0.0, 1.0);
}`;
const quadVerts = new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]);

interface GalleryCellState {
  gl: WebGLRenderingContext;
  program: WebGLProgram;
  buffer: WebGLBuffer;
  defaultTex: WebGLTexture;
  startTime: number;
  width: number;
  height: number;
}

const cellStates = new Map<number, GalleryCellState>();
let galleryRafId = 0;
let lastGalleryDrawTime = 0;
let lastGalleryLedSend = 0;

let galleryRoliblockMouseX = 0.5;
let galleryRoliblockMouseY = 0.5;

export function setGalleryRoliblockMouse(x: number, y: number): void {
  galleryRoliblockMouseX = x;
  galleryRoliblockMouseY = y;
}

function compileShader(gl: WebGLRenderingContext, type: number, src: string): WebGLShader {
  const s = gl.createShader(type);
  if (!s) throw new Error('createShader failed');
  gl.shaderSource(s, src);
  gl.compileShader(s);
  if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) {
    const log = gl.getShaderInfoLog(s);
    gl.deleteShader(s);
    throw new Error(log || 'compile failed');
  }
  return s;
}

function createCellProgram(gl: WebGLRenderingContext, fragSrc: string): WebGLProgram {
  const prepared = prepareFragmentForOffscreenRender(stripLeadingGarbage(fragSrc || ''));
  const v = compileShader(gl, gl.VERTEX_SHADER, vertSrc);
  const f = compileShader(gl, gl.FRAGMENT_SHADER, prepared);
  const p = gl.createProgram();
  if (!p) throw new Error('createProgram failed');
  gl.attachShader(p, v);
  gl.attachShader(p, f);
  gl.linkProgram(p);
  gl.deleteShader(v);
  gl.deleteShader(f);
  if (!gl.getProgramParameter(p, gl.LINK_STATUS)) {
    const log = gl.getProgramInfoLog(p);
    gl.deleteProgram(p);
    throw new Error(log || 'link failed');
  }
  return p;
}

function createDefaultTex(gl: WebGLRenderingContext): WebGLTexture {
  const tex = gl.createTexture();
  if (!tex) throw new Error('createTexture failed');
  gl.bindTexture(gl.TEXTURE_2D, tex);
  gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0, gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([128, 128, 128, 255]));
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
  return tex;
}

function setCellUniforms(gl: WebGLRenderingContext, prog: WebGLProgram, w: number, h: number, time: number, frameIndex: number, defaultTex: WebGLTexture, cellIndex: number): void {
  const timeLoc = gl.getUniformLocation(prog, 'TIME');
  if (timeLoc) gl.uniform1f(timeLoc, time);
  const timeLowerLoc = gl.getUniformLocation(prog, 'time');
  if (timeLowerLoc) gl.uniform1f(timeLowerLoc, time);
  const resLoc = gl.getUniformLocation(prog, 'resolution');
  if (resLoc) gl.uniform2f(resLoc, w, h);
  const rendLoc = gl.getUniformLocation(prog, 'RENDERSIZE');
  if (rendLoc) gl.uniform2f(rendLoc, w, h);
  const isFocused = cellIndex === galleryFocusedIndex;
  const mx = isFocused ? galleryRoliblockMouseX : 0.5;
  const my = isFocused ? galleryRoliblockMouseY : 0.5;
  const mouseLoc = gl.getUniformLocation(prog, 'mouse');
  if (mouseLoc) gl.uniform2f(mouseLoc, mx, my);
  const mouseXLoc = gl.getUniformLocation(prog, 'mouseX');
  if (mouseXLoc) gl.uniform1f(mouseXLoc, mx);
  const mouseYLoc = gl.getUniformLocation(prog, 'mouseY');
  if (mouseYLoc) gl.uniform1f(mouseYLoc, my);
  const uMouseLoc = gl.getUniformLocation(prog, 'uMouse');
  if (uMouseLoc) gl.uniform2f(uMouseLoc, mx, my);
  const iframeLoc = gl.getUniformLocation(prog, 'iFrame');
  if (iframeLoc) gl.uniform1f(iframeLoc, frameIndex);
  const frameIndexLoc = gl.getUniformLocation(prog, 'FRAMEINDEX');
  if (frameIndexLoc) gl.uniform1f(frameIndexLoc, frameIndex);
  const SAMPLER2D = 35678;
  const n = gl.getProgramParameter(prog, gl.ACTIVE_UNIFORMS) as number;
  for (let i = 0; i < n; i++) {
    const u = gl.getActiveUniform(prog, i);
    if (!u || u.type !== SAMPLER2D) continue;
    gl.activeTexture(gl.TEXTURE0 + i);
    gl.bindTexture(gl.TEXTURE_2D, defaultTex);
    const loc = gl.getUniformLocation(prog, u.name);
    if (loc) gl.uniform1i(loc, i);
  }
  const skip = new Set(['time', 'mouse', 'resolution', 'TIME', 'RENDERSIZE', 'uMouse', 'iFrame', 'mouseX', 'mouseY', 'FRAMEINDEX', 'fps']);
  const n2 = gl.getProgramParameter(prog, gl.ACTIVE_UNIFORMS) as number;
  for (let i = 0; i < n2; i++) {
    const u = gl.getActiveUniform(prog, i);
    if (!u) continue;
    if (u.type === gl.FLOAT && !skip.has(u.name)) {
      const loc = gl.getUniformLocation(prog, u.name);
      if (loc) gl.uniform1f(loc, 0.5);
    } else if (u.type === gl.BOOL) {
      const loc = gl.getUniformLocation(prog, u.name);
      if (loc) gl.uniform1i(loc, 0);
    }
  }
}

function disposeCellStates(): void {
  cellStates.forEach((s) => {
    s.gl.deleteProgram(s.program);
    s.gl.deleteBuffer(s.buffer);
    s.gl.deleteTexture(s.defaultTex);
  });
  cellStates.clear();
}

function galleryDrawLoop(): void {
  galleryRafId = 0;
  if (!document.querySelector('.view-tab[data-view="gallery"].active')) return;
  const now = performance.now() / 1000;
  const frameInterval = 1 / GALLERY_FPS;
  if (now - lastGalleryDrawTime < frameInterval && cellStates.size > 0) {
    galleryRafId = requestAnimationFrame(galleryDrawLoop);
    return;
  }
  lastGalleryDrawTime = now;
  cellStates.forEach((state, index) => {
    const gl = state.gl;
    const time = now - state.startTime;
    const frameIndex = Math.floor(time * 60);
    gl.viewport(0, 0, state.width, state.height);
    gl.useProgram(state.program);
    const posLoc = gl.getAttribLocation(state.program, 'a_pos');
    gl.bindBuffer(gl.ARRAY_BUFFER, state.buffer);
    gl.enableVertexAttribArray(posLoc);
    gl.vertexAttribPointer(posLoc, 2, gl.FLOAT, false, 0, 0);
    setCellUniforms(gl, state.program, state.width, state.height, time, frameIndex, state.defaultTex, index);
    gl.clearColor(0.1, 0.1, 0.1, 1);
    gl.clear(gl.COLOR_BUFFER_BIT);
    gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
  });
  if (document.querySelector('.view-tab[data-view="gallery"].active')) {
    const nowMs = Date.now();
    if (nowMs - lastGalleryLedSend >= GALLERY_LED_SEND_INTERVAL_MS) {
      const focusedState = cellStates.get(galleryFocusedIndex);
      const canvas = focusedState?.gl?.canvas;
      if (canvas) {
        for (const dev of roliblockManager.getDevices()) {
          if (dev.enabled) dev.sampleAndSendLed(canvas as HTMLCanvasElement);
        }
        lastGalleryLedSend = nowMs;
      }
    }
  }
  galleryRafId = requestAnimationFrame(galleryDrawLoop);
}

function getAllSetNames(): string[] {
  const names = new Set<string>();
  entries.forEach((e) => (e.sets || []).forEach((s) => names.add(s)));
  return [...names].sort();
}

function getPageEntries(): IndexEntry[] {
  const filtered = filterEntries();
  const start = galleryPage * galleryPerPage;
  return filtered.slice(start, start + galleryPerPage);
}

function getTotalPages(): number {
  const filtered = filterEntries();
  return Math.max(1, Math.ceil(filtered.length / galleryPerPage));
}

function exportListAsCsv(rows: IndexEntry[]): string {
  const header = 'path,name,fixedName,category,tags,sets,format';
  const escape = (s: string) => {
    const t = String(s).replace(/"/g, '""');
    return t.includes(',') || t.includes('"') || t.includes('\n') ? '"' + t + '"' : t;
  };
  const lines = [header];
  for (const e of rows) {
    lines.push([
      escape(e.path || ''),
      escape(e.name || ''),
      escape(e.fixedName || ''),
      escape(e.category || ''),
      (e.tags || []).join(';'),
      (e.sets || []).join(';'),
      escape(e.format || '')
    ].join(','));
  }
  return lines.join('\n');
}

function exportListAsJson(rows: IndexEntry[]): string {
  const out = rows.map((e) => ({
    path: e.path,
    name: e.name,
    fixedName: e.fixedName,
    category: e.category,
    tags: e.tags || [],
    sets: e.sets || [],
    format: e.format
  }));
  return JSON.stringify(out, null, 2);
}

function doExportList(scope: 'filtered' | 'page'): void {
  const filtered = filterEntries();
  const rows = scope === 'page' ? getPageEntries() : filtered;
  if (rows.length === 0) {
    status('No shaders to export', true);
    return;
  }
  const csv = exportListAsCsv(rows);
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'shader-list-' + (scope === 'page' ? 'page' + (galleryPage + 1) : 'filtered') + '.csv';
  a.click();
  URL.revokeObjectURL(url);
  status('Exported ' + rows.length + ' shaders (CSV)');
}

function doExportListJson(scope: 'filtered' | 'page'): void {
  const filtered = filterEntries();
  const rows = scope === 'page' ? getPageEntries() : filtered;
  if (rows.length === 0) {
    status('No shaders to export', true);
    return;
  }
  const json = exportListAsJson(rows);
  const blob = new Blob([json], { type: 'application/json;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'shader-list-' + (scope === 'page' ? 'page' + (galleryPage + 1) : 'filtered') + '.json';
  a.click();
  URL.revokeObjectURL(url);
  status('Exported ' + rows.length + ' shaders (JSON)');
}

function doExportPaths(rows: IndexEntry[], suffix = 'filtered'): void {
  if (rows.length === 0) { status('No shaders to export', true); return; }
  const lines = rows.map((e) => (e.path || '').replace(/\|/g, '\\'));
  const txt = lines.join('\n');
  const blob = new Blob([txt], { type: 'text/plain;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'macroverse-' + suffix + '.txt';
  a.click();
  URL.revokeObjectURL(url);
  status('Exported ' + rows.length + ' paths');
}

function toKebabCase(name: string): string {
  let s = name;
  // Strip file extension if present
  s = s.replace(/\.(fs|frag|glsl|isf|txt)$/i, '');
  // Remove -fixed / _fixed suffix
  s = s.replace(/[-_]fixed$/i, '');
  // Remove trailing _1, _2 etc (duplicate suffixes)
  s = s.replace(/_(\d+)$/, '-$1');
  // Insert hyphen before uppercase runs: "BouncyBalls" → "Bouncy-Balls"
  s = s.replace(/([a-z0-9])([A-Z])/g, '$1-$2');
  // Insert hyphen between uppercase run and lowercase: "GLSLNoise" → "GLSL-Noise"
  s = s.replace(/([A-Z]+)([A-Z][a-z])/g, '$1-$2');
  // Replace underscores and spaces with hyphens
  s = s.replace(/[_\s]+/g, '-');
  // Collapse multiple hyphens
  s = s.replace(/-{2,}/g, '-');
  // Lowercase
  s = s.toLowerCase();
  // Strip leading/trailing hyphens
  s = s.replace(/^-+|-+$/g, '');
  return s;
}

function buildToolbar(): void {
  const toolbar = document.getElementById('galleryToolbar');
  if (!toolbar) return;

  const setNames = getAllSetNames();
  const totalFiltered = filterEntries().length;
  const totalPages = getTotalPages();

  toolbar.innerHTML = '';
  const add = (el: HTMLElement) => toolbar.appendChild(el);

  const row1 = document.createElement('div');
  row1.style.display = 'flex';
  row1.style.flexWrap = 'wrap';
  row1.style.alignItems = 'center';
  row1.style.gap = '8px';

  const formatLabel = document.createElement('span');
  formatLabel.className = 'toolbar-group-label';
  formatLabel.textContent = 'Format';
  row1.appendChild(formatLabel);

  const formatSelect = document.createElement('select');
  formatSelect.title = 'Filter by format';
  ['all', 'glsl', 'isf'].forEach((v) => {
    const o = document.createElement('option');
    o.value = v;
    o.textContent = v === 'all' ? 'All' : v.toUpperCase();
    if (listFormatFilter === v) o.selected = true;
    formatSelect.appendChild(o);
  });
  formatSelect.addEventListener('change', () => {
    setListFormatFilter(formatSelect.value as 'all' | 'glsl' | 'isf');
    setGalleryPage(0);
    refreshGallery();
  });
  row1.appendChild(formatSelect);

  const setLabel = document.createElement('span');
  setLabel.className = 'toolbar-group-label';
  setLabel.textContent = 'Set';
  setLabel.style.marginLeft = '8px';
  row1.appendChild(setLabel);

  const setSelect = document.createElement('select');
  setSelect.title = 'Filter by set';
  const setOptAll = document.createElement('option');
  setOptAll.value = '';
  setOptAll.textContent = 'All sets';
  setSelect.appendChild(setOptAll);
  setNames.forEach((s) => {
    const o = document.createElement('option');
    o.value = s;
    o.textContent = s;
    if (listSetFilter === s) o.selected = true;
    setSelect.appendChild(o);
  });
  setSelect.addEventListener('change', () => {
    setListSetFilter(setSelect.value || null);
    setGalleryPage(0);
    refreshGallery();
  });
  row1.appendChild(setSelect);

  const searchLabel = document.createElement('span');
  searchLabel.className = 'toolbar-group-label';
  searchLabel.textContent = 'Search';
  searchLabel.style.marginLeft = '8px';
  row1.appendChild(searchLabel);

  const searchInput = document.createElement('input');
  searchInput.type = 'text';
  searchInput.placeholder = 'name, tags...';
  searchInput.value = listSearchQuery;
  searchInput.addEventListener('input', () => {
    setListSearchQuery(searchInput.value);
    setGalleryPage(0);
    refreshGallery();
  });
  row1.appendChild(searchInput);

  const pageInfo = document.createElement('span');
  pageInfo.style.fontSize = '10px';
  pageInfo.style.color = 'var(--crt-dim)';
  pageInfo.style.marginLeft = '8px';
  pageInfo.textContent = totalFiltered + ' shaders | page ' + (galleryPage + 1) + ' / ' + totalPages;
  row1.appendChild(pageInfo);

  add(row1);

  const row2 = document.createElement('div');
  row2.style.display = 'flex';
  row2.style.flexWrap = 'wrap';
  row2.style.alignItems = 'center';
  row2.style.gap = '8px';
  row2.style.marginTop = '6px';

  const resLabel = document.createElement('span');
  resLabel.className = 'toolbar-group-label';
  resLabel.textContent = 'Resolution';
  row2.appendChild(resLabel);

  const resSelect = document.createElement('select');
  resSelect.title = 'Cell resolution';
  [256, 320, 426, 512].forEach((w) => {
    const o = document.createElement('option');
    o.value = String(w);
    o.textContent = w + 'px';
    if (galleryResolution === w) o.selected = true;
    resSelect.appendChild(o);
  });
  resSelect.addEventListener('change', () => {
    setGalleryResolution(Number(resSelect.value));
    refreshGallery();
  });
  row2.appendChild(resSelect);

  const qualityLabel = document.createElement('span');
  qualityLabel.className = 'toolbar-group-label';
  qualityLabel.textContent = 'Quality';
  qualityLabel.style.marginLeft = '8px';
  row2.appendChild(qualityLabel);

  const qualitySelect = document.createElement('select');
  qualitySelect.title = 'Render quality scale';
  [0.5, 0.75, 1].forEach((q) => {
    const o = document.createElement('option');
    o.value = String(q);
    o.textContent = String(q) + 'x';
    if (galleryQuality === q) o.selected = true;
    qualitySelect.appendChild(o);
  });
  qualitySelect.addEventListener('change', () => {
    setGalleryQuality(Number(qualitySelect.value));
    refreshGallery();
  });
  row2.appendChild(qualitySelect);

  const perPageLabel = document.createElement('span');
  perPageLabel.className = 'toolbar-group-label';
  perPageLabel.textContent = 'Per page';
  perPageLabel.style.marginLeft = '8px';
  row2.appendChild(perPageLabel);

  const perPageSelect = document.createElement('select');
  perPageSelect.title = 'Shaders per page';
  [1, 4, 8, 14, 24].forEach((n) => {
    const o = document.createElement('option');
    o.value = String(n);
    o.textContent = String(n);
    if (galleryPerPage === n) o.selected = true;
    perPageSelect.appendChild(o);
  });
  perPageSelect.addEventListener('change', () => {
    setGalleryPerPage(Number(perPageSelect.value));
    setGalleryPage(0);
    refreshGallery();
  });
  row2.appendChild(perPageSelect);

  const autoLabel = document.createElement('label');
  autoLabel.style.display = 'inline-flex';
  autoLabel.style.alignItems = 'center';
  autoLabel.style.gap = '4px';
  autoLabel.style.marginLeft = '12px';
  autoLabel.style.fontSize = '10px';
  autoLabel.style.color = 'var(--crt-dim)';
  const autoCheck = document.createElement('input');
  autoCheck.type = 'checkbox';
  autoCheck.checked = galleryAutoAdvanceEnabled;
  autoCheck.addEventListener('change', () => {
    setGalleryAutoAdvanceEnabled(autoCheck.checked);
    refreshGallery();
  });
  autoLabel.appendChild(autoCheck);
  autoLabel.appendChild(document.createTextNode('Auto-advance'));
  row2.appendChild(autoLabel);

  const intervalSelect = document.createElement('select');
  intervalSelect.title = 'Auto-advance interval (seconds)';
  intervalSelect.style.marginLeft = '4px';
  [3, 5, 8, 12, 20].forEach((s) => {
    const o = document.createElement('option');
    o.value = String(s);
    o.textContent = s + 's';
    if (galleryAutoAdvanceIntervalSec === s) o.selected = true;
    intervalSelect.appendChild(o);
  });
  intervalSelect.addEventListener('change', () => {
    setGalleryAutoAdvanceIntervalSec(Number(intervalSelect.value));
  });
  row2.appendChild(intervalSelect);

  const modeSelect = document.createElement('select');
  modeSelect.title = 'Advance by shader or by page';
  const optShader = document.createElement('option');
  optShader.value = 'shader';
  optShader.textContent = 'Next shader';
  if (galleryAutoAdvanceMode === 'shader') optShader.selected = true;
  modeSelect.appendChild(optShader);
  const optPage = document.createElement('option');
  optPage.value = 'page';
  optPage.textContent = 'Next page';
  if (galleryAutoAdvanceMode === 'page') optPage.selected = true;
  modeSelect.appendChild(optPage);
  modeSelect.addEventListener('change', () => {
    setGalleryAutoAdvanceMode(modeSelect.value as 'shader' | 'page');
  });
  row2.appendChild(modeSelect);

  function makeExportBtn(label: string, fn: () => void, closeMenu: () => void): HTMLButtonElement {
    const b = document.createElement('button');
    b.type = 'button'; b.className = 'wire-btn';
    b.style.cssText = 'display:block;width:100%;margin-bottom:3px;text-align:left;';
    b.textContent = label;
    b.addEventListener('click', () => { fn(); closeMenu(); });
    return b;
  }

  const exportBtn = document.createElement('button');
  exportBtn.type = 'button';
  exportBtn.className = 'wire-btn';
  exportBtn.textContent = 'Export ▾';
  exportBtn.title = 'Export shaders as CSV, JSON, or paths list';
  exportBtn.style.marginLeft = '12px';
  exportBtn.addEventListener('click', (evt) => {
    evt.stopPropagation();
    const menu = document.createElement('div');
    menu.style.cssText = 'position:fixed;background:var(--amiga-panel);border:2px solid var(--amiga-copper);padding:6px;z-index:10001;min-width:200px;';
    const close = () => { menu.remove(); document.removeEventListener('click', close); };

    const addSep = (label: string) => {
      const s = document.createElement('div');
      s.style.cssText = 'font-size:9px;color:var(--amiga-copper);text-transform:uppercase;padding:4px 2px 2px;letter-spacing:.08em;border-top:1px solid var(--bevel-dark);margin-top:3px;';
      s.textContent = label;
      menu.appendChild(s);
    };

    addSep('Current view');
    menu.appendChild(makeExportBtn('CSV — filtered list', () => doExportList('filtered'), close));
    menu.appendChild(makeExportBtn('CSV — this page', () => doExportList('page'), close));
    menu.appendChild(makeExportBtn('JSON — filtered list', () => doExportListJson('filtered'), close));
    menu.appendChild(makeExportBtn('Paths (.txt) — filtered', () => doExportPaths(filterEntries()), close));

    const allSets = getAllSetNames();
    if (allSets.length > 0) {
      addSep('Export by Set');
      allSets.forEach((setName) => {
        const setRows = entries.filter((e) => (e.sets || []).includes(setName));
        menu.appendChild(makeExportBtn(setName + ' (' + setRows.length + ')', () => {
          doExportPaths(setRows, 'set-' + setName);
        }, close));
      });
    }

    const allTags = [...new Set(entries.flatMap((e) => e.tags || []))].sort();
    if (allTags.length > 0) {
      addSep('Export by Tag');
      allTags.slice(0, 12).forEach((tag) => {
        const tagRows = entries.filter((e) => (e.tags || []).includes(tag));
        menu.appendChild(makeExportBtn(tag + ' (' + tagRows.length + ')', () => {
          doExportPaths(tagRows, 'tag-' + tag);
        }, close));
      });
    }

    const rect = exportBtn.getBoundingClientRect();
    menu.style.left = rect.left + 'px';
    menu.style.top = (rect.bottom + 4) + 'px';
    document.body.appendChild(menu);
    setTimeout(() => document.addEventListener('click', close), 0);
  });
  row2.appendChild(exportBtn);

  const helpBtn = document.createElement('button');
  helpBtn.type = 'button';
  helpBtn.className = 'wire-btn';
  helpBtn.textContent = '?';
  helpBtn.title = 'Show keyboard shortcuts';
  helpBtn.style.marginLeft = '4px';
  helpBtn.addEventListener('click', () => { toggleShortcutHud(); });
  row2.appendChild(helpBtn);

  const prevBtn = document.createElement('button');
  prevBtn.type = 'button';
  prevBtn.className = 'wire-btn';
  prevBtn.textContent = 'Prev';
  prevBtn.title = 'Previous page';
  prevBtn.disabled = galleryPage <= 0;
  prevBtn.addEventListener('click', () => {
    setGalleryPage(Math.max(0, galleryPage - 1));
    refreshGallery();
  });
  row2.appendChild(prevBtn);

  const nextBtn = document.createElement('button');
  nextBtn.type = 'button';
  nextBtn.className = 'wire-btn';
  nextBtn.textContent = 'Next';
  nextBtn.title = 'Next page';
  nextBtn.disabled = galleryPage >= totalPages - 1;
  nextBtn.addEventListener('click', () => {
    setGalleryPage(Math.min(totalPages - 1, galleryPage + 1));
    refreshGallery();
  });
  row2.appendChild(nextBtn);

  add(row2);

  const row3 = document.createElement('div');
  row3.style.cssText = 'display:flex;flex-wrap:wrap;align-items:center;gap:8px;margin-top:4px;font-size:10px;color:var(--crt-dim);';

  const shortcutHint = document.createElement('span');
  shortcutHint.style.cssText = 'color:var(--crt-dim);font-size:10px;';
  shortcutHint.innerHTML = '<b style="color:var(--amiga-accent);">←→↑↓</b> navigate &nbsp; <b style="color:var(--amiga-accent);">1-9</b> toggle tag &nbsp; <b style="color:var(--amiga-accent);">Shift+1-9</b> toggle set &nbsp; <b style="color:var(--amiga-accent);">A</b> set prompt &nbsp; <b style="color:var(--amiga-accent);">F</b> fav &nbsp; <b style="color:var(--amiga-accent);">R</b> rename &nbsp; <b style="color:var(--amiga-accent);">?</b> full help';
  row3.appendChild(shortcutHint);

  const seedBtn = document.createElement('button');
  seedBtn.type = 'button';
  seedBtn.className = 'wire-btn';
  seedBtn.textContent = 'Seed VJ Sets';
  seedBtn.title = 'Create boilerplate VJ set names and auto-assign shaders by format/category';
  seedBtn.style.marginLeft = 'auto';
  seedBtn.addEventListener('click', () => {
    const toSeed = entries.filter((e) => {
      const sets = e.sets || [];
      return !GALLERY_PRESET_SETS.some((ps) => sets.includes(ps));
    });
    if (toSeed.length === 0) { status('All shaders already in VJ sets'); return; }
    let queued = 0;
    const promises = toSeed.map((e) => {
      const sets = [...(e.sets || [])];
      // Auto-assign based on format and category/name heuristics
      if (e.format === 'isf' && !sets.includes('vj-wire-ready')) sets.push('vj-wire-ready');
      const nm = ((e.fixedName || e.name || '') + ' ' + (e.category || '') + ' ' + ((e.path || '').split(/[/\\|]/).slice(-2, -1)[0] || '')).toLowerCase();

      // vj-ambient: slow, atmospheric, generative, drifting
      if (/plasma|cloud|smoke|fog|fluid|noise|ambient|drift|flow|aurora|mist|ether|aether|haze|vapor|zen|calm|serene|breath|gentle|slow|glow|fade|bloom|bokeh|blur|soft|dream|float|breeze|evolve|morph|liquid|water|ocean|wave|ripple|rain|snow|ice|frost|crystal|bubble/.test(nm) && !sets.includes('vj-ambient')) sets.push('vj-ambient');

      // vj-techno: fast, harsh, rhythmic, strobe, beat-reactive
      if (/techno|strobe|beat|pulse|flash|rapid|blink|flicker|rave|bass|kick|drum|club|disco|dance|edm|electro|synth|acid|hard|harsh|intense|aggressive|sharp|spike|burst|blast|bang|explod|boom|impact|stutter|glide|bounce|bouncy|spin|rotate|zoom|rush|speed|turbo|hyper|cyber|circuit|matrix|neon|laser|light[-_ ]?source|scanner|bars|stripe|smpte|segment|led|dot[-_ ]?matrix/.test(nm) && !sets.includes('vj-techno')) sets.push('vj-techno');

      // vj-glitch: digital, corrupted, databend, noise artifacts
      if (/glitch|corrupt|pixel|databend|scan|error|digital|hack|static|artifact|distort|broken|destroy|noise[-_ ]?overlay|interference|signal|vhs|retro|8bit|8[-_ ]?bit|c64|ascii|encode|decode|compress|binary|data|byte|buffer|overflow|crash|bug|virus|malware|matrix/.test(nm) && !sets.includes('vj-glitch')) sets.push('vj-glitch');

      // vj-cosmic: space, void, nebula, stars, universe
      if (/star|nebula|void|space|cosmic|galaxy|orbit|planet|universe|astral|solar|lunar|moon|sun|comet|meteor|asteroid|nova|supernova|constellation|milky|warp[-_ ]?field|hyperspace|wormhole|singularity|quasar|pulsar|interstellar|celestial|heaven|sky|atmosphere|aurora/.test(nm) && !sets.includes('vj-cosmic')) sets.push('vj-cosmic');

      // vj-geometric: shapes, patterns, grids, kaleidoscope, math
      if (/grid|hex|tria|cube|box|sphere|geomet|kaleid|pattern|tile|tessellat|mosaic|polygon|diamond|octagon|pentagon|prism|pyramid|crystal|lattice|mesh|wireframe|voronoi|delaunay|mandala|symmetric|mirror|fractal|sacred|fibonacci|golden|math|checker|board|cross|circle|ring|torus|donut|cylinder|cone|plane|line|point|vertex|edge|face/.test(nm) && !sets.includes('vj-geometric')) sets.push('vj-geometric');

      // vj-organic: fluid, liquid, biological, nature
      if (/organic|bio|cell|grow|vine|morph|flesh|body|heart|brain|eye|blood|vein|artery|coral|tree|branch|leaf|flower|petal|root|seed|fungus|mushroom|jellyfish|tentacle|amoeba|bacteria|dna|life|evolve|creature|alien|insect|butterfly|worm|snake|fish|bird|feather|fur|skin|tissue|muscle|bone/.test(nm) && !sets.includes('vj-organic')) sets.push('vj-organic');

      // vj-dark: dark, moody, low-energy, horror
      if (/dark|shadow|night|deep|black|death|dead|skull|horror|evil|demon|hell|abyss|doom|grim|gothic|sinister|creep|haunt|ghost|phantom|spectr|ominous|dread|fear|terror|nightmare|grave|tomb|crypt|decay|rot|rust|corrosion|erosion|dissolve|disintegrat|collapse|ruin|wreck|desolat|barren|waste|ash|ember|cinder|char|soot/.test(nm) && !sets.includes('vj-dark')) sets.push('vj-dark');

      // vj-colour: colour-forward, palette-heavy, rainbow, gradient
      if (/color|colour|rgb|hue|rainbow|palette|chroma|gradient|spectrum|prismatic|iridescen|pastel|neon|vivid|saturat|tint|shade|tone|warm|cool|cyan|magenta|crimson|scarlet|violet|indigo|cobalt|emerald|amber|gold|silver|copper|bronze|pearl|opal|sapphire|ruby|jade/.test(nm) && !sets.includes('vj-colour')) sets.push('vj-colour');

      // Category-based fallback: assign by directory name from path
      const dirCat = ((e.path || '').split(/[/\\|]/).slice(-2, -1)[0] || '').toLowerCase();
      if (dirCat === 'tunnel' && !sets.includes('vj-geometric') && !sets.includes('vj-dark')) sets.push('vj-geometric');
      if (dirCat === 'plasma' && !sets.includes('vj-ambient')) sets.push('vj-ambient');
      if (dirCat === 'fractal' && !sets.includes('vj-geometric')) sets.push('vj-geometric');
      if (dirCat === 'particles' && !sets.includes('vj-cosmic') && !sets.includes('vj-organic')) sets.push('vj-cosmic');
      if (dirCat === 'psychedelic' && !sets.includes('vj-colour')) sets.push('vj-colour');
      if (dirCat === 'noise' && !sets.includes('vj-ambient')) sets.push('vj-ambient');
      if (dirCat === 'water' && !sets.includes('vj-ambient') && !sets.includes('vj-organic')) sets.push('vj-ambient');
      if (dirCat === 'space' && !sets.includes('vj-cosmic')) sets.push('vj-cosmic');
      if (dirCat === 'abstract' && !sets.includes('vj-ambient') && !sets.includes('vj-geometric')) sets.push('vj-ambient');
      if (dirCat === 'color' && !sets.includes('vj-colour')) sets.push('vj-colour');
      if (dirCat === 'concept' && !GALLERY_PRESET_SETS.some((s) => sets.includes(s))) sets.push('vj-ambient');
      if ((e.sets || []).join(',') === sets.join(',')) return Promise.resolve();
      queued++;
      return postUpdate({ id: e.id, sets }).then(() => {
        const idx = entries.findIndex((x) => x.id === e.id);
        if (idx >= 0) entries[idx] = { ...entries[idx], sets };
      });
    });
    Promise.all(promises).then(() => {
      refreshGallery();
      status('Seeded VJ sets — ' + queued + ' shader(s) assigned');
    }).catch((err) => status('Seed failed: ' + (err as Error).message, true));
  });
  row3.appendChild(seedBtn);

  const seedWireBtn = document.createElement('button');
  seedWireBtn.type = 'button';
  seedWireBtn.className = 'wire-btn';
  seedWireBtn.textContent = 'Seed Wire Patches';
  seedWireBtn.title = 'Generate Resolume Wire patches from VJ sets (ISF shaders with crossfaders + MIDI-ready controls)';
  seedWireBtn.style.marginLeft = '4px';
  seedWireBtn.addEventListener('click', async () => {
    status('Generating Wire patches from VJ sets...');
    seedWireBtn.disabled = true;
    try {
      const result = await postSeedWire({ autoSeed: true });
      const count = result.generated.length;
      const shaderCount = result.generated.reduce((sum: number, g: { shaders: number }) => sum + g.shaders, 0);
      status(count > 0
        ? `Generated ${count} Wire patch(es) with ${shaderCount} shaders in resolume/`
        : 'No ISF shaders found in VJ sets — run Seed VJ Sets first');
    } catch (err) {
      status('Wire seed failed: ' + (err as Error).message, true);
    } finally {
      seedWireBtn.disabled = false;
    }
  });
  row3.appendChild(seedWireBtn);

  const seedAvenueBtn = document.createElement('button');
  seedAvenueBtn.type = 'button';
  seedAvenueBtn.className = 'wire-btn';
  seedAvenueBtn.textContent = 'Build Avenue (.avc)';
  seedAvenueBtn.title = 'Generate Resolume Avenue composition from Wire patches (decks per VJ set, dashboard, MIDI)';
  seedAvenueBtn.style.marginLeft = '4px';
  seedAvenueBtn.addEventListener('click', async () => {
    status('Generating Resolume Avenue composition...');
    seedAvenueBtn.disabled = true;
    try {
      const result = await postSeedAvenue({});
      // Extract full path from output
      const pathMatch = result.match(/Full path:\s*(.+)/);
      const avcPath = pathMatch ? pathMatch[1].trim() : '';
      if (avcPath) {
        status('Avenue generated: ' + avcPath);
        // Try to open in Resolume automatically
        try { await postOpenInResolume({ path: avcPath }); } catch { /* user can open manually */ }
      } else {
        status(result.includes('WROTE') ? 'Avenue composition generated: macroverse-vj.avc' : result);
      }
    } catch (err) {
      status('Avenue build failed: ' + (err as Error).message, true);
    } finally {
      seedAvenueBtn.disabled = false;
    }
  });
  row3.appendChild(seedAvenueBtn);

  const normalizeBtn = document.createElement('button');
  normalizeBtn.type = 'button';
  normalizeBtn.className = 'wire-btn';
  normalizeBtn.textContent = 'Normalize Names';
  normalizeBtn.title = 'Rename all shaders to consistent kebab-case (e.g. BouncyBalls1 → bouncy-balls-1)';
  normalizeBtn.style.marginLeft = '4px';
  normalizeBtn.addEventListener('click', async () => {
    const filtered = filterEntries();
    if (filtered.length === 0) { status('No shaders to normalize', true); return; }
    const renames: Array<{ id: number; newName: string }> = [];
    const seenInDir = new Map<string, Set<string>>();
    for (const e of filtered) {
      const rawName = e.fixedName || e.name || '';
      if (!rawName) continue;
      let kebab = toKebabCase(rawName);
      if (kebab === rawName) continue;
      const dir = (e.path || '').replace(/[/\\|][^/\\|]*$/, '');
      if (!seenInDir.has(dir)) seenInDir.set(dir, new Set());
      const dirSet = seenInDir.get(dir)!;
      let final = kebab;
      let suffix = 2;
      while (dirSet.has(final)) { final = kebab + '-' + suffix++; }
      dirSet.add(final);
      renames.push({ id: e.id, newName: final });
    }
    if (renames.length === 0) { status('All names already normalized'); return; }
    if (!window.confirm('Normalize ' + renames.length + ' shader names to kebab-case?\n\nExamples:\n' +
      renames.slice(0, 5).map((r) => {
        const orig = filtered.find((e) => e.id === r.id);
        return '  ' + (orig?.fixedName || orig?.name || '') + ' → ' + r.newName;
      }).join('\n') +
      (renames.length > 5 ? '\n  ... and ' + (renames.length - 5) + ' more' : ''))) return;
    normalizeBtn.disabled = true;
    status('Normalizing ' + renames.length + ' shader names...');
    try {
      const result = await postBulkRename(renames);
      if (result.renamed > 0) {
        for (const r of result.results) {
          if (!r.error) {
            const idx = entries.findIndex((e) => e.id === r.id);
            if (idx >= 0) entries[idx] = { ...entries[idx], fixedName: r.newName };
          }
        }
        refreshGallery();
      }
      const msg = 'Normalized ' + result.renamed + ' names' +
        (result.errors.length > 0 ? ' (' + result.errors.length + ' errors)' : '');
      status(msg, result.errors.length > 0);
    } catch (err) {
      status('Normalize failed: ' + (err as Error).message, true);
    } finally {
      normalizeBtn.disabled = false;
    }
  });
  row3.appendChild(normalizeBtn);

  const tagScanBtn = document.createElement('button');
  tagScanBtn.type = 'button';
  tagScanBtn.className = 'wire-btn';
  tagScanBtn.textContent = 'Tag Roliblock + Mouse';
  tagScanBtn.title = 'Scan all shaders: tag mouse-interactive shaders and identify roliblock-suitable shaders for Roli Lightpad';
  tagScanBtn.style.marginLeft = '4px';
  tagScanBtn.addEventListener('click', async () => {
    tagScanBtn.disabled = true;
    status('Scanning shaders for mouse uniforms and roliblock suitability...');
    try {
      const result = await postTagScan();
      const parts: string[] = [];
      parts.push('Scanned ' + result.scanned);
      if (result.mouseTagged > 0) parts.push(result.mouseTagged + ' mouse-interactive');
      if (result.roliTagged > 0) parts.push(result.roliTagged + ' roliblock');
      if (result.uniformsFilled > 0) parts.push(result.uniformsFilled + ' uniforms populated');
      status(parts.join(', '));
      // Refresh entries to pick up new tags
      const { fetchIndex } = await import('../api.js');
      const fresh = await fetchIndex();
      entries.length = 0;
      entries.push(...fresh);
      refreshGallery();
    } catch (err) {
      status('Tag scan failed: ' + (err as Error).message, true);
    } finally {
      tagScanBtn.disabled = false;
    }
  });
  row3.appendChild(tagScanBtn);

  const tagEffectsBtn = document.createElement('button');
  tagEffectsBtn.type = 'button';
  tagEffectsBtn.className = 'wire-btn';
  tagEffectsBtn.textContent = 'Tag Effects';
  tagEffectsBtn.title = 'Classify shaders as source or texture-effect based on sampler2D inputs (for Wire effect generation)';
  tagEffectsBtn.style.cssText = 'margin-left:4px;color:#44ffaa';
  tagEffectsBtn.addEventListener('click', async () => {
    tagEffectsBtn.disabled = true;
    status('Classifying shaders as source/effect...');
    try {
      const result = await postWireClassifyEffects();
      status('Classified ' + result.scanned + ' shaders: ' + result.effectsTagged + ' effects, ' + result.sourcesTagged + ' sources');
      const { fetchIndex } = await import('../api.js');
      const fresh = await fetchIndex();
      entries.length = 0;
      entries.push(...fresh);
      refreshGallery();
    } catch (err) {
      status('Tag effects failed: ' + (err as Error).message, true);
    } finally {
      tagEffectsBtn.disabled = false;
    }
  });
  row3.appendChild(tagEffectsBtn);

  add(row3);
}

let refreshGallery: () => void = () => {};
let galleryAutoAdvanceTimerId: ReturnType<typeof setInterval> | null = null;

function tickGalleryAutoAdvance(): void {
  if (!isGalleryActive() || !galleryAutoAdvanceEnabled) return;
  const pageEnts = getPageEntries();
  const totalPages = getTotalPages();
  if (galleryAutoAdvanceMode === 'page') {
    setGalleryPage((galleryPage + 1) % totalPages);
    setGalleryFocusedIndex(0);
  } else {
    if (galleryFocusedIndex >= pageEnts.length - 1) {
      setGalleryPage((galleryPage + 1) % totalPages);
      setGalleryFocusedIndex(0);
    } else {
      setGalleryFocusedIndex(galleryFocusedIndex + 1);
    }
  }
  refreshGallery();
}

function startGalleryAutoAdvanceTimer(): void {
  if (galleryAutoAdvanceTimerId) return;
  galleryAutoAdvanceTimerId = setInterval(() => {
    tickGalleryAutoAdvance();
  }, galleryAutoAdvanceIntervalSec * 1000);
}

function stopGalleryAutoAdvanceTimer(): void {
  if (galleryAutoAdvanceTimerId) {
    clearInterval(galleryAutoAdvanceTimerId);
    galleryAutoAdvanceTimerId = null;
  }
}

function updateGalleryFocusUI(): void {
  const grid = document.getElementById('galleryGrid');
  const sidebar = document.getElementById('gallerySidebar');
  if (grid) {
    grid.querySelectorAll('.gallery-cell').forEach((cell, i) => {
      cell.classList.toggle('focused', i === galleryFocusedIndex);
    });
  }
  if (sidebar) {
    const pageEnts = getPageEntries();
    const focusedEnt = pageEnts[galleryFocusedIndex];
    sidebar.innerHTML = '';
    if (focusedEnt) {
      const h4 = document.createElement('h4');
      h4.textContent = 'Selected';
      sidebar.appendChild(h4);
      const nameDiv = document.createElement('div');
      nameDiv.className = 'gallery-detail-name';
      nameDiv.textContent = focusedEnt.fixedName || focusedEnt.name || focusedEnt.path || '';
      sidebar.appendChild(nameDiv);
      const tagsDiv = document.createElement('div');
      tagsDiv.className = 'gallery-detail-tags';
      tagsDiv.textContent = 'Tags: ' + (focusedEnt.tags || []).join(', ') || 'none';
      sidebar.appendChild(tagsDiv);
      const setsDiv = document.createElement('div');
      setsDiv.className = 'gallery-detail-sets';
      setsDiv.textContent = 'Sets: ' + (focusedEnt.sets || []).join(', ') || 'none';
      sidebar.appendChild(setsDiv);
      const paramsDiv = document.createElement('div');
      paramsDiv.className = 'gallery-detail-params';
      const uniforms = focusedEnt.uniforms || [];
      const pr = (focusedEnt as IndexEntry & { paramRanges?: unknown[] }).paramRanges || [];
      paramsDiv.textContent = 'Parameters: ' + (uniforms.length || pr.length ? (uniforms.join(', ') + (pr.length ? ' (' + pr.length + ' ranges)' : '')) : 'none');
      sidebar.appendChild(paramsDiv);
    }
  }
}

function showGalleryCellContextMenu(ev: MouseEvent, entry: IndexEntry, cellIndex: number): void {
  const path = entry.path || '';
  const pathNorm = (path || '').replace(/\|/g, '\\');
  const pathForApi = (path || '').replace(/\\/g, '|');
  const menu = document.createElement('div');
  menu.className = 'ctx-menu';
  menu.style.cssText = 'position:fixed;background:var(--amiga-panel);border:2px solid var(--amiga-copper);padding:4px 0;z-index:10003;min-width:180px;';
  const addItem = (label: string, fn: () => void) => {
    const item = document.createElement('div');
    item.className = 'ctx-menu-item';
    item.style.cssText = 'padding:6px 12px;cursor:pointer;font-size:12px;color:var(--crt-fg);';
    item.textContent = label;
    item.onmouseenter = () => { item.style.background = 'var(--amiga-surface)'; };
    item.onmouseleave = () => { item.style.background = ''; };
    item.onclick = () => { menu.remove(); document.removeEventListener('click', close); fn(); };
    menu.appendChild(item);
  };
  addItem('Copy path to clipboard', () => {
    navigator.clipboard.writeText(pathNorm).then(() => status('Path copied')).catch(() => status('Copy failed', true));
  });
  addItem('Open in Cursor', () => {
    postOpenInCursor({ path: pathForApi }).then(() => status('Opened in Cursor')).catch((e) => status('Cursor: ' + (e as Error).message, true));
  });
  addItem('Open in file explorer', () => {
    postOpenInExplorer({ path: pathForApi }).then(() => status('Opened in explorer')).catch((e) => status('Explorer: ' + (e as Error).message, true));
  });
  addItem('Open with Notepad', () => {
    postOpenInNotepad({ path: pathForApi }).then(() => status('Opened in Notepad')).catch((e) => status('Notepad: ' + (e as Error).message, true));
  });
  const versionsRow = document.createElement('div');
  versionsRow.className = 'ctx-menu-item';
  versionsRow.style.cssText = 'padding:6px 12px;cursor:pointer;font-size:12px;color:var(--crt-fg);';
  versionsRow.textContent = 'See versions...';
  versionsRow.onmouseenter = () => { versionsRow.style.background = 'var(--amiga-surface)'; };
  versionsRow.onmouseleave = () => { versionsRow.style.background = ''; };
  versionsRow.onclick = (e) => {
    e.stopPropagation();
    versionsRow.textContent = 'Loading...';
    fetchGitLog(pathForApi).then((versions: { sha: string; date: string; subject: string }[]) => {
      menu.innerHTML = '';
      const header = document.createElement('div');
      header.style.cssText = 'padding:6px 12px;font-size:10px;color:var(--amiga-copper);border-bottom:1px solid var(--bevel-dark);text-transform:uppercase;';
      header.textContent = 'Version History (' + versions.length + ' revisions)';
      menu.appendChild(header);
      const back = document.createElement('div');
      back.className = 'ctx-menu-item';
      back.style.cssText = 'padding:6px 12px;cursor:pointer;font-size:12px;color:var(--amiga-copper);border-bottom:1px solid var(--bevel-dark);';
      back.textContent = '< Close';
      back.onclick = () => { menu.remove(); document.removeEventListener('click', close); };
      menu.appendChild(back);
      if (versions.length === 0) {
        const row = document.createElement('div');
        row.style.cssText = 'padding:8px 12px;font-size:11px;color:var(--crt-dim);';
        row.textContent = 'No git history yet.';
        menu.appendChild(row);
      } else {
        versions.slice(0, 20).forEach((v, i) => {
          const d = v.date.slice(0, 10) + ' ' + v.date.slice(11, 19);
          const vRow = document.createElement('div');
          vRow.className = 'ctx-menu-item';
          vRow.style.cssText = 'padding:6px 12px;cursor:pointer;font-size:11px;color:var(--crt-fg);display:flex;gap:6px;align-items:center;';
          const numSpan = document.createElement('span');
          numSpan.style.cssText = 'color:var(--amiga-accent);font-size:10px;min-width:20px;';
          numSpan.textContent = '#' + (versions.length - i);
          const dateSpan = document.createElement('span');
          dateSpan.style.cssText = 'color:var(--crt-dim);font-size:10px;min-width:120px;';
          dateSpan.textContent = d;
          const msgSpan = document.createElement('span');
          msgSpan.style.cssText = 'overflow:hidden;text-overflow:ellipsis;white-space:nowrap;';
          msgSpan.textContent = v.subject.length > 40 ? v.subject.slice(0, 40) + '...' : v.subject;
          vRow.appendChild(numSpan);
          vRow.appendChild(dateSpan);
          vRow.appendChild(msgSpan);
          vRow.onmouseenter = () => { vRow.style.background = 'var(--amiga-surface)'; };
          vRow.onmouseleave = () => { vRow.style.background = ''; };
          vRow.onclick = (ev) => {
            ev.stopPropagation();
            menu.remove();
            document.removeEventListener('click', close);
            postGitRevertVersion({ path: pathForApi, sha: v.sha })
              .then(() => {
                refreshGallery();
                status('Reverted to version #' + (versions.length - i));
              })
              .catch((err) => status('Revert: ' + (err as Error).message, true));
          };
          menu.appendChild(vRow);
        });
      }
    }).catch(() => {
      menu.innerHTML = '';
      const row = document.createElement('div');
      row.style.cssText = 'padding:8px 12px;font-size:11px;color:var(--crt-dim);';
      row.textContent = 'Failed to load version history';
      menu.appendChild(row);
    });
  };
  menu.appendChild(versionsRow);
  menu.style.left = ev.clientX + 'px';
  menu.style.top = ev.clientY + 'px';
  document.body.appendChild(menu);
  clampContextMenuToViewport(menu);
  const close = () => { menu.remove(); document.removeEventListener('click', close); };
  ev.preventDefault();
  setTimeout(() => document.addEventListener('click', close), 0);
}

function toggleTagOnFocused(tag: string): void {
  const pageEnts = getPageEntries();
  const entry = pageEnts[galleryFocusedIndex];
  if (!entry || !tag) return;
  const tags = [...(entry.tags || [])];
  const has = tags.includes(tag);
  const newTags = has ? tags.filter((t) => t !== tag) : [...tags, tag];
  postUpdate({ id: entry.id, tags: newTags }).then(() => {
    const idx = entries.findIndex((e) => e.id === entry.id);
    if (idx >= 0) entries[idx] = { ...entries[idx], tags: newTags };
    updateGalleryFocusUI();
    status(has ? 'Tag "' + tag + '" removed' : 'Tag "' + tag + '" added');
  }).catch((e) => status('Update failed: ' + (e as Error).message, true));
}

function toggleSetOnFocused(setName: string): void {
  const pageEnts = getPageEntries();
  const entry = pageEnts[galleryFocusedIndex];
  if (!entry || !setName) return;
  const sets = [...(entry.sets || [])];
  const has = sets.includes(setName);
  const newSets = has ? sets.filter((s) => s !== setName) : [...sets, setName];
  postUpdate({ id: entry.id, sets: newSets }).then(() => {
    const idx = entries.findIndex((e) => e.id === entry.id);
    if (idx >= 0) entries[idx] = { ...entries[idx], sets: newSets };
    updateGalleryFocusUI();
    status(has ? 'Removed from set "' + setName + '"' : 'Added to set "' + setName + '"');
  }).catch((e) => status('Update failed: ' + (e as Error).message, true));
}

function showToggleSetModal(): void {
  const pageEnts = getPageEntries();
  const entry = pageEnts[galleryFocusedIndex];
  if (!entry) { status('No shader selected', true); return; }
  const existingSets = getAllSetNames();
  const currentSets = entry.sets || [];
  const suggestions = [...new Set([...GALLERY_PRESET_SETS, ...existingSets])];
  const hint = suggestions.map((s, i) => (i < 9 ? (i + 1) + ':' : '') + s + (currentSets.includes(s) ? '✓' : '')).join('  ');
  const name = window.prompt(
    'Toggle set membership for "' + (entry.fixedName || entry.name || '') + '"\n\nCurrent sets: ' +
    (currentSets.length ? currentSets.join(', ') : 'none') +
    '\n\nPreset sets (✓ = already in):\n' + hint +
    '\n\nType set name or number 1-9 for preset:',
    ''
  );
  if (name === null || !name.trim()) return;
  const num = parseInt(name.trim(), 10);
  const resolved = (num >= 1 && num <= 9 && GALLERY_PRESET_SETS[num - 1]) ? GALLERY_PRESET_SETS[num - 1] : name.trim();
  toggleSetOnFocused(resolved);
}

function renameFocused(): void {
  const pageEnts = getPageEntries();
  const entry = pageEnts[galleryFocusedIndex];
  if (!entry) return;
  const current = entry.fixedName || entry.name || entry.path || '';
  const name = window.prompt('Rename (fixed name)', current);
  if (name === null || name.trim() === '') return;
  postUpdate({ id: entry.id, name: name.trim() }).then(() => {
    const idx = entries.findIndex((e) => e.id === entry.id);
    if (idx >= 0) entries[idx] = { ...entries[idx], fixedName: name.trim() };
    updateGalleryFocusUI();
    refreshGallery();
    status('Renamed');
  }).catch((e) => status('Rename failed: ' + (e as Error).message, true));
}

function navigateGallery(delta: number): void {
  const pageEnts = getPageEntries();
  const totalPages = getTotalPages();
  let next = galleryFocusedIndex + delta;
  if (next < 0) {
    if (galleryPage > 0) {
      setGalleryPage(galleryPage - 1);
      setGalleryFocusedIndex(galleryPerPage - 1);
      refreshGallery();
    }
    return;
  }
  if (next >= pageEnts.length) {
    if (galleryPage < totalPages - 1) {
      setGalleryPage(galleryPage + 1);
      setGalleryFocusedIndex(0);
      refreshGallery();
    }
    return;
  }
  setGalleryFocusedIndex(next);
  updateGalleryFocusUI();
}

let shortcutHudVisible = false;
function toggleShortcutHud(): void {
  shortcutHudVisible = !shortcutHudVisible;
  let hud = document.getElementById('galleryShortcutHud');
  if (!shortcutHudVisible) { hud?.remove(); return; }
  if (!hud) {
    hud = document.createElement('div');
    hud.id = 'galleryShortcutHud';
    hud.style.cssText = 'position:absolute;top:8px;right:8px;background:rgba(6,4,15,0.93);border:1px solid var(--amiga-copper);padding:12px 16px;z-index:200;font-size:11px;color:var(--crt-fg);line-height:1.8;pointer-events:none;max-width:320px;';
    hud.innerHTML = `
      <div style="color:var(--amiga-copper);font-size:10px;text-transform:uppercase;margin-bottom:6px;letter-spacing:.1em;">Gallery Shortcuts</div>
      <table style="border-collapse:collapse;width:100%;">
        <tr><td style="color:var(--amiga-accent);padding-right:12px;">← → ↑ ↓</td><td>Navigate cells</td></tr>
        <tr><td style="color:var(--amiga-accent);">Alt + ← →</td><td>Previous / next page</td></tr>
        <tr><td style="color:var(--amiga-accent);">SPACE</td><td>Toggle auto-advance</td></tr>
        <tr><td style="color:var(--amiga-accent);">R</td><td>Rename focused shader</td></tr>
        <tr><td style="color:var(--amiga-accent);">A</td><td>Add/remove from Set (prompt)</td></tr>
        <tr><td style="color:var(--amiga-accent);">F</td><td>Toggle favourite</td></tr>
        <tr><td style="color:var(--amiga-accent);">1 – 9</td><td>Toggle tag (preset tags 1–9)</td></tr>
        <tr><td style="color:var(--amiga-accent);">Shift + 1–9</td><td>Toggle set (preset sets 1–9)</td></tr>
        <tr><td style="color:var(--amiga-accent);">?</td><td>Show / hide this panel</td></tr>
      </table>
      <div style="margin-top:8px;color:var(--crt-dim);font-size:10px;">Preset tags: ${galleryPresetTags.map((t,i)=>(i+1)+':'+t).join('  ')}</div>
      <div style="color:var(--crt-dim);font-size:10px;">Preset sets: ${GALLERY_PRESET_SETS.map((s,i)=>(i+1)+':'+s).join('  ')}</div>
    `;
    const container = document.getElementById('galleryContainer');
    container?.style && Object.assign(container.style, { position: 'relative' });
    container?.appendChild(hud);
  }
}

function toggleFavouriteFocused(): void {
  const pageEnts = getPageEntries();
  const entry = pageEnts[galleryFocusedIndex];
  if (!entry) return;
  const newFav = !entry.favorite;
  postUpdate({ id: entry.id, favorite: newFav }).then(() => {
    const idx = entries.findIndex((e) => e.id === entry.id);
    if (idx >= 0) entries[idx] = { ...entries[idx], favorite: newFav };
    updateGalleryFocusUI();
    status(newFav ? '★ Marked as favourite' : '☆ Removed from favourites');
  }).catch((e) => status('Update failed: ' + (e as Error).message, true));
}

function setupGalleryKeyboard(): void {
  const container = document.getElementById('galleryContainer');
  if (!container) return;
  container.tabIndex = 0;
  container.addEventListener('keydown', (e) => {
    if (!isGalleryActive()) return;
    const target = e.target as HTMLElement;
    if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.tagName === 'SELECT') return;
    switch (e.key) {
      case ' ':
        e.preventDefault();
        setGalleryAutoAdvanceEnabled(!galleryAutoAdvanceEnabled);
        refreshGallery();
        return;
      case 'ArrowRight':
        e.preventDefault();
        if (e.altKey) { const tp = getTotalPages(); setGalleryPage(Math.min(tp - 1, galleryPage + 1)); setGalleryFocusedIndex(0); refreshGallery(); }
        else navigateGallery(1);
        return;
      case 'ArrowLeft':
        e.preventDefault();
        if (e.altKey) { setGalleryPage(Math.max(0, galleryPage - 1)); setGalleryFocusedIndex(0); refreshGallery(); }
        else navigateGallery(-1);
        return;
      case 'ArrowDown':
        e.preventDefault();
        navigateGallery(galleryGridCols);
        return;
      case 'ArrowUp':
        e.preventDefault();
        navigateGallery(-galleryGridCols);
        return;
      case 'r': case 'R':
        e.preventDefault(); renameFocused(); return;
      case 'a': case 'A':
        e.preventDefault(); showToggleSetModal(); return;
      case 'f': case 'F':
        e.preventDefault(); toggleFavouriteFocused(); return;
      case '?':
        e.preventDefault(); toggleShortcutHud(); return;
    }
    const digit = e.key >= '1' && e.key <= '9' ? parseInt(e.key, 10) - 1 : -1;
    if (digit >= 0) {
      e.preventDefault();
      if (e.shiftKey) {
        if (GALLERY_PRESET_SETS[digit]) toggleSetOnFocused(GALLERY_PRESET_SETS[digit]);
      } else {
        if (galleryPresetTags[digit]) toggleTagOnFocused(galleryPresetTags[digit]);
      }
    }
  });
}

function buildGrid(): void {
  const grid = document.getElementById('galleryGrid');
  if (!grid) return;

  disposeCellStates();

  const pageEnts = getPageEntries();
  const w = Math.max(1, Math.floor(galleryResolution * galleryQuality));
  const h = Math.max(1, Math.floor((w / 16) * 9));

  grid.style.display = 'grid';
  grid.style.gridTemplateColumns = `repeat(${galleryGridCols}, 1fr)`;
  grid.style.gridTemplateRows = `repeat(${galleryGridRows}, minmax(0, 1fr))`;

  grid.innerHTML = '';
  for (let i = 0; i < galleryPerPage; i++) {
    const cell = document.createElement('div');
    cell.className = 'gallery-cell';
    cell.dataset.index = String(i);
    const canvas = document.createElement('canvas');
    canvas.width = w;
    canvas.height = h;
    canvas.style.display = 'block';
    canvas.style.width = '100%';
    canvas.style.height = 'auto';
    canvas.style.background = 'var(--amiga-bg)';
    const label = document.createElement('div');
    label.className = 'gallery-cell-label';
    const e = pageEnts[i];
    label.textContent = e ? (e.fixedName || e.name || e.path || ('#' + e.id)) : '';
    cell.appendChild(canvas);
    cell.appendChild(label);
    grid.appendChild(cell);

    cell.addEventListener('click', () => {
      setGalleryFocusedIndex(i);
      updateGalleryFocusUI();
    });
    cell.addEventListener('contextmenu', (ev) => {
      ev.preventDefault();
      if (e) showGalleryCellContextMenu(ev, e, i);
    });

    if (!e?.path) continue;
    const gl = canvas.getContext('webgl', {
      premultipliedAlpha: false,
      preserveDrawingBuffer: true,
      powerPreference: 'low-power'
    });
    if (!gl) continue;

    fetchShader(e.path)
      .then((src) => {
        if (!grid.contains(canvas)) return;
        try {
          const program = createCellProgram(gl, src);
          const buf = gl.createBuffer();
          if (!buf) return;
          gl.bindBuffer(gl.ARRAY_BUFFER, buf);
          gl.bufferData(gl.ARRAY_BUFFER, quadVerts, gl.STATIC_DRAW);
          const defaultTex = createDefaultTex(gl);
          cellStates.set(i, {
            gl,
            program,
            buffer: buf,
            defaultTex,
            startTime: performance.now() / 1000,
            width: w,
            height: h
          });
          if (!galleryRafId) galleryDrawLoop();
        } catch {
          label.textContent = (label.textContent || '') + ' (compile error)';
        }
      })
      .catch(() => {
        label.textContent = (label.textContent || '') + ' (load failed)';
      });
  }

  updateGalleryFocusUI();
}

export function stopGalleryRendering(): void {
  stopGalleryAutoAdvanceTimer();
}

export function initGallery(): void {
  initGalleryStateFromStorage();
  setupGalleryKeyboard();
  refreshGallery = () => {
    buildToolbar();
    buildGrid();
  };
  buildToolbar();
  buildGrid();
  startGalleryAutoAdvanceTimer();
}

export function isGalleryActive(): boolean {
  const tab = document.querySelector('.view-tab[data-view="gallery"].active');
  return !!tab;
}
