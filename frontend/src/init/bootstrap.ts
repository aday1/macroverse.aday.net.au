import { status, el } from '../dom.js';
import { fetchSettings, fetchAgentStatus, fetchShader, fetchUpdateCheck, postUpdateApply } from '../api.js';
import { setAppSettings, getPendingCursorConfirm, setPendingCursorConfirm, setPendingAgentReload, lastCompileError, lastCompileErrorPath, setLastCompileError, clearLastCompileError, currentEntry, currentSource, setCurrentSource, appSettings, setCurrentEntry, entries, getCursorApiKey, getGithubToken, setLocalCursorApiKey, setLocalGithubToken, getLocalCursorKeyStored, getLocalGithubTokenStored } from '../state.js';
import { applyTheme } from '../themeUtils.js';
import { loadSequence } from './loadSequence.js';
import * as render from '../render.js';
import { stripLeadingGarbage } from '../render.js';
import { initSettings } from '../panels/settings.js';
import { initCodeEditor, applyInitialView } from './codeEditor.js';
import { initPreviewContextMenu } from './previewContextMenu.js';
import { initAppBar } from './appBar.js';
import { initCommandPalette } from './commandPalette.js';
import { registerCoreCommands } from './coreCommands.js';
import { initDockSystem } from './dockSystem.js';
import { initBottomTabBar } from './bottomTabBar.js';
import { initLayoutTier, onLayoutTierChange, isCompactLayout, isLayoutPhone, applyLayoutTier } from './layoutTier.js';
import { initMobilePanels } from './mobilePanels.js';
import { CODE_VIEW_STORAGE_KEY, applyCodeViewState, toggleCodeView } from './codeViewEffect.js';
import { isVjViewOnlyMode, persistVjSessionFromUrl } from '../vjSession.js';
import { ensureVjTokens, syncVjTokensWithDeployMeta } from '../vjTokens.js';
import { reconnectVjSession } from '../vjWs.js';
import { maybeShowQuickStartGuide } from './quickStartGuide.js';
import { initSplashVjQr, refreshSplashVjQr, requestSplashDismiss, initSplashDismiss } from './splashVjQr.js';
import { initSplashBuildInfo } from './splashBuildInfo.js';
import { loadHostCapabilities } from '../hostCapabilities.js';
import { onLocalStoreChange } from '../cloudLocalStore.js';
import { initBackendViewer } from './backendViewer.js';
import { initControllerAutoMap } from './controllerAutoMap.js';

let cooldownTimerId: ReturnType<typeof setInterval> | null = null;

const FIX_BTN_WIZARD = '<span class="btn-ollama-icon" aria-hidden="true">&#x1F9D9;</span> ';
function restoreFixButtonLabel(btn: HTMLElement | null, label: string): void {
  if (btn) (btn as HTMLButtonElement).innerHTML = FIX_BTN_WIZARD + label;
}

type FixStage = 'local' | 'ollama' | 'cursor' | 'none';

function setFixingState(stage: FixStage): void {
  const fixBtn = document.getElementById('fixBtn');
  const previewFixBtn = document.getElementById('previewFixBtn');
  const btns = [fixBtn, previewFixBtn].filter(Boolean) as HTMLElement[];
  const allClasses = ['fixing', 'fixing-local', 'fixing-ollama', 'fixing-cursor', 'fix-escalate'];
  for (const b of btns) {
    allClasses.forEach((c) => b.classList.remove(c));
  }
  if (stage === 'none') return;
  const stageClass = 'fixing-' + stage;
  for (const b of btns) {
    b.classList.add('fixing', stageClass);
  }
}

function clearFixingState(): void {
  setFixingState('none');
}

function showTagDeadButton(show: boolean): void {
  const btn = document.getElementById('previewTagDeadBtn');
  if (btn) (btn as HTMLElement).style.display = show ? '' : 'none';
}

export function setCursorApiCooldown(remainingSec: number): void {
  const b = document.body;
  if (!b) return;
  if (remainingSec > 0) {
    b.dataset.cursorApiCooldown = String(remainingSec);
    b.querySelectorAll('.btn-ai-tokens').forEach((el) => { (el as HTMLButtonElement).disabled = true; });
  } else {
    delete b.dataset.cursorApiCooldown;
    if (!b.dataset.cursorApiThinking) {
      b.querySelectorAll('.btn-ai-tokens').forEach((el) => { (el as HTMLButtonElement).disabled = false; });
    }
  }
}

export function setCursorApiThinking(thinking: boolean): void {
  const b = document.body;
  if (!b) return;
  if (thinking) {
    b.dataset.cursorApiThinking = '1';
    b.querySelectorAll('.btn-ai-tokens').forEach((el) => { (el as HTMLButtonElement).disabled = true; });
  } else {
    delete b.dataset.cursorApiThinking;
    if (!b.dataset.cursorApiCooldown) {
      b.querySelectorAll('.btn-ai-tokens').forEach((el) => { (el as HTMLButtonElement).disabled = false; });
    }
  }
}

export function startCooldownCountdown(initialSec: number, onTick: (remainingSec: number) => void): void {
  if (cooldownTimerId) clearInterval(cooldownTimerId);
  let remaining = Math.max(0, Math.floor(initialSec));
  onTick(remaining);
  setCursorApiCooldown(remaining);
  if (remaining <= 0) return;
  cooldownTimerId = setInterval(() => {
    fetchAgentStatus()
      .then((st) => {
        const sec = st.cooldownRemainingSec ?? 0;
        onTick(sec);
        setCursorApiCooldown(sec);
        if (sec <= 0 && cooldownTimerId) {
          clearInterval(cooldownTimerId);
          cooldownTimerId = null;
        }
      })
      .catch(() => {
        remaining = Math.max(0, remaining - 1);
        onTick(remaining);
        setCursorApiCooldown(remaining);
        if (remaining <= 0 && cooldownTimerId) {
          clearInterval(cooldownTimerId);
          cooldownTimerId = null;
        }
      });
  }, 1000);
}

export async function run(): Promise<void> {
  initSplashDismiss();
  initSplashVjQr();

  await syncVjTokensWithDeployMeta();
  await loadHostCapabilities();
  const joinedSid = persistVjSessionFromUrl();
  if (joinedSid) reconnectVjSession();
  else if (!isVjViewOnlyMode()) void ensureVjTokens().catch(() => {});
  if (typeof window !== 'undefined' && /[?&]bulk=1/.test(window.location.search)) {
    import('../thumbnailRenderer.js').then((m) => {
      (window as unknown as { renderThumbnailSyncForBulk: (src: string) => string | null }).renderThumbnailSyncForBulk = m.renderThumbnailSync;
    });
  }
  status('Starting...');

  onLocalStoreChange(() => {
    loadSequence().catch(() => {});
  });

  void initSplashBuildInfo().then((v) => {
    const tag = v?.releaseTag ? v.releaseTag : 'Development/Test';
    document.title = 'Macroverse 42 - The Wired Atelier [' + tag + ']';
    const verEl = el('statusVersion');
    if (verEl && v) verEl.textContent = 'v' + (v.gitRev || v.version || 'dev') + (v.gitDirty ? '+dirty' : '');
  });

  const canvas = el('canvas');
  if (!canvas) {
    status('Canvas element not found', true);
    return;
  }

  const glOpts = { preserveDrawingBuffer: true };
  const gl = (canvas as HTMLCanvasElement).getContext('webgl', glOpts) || (canvas as HTMLCanvasElement).getContext('experimental-webgl', glOpts);
  if (!gl) {
    status('WebGL not available', true);
    return;
  }
  const glCtx = gl as WebGLRenderingContext;
  glCtx.getExtension('OES_standard_derivatives');
  render.initGl(glCtx);
  canvas.addEventListener('mousemove', (e: MouseEvent) => {
    const r = canvas.getBoundingClientRect();
    render.setMouse((e.clientX - r.left) / r.width, 1 - (e.clientY - r.top) / r.height);
  });
  window.addEventListener('resize', () => render.resizeCanvas());
  const ro = new ResizeObserver(() => render.resizeCanvas());
  const area = document.querySelector('.preview-area');
  if (area) ro.observe(area);
  render.resizeCanvas();

  initSettings();
  initAppBar();
  initCommandPalette();
  registerCoreCommands();
  initLayoutTier();
  onLayoutTierChange((tier, prev) => {
    if (tier === 'desktop' && prev !== 'desktop') {
      import('./dockSystem.js').then((m) => m.closeAllDocks()).catch(() => {});
    }
    render.resizeCanvas();
  });
  initDockSystem();
  initBottomTabBar();
  window.addEventListener('macroverse:dock-change', () => render.resizeCanvas());
  initPreviewContextMenu();
  const canvasWrap = document.querySelector('.canvas-wrap');
  if (canvasWrap) {
    const popBtn = document.createElement('button');
    popBtn.type = 'button';
    popBtn.textContent = 'Pop out';
    popBtn.title = 'Open preview on another screen (mouse = XY pad)';
    popBtn.style.cssText = 'position:absolute;top:6px;right:6px;z-index:60;padding:3px 10px;font-size:10px;background:rgba(20,20,40,0.8);color:var(--amiga-copper);border:1px solid var(--bevel-dark);cursor:pointer;font-family:inherit;';
    popBtn.onclick = () => render.openPreviewPopOut();
    (canvasWrap as HTMLElement).style.position = 'relative';
    canvasWrap.appendChild(popBtn);
  }
  initFixButton();
  initScanlineToggle();
  initVignetteToggle();
  initCrtCodeToggle();
  initFullscreenButton();
  initMobileToggle();
  initRightTabs();
  initColResizer();
  initIndexResizer();
  initCodeEditor();
  initDragDropPreviewAndCode();
  initIndexPanelCollapse();
  initRightPanelCollapse();
  initToolbarMore();
  initIndexToolsToggle();
  initMobilePanels();
  initSidebarButtons();
  initUpdateButton();
  initBackendViewer();
  initControllerAutoMap();

  try {
    const s = await fetchSettings();
    if (s && typeof s === 'object') {
      setAppSettings(s);
      const tc = (s as { themeColors?: Record<string, string> }).themeColors;
      if (tc) applyTheme(tc);
    }
  } catch (_) {}

  const abort = new AbortController();
  const timeout = setTimeout(() => abort.abort(), 30000);

  try {
    await loadSequence({ signal: abort.signal });
    refreshSplashVjQr();
  } catch (e) {
    if (e instanceof Error && e.name === 'AbortError') {
      status('Load cancelled or timed out. Retry or open Settings to fix paths.', true);
    } else {
      status(String((e as Error)?.message || e), true);
    }
    requestSplashDismiss();
  } finally {
    clearTimeout(timeout);
    applyInitialView();
  }

  status('Ready.');
  maybeShowQuickStartGuide();
}

async function pollAgentOutput(outEl: HTMLElement | null, maxMs: number): Promise<void> {
  if (!outEl) return;
  const { fetchAgentOutput, fetchAgentStatus } = await import('../api.js');
  const start = Date.now();
  const poll = async () => {
    if (Date.now() - start > maxMs) return;
    try {
      const [j, st] = await Promise.all([fetchAgentOutput(), fetchAgentStatus()]);
      const text = j.output || '';
      if (outEl) outEl.textContent = text || 'Waiting for agent output...';
      const actions = document.getElementById('agentOutputActions');
      if (actions) (actions as HTMLElement).style.display = outEl.style.display === 'block' ? 'inline' : 'none';
      if (!st.online) return;
    } catch (_) {}
    setTimeout(poll, 1200);
  };
  setTimeout(poll, 800);
}

const MAX_AUTOFIX_ROUNDS = 50;

const TOKEN_CORRUPTIONS: [string, string][] = [
  ['Uuniform', 'uniform'],
  ['uuniform', 'uniform'],
  ['Ununiform', 'uniform'],
  ['unifrom', 'uniform'],
  ['flaot', 'float'],
];

function fixTokenCorruptions(content: string, compileErr: string): string {
  const m = compileErr.match(/'(\w+)'\s*:\s*syntax error/);
  if (!m) return content;
  const bad = m[1];
  for (const [from, to] of TOKEN_CORRUPTIONS) {
    if (bad === from && content.includes(from)) {
      return content.split(from).join(to);
    }
  }
  return content;
}

async function tryAutoFixThenRetryUntilCompiles(
  entry: typeof currentEntry,
  path: string,
  fixedContent: string,
  _prevError: string,
  name: string,
  _prevContent: string,
  apiKey: string,
  btn: HTMLButtonElement,
  agentOut: HTMLElement | null,
  _doConfirm: boolean
): Promise<void> {
  let content = stripLeadingGarbage(fixedContent);
  content = fixTokenCorruptions(content, _prevError);
  let round = 0;
  const previousErrors: string[] = [];
  const isISF = content.includes('"ISFVSN"');

  const syncEditor = (): void => {
    setCurrentSource(content);
    const sync = (globalThis as unknown as { syncCodeFromState?: () => void }).syncCodeFromState;
    if (typeof sync === 'function') sync();
  };

  while (round < MAX_AUTOFIX_ROUNDS) {
    round++;
    const lastErr = previousErrors[previousErrors.length - 1] || _prevError || '';
    content = fixTokenCorruptions(content, lastErr);
    setCurrentSource(content);
    syncEditor();
    try {
      render.render(content);
      clearLastCompileError();
      clearFixingState();
      showTagDeadButton(false);
      if (btn) {
        btn.style.display = 'none';
        btn.disabled = false;
        restoreFixButtonLabel(btn as HTMLElement, 'Fix');
      }
      status('Fixed. Save to persist.');
      const { loadShader } = await import('../render.js');
      const { buildParamsPanel } = await import('../panels/params.js');
      loadShader(entry);
      buildParamsPanel(entry);
      return;
    } catch (e) {
      const compileErr = e instanceof Error ? e.message : String(e);
      setLastCompileError(compileErr, path);
      previousErrors.push(compileErr.slice(0, 150));
      if (round >= MAX_AUTOFIX_ROUNDS) {
        clearFixingState();
        if (apiKey && apiKey !== '***') {
          status('Auto-fix exhausted. Escalating to Cursor AI...');
          setFixingState('cursor');
          try {
            const r = await fetch('/api/cursor-fix', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({
                path, error: compileErr, content, filename: name, isISF,
                confirm: true, cursorApiKey: getCursorApiKey(),
                attemptNumber: round + 1, previousErrors: previousErrors.slice(-5)
              })
            });
            const j = await r.json() as { error?: string; message?: string; rateLimit?: boolean };
            if (r.status === 429 || j.rateLimit) {
              status('Agent cooldown. Wait or use Open in Cursor.', true);
            } else if (j.error) {
              status('Agent: ' + j.error, true);
            } else {
              status(j.message || 'Agent launched. Click Reload when ready.');
              pollAgentOutput(agentOut, 60000);
              startAgentReloadPoll(entry);
            }
          } catch (agentErr) {
            status('Agent launch failed: ' + (agentErr instanceof Error ? agentErr.message : String(agentErr)), true);
          }
        } else {
          status('Auto-fix stopped after ' + MAX_AUTOFIX_ROUNDS + ' rounds. Add Cursor API key in Settings and click Fix again.', true);
        }
        if (btn) {
          btn.disabled = false;
          restoreFixButtonLabel(btn as HTMLElement, 'Fix');
        }
        return;
      }
      setFixingState('local');
      status('Auto-fix round ' + round + '/' + MAX_AUTOFIX_ROUNDS + ', retrying...');
      try {
        const r = await fetch('/api/cursor-fix', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            path,
            error: compileErr,
            content,
            filename: name,
            isISF,
            confirm: false,
            cursorApiKey: getCursorApiKey(),
            attemptNumber: round + 1,
            previousErrors: previousErrors.slice(-5)
          })
        });
        const j = await r.json() as { error?: string; needsAgent?: boolean; autoFix?: boolean; content?: string; message?: string; thinking?: string; triedSummary?: string; unrecoverable?: boolean };
            if (r.status === 429 || j.error) {
              clearFixingState();
              status(j.error || 'Fix failed', true);
              if (btn) { btn.disabled = false; restoreFixButtonLabel(btn as HTMLElement, 'Fix'); }
              return;
            }
            if (j.thinking && agentOut) {
              const prev = agentOut.textContent || '';
              agentOut.textContent = prev + '\n\n--- Round ' + round + ' ---\n' + j.thinking;
            }
            if (j.needsAgent) {
          clearFixingState();
          const reasonShort = (j as { reasonShort?: string }).reasonShort;
          const reason = (j as { reason?: string }).reason || j.message || 'No more auto-fix.';
          const verbose = (j as { verbose?: string }).verbose || '';
          const irreparable = !!(j as { irreparable?: boolean }).irreparable;
          const triedSummary = (j as { triedSummary?: string }).triedSummary || '';
          const unrecoverable = !!(j as { unrecoverable?: boolean }).unrecoverable;
          const errToShow = (j as { compileError?: string }).compileError || compileErr;
          const overlay = document.getElementById('previewCompileErrorOverlay');
          if (overlay) (overlay as HTMLElement).dataset.unrecoverable = unrecoverable ? '1' : '';
          showTagDeadButton(unrecoverable);
          if (apiKey && apiKey !== '***') {
            status('Auto-fix could not resolve. Escalating to Cursor AI...');
            setFixingState('cursor');
            const friendlyMsg = (reasonShort || reason) + (verbose ? '\n\n' + verbose : '');
            let agentText = (irreparable ? 'IRREPARABLE - ' : '') + friendlyMsg;
            if (triedSummary) agentText += '\n\nWhat was tried: ' + triedSummary;
            agentText += '\n\nEscalating to Cursor AI agent...';
            if (agentOut) (agentOut as HTMLElement).textContent = agentText;
            setLastCompileError(friendlyMsg, path);
            const errMsgEl = document.getElementById('previewErrorMsg');
            if (errMsgEl) (errMsgEl as HTMLElement).textContent = friendlyMsg;
            try {
              const ar = await fetch('/api/cursor-fix', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                  path, error: compileErr, content, filename: name, isISF,
                  confirm: true, cursorApiKey: getCursorApiKey(),
                  attemptNumber: round + 1, previousErrors: previousErrors.slice(-5)
                })
              });
              const aj = await ar.json() as { error?: string; message?: string; rateLimit?: boolean };
              clearFixingState();
              if (ar.status === 429 || aj.rateLimit) {
                setPendingCursorConfirm(true);
                status('Cursor API cooldown. Click Fix again when ready.', true);
                if (btn) { btn.disabled = false; btn.classList.add('fix-escalate'); restoreFixButtonLabel(btn as HTMLElement, 'Escalate to Cursor AI?'); btn.style.display = ''; }
              } else if (aj.error) {
                setPendingCursorConfirm(true);
                status('Agent: ' + aj.error, true);
                if (btn) { btn.disabled = false; btn.classList.add('fix-escalate'); restoreFixButtonLabel(btn as HTMLElement, 'Escalate to Cursor AI?'); btn.style.display = ''; }
              } else {
                setFixingState('cursor');
                status(aj.message || 'Cursor AI launched. Click Reload when ready.');
                restoreFixButtonLabel(btn as HTMLElement, 'Cursor AI working...');
                pollAgentOutput(agentOut, 60000);
                startAgentReloadPoll(entry);
                if (btn) { btn.disabled = false; }
              }
            } catch (agentErr) {
              clearFixingState();
              setPendingCursorConfirm(true);
              status('Agent launch failed: ' + (agentErr instanceof Error ? agentErr.message : String(agentErr)), true);
              if (btn) { btn.disabled = false; btn.classList.add('fix-escalate'); restoreFixButtonLabel(btn as HTMLElement, 'Escalate to Cursor AI?'); btn.style.display = ''; }
            }
          } else {
            setPendingCursorConfirm(true);
            status((irreparable ? 'IRREPARABLE - ' : '') + reason + ' Add Cursor API key in Settings and click Fix again.');
            if (btn) {
              btn.disabled = false;
              btn.classList.add('fix-escalate');
              restoreFixButtonLabel(btn as HTMLElement, 'Escalate to Cursor AI?');
              btn.style.display = '';
            }
            const irrepLine = irreparable ? 'IRREPARABLE - Fix cannot auto-resolve\n\n' : '';
            let out = irrepLine + (reasonShort || reason);
            if (verbose) out += '\n\n' + verbose;
            if (triedSummary) out += '\n\nWhat was tried: ' + triedSummary;
            if (agentOut) (agentOut as HTMLElement).textContent = out;
            const friendlyMsg = (reasonShort || reason) + (verbose ? '\n\n' + verbose : '');
            setLastCompileError(friendlyMsg, path);
            const errMsg = document.getElementById('previewErrorMsg');
            if (errMsg) (errMsg as HTMLElement).textContent = friendlyMsg;
            const irrepEl = document.getElementById('previewErrorIrrep');
            if (irrepEl) (irrepEl as HTMLElement).style.display = irreparable ? 'block' : 'none';
          }
          return;
        }
        if (j.autoFix && j.content) {
          content = stripLeadingGarbage(j.content);
          continue;
        }
        clearFixingState();
        status(j.message || 'No further auto-fix.');
        if (btn) { btn.disabled = false; restoreFixButtonLabel(btn as HTMLElement, 'Fix'); }
        return;
      } catch (err) {
        clearFixingState();
        status('Retry failed: ' + (err instanceof Error ? err.message : String(err)), true);
        if (btn) { btn.disabled = false; restoreFixButtonLabel(btn as HTMLElement, 'Fix'); }
        return;
      }
    }
  }
}

export function startAgentReloadPoll(entry: typeof currentEntry, maxMs = 120000): void {
  const start = Date.now();
  const poll = async (): Promise<void> => {
    if (Date.now() - start > maxMs) return;
    try {
      const st = await fetchAgentStatus();
      if (!st.online) {
        status('Agent finished. Click Reload to load new shader.');
        setPendingAgentReload(true);
        const reloadBtn = document.getElementById('codeReloadBtn');
        if (reloadBtn) reloadBtn.classList.add('reload-ready');
        window.dispatchEvent(new CustomEvent('agent-reload-ready', { detail: { entry } }));
        return;
      }
    } catch (_) {}
    setTimeout(() => poll(), 2000);
  };
  setTimeout(poll, 3000);
}

function initCompileOverlayApiKeys(): void {
  const cur = document.getElementById('previewOverlayCursorKey') as HTMLInputElement | null;
  const gh = document.getElementById('previewOverlayGithubToken') as HTMLInputElement | null;
  const hint = document.getElementById('previewErrorApiKeyHint');
  if (cur) {
    cur.value = getLocalCursorKeyStored();
    const saveC = () => {
      setLocalCursorApiKey(cur.value);
      if (hint) {
        const parts: string[] = [];
        if (getLocalCursorKeyStored()) parts.push('Cursor key stored in this browser');
        if (getLocalGithubTokenStored()) parts.push('GitHub token stored in this browser');
        hint.textContent = parts.length ? parts.join('. ') + '.' : '';
      }
    };
    cur.addEventListener('blur', saveC);
    cur.addEventListener('change', saveC);
  }
  if (gh) {
    gh.value = getLocalGithubTokenStored();
    const saveG = () => {
      setLocalGithubToken(gh.value);
      if (hint) {
        const parts: string[] = [];
        if (getLocalCursorKeyStored()) parts.push('Cursor key stored in this browser');
        if (getLocalGithubTokenStored()) parts.push('GitHub token stored in this browser');
        hint.textContent = parts.length ? parts.join('. ') + '.' : '';
      }
    };
    gh.addEventListener('blur', saveG);
    gh.addEventListener('change', saveG);
  }
  if (hint) {
    const parts: string[] = [];
    if (getLocalCursorKeyStored()) parts.push('Cursor key stored in this browser');
    if (getLocalGithubTokenStored()) parts.push('GitHub token stored in this browser');
    hint.textContent = parts.length ? parts.join('. ') + '.' : 'Values here are saved only in this browser. Settings app still has server-backed keys if you use them.';
  }
}

function initFixButton(): void {
  initCompileOverlayApiKeys();
  const btn = el('fixBtn');
  const previewFixBtn = el('previewFixBtn');
  const previewDismissBtn = el('previewDismissBtn');
  if (previewFixBtn) {
    previewFixBtn.onclick = () => { if (btn) (btn as HTMLButtonElement).click(); };
  }
  if (previewDismissBtn) {
    previewDismissBtn.onclick = () => {
      render.clearSessionForNewShader();
    };
  }
  const previewRevertBtn = document.getElementById('previewRevertBtn');
  if (previewRevertBtn) {
    previewRevertBtn.addEventListener('click', () => {
      const path = (lastCompileErrorPath || '').replace(/\\/g, '|');
      if (!path) {
        status('No shader path to revert', true);
        return;
      }
      fetchShader(path)
        .then((content) => {
          setCurrentSource(content);
          const sync = (globalThis as unknown as { syncCodeFromState?: () => void }).syncCodeFromState;
          if (typeof sync === 'function') sync();
          clearLastCompileError();
          render.setPreviewCompileErrorOverlayVisible(false);
          const overlay = document.getElementById('previewCompileErrorOverlay');
          if (overlay) delete (overlay as HTMLElement).dataset.unrecoverable;
          const fixBtn = document.getElementById('fixBtn');
          if (fixBtn) (fixBtn as HTMLElement).style.display = 'none';
          status('Reverted to last saved.');
          render.render(content);
        })
        .catch((e) => status('Revert failed: ' + (e?.message || 'load failed'), true));
    });
  }
  const previewFixGithubBtn = document.getElementById('previewFixGithubBtn');
  if (previewFixGithubBtn) {
    previewFixGithubBtn.addEventListener('click', async () => {
      const oc = document.getElementById('previewOverlayCursorKey') as HTMLInputElement | null;
      const og = document.getElementById('previewOverlayGithubToken') as HTMLInputElement | null;
      if (oc) setLocalCursorApiKey(oc.value);
      if (og) setLocalGithubToken(og.value);
      const path = (lastCompileErrorPath || '').replace(/\\/g, '|');
      const err = lastCompileError || '';
      const content = currentSource || '';
      if (!content) {
        status('No shader content to fix', true);
        return;
      }
      const prompt = err
        ? 'Fix the following GLSL/ISF shader. It currently fails to compile with this error:\n\n' + err + '\n\nReturn only the corrected shader source, no explanation.'
        : 'Improve or fix the following GLSL/ISF shader. Return only the shader source, no explanation.';
      try {
        status('Fix with GitHub...');
        (previewFixGithubBtn as HTMLButtonElement).disabled = true;
        const { postGithubAiFix } = await import('../api.js');
        const tok = getGithubToken();
        const { content: newContent } = await postGithubAiFix({ content, prompt, path, token: tok || undefined });
        if (!newContent) {
          status('GitHub returned no content', true);
          return;
        }
        setCurrentSource(newContent);
        const sync = (globalThis as unknown as { syncCodeFromState?: () => void }).syncCodeFromState;
        if (typeof sync === 'function') sync();
        clearLastCompileError();
        render.setPreviewCompileErrorOverlayVisible(false);
        render.render(newContent);
        status('Applied GitHub fix. Save to persist.');
      } catch (e) {
        const msg = e instanceof Error ? e.message : String(e);
        status('GitHub fix: ' + msg, true);
        if (msg.includes('401') || msg.includes('token')) {
          status('Add a GitHub token in this overlay or Settings (GitHub section)', true);
        }
      } finally {
        (previewFixGithubBtn as HTMLButtonElement).disabled = false;
      }
    });
  }
  const tagDeadBtn = document.getElementById('previewTagDeadBtn');
  if (tagDeadBtn) {
    tagDeadBtn.onclick = async () => {
      const entry = currentEntry;
      if (!entry) { status('No shader selected', true); return; }
      const tags = [...new Set([...(entry.tags || []), 'dead'])];
      try {
        const { postUpdate } = await import('../api.js');
        await postUpdate({ id: entry.id, tags });
        entry.tags = tags;
        if (currentEntry && currentEntry.id === entry.id) currentEntry.tags = tags;
        const { buildList } = await import('../list.js');
        buildList();
        render.clearSessionForNewShader();
        showTagDeadButton(false);
        status('Shader tagged as DEAD. Enable "Show dead" in the index to see it again.');
      } catch (e) {
        status('Tag DEAD failed: ' + (e instanceof Error ? e.message : String(e)), true);
      }
    };
  }
  if (!btn) return;
  (btn as HTMLButtonElement).onclick = () => {
    const b = btn as HTMLButtonElement;
    if (b.disabled) return;
    const oc = document.getElementById('previewOverlayCursorKey') as HTMLInputElement | null;
    const og = document.getElementById('previewOverlayGithubToken') as HTMLInputElement | null;
    if (oc) setLocalCursorApiKey(oc.value);
    if (og) setLocalGithubToken(og.value);
    b.classList.remove('fix-escalate');
    clearFixingState();
    const agentOut = document.getElementById('agentOutput');
    const agentPane = document.getElementById('agentOutputPane');
    if (agentOut) {
      (agentOut as HTMLElement).style.display = 'block';
      (agentOut as HTMLElement).textContent = 'Waiting for agent...';
      if (agentPane) (agentPane as HTMLElement).classList.remove('collapsed');
      const actions = document.getElementById('agentOutputActions');
      if (actions) (actions as HTMLElement).style.display = 'inline';
    }
    const path = (lastCompileErrorPath || '').replace(/\\/g, '|');
    const err = lastCompileError || '';
    const entry = currentEntry;
    const name = entry ? (entry.fixedName || entry.name || '') : '';
    const content = stripLeadingGarbage(currentSource || '');
    if (!path && !err) {
      status('No compile error to fix. Open a shader that fails to compile.', true);
      return;
    }
    const apiKey = getCursorApiKey();
    const doConfirm = getPendingCursorConfirm() || !!apiKey;
    setPendingCursorConfirm(false);
    showTagDeadButton(false);
    if (doConfirm) {
      status('Launching Cursor agent... (Cursor API)');
      setFixingState('cursor');
      restoreFixButtonLabel(b, 'Cursor AI working...');
    } else {
      status('Trying local + Ollama fixes...');
      setFixingState('local');
      restoreFixButtonLabel(b, 'Fixing (local + AI)...');
    }
    b.disabled = true;
    if (doConfirm) setCursorApiThinking(true);
    fetch('/api/cursor-fix', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        path, error: err, content: content || null, filename: name,
        isISF: content.includes('"ISFVSN"'), confirm: doConfirm, cursorApiKey: apiKey,
        attemptNumber: 1, previousErrors: []
      })
    })
      .then((r) => {
        const is429 = r.status === 429;
        return r.json().then((j: { error?: string; message?: string; needsAgent?: boolean; autoFix?: boolean; content?: string; compileError?: string; rateLimit?: boolean; cooldownRemainingSec?: number }) => ({ r, j, is429 }));
      })
      .then(({ j, is429 }) => {
        const b = btn as HTMLButtonElement;
        b.disabled = false;
        clearFixingState();
        if (doConfirm) setCursorApiThinking(false);
        if (is429 || j.rateLimit) {
          const sec = typeof (j as { cooldownRemainingSec?: number }).cooldownRemainingSec === 'number'
            ? (j as { cooldownRemainingSec: number }).cooldownRemainingSec
            : 15;
          startCooldownCountdown(sec, (s) => {
            status('Cursor API cooldown: ' + s + 's. Or use Open in Cursor.', true);
          });
          restoreFixButtonLabel(b, 'Fix');
          return;
        }
        if (j.error) {
          let msg = 'Fix: ' + j.error;
          if (/rate|limit|verify|human|captcha/i.test(j.error)) {
            msg += ' Wait a few minutes or use Open in Cursor and run Agent from Cursor IDE.';
          }
          status(msg, true);
          restoreFixButtonLabel(b, 'Fix');
          console.warn('[Fix] Error:', j.error);
          return;
        }
        if (j.autoFix && j.content) {
          const fixStage = (j as { fixStage?: string }).fixStage || 'local';
          const thinking = (j as { thinking?: string }).thinking || '';
          if (thinking && agentOut) {
            (agentOut as HTMLElement).style.display = 'block';
            (agentOut as HTMLElement).textContent = (fixStage === 'ollama' ? '[Ollama LLM fix]\n' : '[Local regex fix]\n') + thinking;
            const agentPane = document.getElementById('agentOutputPane');
            if (agentPane) (agentPane as HTMLElement).classList.remove('collapsed');
            const actions = document.getElementById('agentOutputActions');
            if (actions) (actions as HTMLElement).style.display = 'inline';
          }
          status(fixStage === 'ollama' ? 'Ollama found a fix. Compiling...' : 'Local fix found. Compiling...');
          setFixingState(fixStage === 'ollama' ? 'ollama' : 'local');
          tryAutoFixThenRetryUntilCompiles(entry, path, stripLeadingGarbage(j.content), err, name, content, apiKey, b, agentOut as HTMLElement | null, doConfirm);
          return;
        }
        if (j.needsAgent) {
          setPendingCursorConfirm(true);
          if (doConfirm) setCursorApiThinking(false);
          const reasonShort = (j as { reasonShort?: string }).reasonShort;
          const reason = (j as { reason?: string }).reason || j.message || 'No auto-fix.';
          const verbose = (j as { verbose?: string }).verbose || '';
          const irreparable = !!(j as { irreparable?: boolean }).irreparable;
          const triedSummary = (j as { triedSummary?: string }).triedSummary || '';
          const unrecoverable = !!(j as { unrecoverable?: boolean }).unrecoverable;
          const errToShow = j.compileError || '';
          const overlay = document.getElementById('previewCompileErrorOverlay');
          if (overlay) (overlay as HTMLElement).dataset.unrecoverable = unrecoverable ? '1' : '';
          const statusLine = reasonShort || reason;
          status((irreparable ? 'IRREPARABLE - ' : '') + statusLine + ' Click Fix again for Cursor agent.');
          b.classList.add('fix-escalate');
          restoreFixButtonLabel(b, 'Escalate to Cursor AI?');
          b.style.display = '';
          showTagDeadButton(unrecoverable);
          const irrepLine = irreparable ? 'IRREPARABLE - Fix cannot auto-resolve\n\n' : '';
          let out = irrepLine + (reasonShort || reason);
          if (verbose) out += '\n\n' + verbose;
          if (triedSummary) out += '\n\nWhat was tried: ' + triedSummary;
          if (errToShow) out += '\n\nCompile error:\n' + errToShow;
          if (agentOut) (agentOut as HTMLElement).textContent = out;
          const irrepEl = document.getElementById('previewErrorIrrep');
          if (irrepEl) (irrepEl as HTMLElement).style.display = irreparable ? 'block' : 'none';
          const hasApiKey = apiKey !== '';
          const box = document.createElement('div');
          box.style.cssText = 'position:fixed;inset:0;z-index:10003;background:rgba(0,0,0,0.75);display:flex;align-items:center;justify-content:center;';
          const card = document.createElement('div');
          card.style.cssText = 'background:var(--amiga-panel);border:2px solid var(--amiga-copper);padding:20px;max-width:420px;font-family:inherit;';
          card.innerHTML = '<div style="font-weight:bold;margin-bottom:10px;color:var(--amiga-copper);">Auto-fix could not resolve</div>' +
            '<p style="margin:0 0 12px;font-size:13px;">' + (reasonShort || reason) + '</p>' +
            (hasApiKey
              ? '<p style="margin:0 0 14px;font-size:12px;color:var(--crt-dim);">Launch Cursor AI to try to fix? (Uses API tokens)</p>'
              : '<p style="margin:0 0 14px;font-size:12px;color:var(--crt-dim);">Add Cursor API key in Settings to use Cursor AI.</p>');
          const btnWrap = document.createElement('div');
          btnWrap.style.cssText = 'display:flex;gap:8px;justify-content:flex-end;';
          const cancelBtn = document.createElement('button');
          cancelBtn.type = 'button';
          cancelBtn.className = 'wire-btn';
          cancelBtn.textContent = 'Cancel';
          cancelBtn.onclick = () => { box.remove(); };
          const launchBtn = document.createElement('button');
          launchBtn.type = 'button';
          launchBtn.className = 'wire-btn';
          launchBtn.textContent = 'Launch Cursor AI';
          launchBtn.style.background = 'var(--amiga-copper)';
          launchBtn.style.color = 'var(--amiga-bg)';
          launchBtn.onclick = () => {
            box.remove();
            setPendingCursorConfirm(false);
            status('Launching Cursor agent...');
            setFixingState('cursor');
            restoreFixButtonLabel(b, 'Cursor AI working...');
            b.disabled = true;
            setCursorApiThinking(true);
            fetch('/api/cursor-fix', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({
                path, error: err, content: content || null, filename: name,
                isISF: content.includes('"ISFVSN"'), confirm: true, cursorApiKey: getCursorApiKey(),
                attemptNumber: 1, previousErrors: []
              })
            })
              .then((r) => r.json().then((jj: Record<string, unknown>) => ({ r, jj })))
              .then(({ r, jj }) => {
                b.disabled = false;
                setCursorApiThinking(false);
                if (r.status === 429 || jj.rateLimit) {
                  status('Cursor API cooldown. Wait or use Open in Cursor.', true);
                  restoreFixButtonLabel(b, 'Fix');
                  return;
                }
                if (jj.error) {
                  status('Agent: ' + String(jj.error), true);
                  restoreFixButtonLabel(b, 'Fix');
                  return;
                }
                if (jj.autoFix && jj.content) {
                  setFixingState('local');
                  tryAutoFixThenRetryUntilCompiles(entry, path, stripLeadingGarbage(String(jj.content)), err, name, content, apiKey, b, agentOut as HTMLElement | null, true);
                  return;
                }
                if (jj.needsAgent) {
                  status(String(jj.message || jj.reason || 'Agent could not fix.'), true);
                  restoreFixButtonLabel(b, 'Fix');
                  return;
                }
                status(String(jj.message || 'Agent launched. Click Reload when ready.'));
                restoreFixButtonLabel(b, 'Fix');
                pollAgentOutput(agentOut as HTMLElement | null, 30000);
                startAgentReloadPoll(entry);
              })
              .catch((e) => {
                b.disabled = false;
                setCursorApiThinking(false);
                status('Fix: ' + (e?.message || 'failed'), true);
                restoreFixButtonLabel(b, 'Fix');
              });
          };
          if (!hasApiKey) launchBtn.disabled = true;
          btnWrap.appendChild(cancelBtn);
          btnWrap.appendChild(launchBtn);
          card.appendChild(btnWrap);
          box.appendChild(card);
          box.onclick = (e) => { if (e.target === box) box.remove(); };
          card.onclick = (e) => e.stopPropagation();
          document.body.appendChild(box);
          return;
        }
        setPendingCursorConfirm(false);
        setFixingState('cursor');
        status(j.message || 'Agent launched. Reload will flash when ready.');
        restoreFixButtonLabel(b, 'Cursor AI working...');
        pollAgentOutput(agentOut as HTMLElement | null, 30000);
        startAgentReloadPoll(entry);
      })
      .catch((e) => {
        setPendingCursorConfirm(false);
        (btn as HTMLButtonElement).disabled = false;
        clearFixingState();
        const msg = e?.message || 'failed';
        status('Fix: ' + msg, true);
        restoreFixButtonLabel(btn as HTMLElement, 'Fix');
        console.warn('[Fix] Failed:', msg);
      });
  };
}

const SCANLINE_STORAGE_KEY = 'macroverse-scanline';
const VIGNETTE_STORAGE_KEY = 'macroverse-vignette';

/** Toggle a display effect by storage key and reapply state.
 *  Exposed on globalThis so the command palette / commands can fire
 *  these without relying on a checkbox being present in the DOM.
 *  After Phase 7, the checkboxes live in the Settings panel which is
 *  only built on first open, so reaching for getElementById would
 *  be a no-op until the user opens Settings. */
function toggleEffectByKey(key: string): void {
  const cur = localStorage.getItem(key) === 'true';
  localStorage.setItem(key, String(!cur));
  applyDisplayEffectState();
}
(globalThis as unknown as Record<string, unknown>).toggleDisplayEffect = (name: 'scanline' | 'vignette' | 'crt') => {
  if (name === 'scanline') toggleEffectByKey(SCANLINE_STORAGE_KEY);
  else if (name === 'vignette') toggleEffectByKey(VIGNETTE_STORAGE_KEY);
  else if (name === 'crt') toggleCodeView();
};

/** Apply persisted display-effect state to the DOM. Idempotent. */
function applyDisplayEffectState(): void {
  const area = document.querySelector('.preview-area');
  if (area) {
    area.classList.toggle('scanline-on',
      localStorage.getItem(SCANLINE_STORAGE_KEY) === 'true');
    area.classList.toggle('vignette-on',
      localStorage.getItem(VIGNETTE_STORAGE_KEY) === 'true');
  }
  applyCodeViewState();
  const sn = document.getElementById('scanlineToggle') as HTMLInputElement | null;
  const vg = document.getElementById('vignetteToggle') as HTMLInputElement | null;
  if (sn) sn.checked = localStorage.getItem(SCANLINE_STORAGE_KEY) === 'true';
  if (vg) vg.checked = localStorage.getItem(VIGNETTE_STORAGE_KEY) === 'true';
}

/** Wire all three display-effect toggles using event delegation so it
 *  works for inputs that appear lazily (e.g. when the Settings panel
 *  is built on first open). */
function initScanlineToggle(): void {
  applyDisplayEffectState();
  document.addEventListener('change', (ev) => {
    const t = ev.target as HTMLInputElement | null;
    if (!t || t.tagName !== 'INPUT' || t.type !== 'checkbox') return;
    if (t.id === 'scanlineToggle') {
      localStorage.setItem(SCANLINE_STORAGE_KEY, String(t.checked));
    } else if (t.id === 'vignetteToggle') {
      localStorage.setItem(VIGNETTE_STORAGE_KEY, String(t.checked));
    } else if (t.id === 'crtCodeToggle') {
      localStorage.setItem(CODE_VIEW_STORAGE_KEY, String(t.checked));
    } else {
      return;
    }
    applyDisplayEffectState();
  });
  // Re-apply when settings panel opens (any DOM mutation that might
  // re-create the checkboxes).
  const obs = new MutationObserver(() => applyDisplayEffectState());
  obs.observe(document.body, { childList: true, subtree: true });
}

function initVignetteToggle(): void { /* covered by initScanlineToggle */ }
function initCrtCodeToggle(): void  { /* covered by initScanlineToggle */ }

function isFullscreen(): boolean {
  return !!(document.fullscreenElement ?? (document as unknown as { webkitFullscreenElement?: Element }).webkitFullscreenElement);
}

function updateFullscreenButton(): void {
  const btn = document.getElementById('fullscreenBtn');
  if (!btn) return;
  (btn as HTMLButtonElement).textContent = isFullscreen() ? 'Exit full screen' : 'Full screen';
}

function enterFullscreen(elem: HTMLElement): void {
  if (elem.requestFullscreen) {
    void elem.requestFullscreen();
  } else {
    (elem as unknown as { webkitRequestFullscreen?: () => void }).webkitRequestFullscreen?.();
  }
}

function exitFullscreen(): void {
  if (document.exitFullscreen) {
    void document.exitFullscreen();
  } else {
    (document as unknown as { webkitExitFullscreen?: () => void }).webkitExitFullscreen?.();
  }
}

function initFullscreenButton(): void {
  const btn = document.getElementById('fullscreenBtn');
  if (!btn) return;
  const el = document.documentElement;
  btn.addEventListener('click', () => {
    if (isFullscreen()) { exitFullscreen(); } else { enterFullscreen(el); }
  });
  document.addEventListener('fullscreenchange', updateFullscreenButton);
  document.addEventListener('webkitfullscreenchange', updateFullscreenButton);
  document.addEventListener('keydown', (e) => {
    if (e.key === 'F11') {
      e.preventDefault();
      if (isFullscreen()) { exitFullscreen(); } else { enterFullscreen(el); }
    }
  });
  updateFullscreenButton();
}

// ── Mobile / Desktop toggle ──────────────────────────────────────────────────
function initMobileToggle(): void {
  const btn = document.getElementById('mobileToggleBtn');
  if (!btn) return;
  const devLayout =
    typeof location !== 'undefined' &&
    new URLSearchParams(location.search).get('dev') === '1';
  if (isLayoutPhone() && !devLayout) {
    btn.style.display = 'none';
    return;
  }
  const stored = localStorage.getItem('forceMobile');
  if (stored === '1') document.documentElement.classList.add('force-mobile');

  function updateLabel(): void {
    btn.textContent = document.documentElement.classList.contains('force-mobile')
      ? 'Desktop' : 'Mobile';
  }
  updateLabel();
  btn.addEventListener('click', () => {
    const isMobile = document.documentElement.classList.toggle('force-mobile');
    localStorage.setItem('forceMobile', isMobile ? '1' : '0');
    applyLayoutTier();
    updateLabel();
    render.resizeCanvas();
  });
}

// ── Read-only / demo banner (host mode loaded earlier in run()) ───────────────
export async function applyServerConfig(): Promise<void> {
  await loadHostCapabilities();
}

function initRightTabs(): void {
  const tabs = document.querySelectorAll('.right-tab');
  if (tabs.length === 0) return;
  tabs.forEach((tab) => {
    tab.addEventListener('click', () => {
      const t = tab as HTMLElement;
      const panelId = t.dataset.tab === 'params' ? 'paramsPanel' : 'isfPanel';
      document.querySelectorAll('.right-tab').forEach((x) => x.classList.remove('active'));
      document.querySelectorAll('.right-panel').forEach((x) => x.classList.remove('active'));
      t.classList.add('active');
      const p = document.getElementById(panelId);
      if (p) p.classList.add('active');
    });
  });
}

function initColResizer(): void {
  const res = el('colResizer');
  const main = res?.closest('.main');
  if (!res || !main) return;
  let down = false;
  const MIN_RIGHT = 200;
  const MAX_RIGHT = 600;
  const onMove = (e: MouseEvent) => {
    if (!down) return;
    const rect = main.getBoundingClientRect();
    const rightWidth = Math.max(MIN_RIGHT, Math.min(MAX_RIGHT, rect.right - e.clientX - 4));
    (main as HTMLElement).style.gridTemplateColumns = `minmax(0, 1fr) 8px ${rightWidth}px`;
  };
  res.onmousedown = () => {
    if (isCompactLayout()) return;
    down = true;
    document.body.style.cursor = 'col-resize';
  };
  document.addEventListener('mouseup', () => { down = false; document.body.style.cursor = ''; });
  document.addEventListener('mousemove', onMove);
}

function initSidebarButtons(): void {
  const rescanBtn = document.getElementById('sidebarRescanBtn');
  if (rescanBtn) {
    rescanBtn.addEventListener('click', async () => {
      rescanBtn.textContent = '...';
      (rescanBtn as HTMLButtonElement).disabled = true;
      try {
        const resp = await fetch('/api/native-scan', { method: 'POST' });
        const j = await resp.json();
        if (j.ok) {
          status('Scan: ' + j.total + ' shaders (' + j.added + ' added, ' + j.removed + ' removed)');
          const { loadSequence: ls } = await import('./loadSequence.js');
          ls();
        } else {
          status('Scan failed', true);
        }
      } catch (e) {
        status('Scan: ' + ((e as Error)?.message || 'failed'), true);
      } finally {
        rescanBtn.textContent = 'Scan';
        (rescanBtn as HTMLButtonElement).disabled = false;
      }
    });
  }

  const helpBtn = document.getElementById('sidebarHelpBtn');
  if (helpBtn) {
    helpBtn.addEventListener('click', () => showHelpModal());
  }
}

function showHelpModal(): void {
  let existing = document.getElementById('helpOverlay');
  if (existing) { existing.remove(); return; }

  const overlay = document.createElement('div');
  overlay.id = 'helpOverlay';
  overlay.className = 'help-overlay' + (isCompactLayout() ? ' help-overlay--sheet' : '');
  overlay.style.cssText = '';
  overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };

  const box = document.createElement('div');
  box.className = 'help-overlay-box';
  box.onclick = (e) => e.stopPropagation();

  const header = document.createElement('div');
  header.className = 'help-overlay-header';
  header.innerHTML = '<span class="help-overlay-title">Macroverse Help</span>';
  const closeBtn = document.createElement('button');
  closeBtn.type = 'button';
  closeBtn.className = 'help-overlay-close';
  closeBtn.textContent = 'Close';
  closeBtn.setAttribute('aria-label', 'Close help');
  closeBtn.onclick = () => overlay.remove();
  header.appendChild(closeBtn);

  const content = document.createElement('div');
  content.className = 'help-overlay-body';
  content.innerHTML = `
<div style="margin-bottom:16px">
  <div style="color:var(--amiga-copper);font-size:13px;margin-bottom:6px;font-weight:bold">GETTING STARTED</div>
  <div style="padding-left:8px;border-left:2px solid var(--amiga-accent);margin-bottom:8px">
    <b>1.</b> Click a shader in the index list below to preview it<br>
    <b>2.</b> Use <b>Preview / Code / Split H / Split V</b> tabs to switch views<br>
    <b>3.</b> Adjust parameter sliders on the right panel<br>
    <b>4.</b> Click <b>Save</b> to persist changes (auto git-commits if enabled)
  </div>
</div>

<div style="margin-bottom:16px">
  <div style="color:var(--amiga-copper);font-size:13px;margin-bottom:6px;font-weight:bold">EXPOSE PARAMETERS (turn numbers into sliders)</div>
  <div style="padding-left:8px;border-left:2px solid #2ecc71;margin-bottom:8px">
    <b>Expose (instant):</b> Click <b>Expose</b> in toolbar for regex-based detection. Local-first, no tokens.<br>
    <b>AI analysis:</b> Use the AI button for deeper LLM-based discovery of exposable values.<br>
    <b>Search:</b> Click <b>Search</b> in toolbar. Popover shows numeric literals + named values. Click <b>Expose</b> next to any to make it a slider.<br>
    <b>Sweet spots:</b> Gold dashed underlines in code = potentially exposable values. Right-click one to expose it.<br>
    <b>Format:</b> <code>uniform float name; // @expose min max</code><br>
    <b>Save</b> after exposing to keep changes.
  </div>
</div>

<div style="margin-bottom:16px">
  <div style="color:var(--amiga-copper);font-size:13px;margin-bottom:6px;font-weight:bold">WIRE / ISF EXPORT</div>
  <div style="padding-left:8px;border-left:2px solid #44aadd;margin-bottom:8px">
    <b>Clipboard to Wire:</b> Click <b>Clipboard to Wire</b> to copy ISF (with INPUTS array) to clipboard. Paste in Wire.<br>
    <b>Includes:</b> useFrameIndex toggle (ON by default -- FRAMEINDEX drives animation in Wire), fps, timeScale, mouseX/Y as built-in ISF INPUTS.<br>
    <b>Textures:</b> sampler2D uniforms become ISF image INPUTS, texture2D() becomes IMG_NORM_PIXEL().<br>
    <b>Check ISF Wire:</b> Validates compatibility with ISF Wire (exposed params and sampler2D if appropriate).
  </div>
</div>

<div style="margin-bottom:16px">
  <div style="color:var(--amiga-copper);font-size:13px;margin-bottom:6px;font-weight:bold">VIBE STATION (create new shaders)</div>
  <div style="padding-left:8px;border-left:2px solid var(--amiga-copper);margin-bottom:8px">
    <b>Modify current:</b> Takes screenshot + your description, sends to AI agent to modify the shader visually.<br>
    <b>Create new:</b> Type a name, pick a genre (Particles, Fractal, 3D Sphere/Cube/Torus, Tunnel, Kaleidoscope, Audio, Gradient), optionally describe your vision, click Create.<br>
    <b>3D objects:</b> 3D genres use raymarching with SDF functions -- real 3D geometry in a fragment shader.
  </div>
</div>

<div style="margin-bottom:16px">
  <div style="color:var(--amiga-copper);font-size:13px;margin-bottom:6px;font-weight:bold">VJ SCRATCHPAD</div>
  <div style="padding-left:8px;border-left:2px solid var(--amiga-accent);margin-bottom:8px">
    <b>Access:</b> Click the <b>VJ</b> tab.<br>
    <b>A/B decks:</b> Load different shaders in Deck A and Deck B, tweak params independently.<br>
    <b>Crossfader:</b> Blend between decks.<br>
    <b>Mix modes:</b> Crossfade, Alpha Layer, Add, Multiply.
  </div>
</div>

<div style="margin-bottom:16px">
  <div style="color:var(--amiga-copper);font-size:13px;margin-bottom:6px;font-weight:bold">PIPELINE &amp; WIRE HUB (LOCAL / DESKTOP)</div>
  <div style="padding-left:8px;border-left:2px solid #44ffaa;margin-bottom:8px">
    <b>Pipeline:</b> Visual map of the signal flow (shader, MIDI, FFT, WebGL, VJ, Wire). Quick links to classify shaders and batch-generate patches.<br>
    <b>Wire Hub:</b> Pick shaders, choose topology (feedback, beat, glitch, etc.), generate <code>.wire</code> patches and Avenue <code>.avc</code> compositions. Opens patches in Resolume Wire when installed.<br>
    <b>Cloud vs local:</b> On macroverse.aday.net.au these tabs are hidden. Run Macroverse on your Windows PC (or self-host with Resolume Wire) for the full export pipeline. Use <b>Clipboard to Wire</b> on any host to paste ISF into Wire manually.<br>
    <b>Empty library?</b> Click <b>Gen Sources</b> or <b>Generate .wire</b> after selecting shaders. Wire files land in the <code>resolume/</code> folder next to the app.
  </div>
</div>

<div style="margin-bottom:16px">
  <div style="color:var(--amiga-copper);font-size:13px;margin-bottom:6px;font-weight:bold">CLOUD: BROWSER-LOCAL EDITS</div>
  <div style="padding-left:8px;border-left:2px solid #44ffaa;margin-bottom:8px">
    On the cloud site the server shader library is read-only. You can still edit code, change tags/favorites, create shaders, and tweak UI settings — changes save in <b>IndexedDB in this browser</b>, not on the server.<br>
    <b>Settings &gt; Browser-local edits:</b> export a JSON backup, import on another machine, or clear local overrides. Run Macroverse locally to write files to disk and use Resolume Wire.
  </div>
</div>

<div style="margin-bottom:16px">
  <div style="color:var(--amiga-copper);font-size:13px;margin-bottom:6px;font-weight:bold">LLM PROVIDER CHAIN</div>
  <div style="padding-left:8px;border-left:2px solid var(--amiga-accent);margin-bottom:8px">
    <b>Priority:</b> Local (regex) -> Ollama (free) -> Cursor (cloud). Configure in Settings > LLM Provider Chain.<br>
    <b>Enable/disable:</b> Toggle each provider. Reorder for priority.<br>
    <b>Ollama:</b> Optional. Install from ollama.com, then run "ollama pull llama3.2" (or any model). Configure in Settings.
  </div>
</div>

<div style="margin-bottom:16px">
  <div style="color:var(--amiga-copper);font-size:13px;margin-bottom:6px;font-weight:bold">VIRTUAL WEBCAM / OBS</div>
  <div style="padding-left:8px;border-left:2px solid var(--amiga-accent);margin-bottom:8px">
    <b>MacroCam stream:</b> <code>http://localhost:8765/api/output/macrocam/stream</code><br>
    <b>OBS:</b> Use as Media Source or Browser Source.<br>
    <b>Linux:</b> Pipe to v4l2loopback with ffmpeg for virtual webcam.
  </div>
</div>

<div style="margin-bottom:16px">
  <div style="color:var(--amiga-copper);font-size:13px;margin-bottom:6px;font-weight:bold">FIX ERRORS</div>
  <div style="padding-left:8px;border-left:2px solid #aa4444;margin-bottom:8px">
    <b>Fix chain:</b> Local regex (free) -> Ollama (free) -> Cursor agent (cloud). Configure in Settings.<br>
    <b>Fix button:</b> When shader has compile errors, click <b>Fix</b>. Tries local patterns first, then LLM if needed.<br>
    <b>Safe:</b> Fixes are in-memory only. You must click <b>Save</b> to persist.
  </div>
</div>

<div style="margin-bottom:16px">
  <div style="color:var(--amiga-copper);font-size:13px;margin-bottom:6px;font-weight:bold">VERSION CONTROL</div>
  <div style="padding-left:8px;border-left:2px solid #9b59b6;margin-bottom:8px">
    <b>Auto-commit:</b> Every Save auto-commits to git (if enableGit is on in Settings).<br>
    <b>History:</b> Right-click a shader in the list, choose "See versions..." to see commit history.<br>
    <b>Revert:</b> Click any version to revert to it. Click <b>Revert</b> in toolbar to go back to last commit.<br>
    <b>Undo:</b> Click <b>Undo</b> in toolbar for in-editor undo (does not save).
  </div>
</div>

<div style="margin-bottom:16px">
  <div style="color:var(--amiga-copper);font-size:13px;margin-bottom:6px;font-weight:bold">EXTERNAL CONTROLS</div>
  <div style="padding-left:8px;border-left:2px solid var(--crt-dim);margin-bottom:8px">
    <b>OSC:</b> Expand OSC section, set port, click Listen. Send UDP messages to <code>/shader/paramName</code>.<br>
    <b>MIDI:</b> Expand MIDI section, click Enable. Click <b>Learn</b> next to a param, then move a CC knob.<br>
    <b>Audio FFT:</b> Expand Audio section, click Start. Map frequency bands (Sub-Brilliance) to params.<br>
    <b>Textures:</b> Expand Texture inputs. Click <b>Add sampler2D</b>, then <b>Webcam</b> or <b>Image</b> to attach source.
  </div>
</div>

<div style="margin-bottom:16px">
  <div style="color:var(--amiga-copper);font-size:13px;margin-bottom:6px;font-weight:bold">OUTPUT</div>
  <div style="padding-left:8px;border-left:2px solid var(--crt-dim);margin-bottom:8px">
    <b>MacroCam:</b> MJPEG stream at <code>/api/output/macrocam/stream</code> for OBS, virtual webcam (desktop / local install only).<br>
    <b>Spout/NDI:</b> Right-click the preview canvas (desktop only). Platform support varies (e.g. Spout on Windows). Requires wire-output binary.
  </div>
</div>

<div style="margin-bottom:16px">
  <div style="color:var(--amiga-copper);font-size:13px;margin-bottom:6px;font-weight:bold">CRT EFFECTS</div>
  <div style="padding-left:8px;border-left:2px solid var(--crt-dim);margin-bottom:8px">
    <b>Scanlines:</b> Check <b>Scan</b> in status bar for horizontal scan lines on preview.<br>
    <b>Vignette:</b> Check <b>Vignette</b> for darkened corners on preview.<br>
    <b>CRT:</b> Check <b>CRT</b> for phosphor glow effect on code editor.<br>
    <b>Full screen:</b> Click <b>Full screen</b> in status bar or press F11.<br>
    <b>Theme:</b> Click <b>Settings</b> to access HSV color sliders for full theme customization.
  </div>
</div>

<div style="margin-bottom:16px">
  <div style="color:var(--amiga-copper);font-size:13px;margin-bottom:6px;font-weight:bold">DEBUG: GRAVEYARD (BROKEN SHADERS LOG)</div>
  <div style="padding-left:8px;border-left:2px solid var(--crt-dim);margin-bottom:8px">
    Shaders that fail to compile and are marked <b>unrecoverable</b> are logged to a JSON file (path, compile error, tried-summary). Use it to batch-fix later with an agent or script.<br>
    <b>Path:</b> Shown in <b>Settings</b> under <b>Debug: Graveyard</b>. File is <code>unrecoverable-shaders.json</code> in the same directory as the shader index (macroverse.db).
  </div>
</div>

<div style="margin-bottom:16px">
  <div style="color:var(--amiga-copper);font-size:13px;margin-bottom:6px;font-weight:bold">KEYBOARD SHORTCUTS</div>
  <div style="padding-left:8px;border-left:2px solid var(--crt-dim)">
    <b>Ctrl+S:</b> Save shader<br>
    <b>Ctrl+Z:</b> Undo in editor<br>
    <b>F11:</b> Toggle full screen<br>
    <b>Right-click preview:</b> Output options — Spout, NDI, MacroCam MJPEG (desktop / local install only)<br>
    <b>Right-click code:</b> AI actions (Explain, Enhance, Refactor, Generate)
  </div>
</div>

<div style="text-align:center;color:var(--crt-dim);font-size:10px;margin-top:12px;border-top:1px solid var(--bevel-dark);padding-top:8px">
  Macroverse 42 - The Wired Atelier. Ride the signal. GLSL shaders for Resolume Wire.
</div>`;

  box.appendChild(header);
  box.appendChild(content);
  overlay.appendChild(box);
  document.body.appendChild(overlay);
}

const SHADER_PATH_DROP_TYPE = 'application/x-macroverse-shader-path';

function initDragDropPreviewAndCode(): void {
  const previewArea = document.querySelector('.preview-area');
  const codeWrap = document.getElementById('codeWrap');
  const dropTargets = [previewArea, codeWrap].filter(Boolean) as HTMLElement[];

  function allowDrop(ev: DragEvent): void {
    if (!ev.dataTransfer) return;
    if (ev.dataTransfer.types.includes('text/plain') || ev.dataTransfer.types.includes(SHADER_PATH_DROP_TYPE)) {
      ev.preventDefault();
      ev.dataTransfer.dropEffect = 'copy';
    }
  }

  function handleDrop(ev: DragEvent): void {
    ev.preventDefault();
    const path = ev.dataTransfer?.getData('text/plain') || ev.dataTransfer?.getData(SHADER_PATH_DROP_TYPE);
    if (!path || typeof path !== 'string' || !path.trim()) return;
    const entry = entries.find((e) => (e.path ?? '') === path.trim());
    if (!entry) return;
    setCurrentEntry(entry);
    render.loadShader(entry);
    status(entry.path || path || entry.name || '');
  }

  for (const el of dropTargets) {
    el.addEventListener('dragover', allowDrop);
    el.addEventListener('drop', handleDrop);
    if (!el.title) el.title = 'Drag a shader from the list and drop here to open it';
  }
}

function initIndexResizer(): void {
  const res = document.getElementById('indexResizer');
  const panel = document.getElementById('indexPanel');
  const main = document.getElementById('mainGrid');
  if (!res || !panel || !main) return;
  const MIN_INDEX = 180;
  const MAX_INDEX = 480;
  let startX = 0;
  let startWidth = 0;
  res.addEventListener('mousedown', (e: MouseEvent) => {
    if (isCompactLayout()) return;
    e.preventDefault();
    startX = e.clientX;
    startWidth = panel.getBoundingClientRect().width;
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
    const onMove = (move: MouseEvent) => {
      const dx = move.clientX - startX;
      const newW = Math.max(MIN_INDEX, Math.min(MAX_INDEX, startWidth + dx));
      main.style.setProperty('--index-width', newW + 'px');
    };
    const onUp = () => {
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup', onUp);
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
      const w = panel.getBoundingClientRect().width;
      localStorage.setItem('macroverse-index-width', String(w));
    };
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
  });
  const storedW = localStorage.getItem('macroverse-index-width');
  if (storedW) {
    const w = Math.max(MIN_INDEX, Math.min(MAX_INDEX, parseInt(storedW, 10)));
    if (!isNaN(w)) main.style.setProperty('--index-width', w + 'px');
  }
}

function initIndexPanelCollapse(): void {
  const btn = document.getElementById('indexCollapseBtn');
  const panel = document.getElementById('indexPanel');
  const main = document.getElementById('mainGrid');
  if (!btn || !panel || !main) return;
  const stored = localStorage.getItem('macroverse-index-collapsed');
  if (stored === 'true') {
    panel.classList.add('collapsed');
    main.classList.add('index-collapsed');
  }
  btn.addEventListener('click', () => {
    const collapsed = !panel.classList.contains('collapsed');
    panel.classList.toggle('collapsed');
    main.classList.toggle('index-collapsed', collapsed);
    localStorage.setItem('macroverse-index-collapsed', String(collapsed));
    render.resizeCanvas();
  });
}

function initRightPanelCollapse(): void {
  const btn = document.getElementById('rightCollapseBtn');
  const panel = document.getElementById('rightColumn');
  const main = document.getElementById('mainGrid');
  if (!btn || !panel || !main) return;
  const stored = localStorage.getItem('macroverse-right-collapsed');
  if (stored === 'true') {
    panel.classList.add('collapsed');
    main.classList.add('right-collapsed');
  }
  btn.addEventListener('click', () => {
    const collapsed = !panel.classList.contains('collapsed');
    panel.classList.toggle('collapsed');
    main.classList.toggle('right-collapsed', collapsed);
    localStorage.setItem('macroverse-right-collapsed', String(collapsed));
    render.resizeCanvas();
  });
}

function initIndexToolsToggle(): void {
  const btn = document.getElementById('indexToolsToggle');
  const toolbar = document.querySelector('.index-toolbar') as HTMLElement | null;
  if (!btn || !toolbar) return;
  btn.addEventListener('click', () => {
    const expanded = toolbar.classList.toggle('expanded');
    btn.textContent = expanded
      ? 'Hide tools \u2191'
      : isCompactLayout()
        ? 'Filters & settings \u2193'
        : 'More tools \u2193';
  });
}

function initToolbarMore(): void {
  const btn = document.getElementById('toolbarMoreBtn');
  const extGroup = document.querySelector('.toolbar-group-ext') as HTMLElement | null;
  const codeToolbar = document.getElementById('codeToolbar');
  if (!btn) return;

  btn.addEventListener('click', () => {
    // On phone, expand the whole toolbar; on desktop, only the
    // External group is hidden by default.
    const isPhone = isLayoutPhone();
    if (isPhone && codeToolbar) {
      const expanded = codeToolbar.classList.toggle('expanded');
      btn.textContent = expanded ? 'x' : '...';
      // Mirror to ext group so toggling on tablet/desktop still
      // hides the externals when the user collapses.
      if (extGroup) extGroup.style.display = expanded ? 'flex' : 'none';
    } else if (extGroup) {
      const visible = extGroup.style.display !== 'none';
      extGroup.style.display = visible ? 'none' : 'flex';
      btn.textContent = visible ? '...' : 'x';
    }
  });

  // Phone default: ensure the toolbar starts collapsed regardless
  // of where Vite/HMR left it.
  if (isLayoutPhone() && codeToolbar) {
    codeToolbar.classList.remove('expanded');
  }
}

function initUpdateButton(): void {
  const btn = document.getElementById('sidebarUpdateBtn') as HTMLButtonElement | null;
  if (!btn) return;

  function setUpdateAvailable(commits: string[]): void {
    btn!.style.color = 'var(--amiga-copper)';
    btn!.title = `${commits.length} update(s) available. Click to apply.`;
    btn!.textContent = `Update (${commits.length})`;
  }

  function setChecking(): void {
    btn!.disabled = true;
    btn!.textContent = 'Checking...';
    btn!.style.color = 'var(--crt-dim)';
  }

  function setApplying(): void {
    btn!.disabled = true;
    btn!.textContent = 'Updating...';
    btn!.style.color = 'var(--amiga-copper)';
  }

  function resetBtn(): void {
    btn!.disabled = false;
    btn!.textContent = 'Update';
    btn!.style.color = 'var(--crt-dim)';
    btn!.title = 'Check GitHub for updates, rebuild and restart if available';
  }

  let pendingCommits: string[] = [];
  let pendingLocalHead = '';
  let pendingRemoteHead = '';

  function showUpdateModal(commits: string[], localHead: string, remoteHead: string): void {
    const existing = document.getElementById('updateModal');
    if (existing) existing.remove();

    const overlay = document.createElement('div');
    overlay.id = 'updateModal';
    overlay.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,0.6);z-index:10000;display:flex;align-items:center;justify-content:center;';

    const box = document.createElement('div');
    box.style.cssText = 'background:var(--amiga-panel);border:2px solid var(--amiga-copper);padding:20px;max-width:520px;width:90%;font-size:12px;color:var(--crt-fg);';

    const titleEl = document.createElement('div');
    titleEl.style.cssText = 'color:var(--amiga-copper);font-weight:bold;font-size:13px;margin-bottom:12px;text-transform:uppercase;letter-spacing:0.1em;';
    titleEl.textContent = `${commits.length} update(s) available`;
    box.appendChild(titleEl);

    if (localHead || remoteHead) {
      const revInfo = document.createElement('div');
      revInfo.style.cssText = 'color:var(--crt-dim);font-size:10px;margin-bottom:10px;';
      revInfo.textContent = `local: ${localHead || 'unknown'}  →  remote: ${remoteHead || 'unknown'}`;
      box.appendChild(revInfo);
    }

    const list = document.createElement('div');
    list.style.cssText = 'background:var(--amiga-bg);border:1px solid var(--bevel-dark);padding:8px;margin-bottom:14px;max-height:200px;overflow-y:auto;font-size:11px;line-height:1.6;white-space:pre;';
    list.textContent = commits.join('\n');
    box.appendChild(list);

    const note = document.createElement('div');
    note.style.cssText = 'color:var(--crt-dim);font-size:10px;margin-bottom:14px;';
    note.textContent = 'Applying will git pull, rebuild the binary, kill this instance, and relaunch. This takes ~30 seconds.';
    box.appendChild(note);

    const btnRow = document.createElement('div');
    btnRow.style.cssText = 'display:flex;gap:10px;justify-content:flex-end;';

    const cancelBtn = document.createElement('button');
    cancelBtn.className = 'wire-btn';
    cancelBtn.style.cssText = 'padding:5px 14px;font-size:11px;';
    cancelBtn.textContent = 'Cancel';
    cancelBtn.onclick = () => overlay.remove();

    const applyBtn = document.createElement('button');
    applyBtn.className = 'wire-btn';
    applyBtn.style.cssText = 'padding:5px 14px;font-size:11px;background:var(--amiga-surface);color:var(--amiga-copper);border-color:var(--amiga-copper);';
    applyBtn.textContent = 'Apply & Restart';
    applyBtn.onclick = async () => {
      overlay.remove();
      setApplying();
      try {
        const result = await postUpdateApply();
        if (result.success) {
          status('Updating: ' + result.message + ' App will restart shortly.');
          btn!.textContent = 'Restarting...';
        } else {
          status('Update failed: ' + (result.error || result.message), true);
          resetBtn();
        }
      } catch (e) {
        status('Update error: ' + String(e), true);
        resetBtn();
      }
    };

    btnRow.appendChild(cancelBtn);
    btnRow.appendChild(applyBtn);
    box.appendChild(btnRow);
    overlay.appendChild(box);
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
    document.body.appendChild(overlay);
  }

  btn.addEventListener('click', async () => {
    if ((btn as HTMLButtonElement).disabled) return;

    if (pendingCommits.length > 0) {
      showUpdateModal(pendingCommits, pendingLocalHead, pendingRemoteHead);
      return;
    }

    setChecking();
    try {
      const result = await fetchUpdateCheck();
      if (result.error && !result.hasUpdates) {
        status('Update check: ' + result.error);
        resetBtn();
        return;
      }
      if (!result.hasUpdates) {
        status('Already up to date.');
        resetBtn();
        return;
      }
      pendingCommits = result.commits;
      pendingLocalHead = result.localHead || '';
      pendingRemoteHead = result.remoteHead || '';
      setUpdateAvailable(result.commits);
      showUpdateModal(result.commits, pendingLocalHead, pendingRemoteHead);
    } catch (e) {
      status('Update check failed: ' + String(e));
      resetBtn();
    }
  });

  // Auto-check on startup after a short delay (silent, non-blocking)
  setTimeout(async () => {
    try {
      const result = await fetchUpdateCheck();
      if (result.hasUpdates && result.commits.length > 0) {
        pendingCommits = result.commits;
        pendingLocalHead = result.localHead || '';
        pendingRemoteHead = result.remoteHead || '';
        setUpdateAvailable(result.commits);
      }
    } catch (_) {
      // ignore - may not be a git repo or offline
    }
  }, 5000);
}
