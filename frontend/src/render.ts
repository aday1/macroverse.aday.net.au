import { el } from './dom.js';
import { status } from './dom.js';
import { fetchShader, postThumbnailSave, postMacroCamFrame } from './api.js';
import { setCurrentSource, appSettings, setLastCompileError, clearLastCompileError, setPendingCursorConfirm, currentEntry, getPendingCursorConfirm, setThumbnail, getThumbnail } from './state.js';
import { paramValues, buildParamsPanel, updateISFPanel, currentParamsMeta, lastDiscoveredParams, setLastDiscoveredParams, paramToUniformMap, setParamToUniformMap, clearParamsForNewShader } from './panels/params.js';
import { oscEngine } from './engines/osc.js';
import { midiEngine } from './engines/midi.js';
import { audioEngine } from './engines/audio.js';
import type { IndexEntry } from './types.js';
import { captureSnapshot, playTransition } from './transitions.js';
import { roliblockManager } from './engines/roliblock.js';

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

let gl: WebGLRenderingContext | null = null;
let program: WebGLProgram | null = null;
let programUniformNames: Set<string> = new Set();
let programSamplerNames: string[] = [];
let paramUniformLocations: Map<string, WebGLUniformLocation> = new Map();
let pendingThumbnailPath: string | null = null;
const THUMB_CAPTURE_DELAY_MS = 5000;
let pendingThumbnailStartTime = 0;

const previewChannel = new BroadcastChannel('macroverse-preview-output');
let lastPreparedPreviewSource = '';
let lastPreviewParamsJson = '';

previewChannel.onmessage = (ev: MessageEvent) => {
  if (ev.data && ev.data.type === 'preview-ready' && lastPreparedPreviewSource) {
    previewChannel.postMessage({
      type: 'shader',
      preparedSource: lastPreparedPreviewSource,
      meta: []
    });
  }
};

let previewPopOutWin: Window | null = null;
export function openPreviewPopOut(): void {
  if (previewPopOutWin && !previewPopOutWin.closed) {
    previewPopOutWin.focus();
    return;
  }
  previewPopOutWin = window.open('/preview-output.html', 'macroverse-preview-output');
  if (!previewPopOutWin) {
    console.warn('[Preview] Pop-out blocked by browser.');
    return;
  }
  previewPopOutWin.addEventListener('beforeunload', () => { previewPopOutWin = null; });
}

export interface TextureSource {
  texture: WebGLTexture | null;
  video: HTMLVideoElement | null;
  image: TexImageSource | null;
  source: 'webcam' | 'image' | null;
}
const textureSources: Map<string, TextureSource> = new Map();

export function getSamplerNames(): string[] {
  return [...programSamplerNames];
}

export function setTextureWebcam(samplerName: string): void {
  const existing = textureSources.get(samplerName);
  if (existing?.video) return;
  if (existing?.texture && gl) {
    gl.deleteTexture(existing.texture);
    textureSources.delete(samplerName);
  }
  navigator.mediaDevices.getUserMedia({ video: true, audio: false })
    .then((stream) => {
      const video = document.createElement('video');
      video.srcObject = stream;
      video.play();
      video.setAttribute('playsinline', '');
      textureSources.set(samplerName, { texture: null, video, image: null, source: 'webcam' });
      status('Webcam on for ' + samplerName);
    })
    .catch((e) => status('Webcam: ' + (e as Error).message, true));
}

export function setTextureImage(samplerName: string, file: File): void {
  const existing = textureSources.get(samplerName);
  if (existing?.texture && gl) gl.deleteTexture(existing.texture);
  const url = URL.createObjectURL(file);
  const img = new Image();
  img.onload = () => {
    URL.revokeObjectURL(url);
    textureSources.set(samplerName, { texture: null, video: null, image: img, source: 'image' });
    status('Image set for ' + samplerName);
  };
  img.onerror = () => {
    URL.revokeObjectURL(url);
    status('Failed to load image', true);
  };
  img.src = url;
}

export function clearTextureSource(samplerName: string): void {
  const existing = textureSources.get(samplerName);
  if (existing?.texture && gl) gl.deleteTexture(existing.texture);
  if (existing?.video && existing.video.srcObject) {
    const tracks = (existing.video.srcObject as MediaStream).getTracks();
    tracks.forEach((t) => t.stop());
  }
  textureSources.delete(samplerName);
}

let loadAbortController: AbortController | null = null;

let defaultSamplerTexture: WebGLTexture | null = null;
function getDefaultSamplerTexture(glCtx: WebGLRenderingContext): WebGLTexture {
  if (defaultSamplerTexture) return defaultSamplerTexture;
  const tex = glCtx.createTexture();
  if (!tex) throw new Error('createTexture failed');
  glCtx.bindTexture(glCtx.TEXTURE_2D, tex);
  glCtx.texImage2D(glCtx.TEXTURE_2D, 0, glCtx.RGBA, 1, 1, 0, glCtx.RGBA, glCtx.UNSIGNED_BYTE, new Uint8Array([128, 128, 128, 255]));
  glCtx.texParameteri(glCtx.TEXTURE_2D, glCtx.TEXTURE_MIN_FILTER, glCtx.LINEAR);
  glCtx.texParameteri(glCtx.TEXTURE_2D, glCtx.TEXTURE_WRAP_S, glCtx.CLAMP_TO_EDGE);
  glCtx.texParameteri(glCtx.TEXTURE_2D, glCtx.TEXTURE_WRAP_T, glCtx.CLAMP_TO_EDGE);
  defaultSamplerTexture = tex;
  return tex;
}

function bindSamplerTextures(glCtx: WebGLRenderingContext, prog: WebGLProgram): void {
  const defaultTex = getDefaultSamplerTexture(glCtx);
  for (let i = 0; i < programSamplerNames.length; i++) {
    const name = programSamplerNames[i];
    const src = textureSources.get(name);
    let tex: WebGLTexture | null = null;
    if (src?.video && src.video.readyState >= 2) {
      if (!src.texture) {
        src.texture = glCtx.createTexture();
        if (src.texture) {
          glCtx.bindTexture(glCtx.TEXTURE_2D, src.texture);
          glCtx.texParameteri(glCtx.TEXTURE_2D, glCtx.TEXTURE_MIN_FILTER, glCtx.LINEAR);
          glCtx.texParameteri(glCtx.TEXTURE_2D, glCtx.TEXTURE_WRAP_S, glCtx.CLAMP_TO_EDGE);
          glCtx.texParameteri(glCtx.TEXTURE_2D, glCtx.TEXTURE_WRAP_T, glCtx.CLAMP_TO_EDGE);
        }
      }
      if (src.texture) {
        glCtx.activeTexture(glCtx.TEXTURE0 + i);
        glCtx.bindTexture(glCtx.TEXTURE_2D, src.texture);
        glCtx.texImage2D(glCtx.TEXTURE_2D, 0, glCtx.RGBA, glCtx.RGBA, glCtx.UNSIGNED_BYTE, src.video);
        tex = src.texture;
      }
    } else if (src?.image) {
      if (!src.texture) {
        src.texture = glCtx.createTexture();
        if (src.texture) {
          glCtx.bindTexture(glCtx.TEXTURE_2D, src.texture);
          glCtx.texParameteri(glCtx.TEXTURE_2D, glCtx.TEXTURE_MIN_FILTER, glCtx.LINEAR);
          glCtx.texParameteri(glCtx.TEXTURE_2D, glCtx.TEXTURE_WRAP_S, glCtx.CLAMP_TO_EDGE);
          glCtx.texParameteri(glCtx.TEXTURE_2D, glCtx.TEXTURE_WRAP_T, glCtx.CLAMP_TO_EDGE);
        }
      }
      if (src.texture) {
        glCtx.activeTexture(glCtx.TEXTURE0 + i);
        glCtx.bindTexture(glCtx.TEXTURE_2D, src.texture);
        glCtx.texImage2D(glCtx.TEXTURE_2D, 0, glCtx.RGBA, glCtx.RGBA, glCtx.UNSIGNED_BYTE, src.image);
        tex = src.texture;
      }
    }
    glCtx.activeTexture(glCtx.TEXTURE0 + i);
    glCtx.bindTexture(glCtx.TEXTURE_2D, tex || defaultTex);
    const loc = glCtx.getUniformLocation(prog, name);
    if (loc) glCtx.uniform1i(loc, i);
  }
}

export function initGl(glContext: WebGLRenderingContext): void {
  gl = glContext;
  setInterval(() => {
    if (program === null || !gl?.canvas) return;
    for (const dev of roliblockManager.getDevices()) {
      if (dev.enabled) dev.sampleAndSendLed(gl.canvas as HTMLCanvasElement);
    }
  }, 50);
}

export function setMouse(x: number, y: number): void {
  paramValues.mouseX = x;
  paramValues.mouseY = y;
}

let rafId = 0;
let bgIntervalId = 0;
let startTime = 0;
const quadVerts = new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]);

let macroCamEnabled = false;
let macroCamFrameCount = 0;
let macroCamFrameInterval = 3; // send every Nth frame (~20fps at 60fps)
let macroCamQuality = 0.65; // JPEG quality (lower = faster, less bandwidth)

export function setMacroCamEnabled(on: boolean): void { macroCamEnabled = on; macroCamFrameCount = 0; }
export function isMacroCamEnabled(): boolean { return macroCamEnabled; }
export function setMacroCamFrameInterval(n: number): void { macroCamFrameInterval = Math.max(1, Math.min(30, n)); }
export function setMacroCamQuality(q: number): void { macroCamQuality = Math.max(0.1, Math.min(1.0, q)); }

function sendMacroCamFrame(): void {
  if (!macroCamEnabled || !gl || !gl.canvas) return;
  macroCamFrameCount++;
  if (macroCamFrameCount % macroCamFrameInterval !== 0) return;
  const canvas = gl.canvas as HTMLCanvasElement;
  canvas.toBlob((blob) => {
    if (blob) postMacroCamFrame(blob).catch(() => {});
  }, 'image/jpeg', macroCamQuality);
}

function shaderPath(entry: IndexEntry | null | undefined): string {
  return (entry && entry.path) || '';
}

export function applyExposeToSource(src: string, paramNames: string[]): string {
  let body = src || '';
  const toInject: Array<{ suggested: string; shaderName: string }> = [];
  for (const name of paramNames) {
    if (!/^\w+$/.test(name)) continue;
    if (/^(time|mouse|resolution|TIME|RENDERSIZE|mouseX|mouseY|timeScale)$/i.test(name)) continue;
    const re = new RegExp('\\b(' + name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + ')\\b', 'gi');
    const m = body.match(re);
    if (!m) continue;
    const shaderName = m[0];
    if (new RegExp('uniform\\s+\\w+\\s+' + shaderName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\s*[;=]', 'i').test(body)) continue;
    const esc = shaderName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const hasConstOrDefine = new RegExp('^\\s*(?:const\\s+)?(?:float|highp\\s+float)\\s+' + esc + '\\s*=\\s*[^;]+', 'im').test(body)
      || new RegExp('^\\s*#define\\s+' + esc + '\\s+[^\\s/]+', 'im').test(body);
    if (!hasConstOrDefine) continue;
    toInject.push({ suggested: name, shaderName });
  }
  if (toInject.length === 0) return body;
  let uniformBlock = '';
  for (const { shaderName } of toInject) {
    uniformBlock += 'uniform float ' + shaderName + '; // @expose\n';
    const esc = shaderName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    body = body.replace(new RegExp('^\\s*(?:const\\s+)?(?:float|highp\\s+float)\\s+' + esc + '\\s*=\\s*[^;]+;(?:\\s*\\/\\/[^\\n]*)?\\s*$', 'gim'), '/* uniform ' + shaderName + ' from params */\n');
    body = body.replace(new RegExp('^\\s*#define\\s+' + esc + '\\s+[^\\s/]+(?:\\s*\\/\\/[^\\n]*)?\\s*$', 'gim'), '/* #define ' + shaderName + ' overridden by param */\n');
  }
  let insert = 0;
  const openBlock = body.indexOf('/*');
  if (openBlock >= 0 && openBlock < 600) {
    const closeBlock = body.indexOf('*/', openBlock);
    const maxBlock = body.indexOf('/*{') === openBlock ? 50000 : 12000;
    if (closeBlock !== -1 && closeBlock < maxBlock) {
      insert = closeBlock + 2;
    }
  }
  if (insert === 0) {
    const firstNewline = body.indexOf('\n');
    const firstLine = firstNewline >= 0 ? body.slice(0, firstNewline).trim() : body.trim().slice(0, 80);
    if (/^#version\b|^#extension\b|^precision\s+/i.test(firstLine)) {
      let pos = firstNewline >= 0 ? firstNewline + 1 : body.length;
      while (pos < body.length && pos < 800) {
        const next = body.indexOf('\n', pos);
        const line = next >= 0 ? body.slice(pos, next).trim() : body.slice(pos).trim();
        if (line === '' || /^#version\b|^#extension\b|^precision\s+/i.test(line)) {
          pos = next >= 0 ? next + 1 : body.length;
        } else {
          break;
        }
      }
      insert = pos;
    }
  }
  const before = body.slice(0, insert);
  const after = body.slice(insert);
  const needNewlineBefore = insert > 0 && !before.endsWith('\n');
  const needNewlineAfter = insert > 0 && after !== '' && !after.startsWith('\n');
  return before + (needNewlineBefore ? '\n' : '') + uniformBlock + (needNewlineAfter ? '\n' : insert === 0 ? '\n' : '') + after;
}

export function removeExposeFromSource(src: string, paramName: string, currentValue: number | boolean): string {
  const body = src || '';
  const esc = paramName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const lineRe = new RegExp('^\\s*uniform\\s+(float|bool)\\s+(' + esc + ')\\s*;\\s*\\/\\/\\s*@expose[^\\n]*\\n', 'gim');
  const newLine = body.replace(lineRe, (_match, type: string, name: string) => {
    if (type.toLowerCase() === 'bool') {
      return 'bool ' + name + ' = ' + (currentValue ? 'true' : 'false') + ';  // unexposed\n';
    }
    return 'float ' + name + ' = ' + Number(currentValue) + ';  // unexposed\n';
  });
  return newLine === body ? body : newLine;
}

function rangeFromValue(value: number): { min: number; max: number } {
  const margin = value > 1 ? Math.max(1, value * 0.5) : value < 0 ? Math.max(1, -value * 0.5) : 1;
  return {
    min: Math.min(0, value - margin),
    max: Math.max(1, value + margin)
  };
}

export function exposeLiteralInSource(src: string, value: number, startIndex: number, endIndex: number, suggestedName?: string): { newSrc: string; paramName: string } {
  const existingNames = new Set<string>();
  const re = /uniform\s+float\s+(\w+)\s*;/gi;
  let m: RegExpExecArray | null;
  while ((m = re.exec(src)) !== null) existingNames.add(m[1].toLowerCase());
  const baseName = suggestedName || ('val_' + String(value).replace(/\./g, '_').replace(/-/g, 'm').replace(/^(\d)/, 'n$1'));
  const safeBase = baseName.replace(/\W/g, '_').replace(/^(\d)/, 'n$1') || 'val';
  let paramName = safeBase;
  let n = 0;
  while (existingNames.has(paramName.toLowerCase())) {
    n++;
    paramName = safeBase + '_' + n;
  }
  const { min, max } = rangeFromValue(value);
  const exposeLine = 'uniform float ' + paramName + '; // @expose ' + min + ' ' + max + '\n';
  let insert = 0;
  const openBlock = src.indexOf('/*');
  if (openBlock >= 0 && openBlock < 600) {
    const closeBlock = src.indexOf('*/', openBlock);
    const maxBlock = src.indexOf('/*{') === openBlock ? 50000 : 12000;
    if (closeBlock !== -1 && closeBlock < maxBlock) insert = closeBlock + 2;
  }
  if (insert === 0) {
    const firstNewline = src.indexOf('\n');
    const firstLine = firstNewline >= 0 ? src.slice(0, firstNewline).trim() : src.trim().slice(0, 80);
    if (/^#version\b|^#extension\b|^precision\s+/i.test(firstLine)) {
      let pos = firstNewline >= 0 ? firstNewline + 1 : src.length;
      while (pos < src.length && pos < 800) {
        const next = src.indexOf('\n', pos);
        const line = next >= 0 ? src.slice(pos, next).trim() : src.slice(pos).trim();
        if (line === '' || /^#version\b|^#extension\b|^precision\s+/i.test(line)) {
          pos = next >= 0 ? next + 1 : src.length;
        } else {
          break;
        }
      }
      insert = pos;
    }
  }
  let replaceStart = startIndex;
  let replaceEnd = endIndex;
  if (startIndex > 0 && replaceEnd > replaceStart) {
    const charBefore = src[replaceStart - 1];
    if (charBefore === '.') {
      while (replaceStart > 0 && /[\d.]/.test(src[replaceStart - 1])) replaceStart--;
      if (replaceStart > 0 && src[replaceStart - 1] === '-') replaceStart--;
    }
  }
  const beforeInsert = src.slice(0, insert);
  const afterInsert = src.slice(insert);
  const needNewlineBefore = insert > 0 && !beforeInsert.endsWith('\n');
  const needNewlineAfter = afterInsert !== '' && !afterInsert.startsWith('\n');
  const withUniform = beforeInsert + (needNewlineBefore ? '\n' : '') + exposeLine.trim() + (needNewlineAfter ? '\n' : insert === 0 ? '\n' : '') + afterInsert;
  const offset = withUniform.length - src.length;
  const newStart = replaceStart + (replaceStart >= insert ? offset : 0);
  const newEnd = replaceEnd + (replaceEnd >= insert ? offset : 0);
  const newSrc = withUniform.slice(0, newStart) + paramName + withUniform.slice(newEnd);
  return { newSrc, paramName };
}

export function addConstantToSource(src: string, name: string, value: number, min: number, max: number): string {
  const body = src || '';
  const esc = name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const exposeLine = 'uniform float ' + name + '; // @expose ' + min + ' ' + max + '\n';
  if (new RegExp('uniform\\s+float\\s+' + esc + '\\s*;', 'i').test(body)) return body;
  let out = body;
  const defineRe = new RegExp('^\\s*#define\\s+' + esc + '\\s+[^\\s/]+(?:\\s*\\/\\/[^\\n]*)?\\s*$', 'gim');
  const constRe = new RegExp('^\\s*(?:const\\s+)?(?:float|highp\\s+float)\\s+' + esc + '\\s*=\\s*[^;]+;(?:\\s*\\/\\/[^\\n]*)?\\s*$', 'gim');
  const afterDefine = out.replace(defineRe, exposeLine.trim() + '\n');
  if (afterDefine !== out) return afterDefine;
  const afterConst = out.replace(constRe, exposeLine.trim() + '\n');
  if (afterConst !== out) return afterConst;
  let insert = 0;
  const openBlock = out.indexOf('/*');
  if (openBlock >= 0 && openBlock < 600) {
    const closeBlock = out.indexOf('*/', openBlock);
    const maxBlock = out.indexOf('/*{') === openBlock ? 50000 : 12000;
    if (closeBlock !== -1 && closeBlock < maxBlock) insert = closeBlock + 2;
  }
  if (insert === 0) {
    const firstNewline = out.indexOf('\n');
    const firstLine = firstNewline >= 0 ? out.slice(0, firstNewline).trim() : out.trim().slice(0, 80);
    if (/^#version\b|^#extension\b|^precision\s+/i.test(firstLine)) {
      let pos = firstNewline >= 0 ? firstNewline + 1 : out.length;
      while (pos < out.length && pos < 800) {
        const next = out.indexOf('\n', pos);
        const line = next >= 0 ? out.slice(pos, next).trim() : out.slice(pos).trim();
        if (line === '' || /^#version\b|^#extension\b|^precision\s+/i.test(line)) {
          pos = next >= 0 ? next + 1 : out.length;
        } else {
          break;
        }
      }
      insert = pos;
    }
  }
  const before = out.slice(0, insert);
  const after = out.slice(insert);
  const needNewlineBefore = insert > 0 && !before.endsWith('\n');
  const needNewlineAfter = after !== '' && !after.startsWith('\n');
  return before + (needNewlineBefore ? '\n' : '') + exposeLine.trim() + (needNewlineAfter ? '\n' : insert === 0 ? '\n' : '') + after;
}

function findInsertPointAfterPreamble(body: string): number {
  let insert = 0;
  const openBlock = body.indexOf('/*');
  if (openBlock >= 0 && openBlock < 600) {
    const closeBlock = body.indexOf('*/', openBlock);
    const maxBlock = body.indexOf('/*{') === openBlock ? 50000 : 12000;
    if (closeBlock !== -1 && closeBlock < maxBlock) insert = closeBlock + 2;
  }
  if (insert === 0) {
    const firstNewline = body.indexOf('\n');
    const firstLine = firstNewline >= 0 ? body.slice(0, firstNewline).trim() : body.trim().slice(0, 80);
    if (/^#version\b|^#extension\b|^precision\s+/i.test(firstLine)) {
      let pos = firstNewline >= 0 ? firstNewline + 1 : body.length;
      while (pos < body.length && pos < 800) {
        const next = body.indexOf('\n', pos);
        const line = next >= 0 ? body.slice(pos, next).trim() : body.slice(pos).trim();
        if (line === '' || /^#version\b|^#extension\b|^precision\s+/i.test(line)) {
          pos = next >= 0 ? next + 1 : body.length;
        } else {
          break;
        }
      }
      insert = pos;
    }
  }
  return insert;
}

const MAX_SAMPLER2D = 5;
const SAMPLER2D_NAMES = ['tex', 'video', 'inputImage', 'tex2', 'tex3'];

function getExistingSamplerNames(body: string): string[] {
  const re = /uniform\s+sampler2D\s+(\w+)\s*[;=]/gi;
  const out: string[] = [];
  let m: RegExpExecArray | null;
  while ((m = re.exec(body)) !== null) out.push(m[1].toLowerCase());
  return out;
}

export function canAddSampler2D(src: string): boolean {
  const body = src || '';
  if (!/\bgl_FragColor\b/.test(body)) return false;
  if (!/\bvoid\s+main\s*\(/.test(body)) return false;
  const existing = getExistingSamplerNames(body);
  if (existing.length >= MAX_SAMPLER2D) return false;
  return true;
}

export function addSampler2DToSource(src: string, uniformName?: string): string | null {
  const body = src || '';
  if (!canAddSampler2D(body)) return null;
  const existingSet = new Set(getExistingSamplerNames(body));
  let name = uniformName ? uniformName.replace(/\W/g, '_') : '';
  if (!name || existingSet.has(name.toLowerCase())) {
    for (const n of SAMPLER2D_NAMES) {
      if (!existingSet.has(n.toLowerCase())) { name = n; break; }
    }
    if (!name) name = 'tex' + (existingSet.size + 1);
  }
  const uniformLine = 'uniform sampler2D ' + name + ';\n';
  const insert = findInsertPointAfterPreamble(body);
  const before = body.slice(0, insert);
  const after = body.slice(insert);
  const needNewlineBefore = insert > 0 && !before.endsWith('\n');
  const needNewlineAfter = after !== '' && !after.startsWith('\n');
  const withUniform = before + (needNewlineBefore ? '\n' : '') + uniformLine.trim() + (needNewlineAfter ? '\n' : insert === 0 ? '\n' : '') + after;
  const lastFragColor = withUniform.lastIndexOf('gl_FragColor');
  if (lastFragColor === -1) return null;
  const semicolonAfter = withUniform.indexOf(';', lastFragColor);
  if (semicolonAfter === -1) return null;
  const insertBlendAt = semicolonAfter + 1;
  const uvExpr = (/\bRENDERSIZE\b/.test(body) || /\bresolution\b/.test(body)) ? 'RENDERSIZE' : 'vec2(1920.,1080.)';
  const blendLine = '\n  gl_FragColor = mix(texture2D(' + name + ', gl_FragCoord.xy/' + uvExpr + '), gl_FragColor, 0.65);';
  const newSrc = withUniform.slice(0, insertBlendAt) + blendLine + withUniform.slice(insertBlendAt);
  return newSrc;
}

function extractUniformNames(preamble: string): Set<string> {
  const names = new Set<string>();
  const re = /uniform\s+\w+\s+(\w+)\s*[;=]/g;
  let m: RegExpExecArray | null;
  while ((m = re.exec(preamble)) !== null) names.add(m[1]);
  return names;
}

function stripDuplicateUniformDecls(body: string, preamble: string): string {
  const declared = extractUniformNames(preamble);
  if (declared.size === 0) return body;
  return body.replace(/^\s*uniform\s+\w+\s+(\w+)\s*[;=][^\n]*/gm, (line, name) => {
    return declared.has(name) ? '' : line;
  });
}

const ISF_PREAMBLE_NAMES = new Set([
  'TIME', 'RENDERSIZE', 'FRAMEINDEX', 'iFrame',
  'useFrameIndex', 'fps', 'timeScale', 'mouseX', 'mouseY',
  'uTimeScale', 'uMouse'
]);

function isfInputsToUniforms(body: string): string {
  const blockMatch = body.match(/\/\*\s*\{[\s\S]*?\}\s*\*\//);
  if (!blockMatch) return '';
  let jsonStr = blockMatch[0].replace(/^\s*\/\*\s*/, '').replace(/\s*\*\/\s*$/, '');
  try {
    const meta = JSON.parse(jsonStr) as { INPUTS?: Array<{ NAME?: string; TYPE?: string }> };
    const inputs = meta.INPUTS;
    if (!Array.isArray(inputs) || inputs.length === 0) return '';
    const lines: string[] = [];
    for (const inp of inputs) {
      const name = inp.NAME;
      if (!name || typeof name !== 'string' || !/^\w+$/.test(name)) continue;
      if (ISF_PREAMBLE_NAMES.has(name)) continue;
      const t = (inp.TYPE || 'float').toLowerCase();
      let glslType = 'float';
      if (t === 'bool') glslType = 'bool';
      else if (t === 'vec2' || t === 'point2d') glslType = 'vec2';
      else if (t === 'vec3' || t === 'color') glslType = 'vec3';
      else if (t === 'vec4') glslType = 'vec4';
      else if (t === 'image' || t === 'sampler2d') glslType = 'sampler2D';
      else glslType = 'float';
      lines.push('uniform ' + glslType + ' ' + name + ';');
    }
    return lines.length ? lines.join('\n') + '\n' : '';
  } catch {
    return '';
  }
}

function addWebGLUniforms(src: string): string {
  let body = src || '';
  const extensions: string[] = [];
  body = body.replace(/#\s*(extension|version)\s+[^\n]+/g, (m) => {
    extensions.push(m.trim());
    return '';
  });
  body = body.replace(/\n\s*\n\s*\n/g, '\n\n').trim();

  const hasSandboxUniforms = /uniform\s+float\s+time\s*;/.test(body) || /uniform\s+vec2\s+(mouse|resolution)\s*;/.test(body);
  const hasISF = /"INPUTS"\s*:/.test(body) && (/\bTIME\b/.test(body) || /\bRENDERSIZE\b/.test(body));
  const hasOurPreamble = /uniform\s+float\s+TIME\s*;/.test(body);
  const needsPreamble = !hasOurPreamble && !hasSandboxUniforms;

  body = body.replace(/\s*precision\s+(lowp|mediump|highp)\s+float\s*;\s*/gi, '\n');

  const toInject: Array<{ suggested: string; shaderName: string }> = [];
  const allMapped: Array<{ suggested: string; shaderName: string }> = [];
    const skipUniforms = new Set(['time', 'mouse', 'resolution', 'TIME', 'RENDERSIZE', 'uTimeScale', 'uMouse', 'FRAMEINDEX', 'iFrame', 'mouseX', 'mouseY', 'useFrameIndex', 'fps', 'timeScale']);
  for (const name of lastDiscoveredParams) {
    if (!/^\w+$/.test(name)) continue;
    if (/^(time|mouse|resolution|TIME|RENDERSIZE|mouseX|mouseY)$/i.test(name)) continue;
    const re = new RegExp('\\b(' + name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + ')\\b', 'gi');
    const m = body.match(re);
    if (!m) continue;
    const shaderName = m[0];
    allMapped.push({ suggested: name, shaderName });
    if (new RegExp('uniform\\s+\\w+\\s+' + shaderName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\s*[;=]', 'i').test(body)) continue;
    const esc = shaderName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const hasConstOrDefine = new RegExp('^\\s*(?:const\\s+)?(?:float|highp\\s+float)\\s+' + esc + '\\s*=\\s*[^;]+', 'im').test(body)
      || new RegExp('^\\s*#define\\s+' + esc + '\\s+[^\\s/]+', 'im').test(body);
    if (!hasConstOrDefine) continue;
    toInject.push({ suggested: name, shaderName });
  }
  const baseMap: Record<string, string> = {};
  const uniformRe = /uniform\s+(?:float|bool)\s+(\w+)\s*[;=]/g;
  let um: RegExpExecArray | null;
  while ((um = uniformRe.exec(body)) !== null) {
    const n = um[1];
    if (!skipUniforms.has(n)) baseMap[n] = n;
  }
  const combined = { ...baseMap, ...Object.fromEntries(allMapped.map(({ suggested, shaderName }) => [suggested, shaderName])) };
  setParamToUniformMap(combined);
  let discoveredUniforms = '';
  for (const { suggested, shaderName } of toInject) {
    discoveredUniforms += 'uniform float ' + shaderName + ';\n';
    const esc = shaderName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    body = body.replace(new RegExp('^\\s*(?:const\\s+)?(?:float|highp\\s+float)\\s+' + esc + '\\s*=\\s*[^;]+;(?:\\s*\\/\\/[^\\n]*)?\\s*$', 'gim'), '/* uniform ' + shaderName + ' from params */\n');
    body = body.replace(new RegExp('^\\s*#define\\s+' + esc + '\\s+[^\\s/]+(?:\\s*\\/\\/[^\\n]*)?\\s*$', 'gim'), '/* #define ' + shaderName + ' overridden by param */\n');
  }

  const usesRendersize = /\bRENDERSIZE\b|\bresolution\b|\biResolution\b/.test(body);
  const declaresRendersize = /uniform\s+vec2\s+RENDERSIZE\s*[;=]/.test(body) || /uniform\s+vec2\s+resolution\s*[;=]/.test(body) || /#define\s+RENDERSIZE\s+/.test(body);
  if (usesRendersize && declaresRendersize) {
    body = body.replace(/^\s*uniform\s+vec2\s+RENDERSIZE\s*[;=]\s*\n?/gm, '');
    body = body.replace(/^\s*uniform\s+vec2\s+resolution\s*[;=]\s*\n?/gm, '');
    body = body.replace(/^\s*#ifndef\s+RENDERSIZE\s*\n#define\s+RENDERSIZE\s+[^\n]+\n#endif\s*\n?/gm, '');
  }
  const needsRendersize = usesRendersize;
  const rendersizeDecl = needsRendersize ? 'uniform vec2 RENDERSIZE;\n' : '';
  const preamble = `uniform float TIME;
` + rendersizeDecl + `uniform float uTimeScale;
uniform vec2 uMouse;
uniform float iFrame;
` + discoveredUniforms + `
#ifndef time
#define time (TIME * uTimeScale)
#endif
#ifndef resolution
#define resolution RENDERSIZE
#endif
#ifndef mouse
#define mouse uMouse
#endif
#ifndef iGlobalTime
#define iGlobalTime TIME
#endif
#ifndef iTime
#define iTime TIME
#endif
#ifndef iResolution
#define iResolution RENDERSIZE
#endif
#ifndef iMouse
#define iMouse vec4(uMouse,0.,0.)
#endif
#ifndef iTimeDelta
#define iTimeDelta 0.016
#endif
`;

  const hasTimeScale = /\buniform\s+float\s+timeScale\s*[;=]|\bfloat\s+timeScale\s*[=;]|\b#define\s+timeScale\b/.test(body);
  const hasMouseX = /\buniform\s+float\s+mouseX\s*[;=]|\bfloat\s+mouseX\s*[=;]|\b#define\s+mouseX\b/.test(body);
  const hasMouseY = /\buniform\s+float\s+mouseY\s*[;=]|\bfloat\s+mouseY\s*[=;]|\b#define\s+mouseY\b/.test(body);
  const isfRendersizeDecl = needsRendersize ? 'uniform vec2 RENDERSIZE;\n' : '';
  let isfPreamble = `uniform float TIME;
` + isfRendersizeDecl + `uniform float FRAMEINDEX;
uniform float iFrame;
uniform bool useFrameIndex;
uniform float fps;
`;
  if (!hasTimeScale) isfPreamble += 'uniform float timeScale;\n';
  if (!hasMouseX) isfPreamble += 'uniform float mouseX;\n';
  if (!hasMouseY) isfPreamble += 'uniform float mouseY;\n';
  if (hasISF) isfPreamble += isfInputsToUniforms(body);

  const parts: string[] = [];
  if (extensions.length) parts.push(extensions.join('\n'));
  parts.push('precision highp float;');

  const usesIFrame = /\biFrame\b/.test(body);
  const declaresIFrame = /uniform\s+float\s+iFrame\s*[;=]/.test(body);
  const needsIFrame = usesIFrame && !declaresIFrame;
  const rendersizeBlock = needsRendersize ? `uniform vec2 RENDERSIZE;
#ifndef resolution
#define resolution RENDERSIZE
#endif
#ifndef iResolution
#define iResolution RENDERSIZE
#endif
` : '';
  const iFrameLine = needsIFrame ? 'uniform float iFrame;\n' : '';

  let rest = body;
  if (hasISF && !hasOurPreamble) {
    rest = stripDuplicateUniformDecls(rest, isfPreamble);
    const insert = rest.indexOf('*/') >= 0 && rest.indexOf('*/') < 800 ? rest.indexOf('*/') + 2 : 0;
    rest = rest.slice(0, insert) + '\n' + isfPreamble + (discoveredUniforms ? discoveredUniforms : '') + rest.slice(insert);
  } else if (needsPreamble) {
    const insert = rest.indexOf('*/') >= 0 && rest.indexOf('*/') < 500 ? rest.indexOf('*/') + 2 : 0;
    rest = rest.slice(0, insert) + '\n' + preamble + rest.slice(insert);
  } else {
    const insert = rest.indexOf('*/') >= 0 && rest.indexOf('*/') < 500 ? rest.indexOf('*/') + 2 : 0;
    const toAdd = rendersizeBlock + iFrameLine + (discoveredUniforms || '');
    if (toAdd) rest = rest.slice(0, insert) + '\n' + toAdd + rest.slice(insert);
  }

  parts.push(rest);
  return parts.filter(Boolean).join('\n');
}

export function prepareFragmentForOffscreenRender(src: string): string {
  let body = src || '';
  body = body.replace(/#\s*(extension|version)\s+[^\n]+/g, '');
  body = body.replace(/\n\s*\n\s*\n/g, '\n\n').trim();
  const hasSandboxUniforms = /uniform\s+float\s+time\s*;/.test(body) || /uniform\s+vec2\s+(mouse|resolution)\s*;/.test(body);
  const hasISF = /"INPUTS"\s*:/.test(body) && (/\bTIME\b/.test(body) || /\bRENDERSIZE\b/.test(body));
  const hasOurPreamble = /uniform\s+float\s+TIME\s*;/.test(body);
  const needsPreamble = !hasOurPreamble && !hasSandboxUniforms;
  body = body.replace(/\s*precision\s+(lowp|mediump|highp)\s+float\s*;\s*/gi, '\n');
  const usesRendersize = /\bRENDERSIZE\b|\bresolution\b|\biResolution\b/.test(body);
  const declaresRendersize = /uniform\s+vec2\s+RENDERSIZE\s*[;=]/.test(body) || /uniform\s+vec2\s+resolution\s*[;=]/.test(body) || /#define\s+RENDERSIZE\s+/.test(body);
  if (usesRendersize && declaresRendersize) {
    body = body.replace(/^\s*uniform\s+vec2\s+RENDERSIZE\s*[;=]\s*\n?/gm, '');
    body = body.replace(/^\s*uniform\s+vec2\s+resolution\s*[;=]\s*\n?/gm, '');
    body = body.replace(/^\s*#ifndef\s+RENDERSIZE\s*\n#define\s+RENDERSIZE\s+[^\n]+\n#endif\s*\n?/gm, '');
  }
  const needsRendersize = usesRendersize;
  const rendersizeDecl = needsRendersize ? 'uniform vec2 RENDERSIZE;\n' : '';
  const preamble = `uniform float TIME;
` + rendersizeDecl + `uniform float uTimeScale;
uniform vec2 uMouse;
uniform float iFrame;
#ifndef time
#define time (TIME * uTimeScale)
#endif
#ifndef resolution
#define resolution RENDERSIZE
#endif
#ifndef mouse
#define mouse uMouse
#endif
#ifndef iGlobalTime
#define iGlobalTime TIME
#endif
#ifndef iTime
#define iTime TIME
#endif
#ifndef iResolution
#define iResolution RENDERSIZE
#endif
#ifndef iMouse
#define iMouse vec4(uMouse,0.,0.)
#endif
#ifndef iTimeDelta
#define iTimeDelta 0.016
#endif
`;
  const hasTimeScale = /\buniform\s+float\s+timeScale\s*[;=]|\bfloat\s+timeScale\s*[=;]|\b#define\s+timeScale\b/.test(body);
  const hasMouseX = /\buniform\s+float\s+mouseX\s*[;=]|\bfloat\s+mouseX\s*[=;]|\b#define\s+mouseX\b/.test(body);
  const hasMouseY = /\buniform\s+float\s+mouseY\s*[;=]|\bfloat\s+mouseY\s*[=;]|\b#define\s+mouseY\b/.test(body);
  let isfPreamble = `uniform float TIME;
` + (needsRendersize ? 'uniform vec2 RENDERSIZE;\n' : '') + `uniform float FRAMEINDEX;
uniform float iFrame;
uniform bool useFrameIndex;
uniform float fps;
`;
  if (!hasTimeScale) isfPreamble += 'uniform float timeScale;\n';
  if (!hasMouseX) isfPreamble += 'uniform float mouseX;\n';
  if (!hasMouseY) isfPreamble += 'uniform float mouseY;\n';
  if (hasISF) isfPreamble += isfInputsToUniforms(body);
  const usesIFrame = /\biFrame\b/.test(body);
  const declaresIFrame = /uniform\s+float\s+iFrame\s*[;=]/.test(body);
  const needsIFrame = usesIFrame && !declaresIFrame;
  const rendersizeBlock = needsRendersize ? `uniform vec2 RENDERSIZE;
#ifndef resolution
#define resolution RENDERSIZE
#endif
#ifndef iResolution
#define iResolution RENDERSIZE
#endif
` : '';
  const iFrameLine = needsIFrame ? 'uniform float iFrame;\n' : '';
  let rest = body;
  if (hasISF && !hasOurPreamble) {
    rest = stripDuplicateUniformDecls(rest, isfPreamble);
    const insert = rest.indexOf('*/') >= 0 && rest.indexOf('*/') < 800 ? rest.indexOf('*/') + 2 : 0;
    rest = rest.slice(0, insert) + '\n' + isfPreamble + rest.slice(insert);
  } else if (needsPreamble) {
    const insert = rest.indexOf('*/') >= 0 && rest.indexOf('*/') < 500 ? rest.indexOf('*/') + 2 : 0;
    rest = rest.slice(0, insert) + '\n' + preamble + rest.slice(insert);
  } else {
    const insert = rest.indexOf('*/') >= 0 && rest.indexOf('*/') < 500 ? rest.indexOf('*/') + 2 : 0;
    const toAdd = rendersizeBlock + iFrameLine;
    if (toAdd) rest = rest.slice(0, insert) + '\n' + toAdd + rest.slice(insert);
  }
  return 'precision highp float;\n' + rest;
}

function processExternalInputs(): void {
  if (audioEngine.active) {
    for (const bandIdxStr of Object.keys(audioEngine.bandParamMap)) {
      const bandIdx = parseInt(bandIdxStr, 10);
      const paramName = audioEngine.bandParamMap[bandIdx];
      if (!paramName || audioEngine.bands[bandIdx] === undefined) continue;
      const meta = currentParamsMeta.find((p) => p.id === paramName);
      if (!meta) continue;
      const norm = audioEngine.bands[bandIdx];
      const scaled = meta.min + norm * (meta.max - meta.min);
      const clamped = Math.min(meta.max, Math.max(meta.min, scaled));
      const inp = document.querySelector('input[data-param="' + paramName + '"]') as HTMLInputElement | null;
      if (meta.type === 'bool') {
        paramValues[paramName] = clamped > 0.5;
        if (inp && inp.type === 'checkbox') {
          inp.checked = clamped > 0.5;
          const row = inp.closest('.param-row');
          const valEl = row?.querySelector('.val');
          if (valEl) valEl.textContent = (clamped > 0.5) ? '1' : '0';
        }
      } else {
        paramValues[paramName] = clamped;
        if (inp && inp.type === 'range') {
          inp.value = String(clamped);
          const row = inp.closest('.param-row');
          const valEl = row?.querySelector('.val');
          if (valEl) valEl.textContent = clamped.toFixed(2);
        }
      }
    }
  }
  if (midiEngine.active) {
    for (const paramName of Object.keys(midiEngine.pendingValues)) {
      const meta = currentParamsMeta.find((p) => p.id === paramName);
      if (!meta) continue;
      const norm = midiEngine.pendingValues[paramName];
      const scaled = meta.min + norm * (meta.max - meta.min);
      const clamped = Math.min(meta.max, Math.max(meta.min, scaled));
      const inp = document.querySelector('input[data-param="' + paramName + '"]') as HTMLInputElement | null;
      if (meta.type === 'bool') {
        paramValues[paramName] = clamped > 0.5;
        if (inp && inp.type === 'checkbox') {
          inp.checked = clamped > 0.5;
          const row = inp.closest('.param-row');
          const valEl = row?.querySelector('.val');
          if (valEl) valEl.textContent = (clamped > 0.5) ? '1' : '0';
        }
      } else {
        paramValues[paramName] = clamped;
        if (inp && inp.type === 'range') {
          inp.value = String(clamped);
          const row = inp.closest('.param-row');
          const valEl = row?.querySelector('.val');
          if (valEl) valEl.textContent = clamped.toFixed(2);
        }
      }
    }
    midiEngine.pendingValues = {};
  }
  if (oscEngine.active) {
    for (const paramName of Object.keys(oscEngine.pendingValues)) {
      const meta = currentParamsMeta.find((p) => p.id === paramName);
      if (!meta) continue;
      const norm = oscEngine.pendingValues[paramName];
      const scaled = meta.min + norm * (meta.max - meta.min);
      const clamped = Math.min(meta.max, Math.max(meta.min, scaled));
      const inp = document.querySelector('input[data-param="' + paramName + '"]') as HTMLInputElement | null;
      if (meta.type === 'bool') {
        paramValues[paramName] = clamped > 0.5;
        if (inp && inp.type === 'checkbox') {
          inp.checked = clamped > 0.5;
          const row = inp.closest('.param-row');
          const valEl = row?.querySelector('.val');
          if (valEl) valEl.textContent = (clamped > 0.5) ? '1' : '0';
        }
      } else {
        paramValues[paramName] = clamped;
        if (inp && inp.type === 'range') {
          inp.value = String(clamped);
          const row = inp.closest('.param-row');
          const valEl = row?.querySelector('.val');
          if (valEl) valEl.textContent = clamped.toFixed(2);
        }
      }
    }
    oscEngine.pendingValues = {};
  }
}

function compileShader(glCtx: WebGLRenderingContext, type: number, src: string): WebGLShader {
  const s = glCtx.createShader(type);
  if (!s) throw new Error('createShader failed');
  glCtx.shaderSource(s, src);
  glCtx.compileShader(s);
  if (!glCtx.getShaderParameter(s, glCtx.COMPILE_STATUS)) {
    const log = glCtx.getShaderInfoLog(s);
    glCtx.deleteShader(s);
    throw new Error(log || 'compile failed');
  }
  return s;
}

function createProgram(glCtx: WebGLRenderingContext, fragSrc: string): WebGLProgram {
  const v = compileShader(glCtx, glCtx.VERTEX_SHADER, vertSrc);
  const fullFrag = addWebGLUniforms(fragSrc);
  const f = compileShader(glCtx, glCtx.FRAGMENT_SHADER, fullFrag);
  const p = glCtx.createProgram();
  if (!p) throw new Error('createProgram failed');
  glCtx.attachShader(p, v);
  glCtx.attachShader(p, f);
  glCtx.linkProgram(p);
  glCtx.deleteShader(v);
  glCtx.deleteShader(f);
  if (!glCtx.getProgramParameter(p, glCtx.LINK_STATUS)) {
    const log = glCtx.getProgramInfoLog(p);
    glCtx.deleteProgram(p);
    throw new Error(log || 'link failed');
  }
  const n = glCtx.getProgramParameter(p, glCtx.ACTIVE_UNIFORMS) as number;
  programUniformNames = new Set();
  programSamplerNames = [];
  const SAMPLER2D = 35678;
  for (let i = 0; i < n; i++) {
    const u = glCtx.getActiveUniform(p, i);
    if (u && u.name) {
      programUniformNames.add(u.name);
      if (u.type === SAMPLER2D) programSamplerNames.push(u.name);
    }
  }
  return p;
}

function translateToFriendlyError(err: string): string | null {
  if (/Illegal character at fieldname start/i.test(err) && /'-'/.test(err)) {
    const m = err.match(/0:(\d+):/);
    const ln = m ? ' at line ' + m[1] : '';
    return 'Float or expression followed by minus without space' + ln + '.\n\nE.g. "3.-x" is parsed as float "3." then dot (field access) then "-". Use "3.0 - x" or add a space: "3. - x". Click Fix to try auto-repair.';
  }
  if (/divide by zero|array index out of range|array index out of bounds/i.test(err)) return null;
  return null;
}

function simplifyCompileErrorForDisplay(err: string): string {
  const friendly = translateToFriendlyError(err);
  if (friendly) return friendly;
  const lines = err.trim().split('\n').filter((l) => l.trim());
  if (lines.length <= 6) return err;
  const hints: string[] = [];
  const seen: Record<string, number[]> = {};
  for (const line of lines) {
    const m = line.match(/0:(\d+):/);
    const ln = m ? parseInt(m[1], 10) : 0;
    const raw = line.replace(/^\s*ERROR:\s*\d+:\d+:\s*/, '').trim();
    let group = raw;
    if (/uniform.*global scope|uniform.*Local variables|uniform.*invalid qualifier/i.test(raw)) {
      group = 'uniform in wrong place';
    } else if (/redefinition/i.test(raw)) group = 'redefinition';
    else if (/loop.*non-constant|Loop index/i.test(raw)) group = 'loop non-constant';
    else if (raw.length > 50) group = raw.slice(0, 50) + '...';
    if (!seen[group]) seen[group] = [];
    if (ln && !seen[group].includes(ln)) seen[group].push(ln);
  }
  for (const [msg, lns] of Object.entries(seen)) {
    lns.sort((a, b) => a - b);
    const range = lns.length > 4 ? lns[0] + '-' + lns[lns.length - 1] : lns.join(',');
    hints.push('L' + range + ': ' + msg);
  }
  let out = hints.slice(0, 5).join('\n');
  if (lines.length > 10) {
    if (/uniform in wrong place/i.test(out)) {
      out += '\n\nFix: Move all uniform declarations to top of file, outside main().';
    } else if (/redefinition/i.test(out)) {
      out += '\n\nFix: Remove duplicate #define or variable; use #ifndef for guards.';
    }
  }
  return out;
}

export function stripLeadingGarbage(src: string): string {
  if (!src || typeof src !== 'string') return src;
  const lines = src.split('\n');
  let i = 0;
  while (i < lines.length) {
    const t = lines[i].trim();
    if (t === '') { i++; continue; }
    if (/^#|^precision\s|^\/\/|^\/\*|^uniform\s|^varying\s|^attribute\s|^void\s|^const\s|^layout\s|^in\s|^out\s|^flat\s|^smooth\s|^float\s|^vec[234]\s|^mat[234]\s|^int\s|^bool\s|^sampler2D\s|^if\s|^for\s|^while\s|^return\s|^discard\s|^struct\s|^\{\s*$/i.test(t)) break;
    if (/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(t) && t.length > 10) {
      i++;
      continue;
    }
    break;
  }
  if (i === 0) return src;
  return lines.slice(i).join('\n');
}

function hasMain(s: string): boolean {
  const t = s.trim();
  return t.length > 0 && (t.includes('void main(') || t.includes('main()'));
}

export function render(fragSrc: string): void {
  if (!gl) return;
  const src = stripLeadingGarbage(fragSrc || '');
  const emptyOrNoMain = src.trim() === '' || !hasMain(src);
  if (emptyOrNoMain) {
    if (program) {
      gl.deleteProgram(program);
      program = null;
    }
    const msg = "Empty shader or missing main(). Add code or trash this shader.";
    status(msg, true);
    setLastCompileError(msg, currentEntry?.path ?? '');
    const fixBtn = el('fixBtn');
    if (fixBtn) (fixBtn as HTMLButtonElement).style.display = '';
    const overlay = el('previewCompileErrorOverlay');
    const errMsg = el('previewErrorMsg');
    if (overlay) (overlay as HTMLElement).style.display = 'flex';
    const errPathEl = el('previewErrorPath');
    if (errPathEl) errPathEl.textContent = currentEntry?.path ? 'Shader: ' + currentEntry.path : '(no shader loaded)';
    if (errMsg) (errMsg as HTMLElement).textContent = msg;
    const irrepEl = document.getElementById('previewErrorIrrep');
    if (irrepEl) (irrepEl as HTMLElement).style.display = 'none';
    return;
  }
  try {
    if (program) {
      gl.deleteProgram(program);
      program = null;
    }
    program = createProgram(gl, src);
  } catch (e) {
    let msg = e instanceof Error ? e.message : String(e);
    if (/Fsqrt|no matching overloaded/i.test(msg)) {
      const lineMatch = msg.match(/:(\d+):/);
      const lineHint = lineMatch ? ' at line ' + lineMatch[1] : '';
      msg += ' Tip: Use sqrt() not Fsqrt. Check for typos' + lineHint;
    }
    const lineCount = msg.split('\n').filter((l) => l.trim()).length;
    const firstLine = msg.split('\n')[0] || '';
    const path = currentEntry?.path ?? '';
    const pathPrefix = path ? path + ': ' : '(no shader) ';
    const statusMsg = pathPrefix + (lineCount > 8 ? lineCount + ' compile errors - click Fix' : firstLine.slice(0, 120));
    status(statusMsg, true);
    setLastCompileError(msg, path);
    const fixBtn = el('fixBtn');
    if (fixBtn) { (fixBtn as HTMLButtonElement).style.display = ''; }
    const overlay = el('previewCompileErrorOverlay');
    const errMsg = el('previewErrorMsg');
    if (overlay) (overlay as HTMLElement).style.display = 'flex';
    const errPathEl = el('previewErrorPath');
    if (errPathEl) errPathEl.textContent = path ? 'Shader: ' + path : '(no shader loaded - may be empty or from index)';
    if (errMsg) {
      const friendly = translateToFriendlyError(msg);
      errMsg.textContent = friendly || (lineCount > 6 ? simplifyCompileErrorForDisplay(msg) : msg);
    }
    const irrepEl = document.getElementById('previewErrorIrrep');
    const isIrrep = /divide by zero|array index out of range|array index out of bounds/i.test(msg);
    if (irrepEl) (irrepEl as HTMLElement).style.display = isIrrep ? 'block' : 'none';
    return;
  }
  clearLastCompileError();
  setPendingCursorConfirm(false);
  try {
    const prepared = prepareFragmentForOffscreenRender(src);
    lastPreparedPreviewSource = prepared;
    previewChannel.postMessage({ type: 'shader', preparedSource: prepared, meta: [] });
  } catch (_) {}
  const fixBtn = el('fixBtn');
  if (fixBtn) { (fixBtn as HTMLButtonElement).style.display = 'none'; }
  const overlay = el('previewCompileErrorOverlay');
  if (overlay) {
    (overlay as HTMLElement).style.display = 'none';
    delete (overlay as HTMLElement).dataset.unrecoverable;
  }
  gl.useProgram(program);
  paramUniformLocations.clear();
  function setLocForUniform(uniformName: string, loc: WebGLUniformLocation): void {
    paramUniformLocations.set(uniformName, loc);
    for (const paramId of Object.keys(paramToUniformMap)) {
      const u = paramToUniformMap[paramId];
      if (u && u.toLowerCase() === uniformName.toLowerCase()) paramUniformLocations.set(paramId, loc);
    }
    for (const paramId of Object.keys(paramValues)) {
      if (['timeScale', 'mouseX', 'mouseY'].includes(paramId)) continue;
      const u = paramToUniformMap[paramId] ?? paramId;
      if (u.toLowerCase() === uniformName.toLowerCase()) paramUniformLocations.set(paramId, loc);
    }
  }
  for (const paramId of Object.keys(paramToUniformMap)) {
    const candidate = paramToUniformMap[paramId] ?? paramId;
    let uniformName = candidate;
    if (!programUniformNames.has(candidate)) {
      const lower = candidate.toLowerCase();
      const found = [...programUniformNames].find((n) => n.toLowerCase() === lower);
      if (!found) continue;
      uniformName = found;
    }
    const loc = gl.getUniformLocation(program!, uniformName);
    if (loc) setLocForUniform(uniformName, loc);
  }
  for (const paramId of Object.keys(paramValues)) {
    if (['timeScale', 'mouseX', 'mouseY'].includes(paramId)) continue;
    if (paramUniformLocations.has(paramId)) continue;
    const candidate = paramToUniformMap[paramId] ?? paramId;
    let uniformName = candidate;
    if (!programUniformNames.has(candidate)) {
      const lower = candidate.toLowerCase();
      const found = [...programUniformNames].find((n) => n.toLowerCase() === lower);
      if (!found) continue;
      uniformName = found;
    }
    const loc = gl.getUniformLocation(program!, uniformName);
    if (loc) setLocForUniform(uniformName, loc);
  }
  const builtInUniforms = new Set(['time', 'mouse', 'resolution', 'TIME', 'RENDERSIZE', 'uTimeScale', 'uMouse', 'FRAMEINDEX', 'iFrame', 'mouseX', 'mouseY', 'useFrameIndex', 'fps', 'timeScale']);
  for (const uniformName of programUniformNames) {
    if (builtInUniforms.has(uniformName)) continue;
    if (paramUniformLocations.has(uniformName)) continue;
    const loc = gl.getUniformLocation(program!, uniformName);
    if (loc) setLocForUniform(uniformName, loc);
  }
  const buf = gl.createBuffer();
  gl.bindBuffer(gl.ARRAY_BUFFER, buf);
  gl.bufferData(gl.ARRAY_BUFFER, quadVerts, gl.STATIC_DRAW);
  const loc = gl.getAttribLocation(program!, 'a_pos');
  gl.enableVertexAttribArray(loc);
  gl.vertexAttribPointer(loc, 2, gl.FLOAT, false, 0, 0);

  const builtInLocs = new Map<string, WebGLUniformLocation | null>();
  for (const name of ['TIME', 'RENDERSIZE', 'uTimeScale', 'uMouse', 'time', 'mouse', 'resolution', 'FRAMEINDEX', 'iFrame', 'useFrameIndex', 'fps', 'timeScale', 'mouseX', 'mouseY']) {
    builtInLocs.set(name, gl.getUniformLocation(program!, name));
  }

  const targetFps = Math.min(60, Math.max(24, appSettings.targetFps || 30));
  const frameInterval = 1000 / targetFps;
  let nextFrameTime = 0;
  let loopFrameCount = 0;

  const loop = (t: number): void => {
    if (!document.hidden) rafId = requestAnimationFrame(loop);
    if (!program || !gl) return;
    if (nextFrameTime === 0) nextFrameTime = t;
    if (t < nextFrameTime) return;
    nextFrameTime = t + frameInterval;
    loopFrameCount++;

    if (audioEngine.active) audioEngine.update();
    processExternalInputs();

    const w = gl.drawingBufferWidth;
    const h = gl.drawingBufferHeight;
    gl.viewport(0, 0, w, h);
    gl.clearColor(0.05, 0.05, 0.08, 1);
    gl.clear(gl.COLOR_BUFFER_BIT);

    const ts = (paramValues.timeScale as number) ?? 1;
    const mx = (paramValues.mouseX as number) ?? 0.5;
    const my = (paramValues.mouseY as number) ?? 0.5;
    const timeVal = (t - startTime) / 1000 * ts;
    const timeLoc = builtInLocs.get('TIME');
    if (timeLoc) gl.uniform1f(timeLoc, (t - startTime) / 1000);
    const resLoc = builtInLocs.get('RENDERSIZE');
    if (resLoc) gl.uniform2f(resLoc, w, h);
    const scaleLoc = builtInLocs.get('uTimeScale');
    if (scaleLoc) gl.uniform1f(scaleLoc, ts);
    const mouseLoc = builtInLocs.get('uMouse');
    if (mouseLoc) gl.uniform2f(mouseLoc, mx, my);
    const t2 = builtInLocs.get('time');
    if (t2) gl.uniform1f(t2, timeVal);
    const m2 = builtInLocs.get('mouse');
    if (m2) gl.uniform2f(m2, mx, my);
    const r2 = builtInLocs.get('resolution');
    if (r2) gl.uniform2f(r2, w, h);
    const frameLoc = builtInLocs.get('FRAMEINDEX');
    if (frameLoc) gl.uniform1f(frameLoc, loopFrameCount);
    const iFrameLoc = builtInLocs.get('iFrame');
    if (iFrameLoc) gl.uniform1f(iFrameLoc, loopFrameCount);
    const useFrameLoc = builtInLocs.get('useFrameIndex');
    if (useFrameLoc) gl.uniform1i(useFrameLoc, (paramValues.useFrameIndex as boolean) ?? false ? 1 : 0);
    const fpsLoc = builtInLocs.get('fps');
    if (fpsLoc) gl.uniform1f(fpsLoc, targetFps);
    const tsLoc = builtInLocs.get('timeScale');
    if (tsLoc) gl.uniform1f(tsLoc, ts);
    const mxLoc = builtInLocs.get('mouseX');
    if (mxLoc) gl.uniform1f(mxLoc, mx);
    const myLoc = builtInLocs.get('mouseY');
    if (myLoc) gl.uniform1f(myLoc, my);
    bindSamplerTextures(gl, program!);
    for (const k of Object.keys(paramValues)) {
      if (['timeScale', 'mouseX', 'mouseY'].includes(k)) continue;
      let loc = paramUniformLocations.get(k);
      if (!loc) {
        const uniformName = paramToUniformMap[k] ?? k;
        loc = paramUniformLocations.get(uniformName);
        if (!loc && uniformName !== k) {
          const lower = k.toLowerCase();
          for (const [key, loc2] of paramUniformLocations) {
            if (key.toLowerCase() === lower) { loc = loc2; break; }
          }
        }
      }
      if (!loc) continue;
      const v = paramValues[k];
      if (typeof v === 'boolean') gl.uniform1i(loc, v ? 1 : 0);
      else if (typeof v === 'number') gl.uniform1f(loc, v);
    }
    gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);

    for (const dev of roliblockManager.getDevices()) {
      if (dev.enabled && gl.canvas) dev.sampleAndSendLed(gl.canvas as HTMLCanvasElement);
    }

    if (loopFrameCount % 2 === 0) {
      const frameData = { type: 'frame' as const, params: { ...paramValues }, mouseX: mx, mouseY: my };
      const json = JSON.stringify(frameData);
      if (json !== lastPreviewParamsJson) {
        lastPreviewParamsJson = json;
        previewChannel.postMessage(frameData);
      }
    }
    if (audioEngine.active) {
      const fftCanvas = el('fftCanvas') as HTMLCanvasElement | null;
      if (fftCanvas) audioEngine.drawFFT(fftCanvas);
    }
    sendMacroCamFrame();

    if (pendingThumbnailPath && currentEntry?.path === pendingThumbnailPath && pendingThumbnailStartTime > 0 && (performance.now() - pendingThumbnailStartTime) >= THUMB_CAPTURE_DELAY_MS && w > 0 && h > 0) {
      const pathToCapture = pendingThumbnailPath;
      pendingThumbnailPath = null;
      pendingThumbnailStartTime = 0;
      const doCapture = () => {
        try {
          const dataUrl = captureThumbnailDataUrl(gl!.canvas as HTMLCanvasElement);
          if (dataUrl) {
            setThumbnail(pathToCapture, dataUrl);
            postThumbnailSave({ path: pathToCapture, dataUrl }).catch(() => {});
            window.dispatchEvent(new CustomEvent('thumbnail-captured', { detail: { path: pathToCapture } }));
          }
        } catch (_) {}
      };
      if (typeof requestIdleCallback === 'function') {
        requestIdleCallback(doCapture, { timeout: 500 });
      } else {
        setTimeout(doCapture, 50);
      }
    }
  };
  startTime = performance.now();
  if (rafId) cancelAnimationFrame(rafId);
  rafId = requestAnimationFrame(loop);

  // Keep rendering when tab is hidden (for LED streaming to Roli devices)
  if (!bgIntervalId) {
    document.addEventListener('visibilitychange', () => {
      if (document.hidden) {
        if (!bgIntervalId) bgIntervalId = window.setInterval(() => loop(performance.now()), 33);
      } else {
        if (bgIntervalId) { clearInterval(bgIntervalId); bgIntervalId = 0; }
        if (rafId) cancelAnimationFrame(rafId);
        rafId = requestAnimationFrame(loop);
      }
    });
  }

  const path = currentEntry?.path;
  if (path && !getThumbnail(path)) {
    pendingThumbnailPath = path;
    pendingThumbnailStartTime = performance.now();
  }
}

function captureThumbnailDataUrl(canvas: HTMLCanvasElement): string | null {
  if (!canvas || canvas.width <= 0 || canvas.height <= 0) return null;
  const quality = appSettings.thumbnailQuality ?? 0.5;
  const maxSize = appSettings.thumbnailMaxSize ?? 120;
  const srcW = canvas.width;
  const srcH = canvas.height;
  let targetCanvas: HTMLCanvasElement = canvas;
  if (maxSize > 0 && (srcW > maxSize || srcH > maxSize)) {
    const scale = Math.min(maxSize / srcW, maxSize / srcH);
    const tw = Math.max(1, Math.floor(srcW * scale));
    const th = Math.max(1, Math.floor(srcH * scale));
    const off = document.createElement('canvas');
    off.width = tw;
    off.height = th;
    const ctx = off.getContext('2d');
    if (ctx) {
      ctx.drawImage(canvas, 0, 0, srcW, srcH, 0, 0, tw, th);
      targetCanvas = off;
    }
  }
  return targetCanvas.toDataURL('image/jpeg', quality);
}

function scheduleThumbnailCapture(path: string, delayMs: number): void {
  setTimeout(() => {
    try {
      if (gl && program && gl.canvas) {
        const canvas = gl.canvas as HTMLCanvasElement;
        const dataUrl = captureThumbnailDataUrl(canvas);
        if (dataUrl) {
          setThumbnail(path, dataUrl);
          postThumbnailSave({ path, dataUrl }).catch(() => {});
          window.dispatchEvent(new CustomEvent('thumbnail-captured', { detail: { path } }));
        }
      }
    } catch (_) {}
  }, delayMs);
}

export function capturePreviewScreenshot(): string | null {
  if (!gl || !gl.canvas) return null;
  try {
    return (gl.canvas as HTMLCanvasElement).toDataURL('image/png');
  } catch {
    return null;
  }
}

export function captureThumbnailNow(): void {
  const path = currentEntry?.path;
  if (!path || !gl || !program || !gl.canvas) return;
  status('Capturing thumbnail in 5s...');
  setTimeout(() => {
    try {
      if (!gl || !program || !gl.canvas || currentEntry?.path !== path) return;
      const dataUrl = captureThumbnailDataUrl(gl.canvas as HTMLCanvasElement);
      if (dataUrl) {
        setThumbnail(path, dataUrl);
        postThumbnailSave({ path, dataUrl }).catch(() => {});
        window.dispatchEvent(new CustomEvent('thumbnail-captured', { detail: { path } }));
        status('Thumbnail captured');
      }
    } catch (_) {}
  }, THUMB_CAPTURE_DELAY_MS);
}

export function clearSessionForNewShader(): void {
  clearLastCompileError();
  setPendingCursorConfirm(false);
  clearParamsForNewShader();
  const fixBtn = el('fixBtn');
  if (fixBtn) (fixBtn as HTMLButtonElement).style.display = 'none';
  const overlay = el('previewCompileErrorOverlay');
  if (overlay) {
    (overlay as HTMLElement).style.display = 'none';
    delete (overlay as HTMLElement).dataset.unrecoverable;
  }
  const agentOut = document.getElementById('agentOutput');
  if (agentOut) {
    (agentOut as HTMLElement).textContent = '';
    (agentOut as HTMLElement).style.display = 'none';
  }
  const agentPane = document.getElementById('agentOutputPane');
  if (agentPane) agentPane.classList.add('collapsed');
  const actions = document.getElementById('agentOutputActions');
  if (actions) (actions as HTMLElement).style.display = 'none';
}

export function stripCommentPollution(src: string): string {
  if (!src || typeof src !== 'string') return src;
  const lineByLine = src.split('\n').map((line) => {
    return line
      .replace(/\s*["']?\s*token\s*comment\s*["']?\s*>?\s*/gi, '')
      .replace(/^\s*["']?token\s*comment["']?\s*>?\s*/i, '')
      .replace(/^\s*>\s*\/\//, '//')
      .replace(/^\s*>\s*\/\*\s*\{/, ' /*{');
  }).join('\n');
  return lineByLine
    .replace(/\s*"token\s*comment"\s*>/gi, '')
    .replace(/<span\s+class="token\s+comment">/gi, '')
    .replace(/<span\s+class="shader-comment">/gi, '')
    .replace(/<span\s+class="[^"]*token[^"]*">/gi, '')
    .replace(/<\/span>/g, '')
    .replace(/"shader-comment">/g, '')
    .replace(/^\s*>\s*\/\//gm, '//')
    .replace(/\s*>\s*\/\*\s*\{/g, ' /*{');
}

export function loadShader(entry: IndexEntry | null | undefined): Promise<void> {
  const path = shaderPath(entry);
  if (!path) return Promise.resolve();
  // Capture current frame for transition before switching
  if (gl && gl.canvas) captureSnapshot(gl.canvas as HTMLCanvasElement);
  if (loadAbortController) loadAbortController.abort();
  loadAbortController = new AbortController();
  const signal = loadAbortController.signal;
  status('Loading...');
  setLastDiscoveredParams([]);
  const entryId = entry?.id;
  const entryPath = entry?.path;
  return fetchShader(path, { signal })
    .then((src) => {
      if (signal.aborted) return;
      if (currentEntry?.id !== entryId || currentEntry?.path !== entryPath) return;
      const cleaned = stripCommentPollution(src);
      setCurrentSource(cleaned);
      status(path);
      render(cleaned);
      playTransition();
      buildParamsPanel(entry);
      updateISFPanel();
      const sync = (globalThis as unknown as { syncCodeFromState?: () => void }).syncCodeFromState;
      if (typeof sync === 'function') sync();
      // Notify any listener (app bar, etc.) that the active shader changed.
      try {
        window.dispatchEvent(new CustomEvent('macroverse:shader-changed', {
          detail: { entry, path }
        }));
      } catch (_) { /* no-op */ }
    })
    .catch((e) => {
      if (e instanceof Error && e.name === 'AbortError') return;
      const msg = e instanceof Error ? e.message : String(e);
      status('load: ' + msg, true);
      setCurrentSource('');
      const sync = (globalThis as unknown as { syncCodeFromState?: () => void }).syncCodeFromState;
      if (typeof sync === 'function') sync();
      throw e;
    });
}

export function resizeCanvas(): void {
  const canvas = el('canvas');
  if (!canvas || !gl) return;
  const wrap = canvas.parentElement;
  if (!wrap) return;
  const res = appSettings.previewResolution;
  let w: number;
  let h: number;
  if (res && res !== 'auto') {
    const m = res.match(/^(\d+)\s*x\s*(\d+)$/i);
    if (m) {
      w = parseInt(m[1], 10) || 854;
      h = parseInt(m[2], 10) || 480;
    } else {
      w = appSettings.previewWidth ?? 854;
      h = appSettings.previewHeight ?? 480;
    }
  } else {
    w = Math.floor(wrap.clientWidth) || 854;
    h = Math.floor(wrap.clientHeight) || 480;
  }
  const scale = appSettings.previewQuality ?? 1;
  w = Math.max(1, Math.floor(w * scale));
  h = Math.max(1, Math.floor(h * scale));
  if (canvas.width !== w || canvas.height !== h) {
    canvas.width = w;
    canvas.height = h;
  }
}
