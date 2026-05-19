import { status } from './dom.js';
import { postShaderSave, postNativeScan, fetchTextTemplatesList, fetchTextTemplate } from './api.js';
import { showPathPicker } from './pathPicker.js';
import { appSettings } from './state.js';
import { loadSequence } from './init/loadSequence.js';
import { setCurrentEntry, setCurrentSource, entries } from './state.js';
import { loadShader } from './render.js';

const BLANK_GLSL = `precision highp float;
uniform float TIME;
uniform vec2 RENDERSIZE;
uniform vec2 uMouse;

void main() {
  vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
  float t = TIME;
  gl_FragColor = vec4(uv.x, uv.y, 0.5 + 0.5 * sin(t), 1.0);
}
`;

const BLANK_ISF = `/*{
  "CATEGORIES": ["Generator"],
  "DESCRIPTION": "New ISF shader",
  "INPUTS": []
}*/
precision highp float;
uniform float TIME;
uniform vec2 RENDERSIZE;

void main() {
  vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
  gl_FragColor = vec4(uv.x, uv.y, 0.5 + 0.5 * sin(TIME), 1.0);
}
`;

function shadertoyToMacroverse(paste: string): string {
  let s = paste.trim();
  s = s.replace(/\biResolution\b/g, 'RENDERSIZE');
  s = s.replace(/\biTime\b/g, 'TIME');
  s = s.replace(/\biTimeDelta\b/g, '0.016');
  s = s.replace(/\biFrame\b/g, '0');
  s = s.replace(/\biMouse\b/g, 'uMouse');
  s = s.replace(/\biChannelResolution\s*\[[^\]]*\]/g, 'RENDERSIZE');
  const mainImageMatch = s.match(/void\s+mainImage\s*\(\s*out\s+vec4\s+(\w+)\s*,\s*(?:in\s+)?vec2\s+(\w+)\s*\)\s*\{/);
  if (mainImageMatch) {
    const outVar = mainImageMatch[1];
    const coordVar = mainImageMatch[2];
    s = s.replace(/void\s+mainImage\s*\(\s*out\s+vec4\s+\w+\s*,\s*(?:in\s+)?vec2\s+\w+\s*\)\s*\{/, 'void main() { vec2 ' + coordVar + ' = gl_FragCoord.xy; vec4 ' + outVar + ';');
    const closeRe = new RegExp(outVar.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\s*;\\s*\\}\\s*$', 's');
    s = s.replace(closeRe, 'gl_FragColor = ' + outVar + ';\n}');
  }
  if (!/precision\s+highp\s+float\s*;/.test(s)) s = 'precision highp float;\n' + s;
  if (!/uniform\s+float\s+TIME\s*;/.test(s)) s = 'uniform float TIME;\nuniform vec2 RENDERSIZE;\nuniform vec2 uMouse;\n' + s;
  return s;
}

function glslSandboxToMacroverse(paste: string): string {
  let s = paste.trim();
  if (!/precision\s+highp\s+float\s*;/.test(s)) s = 'precision highp float;\n' + s;
  if (!/uniform\s+float\s+time\s*;/.test(s) && !/uniform\s+float\s+TIME\s*;/.test(s)) s = 'uniform float TIME;\nuniform vec2 RENDERSIZE;\nuniform vec2 uMouse;\n#ifndef time\n#define time (TIME * 1.0)\n#endif\n#ifndef resolution\n#define resolution RENDERSIZE\n#endif\n#ifndef mouse\n#define mouse uMouse\n#endif\n' + s;
  return s;
}

const TEXT_TEMPLATE_LETTERS = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';

function buildTextTemplateLetterSequence(message: string): string {
  const lines: string[] = [];
  const upper = (message || '').toUpperCase();
  for (let i = 0; i < upper.length; i++) {
    const c = upper[i];
    if (c === ' ') {
      lines.push('space(); add();');
    } else if (c === '\n') {
      lines.push('newline();');
    } else if (TEXT_TEMPLATE_LETTERS.includes(c)) {
      lines.push('d += ' + c + '(r());add();');
    }
  }
  if (lines.length === 0) lines.push('d += T(r());add(); d += E(r());add(); d += X(r());add(); d += T(r());add();');
  return lines.join(' ');
}

function injectTextIntoTemplateSource(source: string, message: string): string {
  const block = buildTextTemplateLetterSequence(message);
  const re = /(\n\s*)(float ti = floor\(time\/10\.0\);\s*\n\s*\/\/[^\n]*\n)([\s\S]*?)(\n\s*d = clamp\()/;
  const m = source.match(re);
  if (!m) return source;
  return source.replace(re, '$1$2\t' + block + '\n$4');
}

function buildCustomTextBlock(message: string): string {
  const lines = (message || '').toUpperCase().split('\n');
  const glslLines: string[] = [];
  const spacing = 0.55;
  const lineHeight = 1.0;
  const startY = 1.5 - ((lines.length - 1) * lineHeight) / 2;
  for (let row = 0; row < lines.length; row++) {
    const chars = lines[row].replace(/[^A-Z ]/g, '');
    if (!chars) continue;
    const y = (startY + row * lineHeight).toFixed(1);
    const totalWidth = chars.length * spacing;
    const startX = -totalWidth / 2;
    for (let i = 0; i < chars.length; i++) {
      const c = chars[i];
      if (c === ' ') continue;
      const x = (startX + i * spacing).toFixed(1);
      glslLines.push('\td = min(d,' + c + '(uv-vec2(' + x + ',' + y + ')));');
    }
  }
  if (glslLines.length === 0) {
    glslLines.push('\td = min(d,T(uv-vec2(-1.0,1.0)));');
    glslLines.push('\td = min(d,E(uv-vec2(-0.5,1.0)));');
    glslLines.push('\td = min(d,X(uv-vec2(0.0,1.0)));');
    glslLines.push('\td = min(d,T(uv-vec2(0.5,1.0)));');
  }
  return glslLines.join('\n');
}

function injectTextIntoCustomSource(source: string, message: string): string {
  const re = /(\bfloat d = 1\.0;\s*\n)([\s\S]*?)(\n\s*float w =)/;
  const m = source.match(re);
  if (!m) return source;
  const block = buildCustomTextBlock(message);
  return source.replace(re, '$1\n' + block + '\n$3');
}

function buildCenteredTextBlock(message: string): string {
  const lines = (message || '').toUpperCase().split('\n');
  const glslLines: string[] = [];
  const spacing = 0.5;
  const lineHeight = 0.65;
  const startY = -((lines.length - 1) * lineHeight) / 2;
  for (let row = 0; row < lines.length; row++) {
    const chars = lines[row].replace(/[^A-Z ]/g, '');
    if (!chars) continue;
    const y = (startY + row * lineHeight).toFixed(2);
    const totalWidth = chars.length * spacing;
    const startX = -totalWidth / 2;
    for (let i = 0; i < chars.length; i++) {
      const c = chars[i];
      if (c === ' ') continue;
      const x = (startX + i * spacing).toFixed(2);
      glslLines.push('\td = min(d,' + c + '(uv-vec2(' + x + ',' + y + ')));');
    }
  }
  if (glslLines.length === 0) {
    glslLines.push('\td = min(d,T(uv-vec2(-0.75,0.0)));');
    glslLines.push('\td = min(d,E(uv-vec2(-0.25,0.0)));');
    glslLines.push('\td = min(d,X(uv-vec2(0.25,0.0)));');
    glslLines.push('\td = min(d,T(uv-vec2(0.75,0.0)));');
  }
  return glslLines.join('\n');
}

function injectTextIntoNeonOrLcd(source: string, message: string): string {
  const re = /(\bfloat d = 1\.0;\s*\n)([\s\S]*?)(\n\s*float w =)/;
  const m = source.match(re);
  if (!m) return source;
  const block = buildCenteredTextBlock(message);
  return source.replace(re, '$1\n' + block + '\n$3');
}

function buildDotmatrixTextBlock(message: string): string {
  const lines = (message || '').toUpperCase().split('\n');
  const glslLines: string[] = [];
  const charW = 6.0;
  const lineH = 9.0;
  for (let row = 0; row < lines.length; row++) {
    const chars = lines[row].replace(/[^A-Z ]/g, '');
    if (!chars) continue;
    for (let i = 0; i < chars.length; i++) {
      const c = chars[i];
      if (c === ' ') continue;
      const x = (i * charW).toFixed(1);
      const y = (row * lineH).toFixed(1);
      glslLines.push('\td += ' + c + '(textUV - vec2(' + x + ', ' + y + '));');
    }
  }
  if (glslLines.length === 0) {
    glslLines.push('\td += T(textUV - vec2(0.0, 0.0));');
    glslLines.push('\td += E(textUV - vec2(6.0, 0.0));');
    glslLines.push('\td += X(textUV - vec2(12.0, 0.0));');
    glslLines.push('\td += T(textUV - vec2(18.0, 0.0));');
  }
  return glslLines.join('\n');
}

function injectTextIntoDotmatrix(source: string, message: string): string {
  const lines = (message || '').toUpperCase().split('\n');
  const maxLen = Math.max(...lines.map(l => l.replace(/[^A-Z ]/g, '').length), 1);
  const totalCharsLine = '\tfloat totalChars = ' + maxLen.toFixed(1) + ';';
  source = source.replace(/\bfloat totalChars = [\d.]+;/, 'float totalChars = ' + maxLen.toFixed(1) + ';');

  const re = /(\bfloat d = 0\.0;\s*\n)([\s\S]*?)(\n\s*float dot2)/;
  const m = source.match(re);
  if (!m) return source;
  const block = buildDotmatrixTextBlock(message);
  return source.replace(re, '$1\n' + block + '\n$3');
}

function build16segTextBlock(message: string): string {
  const lines = (message || '').toUpperCase().split('\n');
  const macroLines: string[] = [];
  for (let row = 0; row < lines.length; row++) {
    const chars = lines[row].replace(/[^A-Z0-9 \-+<>.]/g, '');
    if (!chars && row > 0) { macroLines.push('\tnl'); continue; }
    const tokens: string[] = [];
    for (const c of chars) {
      if (c === ' ') tokens.push('_');
      else if (c === '-') tokens.push('s_minus');
      else if (c === '+') tokens.push('s_plus');
      else if (c === '>') tokens.push('s_greater');
      else if (c === '<') tokens.push('s_less');
      else if (c === '.') tokens.push('s_dot');
      else if (c >= '0' && c <= '9') tokens.push('n' + c);
      else tokens.push(c);
    }
    if (tokens.length > 0) macroLines.push('\t' + tokens.join(' '));
    if (row < lines.length - 1) macroLines.push('\tnl');
  }
  if (macroLines.length === 0) macroLines.push('\tT E X T');
  return macroLines.join('\n');
}

function inject16seg(source: string, message: string): string {
  const lines = (message || '').toUpperCase().split('\n');
  const maxLen = Math.max(...lines.map(l => l.replace(/[^A-Z0-9 \-+<>.]/g, '').length), 1);
  const halfWidth = maxLen / 2;
  source = source.replace(
    /ch_start\s*=\s*vec2\s*\([^)]+\)/,
    'ch_start = vec2(ch_space.x * -' + halfWidth.toFixed(1) + ', ' + (lines.length * 1.5).toFixed(1) + ')'
  );
  const re = /(ch_color\s*=[^;]+;\s*\n)([\s\S]*?)(\n\s*vec3 color)/;
  const m = source.match(re);
  if (!m) return source;
  const block = build16segTextBlock(message);
  return source.replace(re, '$1' + block + '\n$3');
}

const styles = {
  overlay: 'position:fixed;inset:0;z-index:10002;background:rgba(0,0,0,0.7);display:flex;align-items:center;justify-content:center;',
  box: 'background:var(--amiga-panel);border:2px solid var(--amiga-copper);padding:20px;max-width:90vw;max-height:85vh;overflow:auto;min-width:360px;font-family:inherit;',
  row: 'display:flex;gap:8px;align-items:center;margin-bottom:10px;flex-wrap:wrap;',
  label: 'color:var(--amiga-copper);font-size:11px;text-transform:uppercase;margin-bottom:4px;display:block;',
  input: 'flex:1;min-width:180px;padding:8px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);font-size:12px;',
  btn: 'padding:6px 12px;font-size:11px;background:var(--amiga-surface);color:var(--amiga-accent);border:1px solid var(--bevel-dark);cursor:pointer;',
  textarea: 'width:100%;height:140px;padding:8px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);font-size:11px;font-family:monospace;resize:vertical;',
};

export function showCreateShaderModal(): void {
  const overlay = document.createElement('div');
  overlay.style.cssText = styles.overlay;
  overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };

  const box = document.createElement('div');
  box.style.cssText = styles.box;
  box.onclick = (e) => e.stopPropagation();

  const header = document.createElement('div');
  header.style.cssText = 'color:var(--amiga-copper);font-weight:bold;margin-bottom:16px;font-size:14px;';
  header.textContent = 'Create new ISF or GLSL shader';

  const templateLabel = document.createElement('label');
  templateLabel.style.cssText = styles.label;
  templateLabel.textContent = 'Template';
  const templateSelect = document.createElement('select');
  templateSelect.style.cssText = styles.input + ' max-width:100%;';
  templateSelect.innerHTML = '<option value="glsl">Blank GLSL</option><option value="isf">Blank ISF</option><option value="text">Text / Title / Banner</option><option value="shadertoy">Paste from Shadertoy</option><option value="sandbox">Paste from GLSLSandbox</option>';
  const pasteWrap = document.createElement('div');
  pasteWrap.style.cssText = 'margin-bottom:10px;display:none;';
  const pasteLabel = document.createElement('label');
  pasteLabel.style.cssText = styles.label;
  pasteLabel.textContent = 'Paste code (then click Create)';
  const pasteArea = document.createElement('textarea');
  pasteArea.style.cssText = styles.textarea;
  pasteArea.placeholder = 'Paste fragment shader code here...';
  pasteWrap.appendChild(pasteLabel);
  pasteWrap.appendChild(pasteArea);

  const textBuilderWrap = document.createElement('div');
  textBuilderWrap.style.cssText = 'margin-bottom:10px;display:none;';
  const textStyleLabel = document.createElement('label');
  textStyleLabel.style.cssText = styles.label;
  textStyleLabel.textContent = 'Style (from shaders/core/text)';
  const textStyleSelect = document.createElement('select');
  textStyleSelect.style.cssText = styles.input + ' max-width:100%;';
  textStyleSelect.innerHTML = '<option value="">Loading...</option>';
  const textMessageLabel = document.createElement('label');
  textMessageLabel.style.cssText = styles.label;
  textMessageLabel.textContent = 'Title / message (A-Z, spaces, newlines)';
  const textMessageArea = document.createElement('textarea');
  textMessageArea.style.cssText = styles.textarea + ' height:80px;';
  textMessageArea.placeholder = 'HELLO WORLD\nor banner text...';
  textBuilderWrap.appendChild(textStyleLabel);
  textBuilderWrap.appendChild(textStyleSelect);
  textBuilderWrap.appendChild(textMessageLabel);
  textBuilderWrap.appendChild(textMessageArea);
  fetchTextTemplatesList().then((list) => {
    textStyleSelect.innerHTML = list.length ? list.map((t) => '<option value="' + t.name + '">' + t.label + '</option>').join('') : '<option value="">No templates found</option>';
  }).catch(() => { textStyleSelect.innerHTML = '<option value="">Failed to load</option>'; });

  templateSelect.addEventListener('change', () => {
    const v = (templateSelect as HTMLSelectElement).value;
    (pasteWrap as HTMLElement).style.display = (v === 'shadertoy' || v === 'sandbox') ? 'block' : 'none';
    (textBuilderWrap as HTMLElement).style.display = v === 'text' ? 'block' : 'none';
  });

  const dirLabel = document.createElement('label');
  dirLabel.style.cssText = styles.label;
  dirLabel.textContent = 'Save in folder';
  const sourcePaths = appSettings.sourcePaths || [];
  const defaultDir = sourcePaths[0] || '';
  const dirInput = document.createElement('input');
  dirInput.type = 'text';
  dirInput.style.cssText = styles.input;
  dirInput.value = defaultDir;
  dirInput.placeholder = 'C:\\path\\to\\shaders';
  const browseBtn = document.createElement('button');
  browseBtn.type = 'button';
  browseBtn.textContent = 'Browse...';
  browseBtn.title = 'TLDR: Pick folder to save shader';
  browseBtn.style.cssText = styles.btn;
  browseBtn.onclick = () => {
    showPathPicker((selectedPath) => {
      if (selectedPath) dirInput.value = selectedPath;
    });
  };

  const nameLabel = document.createElement('label');
  nameLabel.style.cssText = styles.label;
  nameLabel.textContent = 'Filename';
  const nameInput = document.createElement('input');
  nameInput.type = 'text';
  nameInput.style.cssText = styles.input;
  nameInput.value = 'newShader.fs';
  nameInput.placeholder = 'myShader.fs';

  const btnRow = document.createElement('div');
  btnRow.style.cssText = styles.row + ' margin-top:16px;';
  const createBtn = document.createElement('button');
  createBtn.type = 'button';
  createBtn.textContent = 'Create';
  createBtn.title = 'TLDR: Create shader file and reindex';
  createBtn.style.cssText = 'padding:8px 16px;background:var(--amiga-accent);color:#fff;border:none;cursor:pointer;';
  const cancelBtn = document.createElement('button');
  cancelBtn.type = 'button';
  cancelBtn.textContent = 'Cancel';
  cancelBtn.title = 'TLDR: Cancel and close';
  cancelBtn.style.cssText = styles.btn;
  cancelBtn.onclick = () => overlay.remove();

  createBtn.onclick = async () => {
    const template = (templateSelect as HTMLSelectElement).value;
    const dir = (dirInput.value || '').trim().replace(/\|/g, '/').replace(/\\/g, '/');
    let name = (nameInput.value || '').trim().replace(/^[/\\]+/, '');
    if (!name) name = 'newShader.fs';
    if (!/\.(fs|frag|glsl|vert)$/i.test(name)) name = name + '.fs';
    let fullPath = dir ? dir.replace(/\/+$/, '') + '/' + name : name;

    if (!dir) {
      status('Choose a folder (Source path or Browse)', true);
      return;
    }

    createBtn.disabled = true;
    try {
      let content: string;
      if (template === 'glsl') content = BLANK_GLSL;
      else if (template === 'isf') content = BLANK_ISF;
      else if (template === 'text') {
        const styleName = (textStyleSelect as HTMLSelectElement).value;
        if (!styleName) {
          status('Pick a text style from the list', true);
          createBtn.disabled = false;
          return;
        }
        content = await fetchTextTemplate(styleName);
        const message = (textMessageArea.value || '').trim();
        if (message && styleName === 'core-text-template.fs') content = injectTextIntoTemplateSource(content, message);
        else if (message && styleName === 'core-text-custom.fs') content = injectTextIntoCustomSource(content, message);
        else if (message && styleName === 'core-text-neon.fs') content = injectTextIntoNeonOrLcd(content, message);
        else if (message && styleName === 'core-text-lcd.fs') content = injectTextIntoNeonOrLcd(content, message);
        else if (message && styleName === 'core-text-dotmatrix.fs') content = injectTextIntoDotmatrix(content, message);
        else if (message && styleName === 'core-text-16segment.fs') content = inject16seg(content, message);
        if (!name || name === 'newShader.fs') {
          const firstLine = (message.split('\n')[0] || '').replace(/[^\w\s-]/g, '').trim().slice(0, 24);
          name = (firstLine ? firstLine.replace(/\s+/g, '-') : 'text') + '.fs';
          fullPath = dir ? dir.replace(/\/+$/, '') + '/' + name : name;
        }
      } else if (template === 'shadertoy') content = shadertoyToMacroverse(pasteArea.value || '');
      else if (template === 'sandbox') content = glslSandboxToMacroverse(pasteArea.value || '');
      else content = BLANK_GLSL;

      await postShaderSave({ path: fullPath.replace(/\//g, '|'), content });
      status('Created. Reindexing...');
      overlay.remove();
      await postNativeScan();
      await loadSequence();
      const norm = (p: string) => p.replace(/\\/g, '/').toLowerCase();
      const entry = entries.find((e) => norm(e.path || '') === norm(fullPath));
      if (entry) {
        setCurrentEntry(entry);
        setCurrentSource(content);
        loadShader(entry);
        status('Created and opened: ' + name);
      } else {
        status('Created: ' + fullPath + '. Select it from the list.');
      }
    } catch (e) {
      status('Create failed: ' + (e as Error).message, true);
      createBtn.disabled = false;
    }
  };

  btnRow.appendChild(createBtn);
  btnRow.appendChild(cancelBtn);

  const dirRow = document.createElement('div');
  dirRow.style.cssText = styles.row;
  dirRow.appendChild(dirInput);
  dirRow.appendChild(browseBtn);

  box.appendChild(header);
  box.appendChild(templateLabel);
  box.appendChild(templateSelect);
  box.appendChild(pasteWrap);
  box.appendChild(textBuilderWrap);
  box.appendChild(dirLabel);
  box.appendChild(dirRow);
  box.appendChild(nameLabel);
  box.appendChild(nameInput);
  box.appendChild(btnRow);

  overlay.appendChild(box);
  document.body.appendChild(overlay);
}
