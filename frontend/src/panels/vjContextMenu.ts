import { status } from '../dom.js';
import { showContextMenu, type ContextMenuItem } from '../ui/contextMenu.js';
import { isGigOutputQrVisible, setGigOutputQrVisible } from '../gigOutputQr.js';
import { dispatchVjDeckLoad } from '../shaderPointerDrag.js';
import { loadShader } from '../render.js';
import { entries } from '../state.js';
import type { IndexEntry } from '../types.js';

export type VjContextMenuActions = {
  deckNavigate: (deck: 'A' | 'B', delta: number) => void;
  crossfaderInput: HTMLInputElement;
  mixModeSelect: HTMLSelectElement;
  autoVjToggle: () => void;
  autoVjTap: () => void;
  autoVjEnabled: () => boolean;
  oscToggle: () => void;
  oscActive: () => boolean;
  oscPortInput: HTMLInputElement;
  midiEnable: () => void;
  midiActive: () => boolean;
  audioToggle: () => void;
  audioActive: () => boolean;
  popOut: () => void;
  copyOutputUrl: () => void;
  flipH: () => void;
  flipV: () => void;
  flipHState: () => boolean;
  flipVState: () => boolean;
};

function entryForPath(path: string): IndexEntry | undefined {
  return entries.find((e) => (e.path ?? '') === path);
}

function openShaderInEditor(path: string): void {
  if (!path) return;
  const entry = entryForPath(path);
  if (entry) {
    void loadShader(entry);
    status('Opened in editor: ' + (entry.fixedName || entry.name || path));
    return;
  }
  status('Shader not in index: ' + path, true);
}

function loadToOtherDeck(fromDeck: 'A' | 'B', path: string): void {
  if (!path) return;
  dispatchVjDeckLoad(fromDeck === 'A' ? 'B' : 'A', path);
}

export function initVjContextMenus(host: HTMLElement, actions: VjContextMenuActions): void {
  host.addEventListener('contextmenu', (ev: Event) => {
    const e = ev as MouseEvent;
    const target = e.target as HTMLElement;
    if (target.closest('input, textarea, select, .mv-rich-slider, .vj-shader-picker, .vj-shader-filter')) {
      return;
    }
    if (target.closest('.vj-shader-picker-row')) {
      return;
    }

    e.preventDefault();
    e.stopPropagation();

    const deckEl = target.closest('.vj-deck') as HTMLElement | null;
    if (deckEl) {
      const deck = (deckEl.dataset.vjDeck === 'B' ? 'B' : 'A') as 'A' | 'B';
      const path = deckEl.dataset.vjShaderPath || '';
      const items: ContextMenuItem[] = [
        { label: 'Previous shader', action: () => actions.deckNavigate(deck, -1) },
        { label: 'Next shader', action: () => actions.deckNavigate(deck, 1) },
        { type: 'sep' },
        {
          label: 'Open shader in editor',
          action: () => openShaderInEditor(path),
          disabled: !path,
        },
        {
          label: 'Load to Deck ' + (deck === 'A' ? 'B' : 'A'),
          action: () => loadToOtherDeck(deck, path),
          disabled: !path,
        },
        {
          label: 'Copy shader path',
          action: () => {
            navigator.clipboard.writeText(path).then(() => status('Path copied')).catch(() => status('Copy failed', true));
          },
          disabled: !path,
        },
      ];
      showContextMenu(e.clientX, e.clientY, items, { className: 'ctx-menu--vj-deck' });
      return;
    }

    if (target.closest('.vj-mixer-panel')) {
      const xfade = parseFloat(actions.crossfaderInput.value);
      const items: ContextMenuItem[] = [
        {
          label: 'Crossfade center (50%)',
          action: () => {
            actions.crossfaderInput.value = '0.5';
            actions.crossfaderInput.dispatchEvent(new Event('input', { bubbles: true }));
          },
        },
        {
          label: 'Full Deck A',
          action: () => {
            actions.crossfaderInput.value = '0';
            actions.crossfaderInput.dispatchEvent(new Event('input', { bubbles: true }));
          },
          checked: xfade <= 0.01,
        },
        {
          label: 'Full Deck B',
          action: () => {
            actions.crossfaderInput.value = '1';
            actions.crossfaderInput.dispatchEvent(new Event('input', { bubbles: true }));
          },
          checked: xfade >= 0.99,
        },
        { type: 'sep' },
        {
          label: 'Mix mode: ' + actions.mixModeSelect.selectedOptions[0]?.textContent,
          action: () => {
            const idx = actions.mixModeSelect.selectedIndex;
            actions.mixModeSelect.selectedIndex = (idx + 1) % actions.mixModeSelect.options.length;
            actions.mixModeSelect.dispatchEvent(new Event('change', { bubbles: true }));
          },
        },
      ];
      showContextMenu(e.clientX, e.clientY, items, { className: 'ctx-menu--vj-mixer' });
      return;
    }

    if (target.closest('.vj-autovj-section')) {
      showContextMenu(e.clientX, e.clientY, [
        {
          label: actions.autoVjEnabled() ? 'Turn Auto VJ off' : 'Turn Auto VJ on',
          action: () => actions.autoVjToggle(),
          checked: actions.autoVjEnabled(),
        },
        { label: 'Tap tempo (BPM)', action: () => actions.autoVjTap() },
      ], { className: 'ctx-menu--vj-autovj' });
      return;
    }

    if (target.closest('.vj-osc-section')) {
      showContextMenu(e.clientX, e.clientY, [
        {
          label: actions.oscActive() ? 'Stop OSC listen' : 'Start OSC listen',
          action: () => actions.oscToggle(),
          checked: actions.oscActive(),
        },
        {
          label: 'Copy OSC port (' + actions.oscPortInput.value + ')',
          action: () => {
            navigator.clipboard.writeText(actions.oscPortInput.value).then(() => status('OSC port copied')).catch(() => status('Copy failed', true));
          },
        },
        {
          label: 'Set port to 9000',
          action: () => {
            actions.oscPortInput.value = '9000';
          },
        },
      ], { className: 'ctx-menu--vj-osc' });
      return;
    }

    if (target.closest('.vj-midi-section')) {
      showContextMenu(e.clientX, e.clientY, [
        {
          label: actions.midiActive() ? 'MIDI enabled' : 'Enable MIDI',
          action: () => actions.midiEnable(),
          disabled: actions.midiActive(),
          checked: actions.midiActive(),
        },
      ], { className: 'ctx-menu--vj-midi' });
      return;
    }

    if (target.closest('.vj-audio-section')) {
      showContextMenu(e.clientX, e.clientY, [
        {
          label: actions.audioActive() ? 'Stop audio FFT' : 'Start audio FFT',
          action: () => actions.audioToggle(),
          checked: actions.audioActive(),
        },
      ], { className: 'ctx-menu--vj-audio' });
      return;
    }

    if (target.closest('.vj-output-section')) {
      const qrOn = isGigOutputQrVisible();
      showContextMenu(e.clientX, e.clientY, [
        { label: 'Pop out VJ output', action: () => actions.popOut() },
        {
          label: qrOn ? 'Hide QR on output' : 'Show Audience QR',
          action: () => {
            if (qrOn) setGigOutputQrVisible(false);
            else setGigOutputQrVisible(true);
          },
          checked: qrOn,
        },
        { label: 'Copy output / stream URL', action: () => actions.copyOutputUrl() },
        { type: 'sep' },
        {
          label: actions.flipVState() ? 'Unflip vertical' : 'Flip vertical',
          action: () => actions.flipV(),
          checked: actions.flipVState(),
        },
        {
          label: actions.flipHState() ? 'Unflip horizontal' : 'Flip horizontal',
          action: () => actions.flipH(),
          checked: actions.flipHState(),
        },
      ], { className: 'ctx-menu--vj-output' });
      return;
    }

    if (target.closest('.vj-deck-wrap')) {
      showContextMenu(e.clientX, e.clientY, [
        { label: 'Pop out VJ output', action: () => actions.popOut() },
        { label: 'Copy output / stream URL', action: () => actions.copyOutputUrl() },
      ], { className: 'ctx-menu--vj' });
    }
  });
}
