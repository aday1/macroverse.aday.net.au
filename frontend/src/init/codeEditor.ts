import { el, status, clampContextMenuToViewport } from '../dom.js';
import { currentSource, currentPath, currentEntry, setCurrentSource, setLastSaved, appSettings, getPendingAgentReload, setPendingAgentReload, getCursorApiKey, getGithubToken } from '../state.js';
import { postShaderSave, postOpenInCursor, postOpenAgent, postOpenInExplorer, postOpenInNotepad, postCursorSuggestParamsStream, postCursorRefactorParams, postCursorAssist, postCursorAssistVisual, fetchShader, postGitRollback, fetchAgentStatus } from '../api.js';
import { setCursorApiThinking, startCooldownCountdown } from './bootstrap.js';
import * as render from '../render.js';
import { buildParamsPanel, currentParamsMeta, lastDiscoveredParams, setLastDiscoveredParams, applyParamRangesToExposeSource, convertGLSLToISF, normalizeISFForWire, paramValues, updateISFPanel, syncRoliblockFromView } from '../panels/params.js';
import { EditorView, keymap, Decoration } from '@codemirror/view';
import { EditorState, StateField, StateEffect, RangeSetBuilder } from '@codemirror/state';
import { basicSetup } from 'codemirror';
import { cpp } from '@codemirror/lang-cpp';
import { undo } from '@codemirror/commands';
import { isCodeViewEnabled, toggleCodeView } from './codeViewEffect.js';

const DEBOUNCE_MS = 200;

let cmEditor: EditorView | null = null;

function showVibePromptModal(onSubmit: (vibe: string) => void): void {
  const overlay = document.createElement('div');
  overlay.style.cssText = 'position:fixed;inset:0;z-index:10002;background:rgba(0,0,0,0.7);display:flex;align-items:center;justify-content:center;';
  overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
  const box = document.createElement('div');
  box.style.cssText = 'background:var(--amiga-panel);border:2px solid var(--amiga-copper);padding:20px;min-width:320px;max-width:90vw;font-family:inherit;';
  box.onclick = (e) => e.stopPropagation();
  const label = document.createElement('label');
  label.style.cssText = 'color:var(--amiga-copper);font-size:11px;text-transform:uppercase;margin-bottom:6px;display:block;';
  label.textContent = 'Vibe / Describe the visual change';
  const input = document.createElement('textarea');
  input.style.cssText = 'width:100%;min-height:80px;padding:8px;margin-bottom:12px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);font-size:12px;font-family:inherit;resize:vertical;box-sizing:border-box;';
  input.placeholder = 'e.g. make it more neon, add more particles, darker mood';
  input.value = 'make it more vibrant';
  const btnRow = document.createElement('div');
  btnRow.style.cssText = 'display:flex;gap:8px;justify-content:flex-end;';
  const okBtn = document.createElement('button');
  okBtn.type = 'button';
  okBtn.textContent = 'Generate';
  okBtn.style.cssText = 'padding:8px 16px;background:var(--amiga-accent);color:#fff;border:none;cursor:pointer;';
  const cancelBtn = document.createElement('button');
  cancelBtn.type = 'button';
  cancelBtn.textContent = 'Cancel';
  cancelBtn.style.cssText = 'padding:6px 12px;background:var(--amiga-surface);color:var(--amiga-accent);border:1px solid var(--bevel-dark);cursor:pointer;';
  cancelBtn.onclick = () => overlay.remove();
  okBtn.onclick = () => {
    const trimmed = input.value.trim();
    overlay.remove();
    if (trimmed) onSubmit(trimmed);
  };
  btnRow.appendChild(okBtn);
  btnRow.appendChild(cancelBtn);
  box.appendChild(label);
  box.appendChild(input);
  box.appendChild(btnRow);
  overlay.appendChild(box);
  document.body.appendChild(overlay);
  input.focus();
  input.select();
}

export function initCodeEditor(): void {
  const contentSplit = el('contentSplit');
  const codeArea = el('codeArea');
  const container = el('codeEditorContainer');
  const codeWrap = el('codeWrap');
  const viewTabs = document.querySelectorAll('.view-tab[data-view]');
  const splitResizer = el('splitResizer');
  const saveBtn = el('codeSaveBtn');
  const revertBtn = el('codeRevertBtn');
  const reloadBtn = el('codeReloadBtn');
  const undoBtn = el('codeUndoBtn');
  const openCursorBtn = el('codeOpenCursorBtn');
  const openAgentBtn = el('codeOpenAgentBtn');
  const openExplorerBtn = el('codeOpenExplorerBtn');
  const openNotepadBtn = el('codeOpenNotepadBtn');
  const checkISFWireBtn = el('codeCheckISFWireBtn');
  const copyForWireBtn = el('codeCopyForWireBtn');
  const searchParamsBtn = el('codeSearchParamsBtn');
  const exposeBtn = el('codeExposeBtn');
  const refactorParamsBtn = el('codeRefactorParamsBtn');
  const visualModifyBtn = el('codeVisualModifyBtn');
  const paramsSearchPopover = el('paramsSearchPopover');

  if (checkISFWireBtn) checkISFWireBtn.classList.add('btn-wire');

  if (!container || !contentSplit || !codeArea) return;

  const setParamNamesEffect = StateEffect.define<string[]>();
  const SKIP_PARAMS = new Set(['time', 'mouse', 'resolution', 'TIME', 'RENDERSIZE', 'mouseX', 'mouseY', 'timeScale', 'FRAMEINDEX', 'PASSINDEX']);

  function buildParamDecorations(doc: { toString: () => string }, paramNames: string[]): ReturnType<typeof Decoration.none> {
    const text = doc.toString();
    const paramSet = new Set(paramNames.filter((n) => !SKIP_PARAMS.has(n)));
    const ranges: Array<{ from: number; to: number; cls: string; attrs: Record<string, string> }> = [];
    for (const name of paramSet) {
      const esc = name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
      const re = new RegExp('\\b(' + esc + ')\\b', 'gi');
      let m: RegExpExecArray | null;
      while ((m = re.exec(text)) !== null) {
        ranges.push({ from: m.index, to: m.index + m[0].length, cls: 'param-ref', attrs: { 'data-param': name } });
      }
    }
    const litRe = /(?<![a-zA-Z_])(\d+\.\d+f?|\d+\.f?)(?![a-zA-Z_])/g;
    let lm: RegExpExecArray | null;
    const bodyStart = text.indexOf('void main');
    if (bodyStart >= 0) {
      litRe.lastIndex = bodyStart;
      while ((lm = litRe.exec(text)) !== null) {
        const val = parseFloat(lm[1]);
        if (val === 0.0 || val === 1.0) continue;
        const overlap = ranges.some((r) => lm!.index >= r.from && lm!.index < r.to);
        if (!overlap) {
          ranges.push({ from: lm.index, to: lm.index + lm[0].length, cls: 'exposable-hint', attrs: { 'data-literal': lm[1] } });
        }
      }
    }
    ranges.sort((a, b) => a.from - b.from);
    const builder = new RangeSetBuilder<Decoration>();
    let prev = -1;
    for (const r of ranges) {
      if (r.from < prev) continue;
      builder.add(r.from, r.to, Decoration.mark({ class: r.cls, attributes: r.attrs }));
      prev = r.to;
    }
    return builder.finish();
  }

  const paramDecorationsField = StateField.define<{ names: string[]; decos: ReturnType<typeof Decoration.none> }>({
    create: () => ({ names: [], decos: Decoration.none }),
    update: (state, tr) => {
      let names = state.names;
      for (const e of tr.effects) {
        if (e.is(setParamNamesEffect)) names = e.value;
      }
      const doc = tr.state.doc;
      const decos = buildParamDecorations(doc, names);
      return { names, decos };
    },
    provide: (f) => EditorView.decorations.from(f, (s) => s.decos)
  });

  const paramRefTheme = EditorView.theme({
    '.cm-content .param-ref': {
      color: 'var(--amiga-copper)',
      borderBottom: '1px solid rgba(204,119,68,0.5)',
      borderRadius: '2px',
      padding: '0 2px',
      background: 'rgba(204,119,68,0.15)',
      fontWeight: 500,
      cursor: 'pointer'
    },
    '.cm-content .param-ref:hover': {
      background: 'rgba(204,119,68,0.25)',
      borderBottomColor: 'var(--amiga-copper)'
    },
    '.cm-content .exposable-hint': {
      background: 'rgba(255,220,40,0.12)',
      borderBottom: '1px dashed rgba(255,215,0,0.5)',
      borderRadius: '2px',
      padding: '0 1px',
      cursor: 'pointer'
    },
    '.cm-content .exposable-hint:hover': {
      background: 'rgba(255,220,40,0.25)',
      borderBottomColor: '#ffd700',
      borderBottomStyle: 'solid'
    },
    '.cm-content .param-ref.param-ref--highlight': {
      background: 'rgba(204,119,68,0.35)',
      borderBottom: '2px solid var(--amiga-copper)',
      color: '#eeaa55'
    }
  });

  const glslTheme = EditorView.theme({
    '&': { backgroundColor: 'var(--theme-editor-bg)' },
    '.cm-content': { color: 'var(--theme-editor-fg)', caretColor: 'var(--theme-editor-caret)' },
    '.cm-selectionMatch': { backgroundColor: 'var(--theme-editor-selection)' },
    '&.cm-focused .cm-selectionBackground': { backgroundColor: 'var(--theme-editor-selection)' }
  });

  let lastCommittedValue = '';
  let debounceTimer: ReturnType<typeof setTimeout> | null = null;

  function updateDirtyState(): void {
    const sb = document.getElementById('codeSaveBtn');
    if (!sb) return;
    const current = cmEditor ? cmEditor.state.doc.toString() : '';
    const isDirty = current !== lastCommittedValue && lastCommittedValue !== '';
    if (isDirty) {
      sb.classList.add('btn-unsaved');
      sb.textContent = 'Save *';
    } else {
      sb.classList.remove('btn-unsaved');
      sb.textContent = 'Save';
    }
  }

  const docChangeExtension = EditorView.updateListener.of((update) => {
    if (update.docChanged) {
      if (debounceTimer) clearTimeout(debounceTimer);
      debounceTimer = setTimeout(() => {
        debounceTimer = null;
        let newVal = cmEditor ? cmEditor.state.doc.toString() : '';
        const cleaned = render.stripCommentPollution(newVal);
        if (cleaned !== newVal && cmEditor) {
          cmEditor.dispatch({ changes: { from: 0, to: cmEditor.state.doc.length, insert: cleaned } });
          newVal = cleaned;
        }
        setCurrentSource(newVal);
        render.render(newVal);
        updateDirtyState();
      }, DEBOUNCE_MS);
    }
  });

  const saveKeymap = keymap.of([{
    key: 'Mod-s',
    run() {
      const sb = document.getElementById('codeSaveBtn');
      if (sb) (sb as HTMLButtonElement).click();
      return true;
    }
  }]);

  function selectParamInPanel(paramId: string): void {
    const escaped = CSS.escape ? CSS.escape(paramId) : paramId.replace(/["\\]/g, '\\$&');
    const row = document.querySelector('.param-row[data-param="' + escaped + '"]') as HTMLElement | null
      || document.querySelector('.param-row [data-param="' + escaped + '"]')?.closest('.param-row') as HTMLElement | null;
    if (row) {
      row.classList.add('param-ref--highlight');
      row.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
      (row.querySelector('input[type="range"], input[type="checkbox"]') as HTMLElement)?.focus();
      setTimeout(() => row.classList.remove('param-ref--highlight'), 1500);
    }
  }

  const paramClickHandler = EditorView.domEventHandlers({
    click: (ev) => {
      const target = (ev.target as HTMLElement).closest('.param-ref');
      if (target) {
        const paramId = (target as HTMLElement).getAttribute('data-param');
        if (paramId) selectParamInPanel(paramId);
      }
    }
  });

  cmEditor = new EditorView({
    state: EditorState.create({
      doc: '// Select a shader to view code',
      extensions: [
        basicSetup,
        cpp(),
        glslTheme,
        paramRefTheme,
        paramDecorationsField,
        paramClickHandler,
        EditorView.lineWrapping,
        docChangeExtension,
        saveKeymap
      ]
    }),
    parent: container as HTMLElement
  });

  (globalThis as unknown as { refreshParamDecorations?: () => void }).refreshParamDecorations = () => {
    if (!cmEditor) return;
    const names = (currentParamsMeta || []).filter((p) => p.type === 'float' || p.type === 'bool').map((p) => p.id);
    cmEditor.dispatch({ effects: setParamNamesEffect.of(names) });
  };

  let splitVertical = false;
  let suggestedParams: string[] = [];

  function getEditorValue(): string {
    return cmEditor ? cmEditor.state.doc.toString() : '';
  }
  function getEditorSelectionStart(): number {
    return cmEditor ? cmEditor.state.selection.main.from : 0;
  }
  function setEditorValue(text: string): void {
    if (!cmEditor) return;
    cmEditor.dispatch({
      changes: { from: 0, to: cmEditor.state.doc.length, insert: text || '' }
    });
  }

  function parseExposeLines(src: string): Array<{ line: number; name: string; def: number }> {
    const out: Array<{ line: number; name: string; def: number }> = [];
    const lines = (src || '').split('\n');
    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      if (!/\/\/\s*@expose/.test(line)) continue;
      const uMatch = line.match(/uniform\s+float\s+(\w+)\s*;/);
      if (uMatch) {
        out.push({ line: i + 1, name: uMatch[1], def: 0.5 });
        continue;
      }
      const defMatch = line.match(/(?:const\s+)?float\s+(\w+)\s*=\s*([^;]+);\s*\/\//);
      if (defMatch) {
        const num = parseFloat(defMatch[2]);
        out.push({ line: i + 1, name: defMatch[1], def: isNaN(num) ? 0.5 : num });
        continue;
      }
      const defineMatch = line.match(/#define\s+(\w+)\s+([^\s\/]+)\s*\/\//);
      if (defineMatch) {
        const num = parseFloat(defineMatch[2]);
        out.push({ line: i + 1, name: defineMatch[1], def: isNaN(num) ? 0.5 : num });
      }
    }
    return out;
  }

  function getExposedNames(src: string): string[] {
    const out: string[] = [];
    const lines = (src || '').split('\n');
    const skip = new Set(['time', 'mouse', 'resolution', 'TIME', 'RENDERSIZE', 'mouseX', 'mouseY', 'timeScale']);
    for (const line of lines) {
      if (!/\/\/\s*@expose/.test(line)) continue;
      const u = line.match(/uniform\s+(?:float|bool)\s+(\w+)\s*;/);
      if (u && !skip.has(u[1])) { out.push(u[1]); continue; }
      const c = line.match(/(?:const\s+)?(?:float|highp\s+float)\s+(\w+)\s*=/i);
      if (c && !skip.has(c[1])) { out.push(c[1]); continue; }
      const d = line.match(/#define\s+(\w+)\s+/);
      if (d && !skip.has(d[1])) out.push(d[1]);
    }
    return out;
  }

  function parseSuggestedParams(src: string): Array<{ name: string; line: number; def: number }> {
    const out: Array<{ name: string; line: number; def: number }> = [];
    const lines = (src || '').split('\n');
    const reUni = /uniform\s+float\s+(\w+)\s*(?:=\s*([^;]+))?\s*;(?:\s|$)/;
    const reConst = /(?:const\s+)?float\s+(\w+)\s*=\s*([^;]+)\s*;(?:\s|$)/;
    const reDefine = /#define\s+(\w+)\s+([^\s\/]+)(?:\s|$)/;
    const skip = new Set(['time', 'mouse', 'resolution', 'TIME', 'RENDERSIZE', 'mouseX', 'mouseY', 'uTimeScale', 'uMouse']);
    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      if (/\/\/\s*@expose/.test(line)) continue;
      let m = line.match(reUni);
      if (m && !skip.has(m[1])) {
        const def = m[2] ? parseFloat(m[2]) : 0.5;
        out.push({ name: m[1], line: i + 1, def: isNaN(def) ? 0.5 : def });
        continue;
      }
      m = line.match(reConst);
      if (m && !skip.has(m[1])) {
        const def = parseFloat(m[2]);
        out.push({ name: m[1], line: i + 1, def: isNaN(def) ? 0.5 : def });
        continue;
      }
      m = line.match(reDefine);
      if (m && !skip.has(m[1])) {
        const def = parseFloat(m[2]);
        out.push({ name: m[1], line: i + 1, def: isNaN(def) ? 0.5 : def });
      }
    }
    return out;
  }

  function getAssignableParamNames(): string[] {
    return (currentParamsMeta || []).filter((p) => p.type === 'float' || p.type === 'bool').map((p) => p.id);
  }

  function findDefinitionLineForParam(src: string, paramName: string): number {
    const lines = src.split('\n');
    const esc = paramName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      if (/\/\/\s*@expose/.test(line) && new RegExp('\\b' + esc + '\\b').test(line)) return i + 1;
      if (new RegExp('uniform\\s+(?:float|bool|vec2|vec3|vec4)\\s+' + esc + '\\s*[;=]', 'i').test(line)) return i + 1;
      if (new RegExp('(?:const\\s+)?(?:float|highp\\s+float)\\s+' + esc + '\\s*=', 'i').test(line)) return i + 1;
      if (new RegExp('#define\\s+' + esc + '\\b').test(line)) return i + 1;
    }
    return 0;
  }

  (globalThis as unknown as { setHighlightParamInCode?: (id: string | null) => void }).setHighlightParamInCode = (id: string | null) => {
    if (!cmEditor || !id) return;
    const codeAreaEl = el('codeArea');
    if (codeAreaEl && !codeAreaEl.classList.contains('visible')) {
      const splitTab = document.querySelector('.view-tab[data-view="split"]');
      if (splitTab) (splitTab as HTMLElement).click();
    }
    const text = getEditorValue();
    const lineNum = findDefinitionLineForParam(text, id);
    let from: number;
    let to: number;
    if (lineNum > 0) {
      const line = cmEditor.state.doc.line(lineNum);
      const esc = id.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
      const re = new RegExp('\\b(' + esc + ')\\b', 'i');
      const m = line.text.match(re);
      if (m && typeof m.index === 'number') {
        from = line.from + m.index;
        to = from + m[0].length;
      } else {
        from = line.from;
        to = line.to;
      }
    } else {
      const esc = id.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
      const re = new RegExp('\\b(' + esc + ')\\b', 'gi');
      const m = re.exec(text);
      if (!m) return;
      from = m.index;
      to = m.index + m[0].length;
    }
    cmEditor.dispatch({ selection: { anchor: from, head: to } });
    cmEditor.scrollPosIntoView(from, 100);
    cmEditor.focus();
  };

  function setCode(text: string): void {
    const cleaned = render.stripCommentPollution(text || '');
    setEditorValue(cleaned);
    lastCommittedValue = cleaned;
    setCurrentSource(cleaned);
    updateDirtyState();
    if (cmEditor) cmEditor.focus();
  }

  (globalThis as unknown as { syncCodeFromState?: () => void }).syncCodeFromState = () => {
    setCode(currentSource || '');
    const r = (globalThis as unknown as { refreshParamDecorations?: () => void }).refreshParamDecorations;
    if (typeof r === 'function') r();
  };

  (globalThis as unknown as { removeExposedParam?: (name: string, value: number | boolean) => void }).removeExposedParam = (paramName: string, currentValue: number | boolean) => {
    const src = currentSource || '';
    const newSrc = render.removeExposeFromSource(src, paramName, currentValue);
    if (newSrc === src) return;
    setCurrentSource(newSrc);
    setCode(newSrc);
    render.render(newSrc);
    setLastDiscoveredParams(lastDiscoveredParams.filter((n) => n !== paramName));
    buildParamsPanel(currentEntry);
    status('Removed parameter: ' + paramName + '. Save to persist.');
  };

  const vjContainer = document.getElementById('vjDeckContainer');
  const splitOrientationSelect = document.getElementById('splitOrientationSelect') as HTMLSelectElement | null;

  function applySplitOrientation(): void {
    const orient = splitOrientationSelect?.value === 'vertical';
    splitVertical = orient;
    contentSplit.classList.remove('split-vertical');
    if (splitVertical) contentSplit.classList.add('split-vertical');
  }

  const galleryContainer = document.getElementById('galleryContainer');
  const pipelineContainer = document.getElementById('pipelineContainer');
  const wirePipelineContainer = document.getElementById('wirePipelineContainer');

  viewTabs.forEach((tab) => {
    tab.addEventListener('click', () => {
      const view = (tab as HTMLElement).dataset.view;
      viewTabs.forEach((t) => t.classList.remove('active'));
      tab.classList.add('active');
      contentSplit.classList.remove('preview-only', 'code-only', 'split-mode', 'split-vertical');
      if (vjContainer) vjContainer.style.display = 'none';
      if (galleryContainer) galleryContainer.style.display = 'none';
      if (pipelineContainer) pipelineContainer.style.display = 'none';
      if (wirePipelineContainer) wirePipelineContainer.style.display = 'none';
      const previewSection = contentSplit.querySelector('.preview-section') as HTMLElement;
      const splitResizer_ = document.getElementById('splitResizer');
      if (previewSection) previewSection.style.display = '';
      if (splitResizer_) splitResizer_.style.display = '';
      if (codeArea) (codeArea as HTMLElement).style.display = '';
      contentSplit.style.display = '';
      if (view === 'preview') {
        contentSplit.classList.add('preview-only');
      } else if (view === 'code') {
        contentSplit.classList.add('code-only');
        codeArea.classList.add('visible');
      } else if (view === 'vj') {
        if (previewSection) previewSection.style.display = 'none';
        if (splitResizer_) splitResizer_.style.display = 'none';
        if (codeArea) (codeArea as HTMLElement).style.display = 'none';
        if (vjContainer) {
          vjContainer.style.display = 'flex';
          import('../panels/vjDeck.js').then((m) => m.initVJDeck());
        }
      } else if (view === 'gallery') {
        contentSplit.style.display = 'none';
        if (galleryContainer) {
          galleryContainer.style.display = 'flex';
          import('../panels/gallery.js').then((m) => m.initGallery());
        }
      } else if (view === 'split') {
        contentSplit.classList.add('split-mode');
        codeArea.classList.add('visible');
        applySplitOrientation();
      } else if (view === 'pipeline') {
        contentSplit.style.display = 'none';
        if (pipelineContainer) {
          pipelineContainer.style.display = 'flex';
          import('../panels/pipeline.js').then((m) => m.initPipeline());
        }
      } else if (view === 'wire') {
        contentSplit.style.display = 'none';
        if (wirePipelineContainer) {
          wirePipelineContainer.style.display = 'flex';
          import('../panels/wirePipeline.js').then((m) => m.initWirePipeline());
        }
      }
      if (view !== 'gallery') {
        import('../panels/gallery.js').then((m) => { if (typeof m.stopGalleryRendering === 'function') m.stopGalleryRendering(); }).catch(() => {});
      }
      syncRoliblockFromView();
    });
  });

  if (splitOrientationSelect) {
    splitOrientationSelect.addEventListener('change', () => {
      const activeView = document.querySelector('.view-tab.active')?.getAttribute('data-view');
      if (activeView === 'split') applySplitOrientation();
    });
  }

  // Apply default view from settings (default: split-v)
  const dv = (appSettings as Record<string, unknown>).defaultView as string || 'split-v';
  let defaultTab: Element | null = null;
  if (dv === 'preview') defaultTab = document.querySelector('.view-tab[data-view="preview"]');
  else if (dv === 'code') defaultTab = document.querySelector('.view-tab[data-view="code"]');
  else if (dv === 'split-h' || dv === 'split-v' || dv === 'split') {
    defaultTab = document.querySelector('.view-tab[data-view="split"]');
    if (splitOrientationSelect) splitOrientationSelect.value = dv === 'split-h' ? 'horizontal' : 'vertical';
  } else if (dv === 'vj') defaultTab = document.querySelector('.view-tab[data-view="vj"]');
  if (defaultTab) (defaultTab as HTMLElement).click();

  const agentCodeResizer = el('agentCodeResizer');
  const agentOutputPane = el('agentOutputPane');
  const agentOutput = document.getElementById('agentOutput');
  const agentOutputActions = document.getElementById('agentOutputActions');

  const agentOutputCopy = document.getElementById('agentOutputCopy');
  const agentOutputPrint = document.getElementById('agentOutputPrint');
  const agentOutputClear = document.getElementById('agentOutputClear');
  const agentOutputHide = document.getElementById('agentOutputHide');

  agentOutputCopy?.addEventListener('click', () => {
    if (agentOutput?.textContent) {
      navigator.clipboard.writeText(agentOutput.textContent).then(() => status('Copied to clipboard')).catch(() => {});
    }
  });
  agentOutputPrint?.addEventListener('click', () => {
    if (agentOutput?.textContent) {
      const w = window.open('', '_blank');
      if (w) {
        w.document.write('<pre>' + (agentOutput.textContent || '').replace(/</g, '&lt;') + '</pre>');
        w.document.close();
        w.print();
        w.close();
      }
    }
  });
  agentOutputClear?.addEventListener('click', () => {
    if (agentOutput) {
      (agentOutput as HTMLElement).textContent = '';
      status('Agent output cleared');
    }
  });
  agentOutputHide?.addEventListener('click', () => {
    if (agentOutputPane) agentOutputPane.classList.add('collapsed');
    if (agentOutput) (agentOutput as HTMLElement).style.display = 'none';
    if (agentOutputActions) (agentOutputActions as HTMLElement).style.display = 'none';
  });

  agentCodeResizer?.addEventListener('mousedown', (e: MouseEvent) => {
    e.preventDefault();
    document.body.style.cursor = 'col-resize';
    const splitEl = el('codeAgentSplit');
    if (!splitEl || !agentOutputPane) return;
    const startX = e.clientX;
    const startW = agentOutputPane.offsetWidth;
    const total = splitEl.offsetWidth - (agentCodeResizer?.offsetWidth || 8);
    const onMove = (e2: MouseEvent) => {
      const delta = e2.clientX - startX;
      const targetW = startW + delta;
      const newW = Math.max(200, Math.min(total - 180, targetW));
      agentOutputPane.classList.remove('collapsed');
      agentOutputPane.style.flex = '0 0 ' + newW + 'px';
      agentOutputPane.style.minWidth = newW + 'px';
    };
    const onUp = () => {
      document.body.style.cursor = '';
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup', onUp);
    };
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
  });

  splitResizer?.addEventListener('mousedown', (e: MouseEvent) => {
    const vertical = contentSplit.classList.contains('split-vertical');
    document.body.style.cursor = vertical ? 'row-resize' : 'col-resize';
    const previewSection = contentSplit.querySelector('.preview-section') as HTMLElement | null;
    const codeSection = contentSplit.querySelector('.code-area') as HTMLElement | null;
    if (vertical) {
      const startY = e.clientY;
      const startH = previewSection?.offsetHeight || 0;
      const total = contentSplit.offsetHeight - (splitResizer?.offsetHeight || 8);
      const onMove = (e2: MouseEvent) => {
        const delta = e2.clientY - startY;
        const newH = Math.max(80, Math.min(total - 100, startH + delta));
        const pct = newH / total;
        if (previewSection) previewSection.style.flex = String(pct);
        if (codeSection) codeSection.style.flex = String(1 - pct);
      };
      const onUp = () => {
        document.body.style.cursor = '';
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('mouseup', onUp);
      };
      document.addEventListener('mousemove', onMove);
      document.addEventListener('mouseup', onUp);
    } else {
      const startX = e.clientX;
      const startW = previewSection?.offsetWidth || 0;
      const total = contentSplit.offsetWidth - (splitResizer?.offsetWidth || 8);
      const onMove = (e2: MouseEvent) => {
        const delta = e2.clientX - startX;
        const newW = Math.max(120, Math.min(total - 180, startW + delta));
        const pct = newW / total;
        if (previewSection) previewSection.style.flex = String(pct);
        if (codeSection) codeSection.style.flex = String(1 - pct);
      };
      const onUp = () => {
        document.body.style.cursor = '';
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('mouseup', onUp);
      };
      document.addEventListener('mousemove', onMove);
      document.addEventListener('mouseup', onUp);
    }
  });

  function getNumberAtCursor(text: string, offset: number): { value: number; start: number; end: number } | null {
    if (offset < 0 || offset >= text.length) return null;
    const re = /-?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?/g;
    let m: RegExpExecArray | null;
    while ((m = re.exec(text)) !== null) {
      if (offset >= m.index && offset <= m.index + m[0].length) {
        const n = parseFloat(m[0]);
        if (!isNaN(n)) return { value: n, start: m.index, end: m.index + m[0].length };
      }
    }
    return null;
  }

  function getWordAtOffset(text: string, offset: number): { word: string; start: number; end: number; line: string; lineStart: number; lineNum: number } | null {
    if (offset < 0 || offset > text.length) return null;
    const lineEnd = text.indexOf('\n', offset);
    const lineStart = offset;
    let s = offset;
    let e = offset;
    while (s > 0 && /\w/.test(text[s - 1])) s--;
    while (e < text.length && /\w/.test(text[e])) e++;
    const word = text.slice(s, e);
    if (!word) return null;
    const lineStartIdx = text.lastIndexOf('\n', offset - 1) + 1;
    const lineEndIdx = text.indexOf('\n', offset);
    const line = lineEndIdx < 0 ? text.slice(lineStartIdx) : text.slice(lineStartIdx, lineEndIdx);
    const lineNum = (text.slice(0, lineStartIdx).match(/\n/g) || []).length;
    return { word, start: s, end: e, line, lineStart: lineStartIdx, lineNum };
  }

  function canExposeLine(line: string, word: string): { ok: boolean; already: boolean } {
    const trimmed = line.trim();
    if (/\S+\s+\S+/.test(trimmed) && /\/\/\s*@expose/.test(trimmed)) return { ok: true, already: true };
    const uniformFloat = new RegExp('uniform\\s+float\\s+' + word.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\s*;');
    const uniformBool = new RegExp('uniform\\s+bool\\s+' + word.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\s*;');
    const constFloat = new RegExp('(?:const\\s+)?float\\s+' + word.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\s*=');
    const define = new RegExp('#define\\s+' + word.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\s+');
    const skip = new Set(['time', 'mouse', 'resolution', 'TIME', 'RENDERSIZE', 'mouseX', 'mouseY', 'timeScale']);
    if (skip.has(word)) return { ok: false, already: false };
    if (uniformFloat.test(trimmed) || uniformBool.test(trimmed)) return { ok: true, already: /\/\/\s*@expose/.test(trimmed) };
    if (constFloat.test(trimmed) || define.test(trimmed)) return { ok: true, already: /\/\/\s*@expose/.test(trimmed) };
    return { ok: false, already: false };
  }

  function addExposeToLine(line: string): string {
    if (/\/\/\s*@expose/.test(line)) return line;
    const trimmed = line.trimEnd();
    const pad = line.length - trimmed.length;
    return trimmed + ' // @expose' + (pad > 0 ? ' '.repeat(pad) : '');
  }

  function parseValueFromLine(line: string, word: string): number | boolean | null {
    const esc = word.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const constFloat = new RegExp('(?:const\\s+)?(?:float|highp\\s+float)\\s+' + esc + '\\s*=\\s*([^;]+);', 'i');
    const defineRe = new RegExp('#define\\s+' + esc + '\\s+([^\\s/]+)', 'i');
    const uniformBool = new RegExp('uniform\\s+bool\\s+' + esc + '\\s*=\\s*(true|false)', 'i');
    let m = line.match(constFloat);
    if (m) {
      const n = parseFloat(m[1].trim());
      return isNaN(n) ? null : n;
    }
    m = line.match(defineRe);
    if (m) {
      const n = parseFloat(m[1].trim());
      return isNaN(n) ? null : n;
    }
    m = line.match(uniformBool);
    if (m) return m[1].toLowerCase() === 'true';
    return null;
  }

  function parseValueFromSource(src: string, word: string): number | boolean | null {
    const re = new RegExp('\\b' + word.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\b');
    const lines = src.split('\n');
    for (const line of lines) {
      if (re.test(line)) {
        const v = parseValueFromLine(line, word);
        if (v !== null) return v;
      }
    }
    return null;
  }

  let ctxMenu: HTMLElement | null = null;
  function hideCtxMenu(): void {
    if (ctxMenu) {
      ctxMenu.remove();
      ctxMenu = null;
    }
    document.removeEventListener('click', hideCtxMenu);
  }

  function addCtxItem(menu: HTMLElement, label: string, onClick: () => void): void {
    const item = document.createElement('div');
    item.className = 'ctx-menu-item';
    item.textContent = label;
    item.onclick = () => { onClick(); hideCtxMenu(); };
    menu.appendChild(item);
  }

  function runAssist(prompt: string): void {
    const path = currentPath || (currentEntry && currentEntry.path);
    if (!path) { status('No shader selected', true); return; }
    const content = getEditorValue() || currentSource;
    status('Launching agent...');
    setCursorApiThinking(true);
    const agentOut = document.getElementById('agentOutput');
    const agentPane = document.getElementById('agentOutputPane');
    const agentActions = document.getElementById('agentOutputActions');
    if (agentPane) (agentPane as HTMLElement).classList.remove('collapsed');
    if (agentOut) { (agentOut as HTMLElement).style.display = 'block'; (agentOut as HTMLElement).textContent = 'Agent task: ' + prompt + '\n\n'; }
    if (agentActions) (agentActions as HTMLElement).style.display = 'inline';
    postCursorAssist({ path: path.replace(/\\/g, '|'), content, prompt, cursorApiKey: getCursorApiKey() })
      .then(() => {
        status('Agent working... output below.');
        let pollTimer: ReturnType<typeof setInterval> | null = null;
        const pollOutput = async () => {
          try {
            const resp = await fetch('/api/agent-output');
            const j = await resp.json();
            if (agentOut && j.output) { (agentOut as HTMLElement).textContent = j.output; (agentOut as HTMLElement).scrollTop = (agentOut as HTMLElement).scrollHeight; }
          } catch (_) {}
        };
        pollTimer = setInterval(pollOutput, 1500);
        pollOutput();
        import('./bootstrap.js').then((m) => {
          (m as { startAgentReloadPoll: (e: typeof currentEntry) => void }).startAgentReloadPoll(currentEntry);
          const checkDone = setInterval(async () => {
            try {
              const st = await (await import('../api.js')).fetchAgentStatus();
              if (!st.online) {
                clearInterval(checkDone);
                if (pollTimer) clearInterval(pollTimer);
                pollOutput();
                status('Agent finished. Click Reload to apply changes.');
              }
            } catch (_) {}
          }, 2000);
        }).catch(() => {});
      })
      .catch(async (e) => {
        const msg = (e as Error)?.message || 'failed';
        if (/rate|limit|429|cooldown/i.test(msg)) {
          const st = await fetchAgentStatus().catch(() => ({ cooldownRemainingSec: 15 }));
          const sec = st.cooldownRemainingSec ?? 15;
          startCooldownCountdown(sec, (s) => { status('Cursor API cooldown: ' + s + 's', true); });
        } else {
          status('Assist: ' + msg, true);
        }
      })
      .finally(() => setCursorApiThinking(false));
  }

  container.addEventListener('contextmenu', (e: MouseEvent) => {
    e.preventDefault();
    hideCtxMenu();
    const text = getEditorValue();
    const offset = getEditorSelectionStart();
    const numAt = getNumberAtCursor(text, offset);
    const info = getWordAtOffset(text, offset);
    const exposeInfo = info ? canExposeLine(info.line, info.word) : { ok: false, already: false };
    const path = currentPath || (currentEntry && currentEntry.path);

    ctxMenu = document.createElement('div');
    ctxMenu.className = 'ctx-menu';
    ctxMenu.style.cssText = 'position:fixed;left:' + e.clientX + 'px;top:' + e.clientY + 'px;background:var(--amiga-panel);border:2px solid var(--bevel-dark);padding:4px 0;min-width:220px;z-index:10001;';
    document.body.appendChild(ctxMenu);
    clampContextMenuToViewport(ctxMenu);

    const codeViewOn = isCodeViewEnabled();
    addCtxItem(ctxMenu, codeViewOn ? 'Disable code view' : 'Enable code view', () => {
      const on = toggleCodeView();
      status(on ? 'Code view on' : 'Code view off');
    });
    const codeViewSep = document.createElement('div');
    codeViewSep.style.cssText = 'border-top:1px solid var(--bevel-dark);margin:4px 0;';
    ctxMenu.appendChild(codeViewSep);

    if (numAt) {
      addCtxItem(ctxMenu, 'Expose number as slider (' + numAt.value + ')', () => {
        const { newSrc, paramName } = render.exposeLiteralInSource(text, numAt.value, numAt.start, numAt.end);
        paramValues[paramName] = numAt.value;
        setCode(newSrc);
        setCurrentSource(newSrc);
        render.render(newSrc);
        setLastDiscoveredParams([...lastDiscoveredParams, paramName]);
        buildParamsPanel(currentEntry);
        status('Exposed number as ' + paramName + '. Save to persist.');
      });
      const sep = document.createElement('div');
      sep.style.cssText = 'border-top:1px solid var(--bevel-dark);margin:4px 0;';
      ctxMenu.appendChild(sep);
    } else if (info && exposeInfo.ok && !exposeInfo.already) {
      addCtxItem(ctxMenu, 'Expose as parameter (' + info.word + ')', () => {
        const parsedVal = parseValueFromLine(info.line, info.word);
        if (parsedVal !== null) paramValues[info.word] = parsedVal;
        const newText = render.applyExposeToSource(text, [info.word]);
        if (newText !== text) {
          setCode(newText);
          setCurrentSource(newText);
          render.render(newText);
          setLastDiscoveredParams([...lastDiscoveredParams, info.word]);
          buildParamsPanel(currentEntry);
          status('Exposed: ' + info.word);
        } else {
          const lines = text.split('\n');
          const idx = info.lineNum - 1;
          if (idx >= 0 && idx < lines.length) {
            const joined = lines.map((l, i) => i === idx ? addExposeToLine(l) : l).join('\n');
            setCode(joined);
            setCurrentSource(joined);
            render.render(joined);
            buildParamsPanel(currentEntry);
            status('Exposed: ' + info.word);
          }
        }
      });
      const sep = document.createElement('div');
      sep.style.cssText = 'border-top:1px solid var(--bevel-dark);margin:4px 0;';
      ctxMenu.appendChild(sep);
    }

    if (path) {
      addCtxItem(ctxMenu, 'Open in Notepad', () => {
        postOpenInNotepad({ path: path.replace(/\\/g, '|') }).then(() => status('Opened in Notepad')).catch((err) => status(String((err as Error).message), true));
      });
    }
    addCtxItem(ctxMenu, 'AI: Enhance code', () => runAssist('Improve and optimize this shader code. Keep it compatible with GLSL ES. Add @expose for any new tunable parameters.'));
    addCtxItem(ctxMenu, 'AI: Refactor for params', () => {
      if (!path) { status('No shader selected', true); return; }
      const content = getEditorValue() || currentSource;
      status('Refactoring...');
      setCursorApiThinking(true);
      postCursorRefactorParams({ path: path.replace(/\\/g, '|'), content, cursorApiKey: getCursorApiKey() })
        .then(({ content: refactored }) => {
          setCode(refactored);
          setCurrentSource(refactored);
          render.render(refactored);
          buildParamsPanel(currentEntry);
          status('Refactored. Save to persist.');
        })
        .catch(async (err) => {
          const msg = (err as Error)?.message || 'failed';
          if (/rate|limit|429/i.test(msg)) {
            const st = await fetchAgentStatus().catch(() => ({ cooldownRemainingSec: 15 }));
            const sec = st.cooldownRemainingSec ?? 15;
            startCooldownCountdown(sec, (s) => { status('Cursor API cooldown: ' + s + 's', true); });
          } else {
            status('Refactor: ' + msg, true);
          }
        })
        .finally(() => setCursorApiThinking(false));
    });
    addCtxItem(ctxMenu, 'AI: Explain this', () => runAssist('Explain what this shader does and how it works. Describe the key algorithms and math.'));
    addCtxItem(ctxMenu, 'AI: Generate / vibe', () => runAssist('Re-imagine this shader with a fresh creative take. Use freely available GLSL patterns and libraries. Keep Wire/ISF compatibility.'));

    setTimeout(() => document.addEventListener('click', hideCtxMenu), 0);
  });

  undoBtn?.addEventListener('click', () => {
    if (!cmEditor) return;
    const ok = undo(cmEditor);
    if (ok) status('Undo');
  });

  saveBtn?.addEventListener('click', () => {
    const path = currentPath || (currentEntry && currentEntry.path);
    if (!path) {
      status('No shader selected', true);
      return;
    }
    const raw = render.stripCommentPollution(getEditorValue() || '');
    const content = applyParamRangesToExposeSource(raw);
    status('Saving...');
    postShaderSave({ path: path.replace(/\\/g, '|'), content })
      .then(() => {
        setLastSaved(path);
        render.captureThumbnailNow();
        status('Saved. Git commit when EnableGit.');
        setCurrentSource(content);
        setCode(content);
        lastCommittedValue = content;
        updateDirtyState();
        // Refresh git info display
        import('../api.js').then(({ fetchGitInfo }) => {
          fetchGitInfo(path).then((info) => {
            const revEl = document.getElementById('shaderRevInfo');
            if (revEl) {
              if (info.tracked && info.revisions > 0) {
                const age = info.firstDate ? info.firstDate.slice(0, 10) : '?';
                revEl.textContent = info.revisions + ' rev' + (info.revisions !== 1 ? 's' : '') + ' | created ' + age;
                revEl.style.display = '';
              } else {
                revEl.textContent = 'Not tracked (save to start versioning)';
                revEl.style.display = '';
              }
            }
          }).catch(() => {});
        });
        import('../list.js').then((m) => m.buildList()).catch(() => {});
      })
      .catch((e) => {
        status('Save: ' + (e?.message || 'failed'), true);
      });
  });

  revertBtn?.addEventListener('click', () => {
    const path = currentPath || (currentEntry && currentEntry.path);
    if (!path) {
      status('No shader selected', true);
      return;
    }
    status('Reverting...');
    postGitRollback(path)
      .then(() => fetchShader(path.replace(/\\/g, '|')))
      .then((src) => {
        setCode(src);
        setCurrentSource(src);
        render.render(src);
        buildParamsPanel(currentEntry);
        status('Reverted to last commit.');
      })
      .catch((e) => {
        status('Revert: ' + (e?.message || 'failed'), true);
      });
  });

  reloadBtn?.addEventListener('click', () => {
    const path = currentPath || (currentEntry && currentEntry.path);
    if (!path) {
      status('No shader selected', true);
      return;
    }
    reloadBtn.classList.remove('reload-ready');
    if (getPendingAgentReload()) {
      setPendingAgentReload(false);
      status('Reloading from disk...');
      fetchShader(path.replace(/\\/g, '|'))
        .then((src) => {
          const cleaned = render.stripCommentPollution(src);
          setCurrentSource(cleaned);
          setCode(cleaned);
          render.render(cleaned);
          buildParamsPanel(currentEntry);
          updateISFPanel();
          const sync = (globalThis as unknown as { syncCodeFromState?: () => void }).syncCodeFromState;
          if (typeof sync === 'function') sync();
          status('Reloaded. Params preserved.');
        })
        .catch((e) => status('Reload: ' + (e?.message || 'failed'), true));
      return;
    }
    status('Saving and reloading...');
    postShaderSave({ path: path.replace(/\\/g, '|'), content: getEditorValue() })
      .then(() => {
        setCurrentSource(getEditorValue());
        render.loadShader(currentEntry);
      })
      .catch((e) => {
        status('Save before reload: ' + (e?.message || 'failed'), true);
      });
  });

  openCursorBtn?.addEventListener('click', () => {
    const path = currentPath || (currentEntry && currentEntry.path);
    if (!path) {
      status('No shader selected', true);
      return;
    }
    postOpenInCursor({ path: path.replace(/\\/g, '|') })
      .then(() => status('Opened in Cursor'))
      .catch((e) => status('Open: ' + (e?.message || 'failed'), true));
  });

  openAgentBtn?.addEventListener('click', () => {
    const path = currentPath || (currentEntry && currentEntry.path);
    if (!path) {
      status('No shader selected', true);
      return;
    }
    postOpenAgent({ path: path.replace(/\\/g, '|') })
      .then(() => status('Agent launched in shader directory'))
      .catch((e: Error & { rateLimit?: boolean }) => {
        let msg = 'Agent: ' + (e?.message || 'failed');
        if (e?.rateLimit || /rate|limit|verify|human/i.test(msg)) msg += ' Wait a few min or use Open in Cursor and run Agent from Cursor IDE.';
        status(msg, true);
      });
  });

  openExplorerBtn?.addEventListener('click', () => {
    const path = currentPath || (currentEntry && currentEntry.path);
    if (!path) {
      status('No shader selected', true);
      return;
    }
    postOpenInExplorer({ path: path.replace(/\\/g, '|') })
      .then(() => status('Opened in Explorer'))
      .catch((e) => status('Explorer: ' + (e?.message || 'failed'), true));
  });

  openNotepadBtn?.addEventListener('click', () => {
    const path = currentPath || (currentEntry && currentEntry.path);
    if (!path) {
      status('No shader selected', true);
      return;
    }
    postOpenInNotepad({ path: path.replace(/\\/g, '|') })
      .then(() => status('Opened in Notepad'))
      .catch((e) => status('Notepad: ' + (e?.message || 'failed'), true));
  });

  checkISFWireBtn?.addEventListener('click', () => {
    const raw = getEditorValue() || currentSource || '';
    if (!raw.trim()) {
      status('No shader content', true);
      return;
    }
    const content = applyParamRangesToExposeSource(raw);
    const hasExpose = /\/\/\s*@expose/.test(content);
    const samplerMatch = content.match(/uniform\s+sampler2D\s+(\w+)\s*;/g);
    const samplers = samplerMatch ? samplerMatch.map((m) => m.replace(/uniform\s+sampler2D\s+(\w+)\s*;/, '$1')) : [];
    const exposeMatch = content.match(/uniform\s+(?:float|bool)\s+(\w+)\s*;\s*\/\/\s*@expose/g);
    const exposedParams = exposeMatch ? exposeMatch.map((m) => m.replace(/uniform\s+(?:float|bool)\s+(\w+)\s*;\s*\/\/\s*@expose/, '$1')) : [];
    try {
      const entry = currentEntry;
      const desc = entry ? (entry.name || entry.fixedName || '') : '';
      const isf = convertGLSLToISF(content, desc + ' (compatibility check)');
      if (!isf || !isf.includes('ISFVSN')) {
        status('ISF conversion failed', true);
        return;
      }
      const parts: string[] = [];
      if (exposedParams.length) parts.push('params: ' + exposedParams.join(', '));
      if (samplers.length) parts.push('sampler2D: ' + samplers.join(', '));
      const msg = parts.length ? 'ISF Wire compatible (' + parts.join('; ') + ')' : 'ISF Wire compatible';
      status(msg);
    } catch (e) {
      status('Not compatible: ' + ((e as Error).message || 'conversion error'), true);
    }
  });

  copyForWireBtn?.addEventListener('click', () => {
    const path = currentPath || (currentEntry && currentEntry.path);
    if (!path) {
      status('No shader selected', true);
      return;
    }
    const raw = getEditorValue() || currentSource || '';
    const content = applyParamRangesToExposeSource(raw);
    const isAlreadyISF = /\/\*\s*\{[\s\S]*?"ISFVSN"/.test(content);
    const entry = currentEntry;
    const desc = entry ? (entry.name || entry.fixedName || '') : '';
    const isfContent = isAlreadyISF ? normalizeISFForWire(content) : convertGLSLToISF(content, desc + ' (for Wire)');
    const statusMsg = isAlreadyISF ? 'Copying ISF to clipboard...' : 'Saving then converting to ISF and copying...';
    status(statusMsg);
    postShaderSave({ path: path.replace(/\\/g, '|'), content })
      .then(() => {
        setLastSaved(path);
        setCurrentSource(content);
        setCode(content);
        return navigator.clipboard.writeText(isfContent);
      })
      .then(() => {
        if (isAlreadyISF) {
          status('Copied ISF to clipboard. Paste into Wire.');
        } else {
          status('Copied as ISF (Wire-ready). Paste into Wire.');
        }
      })
      .catch((e) => status('Copy: ' + (e?.message || 'failed'), true));
  });

  const vibeGithubBtn = document.getElementById('codeVibeGithubBtn');
  if (vibeGithubBtn) {
    vibeGithubBtn.addEventListener('click', () => {
      const content = applyParamRangesToExposeSource(getEditorValue() || currentSource);
      if (!content) { status('No shader content', true); return; }
      showVibePromptModal(async (trimmed) => {
        if (!trimmed) return;
        try {
          status('Vibe with GitHub...');
          (vibeGithubBtn as HTMLButtonElement).disabled = true;
          const { postGithubAiFix } = await import('../api.js');
          const prompt = 'Apply this visual change to the GLSL/ISF shader. Describe exactly what to change: ' + trimmed + '. Return only the complete shader source, no explanation.';
          const { content: newContent } = await postGithubAiFix({
            content,
            prompt,
            token: getGithubToken() || undefined
          });
          if (newContent) {
            setCurrentSource(newContent);
            setCode(newContent);
            render.render(newContent);
            status('Applied GitHub vibe. Save to persist.');
          } else {
            status('GitHub returned no content', true);
          }
        } catch (e) {
          const msg = e instanceof Error ? e.message : String(e);
          status('Vibe (GitHub): ' + msg, true);
          if (msg.includes('401') || msg.includes('token')) status('Add GitHub token in Settings', true);
        } finally {
          (vibeGithubBtn as HTMLButtonElement).disabled = false;
        }
      });
    });
  }

  visualModifyBtn?.addEventListener('click', () => {
    const path = currentPath || (currentEntry && currentEntry.path);
    if (!path) { status('No shader selected', true); return; }
    const screenshot = render.capturePreviewScreenshot();
    if (!screenshot) { status('Could not capture preview', true); return; }
    const content = applyParamRangesToExposeSource(getEditorValue() || currentSource);
    showVibePromptModal((trimmed) => {
      if (!trimmed) return;
      status('Sending screenshot + code to agent...');
      setCursorApiThinking(true);
      postCursorAssistVisual({
        path: path.replace(/\\/g, '|'),
        content,
        prompt: trimmed,
        screenshot,
        cursorApiKey: getCursorApiKey()
      })
        .then(() => {
          status('Agent launched. Watching output...');
          const agentOut = document.getElementById('agentOutput');
          const agentPane = document.getElementById('agentOutputPane');
          const agentActions = document.getElementById('agentOutputActions');
          if (agentPane) (agentPane as HTMLElement).classList.remove('collapsed');
          if (agentOut) { (agentOut as HTMLElement).style.display = 'block'; (agentOut as HTMLElement).textContent = 'Agent working on: ' + trimmed + '\n'; }
          if (agentActions) (agentActions as HTMLElement).style.display = 'inline';
          let pollTimer: ReturnType<typeof setInterval> | null = null;
          const pollOutput = async () => {
            try {
              const resp = await fetch('/api/agent-output');
              const j = await resp.json();
              if (agentOut && j.output) { (agentOut as HTMLElement).textContent = j.output; (agentOut as HTMLElement).scrollTop = (agentOut as HTMLElement).scrollHeight; }
            } catch (_) {}
          };
          pollTimer = setInterval(pollOutput, 1500);
          pollOutput();
          import('./bootstrap.js').then((m) => {
            (m as { startAgentReloadPoll: (e: typeof currentEntry) => void }).startAgentReloadPoll(currentEntry);
            const checkDone = setInterval(async () => {
              try {
                const st = await (await import('../api.js')).fetchAgentStatus();
                if (!st.online) {
                  clearInterval(checkDone);
                  if (pollTimer) clearInterval(pollTimer);
                  pollOutput();
                }
              } catch (_) {}
            }, 2000);
          }).catch(() => {});
        })
        .catch(async (e) => {
          const msg = e?.message || 'failed';
          if (/not found|503|not in PATH/i.test(msg)) {
            status('Vibe: cursor-agent not installed or not in PATH. Install it or use Open Agent.', true);
          } else if (/rate|limit|429|cooldown/i.test(msg)) {
            const st = await fetchAgentStatus().catch(() => ({ cooldownRemainingSec: 15 }));
            const sec = st.cooldownRemainingSec ?? 15;
            startCooldownCountdown(sec, (s) => { status('Cursor API cooldown: ' + s + 's', true); });
          } else {
            status('Vibe: ' + msg, true);
          }
        })
        .finally(() => setCursorApiThinking(false));
    });
  });

  function escapeHtml(s: string): string {
    return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  }

  function findLiteralPosition(src: string, value: number, lineOneBased: number): { start: number; end: number } | null {
    const lines = src.split('\n');
    const lineIndex = lineOneBased - 1;
    if (lineIndex < 0 || lineIndex >= lines.length) return null;
    let lineStart = 0;
    for (let i = 0; i < lineIndex; i++) lineStart += lines[i].length + 1;
    const lineContent = lines[lineIndex];
    const re = /\b(\d+\.?\d*f?)\b/g;
    let m: RegExpExecArray | null;
    while ((m = re.exec(lineContent)) !== null) {
      const numStr = m[1].replace(/f$/i, '');
      const num = parseFloat(numStr);
      if (!isNaN(num) && num === value) {
        return { start: lineStart + m.index, end: lineStart + m.index + m[0].length };
      }
    }
    return null;
  }

  function parseNumericLiterals(src: string): Array<{ value: number; line: number; start: number; end: number; literal: string }> {
    const out: Array<{ value: number; line: number; start: number; end: number; literal: string }> = [];
    const re = /\b(\d+\.?\d*f?)\b/gi;
    let m: RegExpExecArray | null;
    while ((m = re.exec(src)) !== null) {
      const literal = m[1];
      const numStr = literal.replace(/f$/i, '');
      const value = parseFloat(numStr);
      if (isNaN(value)) continue;
      const line = (src.slice(0, m.index).match(/\n/g) || []).length + 1;
      out.push({ value, line, start: m.index, end: m.index + m[0].length, literal: m[0] });
    }
    return out;
  }

  function refreshParamsSearchPopover(): void {
    if (!paramsSearchPopover) return;
    const src = getEditorValue() || currentSource;
    const exposedSet = new Set(getExposedNames(src));
    const suggested = parseSuggestedParams(src).filter((p) => !exposedSet.has(p.name));
    const literals = parseNumericLiterals(src);
    const maxLiterals = 80;
    const literalsToShow = literals.slice(0, maxLiterals);
    let html = '<h5>Named (const / #define / uniform)</h5>';
    if (suggested.length === 0) {
      html += '<div class="search-row" style="color:var(--crt-dim);font-size:10px">None.</div>';
    } else {
      for (const p of suggested) {
        html += '<div class="search-row" data-param-name="' + escapeHtml(p.name) + '">';
        html += '<span class="name" title="line ' + p.line + ', def ' + p.def + '">' + escapeHtml(p.name) + ' (L' + p.line + ')</span>';
        html += '<button type="button" class="wire-btn search-expose-one">Expose</button></div>';
      }
    }
    html += '<h5 style="margin-top:8px">Numeric literals (expose as Wire input)</h5>';
    if (literalsToShow.length === 0) {
      html += '<div class="search-row" style="color:var(--crt-dim);font-size:10px">No numeric literals found.</div>';
    } else {
      for (let i = 0; i < literalsToShow.length; i++) {
        const lit = literalsToShow[i];
        const title = 'value ' + lit.value + ', line ' + lit.line;
        html += '<div class="search-row" data-literal-index="' + i + '" data-value="' + lit.value + '" data-start="' + lit.start + '" data-end="' + lit.end + '">';
        html += '<span class="name" title="' + escapeHtml(title) + '">' + escapeHtml(lit.literal) + ' (L' + lit.line + ')</span>';
        html += '<button type="button" class="wire-btn search-expose-literal">Expose</button></div>';
      }
      if (literals.length > maxLiterals) {
        html += '<div class="search-row" style="color:var(--crt-dim);font-size:10px">... and ' + (literals.length - maxLiterals) + ' more. Expose from code (right-click) or add names.</div>';
      }
    }
    html += '<div class="search-ai"><button type="button" class="wire-btn btn-ai-tokens" id="searchSuggestMoreAi"><span class="btn-tokens-icon" aria-hidden="true">&#x1F3AB;&#x1F4B8;</span> Suggest more with AI</button></div>';
    paramsSearchPopover.innerHTML = html;
    paramsSearchPopover.querySelectorAll('.search-expose-one').forEach((btn) => {
      btn.addEventListener('click', () => {
        const row = (btn as HTMLElement).closest('.search-row');
        const name = row?.getAttribute('data-param-name');
        if (!name) return;
        const raw = getEditorValue() || currentSource;
        const v = parseValueFromSource(raw, name);
        if (v !== null) paramValues[name] = v;
        const modified = render.applyExposeToSource(raw, [name]);
        if (modified !== raw) {
          setCode(modified);
          setCurrentSource(modified);
          setLastDiscoveredParams([...lastDiscoveredParams, name]);
          render.render(modified);
          buildParamsPanel(currentEntry);
          status('Exposed: ' + name);
          refreshParamsSearchPopover();
        }
      });
    });
    paramsSearchPopover.querySelectorAll('.search-expose-literal').forEach((btn) => {
      btn.addEventListener('click', () => {
        const row = (btn as HTMLElement).closest('.search-row');
        const startStr = row?.getAttribute('data-start');
        const endStr = row?.getAttribute('data-end');
        const valueStr = row?.getAttribute('data-value');
        if (startStr == null || endStr == null || valueStr == null) return;
        const raw = getEditorValue() || currentSource;
        const start = parseInt(startStr, 10);
        const end = parseInt(endStr, 10);
        const value = parseFloat(valueStr);
        if (isNaN(start) || isNaN(end) || isNaN(value) || start < 0 || end > raw.length) return;
        const { newSrc, paramName } = render.exposeLiteralInSource(raw, value, start, end);
        paramValues[paramName] = value;
        setCode(newSrc);
        setCurrentSource(newSrc);
        setLastDiscoveredParams([...lastDiscoveredParams, paramName]);
        render.render(newSrc);
        buildParamsPanel(currentEntry);
        status('Exposed literal as ' + paramName);
        refreshParamsSearchPopover();
      });
    });
    const aiBtn = paramsSearchPopover.querySelector('#searchSuggestMoreAi');
    aiBtn?.addEventListener('click', () => {
      const path = currentPath || (currentEntry && currentEntry.path);
      if (!path) {
        status('No shader selected', true);
        return;
      }
      status('Suggesting params with AI...');
      setCursorApiThinking(true);
      postCursorSuggestParamsStream(
        { path: path.replace(/\\/g, '|'), content: getEditorValue() || currentSource, cursorApiKey: getCursorApiKey() },
        (line) => {
          const out = document.getElementById('agentOutput');
          if (out) {
            (out as HTMLElement).style.display = 'block';
            (out as HTMLElement).textContent = ((out as HTMLElement).textContent || '') + line + '\n';
            (out as HTMLElement).scrollTop = (out as HTMLElement).scrollHeight;
            const actions = document.getElementById('agentOutputActions');
            if (actions) (actions as HTMLElement).style.display = 'inline';
          }
        }
      )
        .then((j: { params?: string[]; literals?: Array<{ value: number; line: number }> }) => {
          let raw = getEditorValue() || currentSource;
          const existing = getExposedNames(raw);
          const fromAi = j.params || [];
          const merged = [...new Set([...existing, ...lastDiscoveredParams, ...fromAi])];
          for (const name of merged) {
            const v = parseValueFromSource(raw, name);
            if (v !== null) paramValues[name] = v;
          }
          setLastDiscoveredParams(merged);
          let modified = render.applyExposeToSource(raw, merged);
          if (modified !== raw) {
            setCode(modified);
            setCurrentSource(modified);
            raw = modified;
          }
          const literals = j.literals || [];
          for (const lit of literals) {
            const value = Number(lit.value);
            const line = Number(lit.line);
            if (isNaN(value) || isNaN(line)) continue;
            const pos = findLiteralPosition(raw, value, line);
            if (!pos) continue;
            const { newSrc, paramName } = render.exposeLiteralInSource(raw, value, pos.start, pos.end);
            paramValues[paramName] = value;
            setLastDiscoveredParams([...lastDiscoveredParams, paramName]);
            raw = newSrc;
            setCode(raw);
            setCurrentSource(raw);
          }
          render.render(getEditorValue() || currentSource);
          buildParamsPanel(currentEntry);
          refreshParamsSearchPopover();
          const added = merged.length - existing.length + literals.length;
          status('Added ' + added + ' from AI (' + fromAi.length + ' names, ' + literals.length + ' literals). Save to persist.');
        })
        .catch(async (e) => {
          const msg = (e as Error)?.message || 'failed';
          if (/rate|limit|429/i.test(msg)) {
            const st = await fetchAgentStatus().catch(() => ({ cooldownRemainingSec: 15 }));
            const sec = st.cooldownRemainingSec ?? 15;
            startCooldownCountdown(sec, (s) => { status('Cursor API cooldown: ' + s + 's', true); });
          } else {
            status('AI: ' + msg, true);
          }
        })
        .finally(() => setCursorApiThinking(false));
    });
  }

  function hideParamsSearchPopover(): void {
    if (paramsSearchPopover) (paramsSearchPopover as HTMLElement).style.display = 'none';
  }

  searchParamsBtn?.addEventListener('click', (e: Event) => {
    if (!paramsSearchPopover || !codeArea) return;
    const btn = e.currentTarget as HTMLElement;
    const rect = btn.getBoundingClientRect();
    const areaRect = codeArea.getBoundingClientRect();
    (paramsSearchPopover as HTMLElement).style.display = 'block';
    (paramsSearchPopover as HTMLElement).style.left = (rect.left - areaRect.left) + 'px';
    (paramsSearchPopover as HTMLElement).style.top = (rect.bottom - areaRect.top + 4) + 'px';
    refreshParamsSearchPopover();
    const close = (ev: MouseEvent) => {
      if (paramsSearchPopover && !(paramsSearchPopover as HTMLElement).contains(ev.target as Node) && ev.target !== btn) {
        hideParamsSearchPopover();
        document.removeEventListener('click', close);
      }
    };
    setTimeout(() => document.addEventListener('click', close), 0);
  });

  exposeBtn?.addEventListener('click', () => {
    const path = currentPath || (currentEntry && currentEntry.path);
    if (!path) {
      status('No shader selected', true);
      return;
    }
    const content = getEditorValue() || currentSource;
    status('Analyzing shader for params...');
    const agentOut = document.getElementById('agentOutput');
    const agentPane = document.getElementById('agentOutputPane');
    const viewActive = document.querySelector('.view-tab.active')?.getAttribute('data-view') || '';
    if (viewActive === 'preview') {
      const codeTab = document.querySelector('.view-tab[data-view="code"]');
      if (codeTab) (codeTab as HTMLElement).click();
    }
    if (agentOut) {
      (agentOut as HTMLElement).style.display = 'block';
      (agentOut as HTMLElement).textContent = 'Scanning code for exposable params...';
    }
    if (agentPane) (agentPane as HTMLElement).classList.remove('collapsed');
    setCursorApiThinking(true);
    postCursorSuggestParamsStream(
      {
        path: path.replace(/\\/g, '|'),
        content,
        cursorApiKey: getCursorApiKey()
      },
      (line) => {
        const out = document.getElementById('agentOutput');
        const pane = document.getElementById('agentOutputPane');
        if (pane) (pane as HTMLElement).classList.remove('collapsed');
        if (out) {
          const el_ = out as HTMLElement;
          el_.style.display = 'block';
          const prev = el_.textContent || '';
          el_.textContent = (prev === 'Scanning code for exposable params...' ? '' : prev) + line + '\n';
          el_.scrollTop = el_.scrollHeight;
        }
      }
    )
      .then((j: { params?: string[]; literals?: Array<{ value: number; line: number }> }) => {
        let raw = getEditorValue() || currentSource;
        const existing = getExposedNames(raw);
        const fromAi = j.params || [];
        const merged = [...new Set([...existing, ...lastDiscoveredParams, ...fromAi])];
        for (const name of merged) {
          const v = parseValueFromSource(raw, name);
          if (v !== null) paramValues[name] = v;
        }
        suggestedParams = fromAi;
        setLastDiscoveredParams(merged);
        let modified = render.applyExposeToSource(raw, merged);
        if (modified !== raw) {
          setCode(modified);
          setCurrentSource(modified);
          raw = modified;
        }
        const literals = j.literals || [];
        for (const lit of literals) {
          const value = Number(lit.value);
          const line = Number(lit.line);
          if (isNaN(value) || isNaN(line)) continue;
          const pos = findLiteralPosition(raw, value, line);
          if (!pos) continue;
          const { newSrc, paramName } = render.exposeLiteralInSource(raw, value, pos.start, pos.end);
          paramValues[paramName] = value;
          setLastDiscoveredParams([...lastDiscoveredParams, paramName]);
          raw = newSrc;
          setCode(raw);
          setCurrentSource(raw);
        }
        setTimeout(() => {
          const src = getEditorValue() || currentSource;
          render.render(src);
          buildParamsPanel(currentEntry);
          const setDiscoverSuggestions = (globalThis as unknown as { setDiscoverSuggestions?: (s: string) => void }).setDiscoverSuggestions;
          if (typeof setDiscoverSuggestions === 'function') {
            const namesStr = merged.length ? merged.join(', ') : '';
            const litStr = literals.length ? ' + ' + literals.length + ' literals' : '';
            setDiscoverSuggestions(namesStr || literals.length ? namesStr + litStr : 'No params');
          }
          const added = merged.length - existing.length + literals.length;
          if (fromAi.length === 0 && literals.length === 0 && merged.length === existing.length) {
            status('No new params suggested. Try again or use Search for parameters.', true);
          } else {
            status(added > 0 ? 'Added ' + added + ' (' + fromAi.length + ' names, ' + literals.length + ' literals). Save to persist.' : 'Params up to date.');
          }
        }, 0);
      })
      .catch(async (e: Error) => {
        let msg = 'Expose: ' + (e?.message || 'failed');
        if (/rate|limit|verify|human|429/i.test(msg)) {
          const st = await fetchAgentStatus().catch(() => ({ cooldownRemainingSec: 15 }));
          const sec = st.cooldownRemainingSec ?? 15;
          startCooldownCountdown(sec, (s) => {
            status('Cursor API cooldown: ' + s + 's. Or use Open in Cursor and run Agent from IDE.', true);
          });
        } else {
          status(msg, true);
        }
      })
      .finally(() => setCursorApiThinking(false));
  });

  refactorParamsBtn?.addEventListener('click', () => {
    const path = currentPath || (currentEntry && currentEntry.path);
    if (!path) {
      status('No shader selected', true);
      return;
    }
    const content = getEditorValue() || currentSource;
    status('Refactoring for params...');
    setCursorApiThinking(true);
    postCursorRefactorParams({ path: path.replace(/\\/g, '|'), content, cursorApiKey: getCursorApiKey() })
      .then(({ content: refactored }) => {
        setCode(refactored);
        setCurrentSource(refactored);
        render.render(refactored);
        buildParamsPanel(currentEntry);
        status('Refactored. Use Search for parameters or Expose params to add sliders. Save to persist.');
      })
      .catch(async (e: Error) => {
        const msg = 'Refactor: ' + (e?.message || 'failed');
        if (/rate|limit|429/i.test(msg)) {
          const st = await fetchAgentStatus().catch(() => ({ cooldownRemainingSec: 15 }));
          const sec = st.cooldownRemainingSec ?? 15;
          startCooldownCountdown(sec, (s) => { status('Cursor API cooldown: ' + s + 's', true); });
        } else {
          status(msg, true);
        }
      })
      .finally(() => setCursorApiThinking(false));
  });
}
