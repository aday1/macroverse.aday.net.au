/**
 * Core commands registered at boot.
 *
 * Most commands proxy to existing UI elements (Save, Revert, etc.)
 * by clicking the underlying button. This keeps a single source of
 * truth: the toolbar wiring stays in codeEditor.ts / settings.ts /
 * etc., and the palette is just another way to trigger it.
 */

import { registerCommand } from './commandRegistry.js';
import { hasDesktopShell, hasLocalAgents, hasWirePipeline } from '../hostCapabilities.js';
import { openGigQrHub } from '../gigQrHub.js';
import { openJumpIntoVrChooser } from '../jumpIntoVr.js';

function clickById(id: string): void {
  const el = document.getElementById(id);
  if (el) (el as HTMLElement).click();
}

function clickViewTab(view: string): void {
  const tab = document.querySelector(`.view-tab[data-view="${view}"]`) as HTMLElement | null;
  tab?.click();
}

function toggleClass(id: string): void {
  const cb = document.getElementById(id) as HTMLInputElement | null;
  if (cb) {
    cb.checked = !cb.checked;
    cb.dispatchEvent(new Event('change', { bubbles: true }));
  }
}

/** Toggle a display effect via the global helper exposed by
 *  bootstrap.ts. Falls back to clicking a checkbox if the global
 *  isn't installed yet (defensive). */
function toggleEffect(name: 'scanline' | 'vignette' | 'crt', fallbackId: string): void {
  const fn = (globalThis as unknown as { toggleDisplayEffect?: (n: string) => void }).toggleDisplayEffect;
  if (typeof fn === 'function') {
    fn(name);
    return;
  }
  toggleClass(fallbackId);
}

export function registerCoreCommands(): void {
  // ── Editor
  registerCommand({
    id: 'editor.save',
    label: 'Save shader',
    description: 'Persist current changes (auto-commits if Git enabled)',
    category: 'Editor',
    shortcut: 'Ctrl+S',
    run: () => clickById('codeSaveBtn'),
    keywords: ['write', 'commit']
  });
  registerCommand({
    id: 'editor.revert',
    label: 'Revert to saved',
    description: 'Discard unsaved changes',
    category: 'Editor',
    run: () => clickById('codeRevertBtn'),
    keywords: ['discard', 'reset']
  });
  registerCommand({
    id: 'editor.reload',
    label: 'Reload from disk',
    description: 'Pick up edits made by external tools',
    category: 'Editor',
    run: () => clickById('codeReloadBtn'),
    keywords: ['refresh']
  });
  registerCommand({
    id: 'editor.undo',
    label: 'Undo edit',
    description: 'Editor-level undo',
    category: 'Editor',
    shortcut: 'Ctrl+Z',
    run: () => clickById('codeUndoBtn')
  });

  // ── Parameters / shader work
  registerCommand({
    id: 'params.expose',
    label: 'Expose parameters',
    description: 'Scan code for exposable values',
    category: 'Parameters',
    run: () => clickById('codeExposeBtn'),
    keywords: ['detect', 'find', 'sliders']
  });
  registerCommand({
    id: 'params.search',
    label: 'Search parameters',
    description: 'Find numeric values that can become sliders',
    category: 'Parameters',
    run: () => clickById('codeSearchParamsBtn')
  });
  registerCommand({
    id: 'params.refactor',
    label: 'Refactor parameters (AI)',
    description: 'Cursor AI refactor pass on exposable params',
    category: 'Parameters',
    run: () => clickById('codeRefactorParamsBtn'),
    keywords: ['ai', 'cursor']
  });
  registerCommand({
    id: 'params.vibe',
    label: 'Visual modify (Vibe)',
    description: 'Cursor AI visual modification',
    category: 'Parameters',
    run: () => clickById('codeVisualModifyBtn'),
    keywords: ['ai', 'cursor', 'vibe']
  });
  registerCommand({
    id: 'params.vibe.github',
    label: 'Visual modify (GitHub Copilot)',
    description: 'Vibe via GitHub Copilot',
    category: 'Parameters',
    run: () => clickById('codeVibeGithubBtn'),
    keywords: ['ai', 'github', 'copilot']
  });

  // ── Wire / ISF
  registerCommand({
    id: 'wire.check',
    label: 'Check ISF Wire compatibility',
    description: 'Validate ISF compatibility for Resolume Wire',
    category: 'Wire',
    when: () => hasWirePipeline(),
    run: () => clickById('codeCheckISFWireBtn')
  });
  registerCommand({
    id: 'wire.copy',
    label: 'Copy ISF to clipboard for Wire',
    description: 'Copy ISF version with INPUTS array',
    category: 'Wire',
    when: () => hasWirePipeline(),
    run: () => clickById('codeCopyForWireBtn'),
    keywords: ['paste', 'export']
  });

  // ── External editors
  registerCommand({
    id: 'ext.cursor',
    label: 'Open in Cursor',
    description: 'Edit shader in Cursor IDE',
    category: 'External',
    when: () => hasDesktopShell(),
    run: () => clickById('codeOpenCursorBtn'),
    keywords: ['ide']
  });
  registerCommand({
    id: 'ext.agent',
    label: 'Launch cursor-agent',
    description: 'Open agent terminal in shader directory',
    category: 'External',
    when: () => hasLocalAgents(),
    run: () => clickById('codeOpenAgentBtn')
  });
  registerCommand({
    id: 'ext.explorer',
    label: 'Show in file explorer',
    description: 'Reveal shader file in Windows Explorer / file manager',
    category: 'External',
    when: () => hasDesktopShell(),
    run: () => clickById('codeOpenExplorerBtn')
  });
  registerCommand({
    id: 'ext.notepad',
    label: 'Open in Notepad',
    description: 'Quick edit in Notepad',
    category: 'External',
    when: () => hasDesktopShell(),
    run: () => clickById('codeOpenNotepadBtn')
  });

  // ── Views
  registerCommand({
    id: 'view.preview',
    label: 'View: Preview',
    category: 'View',
    run: () => clickViewTab('preview')
  });
  registerCommand({
    id: 'view.code',
    label: 'View: Code editor',
    category: 'View',
    run: () => clickViewTab('code')
  });
  registerCommand({
    id: 'view.split',
    label: 'View: Split',
    category: 'View',
    run: () => clickViewTab('split')
  });
  registerCommand({
    id: 'view.vj',
    label: 'View: VJ deck',
    category: 'View',
    run: () => clickViewTab('vj'),
    keywords: ['mix', 'crossfader', 'a/b']
  });
  registerCommand({
    id: 'vj.show-qrs',
    label: 'Show QR codes',
    description: 'All gig QRs: VJ join, audience stream, VR audience, VR VJ',
    category: 'VJ',
    run: () => openGigQrHub(),
    keywords: ['qr', 'session', 'audience', 'vr', 'webxr', 'quest', 'collaboration']
  });
  registerCommand({
    id: 'vj.jump-vr',
    label: 'Jump into VR',
    description: 'Open WebXR audience or VJ controller for this gig session',
    category: 'VJ',
    run: () => openJumpIntoVrChooser(),
    keywords: ['vr', 'webxr', 'quest', 'headset', 'immersive', 'dome']
  });
  registerCommand({
    id: 'view.gallery',
    label: 'View: Gallery',
    category: 'View',
    run: () => clickViewTab('gallery'),
    keywords: ['grid', 'thumbnails']
  });
  registerCommand({
    id: 'view.pipeline',
    label: 'View: Pipeline',
    category: 'View',
    when: () => hasWirePipeline(),
    run: () => clickViewTab('pipeline'),
    keywords: ['signal', 'flow']
  });
  registerCommand({
    id: 'view.wire',
    label: 'View: Wire Hub',
    category: 'View',
    when: () => hasWirePipeline(),
    run: () => clickViewTab('wire'),
    keywords: ['resolume']
  });

  // ── App actions
  registerCommand({
    id: 'app.settings',
    label: 'Open settings',
    description: 'App settings, theme, AI providers',
    category: 'App',
    run: () => clickById('sidebarSettingsBtn'),
    keywords: ['preferences', 'config', 'theme']
  });
  registerCommand({
    id: 'app.quickstart',
    label: 'Quick start guide',
    description: 'First-visit tour: library, views, VJ, command palette',
    category: 'App',
    run: async () => {
      const { showQuickStartGuide } = await import('./quickStartGuide.js');
      showQuickStartGuide(true);
    },
    keywords: ['onboarding', 'tutorial', 'new', 'first']
  });
  registerCommand({
    id: 'app.help',
    label: 'Open help',
    description: 'Keyboard shortcuts, features, Wire export tips',
    category: 'App',
    run: () => clickById('sidebarHelpBtn'),
    keywords: ['shortcuts', 'tips']
  });
  registerCommand({
    id: 'app.paths',
    label: 'Manage shader source paths',
    description: 'Add or remove folders to scan for shaders',
    category: 'App',
    when: () => hasDesktopShell(),
    run: () => clickById('sidebarPathsBtn')
  });
  registerCommand({
    id: 'app.rescan',
    label: 'Rescan shader folders',
    description: 'Look for new or changed shaders',
    category: 'App',
    when: () => hasDesktopShell(),
    run: () => clickById('sidebarRescanBtn'),
    keywords: ['refresh', 'index']
  });
  registerCommand({
    id: 'app.update',
    label: 'Check for updates',
    description: 'Pull latest from GitHub and rebuild',
    category: 'App',
    when: () => hasDesktopShell(),
    run: () => clickById('sidebarUpdateBtn')
  });

  // ── Display effects (toggles)
  registerCommand({
    id: 'effects.scanlines',
    label: 'Toggle scanlines',
    description: 'Horizontal scanline overlay on preview',
    category: 'Display effects',
    run: () => toggleEffect('scanline', 'scanlineToggle'),
    keywords: ['crt', 'retro']
  });
  registerCommand({
    id: 'effects.vignette',
    label: 'Toggle vignette',
    description: 'Darkened corners on preview',
    category: 'Display effects',
    run: () => toggleEffect('vignette', 'vignetteToggle')
  });
  registerCommand({
    id: 'effects.crt-code',
    label: 'Toggle code view',
    description: 'Green terminal CRT look on the code editor',
    category: 'Display effects',
    run: () => toggleEffect('crt', 'crtCodeToggle')
  });
  registerCommand({
    id: 'effects.fullscreen',
    label: 'Toggle full screen',
    description: 'Browser full-screen mode (F11)',
    category: 'Display effects',
    shortcut: 'F11',
    run: () => clickById('fullscreenBtn')
  });

  // ── Panels (Phase 4 wires the docks; commands work today via the
  // collapse buttons that already exist on desktop)
  registerCommand({
    id: 'panel.index.toggle',
    label: 'Toggle library panel',
    description: 'Show or hide the shader index column',
    category: 'Panels',
    run: () => clickById('indexCollapseBtn'),
    keywords: ['list', 'sidebar']
  });
  registerCommand({
    id: 'panel.params.toggle',
    label: 'Toggle parameters panel',
    description: 'Show or hide the right column',
    category: 'Panels',
    run: () => clickById('rightCollapseBtn')
  });
}
