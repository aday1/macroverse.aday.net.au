export interface ThemeColors {
  amigaBg: string;
  amigaSurface: string;
  amigaPanel: string;
  amigaText: string;
  amigaTextDim: string;
  amigaAccent: string;
  amigaCopper: string;
  bevelDark: string;
  bevelLight: string;
  editorBg: string;
  editorFg: string;
  editorKeyword: string;
  editorString: string;
  editorComment: string;
  editorNumber: string;
  editorPunctuation: string;
  editorOperator: string;
  editorFunction: string;
  editorCaret: string;
  editorSelection: string;
  editorGlow: string;
}

const SYNTHWAVE_THEME: ThemeColors = {
  amigaBg: '#0a0a14',
  amigaSurface: '#141428',
  amigaPanel: '#1a1a30',
  amigaText: '#c0c0d0',
  amigaTextDim: '#666688',
  amigaAccent: '#4488cc',
  amigaCopper: '#ee8833',
  bevelDark: '#060610',
  bevelLight: '#3a3a5a',
  editorBg: '#0a1208',
  editorFg: '#00ff88',
  editorKeyword: '#00ffaa',
  editorString: '#88ffcc',
  editorComment: '#4a8a5a',
  editorNumber: '#66ff99',
  editorPunctuation: '#5acc7a',
  editorOperator: '#44dd88',
  editorFunction: '#00ffcc',
  editorCaret: '#00ff88',
  editorSelection: 'rgba(0,255,136,0.25)',
  editorGlow: 'rgba(0,255,136,0.15)'
};

const WORKBENCH_THEME: ThemeColors = {
  amigaBg: '#2060b8',
  amigaSurface: '#4a8ad4',
  amigaPanel: '#5c9ae0',
  amigaText: '#ffffff',
  amigaTextDim: '#b8d4ff',
  amigaAccent: '#ffcc00',
  amigaCopper: '#ff8800',
  bevelDark: '#104080',
  bevelLight: '#7ab4ff',
  editorBg: '#104080',
  editorFg: '#c8dcff',
  editorKeyword: '#ffcc00',
  editorString: '#88ff88',
  editorComment: '#66aa88',
  editorNumber: '#ffaa66',
  editorPunctuation: '#c8dcff',
  editorOperator: '#ffcc00',
  editorFunction: '#88ddff',
  editorCaret: '#c8dcff',
  editorSelection: 'rgba(255,204,0,0.25)',
  editorGlow: 'rgba(255,204,0,0.1)'
};

export const DEFAULT_THEME: ThemeColors = {
  amigaBg: '#e8e4f0',
  amigaSurface: '#f4f2f8',
  amigaPanel: '#ffffff',
  amigaText: '#1a1428',
  amigaTextDim: '#7a6e98',
  amigaAccent: '#3366bb',
  amigaCopper: '#cc6600',
  bevelDark: '#c8c0d8',
  bevelLight: '#d8d2e4',
  editorBg: '#faf8ff',
  editorFg: '#1a1428',
  editorKeyword: '#8822cc',
  editorString: '#227744',
  editorComment: '#998ab8',
  editorNumber: '#cc5500',
  editorPunctuation: '#444060',
  editorOperator: '#8822cc',
  editorFunction: '#2266bb',
  editorCaret: '#1a1428',
  editorSelection: 'rgba(51,102,187,0.18)',
  editorGlow: 'rgba(51,102,187,0.06)'
};

export interface ThemePreset {
  id: string;
  name: string;
  theme: ThemeColors;
}

export const THEME_PRESETS: ThemePreset[] = [
  {
    id: 'light',
    name: 'Light',
    theme: { ...DEFAULT_THEME }
  },
  {
    id: 'workbench31',
    name: 'Workbench 3.1',
    theme: { ...WORKBENCH_THEME }
  },
  {
    id: 'synthwave',
    name: 'Synthwave',
    theme: { ...SYNTHWAVE_THEME }
  },
  {
    id: 'amiga',
    name: 'Amiga',
    theme: {
      ...SYNTHWAVE_THEME,
      amigaBg: '#000000',
      amigaSurface: '#222244',
      amigaPanel: '#333366',
      amigaText: '#aaccff',
      amigaTextDim: '#6688aa',
      amigaAccent: '#4488cc',
      amigaCopper: '#ee8833',
      bevelDark: '#111122',
      bevelLight: '#5566aa',
      editorBg: '#0a0a18',
      editorFg: '#88aaff',
      editorKeyword: '#ffaa00',
      editorString: '#88ff88',
      editorComment: '#668866',
      editorNumber: '#ffaa66',
      editorPunctuation: '#aaaacc',
      editorOperator: '#aaccff',
      editorFunction: '#ffcc66',
      editorCaret: '#88aaff',
      editorSelection: 'rgba(136,170,255,0.25)',
      editorGlow: 'rgba(136,170,255,0.12)'
    }
  },
  {
    id: 'nord',
    name: 'Nord',
    theme: {
      ...SYNTHWAVE_THEME,
      amigaBg: '#2e3440',
      amigaSurface: '#3b4252',
      amigaPanel: '#434c5e',
      amigaText: '#eceff4',
      amigaTextDim: '#8190a0',
      amigaAccent: '#88c0d0',
      amigaCopper: '#d08770',
      bevelDark: '#2e3440',
      bevelLight: '#4c566a',
      editorBg: '#2e3440',
      editorFg: '#d8dee9',
      editorKeyword: '#81a1c1',
      editorString: '#a3be8c',
      editorComment: '#616e88',
      editorNumber: '#b48ead',
      editorPunctuation: '#eceff4',
      editorOperator: '#81a1c1',
      editorFunction: '#88c0d0',
      editorCaret: '#d8dee9',
      editorSelection: 'rgba(136,192,208,0.25)',
      editorGlow: 'rgba(136,192,208,0.1)'
    }
  },
  {
    id: 'dracula',
    name: 'Dracula',
    theme: {
      ...SYNTHWAVE_THEME,
      amigaBg: '#282a36',
      amigaSurface: '#343746',
      amigaPanel: '#44475a',
      amigaText: '#f8f8f2',
      amigaTextDim: '#6272a4',
      amigaAccent: '#bd93f9',
      amigaCopper: '#ffb86c',
      bevelDark: '#21222c',
      bevelLight: '#6272a4',
      editorBg: '#282a36',
      editorFg: '#f8f8f2',
      editorKeyword: '#ff79c6',
      editorString: '#f1fa8c',
      editorComment: '#6272a4',
      editorNumber: '#bd93f9',
      editorPunctuation: '#f8f8f2',
      editorOperator: '#ff79c6',
      editorFunction: '#50fa7b',
      editorCaret: '#f8f8f2',
      editorSelection: 'rgba(189,147,249,0.25)',
      editorGlow: 'rgba(189,147,249,0.12)'
    }
  },
  {
    id: 'monokai',
    name: 'Monokai',
    theme: {
      ...SYNTHWAVE_THEME,
      amigaBg: '#272822',
      amigaSurface: '#3e3d32',
      amigaPanel: '#49483e',
      amigaText: '#f8f8f2',
      amigaTextDim: '#75715e',
      amigaAccent: '#66d9ef',
      amigaCopper: '#fd971f',
      bevelDark: '#1e1f1c',
      bevelLight: '#75715e',
      editorBg: '#272822',
      editorFg: '#f8f8f2',
      editorKeyword: '#f92672',
      editorString: '#e6db74',
      editorComment: '#75715e',
      editorNumber: '#ae81ff',
      editorPunctuation: '#f8f8f2',
      editorOperator: '#f92672',
      editorFunction: '#a6e22e',
      editorCaret: '#f8f8f2',
      editorSelection: 'rgba(102,217,239,0.25)',
      editorGlow: 'rgba(102,217,239,0.1)'
    }
  },
  {
    id: 'workbench',
    name: 'Workbench',
    theme: {
      ...SYNTHWAVE_THEME,
      amigaBg: '#0051a0',
      amigaSurface: '#2a6fc7',
      amigaPanel: '#3d7fd9',
      amigaText: '#ffffff',
      amigaTextDim: '#aaccff',
      amigaAccent: '#ffcc00',
      amigaCopper: '#ff8800',
      bevelDark: '#003366',
      bevelLight: '#66aaff',
      editorBg: '#003366',
      editorFg: '#aaccff',
      editorKeyword: '#ffcc00',
      editorString: '#88ff88',
      editorComment: '#66aa88',
      editorNumber: '#ffaa66',
      editorPunctuation: '#aaccff',
      editorOperator: '#ffcc00',
      editorFunction: '#88ddff',
      editorCaret: '#aaccff',
      editorSelection: 'rgba(255,204,0,0.25)',
      editorGlow: 'rgba(255,204,0,0.1)'
    }
  }
];

export interface HSV { h: number; s: number; v: number }

export function hexToHsv(hex: string): HSV {
  const m = hex.match(/^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i);
  if (!m) return { h: 0, s: 0, v: 0 };
  let r = parseInt(m[1], 16) / 255;
  let g = parseInt(m[2], 16) / 255;
  let b = parseInt(m[3], 16) / 255;
  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  const d = max - min;
  let h = 0;
  const s = max === 0 ? 0 : d / max;
  const v = max;
  if (d !== 0) {
    switch (max) {
      case r: h = (g - b) / d + (g < b ? 6 : 0); break;
      case g: h = (b - r) / d + 2; break;
      case b: h = (r - g) / d + 4; break;
    }
    h /= 6;
  }
  return { h: Math.round(h * 360), s: Math.round(s * 100), v: Math.round(v * 100) };
}

export function hsvToHex(h: number, s: number, v: number): string {
  h = (h % 360) / 60;
  s /= 100;
  v /= 100;
  const c = v * s;
  const x = c * (1 - Math.abs((h % 2) - 1));
  const m = v - c;
  let r = 0, g = 0, b = 0;
  if (h < 1) { r = c; g = x; }
  else if (h < 2) { r = x; g = c; }
  else if (h < 3) { g = c; b = x; }
  else if (h < 4) { g = x; b = c; }
  else if (h < 5) { r = x; b = c; }
  else { r = c; b = x; }
  const toHex = (n: number) => {
    const v = Math.round((n + m) * 255);
    const s = v.toString(16);
    return s.length === 1 ? '0' + s : s;
  };
  return '#' + toHex(r) + toHex(g) + toHex(b);
}

export function normalizeHex(hex: string): string {
  const m = hex.match(/^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i);
  if (!m) return '#000000';
  return '#' + m[1].toLowerCase() + m[2].toLowerCase() + m[3].toLowerCase();
}

export function mergeTheme(custom?: Partial<ThemeColors>): ThemeColors {
  return { ...DEFAULT_THEME, ...(custom || {}) };
}

let lastAppliedTheme: ThemeColors = DEFAULT_THEME;

export function getLastAppliedTheme(): ThemeColors {
  return lastAppliedTheme;
}

export function applyTheme(custom?: Partial<ThemeColors>): void {
  const t = mergeTheme(custom);
  lastAppliedTheme = t;
  const root = document.documentElement;
  root.style.setProperty('--amiga-bg', t.amigaBg);
  root.style.setProperty('--amiga-surface', t.amigaSurface);
  root.style.setProperty('--amiga-panel', t.amigaPanel);
  root.style.setProperty('--amiga-text', t.amigaText);
  root.style.setProperty('--amiga-text-dim', t.amigaTextDim);
  root.style.setProperty('--amiga-accent', t.amigaAccent);
  root.style.setProperty('--amiga-copper', t.amigaCopper);
  root.style.setProperty('--bevel-dark', t.bevelDark);
  root.style.setProperty('--bevel-light', t.bevelLight);
  root.style.setProperty('--crt-fg', t.amigaText);
  root.style.setProperty('--crt-dim', t.amigaTextDim);
  root.style.setProperty('--theme-editor-bg', t.editorBg);
  root.style.setProperty('--theme-editor-fg', t.editorFg);
  root.style.setProperty('--theme-editor-keyword', t.editorKeyword);
  root.style.setProperty('--theme-editor-string', t.editorString);
  root.style.setProperty('--theme-editor-comment', t.editorComment);
  root.style.setProperty('--theme-editor-number', t.editorNumber);
  root.style.setProperty('--theme-editor-punctuation', t.editorPunctuation);
  root.style.setProperty('--theme-editor-operator', t.editorOperator);
  root.style.setProperty('--theme-editor-function', t.editorFunction);
  root.style.setProperty('--theme-editor-caret', t.editorCaret);
  root.style.setProperty('--theme-editor-selection', t.editorSelection);
  root.style.setProperty('--theme-editor-glow', t.editorGlow);
}
