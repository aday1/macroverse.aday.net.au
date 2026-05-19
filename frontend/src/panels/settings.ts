import { el, status, showPathsInfo } from '../dom.js';
import { postSources, postSettings, postIndexBackup, postIndexClear, postGitHardResetShaders, fetchGitRepoStatus, postGitInit, fetchLLMStatus, postLLMConfig, fetchLLMModels, fetchGithubStatus } from '../api.js';
import type { LLMProviderConfig } from '../api.js';
import { showPathPicker } from '../pathPicker.js';
import { appSettings, setAppSettings, entries } from '../state.js';
import { loadSequence } from '../init/loadSequence.js';
import { buildList } from '../list.js';
import { resizeCanvas } from '../render.js';
import { DEFAULT_THEME, THEME_PRESETS, hexToHsv, hsvToHex, applyTheme, mergeTheme, getLastAppliedTheme } from '../themeUtils.js';
import type { Settings } from '../types.js';
import type { ThemeColors } from '../themeUtils.js';
import { midiEngine } from '../engines/midi.js';
import { VJ_ACTION_IDS } from '../engines/vjController.js';
import { getVjSessionId, setVjSessionId, vjSessionQuery } from '../vjSession.js';
import { reconnectVjSession } from '../vjWs.js';
import { oscEngine } from '../engines/osc.js';

let panelEl: HTMLElement | null = null;
let overlayEl: HTMLElement | null = null;

function escapeHtml(s: string): string {
  const div = document.createElement('div');
  div.textContent = s;
  return div.innerHTML;
}

function getPanelContainer(): HTMLElement {
  if (panelEl) return panelEl;
  panelEl = document.createElement('div');
  panelEl.id = 'settingsPanel';
  panelEl.className = 'settings-panel';
  panelEl.style.cssText = `
    position: fixed; top: 0; right: 0; width: 420px; max-width: 95vw; height: 100vh;
    background: var(--amiga-panel); border-left: 2px solid var(--bevel-dark);
    box-shadow: -4px 0 20px rgba(0,0,0,0.5); z-index: 9998;
    display: none; flex-direction: column; overflow: hidden;
  `;
  document.body.appendChild(panelEl);
  return panelEl;
}

function getOverlay(): HTMLElement {
  if (overlayEl) return overlayEl;
  overlayEl = document.createElement('div');
  overlayEl.id = 'settingsOverlay';
  overlayEl.className = 'settings-overlay';
  overlayEl.style.cssText = `
    position: fixed; inset: 0; background: rgba(0,0,0,0.4); z-index: 9997;
    display: none; cursor: pointer;
  `;
  overlayEl.onclick = () => closePanel();
  document.body.appendChild(overlayEl);
  return overlayEl;
}

function closePanel(): void {
  const p = getPanelContainer();
  const o = getOverlay();
  p.style.display = 'none';
  o.style.display = 'none';
}

function openPanel(): void {
  const p = getPanelContainer();
  const o = getOverlay();
  p.style.display = 'flex';
  o.style.display = 'block';
  renderPanel();
}

async function renderPanel(): Promise<void> {
  const panel = getPanelContainer();
  panel.innerHTML = '';

  const header = document.createElement('div');
  header.style.cssText = 'padding:12px 16px; background:var(--amiga-surface); border-bottom:1px solid var(--bevel-dark); display:flex; align-items:center; justify-content:space-between;';
  header.innerHTML = '<span style="color:var(--amiga-copper); font-weight:bold;">Settings</span><button id="settingsClose" title="TLDR: Close settings" style="background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); padding:4px 12px; cursor:pointer;">Close</button>';
  const closeBtn = header.querySelector('#settingsClose');
  if (closeBtn) closeBtn.addEventListener('click', closePanel);
  panel.appendChild(header);

  const body = document.createElement('div');
  body.style.cssText = 'flex:1; overflow:auto; padding:16px;';
  body.innerHTML = `
    <div class="settings-section" style="margin-bottom:20px;">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">Display effects</div>
      <div style="display:flex; flex-direction:column; gap:8px;">
        <label style="display:flex; align-items:center; gap:8px; cursor:pointer; font-size:12px;">
          <input type="checkbox" id="scanlineToggle" autocomplete="off" />
          <span>Scanlines</span>
          <span style="font-size:10px; color:var(--crt-dim); margin-left:auto;">Horizontal scan lines on preview</span>
        </label>
        <label style="display:flex; align-items:center; gap:8px; cursor:pointer; font-size:12px;">
          <input type="checkbox" id="vignetteToggle" autocomplete="off" />
          <span>Vignette</span>
          <span style="font-size:10px; color:var(--crt-dim); margin-left:auto;">Darkened corners on preview</span>
        </label>
        <label style="display:flex; align-items:center; gap:8px; cursor:pointer; font-size:12px;">
          <input type="checkbox" id="crtCodeToggle" autocomplete="off" />
          <span>Code view (terminal CRT)</span>
          <span style="font-size:10px; color:var(--crt-dim); margin-left:auto;">Green phosphor editor while editing</span>
        </label>
      </div>
      <div style="font-size:10px; color:var(--crt-dim); margin-top:8px;">Toggle retro-CRT visual effects. Settings are remembered between sessions.</div>
    </div>
    <div class="settings-section" style="margin-bottom:20px;">
      <button id="settingsPathsInfo" title="TLDR: Show paths, index, and shader count" style="margin-bottom:12px; background:var(--amiga-surface); color:var(--amiga-accent); border:1px solid var(--bevel-dark); padding:8px 12px; cursor:pointer;">Show paths & shader count</button>
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">Source Paths</div>
      <div id="sourcePathsList" style="display:flex; flex-direction:column; gap:8px;"></div>
      <button id="settingsAddPath" title="TLDR: Browse and add source path" style="margin-top:8px; background:var(--amiga-surface); color:var(--amiga-accent); border:1px solid var(--bevel-dark); padding:6px 12px; cursor:pointer;">Add path...</button>
    </div>
    <div class="settings-section" style="margin-bottom:20px;">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">Index (SQLite, internal)</div>
      <input type="text" id="settingsIndexPath" readonly style="width:100%; padding:8px; background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); font-family:inherit; cursor:default;" />
      <div style="font-size:10px; color:var(--crt-dim); margin-top:4px;">Shaders stored in macroverse.db next to the app. No JSON config needed.</div>
    </div>
    <div class="settings-section" style="margin-bottom:20px;" id="settingsDebugSection">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">Debug: Graveyard (broken shaders log)</div>
      <input type="text" id="settingsGraveyardPath" readonly style="width:100%; padding:8px; background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); font-family:inherit; cursor:default; font-size:10px;" />
      <div style="font-size:10px; color:var(--crt-dim); margin-top:4px;">JSON log of shaders marked unrecoverable (path, compile error, tried summary). Same directory as index, file: unrecoverable-shaders.json. Give this path to an agent or script to batch-fix later.</div>
    </div>
    <div class="settings-section" style="margin-bottom:20px;">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">Debug: Roliblock</div>
      <a href="/roliblock-debug.html" target="_blank" rel="noopener" style="font-size:11px; color:var(--amiga-accent); display:block; margin-bottom:4px;">Roliblock DEBUG page</a>
      <div style="font-size:10px; color:var(--crt-dim);">Standalone test for MIDI and LED. Request MIDI, test touch pad and LEDs. Use Chrome or Edge (sysex required for LEDs).</div>
    </div>
    <div class="settings-section" style="margin-bottom:20px;">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">Open with Wire Path</div>
      <input type="text" id="settingsWirePath" placeholder="C:\\Program Files\\Resolume Wire\\Wire.exe" style="width:100%; padding:8px; background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); font-family:inherit;" />
      <div style="font-size:10px; color:var(--crt-dim); margin-top:4px;">Path to Wire.exe. Leave empty to use system file association.</div>
    </div>
    <div class="settings-section" style="margin-bottom:20px;">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">Cursor API Key</div>
      <input type="password" id="settingsCursorApiKey" placeholder="key_xxx or leave blank if agent login done" style="width:100%; padding:8px; background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); font-family:inherit;" />
      <div style="font-size:10px; color:var(--crt-dim); margin-top:4px;">Authenticate: run <code style="background:var(--amiga-bg); padding:0 4px;">agent login</code> in a terminal (opens browser), or paste a key from Cursor IDE Settings. Saved to settings. Optional: compile error overlay can store a browser-only key in localStorage (overrides this when set).</div>
      <div style="font-size:10px; color:var(--crt-dim); margin-top:6px;">If you get rate limited or "verify human" errors: wait a few minutes between agent calls, or use Open in Cursor and run Agent from Cursor IDE.</div>
    </div>
    <div class="settings-section" style="margin-bottom:20px;" id="settingsGithubSection">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">GitHub CLI</div>
      <div id="githubStatus" style="font-size:11px; color:var(--crt-fg); margin-bottom:8px;">Checking...</div>
      <div style="font-size:10px; color:var(--crt-dim); margin-bottom:8px;">Run <code style="background:var(--amiga-bg); padding:0 4px;">gh auth login</code> in a terminal to authenticate. No token stored for CLI.</div>
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">GitHub / Copilot token (for Fix/Vibe)</div>
      <input type="password" id="settingsGithubToken" placeholder="Optional: token for GitHub API / Copilot" style="width:100%; padding:8px; background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); font-family:inherit;" />
      <div style="font-size:10px; color:var(--crt-dim); margin-top:4px;">Optional. Use a GitHub or Copilot token for Fix/Vibe. Optional: compile error overlay can store a browser-only token in localStorage (overrides this when set).</div>
    </div>
    <div class="settings-section" style="margin-bottom:20px;">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">LLM Provider Chain</div>
      <div style="font-size:10px; color:var(--crt-dim); margin-bottom:10px;">Fix/generate priority: providers tried in order. Local = free regex. Ollama = free local LLM. Cursor = cloud tokens. Use a model you have (e.g. llama3.2); app will fall back to an installed model if needed.</div>
      <div id="llmProviderList" style="display:flex;flex-direction:column;gap:8px;"></div>
      <div style="display:flex;gap:8px;margin-top:8px;">
        <button type="button" id="llmTestBtn" style="font-size:10px;padding:4px 10px;background:var(--amiga-surface);color:var(--amiga-accent);border:1px solid var(--bevel-dark);cursor:pointer;">Test Ollama</button>
        <span id="llmStatusText" style="font-size:10px;color:var(--crt-dim);line-height:24px;"></span>
      </div>
    </div>
    <div class="settings-section" style="margin-bottom:20px;">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">Default View</div>
      <select id="settingsDefaultView" style="width:100%; padding:8px; background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); font-family:inherit;">
        <option value="split-v">Split V (stacked)</option>
        <option value="split-h">Split H (side by side)</option>
        <option value="preview">Preview only</option>
        <option value="code">Code only</option>
      </select>
      <div style="font-size:10px; color:var(--crt-dim); margin-top:4px;">Default layout when opening the app</div>
    </div>
    <div class="settings-section" style="margin-bottom:20px;">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">Param defaults</div>
      <div style="display:flex; align-items:center; gap:12px; margin-bottom:6px;">
        <label style="font-size:10px; min-width:120px;">Default param value</label>
        <input type="number" id="settingsDefaultParamValue" step="0.1" style="width:80px; padding:6px; background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); font-family:inherit;" />
      </div>
      <div style="display:flex; align-items:center; gap:12px;">
        <label style="font-size:10px; min-width:120px;">Default time scale</label>
        <input type="number" id="settingsDefaultTimeScale" min="0" max="4" step="0.1" style="width:80px; padding:6px; background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); font-family:inherit;" />
      </div>
      <div style="font-size:10px; color:var(--crt-dim); margin-top:4px;">New float sliders start at default param value (e.g. 0). Time speed slider starts at default time scale (0 = paused).</div>
    </div>
    <div class="settings-section" style="margin-bottom:20px;">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">Preview Resolution</div>
      <select id="settingsPreviewResolution" style="width:100%; padding:8px; background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); font-family:inherit;">
        <option value="auto">Auto (fit panel)</option>
        <option value="640x360">640 x 360</option>
        <option value="854x480">854 x 480</option>
        <option value="1280x720">1280 x 720</option>
        <option value="1920x1080">1920 x 1080</option>
      </select>
    </div>
    <div class="settings-section" style="margin-bottom:20px;">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">Shader Transition</div>
      <select id="settingsTransition" style="width:100%; padding:8px; background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); font-family:inherit; margin-bottom:6px;">
        <option value="none">None (instant switch)</option>
        <option value="crossfade">Crossfade</option>
        <option value="wipe-left">Wipe Left</option>
        <option value="wipe-right">Wipe Right</option>
        <option value="wipe-down">Wipe Down</option>
        <option value="dissolve">Dissolve</option>
        <option value="zoom-in">Zoom In</option>
        <option value="zoom-out">Zoom Out</option>
        <option value="glitch">Glitch</option>
        <option value="slide-left">Slide Left</option>
      </select>
      <div style="display:flex; align-items:center; gap:8px;">
        <span style="font-size:10px; color:var(--crt-dim); white-space:nowrap;">Duration (ms)</span>
        <input type="range" id="settingsTransitionDuration" min="100" max="2000" step="50" style="flex:1; accent-color:var(--amiga-accent);" />
        <span id="settingsTransitionDurationVal" style="font-size:11px; color:var(--crt-dim); min-width:35px;"></span>
      </div>
      <div style="font-size:10px; color:var(--crt-dim); margin-top:4px;">Smooth transition when switching between shaders</div>
    </div>
    <div class="settings-section" style="margin-bottom:20px;">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">Preview Render Quality</div>
      <select id="settingsPreviewQuality" style="width:100%; padding:8px; background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); font-family:inherit;">
        <option value="0.5">Low (0.5x - fastest)</option>
        <option value="0.75">Medium (0.75x)</option>
        <option value="1">Full (1x - best quality)</option>
      </select>
      <div style="font-size:10px; color:var(--crt-dim); margin-top:4px;">Lower = smoother playback on weak GPUs</div>
    </div>
    <div class="settings-section" style="margin-bottom:20px;">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">Thumbnail Quality</div>
      <input type="range" id="settingsThumbnailQuality" min="0.2" max="0.9" step="0.1" style="width:100%; accent-color:var(--amiga-accent);" />
      <span id="settingsThumbnailQualityVal" style="font-size:11px; color:var(--crt-dim);"></span>
    </div>
    <div class="settings-section" style="margin-bottom:20px;">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">Thumbnail Max Size (px)</div>
      <select id="settingsThumbnailMaxSize" style="width:100%; padding:8px; background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); font-family:inherit;">
        <option value="80">80 (small, fast)</option>
        <option value="120">120</option>
        <option value="160">160</option>
        <option value="240">240 (large)</option>
        <option value="0">Full (no scaling)</option>
      </select>
    </div>
    <div class="settings-section" style="margin-bottom:20px;">
      <label style="display:flex; align-items:center; gap:8px; cursor:pointer;">
        <input type="checkbox" id="settingsShowThumbnails" />
        <span>Show thumbnails in shader list</span>
      </label>
      <label style="display:flex; align-items:center; gap:8px; cursor:pointer; margin-top:8px;">
        <input type="checkbox" id="settingsThumbnailLoadingPaused" />
        <span>Pause thumbnail loading on startup (click Load thumbnails when ready)</span>
      </label>
      <div style="font-size:10px; color:var(--crt-dim); margin-top:4px;">Reduces freeze when opening large lists. Use Load thumbnails in the index panel to load when idle.</div>
      <label style="display:flex; align-items:center; gap:8px; cursor:pointer; margin-top:12px;">
        <input type="checkbox" id="settingsSkipSplash" />
        <span>Skip pipeline launcher splash screen</span>
      </label>
    </div>
    <div class="settings-section" style="margin-bottom:20px;" id="themeSection">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">Themes</div>
      <div style="font-size:10px; color:var(--crt-dim); margin-bottom:10px;">Pick a preset or customize colors below. Click Save at the bottom to keep your choice.</div>
      <div id="themePresetButtons" style="display:flex; flex-wrap:wrap; gap:6px; margin-bottom:12px;"></div>
      <div style="display:flex; gap:8px; align-items:center; margin-bottom:12px;">
        <button type="button" id="themeResetBtn" style="font-size:10px; padding:4px 10px; background:var(--amiga-surface); color:var(--amiga-copper); border:1px solid var(--bevel-dark); cursor:pointer;">Reset to Synthwave</button>
        <button type="button" id="themeCustomizeToggle" style="font-size:10px; padding:4px 10px; background:var(--amiga-surface); color:var(--crt-fg); border:1px solid var(--bevel-dark); cursor:pointer;">Customize colors</button>
      </div>
      <div id="themeCustomizeWrap" style="display:none;">
        <div style="color:var(--crt-dim); font-size:10px; margin-bottom:6px;">App UI</div>
        <div id="themeAppColors" style="margin-bottom:16px;"></div>
        <div style="color:var(--crt-dim); font-size:10px; margin-bottom:6px;">Editor (code view)</div>
        <div id="themeEditorColors" style="margin-bottom:8px;"></div>
      </div>
    </div>
    <div class="settings-section" style="margin-bottom:20px;" id="settingsVjSessionSection">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">VJ show session</div>
      <div style="font-size:10px; color:var(--crt-dim); margin-bottom:8px;">Same session ID on tablets, cloud UI, and Pi HDMI output. Default: default</div>
      <div style="display:flex; gap:8px; flex-wrap:wrap; align-items:center; margin-bottom:8px;">
        <input id="settingsVjSessionId" type="text" placeholder="default" style="flex:1; min-width:140px; padding:6px 8px; font-size:10px; background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); font-family:inherit;" />
        <button type="button" id="settingsVjSessionApply" style="font-size:10px; padding:6px 10px; background:var(--amiga-surface); color:var(--amiga-copper); border:1px solid var(--bevel-dark); cursor:pointer;">Apply session</button>
      </div>
      <div id="settingsVjSessionHint" style="font-size:9px; color:var(--crt-dim);"></div>
    </div>
    <div class="settings-section" style="margin-bottom:20px;" id="settingsVjControllerSection">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">MIDI / OSC - VJ controller</div>
      <div style="font-size:10px; color:var(--crt-dim); margin-bottom:8px;">Map hardware (e.g. APC40 MK2) to crossfader, deck params, clip launch, and page. OSC uses the same actions.</div>
      <div style="display:flex; align-items:center; gap:8px; margin-bottom:8px;">
        <label style="font-size:10px;">Template</label>
        <select id="settingsVjMidiTemplate" style="padding:6px 8px; font-size:10px; background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); font-family:inherit;">
          <option value="none">None</option>
          <option value="apc40_mk2">APC40 MK2 (default)</option>
          <option value="custom">Custom (keep current)</option>
        </select>
      </div>
      <div style="font-size:9px; color:var(--crt-dim); margin-bottom:6px;">Clip launch: Note On ch 0, note 0-39 (8 tracks x 5 cols). Track 1=A page, Track 2=B page, etc. Bank: CC 104-107 = A up/down, B left/right.</div>
      <div id="settingsVjMidiMapList" style="display:flex; flex-direction:column; gap:4px; max-height:280px; overflow-y:auto;"></div>
    </div>
    <div class="settings-section" style="margin-bottom:20px;">
      <button id="settingsSave" title="TLDR: Save settings" style="background:var(--amiga-accent); color:#fff; border:none; padding:8px 16px; cursor:pointer;">Save</button>
    </div>
    <div class="settings-section" style="margin-top:24px; padding-top:16px; border-top:1px solid var(--bevel-dark);">
      <div style="color:var(--amiga-copper); font-size:11px; text-transform:uppercase; margin-bottom:8px;">Danger Zone</div>
      <div style="margin-bottom:10px;">
        <label style="font-size:10px; display:block; margin-bottom:4px;">Hard Reset target path</label>
        <input id="settingsHardResetPath" type="text" placeholder="shaders/custom/" style="width:100%; box-sizing:border-box; padding:6px 8px; font-size:10px; background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); font-family:inherit;" />
        <div style="font-size:9px; color:var(--crt-dim); margin-top:3px;">Absolute path or relative to the app. Hard Reset zips this folder then restores it from git. Default: shaders/custom/</div>
      </div>
      <div style="display:flex; gap:8px; flex-wrap:wrap; align-items:center;">
        <button id="settingsNuke" title="TLDR: Backup index, clear it, and rescan from disk" style="background:#8a2222; color:#fff; border:none; padding:8px 16px; cursor:pointer;">NUKE (backup + clear index)</button>
        <button id="settingsHardResetShaders" title="Zip the Hard Reset target folder and restore it from git. Configure the target path above." style="background:#6a1a4a; color:#fff; border:none; padding:8px 16px; cursor:pointer;">Hard Reset (git)</button>
      </div>
    </div>
  `;
  panel.appendChild(body);

  const theme = mergeTheme(appSettings.themeColors);
  const themeForRows = getLastAppliedTheme();
  const themeKeys: Array<{ key: keyof ThemeColors; label: string }> = [
    { key: 'amigaBg', label: 'App bg' },
    { key: 'amigaSurface', label: 'App surface' },
    { key: 'amigaPanel', label: 'App panel' },
    { key: 'amigaText', label: 'App text' },
    { key: 'amigaAccent', label: 'App accent' },
    { key: 'amigaCopper', label: 'App copper' },
  ];
  const editorKeys: Array<{ key: keyof ThemeColors; label: string }> = [
    { key: 'editorBg', label: 'Editor bg' },
    { key: 'editorFg', label: 'Editor text' },
    { key: 'editorKeyword', label: 'Keywords' },
    { key: 'editorString', label: 'Strings' },
    { key: 'editorComment', label: 'Comments' },
    { key: 'editorNumber', label: 'Numbers' },
    { key: 'editorFunction', label: 'Functions' },
  ];

  function makeColorRow(key: keyof ThemeColors, label: string, parent: HTMLElement): void {
    let hex = themeForRows[key] || theme[key] || '#000000';
    let hsv = hexToHsv(hex);
    const row = document.createElement('div');
    row.className = 'theme-color-row';
    row.style.cssText = 'margin-bottom:10px; padding:8px; background:var(--amiga-bg); border:1px solid var(--bevel-dark);';
    row.innerHTML = `
      <div style="display:flex; align-items:center; gap:8px; margin-bottom:4px;">
        <span style="font-size:10px; min-width:70px;">${escapeHtml(label)}</span>
        <span class="theme-hex-preview" style="width:40px; height:18px; border:1px solid var(--bevel-dark); background:${hex};"></span>
        <span class="theme-hex-val" style="font-size:10px; font-family:monospace;">${hex}</span>
      </div>
      <div style="display:flex; gap:6px; align-items:center; font-size:9px;">
        <span>H</span><input type="range" class="theme-h" min="0" max="360" value="${hsv.h}" style="flex:1;" />
        <span>S</span><input type="range" class="theme-s" min="0" max="100" value="${hsv.s}" style="flex:1;" />
        <span>V</span><input type="range" class="theme-v" min="0" max="100" value="${hsv.v}" style="flex:1;" />
      </div>
    `;
    const update = () => {
      const h = parseInt((row.querySelector('.theme-h') as HTMLInputElement).value, 10);
      const s = parseInt((row.querySelector('.theme-s') as HTMLInputElement).value, 10);
      const v = parseInt((row.querySelector('.theme-v') as HTMLInputElement).value, 10);
      hex = hsvToHex(h, s, v);
      (row.querySelector('.theme-hex-preview') as HTMLElement).style.background = hex;
      (row.querySelector('.theme-hex-val') as HTMLElement).textContent = hex;
      const draft = (window as unknown as { __themeDraft?: Record<string, string> }).__themeDraft || {};
      draft[key] = hex;
      (window as unknown as { __themeDraft: Record<string, string> }).__themeDraft = draft;
      applyTheme({ ...mergeTheme(appSettings.themeColors), ...draft } as Partial<ThemeColors>);
    };
    row.querySelector('.theme-h')?.addEventListener('input', update);
    row.querySelector('.theme-s')?.addEventListener('input', update);
    row.querySelector('.theme-v')?.addEventListener('input', update);
    parent.appendChild(row);
  }

  const appColorsEl = panel.querySelector('#themeAppColors') as HTMLElement;
  const editorColorsEl = panel.querySelector('#themeEditorColors') as HTMLElement;
  if (appColorsEl) themeKeys.forEach(({ key, label }) => makeColorRow(key, label, appColorsEl));
  if (editorColorsEl) editorKeys.forEach(({ key, label }) => makeColorRow(key, label, editorColorsEl));

  const themeResetBtn = panel.querySelector('#themeResetBtn');
  themeResetBtn?.addEventListener('click', () => {
    (window as unknown as { __themeDraft?: Record<string, string> }).__themeDraft = {};
    applyTheme(DEFAULT_THEME);
    renderPanel();
  });

  const themePresetButtonsEl = panel.querySelector('#themePresetButtons') as HTMLElement | null;
  if (themePresetButtonsEl) {
    themePresetButtonsEl.innerHTML = '';
    const currentTheme = getLastAppliedTheme();
    const activePresetId = THEME_PRESETS.find((p) =>
      p.theme.amigaBg === currentTheme.amigaBg &&
      p.theme.amigaAccent === currentTheme.amigaAccent &&
      p.theme.editorFg === currentTheme.editorFg
    )?.id ?? null;
    THEME_PRESETS.forEach((preset) => {
      const btn = document.createElement('button');
      btn.type = 'button';
      btn.textContent = preset.name;
      btn.title = 'Apply ' + preset.name + ' theme';
      btn.style.cssText = 'font-size:10px; padding:6px 12px; background:var(--amiga-surface); color:var(--crt-fg); border:1px solid var(--bevel-dark); cursor:pointer; font-family:inherit;';
      const accentHex = preset.theme.amigaAccent || preset.theme.amigaCopper;
      btn.style.borderLeftColor = accentHex;
      btn.style.borderLeftWidth = '3px';
      if (preset.id === activePresetId) {
        btn.style.background = 'var(--amiga-accent)';
        btn.style.color = 'var(--amiga-bg)';
        btn.style.borderColor = 'var(--amiga-accent)';
      }
      btn.addEventListener('click', () => {
        (window as unknown as { __themeDraft: Record<string, string> }).__themeDraft = { ...preset.theme } as unknown as Record<string, string>;
        applyTheme(preset.theme);
        renderPanel();
      });
      themePresetButtonsEl.appendChild(btn);
    });
  }

  const themeCustomizeToggle = panel.querySelector('#themeCustomizeToggle') as HTMLButtonElement | null;
  const themeCustomizeWrap = panel.querySelector('#themeCustomizeWrap') as HTMLElement | null;
  if (themeCustomizeToggle && themeCustomizeWrap) {
    themeCustomizeToggle.addEventListener('click', () => {
      const isHidden = themeCustomizeWrap.style.display === 'none';
      themeCustomizeWrap.style.display = isHidden ? 'block' : 'none';
      themeCustomizeToggle.textContent = isHidden ? 'Hide customization' : 'Customize colors';
    });
  }

  const vjSessionInput = panel.querySelector('#settingsVjSessionId') as HTMLInputElement | null;
  const vjSessionApply = panel.querySelector('#settingsVjSessionApply') as HTMLButtonElement | null;
  const vjSessionHint = panel.querySelector('#settingsVjSessionHint') as HTMLElement | null;
  if (vjSessionInput) {
    vjSessionInput.value = getVjSessionId();
    const refreshHint = () => {
      if (!vjSessionHint) return;
      const sid = getVjSessionId();
      const base = typeof window !== 'undefined' ? window.location.origin : '';
      vjSessionHint.textContent = `HDMI / Pi preview: ${base}/vj-output.html?remote=1&${vjSessionQuery()}`;
    };
    refreshHint();
    vjSessionApply?.addEventListener('click', () => {
      setVjSessionId(vjSessionInput.value);
      vjSessionInput.value = getVjSessionId();
      refreshHint();
      reconnectVjSession();
    });
  }

  const vjTemplateSelect = panel.querySelector('#settingsVjMidiTemplate') as HTMLSelectElement | null;
  const vjMapListEl = panel.querySelector('#settingsVjMidiMapList') as HTMLElement | null;
  midiEngine.loadVjMappings();
  if (vjTemplateSelect) {
    vjTemplateSelect.value = midiEngine.vjTemplate;
    vjTemplateSelect.addEventListener('change', () => {
      const v = vjTemplateSelect.value as 'none' | 'apc40_mk2' | 'custom';
      midiEngine.setVjTemplate(v);
      if (vjMapListEl) renderVjMidiMapList(vjMapListEl);
    });
  }
  function renderVjMidiMapList(container: HTMLElement): void {
    container.innerHTML = '';
    const actionLabels: Record<string, string> = {
      'vj/crossfader': 'Crossfader',
      'vj/deckA/pageUp': 'Deck A page up', 'vj/deckA/pageDown': 'Deck A page down',
      'vj/deckB/pageLeft': 'Deck B page left', 'vj/deckB/pageRight': 'Deck B page right'
    };
    const clipRow = document.createElement('div');
    clipRow.style.cssText = 'display:flex;align-items:center;gap:8px;padding:4px 6px;font-size:9px;background:var(--amiga-bg);border:1px solid var(--bevel-dark);';
    clipRow.innerHTML = '<span style="min-width:140px;">Clip launch (8x5)</span><span style="color:var(--crt-dim);">Note On ch 0, note 0-39</span>';
    container.appendChild(clipRow);
    for (const actionId of VJ_ACTION_IDS) {
      if (actionId === 'vj/loadClipA' || actionId === 'vj/loadClipB') continue;
      const label = actionLabels[actionId] || actionId.replace('vj/', '');
      const m = midiEngine.getVjActionMapping(actionId);
      const row = document.createElement('div');
      row.style.cssText = 'display:flex;align-items:center;gap:8px;padding:4px 6px;font-size:9px;background:var(--amiga-bg);border:1px solid var(--bevel-dark);';
      const lab = document.createElement('span');
      lab.style.minWidth = '140px';
      lab.textContent = label;
      row.appendChild(lab);
      const midiSpan = document.createElement('span');
      midiSpan.style.cssText = 'min-width:64px;color:var(--crt-dim);';
      midiSpan.textContent = m ? 'Ch' + (m.ch + 1) + ' CC' + m.cc : '-';
      row.appendChild(midiSpan);
      const learnBtn = document.createElement('button');
      learnBtn.type = 'button';
      learnBtn.textContent = midiEngine.vjLearning === actionId ? '...' : 'Learn';
      learnBtn.style.cssText = 'font-size:9px;padding:2px 6px;background:var(--amiga-surface);color:var(--amiga-copper);border:1px solid var(--bevel-dark);cursor:pointer;';
      learnBtn.onclick = () => {
        if (midiEngine.vjLearning === actionId) {
          midiEngine.cancelVjLearn();
          renderVjMidiMapList(container);
        } else {
          midiEngine.learnVjAction(actionId, () => renderVjMidiMapList(container));
          learnBtn.textContent = '...';
        }
      };
      row.appendChild(learnBtn);
      const oscInput = document.createElement('input');
      oscInput.type = 'text';
      const defaultOsc = '/' + actionId.replace(/\//g, '/');
      oscInput.placeholder = defaultOsc;
      oscInput.value = Object.keys(oscEngine.vjOscAddressMap).find((k) => oscEngine.vjOscAddressMap[k] === actionId) || '';
      oscInput.style.cssText = 'flex:1;min-width:80px;padding:2px 4px;font-size:9px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);font-family:inherit;';
      oscInput.onchange = () => {
        const v = oscInput.value.trim();
        if (v) {
          oscEngine.setVjOscAddress(v, actionId);
          oscEngine.loadVjOscAddresses();
        }
      };
      row.appendChild(oscInput);
      container.appendChild(row);
    }
  }
  if (vjMapListEl) {
    oscEngine.loadVjOscAddresses();
    renderVjMidiMapList(vjMapListEl);
  }

  const pathsList = panel.querySelector('#sourcePathsList') as HTMLElement;
  const indexPathInput = panel.querySelector('#settingsIndexPath') as HTMLInputElement;
  const wirePathInput = panel.querySelector('#settingsWirePath') as HTMLInputElement | null;
  const cursorApiKeyInput = panel.querySelector('#settingsCursorApiKey') as HTMLInputElement;
  const showThumbnailsCb = panel.querySelector('#settingsShowThumbnails') as HTMLInputElement | null;
  const addBtn = panel.querySelector('#settingsAddPath');
  const saveBtn = panel.querySelector('#settingsSave');
  const nukeBtn = panel.querySelector('#settingsNuke');
  const hardResetShadersBtn = panel.querySelector('#settingsHardResetShaders');
  const hardResetPathInput = panel.querySelector('#settingsHardResetPath') as HTMLInputElement | null;

  if (showThumbnailsCb) showThumbnailsCb.checked = !!appSettings.showThumbnails;
  const skipSplashCb = panel.querySelector('#settingsSkipSplash') as HTMLInputElement | null;
  if (skipSplashCb) skipSplashCb.checked = !!appSettings.skipSplash;
  const transitionSelect = panel.querySelector('#settingsTransition') as HTMLSelectElement | null;
  const transitionDurationSlider = panel.querySelector('#settingsTransitionDuration') as HTMLInputElement | null;
  const transitionDurationVal = panel.querySelector('#settingsTransitionDurationVal') as HTMLElement | null;
  if (transitionSelect) transitionSelect.value = (appSettings as Record<string, unknown>).transition as string || 'crossfade';
  if (transitionDurationSlider) {
    transitionDurationSlider.value = String((appSettings as Record<string, unknown>).transitionDuration || 400);
    if (transitionDurationVal) transitionDurationVal.textContent = transitionDurationSlider.value + 'ms';
    transitionDurationSlider.addEventListener('input', () => {
      if (transitionDurationVal) transitionDurationVal.textContent = transitionDurationSlider.value + 'ms';
    });
  }
  const thumbLoadingPausedCb = panel.querySelector('#settingsThumbnailLoadingPaused') as HTMLInputElement | null;
  if (thumbLoadingPausedCb) thumbLoadingPausedCb.checked = !!appSettings.thumbnailLoadingPaused;

  const defaultViewSelect = panel.querySelector('#settingsDefaultView') as HTMLSelectElement | null;
  if (defaultViewSelect) {
    const dvVal = (appSettings as Record<string, unknown>).defaultView as string || 'split-v';
    defaultViewSelect.value = ['split-v', 'split-h', 'preview', 'code'].includes(dvVal) ? dvVal : 'split-v';
  }
  const defaultParamValueInput = panel.querySelector('#settingsDefaultParamValue') as HTMLInputElement | null;
  if (defaultParamValueInput) defaultParamValueInput.value = String((appSettings as Record<string, unknown>).defaultParamValue ?? 0);
  const defaultTimeScaleInput = panel.querySelector('#settingsDefaultTimeScale') as HTMLInputElement | null;
  if (defaultTimeScaleInput) defaultTimeScaleInput.value = String((appSettings as Record<string, unknown>).defaultTimeScale ?? 1);

  // LLM provider chain UI
  const llmListEl = panel.querySelector('#llmProviderList') as HTMLElement | null;
  const llmTestBtn = panel.querySelector('#llmTestBtn') as HTMLButtonElement | null;
  const llmStatusText = panel.querySelector('#llmStatusText') as HTMLElement | null;
  const defaultProviders: LLMProviderConfig[] = [
    { name: 'local', enabled: true, priority: 1 },
    { name: 'ollama', enabled: true, priority: 2, model: 'llama3.2', endpoint: 'http://localhost:11434' },
    { name: 'cursor', enabled: false, priority: 3 },
  ];
  let llmProviders: LLMProviderConfig[] = (appSettings as Record<string, unknown>).llmProviders as LLMProviderConfig[] || defaultProviders;
  if (!Array.isArray(llmProviders) || llmProviders.length === 0) llmProviders = defaultProviders;

  function renderLLMProviders(): void {
    if (!llmListEl) return;
    llmListEl.innerHTML = '';
    const sorted = [...llmProviders].sort((a, b) => a.priority - b.priority);
    for (const p of sorted) {
      const row = document.createElement('div');
      row.style.cssText = 'display:flex;gap:6px;align-items:center;padding:6px 8px;background:var(--amiga-bg);border:1px solid var(--bevel-dark);';
      const labelColors: Record<string, string> = { local: '#2ecc71', ollama: '#4488cc', cursor: '#9b59b6' };
      const labelNames: Record<string, string> = { local: 'Local (regex, free)', ollama: 'Ollama (local LLM)', cursor: 'Cursor (cloud tokens)' };
      const cb = document.createElement('input');
      cb.type = 'checkbox';
      cb.checked = p.enabled;
      cb.style.cssText = 'margin:0;';
      cb.disabled = p.name === 'local'; // local always enabled
      cb.onchange = () => {
        const idx = llmProviders.findIndex((x) => x.name === p.name);
        if (idx >= 0) llmProviders[idx].enabled = cb.checked;
      };
      const label = document.createElement('span');
      label.style.cssText = 'font-size:11px;color:' + (labelColors[p.name] || 'var(--crt-fg)') + ';min-width:140px;';
      label.textContent = '#' + p.priority + ' ' + (labelNames[p.name] || p.name);
      row.appendChild(cb);
      row.appendChild(label);
      if (p.name === 'ollama') {
        const modelInput = document.createElement('input');
        modelInput.type = 'text';
        modelInput.value = p.model || 'llama3.2';
        modelInput.placeholder = 'model name';
        modelInput.style.cssText = 'flex:1;padding:4px;font-size:10px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);min-width:100px;';
        modelInput.onchange = () => {
          const idx = llmProviders.findIndex((x) => x.name === 'ollama');
          if (idx >= 0) llmProviders[idx].model = modelInput.value.trim();
        };
        row.appendChild(modelInput);
        const epInput = document.createElement('input');
        epInput.type = 'text';
        epInput.value = p.endpoint || 'http://localhost:11434';
        epInput.placeholder = 'endpoint URL';
        epInput.style.cssText = 'width:140px;padding:4px;font-size:10px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);';
        epInput.onchange = () => {
          const idx = llmProviders.findIndex((x) => x.name === 'ollama');
          if (idx >= 0) llmProviders[idx].endpoint = epInput.value.trim();
        };
        row.appendChild(epInput);
      }
      const priInput = document.createElement('input');
      priInput.type = 'number';
      priInput.min = '1';
      priInput.max = '3';
      priInput.value = String(p.priority);
      priInput.style.cssText = 'width:36px;padding:4px;font-size:10px;background:var(--amiga-bg);color:var(--crt-fg);border:1px solid var(--bevel-dark);text-align:center;';
      priInput.title = 'Priority (1=first, 3=last)';
      priInput.onchange = () => {
        const idx = llmProviders.findIndex((x) => x.name === p.name);
        if (idx >= 0) llmProviders[idx].priority = parseInt(priInput.value, 10) || p.priority;
        renderLLMProviders();
      };
      row.appendChild(priInput);
      llmListEl.appendChild(row);
    }
  }
  renderLLMProviders();

  if (llmTestBtn) {
    llmTestBtn.onclick = () => {
      if (llmStatusText) llmStatusText.textContent = 'Testing...';
      fetchLLMStatus().then((st) => {
        if (llmStatusText) {
          if (st.ollamaOnline) {
            llmStatusText.textContent = 'Ollama: ONLINE (' + (st.ollamaModels || []).length + ' models)';
            llmStatusText.style.color = '#2ecc71';
          } else {
            llmStatusText.textContent = 'Ollama: OFFLINE (install from ollama.com)';
            llmStatusText.style.color = '#ff8888';
          }
        }
      }).catch(() => {
        if (llmStatusText) { llmStatusText.textContent = 'Test failed'; llmStatusText.style.color = '#ff8888'; }
      });
    };
  }

  const previewResSelect = panel.querySelector('#settingsPreviewResolution') as HTMLSelectElement | null;
  const thumbQualitySlider = panel.querySelector('#settingsThumbnailQuality') as HTMLInputElement | null;
  const thumbQualityVal = panel.querySelector('#settingsThumbnailQualityVal') as HTMLElement | null;
  const thumbMaxSizeSelect = panel.querySelector('#settingsThumbnailMaxSize') as HTMLSelectElement | null;

  if (previewResSelect) {
    const val = appSettings.previewResolution || 'auto';
    previewResSelect.value = ['auto', '640x360', '854x480', '1280x720', '1920x1080'].includes(val) ? val : 'auto';
  }
  const previewQualitySelect = panel.querySelector('#settingsPreviewQuality') as HTMLSelectElement | null;
  if (previewQualitySelect) {
    const q = appSettings.previewQuality ?? 1;
    const v = String(q);
    previewQualitySelect.value = ['0.5', '0.75', '1'].includes(v) ? v : '1';
  }
  if (thumbQualitySlider) {
    const q = appSettings.thumbnailQuality ?? 0.5;
    thumbQualitySlider.value = String(Math.round(q * 10) / 10);
  }
  if (thumbQualityVal && thumbQualitySlider) {
    thumbQualityVal.textContent = thumbQualitySlider.value;
  }
  if (thumbMaxSizeSelect) {
    const s = appSettings.thumbnailMaxSize ?? 120;
    const str = String(s);
    const opt = thumbMaxSizeSelect.querySelector('option[value="' + str + '"]');
    thumbMaxSizeSelect.value = opt ? str : '120';
  }

  thumbQualitySlider?.addEventListener('input', () => {
    if (thumbQualityVal && thumbQualitySlider) thumbQualityVal.textContent = thumbQualitySlider.value;
  });

  const paths = appSettings.sourcePaths || [];
  paths.forEach((p: string, i: number) => {
    const row = document.createElement('div');
    row.style.cssText = 'display:flex; gap:8px; align-items:center;';
    row.innerHTML = `
      <input type="text" value="${escapeHtml(p)}" data-index="${i}" class="path-input" style="flex:1; padding:6px; background:var(--amiga-bg); color:var(--crt-fg); border:1px solid var(--bevel-dark); font-size:12px;" />
      <button class="path-remove" data-index="${i}" title="TLDR: Remove this path" style="background:transparent; color:#ff8888; border:1px solid #8a4444; padding:4px 8px; cursor:pointer;">Remove</button>
      <button class="path-git-init" data-path="${escapeHtml(p)}" title="TLDR: Initialize git in this folder for version control (library paths only, not VJ-Sorted-Production)" style="display:none; background:var(--amiga-surface); color:var(--amiga-copper); border:1px solid var(--bevel-dark); padding:4px 8px; cursor:pointer; font-size:11px;">Git init</button>
    `;
    const removeBtn = row.querySelector('.path-remove');
    removeBtn?.addEventListener('click', () => {
      postSources({ action: 'remove', index: i })
        .then((r) => {
          setAppSettings({ sourcePaths: r.paths });
          renderPanel();
          status('Path removed');
        })
        .catch((e) => status(String((e as Error).message), true));
    });
    const gitInitBtn = row.querySelector('.path-git-init') as HTMLButtonElement | null;
    const pathForInit = p.trim();
    const isVJSorted = pathForInit.indexOf('VJ-Sorted-Production') !== -1;
    if (gitInitBtn && !isVJSorted) {
      fetchGitRepoStatus(pathForInit)
        .then(({ isRepo }) => {
          if (!isRepo) gitInitBtn.style.display = '';
        })
        .catch(() => {});
      gitInitBtn.addEventListener('click', () => {
        const currentPath = (row.querySelector('.path-input') as HTMLInputElement)?.value?.trim() || pathForInit;
        gitInitBtn.disabled = true;
        postGitInit(currentPath)
          .then((r) => {
            status(r.message || 'Git initialized');
            renderPanel();
          })
          .catch((e) => status(String((e as Error).message), true))
          .finally(() => { gitInitBtn.disabled = false; });
      });
    }
    pathsList?.appendChild(row);
  });

  if (indexPathInput) indexPathInput.value = appSettings.indexPath || '';
  const graveyardPathInput = panel.querySelector('#settingsGraveyardPath') as HTMLInputElement | null;
  if (graveyardPathInput) graveyardPathInput.value = appSettings.graveyardPath || '';
  if (wirePathInput) wirePathInput.value = appSettings.wirePath || '';
  if (hardResetPathInput) hardResetPathInput.value = (appSettings as Record<string, unknown>).hardResetPath as string || '';
  if (cursorApiKeyInput) {
    const key = (appSettings as Record<string, unknown>).cursorApiKey as string || '';
    cursorApiKeyInput.value = (key && key !== '***') ? key : '';
    cursorApiKeyInput.placeholder = key === '***' ? '(saved - enter new to replace)' : 'key_xxx...';
  }
  const githubTokenInput = panel.querySelector('#settingsGithubToken') as HTMLInputElement | null;
  const githubStatusEl = panel.querySelector('#githubStatus');
  if (githubTokenInput) {
    const tok = (appSettings as Record<string, unknown>).githubToken as string || '';
    githubTokenInput.value = (tok && tok !== '***') ? tok : '';
  }
  if (githubStatusEl) {
    fetchGithubStatus()
      .then((st) => {
        if (st.logged_in) {
          githubStatusEl.textContent = 'Logged in as ' + (st.user || 'GitHub');
          githubStatusEl.style.color = 'var(--crt-fg)';
        } else {
          githubStatusEl.textContent = 'Not logged in. Run gh auth login in a terminal.';
          githubStatusEl.style.color = 'var(--crt-dim)';
        }
      })
      .catch(() => {
        githubStatusEl.textContent = 'gh CLI not found or error. Install GitHub CLI (gh) to use.';
        githubStatusEl.style.color = 'var(--crt-dim)';
      });
  }

  const pathsBtn = panel.querySelector('#settingsPathsInfo');
  pathsBtn?.addEventListener('click', () => {
    showPathsInfo(appSettings.sourcePaths || [], appSettings.indexPath || '', entries.length);
  });

  addBtn?.addEventListener('click', () => {
    showPathPicker((selectedPath) => {
      if (!selectedPath) return;
      postSources({ action: 'add', path: selectedPath })
        .then((r) => {
          if (r) {
            setAppSettings({ sourcePaths: r.paths });
            renderPanel();
            status('Path added: ' + r.paths[r.paths.length - 1]);
          }
        })
        .catch((e) => status(String((e as Error).message), true));
    });
  });

  saveBtn?.addEventListener('click', () => {
    const newCursorApiKey = cursorApiKeyInput?.value?.trim() || '';
    const pathInputs = panel.querySelectorAll('.path-input');
    const newPaths: string[] = [];
    pathInputs.forEach((inp) => {
      const v = (inp as HTMLInputElement).value?.trim();
      if (v) newPaths.push(v);
    });
    const payload: Partial<Settings> = { ...appSettings, sourcePaths: newPaths };
    const wirePathInputEl = panel.querySelector('#settingsWirePath') as HTMLInputElement | null;
    if (wirePathInputEl) payload.wirePath = wirePathInputEl.value?.trim() || '';
    const hardResetPathInputEl = panel.querySelector('#settingsHardResetPath') as HTMLInputElement | null;
    if (hardResetPathInputEl) (payload as Record<string, unknown>).hardResetPath = hardResetPathInputEl.value?.trim() || '';
    if (showThumbnailsCb) payload.showThumbnails = showThumbnailsCb.checked;
    if (skipSplashCb) payload.skipSplash = skipSplashCb.checked;
    const thumbLoadingPausedCb = panel.querySelector('#settingsThumbnailLoadingPaused') as HTMLInputElement | null;
    if (thumbLoadingPausedCb) payload.thumbnailLoadingPaused = thumbLoadingPausedCb.checked;
    const previewResSelect = panel.querySelector('#settingsPreviewResolution') as HTMLSelectElement | null;
    const thumbQualitySlider = panel.querySelector('#settingsThumbnailQuality') as HTMLInputElement | null;
    const thumbMaxSizeSelect = panel.querySelector('#settingsThumbnailMaxSize') as HTMLSelectElement | null;
    const transSelect = panel.querySelector('#settingsTransition') as HTMLSelectElement | null;
    const transDurSlider = panel.querySelector('#settingsTransitionDuration') as HTMLInputElement | null;
    if (transSelect) (payload as Record<string, unknown>).transition = transSelect.value || 'crossfade';
    if (transDurSlider) (payload as Record<string, unknown>).transitionDuration = parseInt(transDurSlider.value, 10) || 400;
    const dvSelect = panel.querySelector('#settingsDefaultView') as HTMLSelectElement | null;
    if (dvSelect) (payload as Record<string, unknown>).defaultView = dvSelect.value || 'split-v';
    const defaultParamValueInp = panel.querySelector('#settingsDefaultParamValue') as HTMLInputElement | null;
    if (defaultParamValueInp) (payload as Record<string, unknown>).defaultParamValue = parseFloat(defaultParamValueInp.value);
    const defaultTimeScaleInp = panel.querySelector('#settingsDefaultTimeScale') as HTMLInputElement | null;
    if (defaultTimeScaleInp) (payload as Record<string, unknown>).defaultTimeScale = Math.max(0, Math.min(4, parseFloat(defaultTimeScaleInp.value) || 1));
    if (llmProviders && llmProviders.length > 0) {
      (payload as Record<string, unknown>).llmProviders = llmProviders;
      postLLMConfig(llmProviders).catch(() => {});
    }
    if (previewResSelect) payload.previewResolution = previewResSelect.value || 'auto';
    const previewQualitySelect = panel.querySelector('#settingsPreviewQuality') as HTMLSelectElement | null;
    if (previewQualitySelect) payload.previewQuality = parseFloat(previewQualitySelect.value) || 1;
    if (thumbQualitySlider) payload.thumbnailQuality = parseFloat(thumbQualitySlider.value) || 0.5;
    if (thumbMaxSizeSelect) {
      const v = parseInt(thumbMaxSizeSelect.value, 10);
      payload.thumbnailMaxSize = isNaN(v) ? 120 : v;
    }
    if (newCursorApiKey) {
      (payload as Record<string, unknown>).cursorApiKey = newCursorApiKey;
    } else {
      delete (payload as Record<string, unknown>).cursorApiKey;
    }
    const githubTokenInp = panel.querySelector('#settingsGithubToken') as HTMLInputElement | null;
    if (githubTokenInp) {
      const v = githubTokenInp.value?.trim() || '';
      if (v) (payload as Record<string, unknown>).githubToken = v;
      else delete (payload as Record<string, unknown>).githubToken;
    }
    const lastTheme = getLastAppliedTheme();
    payload.themeColors = lastTheme;
    postSettings(payload)
      .then(() => {
        setAppSettings(payload);
        resizeCanvas();
        buildList();
        status('Settings saved');
        closePanel();
        loadSequence().catch(() => {});
      })
      .catch((e) => status(String((e as Error).message), true));
  });

  nukeBtn?.addEventListener('click', () => {
    const msg =
      'NUKE: Backup + Clear Index + Rescan\n\n' +
      'This will do the following 3 things:\n\n' +
      '  1. BACKUP — the current index (macroverse.db) is copied to a\n' +
      '     timestamped snapshot file in the same folder.\n\n' +
      '  2. CLEAR — the entire SQLite shader index is wiped. All tags, sets,\n' +
      '     categories, favourites, and version history are erased.\n\n' +
      '  3. RESCAN — all source paths are re-walked. Every shader file found\n' +
      '     on disk is re-indexed from scratch with no metadata.\n\n' +
      'Your actual shader FILES on disk are NOT modified. Only the database\n' +
      'is cleared and rebuilt.\n\n' +
      'You can restore the backup.db manually if needed.\n\n' +
      'Continue?';
    if (!confirm(msg)) return;
    status('Backing up...');
    postIndexBackup()
      .then(() => {
        status('Clearing and scanning...');
        return postIndexClear();
      })
      .then(() => {
        status('Scan complete. Reloading...');
        loadSequence().catch(() => {});
        closePanel();
      })
      .catch((e) => status(String((e as Error).message), true));
  });

  hardResetShadersBtn?.addEventListener('click', () => {
    const targetPath = (panel.querySelector('#settingsHardResetPath') as HTMLInputElement | null)?.value?.trim() || 'shaders/custom/';
    const msg =
      'HARD RESET: ' + targetPath + '\n\n' +
      'What will happen:\n\n' +
      '  1. BACKUP — a zip of the current folder is saved next to\n' +
      '     macroverse.db (timestamped, safe to restore).\n\n' +
      '  2. RESTORE — all files in the target folder are checked out from\n' +
      '     git back to the first commit on main. Any edits you made are reverted.\n\n' +
      '  3. INDEX — the app will rescan after reset so the list reflects the\n' +
      '     restored files.\n\n' +
      'You can restore from the backup zip if you change your mind.\n\n' +
      'Continue?';
    if (!confirm(msg)) return;
    status('Creating backup and resetting shaders...');
    postGitHardResetShaders({ ref: 'init' })
      .then((data) => {
        const backupPath = data.backupPath || '(see server logs)';
        status('Done. Reload to see changes.');
        alert(
          'Hard reset complete.\n\n' +
            targetPath + ' was restored to its original committed state.\n\n' +
            'Backup zip saved at:\n' +
            backupPath +
            '\n\nReloading the shader list now.'
        );
        closePanel();
        loadSequence().catch(() => {});
      })
      .catch((e) => status(String((e as Error).message), true));
  });
}

export function initSettings(): void {
  const toolbar = document.querySelector('.toolbar');
  if (!toolbar) return;
  const pathsInfoBtn = document.createElement('button');
  pathsInfoBtn.id = 'pathsInfoBtn';
  pathsInfoBtn.textContent = 'Paths';
  pathsInfoBtn.style.cssText = 'background:var(--amiga-surface); color:var(--amiga-accent); border:1px solid var(--bevel-dark); padding:6px 12px; cursor:pointer; margin-left:8px;';
  pathsInfoBtn.title = 'TLDR: Paths, index, and Reindex';
  pathsInfoBtn.onclick = () => showPathsInfo(appSettings.sourcePaths || [], appSettings.indexPath || '', entries.length);
  toolbar.appendChild(pathsInfoBtn);
  const btn = document.createElement('button');
  btn.id = 'settingsBtn';
  btn.textContent = 'Settings';
  btn.style.cssText = 'background:var(--amiga-surface); color:var(--amiga-accent); border:1px solid var(--bevel-dark); padding:6px 12px; cursor:pointer; margin-left:8px;';
  btn.title = 'TLDR: App settings (paths, API key, thumbnails)';
  btn.onclick = openPanel;
  toolbar.appendChild(btn);
}
