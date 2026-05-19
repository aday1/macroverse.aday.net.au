import { el } from '../dom.js';
import { status } from '../dom.js';
import { currentSource, currentPath, currentEntry, appSettings, setCurrentSource, getCursorApiKey } from '../state.js';
import { entries } from '../state.js';
import { oscEngine } from '../engines/osc.js';
import { midiEngine } from '../engines/midi.js';
import { audioEngine, FFT_BAND_LABELS, FFT_BAND_COUNT } from '../engines/audio.js';
import { roliblockManager } from '../engines/roliblock.js';
import type { RoliblockDevice, LedSettings, DeckAssignment, LedDisplayMode, ChannelIsolation } from '../engines/roliblock.js';
import { getMonitorEntries, setMonitorUpdateCallback } from '../engines/midiOscMonitor.js';
import { getSamplerNames, setTextureWebcam, setTextureImage, clearTextureSource, addConstantToSource, addSampler2DToSource, canAddSampler2D, render as doRender, capturePreviewScreenshot, setMouse } from '../render.js';
import { setVjMouseFromRoliblock, setVjMouseForDeckA, setVjMouseForDeckB, setPerDeckMouseEnabled } from './vjDeck.js';
import { setGalleryRoliblockMouse } from './gallery.js';
import { vjController } from '../engines/vjController.js';
import type { IndexEntry } from '../types.js';
import { enhanceRangeInput, sliderColorForKey, updateSliderFill } from '../ui/richSlider.js';

export interface ParamDef {
  id: string;
  type: string;
  label: string;
  min: number;
  max: number;
  step: number;
  def: number | boolean;
}

export let currentParamsMeta: ParamDef[] = [];
export let lastDiscoveredParams: string[] = [];
export let paramToUniformMap: Record<string, string> = {};

const PARAM_LABELS_KEY = 'macroverse-param-labels';
function loadParamLabels(): Record<string, Record<string, string>> {
  try {
    const s = localStorage.getItem(PARAM_LABELS_KEY);
    if (s) return JSON.parse(s) as Record<string, Record<string, string>>;
  } catch (_) {}
  return {};
}
function saveParamLabels(data: Record<string, Record<string, string>>): void {
  try { localStorage.setItem(PARAM_LABELS_KEY, JSON.stringify(data)); } catch (_) {}
}
let _paramLabelsData = loadParamLabels();
export function getParamLabel(path: string | undefined, paramId: string): string | undefined {
  if (!path) return undefined;
  return _paramLabelsData[path]?.[paramId];
}
export function setParamLabel(path: string | undefined, paramId: string, label: string): void {
  if (!path) return;
  if (!_paramLabelsData[path]) _paramLabelsData[path] = {};
  _paramLabelsData[path][paramId] = label || '';
  if (!_paramLabelsData[path][paramId]) delete _paramLabelsData[path][paramId];
  if (Object.keys(_paramLabelsData[path]).length === 0) delete _paramLabelsData[path];
  saveParamLabels(_paramLabelsData);
}

export function setParamToUniformMap(m: Record<string, string>): void {
  paramToUniformMap = m;
}

const BUILTIN_PARAM_IDS = new Set(['timeScale', 'mouseX', 'mouseY']);

export function clearParamsForNewShader(): void {
  for (const k of Object.keys(paramValues)) {
    if (!BUILTIN_PARAM_IDS.has(k)) delete paramValues[k];
  }
  currentParamsMeta = [];
}

export function resetParamToDefault(paramId: string): boolean {
  const meta = currentParamsMeta.find((p) => p.id === paramId);
  if (!meta) return false;
  paramValues[paramId] = meta.def;
  buildParamsPanel(currentEntry);
  return true;
}

export function setLastDiscoveredParams(params: string[]): void {
  lastDiscoveredParams.length = 0;
  lastDiscoveredParams.push(...params);
}

export function applyParamRangesToExposeSource(src: string): string {
  const metaByKey: Record<string, ParamDef> = {};
  for (const p of currentParamsMeta) {
    metaByKey[p.id] = p;
  }
  const lines = (src || '').split('\n');
  const out: string[] = [];
  for (let i = 0; i < lines.length; i++) {
    let line = lines[i];
    if (!/\/\/\s*@expose/.test(line)) {
      out.push(line);
      continue;
    }
    let name: string | null = null;
    const u = line.match(/uniform\s+(?:float|vec2|vec3|vec4|bool)\s+(\w+)\s*;\s*\/\//);
    if (u) name = u[1];
    if (!name) {
      const c = line.match(/(?:const\s+|uniform\s+)?(?:float|vec2|vec3|vec4|bool)\s+(\w+)\s*[=;]/);
      if (c) name = c[1];
    }
    if (!name) {
      const d = line.match(/#define\s+(\w+)\s+/);
      if (d) name = d[1];
    }
    if (!name) {
      const s = line.match(/@expose\s+(\w+)/);
      if (s) name = s[1];
    }
    const meta = name ? metaByKey[name] : null;
    if (meta && meta.type === 'float') {
      const min = meta.min;
      const max = meta.max;
      line = line.replace(/\/\/\s*@expose(?:\s+[\d.-]+\s+[\d.-]+)?\s*$/, '// @expose ' + min + ' ' + max);
    }
    out.push(line);
  }
  return out.join('\n');
}

export function convertGLSLToISF(src: string, description?: string): string {
  const lines = (src || '').split('\n');
  const inputs: Array<Record<string, unknown>> = [];
  const bodyLines: string[] = [];
  const skipBuiltins = new Set(['time', 'resolution', 'mouse', 'iGlobalTime', 'iResolution', 'iMouse', 'iTimeDelta', 'iFrame']);
  for (const line of lines) {
    const precisionMatch = line.match(/^\s*precision\s+(lowp|mediump|highp)\s+float\s*;/);
    if (precisionMatch) continue;
    const samplerMatch = line.match(/^\s*uniform\s+sampler2D\s+(\w+)\s*;/);
    if (samplerMatch) {
      inputs.push({ NAME: samplerMatch[1], TYPE: 'image' });
      continue;
    }
    const exposeMatch = line.match(/^\s*uniform\s+(float|bool)\s+(\w+)\s*;\s*\/\/\s*@expose\s+([\d.e+-]+)\s+([\d.e+-]+)/);
    if (exposeMatch) {
      const type = exposeMatch[1];
      const name = exposeMatch[2];
      if (skipBuiltins.has(name.toLowerCase())) { bodyLines.push(line); continue; }
      const min = parseFloat(exposeMatch[3]);
      const max = parseFloat(exposeMatch[4]);
      const meta = currentParamsMeta.find((p) => p.id === name);
      const def = meta ? (paramValues[name] ?? meta.def) : (min + max) / 2;
      if (type === 'bool') {
        inputs.push({ NAME: name, TYPE: 'bool', DEFAULT: def ? true : false });
      } else {
        inputs.push({ NAME: name, TYPE: 'float', DEFAULT: Number(def), MIN: min, MAX: max });
      }
      continue;
    }
    const uniformMatch = line.match(/^\s*uniform\s+(float|vec2|vec3|vec4|int|bool)\s+(\w+)\s*;/);
    if (uniformMatch && skipBuiltins.has(uniformMatch[2].toLowerCase())) continue;
    bodyLines.push(line);
  }
  let body = bodyLines.join('\n');
  body = body.replace(/\bresolution\b/g, 'RENDERSIZE');
  body = body.replace(/\biGlobalTime\b/g, 'time');
  body = body.replace(/\biResolution\b/g, 'RENDERSIZE');
  body = body.replace(/\biMouse\b/g, 'mouse');
  body = body.replace(/\btexture2D\s*\(\s*(\w+)\s*,/g, 'IMG_NORM_PIXEL($1,');
  const builtInInputs: Array<Record<string, unknown>> = [
    { NAME: 'useFrameIndex', TYPE: 'bool', DEFAULT: false, LABEL: 'Use Frame Index (for Wire sync)' },
    { NAME: 'fps', TYPE: 'float', DEFAULT: 30, MIN: 1, MAX: 120, LABEL: 'FPS (for frame index timing)' },
    { NAME: 'timeScale', TYPE: 'float', DEFAULT: 1.0, MIN: 0, MAX: 4.0, LABEL: 'Time Scale' },
    { NAME: 'mouseX', TYPE: 'float', DEFAULT: 0.5, MIN: 0, MAX: 1 },
    { NAME: 'mouseY', TYPE: 'float', DEFAULT: 0.5, MIN: 0, MAX: 1 },
  ];
  const allInputs = [...builtInInputs, ...inputs];
  const header: Record<string, unknown> = {
    ISFVSN: '2.0',
    DESCRIPTION: description || 'Converted from GLSL',
    INPUTS: allInputs
  };
  const defines = '// useFrameIndex=true (default): FRAMEINDEX drives animation -- reliable in Wire\n' +
    '// useFrameIndex=false: falls back to TIME * timeScale\n' +
    '#define time (useFrameIndex ? float(FRAMEINDEX) / max(fps, 1.0) : TIME * timeScale)\n' +
    '#define mouse vec2(mouseX, mouseY)\n';
  const precision = 'precision mediump float;\n';
  return '/*\n' + JSON.stringify(header, null, '\t') + '\n*/\n\n' + defines + '\n' + precision + body.trim() + '\n';
}

export function syncRoliblockFromView(): void {
  const mgr = roliblockManager;
  const activeView = document.querySelector('.view-tab.active')?.getAttribute('data-view') || '';

  // Linked mode: unified XY across both pads (left pad = X 0–0.5, right pad = X 0.5–1)
  if (mgr.ledDisplayMode === 'linked') {
    for (const dev of mgr.getDevices()) {
      if (!dev.enabled) { dev._onMouse = null; dev._onCrossfader = null; continue; }
      dev._onMouse = (_x, _y) => {
        const linked = mgr.getLinkedMouse(dev);
        if (activeView === 'vj') setVjMouseFromRoliblock(linked.x, linked.y);
        else if (activeView === 'gallery') setGalleryRoliblockMouse(linked.x, linked.y);
        else setMouse(linked.x, linked.y);
      };
      dev._onCrossfader = dev.crossfaderEnabled ? (v) => vjController.dispatch('vj/crossfader', v) : null;
    }
    setPerDeckMouseEnabled(false);
    return;
  }

  for (const dev of mgr.getDevices()) {
    if (!dev.enabled) { dev._onMouse = null; dev._onCrossfader = null; continue; }

    if (activeView === 'vj') {
      if (dev.deckAssignment === 'deckA') {
        dev._onMouse = (x, y) => setVjMouseForDeckA(x, y);
      } else if (dev.deckAssignment === 'deckB') {
        dev._onMouse = (x, y) => setVjMouseForDeckB(x, y);
      } else {
        dev._onMouse = (x, y) => setVjMouseFromRoliblock(x, y);
      }
      dev._onCrossfader = dev.crossfaderEnabled ? (v) => vjController.dispatch('vj/crossfader', v) : null;
    } else if (activeView === 'gallery') {
      dev._onMouse = (x, y) => setGalleryRoliblockMouse(x, y);
      dev._onCrossfader = null;
    } else {
      dev._onMouse = (x, y) => setMouse(x, y);
      dev._onCrossfader = null;
    }
  }
  setPerDeckMouseEnabled(mgr.getActiveDeviceCount() >= 2);
}

export const paramValues: Record<string, number | boolean> = {
  timeScale: 1,
  mouseX: 0.5,
  mouseY: 0.5
};

const PI = 3.141592653589793;
const TAU = 6.283185307179586;

const RANGE_PRESETS: Array<{ label: string; min: number; max: number }> = [
  { label: '0 to 1 (normalized)', min: 0, max: 1 },
  { label: '-1 to 1 (bipolar)', min: -1, max: 1 },
  { label: '0 to TAU (full turn)', min: 0, max: TAU },
  { label: '0 to PI', min: 0, max: PI },
  { label: '-PI to PI', min: -PI, max: PI },
  { label: '0 to 360 (degrees)', min: 0, max: 360 },
  { label: '0 to 10', min: 0, max: 10 },
  { label: '-10 to 10', min: -10, max: 10 },
  { label: '0 to 100', min: 0, max: 100 },
  { label: '-100 to 100', min: -100, max: 100 }
];

const VALUE_PRESETS: Array<{ label: string; value: number }> = [
  { label: '0', value: 0 },
  { label: '0.5', value: 0.5 },
  { label: '1', value: 1 },
  { label: '-1', value: -1 },
  { label: 'PI', value: PI },
  { label: 'TAU', value: TAU },
  { label: 'PI/2', value: PI / 2 },
  { label: '-PI', value: -PI }
];

const CONSTANT_PRESETS: Array<{ name: string; value: number; min: number; max: number }> = [
  { name: 'PI', value: PI, min: 0, max: 10 },
  { name: 'TAU', value: TAU, min: 0, max: 10 },
  { name: 'HALF_PI', value: PI / 2, min: 0, max: 10 },
  { name: 'E', value: Math.E, min: 0, max: 10 },
  { name: 'PHI', value: (1 + Math.sqrt(5)) / 2, min: 0, max: 10 }
];

function parseISF(src: string): { INPUTS?: Array<{ NAME: string; TYPE: string; DEFAULT?: number | boolean; MIN?: number; MAX?: number; LABEL?: string }> } | null {
  const m = src.match(/\/\*\s*(\{[\s\S]*?\})\s*\*\//);
  if (!m) return null;
  try {
    return JSON.parse(m[1]) as { INPUTS?: Array<{ NAME: string; TYPE: string; DEFAULT?: number | boolean; MIN?: number; MAX?: number; LABEL?: string }> };
  } catch {
    return null;
  }
}

export function normalizeISFForWire(content: string): string {
  const blockStart = content.indexOf('/*{');
  if (blockStart === -1) return content;
  const blockEnd = content.indexOf('*/', blockStart);
  if (blockEnd === -1) return content;
  const leading = content.slice(0, blockStart);
  const jsonStr = content.slice(blockStart + 2, blockEnd);
  let afterBlock = content.slice(blockEnd + 2);
  const extraInputs: Array<{ NAME: string; TYPE: string; DEFAULT?: number; MIN?: number; MAX?: number; LABEL?: string }> = [];
  const exposeRe = /^\s*uniform\s+(float|bool)\s+(\w+)\s*;\s*\/\/\s*@expose\s+([\d.e+-]+)\s+([\d.e+-]+)\s*$/gm;
  function collectExpose(text: string): void {
    let mm: RegExpExecArray | null;
    while ((mm = exposeRe.exec(text)) !== null) {
      const type = mm[1].toLowerCase();
      const name = mm[2];
      const min = parseFloat(mm[3]);
      const max = parseFloat(mm[4]);
      const def = (min + max) / 2;
      if (type === 'bool') {
        extraInputs.push({ NAME: name, TYPE: 'bool', DEFAULT: 0, LABEL: name });
      } else {
        extraInputs.push({ NAME: name, TYPE: 'float', DEFAULT: def, MIN: min, MAX: max, LABEL: name });
      }
    }
  }
  collectExpose(leading);
  exposeRe.lastIndex = 0;
  collectExpose(afterBlock);
  afterBlock = afterBlock.replace(/^\s*uniform\s+(float|bool)\s+\w+\s*;\s*\/\/\s*@expose\s+[\d.e+-]+\s+[\d.e+-]+\s*$/gm, '');
  afterBlock = afterBlock.replace(/\n\n\n+/g, '\n\n').trimStart();
  let meta: Record<string, unknown>;
  try {
    meta = JSON.parse(jsonStr) as Record<string, unknown>;
  } catch {
    return content;
  }
  const existingInputs = (meta.INPUTS as Array<{ NAME: string; TYPE?: string; DEFAULT?: number; MIN?: number; MAX?: number; LABEL?: string }>) || [];
  const existingNames = new Set(existingInputs.map((i) => i.NAME));
  for (const inp of extraInputs) {
    if (!existingNames.has(inp.NAME)) {
      existingInputs.push(inp);
      existingNames.add(inp.NAME);
    }
  }
  meta.INPUTS = existingInputs;
  const newBlock = '/*' + JSON.stringify(meta, null, '\t') + '*/';
  const hasPrecision = /precision\s+(lowp|mediump|highp)\s+float\s*;/.test(afterBlock);
  const precisionLine = hasPrecision ? '' : 'precision mediump float;\n';
  return newBlock + '\n\n' + (hasPrecision ? '' : precisionLine) + afterBlock;
}

export interface ExposeItem {
  name: string;
  type: string;
  def: number | boolean;
  min: number;
  max: number;
}

function rangeFromValue(value: number): { min: number; max: number } {
  const margin = value > 1 ? Math.max(1, value * 0.5) : value < 0 ? Math.max(1, -value * 0.5) : 1;
  return {
    min: Math.min(0, value - margin),
    max: Math.max(1, value + margin)
  };
}

function parseDiscoveredParamValue(name: string, src: string): { def: number; min: number; max: number } {
  const esc = name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const constRe = new RegExp('(?:const\\s+)?(?:float|highp\\s+float)\\s+' + esc + '\\s*=\\s*([^;]+);', 'i');
  const defineRe = new RegExp('#define\\s+' + esc + '\\s+([^\\s/]+)', 'i');
  const defaultParamValue = (appSettings as Record<string, unknown>).defaultParamValue as number;
  let def = typeof defaultParamValue === 'number' && !isNaN(defaultParamValue) ? defaultParamValue : 0;
  let min = 0;
  let max = 1;
  const constM = src.match(constRe);
  const defineM = src.match(defineRe);
  const m = constM || defineM;
  if (m) {
    const valStr = m[1].trim();
    const num = parseFloat(valStr);
    if (!isNaN(num)) {
      def = num;
      const r = rangeFromValue(num);
      min = r.min;
      max = r.max;
    }
  }
  return { def, min, max };
}

function parseUniformsFromGLSL(src: string): Array<{ name: string; type: string }> {
  const out: Array<{ name: string; type: string }> = [];
  const skip = new Set(['time', 'mouse', 'resolution', 'TIME', 'RENDERSIZE', 'FRAMEINDEX', 'PASSINDEX', 'mouseX', 'mouseY', 'useFrameIndex', 'fps', 'timeScale']);
  const re = /uniform\s+(float|bool)\s+(\w+)\s*(?:=\s*[^;]+)?\s*;/g;
  let m: RegExpExecArray | null;
  while ((m = re.exec(src)) !== null) {
    const name = m[2];
    if (skip.has(name)) continue;
    out.push({ name, type: m[1].toLowerCase() });
  }
  return out;
}

function parseExposeRangeFromLine(line: string): { min: number; max: number } | null {
  const m = line.match(/@expose\s+([\d.-]+)\s+([\d.-]+)/);
  if (!m) return null;
  const min = parseFloat(m[1]);
  const max = parseFloat(m[2]);
  if (isNaN(min) || isNaN(max)) return null;
  return { min, max };
}

const ISF_BUILTIN_NAMES = new Set(['TIME', 'RENDERSIZE', 'FRAMEINDEX', 'iFrame', 'useFrameIndex', 'fps', 'timeScale', 'mouseX', 'mouseY']);

export function parseExposeFromSource(src: string): ExposeItem[] {
  const defaultDef = typeof (appSettings as Record<string, unknown>).defaultParamValue === 'number' && !isNaN((appSettings as Record<string, unknown>).defaultParamValue as number)
    ? ((appSettings as Record<string, unknown>).defaultParamValue as number) : 0;
  const out: ExposeItem[] = [];
  const seen = new Set<string>();
  const isf = parseISF(src || '');
  if (isf?.INPUTS?.length) {
    for (const inp of isf.INPUTS) {
      const name = inp.NAME;
      if (!name || typeof name !== 'string' || !/^\w+$/.test(name) || ISF_BUILTIN_NAMES.has(name)) continue;
      const t = (inp.TYPE || 'float').toLowerCase();
      if (t === 'image' || t === 'sampler2d') continue;
      const def = inp.DEFAULT !== undefined ? (typeof inp.DEFAULT === 'boolean' ? inp.DEFAULT : Number(inp.DEFAULT)) : defaultDef;
      const min = typeof inp.MIN === 'number' ? inp.MIN : 0;
      const max = typeof inp.MAX === 'number' ? inp.MAX : 1;
      if (!seen.has(name)) {
        seen.add(name);
        out.push({ name, type: t === 'bool' ? 'bool' : 'float', def, min, max });
      }
    }
  }
  const lines = (src || '').split('\n');
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    if (!/\/\/\s*@expose/.test(line)) continue;
    let def: number | boolean = defaultDef;
    let min = 0;
    let max = 1;
    const range = parseExposeRangeFromLine(line);
    if (range) {
      min = range.min;
      max = range.max;
    }
    const uMatch = line.match(/uniform\s+(float|vec2|vec3|vec4|sampler2D|bool)\s+(\w+)\s*;\s*\/\//);
    if (uMatch) {
      const type = uMatch[1].toLowerCase();
      const name = uMatch[2];
      if (seen.has(name)) continue;
      seen.add(name);
      if (type === 'vec4') def = 1 as unknown as boolean;
      else if (type === 'image' || type === 'sampler2d') def = 0;
      else if (type === 'bool') def = false;
      out.push({ name, type, def, min, max });
      continue;
    }
    const defMatch = line.match(/(?:const\s+|uniform\s+)?(float|vec2|vec3|vec4|bool)\s+(\w+)\s*=\s*([^;]+);\s*\/\//);
    if (defMatch) {
      const type = defMatch[1].toLowerCase();
      const name = defMatch[2];
      if (seen.has(name)) continue;
      seen.add(name);
      const valStr = defMatch[3].trim();
      const num = parseFloat(valStr);
      if (!isNaN(num) && !range) {
        def = num;
        const r = rangeFromValue(num);
        min = r.min;
        max = r.max;
      } else if (!isNaN(num)) {
        def = num;
      }
      if (type === 'bool') def = valStr === 'true';
      else if (type === 'vec4') def = 1 as unknown as boolean;
      else if (type === 'vec3' || type === 'vec2') def = num;
      out.push({ name, type, def, min, max });
      continue;
    }
    const defineMatch = line.match(/#define\s+(\w+)\s+([^\s\/]+)\s*\/\//);
    if (defineMatch) {
      const name = defineMatch[1];
      if (seen.has(name)) continue;
      seen.add(name);
      const valStr = defineMatch[2].trim();
      const num = parseFloat(valStr);
      if (!isNaN(num) && !range) {
        def = num;
        const r = rangeFromValue(num);
        min = r.min;
        max = r.max;
      } else if (!isNaN(num)) {
        def = num;
      }
      out.push({ name, type: 'float', def, min, max });
      continue;
    }
    const simpleMatch = line.match(/\/\/\s*@expose\s+(\w+)/);
    if (simpleMatch) {
      const name = simpleMatch[1];
      if (!seen.has(name)) {
        seen.add(name);
        out.push({ name, type: 'float', def: defaultDef, min, max });
      }
    }
  }
  return out;
}

function shaderUsesMouse(src: string): boolean {
  if (!src || !src.length) return false;
  if (/\buniform\s+vec2\s+mouse\s*[;=]/.test(src)) return true;
  return /\bmouse\s*\.\s*[xy]\b|\bmouseX\b|\bmouseY\b/.test(src);
}

export function buildParamsPanel(entry: IndexEntry | null): void {
  const list = el('paramsList');
  if (!list) return;
  list.innerHTML = '';

  const src = currentSource || '';
  const defaultTimeScale = Math.max(0, Math.min(4, (appSettings as Record<string, unknown>).defaultTimeScale as number ?? 1));
  const timeScaleDef = typeof paramValues.timeScale === 'number' ? paramValues.timeScale : defaultTimeScale;
  const baseParams: ParamDef[] = [
    { id: 'timeScale', type: 'float', label: 'Time speed', min: 0, max: 4, step: 0.1, def: timeScaleDef }
  ];
  if (shaderUsesMouse(src)) {
    const mx = typeof paramValues.mouseX === 'number' ? paramValues.mouseX : 0.5;
    const my = typeof paramValues.mouseY === 'number' ? paramValues.mouseY : 0.5;
    baseParams.push(
      { id: 'mouseX', type: 'float', label: 'Mouse X', min: 0, max: 1, step: 0.01, def: mx },
      { id: 'mouseY', type: 'float', label: 'Mouse Y', min: 0, max: 1, step: 0.01, def: my }
    );
  }

  const params: ParamDef[] = [...baseParams];
  const known = new Set(['timeScale', 'mouseX', 'mouseY', 'time', 'mouse', 'resolution']);
  const seen = new Set<string>();

  const isf = currentSource ? parseISF(currentSource) : null;
  const defaultParamValue = (appSettings as Record<string, unknown>).defaultParamValue as number;
  const defNum = typeof defaultParamValue === 'number' && !isNaN(defaultParamValue) ? defaultParamValue : 0;
  if (isf && isf.INPUTS) {
    for (const i of isf.INPUTS) {
      if (known.has(i.NAME) || seen.has(i.NAME)) continue;
      seen.add(i.NAME);
      const type = (i.TYPE || 'float').toLowerCase();
      const min = i.MIN != null ? i.MIN : 0;
      const max = i.MAX != null ? i.MAX : 1;
      let def: number | boolean = type === 'bool' ? false : defNum;
      if (i.NAME in paramValues) {
        const v = paramValues[i.NAME];
        def = typeof v === 'boolean' ? v : Number(v);
      } else if (i.DEFAULT != null) {
        def = type === 'bool' ? !!i.DEFAULT : Number(i.DEFAULT);
      } else if (type === 'float') {
        def = defNum;
      }
      const step = type === 'float' ? 0.01 : 0.1;
      if (type === 'image' || type === 'sampler2d') continue;
      params.push({
        id: i.NAME,
        type: type === 'bool' ? 'bool' : 'float',
        label: i.LABEL || i.NAME,
        min,
        max,
        step,
        def
      });
    }
  }

  const existingMetaById: Record<string, ParamDef> = {};
  for (const p of currentParamsMeta) {
    existingMetaById[p.id] = p;
  }

  function mergeWithExisting(
    id: string,
    type: string,
    label: string,
    candidateDef: number | boolean,
    candidateMin: number,
    candidateMax: number,
    candidateStep: number
  ): ParamDef {
    const existing = existingMetaById[id];
    const hasExistingValue = id in paramValues;
    const existingVal = paramValues[id];
    if (existing) {
      const min = existing.min;
      const max = existing.max;
      const step = existing.step;
      const def = hasExistingValue
        ? (typeof existingVal === 'boolean' ? existingVal : Number(existingVal))
        : (typeof existing.def === 'boolean' ? existing.def : Number(existing.def));
      return { id, type, label, min, max, step, def: type === 'bool' ? !!def : def };
    }
    const def = hasExistingValue
      ? (typeof existingVal === 'boolean' ? existingVal : Number(existingVal))
      : candidateDef;
    let min = candidateMin;
    let max = candidateMax;
    if (hasExistingValue && type === 'float' && typeof existingVal === 'number') {
      const r = rangeFromValue(existingVal);
      if (existingVal < candidateMin || existingVal > candidateMax) {
        min = r.min;
        max = r.max;
      }
    }
    return {
      id,
      type,
      label,
      min,
      max,
      step: candidateStep,
      def: type === 'bool' ? !!def : def
    };
  }

  const exposeItems = currentSource ? parseExposeFromSource(currentSource) : [];
  const exposeIds = new Set(exposeItems.map((e) => e.name));
  for (const e of exposeItems) {
    if (known.has(e.name) || seen.has(e.name)) continue;
    if (e.type === 'image' || e.type === 'sampler2d') continue;
    seen.add(e.name);
    const defNum = typeof e.def === 'boolean' ? (e.def ? 1 : 0) : Number(e.def);
    const merged = mergeWithExisting(
      e.name,
      e.type === 'bool' ? 'bool' : 'float',
      e.name,
      e.type === 'bool' ? e.def : defNum,
      e.min,
      e.max,
      e.type === 'float' ? 0.01 : 0.1
    );
    params.push(merged);
  }

  if (!isf && currentSource) {
    const rawUniforms = parseUniformsFromGLSL(currentSource);
    for (const u of rawUniforms) {
      if (known.has(u.name) || seen.has(u.name)) continue;
      seen.add(u.name);
      const existingVal = paramValues[u.name];
      const candidateDef = u.type === 'bool' ? false : (typeof existingVal === 'number' ? existingVal : defNum);
      const merged = mergeWithExisting(u.name, u.type === 'bool' ? 'bool' : 'float', u.name, candidateDef, 0, 1, u.type === 'float' ? 0.01 : 0.1);
      params.push(merged);
    }
  }

  for (const name of lastDiscoveredParams) {
    if (known.has(name) || seen.has(name)) continue;
    seen.add(name);
    const parsed = parseDiscoveredParamValue(name, currentSource || '');
    const merged = mergeWithExisting(name, 'float', name, parsed.def, parsed.min, parsed.max, 0.01);
    params.push(merged);
  }

  const skipIds = new Set(['timeScale', 'mouseX', 'mouseY']);
  const paramIds = new Set(params.map((p) => p.id));
  for (const k of Object.keys(paramValues)) {
    if (!skipIds.has(k) && !paramIds.has(k)) delete paramValues[k];
  }
  for (const p of params) {
    if (!(p.id in paramValues)) {
      paramValues[p.id] = p.def;
    }
    const assignable = p.type === 'float' || p.type === 'bool';
    const row = document.createElement('div');
    row.className = 'param-row';
    row.dataset.param = p.id;
    const m = assignable ? midiEngine.mappings[p.id] : null;
    const oscAddr = oscEngine.getAddress(p.id);
    let badgeHtml = '';
    let forgetHtml = '';
    if (assignable) {
      badgeHtml = '<span class="midi-badge" data-param="' + escapeAttr(p.id) + '" title="TLDR: MIDI Learn - click then twist a knob">' + (midiEngine.learning === p.id ? '...' : m ? 'CC' + m.cc : 'M') + '</span>';
      if (m) forgetHtml = '<button type="button" class="midi-forget" data-param="' + escapeAttr(p.id) + '" title="TLDR: Remove MIDI mapping">X</button>';
    }
    const oscHtml = assignable ? '<input type="text" class="osc-addr" data-param="' + escapeAttr(p.id) + '" value="' + escapeAttr(oscAddr) + '" title="TLDR: OSC address for this param">' : '';
    const src = currentSource || '';
    const idEsc = p.id.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const useCount = (src.match(new RegExp('\\b' + idEsc + '\\b', 'g')) || []).length;
    const useCountHtml = useCount > 0 ? ' <span class="param-use-count" title="Used ' + useCount + ' times in code">' + useCount + '</span>' : '';
    const removeHtml = exposeIds.has(p.id) ? '<button type="button" class="param-remove" data-param="' + escapeAttr(p.id) + '" title="TLDR: Remove param from Wire (revert to fixed value)">Delete</button>' : '';
    const path = currentPath || (currentEntry && currentEntry.path);
    const displayLabel = (path ? getParamLabel(path, p.id) : undefined) || p.label || p.id;
    const renameHtml = assignable ? '<button type="button" class="param-rename" data-param="' + escapeAttr(p.id) + '" title="TLDR: Rename label for Wire (uniform stays ' + escapeAttr(p.id) + ')">Rename</button>' : '';
    const labelTitle = 'Wire uniform: ' + p.id + (path && getParamLabel(path, p.id) ? ' | Display: ' + displayLabel : '');
    if (p.type === 'bool') {
      const val = !!paramValues[p.id];
      row.innerHTML = '<label title="' + escapeAttr(labelTitle) + '">' + escapeHtml(displayLabel) + useCountHtml + '</label>' + badgeHtml + forgetHtml + renameHtml + removeHtml +
        '<input type="checkbox" data-param="' + escapeAttr(p.id) + '"' + (val ? ' checked' : '') + '>' +
        '<span class="val">' + (val ? '1' : '0') + '</span>' + oscHtml;
      const cb = row.querySelector('input[type="checkbox"]');
      if (cb) {
        cb.onchange = () => {
          paramValues[p.id] = (cb as HTMLInputElement).checked;
          row.querySelector('.val')!.textContent = (cb as HTMLInputElement).checked ? '1' : '0';
        };
      }
    } else {
      const val = Number(paramValues[p.id] ?? p.def);
      const rangeStep = p.step <= 0.01 ? 0.01 : (p.max - p.min) > 10 ? 0.1 : p.step;
      const presetSelectStyle = 'font-size:10px;max-width:85px;padding:1px 2px;background:var(--amiga-bg);border:1px solid var(--bevel-dark);color:var(--crt-fg);';
      let rangePresetHtml = '<select class="param-range-preset" data-param="' + escapeAttr(p.id) + '" title="TLDR: Apply min/max range preset" style="' + presetSelectStyle + '"><option value="">Range...</option>';
      for (const pr of RANGE_PRESETS) {
        rangePresetHtml += '<option value="' + pr.min + ',' + pr.max + '">' + escapeHtml(pr.label) + '</option>';
      }
      rangePresetHtml += '</select>';
      let valuePresetHtml = '<select class="param-value-preset" data-param="' + escapeAttr(p.id) + '" title="TLDR: Set value to GLSL constant (0, 0.5, 1, PI, etc)" style="' + presetSelectStyle + '"><option value="">Value...</option>';
      for (const vp of VALUE_PRESETS) {
        valuePresetHtml += '<option value="' + vp.value + '">' + escapeHtml(vp.label) + '</option>';
      }
      valuePresetHtml += '</select>';
      row.innerHTML = '<label title="' + escapeAttr(labelTitle) + '">' + escapeHtml(displayLabel) + useCountHtml + '</label>' + badgeHtml + forgetHtml + renameHtml + removeHtml +
        '<span class="param-range" title="Min"><input type="number" class="param-min" data-param="' + escapeAttr(p.id) + '" value="' + p.min + '" step="any"></span>' +
        '<span class="param-range" title="Max"><input type="number" class="param-max" data-param="' + escapeAttr(p.id) + '" value="' + p.max + '" step="any"></span>' +
        rangePresetHtml + valuePresetHtml +
        '<input type="range" data-param="' + escapeAttr(p.id) + '" min="' + p.min + '" max="' + p.max + '" step="' + rangeStep + '" value="' + val + '">' +
        '<span class="val">' + val.toFixed(2) + '</span>' + oscHtml;
      const sl = row.querySelector('input[type="range"]') as HTMLInputElement | null;
      const minInp = row.querySelector('.param-min') as HTMLInputElement;
      const maxInp = row.querySelector('.param-max') as HTMLInputElement;
      const valEl = row.querySelector('.val') as HTMLElement;
      const rowLabel = row.querySelector('label') as HTMLElement | null;
      if (rowLabel) rowLabel.style.color = sliderColorForKey(p.id);
      if (sl) {
        enhanceRangeInput(sl, { colorKey: p.id, valueEl: valEl, formatValue: (v) => v.toFixed(2) });
      }
      const applyRange = (newMin: number, newMax: number) => {
        if (newMin >= newMax) return;
        p.min = newMin;
        p.max = newMax;
        const v = Number(paramValues[p.id] ?? p.def);
        const clamped = Math.max(newMin, Math.min(newMax, v));
        paramValues[p.id] = clamped;
        if (minInp) minInp.value = String(newMin);
        if (maxInp) maxInp.value = String(newMax);
        if (sl) {
          sl.min = String(newMin);
          sl.max = String(newMax);
          sl.value = String(clamped);
          updateSliderFill(sl);
        }
        if (valEl) valEl.textContent = clamped.toFixed(2);
      };
      const applyValue = (v: number) => {
        const clamped = Math.max(p.min, Math.min(p.max, v));
        paramValues[p.id] = clamped;
        if (sl) {
          sl.value = String(clamped);
          updateSliderFill(sl);
        }
        if (valEl) valEl.textContent = clamped.toFixed(2);
      };
      if (sl) {
        sl.oninput = () => {
          const v = parseFloat(sl.value);
          paramValues[p.id] = v;
          if (valEl) valEl.textContent = v.toFixed(2);
        };
      }
      if (minInp) {
        minInp.addEventListener('change', () => {
          const newMin = parseFloat(minInp.value);
          const newMax = parseFloat((maxInp && maxInp.value) ? maxInp.value : p.max);
          if (!isNaN(newMin)) applyRange(newMin, isNaN(newMax) ? newMin + 1 : newMax);
        });
      }
      if (maxInp) {
        maxInp.addEventListener('change', () => {
          const newMax = parseFloat(maxInp.value);
          const newMin = parseFloat((minInp && minInp.value) ? minInp.value : p.min);
          if (!isNaN(newMax)) applyRange(isNaN(newMin) ? newMax - 1 : newMin, newMax);
        });
      }
      const rangePresetSel = row.querySelector('.param-range-preset') as HTMLSelectElement;
      if (rangePresetSel) {
        rangePresetSel.addEventListener('change', () => {
          const opt = rangePresetSel.value;
          if (!opt) return;
          const parts = opt.split(',');
          const newMin = parseFloat(parts[0]);
          const newMax = parseFloat(parts[1]);
          if (!isNaN(newMin) && !isNaN(newMax)) applyRange(newMin, newMax);
          rangePresetSel.value = '';
        });
      }
      const valuePresetSel = row.querySelector('.param-value-preset') as HTMLSelectElement;
      if (valuePresetSel) {
        valuePresetSel.addEventListener('change', () => {
          const opt = valuePresetSel.value;
          if (opt === '') return;
          const v = parseFloat(opt);
          if (!isNaN(v)) applyValue(v);
          valuePresetSel.value = '';
        });
      }
    }
    const badge = row.querySelector('.midi-badge');
    if (badge && assignable) {
      badge.addEventListener('click', () => {
        if (midiEngine.learning === p.id) midiEngine.cancelLearn();
        else midiEngine.learn(p.id);
        rebuildExternalMappingUI(currentParamsMeta);
      });
    }
    const forgetBtn = row.querySelector('.midi-forget');
    if (forgetBtn && assignable) {
      forgetBtn.addEventListener('click', () => {
        midiEngine.forget(p.id);
        buildParamsPanel(currentEntry);
      });
    }
    const oscInp = row.querySelector('.osc-addr');
    if (oscInp && assignable) {
      (oscInp as HTMLInputElement).addEventListener('change', () => {
        const v = (oscInp as HTMLInputElement).value.trim();
        if (v && v !== '/shader/' + p.id) oscEngine.setCustomAddress(p.id, v);
        else {
          oscEngine.setCustomAddress(p.id, null);
          (oscInp as HTMLInputElement).value = '/shader/' + p.id;
        }
        oscEngine.buildAddressMap(currentParamsMeta);
      });
    }
    const labelEl = row.querySelector('label');
    if (labelEl) {
      labelEl.classList.add('param-label-find');
      labelEl.title = 'Click to find this parameter in code';
      labelEl.addEventListener('click', (e) => {
        e.stopPropagation();
        const setHighlight = (globalThis as unknown as { setHighlightParamInCode?: (id: string | null) => void }).setHighlightParamInCode;
        if (setHighlight) setHighlight(p.id);
      });
    }
    const removeBtn = row.querySelector('.param-remove');
    if (removeBtn && exposeIds.has(p.id)) {
      removeBtn.addEventListener('click', () => {
        const val = p.type === 'bool' ? !!paramValues[p.id] : Number(paramValues[p.id] ?? p.def);
        const removeExposedParam = (globalThis as unknown as { removeExposedParam?: (name: string, value: number | boolean) => void }).removeExposedParam;
        if (removeExposedParam) removeExposedParam(p.id, val);
      });
    }
    const renameBtn = row.querySelector('.param-rename');
    if (renameBtn && path) {
      renameBtn.addEventListener('click', () => {
        const current = getParamLabel(path, p.id) || p.label || p.id;
        const raw = window.prompt('Rename for Wire display (uniform stays ' + p.id + ')', current);
        if (raw === null) return;
        const trimmed = raw.trim();
        setParamLabel(path, p.id, trimmed);
        buildParamsPanel(currentEntry);
        status(trimmed ? 'Renamed to "' + trimmed + '". Wire uniform: ' + p.id : 'Cleared rename.');
      });
    }
    list.appendChild(row);
  }

  currentParamsMeta = params;
  midiEngine.onApc40Param = (index: number, value: number) => {
    const meta = currentParamsMeta;
    if (meta && meta[index]) midiEngine.pendingValues[meta[index].id] = value;
  };
  const refreshParamDecorations = (globalThis as unknown as { refreshParamDecorations?: () => void }).refreshParamDecorations;
  if (typeof refreshParamDecorations === 'function') refreshParamDecorations();
  const isfNames = isf?.INPUTS ? isf.INPUTS.filter((i: { TYPE?: string }) => !/image|sampler2d/i.test((i.TYPE || ''))).map((i: { NAME: string }) => i.NAME) : [];
  const exposeNames = exposeItems.map((e) => e.name);
  const uniformNames = !isf && currentSource ? parseUniformsFromGLSL(currentSource).map((u) => u.name) : [];
  const summaryEl = el('paramsSummary');
  if (summaryEl) {
    const used = params.map((p) => p.id).join(', ');
    let src: string[] = [];
    if (isfNames.length) src.push(isfNames.length + ' from ISF');
    if (exposeNames.length) src.push(exposeNames.length + ' from @expose');
    if (uniformNames.length) src.push(uniformNames.length + ' from uniforms');
    const srcStr = src.length ? ' (' + src.join(', ') + ')' : '';
    let html = params.length > 0
      ? '<strong>Used:</strong> ' + (used || '(none)') + srcStr
      : '<strong>No parameters.</strong> Open the Expose section below: click Expose params (AI finds sliders) or add constants (PI, TAU). Or use Search for parameters in the code toolbar.';
    if (lastDiscoveredParams.length > 0) {
      html += '<br><strong>Live sliders:</strong> ' + lastDiscoveredParams.join(', ');
    }
    summaryEl.innerHTML = html;
  }
  oscEngine.buildAddressMap(params);
  initExternalSections();
  rebuildExternalMappingUI(params);
  rebuildTextureInputsSection();
}

function rebuildTextureInputsSection(): void {
  const body = el('textureInputsBody');
  if (!body) return;
  const names = getSamplerNames();
  const src = currentSource || '';
  const canAdd = canAddSampler2D(src);
  if (names.length === 0 && !canAdd) {
    body.innerHTML = '<div style="font-size:10px;color:var(--crt-dim)">No sampler2D in current shader. Fragment shaders with gl_FragColor can add one.</div>';
    return;
  }
  body.innerHTML = '';
  if (names.length === 0) {
    const addRow = document.createElement('div');
    addRow.className = 'external-row';
    addRow.style.marginBottom = '8px';
    const addBtn = document.createElement('button');
    addBtn.type = 'button';
    addBtn.className = 'wire-btn';
    addBtn.style.fontSize = '10px';
    addBtn.textContent = 'Add sampler2D (video/image)';
    addBtn.title = 'TLDR: Add sampler2D for webcam/image input';
    addBtn.onclick = () => {
      const newSrc = addSampler2DToSource(src);
      if (!newSrc) { status('Could not add sampler2D', true); return; }
      setCurrentSource(newSrc);
      const sync = (globalThis as unknown as { syncCodeFromState?: () => void }).syncCodeFromState;
      if (typeof sync === 'function') sync();
      doRender(newSrc);
      buildParamsPanel(currentEntry);
      status('Added sampler2D. Use Texture inputs above to set webcam/image. Save to persist.');
    };
    addRow.appendChild(addBtn);
    body.appendChild(addRow);
  } else if (canAdd) {
    const addRow = document.createElement('div');
    addRow.className = 'external-row';
    addRow.style.marginBottom = '8px';
    const addBtn = document.createElement('button');
    addBtn.type = 'button';
    addBtn.className = 'wire-btn';
    addBtn.style.fontSize = '10px';
    addBtn.textContent = 'Add another sampler2D';
    addBtn.title = 'TLDR: Add another sampler2D (up to 4)';
    addBtn.onclick = () => {
      const newSrc = addSampler2DToSource(src);
      if (!newSrc) { status('Could not add sampler2D', true); return; }
      setCurrentSource(newSrc);
      const sync = (globalThis as unknown as { syncCodeFromState?: () => void }).syncCodeFromState;
      if (typeof sync === 'function') sync();
      doRender(newSrc);
      buildParamsPanel(currentEntry);
      status('Added another sampler2D. Save to persist.');
    };
    addRow.appendChild(addBtn);
    body.appendChild(addRow);
  }
  for (const name of names) {
    const row = document.createElement('div');
    row.className = 'external-row';
    row.style.marginBottom = '6px';
    const label = document.createElement('label');
    label.textContent = name;
    label.style.cssText = 'font-size:10px;color:var(--crt-dim);min-width:72px;';
    const webcamBtn = document.createElement('button');
    webcamBtn.type = 'button';
    webcamBtn.className = 'wire-btn';
    webcamBtn.textContent = 'Webcam';
    webcamBtn.title = 'TLDR: Use webcam as texture input';
    webcamBtn.style.fontSize = '10px';
    webcamBtn.onclick = () => setTextureWebcam(name);
    const imageLabel = document.createElement('label');
    imageLabel.className = 'wire-btn';
    imageLabel.style.cssText = 'font-size:10px;padding:4px 8px;cursor:pointer;display:inline-block;';
    imageLabel.textContent = 'Image...';
    imageLabel.title = 'TLDR: Pick image file as texture';
    const fileInp = document.createElement('input');
    fileInp.type = 'file';
    fileInp.accept = 'image/*';
    fileInp.style.display = 'none';
    fileInp.onchange = () => {
      const f = fileInp.files?.[0];
      if (f) setTextureImage(name, f);
      fileInp.value = '';
    };
    imageLabel.onclick = () => fileInp.click();
    const clearBtn = document.createElement('button');
    clearBtn.type = 'button';
    clearBtn.className = 'wire-btn';
    clearBtn.textContent = 'None';
    clearBtn.title = 'TLDR: Clear texture source';
    clearBtn.style.fontSize = '10px';
    clearBtn.onclick = () => clearTextureSource(name);
    row.appendChild(label);
    row.appendChild(webcamBtn);
    row.appendChild(imageLabel);
    row.appendChild(clearBtn);
    row.appendChild(fileInp);
    body.appendChild(row);
  }
}

function initExternalSections(): void {
  const wrap = el('paramsExternalWrap');
  if (!wrap || wrap.dataset.inited === '1') return;
  wrap.dataset.inited = '1';
  oscEngine.loadAddresses();
  midiEngine.loadMappings();
  audioEngine.loadMappings();
  wrap.innerHTML = `
    <div class="external-section">
      <div class="external-header" data-section="osc" title="OSC"><span class="external-toggle">></span><span>OSC</span></div>
      <div class="external-body" data-section="osc">
        <div class="external-row">
          <label style="width:28px;flex-shrink:0">Port</label>
          <input type="number" id="oscPort" value="9000" min="1024" max="65535" style="width:60px;font-size:10px;background:var(--amiga-bg);border:1px solid var(--bevel-dark);color:var(--crt-fg);">
          <button type="button" id="oscStartBtn" class="wire-btn" style="font-size:10px;padding:2px 6px" title="TLDR: Start OSC listener">Listen</button>
          <button type="button" id="oscStopBtn" class="wire-btn" style="font-size:10px;padding:2px 6px;display:none" title="TLDR: Stop OSC">Stop</button>
        </div>
        <div id="oscStatus" style="color:var(--crt-dim)">Not listening</div>
        <div id="oscLastReceived" style="color:var(--crt-dim);font-size:10px;margin-top:2px" title="Last OSC message received">Last: (none)</div>
        <div id="oscAddressList" style="margin-top:4px"></div>
      </div>
    </div>
    <div class="external-section">
      <div class="external-header" data-section="midi" title="MIDI"><span class="external-toggle">></span><span>MIDI</span></div>
      <div class="external-body" data-section="midi">
        <div class="external-row">
          <label style="width:28px;flex-shrink:0">Device</label>
          <select id="midiDevice" style="font-size:10px;background:var(--amiga-bg);border:1px solid var(--bevel-dark);color:var(--crt-fg);">
            <option value="">All inputs</option>
          </select>
          <button type="button" id="midiEnableBtn" class="wire-btn" style="font-size:10px;padding:2px 6px" title="TLDR: Enable MIDI input">Enable</button>
          <button type="button" id="midiDisableBtn" class="wire-btn" style="font-size:10px;padding:2px 6px;display:none" title="TLDR: Disable MIDI">Disable</button>
        </div>
        <div id="midiStatus" style="color:var(--crt-dim)">Disabled</div>
        <div id="midiLastReceived" style="color:var(--crt-dim);font-size:10px;margin-top:2px" title="Last MIDI message received (CC or Note)">Last: (none)</div>
        <div id="midiMappingsList" style="margin-top:4px"></div>
      </div>
    </div>
    <div class="external-section">
      <div class="external-header" data-section="midiOscMonitor" title="Last MIDI and OSC messages received"><span class="external-toggle">></span><span>MIDI / OSC monitor</span><span id="midiOscMonitorIndicator" style="margin-left:6px;width:6px;height:6px;border-radius:50%;background:var(--crt-dim);display:inline-block;vertical-align:middle" title="Receives"></span></div>
      <div class="external-body" data-section="midiOscMonitor" id="midiOscMonitorBody">
        <div id="midiOscMonitorList" style="font-size:10px;color:var(--crt-dim);max-height:200px;overflow-y:auto;font-family:monospace"></div>
      </div>
    </div>
    <div class="external-section">
      <div class="external-header" data-section="audio" title="Audio FFT / Microphone"><span class="external-toggle">v</span><span>Audio FFT</span></div>
      <div class="external-body open" data-section="audio">
        <div class="external-row">
          <label style="width:28px;flex-shrink:0">Device</label>
          <select id="audioDevice" style="font-size:10px;background:var(--amiga-bg);border:1px solid var(--bevel-dark);color:var(--crt-fg);">
            <option value="">Default</option>
          </select>
          <button type="button" id="audioStartBtn" class="wire-btn" style="font-size:10px;padding:2px 6px" title="TLDR: Start audio FFT">Start</button>
          <button type="button" id="audioStopBtn" class="wire-btn" style="font-size:10px;padding:2px 6px;display:none" title="TLDR: Stop audio">Stop</button>
        </div>
        <div class="external-row">
          <label style="width:28px;flex-shrink:0">Gain</label>
          <input type="range" id="audioGain" min="0" max="2" step="0.1" value="1" style="flex:1;min-width:60px">
          <span id="audioGainVal" style="font-size:10px;width:24px;flex-shrink:0">1.0</span>
        </div>
        <canvas id="fftCanvas" width="384" height="48" style="width:100%;height:48px;background:var(--amiga-bg);border:1px solid var(--bevel-dark);display:block;margin-bottom:4px"></canvas>
        <div id="fftBandAssignments" style="margin-top:4px"></div>
      </div>
    </div>
    <div class="external-section">
      <div class="external-header" data-section="texture" title="Sampler2D inputs (webcam or image)"><span class="external-toggle">v</span><span>Texture inputs</span></div>
      <div class="external-body" data-section="texture" id="textureInputsBody">
        <div style="font-size:10px;color:var(--crt-dim)">No sampler2D in current shader.</div>
      </div>
    </div>
    <div class="external-section">
      <div class="external-header" data-section="roliblock" title="Roli Lightpad Block - multi-device, per-deck touch + LEDs"><span class="external-toggle">></span><span>Roliblock</span></div>
      <div class="external-body" data-section="roliblock">
        <div class="external-row">
          <button type="button" id="roliblockRequestBtn" class="wire-btn" style="font-size:10px;padding:2px 6px" title="Request MIDI access (sysex required for LEDs)">Request MIDI</button>
          <button type="button" id="roliblockRefreshBtn" class="wire-btn" style="font-size:10px;padding:2px 6px;display:none" title="Re-scan for MIDI devices">Refresh</button>
          <button type="button" id="roliblockAddDeviceBtn" class="wire-btn" style="font-size:10px;padding:2px 6px;display:none" title="Add another Roliblock device slot">+ Device</button>
          <button type="button" id="roliblockAutoDetectBtn" class="wire-btn" style="font-size:10px;padding:2px 6px;display:none" title="Auto-detect and assign Roli devices">Auto-detect</button>
          <button type="button" id="roliblockPairBleBtn" class="wire-btn" style="font-size:10px;padding:2px 6px;display:none" title="Pair a Roli Lightpad Block via Bluetooth Low Energy">Pair BLE</button>
        </div>
        <div id="roliblockStatus" style="color:var(--crt-dim);font-size:10px">Plug in via USB (Request MIDI) or Bluetooth (Pair BLE). Chrome/Edge only.</div>
        <a href="/roliblock-debug.html" target="_blank" style="font-size:9px;color:var(--amiga-copper);margin-top:4px;display:block">Roliblock DEBUG page (test MIDI + LEDs)</a>
        <div class="external-row" style="margin-top:6px;align-items:center;gap:6px">
          <label style="font-size:9px;color:var(--crt-dim);flex-shrink:0">LED Display:</label>
          <select id="roliblockLedDisplayMode" style="font-size:10px;background:var(--amiga-bg);border:1px solid var(--bevel-dark);color:var(--crt-fg);flex:1">
            <option value="independent">Independent (per deck)</option>
            <option value="stretched">Stretched (30x15 combined)</option>
            <option value="linked">Linked (DNA connected, unified XY)</option>
          </select>
        </div>
        <div id="roliblockDeviceList" style="margin-top:6px"></div>
      </div>
    </div>
    <div class="external-section">
      <div class="external-header" data-section="discover" title="Turn shader numbers into sliders for Wire"><span class="external-toggle">v</span><span>Expose</span></div>
      <div class="external-body open" data-section="discover">
        <p style="font-size:10px;color:var(--crt-dim);margin:0 0 8px;line-height:1.4">Turn hardcoded numbers into live sliders. Works here and in Wire.</p>
        <div style="display:flex;gap:4px;margin-bottom:8px;align-items:center">
          <button type="button" id="exposeParamsPanelBtn" class="wire-btn btn-ai-tokens" style="font-size:10px;padding:4px 10px" title="Uses Cursor API - 15s cooldown"><span class="btn-tokens-icon" aria-hidden="true">&#x1F3AB;&#x1F4B8;</span> Expose params</button>
          <span id="discoverSuggestions" style="font-size:10px;color:var(--crt-dim)"></span>
        </div>
        <div class="vibe-section" style="margin-top:12px;padding-top:8px;border-top:2px solid var(--amiga-copper)">
          <div style="font-size:12px;color:var(--amiga-copper);text-transform:uppercase;margin-bottom:8px;letter-spacing:0.15em;font-weight:bold">Vibe Station</div>
          <div style="display:flex;flex-wrap:wrap;gap:4px;margin-bottom:8px">
            <button type="button" id="vibeReimagineBtn" class="wire-btn btn-ai-tokens" style="font-size:10px;padding:4px 8px" title="Uses Cursor API - 15s cooldown"><span class="btn-tokens-icon" aria-hidden="true">&#x1F3AB;&#x1F4B8;</span> Modify current</button>
            <button type="button" id="vibeNewBtn" class="wire-btn" style="font-size:10px;padding:4px 8px" title="Open cursor-agent terminal in the current shader's directory. Create or modify shaders with AI.">Open Agent</button>
          </div>
          <div style="font-size:10px;color:var(--amiga-copper);text-transform:uppercase;margin:8px 0 4px">Create new shader (AI-generated)</div>
          <input type="text" id="vibeNameInput" placeholder="shader name..." style="width:100%;padding:4px 8px;font-size:11px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);font-family:inherit;margin-bottom:4px;box-sizing:border-box" />
          <div style="font-size:9px;color:var(--crt-dim);margin-bottom:4px">Genre / template:</div>
          <div id="vibeGenreButtons" style="display:flex;flex-wrap:wrap;gap:3px;margin-bottom:6px">
            <button type="button" class="wire-btn vibe-genre-btn" data-genre="particles" style="font-size:9px;padding:2px 6px">Particles</button>
            <button type="button" class="wire-btn vibe-genre-btn" data-genre="fractal" style="font-size:9px;padding:2px 6px">Fractal</button>
            <button type="button" class="wire-btn vibe-genre-btn" data-genre="3d-sphere" style="font-size:9px;padding:2px 6px">3D Sphere</button>
            <button type="button" class="wire-btn vibe-genre-btn" data-genre="3d-cube" style="font-size:9px;padding:2px 6px">3D Cube</button>
            <button type="button" class="wire-btn vibe-genre-btn" data-genre="3d-torus" style="font-size:9px;padding:2px 6px">3D Torus</button>
            <button type="button" class="wire-btn vibe-genre-btn" data-genre="tunnel" style="font-size:9px;padding:2px 6px">Tunnel</button>
            <button type="button" class="wire-btn vibe-genre-btn" data-genre="kaleidoscope" style="font-size:9px;padding:2px 6px">Kaleidoscope</button>
            <button type="button" class="wire-btn vibe-genre-btn" data-genre="audio-reactive" style="font-size:9px;padding:2px 6px">Audio</button>
            <button type="button" class="wire-btn vibe-genre-btn" data-genre="gradient" style="font-size:9px;padding:2px 6px">Gradient</button>
          </div>
          <textarea id="vibeDescInput" placeholder="Describe your vision... e.g. neon wireframe torus with pulsing glow, cyberpunk particle storm, organic flowing noise..." style="width:100%;min-height:50px;padding:6px 8px;font-size:11px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);font-family:inherit;resize:vertical;box-sizing:border-box;margin-bottom:6px"></textarea>
          <button type="button" id="vibeCreateBtn" class="wire-btn btn-ai-tokens" style="font-size:11px;padding:6px 14px;background:var(--amiga-copper);color:var(--amiga-bg);border-color:var(--amiga-copper);font-weight:bold;width:100%" title="Uses Cursor API - 15s cooldown"><span class="btn-tokens-icon" aria-hidden="true">&#x1F3AB;&#x1F4B8;</span> Create shader</button>
          <div id="vibeCreateStatus" style="font-size:10px;color:var(--crt-dim);margin-top:4px;display:none"></div>
        </div>
        <div style="margin-bottom:6px">
          <div style="font-size:10px;color:var(--amiga-copper);text-transform:uppercase;margin-bottom:4px;margin-top:8px;border-top:1px solid var(--bevel-dark);padding-top:6px">Math constants as Wire sliders</div>
          <span style="font-size:10px;color:var(--crt-dim);display:block;margin-bottom:4px">Click to add as a Wire slider. Save to keep.</span>
          <div id="constantButtonsWrap" style="display:flex;flex-wrap:wrap;gap:4px;margin-top:4px"></div>
        </div>
      </div>
    </div>
  `;
  const constantWrap = el('constantButtonsWrap');
  if (constantWrap) {
    for (const preset of CONSTANT_PRESETS) {
      const btn = document.createElement('button');
      btn.type = 'button';
      btn.className = 'wire-btn';
      btn.style.fontSize = '10px';
      btn.style.padding = '2px 6px';
      btn.textContent = preset.name;
      btn.title = 'TLDR: Add ' + preset.name + ' as Wire input (default ' + preset.value + ')';
      btn.addEventListener('click', () => {
        const src = currentSource || '';
        if (!src || !src.includes('void main')) {
          status('Load a shader first (select one from the list)', true);
          return;
        }
        const newSrc = addConstantToSource(src, preset.name, preset.value, preset.min, preset.max);
        if (newSrc === src) {
          status(preset.name + ' already in shader. Adjust its slider above, or check code view.');
          return;
        }
        setCurrentSource(newSrc);
        paramValues[preset.name] = preset.value;
        const sync = (globalThis as unknown as { syncCodeFromState?: () => void }).syncCodeFromState;
        if (typeof sync === 'function') sync();
        doRender(newSrc);
        buildParamsPanel(currentEntry);
        status('Added ' + preset.name + ' as Wire input. Save to persist.');
      });
      constantWrap.appendChild(btn);
    }
  }
  wrap.querySelectorAll('.external-header').forEach((hdr) => {
    const section = (hdr as HTMLElement).dataset.section;
    const body = wrap.querySelector('.external-body[data-section="' + section + '"]');
    const toggle = hdr.querySelector('.external-toggle');
    if (!body) return;
    hdr.addEventListener('click', () => {
      const open = (body as HTMLElement).classList.contains('open');
      (body as HTMLElement).classList.toggle('open', !open);
      if (toggle) toggle.textContent = open ? '>' : 'v';
    });
  });

  const midiOscMonitorList = el('midiOscMonitorList');
  const midiOscMonitorIndicator = el('midiOscMonitorIndicator');
  function renderMonitorList(): void {
    if (!midiOscMonitorList) return;
    const entries = getMonitorEntries();
    midiOscMonitorList.innerHTML = '';
    if (entries.length === 0) {
      const p = document.createElement('div');
      p.style.color = 'var(--crt-dim)';
      p.textContent = 'No messages yet. Enable MIDI or start OSC to see activity.';
      midiOscMonitorList.appendChild(p);
      return;
    }
    const maxShow = 15;
    for (let i = 0; i < Math.min(entries.length, maxShow); i++) {
      const e = entries[i];
      const row = document.createElement('div');
      row.style.cssText = 'padding:2px 0;border-bottom:1px solid var(--bevel-dark);display:flex;align-items:baseline;gap:6px;flex-wrap:wrap';
      const typeSpan = document.createElement('span');
      typeSpan.style.cssText = 'flex-shrink:0;font-weight:bold;width:32px';
      typeSpan.textContent = e.type.toUpperCase();
      typeSpan.style.color = e.type === 'midi' ? 'var(--amiga-copper)' : '#6a9';
      row.appendChild(typeSpan);
      if (e.device) {
        const devSpan = document.createElement('span');
        devSpan.style.cssText = 'flex-shrink:0;max-width:120px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap';
        devSpan.textContent = e.device;
        devSpan.title = e.device;
        devSpan.style.color = 'var(--crt-dim)';
        row.appendChild(devSpan);
      }
      const textSpan = document.createElement('span');
      textSpan.textContent = e.text;
      textSpan.style.flex = '1';
      textSpan.style.minWidth = '0';
      row.appendChild(textSpan);
      midiOscMonitorList.appendChild(row);
    }
    if (midiOscMonitorIndicator) {
      midiOscMonitorIndicator.style.background = 'var(--amiga-copper)';
      clearTimeout((midiOscMonitorIndicator as HTMLElement & { _blinkTimeout?: number })._blinkTimeout);
      (midiOscMonitorIndicator as HTMLElement & { _blinkTimeout?: number })._blinkTimeout = window.setTimeout(() => {
        if (midiOscMonitorIndicator) midiOscMonitorIndicator.style.background = 'var(--crt-dim)';
      }, 400);
    }
  }
  renderMonitorList();
  setMonitorUpdateCallback(renderMonitorList);

  const agentOutputCopy = el('agentOutputCopy');
  const agentOutputPrint = el('agentOutputPrint');
  const agentOutputActions = el('agentOutputActions');
  if (agentOutputCopy) {
    agentOutputCopy.addEventListener('click', () => {
      const out = el('agentOutput');
      const text = out?.textContent || '';
      if (!text) return;
      navigator.clipboard.writeText(text).then(() => status('Agent output copied to clipboard')).catch(() => status('Copy failed', true));
    });
  }
  if (agentOutputPrint) {
    agentOutputPrint.addEventListener('click', () => {
      const out = el('agentOutput');
      const text = out?.textContent || '';
      if (!text) return;
      const w = window.open('', '_blank', 'width=700,height=500');
      if (!w) { status('Pop-up blocked; allow to print', true); return; }
      w.document.write('<pre style="white-space:pre-wrap;font-family:monospace;font-size:12px;padding:16px">' + text.replace(/</g, '&lt;').replace(/>/g, '&gt;') + '</pre>');
      w.document.close();
      w.focus();
      setTimeout(() => { w.print(); w.close(); }, 250);
    });
  }
  const oscPort = el('oscPort');
  const oscStartBtn = el('oscStartBtn');
  const oscStopBtn = el('oscStopBtn');
  const oscStatus = el('oscStatus');
  if (oscStartBtn) {
    oscStartBtn.addEventListener('click', async () => {
      const port = parseInt((oscPort as HTMLInputElement)?.value || '9000', 10);
      try {
        await oscEngine.start(port);
        (oscStartBtn as HTMLElement).style.display = 'none';
        (oscStopBtn as HTMLElement).style.display = '';
        if (oscStatus) oscStatus.textContent = 'Listening on port ' + port;
      } catch (e) {
        status('OSC: ' + (e as Error).message, true);
      }
    });
  }
  if (oscStopBtn) {
    oscStopBtn.addEventListener('click', async () => {
      await oscEngine.stop();
      (oscStartBtn as HTMLElement).style.display = '';
      (oscStopBtn as HTMLElement).style.display = 'none';
      if (oscStatus) oscStatus.textContent = 'Not listening';
      const oscLast = document.getElementById('oscLastReceived');
      if (oscLast) oscLast.textContent = 'Last: (none)';
    });
  }
  oscEngine.onSelectShader = (idx: number) => {
    if (entries && idx >= 0 && idx < entries.length) {
      const entry = entries[idx];
      if (entry) import('../render.js').then((m) => m.loadShader(entry)).catch(() => {});
    }
  };
  const midiDevice = el('midiDevice');
  const midiEnableBtn = el('midiEnableBtn');
  const midiDisableBtn = el('midiDisableBtn');
  const midiStatus = el('midiStatus');
  const updateMidiStatusLabel = () => {
    if (!midiStatus) return;
    if (!midiEngine.active) { midiStatus.textContent = 'Disabled'; return; }
    if (!midiEngine.inputs.length) { midiStatus.textContent = 'No devices found'; return; }
    const id = midiEngine.selectedInputId;
    if (!id) { midiStatus.textContent = 'Enabled (all inputs)'; return; }
    const entry = midiEngine.inputs.find((i) => i.id === id);
    midiStatus.textContent = 'Enabled - ' + (entry ? entry.name || id : id);
  };
  if (midiEnableBtn) {
    midiEnableBtn.addEventListener('click', async () => {
      try {
        await midiEngine.start();
        midiEngine.refreshInputs();
        const sel = midiDevice as HTMLSelectElement | null;
        if (sel) {
          sel.innerHTML = '<option value="">All inputs</option>';
          for (const { id, name } of midiEngine.inputs) {
            const opt = document.createElement('option');
            opt.value = id;
            opt.textContent = name || id;
            sel.appendChild(opt);
          }
        }
        (midiEnableBtn as HTMLElement).style.display = 'none';
        (midiDisableBtn as HTMLElement).style.display = '';
        updateMidiStatusLabel();
      } catch (e) {
        status('MIDI: ' + (e as Error).message, true);
      }
    });
  }
  if (midiDisableBtn) {
    midiDisableBtn.addEventListener('click', () => {
      midiEngine.stop();
      (midiEnableBtn as HTMLElement).style.display = '';
      (midiDisableBtn as HTMLElement).style.display = 'none';
      if (midiStatus) midiStatus.textContent = 'Disabled';
      midiEngine.setLastReceivedLabel('Last: (none)');
    });
  }
  if (midiDevice) {
    (midiDevice as HTMLSelectElement).addEventListener('change', () => {
      const v = (midiDevice as HTMLSelectElement).value;
      midiEngine.selectedInputId = v || null;
      updateMidiStatusLabel();
    });
  }
  midiEngine.onLearnComplete = () => {
    rebuildExternalMappingUI(currentParamsMeta);
    buildParamsPanel(currentEntry);
  };

  const roliblockRequestBtn = el('roliblockRequestBtn');
  const roliblockRefreshBtn = el('roliblockRefreshBtn');
  const roliblockAddDeviceBtn = el('roliblockAddDeviceBtn');
  const roliblockAutoDetectBtn = el('roliblockAutoDetectBtn');
  const roliblockPairBleBtn = el('roliblockPairBleBtn');
  const roliblockStatus = el('roliblockStatus');
  const roliblockDeviceList = el('roliblockDeviceList');
  const roliblockLedDisplayMode = el('roliblockLedDisplayMode') as HTMLSelectElement | null;

  // Show Pair BLE button immediately if Web Bluetooth is available
  if (roliblockPairBleBtn && typeof navigator !== 'undefined' && navigator.bluetooth) {
    (roliblockPairBleBtn as HTMLElement).style.display = '';
  }

  function renderDeviceSlotHtml(dev: RoliblockDevice, mgr: typeof roliblockManager): string {
    const inputs = mgr.inputs;
    const outputs = mgr.outputs;
    const s = dev.ledSettings;
    let html = '<div class="roliblock-device" data-device-id="' + dev.id + '" style="border:1px solid var(--bevel-dark);padding:6px;margin-bottom:6px;background:var(--amiga-surface)">';
    html += '<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:4px">';
    const connBadge = dev.connectionType === 'ble' ? ' <span style="font-size:8px;color:var(--amiga-accent)">BLE</span>' : '';
    html += '<span style="font-size:10px;color:var(--amiga-copper);font-weight:bold">' + dev.label + connBadge + '</span>';
    html += '<button type="button" class="wire-btn roli-dev-remove" data-dev-id="' + dev.id + '" style="font-size:9px;padding:1px 4px" title="Remove device">x</button>';
    html += '</div>';
    if (dev.connectionType === 'ble') {
      // BLE devices use a single connection for both input and output
      const bleName = dev.bleConnection?.device?.name || dev.bleDeviceId || 'Bluetooth';
      const bleState = dev.bleConnection ? dev.bleConnection.connectionState : 'disconnected';
      html += '<div style="font-size:9px;color:var(--crt-dim);padding:2px 0">Connected via Bluetooth' + (bleState !== 'connected' ? ' <span style="color:var(--amiga-copper)">(' + bleState + ')</span>' : '') + '</div>';
    } else {
      // Touch input
      html += '<div class="external-row" style="align-items:center;gap:4px">';
      html += '<label style="width:36px;flex-shrink:0;font-size:9px;color:var(--crt-dim)">Touch</label>';
      html += '<select class="roli-dev-input" data-dev-id="' + dev.id + '" style="font-size:10px;background:var(--amiga-bg);border:1px solid var(--bevel-dark);color:var(--crt-fg);flex:1">';
      html += '<option value="">-- select --</option>';
      for (const i of inputs) {
        html += '<option value="' + i.id + '"' + (i.id === dev.midiInputId ? ' selected' : '') + '>' + i.name + '</option>';
      }
      html += '</select></div>';
      // LED output
      html += '<div class="external-row" style="align-items:center;gap:4px">';
      html += '<label style="width:36px;flex-shrink:0;font-size:9px;color:var(--crt-dim)">LEDs</label>';
      html += '<select class="roli-dev-output" data-dev-id="' + dev.id + '" style="font-size:10px;background:var(--amiga-bg);border:1px solid var(--bevel-dark);color:var(--crt-fg);flex:1">';
      html += '<option value="">-- select --</option>';
      for (const o of outputs) {
        html += '<option value="' + o.id + '"' + (o.id === dev.midiOutputId ? ' selected' : '') + '>' + o.name + '</option>';
      }
      html += '</select></div>';
    }
    // Deck assignment
    html += '<div class="external-row" style="align-items:center;gap:4px;margin-top:4px">';
    html += '<label style="width:36px;flex-shrink:0;font-size:9px;color:var(--crt-dim)">Deck</label>';
    html += '<select class="roli-dev-deck" data-dev-id="' + dev.id + '" style="font-size:10px;background:var(--amiga-bg);border:1px solid var(--bevel-dark);color:var(--crt-fg);flex:1">';
    const deckOpts: { v: DeckAssignment; l: string }[] = [
      { v: 'auto', l: 'Auto' }, { v: 'deckA', l: 'Deck A' }, { v: 'deckB', l: 'Deck B' }, { v: 'shared', l: 'Shared (Mouse X/Y)' }
    ];
    for (const d of deckOpts) {
      html += '<option value="' + d.v + '"' + (dev.deckAssignment === d.v ? ' selected' : '') + '>' + d.l + '</option>';
    }
    html += '</select></div>';
    // Crossfader checkbox
    html += '<div class="external-row" style="margin-top:3px">';
    html += '<label style="display:flex;align-items:center;gap:4px;font-size:9px;color:var(--crt-dim);cursor:pointer">';
    html += '<input type="checkbox" class="roli-dev-crossfader" data-dev-id="' + dev.id + '"' + (dev.crossfaderEnabled ? ' checked' : '') + '>';
    html += '<span>Crossfader CC 73</span></label></div>';
    // Enable/Disable
    html += '<div class="external-row" style="margin-top:4px;gap:4px">';
    html += '<button type="button" class="wire-btn roli-dev-enable" data-dev-id="' + dev.id + '" style="font-size:9px;padding:2px 6px;' + (dev.enabled ? 'display:none' : '') + '">Enable</button>';
    html += '<button type="button" class="wire-btn roli-dev-disable" data-dev-id="' + dev.id + '" style="font-size:9px;padding:2px 6px;' + (!dev.enabled ? 'display:none' : '') + '">Disable</button>';
    html += '<span class="roli-dev-status" data-dev-id="' + dev.id + '" style="font-size:9px;color:var(--crt-dim)">' + (dev.enabled ? (dev.isReady() ? 'Active' : 'Handshaking...') : 'Disabled') + '</span>';
    html += '</div>';
    // LED Processing
    html += '<div style="margin-top:6px;border-top:1px solid var(--bevel-dark);padding-top:4px">';
    html += '<div style="font-size:9px;color:var(--amiga-copper);text-transform:uppercase;margin-bottom:3px;letter-spacing:0.1em">LED Processing</div>';
    const sliderDefs = [
      { key: 'contrast', label: 'Contrast', min: 0, max: 3, step: 0.05, val: s.contrast },
      { key: 'brightness', label: 'Bright', min: -1, max: 1, step: 0.05, val: s.brightness },
      { key: 'saturation', label: 'Saturat', min: 0, max: 3, step: 0.05, val: s.saturation },
      { key: 'gamma', label: 'Gamma', min: 0.2, max: 3, step: 0.05, val: s.gamma },
      { key: 'posterize', label: 'Poster', min: 0, max: 16, step: 1, val: s.posterize }
    ];
    for (const sl of sliderDefs) {
      const dispVal = sl.key === 'posterize' ? (sl.val === 0 ? 'Off' : String(sl.val)) : sl.val.toFixed(sl.val === Math.round(sl.val) ? 1 : 2);
      html += '<div class="external-row" style="align-items:center;gap:4px">';
      html += '<label style="width:48px;flex-shrink:0;font-size:9px;color:var(--crt-dim)">' + sl.label + '</label>';
      html += '<input type="range" class="roli-dev-led-slider" data-dev-id="' + dev.id + '" data-led-key="' + sl.key + '" min="' + sl.min + '" max="' + sl.max + '" step="' + sl.step + '" value="' + sl.val + '" style="flex:1;height:12px">';
      html += '<span class="roli-dev-led-val" data-dev-id="' + dev.id + '" data-led-key="' + sl.key + '" style="font-size:9px;color:var(--crt-dim);width:28px;text-align:right">' + dispVal + '</span>';
      html += '</div>';
    }
    // Checkboxes: grayscale, invert
    html += '<div class="external-row" style="margin-top:3px;gap:8px">';
    html += '<label style="display:flex;align-items:center;gap:3px;font-size:9px;color:var(--crt-dim);cursor:pointer">';
    html += '<input type="checkbox" class="roli-dev-led-bool" data-dev-id="' + dev.id + '" data-led-key="grayscale"' + (s.grayscale ? ' checked' : '') + '>';
    html += '<span>Gray</span></label>';
    html += '<label style="display:flex;align-items:center;gap:3px;font-size:9px;color:var(--crt-dim);cursor:pointer">';
    html += '<input type="checkbox" class="roli-dev-led-bool" data-dev-id="' + dev.id + '" data-led-key="invert"' + (s.invert ? ' checked' : '') + '>';
    html += '<span>Invert</span></label>';
    html += '</div>';
    // Channel isolation radio
    html += '<div class="external-row" style="margin-top:3px;gap:6px">';
    html += '<span style="font-size:9px;color:var(--crt-dim)">Ch:</span>';
    const chOpts: { v: ChannelIsolation; l: string }[] = [
      { v: 'all', l: 'All' }, { v: 'r', l: 'R' }, { v: 'g', l: 'G' }, { v: 'b', l: 'B' }
    ];
    for (const ch of chOpts) {
      html += '<label style="display:flex;align-items:center;gap:2px;font-size:9px;color:var(--crt-dim);cursor:pointer">';
      html += '<input type="radio" name="roliCh_' + dev.id + '" class="roli-dev-channel" data-dev-id="' + dev.id + '" value="' + ch.v + '"' + (s.channelIsolation === ch.v ? ' checked' : '') + '>';
      html += '<span>' + ch.l + '</span></label>';
    }
    html += '</div>';
    html += '</div>'; // end LED Processing
    html += '</div>'; // end device slot
    return html;
  }

  function renderRoliblockDeviceList(): void {
    if (!roliblockDeviceList) return;
    const mgr = roliblockManager;
    const devices = mgr.getDevices();
    if (devices.length === 0) {
      roliblockDeviceList.innerHTML = '<div style="font-size:9px;color:var(--crt-dim)">No devices. Click + Device or Auto-detect.</div>';
      return;
    }
    let html = '';
    for (const dev of devices) html += renderDeviceSlotHtml(dev, mgr);
    roliblockDeviceList.innerHTML = html;

    // Bind events for dynamically rendered device slots
    roliblockDeviceList.querySelectorAll('.roli-dev-remove').forEach((btn) => {
      btn.addEventListener('click', () => {
        const id = (btn as HTMLElement).dataset.devId!;
        mgr.removeDevice(id);
        renderRoliblockDeviceList();
        syncRoliblockFromView();
        updateRoliblockStatus();
      });
    });
    roliblockDeviceList.querySelectorAll('.roli-dev-input').forEach((sel) => {
      sel.addEventListener('change', () => {
        const id = (sel as HTMLElement).dataset.devId!;
        const dev = mgr.getDevice(id);
        if (dev) { dev.connectInput((sel as HTMLSelectElement).value); mgr.saveAll(); updateRoliblockStatus(); }
      });
    });
    roliblockDeviceList.querySelectorAll('.roli-dev-output').forEach((sel) => {
      sel.addEventListener('change', () => {
        const id = (sel as HTMLElement).dataset.devId!;
        const dev = mgr.getDevice(id);
        if (dev) { dev.connectOutput((sel as HTMLSelectElement).value); mgr.saveAll(); updateRoliblockStatus(); }
      });
    });
    roliblockDeviceList.querySelectorAll('.roli-dev-deck').forEach((sel) => {
      sel.addEventListener('change', () => {
        const id = (sel as HTMLElement).dataset.devId!;
        const dev = mgr.getDevice(id);
        if (dev) { dev.deckAssignment = (sel as HTMLSelectElement).value as DeckAssignment; mgr.saveAll(); syncRoliblockFromView(); }
      });
    });
    roliblockDeviceList.querySelectorAll('.roli-dev-crossfader').forEach((cb) => {
      cb.addEventListener('change', () => {
        const id = (cb as HTMLElement).dataset.devId!;
        const dev = mgr.getDevice(id);
        if (dev) { dev.crossfaderEnabled = (cb as HTMLInputElement).checked; mgr.saveAll(); syncRoliblockFromView(); }
      });
    });
    roliblockDeviceList.querySelectorAll('.roli-dev-enable').forEach((btn) => {
      btn.addEventListener('click', () => {
        const id = (btn as HTMLElement).dataset.devId!;
        const dev = mgr.getDevice(id);
        if (dev) {
          dev.enable();
          syncRoliblockFromView();
          renderRoliblockDeviceList();
          updateRoliblockStatus();
        }
      });
    });
    roliblockDeviceList.querySelectorAll('.roli-dev-disable').forEach((btn) => {
      btn.addEventListener('click', () => {
        const id = (btn as HTMLElement).dataset.devId!;
        const dev = mgr.getDevice(id);
        if (dev) {
          dev.disable();
          syncRoliblockFromView();
          renderRoliblockDeviceList();
          updateRoliblockStatus();
        }
      });
    });
    roliblockDeviceList.querySelectorAll('.roli-dev-led-slider').forEach((slider) => {
      const inp = slider as HTMLInputElement;
      const id = inp.dataset.devId!;
      const key = inp.dataset.ledKey!;
      const valSpan = roliblockDeviceList!.querySelector(
        '.roli-dev-led-val[data-dev-id="' + id + '"][data-led-key="' + key + '"]'
      ) as HTMLElement | null;
      const ledLabel = inp.closest('.external-row')?.querySelector('label') as HTMLElement | null;
      const colorKey = 'roli-' + key;
      if (ledLabel) ledLabel.style.color = sliderColorForKey(colorKey);
      enhanceRangeInput(inp, {
        colorKey,
        valueEl: valSpan,
        formatValue: (v) => (key === 'posterize' ? (v === 0 ? 'Off' : String(v)) : v.toFixed(v === Math.round(v) ? 1 : 2))
      });
      inp.addEventListener('input', () => {
        const dev = mgr.getDevice(id);
        if (!dev) return;
        const v = parseFloat(inp.value);
        (dev.ledSettings as Record<string, number | boolean | string>)[key] = v;
        mgr.saveAll();
        if (valSpan) valSpan.textContent = key === 'posterize' ? (v === 0 ? 'Off' : String(v)) : v.toFixed(v === Math.round(v) ? 1 : 2);
      });
    });
    roliblockDeviceList.querySelectorAll('.roli-dev-led-bool').forEach((cb) => {
      cb.addEventListener('change', () => {
        const id = (cb as HTMLElement).dataset.devId!;
        const key = (cb as HTMLElement).dataset.ledKey!;
        const dev = mgr.getDevice(id);
        if (dev) { (dev.ledSettings as Record<string, number | boolean | string>)[key] = (cb as HTMLInputElement).checked; mgr.saveAll(); }
      });
    });
    roliblockDeviceList.querySelectorAll('.roli-dev-channel').forEach((radio) => {
      radio.addEventListener('change', () => {
        const id = (radio as HTMLElement).dataset.devId!;
        const dev = mgr.getDevice(id);
        if (dev) { dev.ledSettings.channelIsolation = (radio as HTMLInputElement).value as ChannelIsolation; mgr.saveAll(); }
      });
    });
  }

  function updateRoliblockStatus(): void {
    if (!roliblockStatus) return;
    const mgr = roliblockManager;
    const count = mgr.getActiveDeviceCount();
    const total = mgr.getDevices().length;
    if (total === 0) {
      roliblockStatus.textContent = 'Request MIDI, add devices, then Enable.';
    } else if (count === 0) {
      roliblockStatus.textContent = total + ' device(s) configured. Enable to activate.';
    } else {
      roliblockStatus.textContent = count + '/' + total + ' device(s) active.';
    }
  }

  async function doRoliblockRequest(): Promise<void> {
    const ok = await roliblockManager.requestAccess();
    if (!ok) {
      status('Roliblock: Web MIDI or sysex not supported. Use Chrome/Edge.', true);
      return;
    }
    roliblockManager.reconnectSaved();
    roliblockManager.autoDetectDevices();
    (roliblockRequestBtn as HTMLElement).style.display = 'none';
    (roliblockRefreshBtn as HTMLElement).style.display = '';
    (roliblockAddDeviceBtn as HTMLElement).style.display = '';
    (roliblockAutoDetectBtn as HTMLElement).style.display = '';
    renderRoliblockDeviceList();
    updateRoliblockStatus();
    const mgr = roliblockManager;
    if (mgr.inputs.length === 0 && mgr.outputs.length === 0) {
      status('Roliblock: No MIDI devices. Plug in Roli, allow when prompted, use Chrome/Edge.', true);
    } else {
      status('Roliblock: ' + mgr.inputs.length + ' in, ' + mgr.outputs.length + ' out. ' + mgr.getDevices().length + ' device(s).');
    }
  }

  if (roliblockRequestBtn) {
    roliblockRequestBtn.addEventListener('click', doRoliblockRequest);
  }
  if (roliblockRefreshBtn) {
    roliblockRefreshBtn.addEventListener('click', () => {
      roliblockManager.refreshDeviceList();
      renderRoliblockDeviceList();
      updateRoliblockStatus();
      status('Roliblock: ' + roliblockManager.inputs.length + ' in, ' + roliblockManager.outputs.length + ' out.');
    });
  }
  if (roliblockAddDeviceBtn) {
    roliblockAddDeviceBtn.addEventListener('click', () => {
      roliblockManager.addDevice();
      renderRoliblockDeviceList();
      updateRoliblockStatus();
    });
  }
  if (roliblockAutoDetectBtn) {
    roliblockAutoDetectBtn.addEventListener('click', () => {
      roliblockManager.autoDetectDevices();
      renderRoliblockDeviceList();
      updateRoliblockStatus();
      status('Roliblock: Auto-detected ' + roliblockManager.getDevices().length + ' device(s).');
    });
  }
  if (roliblockPairBleBtn) {
    roliblockPairBleBtn.addEventListener('click', async () => {
      if (typeof navigator === 'undefined' || !navigator.bluetooth) {
        status('Bluetooth not supported. Use Chrome/Edge with Web Bluetooth enabled.', true);
        return;
      }
      status('Roliblock BLE: Opening Bluetooth pairing dialog...');
      const dev = roliblockManager.addDevice();
      const ok = await dev.connectBle();
      if (ok) {
        dev.enable();
        status('Roliblock BLE: Connected ' + (dev.bleConnection?.device?.name || 'device'));
      } else {
        roliblockManager.removeDevice(dev.id);
        status('Roliblock BLE: Pairing cancelled or failed.', true);
      }
      renderRoliblockDeviceList();
      updateRoliblockStatus();
      syncRoliblockFromView();
    });
  }
  if (roliblockLedDisplayMode) {
    roliblockLedDisplayMode.value = roliblockManager.ledDisplayMode;
    roliblockLedDisplayMode.addEventListener('change', () => {
      roliblockManager.ledDisplayMode = roliblockLedDisplayMode!.value as LedDisplayMode;
      roliblockManager.saveAll();
    });
  }
  // Initial render if devices were loaded from localStorage
  if (roliblockManager.getDevices().length > 0) renderRoliblockDeviceList();

  const audioDevice = el('audioDevice');
  const audioStartBtn = el('audioStartBtn');
  const audioStopBtn = el('audioStopBtn');
  const audioGain = el('audioGain');
  const audioGainVal = el('audioGainVal');
  const populateAudioDevices = async () => {
    try {
      const devices = await navigator.mediaDevices.enumerateDevices();
      const audioInputs = devices.filter((d) => d.kind === 'audioinput');
      const sel = audioDevice as HTMLSelectElement | null;
      if (sel) {
        sel.innerHTML = '<option value="">Default</option>';
        for (const d of audioInputs) {
          const opt = document.createElement('option');
          opt.value = d.deviceId;
          opt.textContent = d.label || 'Microphone ' + d.deviceId.slice(0, 8);
          sel.appendChild(opt);
        }
      }
    } catch (_) {}
  };
  populateAudioDevices();
  if (audioStartBtn) {
    audioStartBtn.addEventListener('click', async () => {
      const devId = (audioDevice as HTMLSelectElement)?.value || undefined;
      try {
        await audioEngine.start(devId);
        (audioStartBtn as HTMLElement).style.display = 'none';
        (audioStopBtn as HTMLElement).style.display = '';
      } catch (e) {
        status('Audio: ' + (e as Error).message, true);
      }
    });
  }
  if (audioStopBtn) {
    audioStopBtn.addEventListener('click', () => {
      audioEngine.stop();
      (audioStartBtn as HTMLElement).style.display = '';
      (audioStopBtn as HTMLElement).style.display = 'none';
    });
  }
  if (audioGain) {
    const gainInp = audioGain as HTMLInputElement;
    const gainLabel = gainInp.closest('.external-row')?.querySelector('label') as HTMLElement | null;
    if (gainLabel) gainLabel.style.color = sliderColorForKey('audioGain');
    enhanceRangeInput(gainInp, {
      colorKey: 'audioGain',
      valueEl: audioGainVal as HTMLElement | null,
      formatValue: (v) => v.toFixed(1)
    });
    gainInp.addEventListener('input', () => {
      const v = parseFloat(gainInp.value);
      audioEngine.setGain(v);
      if (audioGainVal) audioGainVal.textContent = v.toFixed(1);
    });
  }

  const exposeParamsPanelBtn = el('exposeParamsPanelBtn');
  const discoverSuggestions = el('discoverSuggestions');
  if (exposeParamsPanelBtn) {
    exposeParamsPanelBtn.addEventListener('click', () => {
      const codeExposeBtn = document.getElementById('codeExposeBtn');
      if (codeExposeBtn) (codeExposeBtn as HTMLButtonElement).click();
    });
  }
  const vibeReimagineBtn = el('vibeReimagineBtn');
  const vibeNewBtn = el('vibeNewBtn');
  if (vibeReimagineBtn) {
    vibeReimagineBtn.addEventListener('click', () => {
      const visualBtn = document.getElementById('codeVisualModifyBtn');
      if (visualBtn) {
        (visualBtn as HTMLButtonElement).click();
      } else {
        status('Visual modify button not found -- open code view first', true);
      }
    });
  }

  let vibeSelectedGenre = 'particles';
  const vibeGenreBtns = document.querySelectorAll('.vibe-genre-btn');
  vibeGenreBtns.forEach((btn) => {
    if ((btn as HTMLElement).dataset.genre === vibeSelectedGenre) {
      (btn as HTMLElement).style.background = 'var(--amiga-copper)';
      (btn as HTMLElement).style.color = 'var(--amiga-bg)';
    }
    btn.addEventListener('click', () => {
      vibeSelectedGenre = (btn as HTMLElement).dataset.genre || 'particles';
      vibeGenreBtns.forEach((b) => {
        (b as HTMLElement).style.background = '';
        (b as HTMLElement).style.color = '';
      });
      (btn as HTMLElement).style.background = 'var(--amiga-copper)';
      (btn as HTMLElement).style.color = 'var(--amiga-bg)';
    });
  });

  const vibeCreateBtn = el('vibeCreateBtn');
  const vibeCreateStatus = el('vibeCreateStatus');
  const CREATE_BTN_LABEL = '<span class="btn-tokens-icon" aria-hidden="true">&#x1F3AB;&#x1F4B8;</span> Create shader';
  if (vibeCreateBtn) {
    vibeCreateBtn.addEventListener('click', async () => {
      const nameInput = el('vibeNameInput') as HTMLInputElement | null;
      const descInput = el('vibeDescInput') as HTMLTextAreaElement | null;
      const name = nameInput?.value.trim() || '';
      const desc = descInput?.value.trim() || '';
      if (!name) { status('Enter a shader name', true); return; }
      const btn = vibeCreateBtn as HTMLButtonElement;
      btn.disabled = true;
      btn.innerHTML = 'Creating...';
      if (vibeCreateStatus) { vibeCreateStatus.style.display = 'block'; vibeCreateStatus.textContent = 'Creating ' + name + ' (' + vibeSelectedGenre + ')...'; }
      const { setCursorApiThinking, startCooldownCountdown } = await import('../init/bootstrap.js');
      const { fetchAgentStatus } = await import('../api.js');
      setCursorApiThinking(true);
      try {
        const { postVibeCreate } = await import('../api.js');
        const result = await postVibeCreate({
          name,
          genre: vibeSelectedGenre,
          description: desc,
          cursorApiKey: getCursorApiKey()
        });
        if (vibeCreateStatus) vibeCreateStatus.textContent = 'Created: ' + result.path + (desc ? ' (agent refining...)' : '');
        status('Shader created: ' + result.name + '. Reload list to see it.');
        const { buildList } = await import('../list.js');
        const { loadShader } = await import('../render.js');
        buildList();
        await loadShader({ id: result.id, path: result.path, name: result.name } as IndexEntry);
        if (nameInput) nameInput.value = '';
        if (descInput) descInput.value = '';
      } catch (e) {
        const msg = (e as Error)?.message || 'failed';
        if (vibeCreateStatus) vibeCreateStatus.textContent = 'Error: ' + msg;
        status('Vibe create: ' + msg, true);
        if (/rate|limit|429|cooldown/i.test(msg)) {
          const st = await fetchAgentStatus().catch(() => ({ cooldownRemainingSec: 15 }));
          const sec = st.cooldownRemainingSec ?? 15;
          startCooldownCountdown(sec, (s) => { status('Cursor API cooldown: ' + s + 's', true); });
        }
      } finally {
        setCursorApiThinking(false);
        btn.disabled = false;
        btn.innerHTML = CREATE_BTN_LABEL;
      }
    });
  }
  if (vibeNewBtn) {
    vibeNewBtn.addEventListener('click', async () => {
      const entry = currentEntry;
      let targetPath = '';
      if (entry && entry.path) {
        targetPath = entry.path;
      } else {
        const paths = appSettings.sourcePaths || [];
        targetPath = paths.length > 0 ? paths[0] : '';
      }
      if (!targetPath) { status('Load a shader first, or add source path in Settings', true); return; }
      try {
        const { postOpenAgent } = await import('../api.js');
        await postOpenAgent({ path: targetPath });
        status('Agent launched in shader directory. Create or modify shaders from the terminal.');
      } catch (e) {
        const err = e as Error & { rateLimit?: boolean };
        if (err.rateLimit) {
          status('Agent cooldown active. Wait and try again.', true);
        } else {
          status('Open agent: ' + (err.message || 'failed'), true);
        }
      }
    });
  }
  (globalThis as unknown as { setDiscoverSuggestions?: (s: string) => void }).setDiscoverSuggestions = (s: string) => {
    if (discoverSuggestions) discoverSuggestions.textContent = s;
  };
}

function rebuildExternalMappingUI(params: ParamDef[]): void {
  const assignable = params.filter((p) => p.type === 'float' || p.type === 'bool');
  const assignableForFft = assignable;

  const assignableIds = new Set(assignable.map((p) => p.id));
  for (const bandIdx of Object.keys(audioEngine.bandParamMap)) {
    const paramId = audioEngine.bandParamMap[parseInt(bandIdx, 10)];
    if (paramId && !assignableIds.has(paramId)) delete audioEngine.bandParamMap[parseInt(bandIdx, 10)];
  }
  audioEngine.saveMappings();

  const oscDiv = el('oscAddressList');
  if (oscDiv) {
    oscDiv.innerHTML = '';
    assignable.forEach((p) => {
      const row = document.createElement('div');
      row.style.cssText = 'display:flex;align-items:center;gap:4px;margin:2px 0;font-size:10px';
      const addr = oscEngine.getAddress(p.id);
      const lbl = document.createElement('span');
      lbl.style.cssText = 'width:60px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap';
      lbl.textContent = p.label;
      const inp = document.createElement('input');
      inp.type = 'text';
      inp.value = addr;
      inp.style.cssText = 'flex:1;font-size:10px;background:var(--amiga-bg);border:1px solid var(--bevel-dark);color:var(--crt-fg);font-family:inherit';
      inp.title = 'OSC address for ' + p.id;
      inp.addEventListener('change', () => {
        const v = inp.value.trim();
        if (v && v !== '/shader/' + p.id) oscEngine.setCustomAddress(p.id, v);
        else {
          oscEngine.setCustomAddress(p.id, null);
          inp.value = '/shader/' + p.id;
        }
        oscEngine.buildAddressMap(currentParamsMeta);
      });
      row.appendChild(lbl);
      row.appendChild(inp);
      oscDiv.appendChild(row);
    });
  }

  const midiDiv = el('midiMappingsList');
  if (midiDiv) {
    midiDiv.innerHTML = '';
    assignable.forEach((p) => {
      const row = document.createElement('div');
      row.style.cssText = 'display:flex;align-items:center;gap:4px;margin:2px 0;font-size:10px';
      const lbl = document.createElement('span');
      lbl.style.cssText = 'width:60px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap';
      lbl.textContent = p.label;
      const m = midiEngine.mappings[p.id];
      const learnBtn = document.createElement('button');
      learnBtn.type = 'button';
      learnBtn.className = 'wire-btn';
      learnBtn.style.cssText = 'font-size:10px;padding:2px 6px';
      learnBtn.textContent = midiEngine.learning === p.id ? '...' : m ? 'CC ' + m.cc + ' Ch ' + (m.ch + 1) : 'Learn';
      learnBtn.title = 'TLDR: ' + (m ? 'Click to remap' : 'Click then twist a knob');
      learnBtn.addEventListener('click', () => {
        if (midiEngine.learning === p.id) {
          midiEngine.cancelLearn();
        } else {
          midiEngine.learn(p.id);
        }
        rebuildExternalMappingUI(currentParamsMeta);
      });
      const forgetBtn = document.createElement('button');
      forgetBtn.type = 'button';
      forgetBtn.className = 'wire-btn';
      forgetBtn.style.cssText = 'font-size:10px;padding:2px 4px;display:' + (m ? '' : 'none') + '';
      forgetBtn.textContent = 'X';
      forgetBtn.title = 'TLDR: Remove MIDI mapping';
      forgetBtn.addEventListener('click', () => {
        midiEngine.forget(p.id);
        rebuildExternalMappingUI(currentParamsMeta);
      });
      row.appendChild(lbl);
      row.appendChild(learnBtn);
      row.appendChild(forgetBtn);
      midiDiv.appendChild(row);
    });
  }

  const fftDiv = el('fftBandAssignments');
  if (fftDiv) {
    fftDiv.innerHTML = '';
    if (assignableForFft.length === 0) {
      const hint = document.createElement('div');
      hint.style.cssText = 'font-size:10px;color:var(--crt-dim);margin-bottom:6px';
      hint.textContent = 'No mappable params. Use Discover params + Auto-expose, or load an ISF shader.';
      fftDiv.appendChild(hint);
    } else {
      const btnRow = document.createElement('div');
      btnRow.style.cssText = 'display:flex;gap:6px;margin-bottom:6px;flex-wrap:wrap';
      const autoArrangeBtn = document.createElement('button');
      autoArrangeBtn.type = 'button';
      autoArrangeBtn.className = 'wire-btn';
      autoArrangeBtn.style.fontSize = '10px';
      autoArrangeBtn.textContent = 'Auto-arrange';
      autoArrangeBtn.title = 'Assign bands 0-' + (FFT_BAND_COUNT - 1) + ' to first ' + FFT_BAND_COUNT + ' params in order';
      autoArrangeBtn.onclick = () => {
        for (let b = 0; b < FFT_BAND_COUNT; b++) {
          if (b < assignableForFft.length) {
            audioEngine.bandParamMap[b] = assignableForFft[b].id;
          } else {
            delete audioEngine.bandParamMap[b];
          }
        }
        audioEngine.saveMappings();
        rebuildExternalMappingUI(currentParamsMeta);
        status('FFT: Auto-arranged bands to params');
      };
      const quickAssignBtn = document.createElement('button');
      quickAssignBtn.type = 'button';
      quickAssignBtn.className = 'wire-btn';
      quickAssignBtn.style.fontSize = '10px';
      quickAssignBtn.textContent = 'Quick assign';
      quickAssignBtn.title = 'Fill unassigned bands with unused params';
      quickAssignBtn.onclick = () => {
        const usedParams = new Set(Object.values(audioEngine.bandParamMap));
        const unusedParams = assignableForFft.filter((p) => !usedParams.has(p.id));
        let u = 0;
        for (let b = 0; b < FFT_BAND_COUNT; b++) {
          if (audioEngine.bandParamMap[b] !== undefined) continue;
          if (u < unusedParams.length) {
            audioEngine.bandParamMap[b] = unusedParams[u].id;
            u++;
          }
        }
        audioEngine.saveMappings();
        rebuildExternalMappingUI(currentParamsMeta);
        status('FFT: Quick-assigned ' + u + ' bands');
      };
      btnRow.appendChild(autoArrangeBtn);
      btnRow.appendChild(quickAssignBtn);
      fftDiv.appendChild(btnRow);
    }
    FFT_BAND_LABELS.forEach((label, i) => {
      const row = document.createElement('div');
      row.style.cssText = 'display:flex;align-items:center;gap:4px;margin:2px 0;font-size:10px';
      const lbl = document.createElement('span');
      lbl.style.cssText = 'width:52px;flex-shrink:0';
      lbl.textContent = label;
      const sel = document.createElement('select');
      sel.style.cssText = 'flex:1;font-size:10px;background:var(--amiga-bg);border:1px solid var(--bevel-dark);color:var(--crt-fg);font-family:inherit';
      sel.innerHTML = '<option value="">--</option>' + assignableForFft.map((p) =>
        '<option value="' + escapeAttr(p.id) + '"' + (audioEngine.bandParamMap[i] === p.id ? ' selected' : '') + '>' + escapeHtml(p.label) + '</option>').join('');
      sel.addEventListener('change', () => {
        if (sel.value) audioEngine.bandParamMap[i] = sel.value;
        else delete audioEngine.bandParamMap[i];
        audioEngine.saveMappings();
      });
      row.appendChild(lbl);
      row.appendChild(sel);
      fftDiv.appendChild(row);
    });
  }

  assignable.forEach((p) => {
    const badge = document.querySelector('.param-row .midi-badge[data-param="' + escapeAttr(p.id) + '"]');
    if (badge) {
      const m = midiEngine.mappings[p.id];
      badge.textContent = midiEngine.learning === p.id ? '...' : m ? 'CC' + m.cc : 'M';
    }
  });
}

function escapeHtml(s: string): string {
  const d = document.createElement('div');
  d.textContent = s;
  return d.innerHTML;
}
function escapeAttr(s: string): string {
  return s.replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

export function updateISFPanel(): void {
  const info = el('isfInfo');
  if (!info) return;
  const isf = currentSource ? parseISF(currentSource) : null;
  if (!isf) {
    info.innerHTML = 'No ISF metadata for this shader.';
    return;
  }
  const inputs = isf.INPUTS || [];
  info.innerHTML = '<div style="font-size:10px;color:var(--crt-dim);margin-bottom:6px">INPUTS: ' + inputs.length + '</div>' +
    (inputs.length ? inputs.map((i: { NAME: string; TYPE: string; LABEL?: string }) =>
      '<div style="font-size:10px;margin:2px 0">' + escapeHtml(i.NAME) + ' (' + i.TYPE + ')' + (i.LABEL ? ' - ' + escapeHtml(i.LABEL) : '') + '</div>').join('') : '');
}
