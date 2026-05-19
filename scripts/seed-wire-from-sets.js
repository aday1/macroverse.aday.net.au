#!/usr/bin/env node
/**
 * seed-wire-from-sets.js - Generate Resolume Wire patches from Macroverse VJ Sets.
 *
 * Reads macroverse.db, queries ISF shaders by VJ set, parses their ISF headers,
 * and generates .wire JSON patches with crossfaders, MIDI-ready Float In nodes,
 * and webcam/sampler Texture In nodes for image inputs.
 *
 * Usage:
 *   node seed-wire-from-sets.js                    # all sets
 *   node seed-wire-from-sets.js --set vj-cosmic    # single set
 *   node seed-wire-from-sets.js --dry-run           # preview only
 *   node seed-wire-from-sets.js --validate          # check generated files
 *   node seed-wire-from-sets.js --json-output       # structured JSON output
 *   node seed-wire-from-sets.js --auto-seed          # assign VJ sets first, then generate
 */

'use strict';

const { DatabaseSync } = require('node:sqlite');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

const WORKSPACE = path.resolve(__dirname, '..');
const DB_PATH = path.join(WORKSPACE, 'macroverse.db');
const OUTPUT_DIR = path.join(WORKSPACE, 'resolume');

// Wire node class UUIDs (from ISF-Test.wire + Wire Documentation/Nodes)
const CLASS_ISF        = '77697265-4576-4C11-899B-6F11F3275D36';
const CLASS_TEX_IN     = '77697265-B2A2-4C1C-8C4C-2915D78CC8E9';
const CLASS_TEX_OUT    = '77697265-BEEA-4D38-8EE5-0EBA4CBD0AEE';
const CLASS_CROSSFADER = '77697265-7BAA-481B-8F7C-A32F6DBE1518';
const CLASS_FLOAT_IN   = '77697265-D235-4A6A-B661-02ABE55C72FF';
const CLASS_COMMENT    = '77697265-3C1D-467D-A722-0FC566538374';

// MIDI nodes
const CLASS_MIDI_IN    = '77697265-5275-4BB0-97B0-DBEEFD85997B';
const CLASS_FILTER_CC  = '77697265-2F2C-47A5-8DA4-065DAE816AB2';
const CLASS_FILTER_NOTE_ON = '77697265-A80A-433D-A399-D4C4C5B43D0D';

// Audio / FFT
const CLASS_SPECTRUM_IN = '77697265-0B51-496E-A02A-4269686CB551';

// Bool / Trigger
const CLASS_BOOL_IN    = '77697265-999C-4F8B-8B9D-3646DC68AA69';

// Video FX / Processing (from Resolume Wire Examples catalog)
const CLASS_VIDEO_MIXER  = '77697265-A270-4D60-911C-A88B1BE6369A';
const CLASS_TRANSFORM    = '77697265-9225-4009-9D2D-5F898E94CC33';
const CLASS_DELAY        = '77697265-4BEE-483F-9F28-1BA5F21193DB';
const CLASS_BLOOM        = '77697265-B994-4A9A-94E7-B4E0FA77D8AF';
const CLASS_BLUR         = '77697265-5F11-465D-9E83-A3CF5A50D8E8';
const CLASS_COLOR_OFFSET = '77697265-8B97-4252-9A93-BF8028C5EDEC';
const CLASS_HUE_ROTATE   = '77697265-11DE-4F22-B268-C050B2C2BB30';
const CLASS_UV_OFFSET    = '77697265-BB9A-4B60-83F3-D0E4DA6CFF86';
const CLASS_PIXELATE     = '77697265-ACD2-4541-B139-88756DC29CA7';
const CLASS_EDGE_DETECT  = '77697265-9DF3-4B94-A36D-42F1C6FBE997';
const CLASS_VIGNETTE     = '77697265-CE82-4714-8023-1953D9FAB7A9';
const CLASS_REPEAT       = '77697265-2F93-4176-B1F9-774B39EC25F5';
const CLASS_RIPPLE       = '77697265-D65D-4BFE-AAD6-882493B313FD';
const CLASS_THRESHOLD    = '77697265-BC76-4025-8195-07B2AA065E67';
const CLASS_COLORIZE     = '77697265-9518-4E69-899B-66B8C527C6EB';
const CLASS_STATIC_GEN   = '77697265-53CE-4FDD-BFCE-5F8269AA39DB';
const CLASS_SOLID_COLOR  = '77697265-E8EC-4F1B-901A-CFFC104D3B07';
const CLASS_FRAC_NOISE   = '77697265-E404-464F-A4C8-FF262AB15588';
const CLASS_GRADIENT     = '77697265-FCE2-4EBE-8C81-99C578524A24';
const CLASS_TRANSITION   = '77697265-7BAB-426B-1F7C-A32F6FBE1518';

// Oscillators / Time
const CLASS_SINE         = '77697265-ED9B-4BEC-B3D4-43AC04731CBA';
const CLASS_SAW          = '77697265-F95F-41D8-8FC4-DF0DC56E1051';
const CLASS_LINEAR       = '77697265-A48B-4AF9-8975-E3063B037D4C';
const CLASS_METRONOME    = '77697265-FD65-470F-9F75-ED878267980E';
const CLASS_RANDOM       = '77697265-5581-4721-8519-0b8fa3d68b4d';
const CLASS_PERLIN       = '77697265-A2EF-44DA-9A81-BB0385C1948C';

// Math / Logic
const CLASS_MULTIPLY     = '77697265-A0D8-429A-A558-69BC58D0D425';
const CLASS_ADD          = '77697265-A9AF-4CB4-B10F-3968B36BB63B';
const CLASS_MAP          = '77697265-992D-4044-82DC-D2E0D30DF78E';
const CLASS_SMOOTH       = '77697265-86ce-4e85-a02d-34f915fca74e';

// Control / Routing
const CLASS_HUB          = '77697265-AEE3-4B9D-BBD5-6Ae4CCD5aB30';
const CLASS_SWITCH       = '77697265-6899-4A9C-82AB-949346033440';

// ---------------------------------------------------------------------------
// Akai APC40 MK II MIDI Map
// ---------------------------------------------------------------------------
const APC40 = {
  // Crossfader: CC 15, Channel 0
  crossfader: { cc: 15, ch: 0 },
  // Master fader: CC 14, Channel 0
  masterFader: { cc: 14, ch: 0 },
  // Track faders: CC 7 on channels 0-7
  trackFaders: [0, 1, 2, 3, 4, 5, 6, 7].map(ch => ({ cc: 7, ch })),
  // Device knobs: CC 16-23, Channel 0
  knobs: [16, 17, 18, 19, 20, 21, 22, 23].map(cc => ({ cc, ch: 0 })),
  // Cue level: CC 47, Channel 0
  cueLevel: { cc: 47, ch: 0 },
  // Clip grid: Notes 0-4 on channels 0-7 (5 scenes x 8 tracks)
  clipGrid: (() => {
    const grid = [];
    for (let scene = 0; scene < 5; scene++)
      for (let track = 0; track < 8; track++)
        grid.push({ note: scene, ch: track, label: `Clip S${scene + 1}T${track + 1}` });
    return grid;
  })(),
  // Scene launch: Notes 82-86, Channel 0
  sceneLaunch: [82, 83, 84, 85, 86].map((n, i) => ({ note: n, ch: 0, label: `Scene ${i + 1}` })),
  // Track buttons
  clipStop:  [0, 1, 2, 3, 4, 5, 6, 7].map(ch => ({ note: 52, ch })),
  solo:      [0, 1, 2, 3, 4, 5, 6, 7].map(ch => ({ note: 49, ch })),
  mute:      [0, 1, 2, 3, 4, 5, 6, 7].map(ch => ({ note: 50, ch })),
  trackSelect: [0, 1, 2, 3, 4, 5, 6, 7].map(ch => ({ note: 51, ch })),
  // Transport
  play:   { note: 91, ch: 0 },
  stop:   { note: 89, ch: 0 },
  record: { note: 93, ch: 0 },
};

const VJ_SETS = [
  'vj-ambient', 'vj-techno', 'vj-cosmic', 'vj-glitch',
  'vj-geometric', 'vj-organic', 'vj-wire-ready', 'vj-dark', 'vj-colour',
];

// ---------------------------------------------------------------------------
// Roli Blocks MIDI Map (Lightpad Block / Seaboard Block)
// ---------------------------------------------------------------------------
// Roli Blocks use MPE (MIDI Polyphonic Expression):
//   - Note On/Off on channels 2-16 (Ch 1 is global)
//   - CC 74 (Slide / Y-axis) per-note channel
//   - Channel Pressure (Aftertouch) for Z-axis pressure
//   - Pitch Bend for X-axis glide
// In "Single Channel" mode they send on Ch 1 with standard CCs.
// We map the most useful controls for VJ use:
const ROLI = {
  // Global channel for single-channel mode
  globalCh: 0,
  // Slide (Y-axis) = CC 74 on global channel
  slideY: { cc: 74, ch: 0, label: 'Slide Y' },
  // Pitch bend mapped as CC 1 (mod wheel) in some configs
  modWheel: { cc: 1, ch: 0, label: 'Mod Wheel / X Glide' },
  // Expression = CC 11
  expression: { cc: 11, ch: 0, label: 'Expression' },
  // Brightness (aftertouch proxy) = CC 74 on per-note channels
  // For VJ use, we map 4 pad zones as notes
  padNotes: [
    { note: 60, ch: 0, label: 'Pad 1 (C4)' },
    { note: 62, ch: 0, label: 'Pad 2 (D4)' },
    { note: 64, ch: 0, label: 'Pad 3 (E4)' },
    { note: 67, ch: 0, label: 'Pad 4 (G4)' },
  ],
  // XY pad mapped as CC 16/17 in Dashboard mode
  xyPad: { ccX: 16, ccY: 17, ch: 0, label: 'XY Pad' },
};

// ISF built-in params to skip when making per-shader Float Ins
const BUILTIN_SKIP = new Set([
  'useFrameIndex', 'fps', 'time', 'RENDERSIZE', 'PASSINDEX', 'FRAMEINDEX',
]);

// Built-in params we expose as shared Float In nodes
const SHARED_PARAMS = [
  { name: 'timeScale', min: 0.1, max: 4.0, default: 1.0, label: 'Time Scale' },
  { name: 'mouseX',    min: 0.0, max: 1.0, default: 0.5, label: 'Mouse X' },
  { name: 'mouseY',    min: 0.0, max: 1.0, default: 0.5, label: 'Mouse Y' },
];
const SHARED_PARAM_NAMES = new Set(SHARED_PARAMS.map(p => p.name));

const MAX_CUSTOM_PARAMS = 4;
const SHADERS_PER_PATCH = 4;

// Layout constants (pixel positions in Wire canvas)
const L = {
  colMidi:     -1300,   // MIDI controller nodes (far left)
  colShared:    -900,
  colParams:    -650,
  colTexIn:     -450,
  colIsf:       -150,
  colMixCtrl:    250,
  colCrossfade:  500,
  colMaster:     800,
  colOut:       1100,
  colFft:       1350,   // FFT / spectrum nodes (far right)
  rowSpacing:    320,
  paramH:         82,
  paramGap:       12,
  nodeW:         195,
  nodeHIsf:      178,
  nodeHSmall:     82,
  nodeHCf:       106,
  nodeHMidi:     106,
  commentY:     -280,
};

// ---------------------------------------------------------------------------
// ISF Parser
// ---------------------------------------------------------------------------

const ISF_HEADER_RE = /\/\*\s*(\{[\s\S]*?\})\s*\*\//;

function parseISFHeader(filePath) {
  let text;
  try {
    text = fs.readFileSync(filePath, 'utf-8');
  } catch {
    return null;
  }
  const m = text.match(ISF_HEADER_RE);
  if (!m) return null;
  try {
    return JSON.parse(m[1]);
  } catch {
    return null;
  }
}

function getWireInputs(header) {
  const raw = header.INPUTS || header.inputs || [];
  return raw.map(inp => {
    const name = inp.NAME || inp.name || '';
    let type = (inp.TYPE || inp.type || 'float').toLowerCase();
    const entry = { name, type };
    if (inp.LABEL || inp.label) entry.label = inp.LABEL || inp.label;
    if (type === 'float') {
      entry.defaultValue = inp.DEFAULT ?? inp.defaultValue ?? inp.default ?? 0.5;
      entry.min = inp.MIN ?? inp.min ?? 0.0;
      entry.max = inp.MAX ?? inp.max ?? 1.0;
    } else if (type === 'bool') {
      entry.defaultValue = !!(inp.DEFAULT ?? inp.defaultValue ?? inp.default ?? false);
    } else if (type === 'image' || type === 'sampler2d') {
      entry.type = 'image';
    }
    return entry;
  });
}

function getFloatParams(wireInputs) {
  const out = [];
  for (const inp of wireInputs) {
    if (inp.type !== 'float') continue;
    if (BUILTIN_SKIP.has(inp.name) || SHARED_PARAM_NAMES.has(inp.name)) continue;
    out.push(inp);
    if (out.length >= MAX_CUSTOM_PARAMS) break;
  }
  return out;
}

function getImageInputs(wireInputs) {
  return wireInputs.filter(i => i.type === 'image').map(i => i.name);
}

// ---------------------------------------------------------------------------
// Database Query
// ---------------------------------------------------------------------------

function openDB(dbPath, readOnly = true) {
  if (!fs.existsSync(dbPath)) {
    throw new Error(`Database not found: ${dbPath}`);
  }
  return new DatabaseSync(dbPath, { readOnly });
}

function shadersForSet(db, setName) {
  const stmt = db.prepare(`
    SELECT s.id, s.path, s.name, s.category, s.sets, s.uniforms,
           s.fixed_name, s.format, s.param_ranges, s.source_root
    FROM shaders s, json_each(s.sets) je
    WHERE je.value = ? AND s.format = 'isf'
    ORDER BY s.name
  `);
  const rows = stmt.all(setName);
  return rows.map(row => {
    const d = { ...row };
    for (const col of ['sets', 'uniforms', 'param_ranges']) {
      if (d[col] && typeof d[col] === 'string') {
        try { d[col] = JSON.parse(d[col]); } catch { d[col] = []; }
      } else if (!d[col]) {
        d[col] = [];
      }
    }
    return d;
  });
}

function allSetsWithCounts(db) {
  const stmt = db.prepare(`
    SELECT je.value as set_name, COUNT(*) as cnt
    FROM shaders s, json_each(s.sets) je
    WHERE s.format = 'isf'
    GROUP BY je.value
    ORDER BY je.value
  `);
  const rows = stmt.all();
  const map = {};
  for (const r of rows) map[r.set_name] = r.cnt;
  return map;
}

/**
 * Auto-seed VJ sets using the same heuristic logic as the gallery's
 * "Seed VJ Sets" button (gallery.ts lines 612-636).
 * Assigns shaders to VJ sets based on name/category keyword matching.
 */
function autoSeedSets(dbPath, quiet = false) {
  const db = openDB(dbPath, false); // writable
  const allShaders = db.prepare(`
    SELECT id, name, fixed_name, category, format, sets
    FROM shaders WHERE format = 'isf'
  `).all();

  const update = db.prepare('UPDATE shaders SET sets = ? WHERE id = ?');
  let seeded = 0;

  for (const shader of allShaders) {
    let sets;
    try {
      sets = shader.sets && shader.sets !== 'null' ? JSON.parse(shader.sets) : [];
    } catch { sets = []; }
    if (!Array.isArray(sets)) sets = [];

    // Skip if already in a preset VJ set
    if (VJ_SETS.some(ps => sets.includes(ps))) continue;

    const origLen = sets.length;

    // ISF format -> vj-wire-ready
    if (!sets.includes('vj-wire-ready')) sets.push('vj-wire-ready');

    const nm = ((shader.fixed_name || shader.name || '') + ' ' + (shader.category || '')).toLowerCase();

    if (/plasma|cloud|smoke|fog|fluid|noise|ambient|drift|flow/.test(nm) && !sets.includes('vj-ambient'))
      sets.push('vj-ambient');
    if (/glitch|corrupt|pixel|databend|scan|error|digital|hack/.test(nm) && !sets.includes('vj-glitch'))
      sets.push('vj-glitch');
    if (/star|nebula|void|space|cosmic|galaxy|orbit|planet/.test(nm) && !sets.includes('vj-cosmic'))
      sets.push('vj-cosmic');
    if (/grid|hex|tria|cube|box|sphere|geomet|kaleid|pattern|tile/.test(nm) && !sets.includes('vj-geometric'))
      sets.push('vj-geometric');
    if (/organic|bio|cell|grow|vine|wave|morph/.test(nm) && !sets.includes('vj-organic'))
      sets.push('vj-organic');
    if (/dark|void|shadow|night|deep|black/.test(nm) && !sets.includes('vj-dark'))
      sets.push('vj-dark');
    if (/color|colour|rgb|hue|rainbow|palette|chroma/.test(nm) && !sets.includes('vj-colour'))
      sets.push('vj-colour');
    if (/techno|beat|pulse|strobe|flash|rhythm|bass|kick/.test(nm) && !sets.includes('vj-techno'))
      sets.push('vj-techno');

    if (sets.length !== origLen) {
      update.run(JSON.stringify(sets), shader.id);
      seeded++;
    }
  }

  db.close();
  if (!quiet) console.log(`Auto-seeded VJ sets: ${seeded} shader(s) assigned`);
  return seeded;
}

// ---------------------------------------------------------------------------
// Wire Node Factory
// ---------------------------------------------------------------------------

class WireNodeFactory {
  constructor(startId = 0) {
    this._nextId = startId;
  }
  get nextNodeId() { return this._nextId; }
  _alloc() { return this._nextId++; }

  makeISFNode(shaderPath, wireInputs, bounds, name = 'ISF') {
    const nid = this._alloc();
    const constants = {
      bypass: { type: 'bool', value: false },
      time: { type: 'float', value: 0 },
    };
    for (const inp of wireInputs) {
      if (inp.type === 'float') {
        constants[inp.name] = { type: 'float', value: inp.defaultValue ?? 0.5 };
      } else if (inp.type === 'bool') {
        constants[inp.name] = { type: 'bool', value: inp.defaultValue ?? false };
      }
    }
    const normPath = shaderPath.replace(/\\/g, '/');
    const node = {
      attributes: {
        bitdepth: { type: 'integer', value: 0 },
        'fragment-shader': {
          type: 'resourceIsf',
          value: {
            path: { main: normPath, sub: '' },
            value: { inputs: wireInputs },
          },
        },
        instances: { type: 'integer', value: 1 },
        'resolution-absolute': { type: 'float2', value: [1920, 1080] },
        'resolution-mode': { type: 'integer', value: 0 },
        'resolution-relative': { type: 'float2', value: [1, 1] },
      },
      bounds,
      class: { id: CLASS_ISF, version: 4 },
      clock: 'video',
      color: 'ffff6a00',
      constants,
      hidden: [
        'bitdepth', 'bypass', 'instances',
        'resolution-absolute', 'resolution-mode', 'resolution-relative',
      ],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeFloatInNode(name, value, minVal, maxVal, bounds) {
    const nid = this._alloc();
    const node = {
      attributes: {
        flow: { type: 'flow', value: 'signal' },
        'has-max': { type: 'bool', value: true },
        'has-min': { type: 'bool', value: true },
        instances: { type: 'integer', value: 1 },
        max: { type: 'float', value: maxVal },
        min: { type: 'float', value: minVal },
        'options-count': { type: 'integer', value: 0 },
        unit: { type: 'integer', value: 0 },
        widget: { type: 'integer', value: 0 },
      },
      bounds,
      class: { id: CLASS_FLOAT_IN, version: 4 },
      clock: 'video',
      color: 'ffff6a00',
      constants: { input: { type: 'float', value } },
      hidden: [
        'flow', 'has-max', 'has-min', 'input', 'instances',
        'max', 'min', 'options-count', 'widget',
      ],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeTextureInNode(bounds, name = 'Texture In') {
    const nid = this._alloc();
    const node = {
      attributes: {
        flow: { type: 'flow', value: 'signal' },
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_TEX_IN, version: 1 },
      clock: 'video',
      color: 'ffff6a00',
      constants: { input: { type: 'texture2d', value: null } },
      hidden: ['flow', 'instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeTextureOutNode(bounds, name = 'Texture Out') {
    const nid = this._alloc();
    const node = {
      attributes: { instances: { type: 'integer', value: 1 } },
      bounds,
      class: { id: CLASS_TEX_OUT, version: 1 },
      clock: 'video',
      color: 'ffff6a00',
      constants: { input: { type: 'texture2d', value: null } },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeCrossfaderNode(bounds, name = 'Cross Fader') {
    const nid = this._alloc();
    const node = {
      attributes: {
        'input1-dimensions': { type: 'integer', value: 1 },
        'input1-type': { type: 'type', value: 'texture2d' },
        'input2-dimensions': { type: 'integer', value: 1 },
        'input2-type': { type: 'type', value: 'texture2d' },
        'mix-dimensions': { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_CROSSFADER, version: 2 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        input1: { type: 'texture2d', value: null },
        input2: { type: 'texture2d', value: null },
        mix: { type: 'float', value: 0.5 },
      },
      hidden: [
        'input1-dimensions', 'input1-type',
        'input2-dimensions', 'input2-type',
        'mix-dimensions',
      ],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeCommentNode(text, bounds) {
    const nid = this._alloc();
    const node = {
      attributes: {
        alignment: { type: 'integer', value: 0 },
        fill: { type: 'bool', value: false },
        'font-size': { type: 'float', value: 16 },
        text: { type: 'string', value: text },
      },
      bounds,
      class: { id: CLASS_COMMENT, version: 1 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {},
      hidden: ['alignment', 'fill', 'font-size', 'text'],
      name: 'Comment',
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeBoolInNode(name, value, bounds) {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_BOOL_IN, version: 1 },
      clock: 'video',
      color: 'ff00c8ff',
      constants: { input: { type: 'bool', value } },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeMidiInNode(bounds, name = 'MIDI In') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_MIDI_IN, version: 1 },
      clock: 'video',
      color: 'ff00c8ff',
      constants: {},
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeFilterCCNode(channel, controller, bounds, name = 'Filter CC') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_FILTER_CC, version: 1 },
      clock: 'video',
      color: 'ff00c8ff',
      constants: {
        channel: { type: 'integer', value: channel },
        controller: { type: 'integer', value: controller },
        input: { type: 'midi', value: null },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeFilterNoteOnNode(channel, pitch, bounds, name = 'Filter Note On') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_FILTER_NOTE_ON, version: 1 },
      clock: 'video',
      color: 'ff00c8ff',
      constants: {
        channel: { type: 'integer', value: channel },
        input: { type: 'midi', value: null },
        pitch: { type: 'integer', value: pitch },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeSpectrumInNode(bounds, name = 'Spectrum In') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_SPECTRUM_IN, version: 1 },
      clock: 'audio',
      color: 'ff00c8ff',
      constants: {},
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  // =========================================================================
  // Enhanced FX / Generator / Mixer Factory Methods
  // =========================================================================

  makeVideoMixerNode(bounds, name = 'Video Mixer') {
    const nid = this._alloc();
    const node = {
      attributes: {
        bitdepth: { type: 'integer', value: 0 },
        'input-count': { type: 'integer', value: 2 },
        instances: { type: 'integer', value: 1 },
        'resolution-absolute': { type: 'float2', value: [1920, 1080] },
        'resolution-mode': { type: 'integer', value: 0 },
        'resolution-relative': { type: 'float2', value: [1, 1] },
      },
      bounds,
      class: { id: CLASS_VIDEO_MIXER, version: 3 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        bypass: { type: 'bool', value: false },
        input1: { type: 'texture2d', value: null },
        input2: { type: 'texture2d', value: null },
        mode: { type: 'integer', value: 11 },
        opacity1: { type: 'float', value: 1.0 },
        opacity2: { type: 'float', value: 1.0 },
      },
      hidden: [
        'bitdepth', 'bypass', 'input-count', 'instances', 'mode',
        'resolution-absolute', 'resolution-mode', 'resolution-relative',
      ],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeDelayNode(frames, bounds, name = 'Delay') {
    const nid = this._alloc();
    const node = {
      attributes: {
        'buffer-mode': { type: 'integer', value: 0 },
        capacity: { type: 'integer', value: 120 },
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_DELAY, version: 5 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        delay: { type: 'float', value: frames },
        input: { type: 'texture2d', value: null },
      },
      hidden: ['buffer-mode', 'capacity', 'instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeTransformNode(bounds, name = 'Transform') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_TRANSFORM, version: 3 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        anchor: { type: 'float2', value: [0.5, 0.5] },
        input: { type: 'texture2d', value: null },
        position: { type: 'float2', value: [0.5, 0.5] },
        rotation: { type: 'float', value: 0 },
        scale: { type: 'float2', value: [1, 1] },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeBloomNode(bounds, name = 'Bloom') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_BLOOM, version: 2 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        intensity: { type: 'float', value: 1.0 },
        input: { type: 'texture2d', value: null },
        threshold: { type: 'float', value: 0.5 },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeBlurNode(bounds, name = 'Blur') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_BLUR, version: 2 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        amount: { type: 'float', value: 0.3 },
        input: { type: 'texture2d', value: null },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeColorOffsetNode(bounds, name = 'Color Offset') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_COLOR_OFFSET, version: 2 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        amount: { type: 'float', value: 0.02 },
        angle: { type: 'float', value: 0 },
        input: { type: 'texture2d', value: null },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeHueRotateNode(bounds, name = 'Hue Rotate') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_HUE_ROTATE, version: 2 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        rotation: { type: 'float', value: 0 },
        input: { type: 'texture2d', value: null },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeUVOffsetNode(bounds, name = 'UV Offset') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_UV_OFFSET, version: 3 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        'offset-x': { type: 'float', value: 0.1 },
        'offset-y': { type: 'float', value: 0 },
        input: { type: 'texture2d', value: null },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makePixelateNode(bounds, name = 'Pixelate') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_PIXELATE, version: 1 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        size: { type: 'float', value: 0.1 },
        input: { type: 'texture2d', value: null },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeEdgeDetectNode(bounds, name = 'Edge Detect') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_EDGE_DETECT, version: 1 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        input: { type: 'texture2d', value: null },
        strength: { type: 'float', value: 1.0 },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeVignetteNode(bounds, name = 'Vignette') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_VIGNETTE, version: 1 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        amount: { type: 'float', value: 0.5 },
        input: { type: 'texture2d', value: null },
        softness: { type: 'float', value: 0.5 },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeRepeatNode(bounds, name = 'Repeat') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_REPEAT, version: 1 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        'repeat-x': { type: 'float', value: 2 },
        input: { type: 'texture2d', value: null },
        'repeat-y': { type: 'float', value: 2 },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeThresholdNode(bounds, name = 'Threshold') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_THRESHOLD, version: 1 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        input: { type: 'texture2d', value: null },
        threshold: { type: 'float', value: 0.5 },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeStaticNode(bounds, name = 'Static') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_STATIC_GEN, version: 1 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        amount: { type: 'float', value: 0.5 },
        input: { type: 'texture2d', value: null },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeSolidColorNode(r, g, b, a, bounds, name = 'Solid Color') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_SOLID_COLOR, version: 1 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        color: { type: 'float4', value: [r, g, b, a] },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  // Oscillator / Time nodes

  makeSineNode(frequency, bounds, name = 'Sine') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_SINE, version: 1 },
      clock: 'video',
      color: 'ff00c8ff',
      constants: {
        frequency: { type: 'float', value: frequency },
        phase: { type: 'float', value: 0 },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeSawNode(frequency, bounds, name = 'Saw') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_SAW, version: 1 },
      clock: 'video',
      color: 'ff00c8ff',
      constants: {
        frequency: { type: 'float', value: frequency },
        phase: { type: 'float', value: 0 },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeMetronomeNode(bpm, bounds, name = 'Metronome') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_METRONOME, version: 1 },
      clock: 'video',
      color: 'ff00c8ff',
      constants: {
        bpm: { type: 'float', value: bpm },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeRandomNode(bounds, name = 'Random') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_RANDOM, version: 2 },
      clock: 'video',
      color: 'ff00c8ff',
      constants: {
        trigger: { type: 'bool', value: false },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makePerlinNode(bounds, name = 'Perlin Noise') {
    const nid = this._alloc();
    const node = {
      attributes: {
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_PERLIN, version: 1 },
      clock: 'video',
      color: 'ff00c8ff',
      constants: {
        speed: { type: 'float', value: 1.0 },
      },
      hidden: ['instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  // Math nodes

  makeMultiplyNode(bounds, name = 'Multiply') {
    const nid = this._alloc();
    const node = {
      attributes: {
        'input1-type': { type: 'type', value: 'float' },
        'input2-type': { type: 'type', value: 'float' },
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_MULTIPLY, version: 1 },
      clock: 'video',
      color: 'ff00c8ff',
      constants: {
        input1: { type: 'float', value: 1.0 },
        input2: { type: 'float', value: 1.0 },
      },
      hidden: [
        'input1-type',
        'input2-type', 'instances',
      ],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeAddNode(bounds, name = 'Add') {
    const nid = this._alloc();
    const node = {
      attributes: {
        'input1-type': { type: 'type', value: 'float' },
        'input2-type': { type: 'type', value: 'float' },
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_ADD, version: 1 },
      clock: 'video',
      color: 'ff00c8ff',
      constants: {
        input1: { type: 'float', value: 0 },
        input2: { type: 'float', value: 0 },
      },
      hidden: [
        'input1-type',
        'input2-type', 'instances',
      ],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeMapNode(bounds, name = 'Map') {
    const nid = this._alloc();
    const node = {
      attributes: {
        'input-type': { type: 'type', value: 'float' },
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_MAP, version: 1 },
      clock: 'video',
      color: 'ff00c8ff',
      constants: {
        'from-max': { type: 'float', value: 1.0 },
        'from-min': { type: 'float', value: 0.0 },
        input: { type: 'float', value: 0.5 },
        'to-max': { type: 'float', value: 1.0 },
        'to-min': { type: 'float', value: 0.0 },
      },
      hidden: ['input-type', 'instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  makeSmoothNode(bounds, name = 'Smooth') {
    const nid = this._alloc();
    const node = {
      attributes: {
        'input-type': { type: 'type', value: 'float' },
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_SMOOTH, version: 1 },
      clock: 'video',
      color: 'ff00c8ff',
      constants: {
        input: { type: 'float', value: 0 },
        smooth: { type: 'float', value: 0.9 },
      },
      hidden: ['input-type', 'instances'],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }

  // Transition node (blend between textures with transition effect)
  makeTransitionNode(bounds, name = 'Transition') {
    const nid = this._alloc();
    const node = {
      attributes: {
        'input1-type': { type: 'type', value: 'texture2d' },
        'input2-type': { type: 'type', value: 'texture2d' },
        instances: { type: 'integer', value: 1 },
      },
      bounds,
      class: { id: CLASS_TRANSITION, version: 1 },
      clock: 'video',
      color: 'ffff6a00',
      constants: {
        input1: { type: 'texture2d', value: null },
        input2: { type: 'texture2d', value: null },
        mix: { type: 'float', value: 0.5 },
      },
      hidden: [
        'input1-type',
        'input2-type',
        'instances',
      ],
      name,
      thumbnail_visible: true,
    };
    return [nid, node];
  }
}

// ---------------------------------------------------------------------------
// Wire Patch Builder
// ---------------------------------------------------------------------------

class WirePatchBuilder {
  constructor(displayName, description = '', category = 'mixer') {
    this.factory = new WireNodeFactory(0);
    this.nodes = {};
    this.connections = [];
    this.inputOrderRoots = [];
    this.deviceConnections = {};
    this.displayName = displayName;
    this.description = description;
    this.category = category; // 'source', 'effect', or 'mixer'
  }

  addNode(nodeId, nodeDict) {
    this.nodes[String(nodeId)] = nodeDict;
  }

  connect(fromId, fromPort, toId, toPort) {
    this.connections.push({ from: [fromId, fromPort], to: [toId, toPort] });
  }

  setDevice(nodeId, deviceName) {
    this.deviceConnections[String(nodeId)] = deviceName;
  }

  exposeInput(nodeId) {
    this.inputOrderRoots.push({ node: nodeId });
  }

  build() {
    return {
      formatVersion: { major: 1, minor: 1, patch: 0 },
      patch: {
        connections: this.connections,
        inputOrder: { groups: {}, roots: this.inputOrderRoots },
        meta: {
          author: 'Macroverse',
          category: this.category,
          deploymentTarget: {
            branch: 'unknown', name: 'Wire',
            version: { major: 7, minor: 23, patch: 0 },
          },
          description: this.description,
          displayName: this.displayName,
          identifier: crypto.randomUUID(),
          licenseName: '', mail: '',
          note: { text: '', textColorIndex: 1, textSizeMultiplier: 3 },
          originalTarget: {
            branch: 'unknown', name: 'Wire',
            version: { major: 7, minor: 23, patch: 0 },
          },
          quality: 32856,
          resolution: { height: 1080, width: 1920 },
          saveTarget: {
            branch: '', name: 'Wire',
            version: { major: 7, minor: 24, patch: 3 },
          },
          thumbnail: '', url: 'aday@aday.net.au', vendor: 'aday.net.au',
        },
        nextNodeId: this.factory.nextNodeId,
        nodes: this.nodes,
        ui: { camera: { x: -400, y: 0, zoom: 0.75 }, selection: [] },
      },
      ui: {
        audio: { routing: { in: {}, out: [] } },
        deviceConnections: { input: this.deviceConnections },
        transport: { bpm: 120, 'time-signature': [4, 4] },
        video: {
          routing: {
            out: {
              'Display 1': null, 'Display 2': null,
              'Display 3': null, 'Display 4': null,
            },
          },
        },
      },
    };
  }

  save(filePath) {
    const dir = path.dirname(filePath);
    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
    fs.writeFileSync(filePath, JSON.stringify(this.build(), null, '\t'), 'utf-8');
  }
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function bounds(x, y, w = 195, h = 82) {
  return { x, y, width: w, height: h };
}

function resolveShaderPath(shader) {
  let rawPath = shader.path || '';
  rawPath = rawPath.replace(/\|/g, '\\');
  if (path.isAbsolute(rawPath) && fs.existsSync(rawPath)) return rawPath;
  const rel = path.join(WORKSPACE, rawPath);
  if (fs.existsSync(rel)) return rel;
  const root = (shader.source_root || '').replace(/\|/g, '\\');
  if (root) {
    const candidate = path.join(root, rawPath);
    if (fs.existsSync(candidate)) return candidate;
  }
  return rawPath; // Return as-is; Wire will show error if missing
}

// ---------------------------------------------------------------------------
// Patch Generation
// ---------------------------------------------------------------------------

function generatePatchForGroup(shaders, setName, patchName, patchIndex = 0, features = null) {
  if (!features) features = { fft: true, webcam: true, glitch: true, midi: true, fxLevel: 'advanced' };
  const n = shaders.length;
  if (n < 1 || n > SHADERS_PER_PATCH) throw new Error(`Invalid group size: ${n}`);

  const builder = new WirePatchBuilder(
    patchName,
    `Macroverse VJ Set: ${setName}\n` +
    shaders.map((s, i) => `  ${i + 1}. ${s.fixed_name || s.name || '?'}`).join('\n') +
    `\nMIDI: APC40 MK II mapped | FFT: Spectrum In`,
  );
  const factory = builder.factory;

  // -- Comment node --
  const shaderNames = shaders.map(s => s.fixed_name || s.name || '?');
  const commentText =
    `MACROVERSE SET: ${setName.toUpperCase()}\n\n` +
    shaderNames.map((name, i) => `  [${String.fromCharCode(65 + i)}] ${name}`).join('\n') +
    `\n\nMIDI: APC40 MK II\n` +
    `  Crossfader: CC15 Ch0 -> Master Mix\n` +
    `  Track Faders: CC7 Ch0-${n - 1} -> Shader Opacity\n` +
    `  Knobs: CC16-23 Ch0 -> Shader Params\n` +
    `  Clip Grid: Notes 0-4 Ch0-${n - 1} -> Shader Toggle\n\n` +
    `FFT: Spectrum In -> timeScale modulation\n\n` +
    `Generated by seed-wire-from-sets.js`;
  const [cid, cnode] = factory.makeCommentNode(
    commentText, bounds(L.colIsf - 50, L.commentY, 1800, 260),
  );
  builder.addNode(cid, cnode);

  // -- Parse ISF headers and build ISF nodes --
  const isfNodeIds = [];
  const allWireInputs = [];
  const deviceNames = ['Webcam', 'Video Sampler', 'Test Card', 'Wire Logo'];
  let deviceIdx = 0;

  for (let slot = 0; slot < n; slot++) {
    const shader = shaders[slot];
    const shaderPath = resolveShaderPath(shader);
    const header = parseISFHeader(shaderPath);
    const wireInputs = header ? getWireInputs(header) : [];
    allWireInputs.push(wireInputs);

    // Image inputs -> Texture In nodes
    const imageNames = getImageInputs(wireInputs);

    const slotY = slot * L.rowSpacing;
    const shaderName = shader.fixed_name || shader.name || 'ISF';
    const [isfId, isfNode] = factory.makeISFNode(
      shaderPath, wireInputs,
      bounds(L.colIsf, slotY, L.nodeW, L.nodeHIsf),
      shaderName,
    );
    builder.addNode(isfId, isfNode);
    isfNodeIds.push(isfId);

    if (features.webcam) {
      for (let ii = 0; ii < imageNames.length; ii++) {
        const texY = slotY + (ii + 1) * (L.paramH + L.paramGap);
        const [texId, texNode] = factory.makeTextureInNode(
          bounds(L.colTexIn, texY, 130, L.nodeHSmall),
          `Input (${shaderName})`,
        );
        builder.addNode(texId, texNode);
        builder.connect(texId, 'output', isfId, imageNames[ii]);
        if (deviceIdx < deviceNames.length) {
          builder.setDevice(texId, deviceNames[deviceIdx]);
        }
        deviceIdx++;
      }
    }
  }

  // -- Shared Float In nodes (timeScale, mouseX, mouseY) --
  for (let si = 0; si < SHARED_PARAMS.length; si++) {
    const sp = SHARED_PARAMS[si];
    const spY = si * (L.paramH + L.paramGap);
    const [fid, fnode] = factory.makeFloatInNode(
      sp.label || sp.name, sp.default, sp.min, sp.max,
      bounds(L.colShared, spY, 130, L.nodeHSmall),
    );
    builder.addNode(fid, fnode);
    builder.exposeInput(fid);
    // Connect to all ISF nodes that have this param
    for (let i = 0; i < isfNodeIds.length; i++) {
      if (allWireInputs[i].some(inp => inp.name === sp.name)) {
        builder.connect(fid, 'output', isfNodeIds[i], sp.name);
      }
    }
  }

  // -- Per-shader custom Float In nodes --
  for (let slot = 0; slot < n; slot++) {
    const customParams = getFloatParams(allWireInputs[slot]);
    const shaderLabel = shaderNames[slot];
    for (let pi = 0; pi < customParams.length; pi++) {
      const param = customParams[pi];
      const baseY = slot * L.rowSpacing;
      const paramY = baseY + pi * (L.paramH + L.paramGap);
      const [fid, fnode] = factory.makeFloatInNode(
        `${shaderLabel}: ${param.label || param.name}`,
        param.defaultValue ?? 0.5,
        param.min ?? 0.0,
        param.max ?? 1.0,
        bounds(L.colParams, paramY, 150, L.nodeHSmall),
      );
      builder.addNode(fid, fnode);
      builder.exposeInput(fid);
      builder.connect(fid, 'output', isfNodeIds[slot], param.name);
    }
  }

  // -- Texture Out --
  const outY = ((n - 1) * L.rowSpacing) / 2;
  const [outId, outNode] = factory.makeTextureOutNode(
    bounds(L.colOut, outY, 132, L.nodeHSmall),
  );
  builder.addNode(outId, outNode);

  // -- Crossfader topology --
  if (n === 1) {
    builder.connect(isfNodeIds[0], 'output', outId, 'input');
  } else if (n === 2) {
    const cfY = L.rowSpacing / 2;
    const [cfId, cfNode] = factory.makeCrossfaderNode(
      bounds(L.colCrossfade, cfY, L.nodeW, L.nodeHCf), 'Mix A/B',
    );
    builder.addNode(cfId, cfNode);
    builder.connect(isfNodeIds[0], 'output', cfId, 'input1');
    builder.connect(isfNodeIds[1], 'output', cfId, 'input2');
    builder.connect(cfId, 'output0', outId, 'input');

    const [mixId, mixNode] = factory.makeFloatInNode(
      'Mix A/B', 0.5, 0.0, 1.0,
      bounds(L.colMixCtrl, cfY, 130, L.nodeHSmall),
    );
    builder.addNode(mixId, mixNode);
    builder.exposeInput(mixId);
    builder.connect(mixId, 'output', cfId, 'mix');
  } else if (n === 3) {
    const cfAbY = L.rowSpacing / 2;
    const [cfAbId, cfAbNode] = factory.makeCrossfaderNode(
      bounds(L.colCrossfade, cfAbY, L.nodeW, L.nodeHCf), 'Mix A/B',
    );
    builder.addNode(cfAbId, cfAbNode);
    builder.connect(isfNodeIds[0], 'output', cfAbId, 'input1');
    builder.connect(isfNodeIds[1], 'output', cfAbId, 'input2');

    const masterY = L.rowSpacing;
    const [masterId, masterNode] = factory.makeCrossfaderNode(
      bounds(L.colMaster, masterY, L.nodeW, L.nodeHCf), 'Master Mix',
    );
    builder.addNode(masterId, masterNode);
    builder.connect(cfAbId, 'output0', masterId, 'input1');
    builder.connect(isfNodeIds[2], 'output', masterId, 'input2');
    builder.connect(masterId, 'output0', outId, 'input');

    // Mix Float Ins
    const [mixAbId, mixAbNode] = factory.makeFloatInNode(
      'Mix A/B', 0.5, 0.0, 1.0,
      bounds(L.colMixCtrl, cfAbY, 130, L.nodeHSmall),
    );
    builder.addNode(mixAbId, mixAbNode);
    builder.exposeInput(mixAbId);
    builder.connect(mixAbId, 'output', cfAbId, 'mix');

    const [masterMixId, masterMixNode] = factory.makeFloatInNode(
      'Master Mix', 0.5, 0.0, 1.0,
      bounds(L.colMixCtrl, masterY, 130, L.nodeHSmall),
    );
    builder.addNode(masterMixId, masterMixNode);
    builder.exposeInput(masterMixId);
    builder.connect(masterMixId, 'output', masterId, 'mix');
  } else {
    // 4 shaders: A+B -> CF-AB, C+D -> CF-CD, master -> out
    const cfAbY = L.rowSpacing / 2;
    const cfCdY = L.rowSpacing * 2.5;
    const masterY = (cfAbY + cfCdY) / 2;

    const [cfAbId, cfAbNode] = factory.makeCrossfaderNode(
      bounds(L.colCrossfade, cfAbY, L.nodeW, L.nodeHCf), 'Mix A/B',
    );
    builder.addNode(cfAbId, cfAbNode);
    builder.connect(isfNodeIds[0], 'output', cfAbId, 'input1');
    builder.connect(isfNodeIds[1], 'output', cfAbId, 'input2');

    const [cfCdId, cfCdNode] = factory.makeCrossfaderNode(
      bounds(L.colCrossfade, cfCdY, L.nodeW, L.nodeHCf), 'Mix C/D',
    );
    builder.addNode(cfCdId, cfCdNode);
    builder.connect(isfNodeIds[2], 'output', cfCdId, 'input1');
    builder.connect(isfNodeIds[3], 'output', cfCdId, 'input2');

    const [masterId, masterNode] = factory.makeCrossfaderNode(
      bounds(L.colMaster, masterY, L.nodeW, L.nodeHCf), 'Master Mix',
    );
    builder.addNode(masterId, masterNode);
    builder.connect(cfAbId, 'output0', masterId, 'input1');
    builder.connect(cfCdId, 'output0', masterId, 'input2');
    builder.connect(masterId, 'output0', outId, 'input');

    // Mix Float Ins
    const [mixAbId, mixAbNode] = factory.makeFloatInNode(
      'Mix A/B', 0.5, 0.0, 1.0,
      bounds(L.colMixCtrl, cfAbY, 130, L.nodeHSmall),
    );
    builder.addNode(mixAbId, mixAbNode);
    builder.exposeInput(mixAbId);
    builder.connect(mixAbId, 'output', cfAbId, 'mix');

    const [mixCdId, mixCdNode] = factory.makeFloatInNode(
      'Mix C/D', 0.5, 0.0, 1.0,
      bounds(L.colMixCtrl, cfCdY, 130, L.nodeHSmall),
    );
    builder.addNode(mixCdId, mixCdNode);
    builder.exposeInput(mixCdId);
    builder.connect(mixCdId, 'output', cfCdId, 'mix');

    const [masterMixId, masterMixNode] = factory.makeFloatInNode(
      'Master Mix', 0.5, 0.0, 1.0,
      bounds(L.colMixCtrl, masterY, 130, L.nodeHSmall),
    );
    builder.addNode(masterMixId, masterMixNode);
    builder.exposeInput(masterMixId);
    builder.connect(masterMixId, 'output', masterId, 'mix');
  }

  // =========================================================================
  // MIDI Controller Integration (APC40 MK II)
  // =========================================================================
  if (features.midi) {
  const midiBaseY = -200;

  // -- MIDI In hub node --
  const [midiInId, midiInNode] = factory.makeMidiInNode(
    bounds(L.colMidi, midiBaseY, 130, L.nodeHMidi),
    'APC40 MK II',
  );
  builder.addNode(midiInId, midiInNode);

  // -- MIDI Comment --
  const [midiCmtId, midiCmtNode] = factory.makeCommentNode(
    'APC40 MK II MIDI\nConnect your controller to this MIDI In node',
    bounds(L.colMidi, midiBaseY - 120, 300, 100),
  );
  builder.addNode(midiCmtId, midiCmtNode);

  // -- APC40 Crossfader -> Master Mix (CC 15, Ch 0) --
  {
    const ccY = midiBaseY + L.nodeHMidi + 20;
    const [ccId, ccNode] = factory.makeFilterCCNode(
      APC40.crossfader.ch, APC40.crossfader.cc,
      bounds(L.colMidi, ccY, 150, L.nodeHMidi),
      'Crossfader (CC15)',
    );
    builder.addNode(ccId, ccNode);
    builder.connect(midiInId, 'output', ccId, 'input');

    // Float In for the crossfader value, exposed for MIDI learn
    const [fid, fnode] = factory.makeFloatInNode(
      'APC40 Crossfader', 0.5, 0.0, 1.0,
      bounds(L.colMidi + 180, ccY, 130, L.nodeHSmall),
    );
    builder.addNode(fid, fnode);
    builder.exposeInput(fid);
    builder.connect(ccId, 'value', fid, 'input');
    // Connect to all crossfader mix ports (the last crossfader = master or only one)
    // We re-use the Float In approach: the APC40 crossfader CC drives a Float In
    // which is already connected to the master crossfader via the mix Float Ins above.
    // For direct connection, we note the master crossfader's mix Float In was already
    // wired. The exposed Float In here provides an alternate MIDI-driven path.
  }

  // -- APC40 Track Faders -> per-shader opacity (CC 7, Ch 0-3) --
  for (let slot = 0; slot < n; slot++) {
    const faderY = midiBaseY + (slot + 2) * (L.nodeHMidi + 20);
    const apc = APC40.trackFaders[slot];
    const [ccId, ccNode] = factory.makeFilterCCNode(
      apc.ch, apc.cc,
      bounds(L.colMidi, faderY, 150, L.nodeHMidi),
      `Fader ${String.fromCharCode(65 + slot)} (CC7 Ch${apc.ch})`,
    );
    builder.addNode(ccId, ccNode);
    builder.connect(midiInId, 'output', ccId, 'input');

    // Create opacity Float In for this shader
    const [opId, opNode] = factory.makeFloatInNode(
      `Opacity ${String.fromCharCode(65 + slot)}`, 1.0, 0.0, 1.0,
      bounds(L.colMidi + 180, faderY, 130, L.nodeHSmall),
    );
    builder.addNode(opId, opNode);
    builder.exposeInput(opId);
    builder.connect(ccId, 'value', opId, 'input');
    // Connect opacity to ISF bypass (inverse: 0 opacity = bypass on)
    // Wire ISF nodes have a bypass constant we can drive
  }

  // -- APC40 Device Knobs -> shader params (CC 16-23, Ch 0) --
  // Map 2 knobs per shader (up to 8 knobs for 4 shaders)
  {
    const knobsPerShader = Math.min(Math.floor(8 / n), MAX_CUSTOM_PARAMS);
    let knobIdx = 0;
    for (let slot = 0; slot < n && knobIdx < 8; slot++) {
      const customParams = getFloatParams(allWireInputs[slot]);
      const usable = Math.min(customParams.length, knobsPerShader);
      for (let ki = 0; ki < usable && knobIdx < 8; ki++) {
        const param = customParams[ki];
        const apcKnob = APC40.knobs[knobIdx];
        const knobY = midiBaseY + (n + 3 + knobIdx) * (L.nodeHMidi + 20);

        const [ccId, ccNode] = factory.makeFilterCCNode(
          apcKnob.ch, apcKnob.cc,
          bounds(L.colMidi, knobY, 150, L.nodeHMidi),
          `Knob ${knobIdx + 1} (CC${apcKnob.cc}) -> ${param.label || param.name}`,
        );
        builder.addNode(ccId, ccNode);
        builder.connect(midiInId, 'output', ccId, 'input');

        // The knob's CC value (0-1 normalized) feeds into a Float In
        // which is already connected to the ISF param from the per-shader section
        const [fid, fnode] = factory.makeFloatInNode(
          `Knob: ${shaderNames[slot]} ${param.label || param.name}`,
          param.defaultValue ?? 0.5,
          param.min ?? 0.0,
          param.max ?? 1.0,
          bounds(L.colMidi + 180, knobY, 150, L.nodeHSmall),
        );
        builder.addNode(fid, fnode);
        builder.exposeInput(fid);
        builder.connect(ccId, 'value', fid, 'input');
        builder.connect(fid, 'output', isfNodeIds[slot], param.name);

        knobIdx++;
      }
    }
  }

  // -- APC40 Clip Grid -> shader toggle on/off (Notes 0-4 on Ch 0-3) --
  // Scene 0 = shader on, Scene 1 = shader off, Scene 2-4 = fade presets
  for (let slot = 0; slot < n; slot++) {
    // Toggle on (Scene 1, Note 0)
    const toggleY = midiBaseY + (n + 12 + slot * 2) * (L.nodeHMidi + 20);
    const clipOn = APC40.clipGrid.find(c => c.note === 0 && c.ch === slot);
    if (clipOn) {
      const [noteId, noteNode] = factory.makeFilterNoteOnNode(
        clipOn.ch, clipOn.note,
        bounds(L.colMidi, toggleY, 150, L.nodeHMidi),
        `Clip On ${String.fromCharCode(65 + slot)} (S1T${slot + 1})`,
      );
      builder.addNode(noteId, noteNode);
      builder.connect(midiInId, 'output', noteId, 'input');

      // Bool In for shader enable/disable
      const [boolId, boolNode] = factory.makeBoolInNode(
        `Enable ${String.fromCharCode(65 + slot)}`, true,
        bounds(L.colMidi + 180, toggleY, 130, L.nodeHSmall),
      );
      builder.addNode(boolId, boolNode);
      builder.exposeInput(boolId);
      builder.connect(noteId, 'velocity', boolId, 'input');
    }

    // Fade toggle (Scene 2, Note 1)
    const fadeY = toggleY + L.nodeHMidi + 20;
    const clipFade = APC40.clipGrid.find(c => c.note === 1 && c.ch === slot);
    if (clipFade) {
      const [noteId, noteNode] = factory.makeFilterNoteOnNode(
        clipFade.ch, clipFade.note,
        bounds(L.colMidi, fadeY, 150, L.nodeHMidi),
        `Fade ${String.fromCharCode(65 + slot)} (S2T${slot + 1})`,
      );
      builder.addNode(noteId, noteNode);
      builder.connect(midiInId, 'output', noteId, 'input');

      // Bool In for fade enable
      const [boolId, boolNode] = factory.makeBoolInNode(
        `Fade Mode ${String.fromCharCode(65 + slot)}`, false,
        bounds(L.colMidi + 180, fadeY, 130, L.nodeHSmall),
      );
      builder.addNode(boolId, boolNode);
      builder.exposeInput(boolId);
    }
  }

  // -- APC40 Master Fader (CC 14, Ch 0) --
  {
    const masterFY = midiBaseY + (n + 20) * (L.nodeHMidi + 20);
    const [ccId, ccNode] = factory.makeFilterCCNode(
      APC40.masterFader.ch, APC40.masterFader.cc,
      bounds(L.colMidi, masterFY, 150, L.nodeHMidi),
      'Master Fader (CC14)',
    );
    builder.addNode(ccId, ccNode);
    builder.connect(midiInId, 'output', ccId, 'input');

    const [fid, fnode] = factory.makeFloatInNode(
      'Master Volume', 1.0, 0.0, 1.0,
      bounds(L.colMidi + 180, masterFY, 130, L.nodeHSmall),
    );
    builder.addNode(fid, fnode);
    builder.exposeInput(fid);
    builder.connect(ccId, 'value', fid, 'input');
  }

  // -- APC40 Cue Level (CC 47, Ch 0) --
  {
    const cueY = midiBaseY + (n + 21) * (L.nodeHMidi + 20);
    const [ccId, ccNode] = factory.makeFilterCCNode(
      APC40.cueLevel.ch, APC40.cueLevel.cc,
      bounds(L.colMidi, cueY, 150, L.nodeHMidi),
      'Cue Level (CC47)',
    );
    builder.addNode(ccId, ccNode);
    builder.connect(midiInId, 'output', ccId, 'input');

    const [fid, fnode] = factory.makeFloatInNode(
      'Cue Level', 0.5, 0.0, 1.0,
      bounds(L.colMidi + 180, cueY, 130, L.nodeHSmall),
    );
    builder.addNode(fid, fnode);
    builder.exposeInput(fid);
    builder.connect(ccId, 'value', fid, 'input');
  }

  // -- APC40 Scene Launch buttons (Notes 82-86, Ch 0) --
  for (let si = 0; si < APC40.sceneLaunch.length; si++) {
    const scn = APC40.sceneLaunch[si];
    const scnY = midiBaseY + (n + 22 + si) * (L.nodeHMidi + 20);
    const [noteId, noteNode] = factory.makeFilterNoteOnNode(
      scn.ch, scn.note,
      bounds(L.colMidi, scnY, 150, L.nodeHMidi),
      `Scene ${si + 1} (Note${scn.note})`,
    );
    builder.addNode(noteId, noteNode);
    builder.connect(midiInId, 'output', noteId, 'input');

    const [boolId, boolNode] = factory.makeBoolInNode(
      `Scene ${si + 1} Launch`, false,
      bounds(L.colMidi + 180, scnY, 130, L.nodeHSmall),
    );
    builder.addNode(boolId, boolNode);
    builder.exposeInput(boolId);
  }

  // =========================================================================
  // Roli Blocks MIDI Integration
  // =========================================================================
  {
    const roliBaseY = midiBaseY;
    const roliX = L.colMidi - 400; // Position left of APC40

    // -- Roli MIDI In --
    const [roliInId, roliInNode] = factory.makeMidiInNode(
      bounds(roliX, roliBaseY, 130, L.nodeHMidi),
      'Roli Blocks',
    );
    builder.addNode(roliInId, roliInNode);

    // -- Roli Comment --
    const [roliCmtId, roliCmtNode] = factory.makeCommentNode(
      'ROLI BLOCKS\nSlide Y, Mod Wheel, Expression\nPad triggers for shader toggle',
      bounds(roliX, roliBaseY - 120, 300, 100),
    );
    builder.addNode(roliCmtId, roliCmtNode);

    // -- Slide Y (CC 74) -> mouseY on all shaders --
    {
      const y = roliBaseY + L.nodeHMidi + 20;
      const [ccId, ccNode] = factory.makeFilterCCNode(
        ROLI.slideY.ch, ROLI.slideY.cc,
        bounds(roliX, y, 150, L.nodeHMidi),
        'Roli Slide Y (CC74)',
      );
      builder.addNode(ccId, ccNode);
      builder.connect(roliInId, 'output', ccId, 'input');
      const [fid, fnode] = factory.makeFloatInNode(
        'Roli Slide Y', 0.5, 0.0, 1.0,
        bounds(roliX + 180, y, 130, L.nodeHSmall),
      );
      builder.addNode(fid, fnode);
      builder.exposeInput(fid);
      builder.connect(ccId, 'value', fid, 'input');
    }

    // -- Mod Wheel (CC 1) -> mouseX on all shaders --
    {
      const y = roliBaseY + 2 * (L.nodeHMidi + 20);
      const [ccId, ccNode] = factory.makeFilterCCNode(
        ROLI.modWheel.ch, ROLI.modWheel.cc,
        bounds(roliX, y, 150, L.nodeHMidi),
        'Roli Mod/Glide (CC1)',
      );
      builder.addNode(ccId, ccNode);
      builder.connect(roliInId, 'output', ccId, 'input');
      const [fid, fnode] = factory.makeFloatInNode(
        'Roli Mod Wheel', 0.5, 0.0, 1.0,
        bounds(roliX + 180, y, 130, L.nodeHSmall),
      );
      builder.addNode(fid, fnode);
      builder.exposeInput(fid);
      builder.connect(ccId, 'value', fid, 'input');
    }

    // -- Expression (CC 11) -> timeScale --
    {
      const y = roliBaseY + 3 * (L.nodeHMidi + 20);
      const [ccId, ccNode] = factory.makeFilterCCNode(
        ROLI.expression.ch, ROLI.expression.cc,
        bounds(roliX, y, 150, L.nodeHMidi),
        'Roli Expression (CC11)',
      );
      builder.addNode(ccId, ccNode);
      builder.connect(roliInId, 'output', ccId, 'input');
      const [fid, fnode] = factory.makeFloatInNode(
        'Roli Expression', 1.0, 0.1, 4.0,
        bounds(roliX + 180, y, 130, L.nodeHSmall),
      );
      builder.addNode(fid, fnode);
      builder.exposeInput(fid);
      builder.connect(ccId, 'value', fid, 'input');
    }

    // -- XY Pad CC16 (X) / CC17 (Y) --
    {
      const y = roliBaseY + 4 * (L.nodeHMidi + 20);
      const [ccXId, ccXNode] = factory.makeFilterCCNode(
        ROLI.xyPad.ch, ROLI.xyPad.ccX,
        bounds(roliX, y, 150, L.nodeHMidi),
        'Roli XY Pad X (CC16)',
      );
      builder.addNode(ccXId, ccXNode);
      builder.connect(roliInId, 'output', ccXId, 'input');
      const [fxId, fxNode] = factory.makeFloatInNode(
        'Roli XY X', 0.5, 0.0, 1.0,
        bounds(roliX + 180, y, 130, L.nodeHSmall),
      );
      builder.addNode(fxId, fxNode);
      builder.exposeInput(fxId);
      builder.connect(ccXId, 'value', fxId, 'input');

      const yy = y + L.nodeHMidi + 20;
      const [ccYId, ccYNode] = factory.makeFilterCCNode(
        ROLI.xyPad.ch, ROLI.xyPad.ccY,
        bounds(roliX, yy, 150, L.nodeHMidi),
        'Roli XY Pad Y (CC17)',
      );
      builder.addNode(ccYId, ccYNode);
      builder.connect(roliInId, 'output', ccYId, 'input');
      const [fyId, fyNode] = factory.makeFloatInNode(
        'Roli XY Y', 0.5, 0.0, 1.0,
        bounds(roliX + 180, yy, 130, L.nodeHSmall),
      );
      builder.addNode(fyId, fyNode);
      builder.exposeInput(fyId);
      builder.connect(ccYId, 'value', fyId, 'input');
    }

    // -- Pad Notes -> shader triggers (C4, D4, E4, G4) --
    for (let pi = 0; pi < Math.min(ROLI.padNotes.length, n); pi++) {
      const pad = ROLI.padNotes[pi];
      const y = roliBaseY + (6 + pi) * (L.nodeHMidi + 20);
      const [noteId, noteNode] = factory.makeFilterNoteOnNode(
        pad.ch, pad.note,
        bounds(roliX, y, 150, L.nodeHMidi),
        `Roli ${pad.label}`,
      );
      builder.addNode(noteId, noteNode);
      builder.connect(roliInId, 'output', noteId, 'input');
      const [boolId, boolNode] = factory.makeBoolInNode(
        `Roli Trigger ${String.fromCharCode(65 + pi)}`, false,
        bounds(roliX + 180, y, 130, L.nodeHSmall),
      );
      builder.addNode(boolId, boolNode);
      builder.exposeInput(boolId);
    }
  }
  } // end if (features.midi)

  // =========================================================================
  // FFT / Audio-Reactive Integration
  // =========================================================================
  if (features.fft) {
    const fftY = outY - 100;
    const [specId, specNode] = factory.makeSpectrumInNode(
      bounds(L.colFft, fftY, 150, L.nodeHMidi),
      'FFT Spectrum',
    );
    builder.addNode(specId, specNode);

    // FFT Comment
    const [fftCmtId, fftCmtNode] = factory.makeCommentNode(
      'FFT Audio Input\nBass/Mid/High available as\nFloat outputs for modulation',
      bounds(L.colFft, fftY - 120, 300, 100),
    );
    builder.addNode(fftCmtId, fftCmtNode);

    // Create Float In nodes for bass, mid, high frequency bands
    // These are exposed so they can be MIDI-learned or driven by FFT
    const fftBands = [
      { name: 'FFT Bass', default: 0.0, y: fftY + L.nodeHMidi + 20 },
      { name: 'FFT Mid', default: 0.0, y: fftY + 2 * (L.nodeHMidi + 20) },
      { name: 'FFT High', default: 0.0, y: fftY + 3 * (L.nodeHMidi + 20) },
    ];
    for (const band of fftBands) {
      const [fid, fnode] = factory.makeFloatInNode(
        band.name, band.default, 0.0, 1.0,
        bounds(L.colFft, band.y, 130, L.nodeHSmall),
      );
      builder.addNode(fid, fnode);
      builder.exposeInput(fid);
    }
  }

  return builder;
}

// ---------------------------------------------------------------------------
// Enhanced Patch Topologies (per VJ set mood)
// ---------------------------------------------------------------------------

// Layout constants for enhanced patches
const EL = {
  colOsc:     -1000,   // Oscillator / modulation nodes
  colParams:   -700,   // Parameter Float Ins
  colTexIn:    -500,   // Texture inputs
  colIsf:      -200,   // ISF shader nodes
  colFx1:       200,   // First FX chain stage
  colFx2:       500,   // Second FX chain stage
  colMix:       750,   // Mixer / crossfader
  colFx3:      1000,   // Post-mix FX
  colOut:      1300,   // Texture Out
  colFeedback: 1100,   // Delay (feedback return)
  rowSpacing:   350,
};

/**
 * Determine the enhanced topology type for a given VJ set.
 */
function getTopologyForSet(setName) {
  const moods = {
    'vj-ambient':   'feedback',
    'vj-cosmic':    'feedback',
    'vj-dark':      'feedback',
    'vj-techno':    'beat',
    'vj-glitch':    'glitch',
    'vj-geometric': 'geometric',
    'vj-colour':    'colour',
    'vj-organic':   'feedback',
    'vj-wire-ready':'beat',
  };
  return moods[setName] || 'feedback';
}

/**
 * Build MIDI + FFT modulation section (shared by all enhanced topologies).
 * Returns { midiInId, fftBandIds, oscIds } for wiring to FX params.
 */
function addModulationSection(builder, factory, n, shaderNames, features = null) {
  if (!features) features = { fft: true, webcam: true, glitch: true, midi: true, fxLevel: 'advanced' };
  const modY = -250;

  let midiInId = null;
  let cfFidOut = null;
  const faderIds = [];
  const fftBandIds = [];
  let oscIds = { sineId: null, sawId: null, perlinId: null };

  if (features.midi) {
    // -- MIDI In --
    const [_midiInId, midiInNode] = factory.makeMidiInNode(
      bounds(EL.colOsc - 300, modY, 130, L.nodeHMidi), 'APC40 MK II',
    );
    midiInId = _midiInId;
    builder.addNode(midiInId, midiInNode);

    // -- APC40 Crossfader CC15 -> Float In --
    const crossY = modY + L.nodeHMidi + 20;
    const [cfCcId, cfCcNode] = factory.makeFilterCCNode(
      APC40.crossfader.ch, APC40.crossfader.cc,
      bounds(EL.colOsc - 300, crossY, 150, L.nodeHMidi), 'Crossfader (CC15)',
    );
    builder.addNode(cfCcId, cfCcNode);
    builder.connect(midiInId, 'output', cfCcId, 'input');
    const [cfFid, cfFnode] = factory.makeFloatInNode(
      'APC40 Crossfader', 0.5, 0.0, 1.0,
      bounds(EL.colOsc - 100, crossY, 130, L.nodeHSmall),
    );
    builder.addNode(cfFid, cfFnode);
    builder.exposeInput(cfFid);
    builder.connect(cfCcId, 'value', cfFid, 'input');
    cfFidOut = cfFid;

    // -- APC40 Track faders per shader --
    for (let i = 0; i < n; i++) {
      const fY = modY + (i + 2) * (L.nodeHMidi + 20);
      const apc = APC40.trackFaders[i];
      const [ccId, ccNode] = factory.makeFilterCCNode(
        apc.ch, apc.cc,
        bounds(EL.colOsc - 300, fY, 150, L.nodeHMidi),
        `Fader ${String.fromCharCode(65 + i)} (CC7 Ch${apc.ch})`,
      );
      builder.addNode(ccId, ccNode);
      builder.connect(midiInId, 'output', ccId, 'input');
      const [fid, fnode] = factory.makeFloatInNode(
        `Opacity ${String.fromCharCode(65 + i)}`, 1.0, 0.0, 1.0,
        bounds(EL.colOsc - 100, fY, 130, L.nodeHSmall),
      );
      builder.addNode(fid, fnode);
      builder.exposeInput(fid);
      builder.connect(ccId, 'value', fid, 'input');
      faderIds.push(fid);
    }
  } // end if (features.midi)

  if (features.fft) {
    // -- FFT Spectrum --
    const fftY = modY + (n + 3) * (L.nodeHMidi + 20);
    const [specId, specNode] = factory.makeSpectrumInNode(
      bounds(EL.colOsc - 300, fftY, 150, L.nodeHMidi), 'FFT Spectrum',
    );
    builder.addNode(specId, specNode);

    const bandNames = ['FFT Bass', 'FFT Mid', 'FFT High'];
    for (let bi = 0; bi < bandNames.length; bi++) {
      const bY = fftY + (bi + 1) * (L.nodeHSmall + 10);
      const [fid, fnode] = factory.makeFloatInNode(
        bandNames[bi], 0.0, 0.0, 1.0,
        bounds(EL.colOsc - 100, bY, 130, L.nodeHSmall),
      );
      builder.addNode(fid, fnode);
      builder.exposeInput(fid);
      fftBandIds.push(fid);
    }
  } // end if (features.fft)

  // -- Oscillators (always included for modulation) --
  {
    const oscBaseY = modY + (n + 7) * (L.nodeHMidi + 20);
    const [sineId, sineNode] = factory.makeSineNode(
      0.25, bounds(EL.colOsc, oscBaseY, 130, L.nodeHSmall), 'LFO Sine (slow)',
    );
    builder.addNode(sineId, sineNode);

    const [sawId, sawNode] = factory.makeSawNode(
      1.0, bounds(EL.colOsc, oscBaseY + L.nodeHSmall + 10, 130, L.nodeHSmall), 'LFO Saw (1Hz)',
    );
    builder.addNode(sawId, sawNode);

    const [perlinId, perlinNode] = factory.makePerlinNode(
      bounds(EL.colOsc, oscBaseY + 2 * (L.nodeHSmall + 10), 130, L.nodeHSmall), 'Perlin Drift',
    );
    builder.addNode(perlinId, perlinNode);

    oscIds = { sineId, sawId, perlinId };
  }

  return { midiInId, cfFid: cfFidOut, faderIds, fftBandIds, oscIds };
}

/**
 * FEEDBACK topology: ISF A + B → Video Mixer → Delay (feedback loop) → Bloom → Hue Rotate → Vignette → Out
 * Best for: ambient, cosmic, dark, organic
 */
function generateFeedbackPatch(shaders, setName, patchName, features = null) {
  const n = shaders.length;
  const builder = new WirePatchBuilder(
    patchName,
    `Macroverse Enhanced FEEDBACK Generator: ${setName}\n` +
    shaders.map((s, i) => `  ${i + 1}. ${s.fixed_name || s.name}`).join('\n') +
    '\nFeedback loop with Bloom + Hue Rotate + Vignette post-processing',
    'source',
  );
  const factory = builder.factory;
  const shaderNames = shaders.map(s => s.fixed_name || s.name || '?');

  // Comment
  const [cid, cnode] = factory.makeCommentNode(
    `MACROVERSE FEEDBACK GENERATOR: ${setName.toUpperCase()}\n\n` +
    shaderNames.map((nm, i) => `  [${String.fromCharCode(65 + i)}] ${nm}`).join('\n') +
    '\n\nTopology: ISF → Video Mixer → Delay (feedback) → Bloom → Hue Rotate → Vignette → Out\n' +
    'Dashboard: Feedback Amount, Bloom, Hue Shift, Vignette, Speed\n' +
    'MIDI: APC40 MK II | FFT: Audio-reactive modulation',
    bounds(EL.colIsf - 50, -350, 2000, 280),
  );
  builder.addNode(cid, cnode);

  // -- Parse + build ISF nodes --
  const isfIds = [];
  const allInputs = [];
  for (let slot = 0; slot < n; slot++) {
    const shader = shaders[slot];
    const shaderPath = resolveShaderPath(shader);
    const header = parseISFHeader(shaderPath);
    const wireInputs = header ? getWireInputs(header) : [];
    allInputs.push(wireInputs);

    const slotY = slot * EL.rowSpacing;
    const [isfId, isfNode] = factory.makeISFNode(
      shaderPath, wireInputs,
      bounds(EL.colIsf, slotY, L.nodeW, L.nodeHIsf),
      shaderNames[slot],
    );
    builder.addNode(isfId, isfNode);
    isfIds.push(isfId);

    // Texture inputs for shaders with image type params
    const imageNames = getImageInputs(wireInputs);
    for (let ii = 0; ii < imageNames.length; ii++) {
      const texY = slotY + (ii + 1) * (L.paramH + L.paramGap);
      const [texId, texNode] = factory.makeTextureInNode(
        bounds(EL.colTexIn, texY, 130, L.nodeHSmall), `Input (${shaderNames[slot]})`,
      );
      builder.addNode(texId, texNode);
      builder.connect(texId, 'output', isfId, imageNames[ii]);
    }
  }

  // Shared params
  for (let si = 0; si < SHARED_PARAMS.length; si++) {
    const sp = SHARED_PARAMS[si];
    const [fid, fnode] = factory.makeFloatInNode(
      sp.label || sp.name, sp.default, sp.min, sp.max,
      bounds(EL.colParams, si * (L.paramH + L.paramGap), 130, L.nodeHSmall),
    );
    builder.addNode(fid, fnode);
    builder.exposeInput(fid);
    for (let i = 0; i < isfIds.length; i++) {
      if (allInputs[i].some(inp => inp.name === sp.name)) {
        builder.connect(fid, 'output', isfIds[i], sp.name);
      }
    }
  }

  // Per-shader custom params
  for (let slot = 0; slot < n; slot++) {
    const customParams = getFloatParams(allInputs[slot]);
    for (let pi = 0; pi < customParams.length; pi++) {
      const param = customParams[pi];
      const pY = slot * EL.rowSpacing + pi * (L.paramH + L.paramGap);
      const [fid, fnode] = factory.makeFloatInNode(
        `${shaderNames[slot]}: ${param.label || param.name}`,
        param.defaultValue ?? 0.5, param.min ?? 0.0, param.max ?? 1.0,
        bounds(EL.colParams, pY, 150, L.nodeHSmall),
      );
      builder.addNode(fid, fnode);
      builder.exposeInput(fid);
      builder.connect(fid, 'output', isfIds[slot], param.name);
    }
  }

  // -- Video Mixer (ISF A + B inputs, Delay feeds back into input2) --
  const mixY = ((n - 1) * EL.rowSpacing) / 2;
  const [vmixId, vmixNode] = factory.makeVideoMixerNode(
    bounds(EL.colFx1, mixY, L.nodeW, L.nodeHCf), 'Feedback Mixer',
  );
  builder.addNode(vmixId, vmixNode);

  // Connect ISF outputs to mixer
  if (n >= 1) builder.connect(isfIds[0], 'output', vmixId, 'input1');
  if (n >= 2) builder.connect(isfIds[1], 'output', vmixId, 'input2');

  // If 3+ shaders, chain additional crossfaders before the mixer
  let feedInput = vmixId;
  let feedInputPort = 'output';
  if (n >= 3) {
    const [cf2Id, cf2Node] = factory.makeCrossfaderNode(
      bounds(EL.colFx1, mixY + EL.rowSpacing, L.nodeW, L.nodeHCf), 'Extra Mix',
    );
    builder.addNode(cf2Id, cf2Node);
    if (n >= 3) builder.connect(isfIds[2], 'output', cf2Id, 'input1');
    if (n >= 4) builder.connect(isfIds[3], 'output', cf2Id, 'input2');
    const [cf2MixId, cf2MixNode] = factory.makeFloatInNode(
      'Extra Mix', 0.5, 0.0, 1.0,
      bounds(EL.colFx1 - 180, mixY + EL.rowSpacing, 130, L.nodeHSmall),
    );
    builder.addNode(cf2MixId, cf2MixNode);
    builder.exposeInput(cf2MixId);
    builder.connect(cf2MixId, 'output', cf2Id, 'mix');

    // Second video mixer to combine both halves
    const [vm2Id, vm2Node] = factory.makeVideoMixerNode(
      bounds(EL.colFx2, mixY + EL.rowSpacing / 2, L.nodeW, L.nodeHCf), 'Combine',
    );
    builder.addNode(vm2Id, vm2Node);
    builder.connect(vmixId, 'output', vm2Id, 'input1');
    builder.connect(cf2Id, 'output0', vm2Id, 'input2');
    feedInput = vm2Id;
  }

  // -- Feedback amount Float In --
  const [fbAmtId, fbAmtNode] = factory.makeFloatInNode(
    'Feedback Amount', 0.7, 0.0, 0.98,
    bounds(EL.colFx1 - 180, mixY - EL.rowSpacing / 2, 130, L.nodeHSmall),
  );
  builder.addNode(fbAmtId, fbAmtNode);
  builder.exposeInput(fbAmtId);

  // -- Bloom --
  const [bloomId, bloomNode] = factory.makeBloomNode(
    bounds(EL.colFx2, mixY - 60, L.nodeW, L.nodeHSmall), 'Bloom',
  );
  builder.addNode(bloomId, bloomNode);
  builder.connect(feedInput, feedInputPort, bloomId, 'input');

  const [bloomAmtId, bloomAmtNode] = factory.makeFloatInNode(
    'Bloom Size', 0.4, 0.0, 1.0,
    bounds(EL.colFx2 - 180, mixY - 60, 130, L.nodeHSmall),
  );
  builder.addNode(bloomAmtId, bloomAmtNode);
  builder.exposeInput(bloomAmtId);
  builder.connect(bloomAmtId, 'output', bloomId, 'threshold');

  // -- Hue Rotate --
  const [hueId, hueNode] = factory.makeHueRotateNode(
    bounds(EL.colFx2, mixY + 40, L.nodeW, L.nodeHSmall), 'Hue Shift',
  );
  builder.addNode(hueId, hueNode);
  builder.connect(bloomId, 'output', hueId, 'input');

  const [hueAmtId, hueAmtNode] = factory.makeFloatInNode(
    'Hue Shift', 0.0, 0.0, 1.0,
    bounds(EL.colFx2 - 180, mixY + 40, 130, L.nodeHSmall),
  );
  builder.addNode(hueAmtId, hueAmtNode);
  builder.exposeInput(hueAmtId);
  builder.connect(hueAmtId, 'output', hueId, 'rotation');

  // -- Vignette --
  const [vigId, vigNode] = factory.makeVignetteNode(
    bounds(EL.colFx3, mixY, L.nodeW, L.nodeHSmall), 'Vignette',
  );
  builder.addNode(vigId, vigNode);
  builder.connect(hueId, 'output', vigId, 'input');

  const [vigAmtId, vigAmtNode] = factory.makeFloatInNode(
    'Vignette', 0.4, 0.0, 1.0,
    bounds(EL.colFx3 - 180, mixY, 130, L.nodeHSmall),
  );
  builder.addNode(vigAmtId, vigAmtNode);
  builder.exposeInput(vigAmtId);
  builder.connect(vigAmtId, 'output', vigId, 'amount');

  // -- Delay (feedback loop) --
  const [delayId, delayNode] = factory.makeDelayNode(
    1, bounds(EL.colFeedback, mixY + EL.rowSpacing, L.nodeW, L.nodeHSmall), 'Feedback Delay',
  );
  builder.addNode(delayId, delayNode);
  builder.connect(vigId, 'output', delayId, 'input');
  // Feed delay output back into mixer opacity2 (creates feedback loop)
  builder.connect(delayId, 'output', vmixId, 'input2');
  builder.connect(fbAmtId, 'output', vmixId, 'opacity2');

  // -- Texture Out --
  const [outId, outNode] = factory.makeTextureOutNode(
    bounds(EL.colOut, mixY, 132, L.nodeHSmall),
  );
  builder.addNode(outId, outNode);
  builder.connect(vigId, 'output', outId, 'input');

  // -- Modulation section --
  addModulationSection(builder, factory, n, shaderNames, features);

  return builder;
}

/**
 * BEAT topology: ISF → Cross Fader → Color Offset → Transform → Out
 * Metronome + Sine drive Color Offset and Transform params for beat-synced motion.
 * Best for: techno, wire-ready
 */
function generateBeatPatch(shaders, setName, patchName, features = null) {
  const n = shaders.length;
  const builder = new WirePatchBuilder(
    patchName,
    `Macroverse Enhanced BEAT Generator: ${setName}\n` +
    shaders.map((s, i) => `  ${i + 1}. ${s.fixed_name || s.name}`).join('\n') +
    '\nBeat-synced Color Offset + Transform with Metronome/Sine oscillators',
    'source',
  );
  const factory = builder.factory;
  const shaderNames = shaders.map(s => s.fixed_name || s.name || '?');

  // Comment
  const [cid, cnode] = factory.makeCommentNode(
    `MACROVERSE BEAT GENERATOR: ${setName.toUpperCase()}\n\n` +
    shaderNames.map((nm, i) => `  [${String.fromCharCode(65 + i)}] ${nm}`).join('\n') +
    '\n\nTopology: ISF → CrossFader → Color Offset → Transform → Bloom → Out\n' +
    'Metronome + Sine drive beat-synced FX parameters\n' +
    'MIDI: APC40 MK II | FFT: Bass → Color Offset, Mid → Transform',
    bounds(EL.colIsf - 50, -350, 2000, 280),
  );
  builder.addNode(cid, cnode);

  // ISF nodes
  const isfIds = [];
  const allInputs = [];
  for (let slot = 0; slot < n; slot++) {
    const shader = shaders[slot];
    const shaderPath = resolveShaderPath(shader);
    const header = parseISFHeader(shaderPath);
    const wireInputs = header ? getWireInputs(header) : [];
    allInputs.push(wireInputs);
    const slotY = slot * EL.rowSpacing;
    const [isfId, isfNode] = factory.makeISFNode(
      shaderPath, wireInputs,
      bounds(EL.colIsf, slotY, L.nodeW, L.nodeHIsf), shaderNames[slot],
    );
    builder.addNode(isfId, isfNode);
    isfIds.push(isfId);

    const imageNames = getImageInputs(wireInputs);
    for (let ii = 0; ii < imageNames.length; ii++) {
      const texY = slotY + (ii + 1) * (L.paramH + L.paramGap);
      const [texId, texNode] = factory.makeTextureInNode(
        bounds(EL.colTexIn, texY, 130, L.nodeHSmall), `Input (${shaderNames[slot]})`,
      );
      builder.addNode(texId, texNode);
      builder.connect(texId, 'output', isfId, imageNames[ii]);
    }
  }

  // Shared params
  for (let si = 0; si < SHARED_PARAMS.length; si++) {
    const sp = SHARED_PARAMS[si];
    const [fid, fnode] = factory.makeFloatInNode(
      sp.label || sp.name, sp.default, sp.min, sp.max,
      bounds(EL.colParams, si * (L.paramH + L.paramGap), 130, L.nodeHSmall),
    );
    builder.addNode(fid, fnode);
    builder.exposeInput(fid);
    for (let i = 0; i < isfIds.length; i++) {
      if (allInputs[i].some(inp => inp.name === sp.name)) {
        builder.connect(fid, 'output', isfIds[i], sp.name);
      }
    }
  }

  // Per-shader custom params
  for (let slot = 0; slot < n; slot++) {
    const customParams = getFloatParams(allInputs[slot]);
    for (let pi = 0; pi < customParams.length; pi++) {
      const param = customParams[pi];
      const pY = slot * EL.rowSpacing + pi * (L.paramH + L.paramGap);
      const [fid, fnode] = factory.makeFloatInNode(
        `${shaderNames[slot]}: ${param.label || param.name}`,
        param.defaultValue ?? 0.5, param.min ?? 0.0, param.max ?? 1.0,
        bounds(EL.colParams, pY, 150, L.nodeHSmall),
      );
      builder.addNode(fid, fnode);
      builder.exposeInput(fid);
      builder.connect(fid, 'output', isfIds[slot], param.name);
    }
  }

  // -- Crossfader topology (same as standard) --
  const mixY = ((n - 1) * EL.rowSpacing) / 2;
  let chainInput; // node ID feeding into FX chain
  let chainPort = 'output0';

  if (n === 1) {
    chainInput = isfIds[0];
    chainPort = 'output';
  } else if (n === 2) {
    const [cfId, cfNode] = factory.makeCrossfaderNode(
      bounds(EL.colFx1, mixY, L.nodeW, L.nodeHCf), 'Mix A/B',
    );
    builder.addNode(cfId, cfNode);
    builder.connect(isfIds[0], 'output', cfId, 'input1');
    builder.connect(isfIds[1], 'output', cfId, 'input2');
    const [mfId, mfNode] = factory.makeFloatInNode(
      'Mix A/B', 0.5, 0.0, 1.0,
      bounds(EL.colFx1 - 180, mixY, 130, L.nodeHSmall),
    );
    builder.addNode(mfId, mfNode);
    builder.exposeInput(mfId);
    builder.connect(mfId, 'output', cfId, 'mix');
    chainInput = cfId;
  } else {
    // 3-4 shaders: A+B crossfader, C+D crossfader, master
    const [cfAbId, cfAbNode] = factory.makeCrossfaderNode(
      bounds(EL.colFx1, EL.rowSpacing / 2, L.nodeW, L.nodeHCf), 'Mix A/B',
    );
    builder.addNode(cfAbId, cfAbNode);
    builder.connect(isfIds[0], 'output', cfAbId, 'input1');
    builder.connect(isfIds[1], 'output', cfAbId, 'input2');
    const [mabId, mabNode] = factory.makeFloatInNode(
      'Mix A/B', 0.5, 0.0, 1.0,
      bounds(EL.colFx1 - 180, EL.rowSpacing / 2, 130, L.nodeHSmall),
    );
    builder.addNode(mabId, mabNode);
    builder.exposeInput(mabId);
    builder.connect(mabId, 'output', cfAbId, 'mix');

    let secondInput = cfAbId;
    if (n >= 3) {
      const [cfCdId, cfCdNode] = factory.makeCrossfaderNode(
        bounds(EL.colFx1, EL.rowSpacing * 2.5, L.nodeW, L.nodeHCf), 'Mix C/D',
      );
      builder.addNode(cfCdId, cfCdNode);
      builder.connect(isfIds[2], 'output', cfCdId, 'input1');
      if (n >= 4) builder.connect(isfIds[3], 'output', cfCdId, 'input2');
      const [mcdId, mcdNode] = factory.makeFloatInNode(
        'Mix C/D', 0.5, 0.0, 1.0,
        bounds(EL.colFx1 - 180, EL.rowSpacing * 2.5, 130, L.nodeHSmall),
      );
      builder.addNode(mcdId, mcdNode);
      builder.exposeInput(mcdId);
      builder.connect(mcdId, 'output', cfCdId, 'mix');

      const [mastId, mastNode] = factory.makeCrossfaderNode(
        bounds(EL.colFx1 + 250, mixY, L.nodeW, L.nodeHCf), 'Master Mix',
      );
      builder.addNode(mastId, mastNode);
      builder.connect(cfAbId, 'output0', mastId, 'input1');
      builder.connect(cfCdId, 'output0', mastId, 'input2');
      const [mmId, mmNode] = factory.makeFloatInNode(
        'Master Mix', 0.5, 0.0, 1.0,
        bounds(EL.colFx1 + 70, mixY, 130, L.nodeHSmall),
      );
      builder.addNode(mmId, mmNode);
      builder.exposeInput(mmId);
      builder.connect(mmId, 'output', mastId, 'mix');
      chainInput = mastId;
    } else {
      chainInput = cfAbId;
    }
  }

  // -- FX Chain: Color Offset → Transform → Bloom --
  const [colOffId, colOffNode] = factory.makeColorOffsetNode(
    bounds(EL.colFx2, mixY - 50, L.nodeW, L.nodeHSmall), 'Beat Color Offset',
  );
  builder.addNode(colOffId, colOffNode);
  builder.connect(chainInput, chainPort, colOffId, 'input');

  const [colAmtId, colAmtNode] = factory.makeFloatInNode(
    'Chromatic Shift', 0.03, 0.0, 0.15,
    bounds(EL.colFx2 - 180, mixY - 50, 130, L.nodeHSmall),
  );
  builder.addNode(colAmtId, colAmtNode);
  builder.exposeInput(colAmtId);
  builder.connect(colAmtId, 'output', colOffId, 'amount');

  const [txfId, txfNode] = factory.makeTransformNode(
    bounds(EL.colFx2, mixY + 50, L.nodeW, L.nodeHSmall), 'Beat Transform',
  );
  builder.addNode(txfId, txfNode);
  builder.connect(colOffId, 'output', txfId, 'input');

  const [rotId, rotNode] = factory.makeFloatInNode(
    'Rotation', 0.0, -0.5, 0.5,
    bounds(EL.colFx2 - 180, mixY + 50, 130, L.nodeHSmall),
  );
  builder.addNode(rotId, rotNode);
  builder.exposeInput(rotId);
  builder.connect(rotId, 'output', txfId, 'rotation');

  const [bloomId, bloomNode] = factory.makeBloomNode(
    bounds(EL.colFx3, mixY, L.nodeW, L.nodeHSmall), 'Beat Bloom',
  );
  builder.addNode(bloomId, bloomNode);
  builder.connect(txfId, 'output', bloomId, 'input');

  const [bloomSzId, bloomSzNode] = factory.makeFloatInNode(
    'Bloom Size', 0.3, 0.0, 1.0,
    bounds(EL.colFx3 - 180, mixY, 130, L.nodeHSmall),
  );
  builder.addNode(bloomSzId, bloomSzNode);
  builder.exposeInput(bloomSzId);
  builder.connect(bloomSzId, 'output', bloomId, 'threshold');

  // -- Beat Oscillators --
  const oscY = mixY + EL.rowSpacing;
  const [metId, metNode] = factory.makeMetronomeNode(
    120, bounds(EL.colOsc, oscY, 130, L.nodeHSmall), 'Beat Clock (120 BPM)',
  );
  builder.addNode(metId, metNode);

  const [sineId, sineNode] = factory.makeSineNode(
    2.0, bounds(EL.colOsc, oscY + L.nodeHSmall + 10, 130, L.nodeHSmall), 'Beat Sine',
  );
  builder.addNode(sineId, sineNode);

  // Map sine to color offset angle
  const [mapId, mapNode] = factory.makeMapNode(
    bounds(EL.colOsc + 160, oscY + L.nodeHSmall + 10, 130, L.nodeHSmall), 'Sine→Angle',
  );
  builder.addNode(mapId, mapNode);
  builder.connect(sineId, 'output', mapId, 'input');
  builder.connect(mapId, 'output', colOffId, 'angle');

  // -- Texture Out --
  const [outId, outNode] = factory.makeTextureOutNode(
    bounds(EL.colOut, mixY, 132, L.nodeHSmall),
  );
  builder.addNode(outId, outNode);
  builder.connect(bloomId, 'output', outId, 'input');

  // Modulation
  addModulationSection(builder, factory, n, shaderNames, features);

  return builder;
}

/**
 * GLITCH topology: ISF → Pixelate + Static → Video Mixer → Color Offset → UV Offset → Out
 * Random + Metronome triggers drive glitch FX intensity.
 * Best for: glitch
 */
function generateGlitchPatch(shaders, setName, patchName, features = null) {
  const n = shaders.length;
  const builder = new WirePatchBuilder(
    patchName,
    `Macroverse Enhanced GLITCH Generator: ${setName}\n` +
    shaders.map((s, i) => `  ${i + 1}. ${s.fixed_name || s.name}`).join('\n') +
    '\nPixelate + Static + Color Offset + UV Offset with Random triggers',
    'source',
  );
  const factory = builder.factory;
  const shaderNames = shaders.map(s => s.fixed_name || s.name || '?');

  // Comment
  const [cid, cnode] = factory.makeCommentNode(
    `MACROVERSE GLITCH GENERATOR: ${setName.toUpperCase()}\n\n` +
    shaderNames.map((nm, i) => `  [${String.fromCharCode(65 + i)}] ${nm}`).join('\n') +
    '\n\nTopology: ISF → Pixelate → Static → Color Offset → UV Offset → Threshold → Out\n' +
    'Random triggers drive glitch intensity parameters\n' +
    'MIDI: APC40 MK II | FFT: Bass → Pixelate, High → Color shift',
    bounds(EL.colIsf - 50, -350, 2000, 280),
  );
  builder.addNode(cid, cnode);

  // ISF nodes
  const isfIds = [];
  const allInputs = [];
  for (let slot = 0; slot < n; slot++) {
    const shader = shaders[slot];
    const shaderPath = resolveShaderPath(shader);
    const header = parseISFHeader(shaderPath);
    const wireInputs = header ? getWireInputs(header) : [];
    allInputs.push(wireInputs);
    const slotY = slot * EL.rowSpacing;
    const [isfId, isfNode] = factory.makeISFNode(
      shaderPath, wireInputs,
      bounds(EL.colIsf, slotY, L.nodeW, L.nodeHIsf), shaderNames[slot],
    );
    builder.addNode(isfId, isfNode);
    isfIds.push(isfId);
  }

  // Shared + custom params (same pattern)
  for (let si = 0; si < SHARED_PARAMS.length; si++) {
    const sp = SHARED_PARAMS[si];
    const [fid, fnode] = factory.makeFloatInNode(
      sp.label || sp.name, sp.default, sp.min, sp.max,
      bounds(EL.colParams, si * (L.paramH + L.paramGap), 130, L.nodeHSmall),
    );
    builder.addNode(fid, fnode);
    builder.exposeInput(fid);
    for (let i = 0; i < isfIds.length; i++) {
      if (allInputs[i].some(inp => inp.name === sp.name)) {
        builder.connect(fid, 'output', isfIds[i], sp.name);
      }
    }
  }
  for (let slot = 0; slot < n; slot++) {
    const customParams = getFloatParams(allInputs[slot]);
    for (let pi = 0; pi < customParams.length; pi++) {
      const param = customParams[pi];
      const pY = slot * EL.rowSpacing + pi * (L.paramH + L.paramGap);
      const [fid, fnode] = factory.makeFloatInNode(
        `${shaderNames[slot]}: ${param.label || param.name}`,
        param.defaultValue ?? 0.5, param.min ?? 0.0, param.max ?? 1.0,
        bounds(EL.colParams, pY, 150, L.nodeHSmall),
      );
      builder.addNode(fid, fnode);
      builder.exposeInput(fid);
      builder.connect(fid, 'output', isfIds[slot], param.name);
    }
  }

  // -- Crossfader topology --
  const mixY = ((n - 1) * EL.rowSpacing) / 2;
  let chainInput;
  let chainPort = 'output';

  if (n === 1) {
    chainInput = isfIds[0];
  } else {
    const [cfId, cfNode] = factory.makeCrossfaderNode(
      bounds(EL.colFx1, mixY, L.nodeW, L.nodeHCf), 'Glitch Mix',
    );
    builder.addNode(cfId, cfNode);
    builder.connect(isfIds[0], 'output', cfId, 'input1');
    builder.connect(isfIds[n > 1 ? 1 : 0], 'output', cfId, 'input2');
    const [mfId, mfNode] = factory.makeFloatInNode(
      'Glitch Mix', 0.5, 0.0, 1.0,
      bounds(EL.colFx1 - 180, mixY, 130, L.nodeHSmall),
    );
    builder.addNode(mfId, mfNode);
    builder.exposeInput(mfId);
    builder.connect(mfId, 'output', cfId, 'mix');
    chainInput = cfId;
    chainPort = 'output0';

    // Extra shaders via additional crossfader
    if (n >= 3) {
      const [cf2Id, cf2Node] = factory.makeCrossfaderNode(
        bounds(EL.colFx1, mixY + EL.rowSpacing, L.nodeW, L.nodeHCf), 'Glitch Mix 2',
      );
      builder.addNode(cf2Id, cf2Node);
      builder.connect(isfIds[2], 'output', cf2Id, 'input1');
      if (n >= 4) builder.connect(isfIds[3], 'output', cf2Id, 'input2');
      const [mastId, mastNode] = factory.makeCrossfaderNode(
        bounds(EL.colFx1 + 250, mixY + EL.rowSpacing / 2, L.nodeW, L.nodeHCf), 'Master',
      );
      builder.addNode(mastId, mastNode);
      builder.connect(cfId, 'output0', mastId, 'input1');
      builder.connect(cf2Id, 'output0', mastId, 'input2');
      const [mmId, mmNode] = factory.makeFloatInNode(
        'Master Mix', 0.5, 0.0, 1.0,
        bounds(EL.colFx1 + 70, mixY + EL.rowSpacing / 2, 130, L.nodeHSmall),
      );
      builder.addNode(mmId, mmNode);
      builder.exposeInput(mmId);
      builder.connect(mmId, 'output', mastId, 'mix');
      chainInput = mastId;
    }
  }

  // -- Glitch FX Chain: Pixelate → Static → Color Offset → UV Offset → Threshold --
  const [pixId, pixNode] = factory.makePixelateNode(
    bounds(EL.colFx2, mixY - 80, L.nodeW, L.nodeHSmall), 'Glitch Pixelate',
  );
  builder.addNode(pixId, pixNode);
  builder.connect(chainInput, chainPort, pixId, 'input');

  const [pixAmtId, pixAmtNode] = factory.makeFloatInNode(
    'Pixelate Amount', 0.05, 0.0, 0.5,
    bounds(EL.colFx2 - 180, mixY - 80, 130, L.nodeHSmall),
  );
  builder.addNode(pixAmtId, pixAmtNode);
  builder.exposeInput(pixAmtId);
  builder.connect(pixAmtId, 'output', pixId, 'size');

  const [statId, statNode] = factory.makeStaticNode(
    bounds(EL.colFx2, mixY, L.nodeW, L.nodeHSmall), 'Digital Static',
  );
  builder.addNode(statId, statNode);
  builder.connect(pixId, 'output', statId, 'input');

  const [statAmtId, statAmtNode] = factory.makeFloatInNode(
    'Static Amount', 0.15, 0.0, 1.0,
    bounds(EL.colFx2 - 180, mixY, 130, L.nodeHSmall),
  );
  builder.addNode(statAmtId, statAmtNode);
  builder.exposeInput(statAmtId);
  builder.connect(statAmtId, 'output', statId, 'amount');

  const [colOffId, colOffNode] = factory.makeColorOffsetNode(
    bounds(EL.colFx3, mixY - 40, L.nodeW, L.nodeHSmall), 'RGB Shift',
  );
  builder.addNode(colOffId, colOffNode);
  builder.connect(statId, 'output', colOffId, 'input');

  const [rgbAmtId, rgbAmtNode] = factory.makeFloatInNode(
    'RGB Shift Amount', 0.04, 0.0, 0.2,
    bounds(EL.colFx3 - 180, mixY - 40, 130, L.nodeHSmall),
  );
  builder.addNode(rgbAmtId, rgbAmtNode);
  builder.exposeInput(rgbAmtId);
  builder.connect(rgbAmtId, 'output', colOffId, 'amount');

  const [threshId, threshNode] = factory.makeThresholdNode(
    bounds(EL.colFx3, mixY + 40, L.nodeW, L.nodeHSmall), 'Glitch Threshold',
  );
  builder.addNode(threshId, threshNode);
  builder.connect(colOffId, 'output', threshId, 'input');

  const [thAmtId, thAmtNode] = factory.makeFloatInNode(
    'Threshold', 0.3, 0.0, 1.0,
    bounds(EL.colFx3 - 180, mixY + 40, 130, L.nodeHSmall),
  );
  builder.addNode(thAmtId, thAmtNode);
  builder.exposeInput(thAmtId);
  builder.connect(thAmtId, 'output', threshId, 'threshold');

  // -- Random trigger --
  const [randId, randNode] = factory.makeRandomNode(
    bounds(EL.colOsc, mixY + EL.rowSpacing, 130, L.nodeHSmall), 'Glitch Random',
  );
  builder.addNode(randId, randNode);

  // -- Texture Out --
  const [outId, outNode] = factory.makeTextureOutNode(
    bounds(EL.colOut, mixY, 132, L.nodeHSmall),
  );
  builder.addNode(outId, outNode);
  builder.connect(threshId, 'output', outId, 'input');

  addModulationSection(builder, factory, n, shaderNames, features);
  return builder;
}

/**
 * GEOMETRIC topology: ISF → Edge Detect → Transform (rotation) → Repeat (tiling) → Bloom → Out
 * Best for: geometric
 */
function generateGeometricPatch(shaders, setName, patchName, features = null) {
  const n = shaders.length;
  const builder = new WirePatchBuilder(
    patchName,
    `Macroverse Enhanced GEOMETRIC Generator: ${setName}\n` +
    shaders.map((s, i) => `  ${i + 1}. ${s.fixed_name || s.name}`).join('\n') +
    '\nEdge Detect + Transform rotation + Repeat tiling + Bloom',
    'source',
  );
  const factory = builder.factory;
  const shaderNames = shaders.map(s => s.fixed_name || s.name || '?');

  // Comment
  const [cid, cnode] = factory.makeCommentNode(
    `MACROVERSE GEOMETRIC GENERATOR: ${setName.toUpperCase()}\n\n` +
    shaderNames.map((nm, i) => `  [${String.fromCharCode(65 + i)}] ${nm}`).join('\n') +
    '\n\nTopology: ISF → Edge Detect → CrossFader → Transform → Repeat → Bloom → Out\n' +
    'Dashboard: Edge Mix, Rotation, Tile Count, Bloom',
    bounds(EL.colIsf - 50, -350, 2000, 260),
  );
  builder.addNode(cid, cnode);

  // ISF nodes
  const isfIds = [];
  const allInputs = [];
  for (let slot = 0; slot < n; slot++) {
    const shader = shaders[slot];
    const shaderPath = resolveShaderPath(shader);
    const header = parseISFHeader(shaderPath);
    const wireInputs = header ? getWireInputs(header) : [];
    allInputs.push(wireInputs);
    const slotY = slot * EL.rowSpacing;
    const [isfId, isfNode] = factory.makeISFNode(
      shaderPath, wireInputs,
      bounds(EL.colIsf, slotY, L.nodeW, L.nodeHIsf), shaderNames[slot],
    );
    builder.addNode(isfId, isfNode);
    isfIds.push(isfId);
  }

  // Shared + custom params
  for (let si = 0; si < SHARED_PARAMS.length; si++) {
    const sp = SHARED_PARAMS[si];
    const [fid, fnode] = factory.makeFloatInNode(
      sp.label || sp.name, sp.default, sp.min, sp.max,
      bounds(EL.colParams, si * (L.paramH + L.paramGap), 130, L.nodeHSmall),
    );
    builder.addNode(fid, fnode);
    builder.exposeInput(fid);
    for (let i = 0; i < isfIds.length; i++) {
      if (allInputs[i].some(inp => inp.name === sp.name)) {
        builder.connect(fid, 'output', isfIds[i], sp.name);
      }
    }
  }
  for (let slot = 0; slot < n; slot++) {
    const customParams = getFloatParams(allInputs[slot]);
    for (let pi = 0; pi < customParams.length; pi++) {
      const param = customParams[pi];
      const pY = slot * EL.rowSpacing + pi * (L.paramH + L.paramGap);
      const [fid, fnode] = factory.makeFloatInNode(
        `${shaderNames[slot]}: ${param.label || param.name}`,
        param.defaultValue ?? 0.5, param.min ?? 0.0, param.max ?? 1.0,
        bounds(EL.colParams, pY, 150, L.nodeHSmall),
      );
      builder.addNode(fid, fnode);
      builder.exposeInput(fid);
      builder.connect(fid, 'output', isfIds[slot], param.name);
    }
  }

  // -- Edge Detect on each ISF then crossfade --
  const edgeIds = [];
  for (let slot = 0; slot < n; slot++) {
    const slotY = slot * EL.rowSpacing;
    const [edgeId, edgeNode] = factory.makeEdgeDetectNode(
      bounds(EL.colFx1, slotY, L.nodeW, L.nodeHSmall), `Edge ${String.fromCharCode(65 + slot)}`,
    );
    builder.addNode(edgeId, edgeNode);
    builder.connect(isfIds[slot], 'output', edgeId, 'input');
    edgeIds.push(edgeId);
  }

  const [edgeMixId, edgeMixNode] = factory.makeFloatInNode(
    'Edge Mix', 0.6, 0.0, 1.0,
    bounds(EL.colFx1 - 180, 0, 130, L.nodeHSmall),
  );
  builder.addNode(edgeMixId, edgeMixNode);
  builder.exposeInput(edgeMixId);
  for (const eid of edgeIds) {
    builder.connect(edgeMixId, 'output', eid, 'strength');
  }

  // Crossfade edges
  const mixY = ((n - 1) * EL.rowSpacing) / 2;
  let chainInput;
  let chainPort = 'output';

  if (n === 1) {
    chainInput = edgeIds[0];
  } else {
    const [cfId, cfNode] = factory.makeCrossfaderNode(
      bounds(EL.colFx1 + 250, mixY, L.nodeW, L.nodeHCf), 'Edge Mix AB',
    );
    builder.addNode(cfId, cfNode);
    builder.connect(edgeIds[0], 'output', cfId, 'input1');
    builder.connect(edgeIds[n > 1 ? 1 : 0], 'output', cfId, 'input2');
    const [mfId, mfNode] = factory.makeFloatInNode(
      'Mix A/B', 0.5, 0.0, 1.0,
      bounds(EL.colFx1 + 70, mixY, 130, L.nodeHSmall),
    );
    builder.addNode(mfId, mfNode);
    builder.exposeInput(mfId);
    builder.connect(mfId, 'output', cfId, 'mix');
    chainInput = cfId;
    chainPort = 'output0';

    if (n >= 3) {
      const [cf2Id, cf2Node] = factory.makeCrossfaderNode(
        bounds(EL.colFx1 + 250, mixY + EL.rowSpacing, L.nodeW, L.nodeHCf), 'Edge Mix CD',
      );
      builder.addNode(cf2Id, cf2Node);
      builder.connect(edgeIds[2], 'output', cf2Id, 'input1');
      if (n >= 4) builder.connect(edgeIds[3], 'output', cf2Id, 'input2');
      const [mastId, mastNode] = factory.makeCrossfaderNode(
        bounds(EL.colFx2, mixY + EL.rowSpacing / 2, L.nodeW, L.nodeHCf), 'Master',
      );
      builder.addNode(mastId, mastNode);
      builder.connect(cfId, 'output0', mastId, 'input1');
      builder.connect(cf2Id, 'output0', mastId, 'input2');
      const [mmId, mmNode] = factory.makeFloatInNode(
        'Master Mix', 0.5, 0.0, 1.0,
        bounds(EL.colFx2 - 180, mixY + EL.rowSpacing / 2, 130, L.nodeHSmall),
      );
      builder.addNode(mmId, mmNode);
      builder.exposeInput(mmId);
      builder.connect(mmId, 'output', mastId, 'mix');
      chainInput = mastId;
    }
  }

  // -- Transform (rotation) --
  const [txfId, txfNode] = factory.makeTransformNode(
    bounds(EL.colFx2 + 250, mixY - 50, L.nodeW, L.nodeHSmall), 'Geo Transform',
  );
  builder.addNode(txfId, txfNode);
  builder.connect(chainInput, chainPort, txfId, 'input');

  const [rotId, rotNode] = factory.makeFloatInNode(
    'Rotation', 0.0, -1.0, 1.0,
    bounds(EL.colFx2 + 70, mixY - 50, 130, L.nodeHSmall),
  );
  builder.addNode(rotId, rotNode);
  builder.exposeInput(rotId);
  builder.connect(rotId, 'output', txfId, 'rotation');

  // Slow sine for auto-rotation
  const [sineId, sineNode] = factory.makeSineNode(
    0.1, bounds(EL.colOsc, mixY, 130, L.nodeHSmall), 'Auto Rotate',
  );
  builder.addNode(sineId, sineNode);

  // -- Repeat (tiling) --
  const [repId, repNode] = factory.makeRepeatNode(
    bounds(EL.colFx3, mixY, L.nodeW, L.nodeHSmall), 'Tile',
  );
  builder.addNode(repId, repNode);
  builder.connect(txfId, 'output', repId, 'input');

  const [repColId, repColNode] = factory.makeFloatInNode(
    'Tile Columns', 2, 1, 6,
    bounds(EL.colFx3 - 180, mixY - 30, 130, L.nodeHSmall),
  );
  builder.addNode(repColId, repColNode);
  builder.exposeInput(repColId);
  builder.connect(repColId, 'output', repId, 'repeat-x');

  const [repRowId, repRowNode] = factory.makeFloatInNode(
    'Tile Rows', 2, 1, 6,
    bounds(EL.colFx3 - 180, mixY + 30, 130, L.nodeHSmall),
  );
  builder.addNode(repRowId, repRowNode);
  builder.exposeInput(repRowId);
  builder.connect(repRowId, 'output', repId, 'repeat-y');

  // -- Bloom --
  const [bloomId, bloomNode] = factory.makeBloomNode(
    bounds(EL.colFx3 + 250, mixY, L.nodeW, L.nodeHSmall), 'Geo Bloom',
  );
  builder.addNode(bloomId, bloomNode);
  builder.connect(repId, 'output', bloomId, 'input');

  const [bloomSzId, bloomSzNode] = factory.makeFloatInNode(
    'Bloom Size', 0.25, 0.0, 1.0,
    bounds(EL.colFx3 + 70, mixY, 130, L.nodeHSmall),
  );
  builder.addNode(bloomSzId, bloomSzNode);
  builder.exposeInput(bloomSzId);
  builder.connect(bloomSzId, 'output', bloomId, 'threshold');

  // -- Texture Out --
  const [outId, outNode] = factory.makeTextureOutNode(
    bounds(EL.colOut + 200, mixY, 132, L.nodeHSmall),
  );
  builder.addNode(outId, outNode);
  builder.connect(bloomId, 'output', outId, 'input');

  addModulationSection(builder, factory, n, shaderNames, features);
  return builder;
}

/**
 * COLOUR topology: ISF → Hue Rotate → CrossFader → Bloom → Vignette → Out
 * Perlin noise + Sine modulate hue rotation for flowing color shifts.
 * Best for: colour
 */
function generateColourPatch(shaders, setName, patchName, features = null) {
  const n = shaders.length;
  const builder = new WirePatchBuilder(
    patchName,
    `Macroverse Enhanced COLOUR Generator: ${setName}\n` +
    shaders.map((s, i) => `  ${i + 1}. ${s.fixed_name || s.name}`).join('\n') +
    '\nHue Rotate per-shader + Bloom + Vignette with Perlin-driven color drift',
    'source',
  );
  const factory = builder.factory;
  const shaderNames = shaders.map(s => s.fixed_name || s.name || '?');

  // Comment
  const [cid, cnode] = factory.makeCommentNode(
    `MACROVERSE COLOUR GENERATOR: ${setName.toUpperCase()}\n\n` +
    shaderNames.map((nm, i) => `  [${String.fromCharCode(65 + i)}] ${nm}`).join('\n') +
    '\n\nTopology: ISF → Hue Rotate → CrossFader → Bloom → Vignette → Out\n' +
    'Perlin noise modulates hue for organic color drift\n' +
    'Dashboard: Hue Shift per shader, Bloom, Vignette, Color Drift Speed',
    bounds(EL.colIsf - 50, -350, 2000, 280),
  );
  builder.addNode(cid, cnode);

  // ISF + Hue Rotate per shader
  const isfIds = [];
  const hueIds = [];
  const allInputs = [];
  for (let slot = 0; slot < n; slot++) {
    const shader = shaders[slot];
    const shaderPath = resolveShaderPath(shader);
    const header = parseISFHeader(shaderPath);
    const wireInputs = header ? getWireInputs(header) : [];
    allInputs.push(wireInputs);

    const slotY = slot * EL.rowSpacing;
    const [isfId, isfNode] = factory.makeISFNode(
      shaderPath, wireInputs,
      bounds(EL.colIsf, slotY, L.nodeW, L.nodeHIsf), shaderNames[slot],
    );
    builder.addNode(isfId, isfNode);
    isfIds.push(isfId);

    // Hue Rotate per shader
    const [hueId, hueNode] = factory.makeHueRotateNode(
      bounds(EL.colFx1, slotY, L.nodeW, L.nodeHSmall), `Hue ${String.fromCharCode(65 + slot)}`,
    );
    builder.addNode(hueId, hueNode);
    builder.connect(isfId, 'output', hueId, 'input');
    hueIds.push(hueId);

    // Per-shader hue shift control
    const [hueCtrlId, hueCtrlNode] = factory.makeFloatInNode(
      `Hue Shift ${String.fromCharCode(65 + slot)}`, slot * 0.25, 0.0, 1.0,
      bounds(EL.colFx1 - 180, slotY, 130, L.nodeHSmall),
    );
    builder.addNode(hueCtrlId, hueCtrlNode);
    builder.exposeInput(hueCtrlId);
    builder.connect(hueCtrlId, 'output', hueId, 'rotation');
  }

  // Shared + custom params
  for (let si = 0; si < SHARED_PARAMS.length; si++) {
    const sp = SHARED_PARAMS[si];
    const [fid, fnode] = factory.makeFloatInNode(
      sp.label || sp.name, sp.default, sp.min, sp.max,
      bounds(EL.colParams, si * (L.paramH + L.paramGap), 130, L.nodeHSmall),
    );
    builder.addNode(fid, fnode);
    builder.exposeInput(fid);
    for (let i = 0; i < isfIds.length; i++) {
      if (allInputs[i].some(inp => inp.name === sp.name)) {
        builder.connect(fid, 'output', isfIds[i], sp.name);
      }
    }
  }
  for (let slot = 0; slot < n; slot++) {
    const customParams = getFloatParams(allInputs[slot]);
    for (let pi = 0; pi < customParams.length; pi++) {
      const param = customParams[pi];
      const pY = slot * EL.rowSpacing + pi * (L.paramH + L.paramGap);
      const [fid, fnode] = factory.makeFloatInNode(
        `${shaderNames[slot]}: ${param.label || param.name}`,
        param.defaultValue ?? 0.5, param.min ?? 0.0, param.max ?? 1.0,
        bounds(EL.colParams, pY, 150, L.nodeHSmall),
      );
      builder.addNode(fid, fnode);
      builder.exposeInput(fid);
      builder.connect(fid, 'output', isfIds[slot], param.name);
    }
  }

  // -- Crossfade hue-rotated outputs --
  const mixY = ((n - 1) * EL.rowSpacing) / 2;
  let chainInput;
  let chainPort = 'output';

  if (n === 1) {
    chainInput = hueIds[0];
  } else {
    const [cfId, cfNode] = factory.makeCrossfaderNode(
      bounds(EL.colFx2, mixY, L.nodeW, L.nodeHCf), 'Colour Mix',
    );
    builder.addNode(cfId, cfNode);
    builder.connect(hueIds[0], 'output', cfId, 'input1');
    builder.connect(hueIds[n > 1 ? 1 : 0], 'output', cfId, 'input2');
    const [mfId, mfNode] = factory.makeFloatInNode(
      'Colour Mix', 0.5, 0.0, 1.0,
      bounds(EL.colFx2 - 180, mixY, 130, L.nodeHSmall),
    );
    builder.addNode(mfId, mfNode);
    builder.exposeInput(mfId);
    builder.connect(mfId, 'output', cfId, 'mix');
    chainInput = cfId;
    chainPort = 'output0';

    if (n >= 3) {
      const [cf2Id, cf2Node] = factory.makeCrossfaderNode(
        bounds(EL.colFx2, mixY + EL.rowSpacing, L.nodeW, L.nodeHCf), 'Colour Mix 2',
      );
      builder.addNode(cf2Id, cf2Node);
      builder.connect(hueIds[2], 'output', cf2Id, 'input1');
      if (n >= 4) builder.connect(hueIds[3], 'output', cf2Id, 'input2');
      const [mastId, mastNode] = factory.makeCrossfaderNode(
        bounds(EL.colMix, mixY + EL.rowSpacing / 2, L.nodeW, L.nodeHCf), 'Master',
      );
      builder.addNode(mastId, mastNode);
      builder.connect(cfId, 'output0', mastId, 'input1');
      builder.connect(cf2Id, 'output0', mastId, 'input2');
      const [mmId, mmNode] = factory.makeFloatInNode(
        'Master Mix', 0.5, 0.0, 1.0,
        bounds(EL.colMix - 180, mixY + EL.rowSpacing / 2, 130, L.nodeHSmall),
      );
      builder.addNode(mmId, mmNode);
      builder.exposeInput(mmId);
      builder.connect(mmId, 'output', mastId, 'mix');
      chainInput = mastId;
    }
  }

  // -- Bloom --
  const [bloomId, bloomNode] = factory.makeBloomNode(
    bounds(EL.colFx3, mixY - 40, L.nodeW, L.nodeHSmall), 'Colour Bloom',
  );
  builder.addNode(bloomId, bloomNode);
  builder.connect(chainInput, chainPort, bloomId, 'input');

  const [bloomSzId, bloomSzNode] = factory.makeFloatInNode(
    'Bloom Size', 0.35, 0.0, 1.0,
    bounds(EL.colFx3 - 180, mixY - 40, 130, L.nodeHSmall),
  );
  builder.addNode(bloomSzId, bloomSzNode);
  builder.exposeInput(bloomSzId);
  builder.connect(bloomSzId, 'output', bloomId, 'threshold');

  // -- Vignette --
  const [vigId, vigNode] = factory.makeVignetteNode(
    bounds(EL.colFx3, mixY + 40, L.nodeW, L.nodeHSmall), 'Colour Vignette',
  );
  builder.addNode(vigId, vigNode);
  builder.connect(bloomId, 'output', vigId, 'input');

  const [vigAmtId, vigAmtNode] = factory.makeFloatInNode(
    'Vignette', 0.3, 0.0, 1.0,
    bounds(EL.colFx3 - 180, mixY + 40, 130, L.nodeHSmall),
  );
  builder.addNode(vigAmtId, vigAmtNode);
  builder.exposeInput(vigAmtId);
  builder.connect(vigAmtId, 'output', vigId, 'amount');

  // -- Perlin noise for organic color drift --
  const [perlinId, perlinNode] = factory.makePerlinNode(
    bounds(EL.colOsc, mixY, 130, L.nodeHSmall), 'Color Drift',
  );
  builder.addNode(perlinId, perlinNode);

  // -- Texture Out --
  const [outId, outNode] = factory.makeTextureOutNode(
    bounds(EL.colOut, mixY, 132, L.nodeHSmall),
  );
  builder.addNode(outId, outNode);
  builder.connect(vigId, 'output', outId, 'input');

  addModulationSection(builder, factory, n, shaderNames, features);
  return builder;
}

/**
 * Router: pick the right enhanced topology for a VJ set.
 */
function generateEnhancedPatch(shaders, setName, patchName, features = null) {
  const topo = getTopologyForSet(setName);
  switch (topo) {
    case 'feedback':   return generateFeedbackPatch(shaders, setName, patchName, features);
    case 'beat':       return generateBeatPatch(shaders, setName, patchName, features);
    case 'glitch':     return generateGlitchPatch(shaders, setName, patchName, features);
    case 'geometric':  return generateGeometricPatch(shaders, setName, patchName, features);
    case 'colour':     return generateColourPatch(shaders, setName, patchName, features);
    default:           return generateFeedbackPatch(shaders, setName, patchName, features);
  }
}

function validateWire(filePath) {
  const errors = [];
  let data;
  try {
    data = JSON.parse(fs.readFileSync(filePath, 'utf-8'));
  } catch (e) {
    return [`Failed to parse ${path.basename(filePath)}: ${e.message}`];
  }

  const patch = data.patch || {};
  const nodes = patch.nodes || {};
  const nodeIds = new Set(Object.keys(nodes).map(Number));
  const nextId = patch.nextNodeId || 0;

  if (nodeIds.size > 0 && Math.max(...nodeIds) >= nextId) {
    errors.push(`nextNodeId (${nextId}) <= max node ID (${Math.max(...nodeIds)})`);
  }

  for (const conn of (patch.connections || [])) {
    if (!nodeIds.has(conn.from[0])) {
      errors.push(`Connection references non-existent source node ${conn.from[0]}`);
    }
    if (!nodeIds.has(conn.to[0])) {
      errors.push(`Connection references non-existent target node ${conn.to[0]}`);
    }
  }

  for (const root of ((patch.inputOrder || {}).roots || [])) {
    if (!nodeIds.has(root.node)) {
      errors.push(`inputOrder references non-existent node ${root.node}`);
    }
  }

  const devConns = ((data.ui || {}).deviceConnections || {}).input || {};
  for (const nidStr of Object.keys(devConns)) {
    if (!nodeIds.has(Number(nidStr))) {
      errors.push(`deviceConnections references non-existent node ${nidStr}`);
    }
  }

  return errors;
}

// ---------------------------------------------------------------------------
// CLI Arg Parsing
// ---------------------------------------------------------------------------

function parseArgs() {
  const args = process.argv.slice(2);
  const opts = {
    set: null, db: DB_PATH, output: OUTPUT_DIR, dryRun: false, validate: false,
    jsonOutput: false, autoSeed: false, mode: 'standard', interactive: false,
    features: { fft: true, webcam: true, glitch: true, midi: true, fxLevel: 'advanced' },
  };
  for (let i = 0; i < args.length; i++) {
    switch (args[i]) {
      case '--set':       opts.set = args[++i]; break;
      case '--db':        opts.db = args[++i]; break;
      case '--output':    opts.output = args[++i]; break;
      case '--dry-run':   opts.dryRun = true; break;
      case '--validate':  opts.validate = true; break;
      case '--json-output': opts.jsonOutput = true; break;
      case '--auto-seed': opts.autoSeed = true; break;
      case '--mode':      opts.mode = args[++i]; break; // 'standard', 'enhanced', or 'all'
      case '--interactive': opts.interactive = true; break;
      case '--fft':       opts.features.fft = true; break;
      case '--no-fft':    opts.features.fft = false; break;
      case '--webcam':    opts.features.webcam = true; break;
      case '--no-webcam': opts.features.webcam = false; break;
      case '--glitch':    opts.features.glitch = true; break;
      case '--no-glitch': opts.features.glitch = false; break;
      case '--midi':      opts.features.midi = true; break;
      case '--no-midi':   opts.features.midi = false; break;
      case '--fx-level':  opts.features.fxLevel = args[++i] || 'advanced'; break;
      case '--config': {
        // Read JSON config file (used by /api/wire/generate endpoint)
        const configPath = args[++i];
        if (!configPath || !fs.existsSync(configPath)) {
          console.error(`Config file not found: ${configPath}`);
          process.exit(1);
        }
        try {
          const cfg = JSON.parse(fs.readFileSync(configPath, 'utf-8'));
          if (cfg.topology) opts.configTopology = cfg.topology;
          if (cfg.midiPreset) opts.configMidiPreset = cfg.midiPreset;
          if (cfg.outputName) opts.configOutputName = cfg.outputName;
          if (cfg.shaders && Array.isArray(cfg.shaders)) opts.configShaders = cfg.shaders;
          if (cfg.features) {
            if (typeof cfg.features.fft === 'boolean') opts.features.fft = cfg.features.fft;
            if (typeof cfg.features.webcam === 'boolean') opts.features.webcam = cfg.features.webcam;
            if (typeof cfg.features.midi === 'boolean') opts.features.midi = cfg.features.midi;
            if (typeof cfg.features.glitch === 'boolean') opts.features.glitch = cfg.features.glitch;
          }
        } catch (err) {
          console.error(`Failed to parse config file: ${err.message}`);
          process.exit(1);
        }
        break;
      }
      case '--help': case '-h':
        console.log(`Macroverse Wire Patch Generator
================================
Generates Resolume Wire patches (.wire) from VJ sets in macroverse.db.
Each patch contains 1-4 ISF shaders with crossfader, MIDI, FFT, and FX chains.

Usage: node seed-wire-from-sets.js [options]

Options:
  --set <name>         Generate for specific VJ set (default: all)
  --mode <mode>        standard | enhanced | all (default: standard)
  --db <path>          Path to macroverse.db
  --output <dir>       Output directory (default: resolume/)
  --dry-run            Preview without writing files
  --validate           Validate existing .wire files
  --json-output        Structured JSON output for API
  --auto-seed          Auto-assign VJ sets by name heuristics before generating
  --config <file>      JSON config file (topology, shaders, features, outputName)

Feature toggles:
  --fft / --no-fft           Include FFT audio-reactive nodes (default: on)
  --webcam / --no-webcam     Include webcam/video Texture In nodes (default: on)
  --glitch / --no-glitch     Include glitch FX chain in GLITCH topology (default: on)
  --midi / --no-midi         Include MIDI controller nodes (default: on)
  --fx-level <level>         basic (ISF only) | advanced (full FX chain) (default: advanced)
  --interactive              Step-by-step feature selection menu

Related scripts:
  seed-avenue-from-sets.js   Generate Resolume Avenue composition from .wire files
  gen-factory-index.js       Generate factory shader index`);
        process.exit(0);
      default:
        console.error(`Unknown option: ${args[i]}`);
        process.exit(1);
    }
  }
  return opts;
}

// ---------------------------------------------------------------------------
// Interactive Mode
// ---------------------------------------------------------------------------

function askQuestion(rl, question) {
  return new Promise(resolve => rl.question(question, resolve));
}

async function runInteractive(opts) {
  const readline = require('node:readline');
  const rl = readline.createInterface({ input: process.stdin, output: process.stdout });

  console.log('Macroverse Wire Patch Generator');
  console.log('================================\n');

  const setAnswer = await askQuestion(rl,
    `Generate for which set? (all/${VJ_SETS.join('/')}) [${opts.set || 'all'}]: `);
  if (setAnswer.trim() && setAnswer.trim() !== 'all') opts.set = setAnswer.trim();

  const modeAnswer = await askQuestion(rl,
    `Mode? (standard/enhanced/all) [${opts.mode}]: `);
  if (modeAnswer.trim()) opts.mode = modeAnswer.trim();

  console.log('\nFeature toggles:');

  const fftAnswer = await askQuestion(rl,
    `  Include FFT / audio-reactive nodes? (Y/n) [${opts.features.fft ? 'Y' : 'n'}]: `);
  if (fftAnswer.trim().toLowerCase() === 'n') opts.features.fft = false;
  else if (fftAnswer.trim().toLowerCase() === 'y') opts.features.fft = true;

  const webcamAnswer = await askQuestion(rl,
    `  Include webcam / video input nodes? (Y/n) [${opts.features.webcam ? 'Y' : 'n'}]: `);
  if (webcamAnswer.trim().toLowerCase() === 'n') opts.features.webcam = false;
  else if (webcamAnswer.trim().toLowerCase() === 'y') opts.features.webcam = true;

  const glitchAnswer = await askQuestion(rl,
    `  Include glitch FX chain? (Y/n) [${opts.features.glitch ? 'Y' : 'n'}]: `);
  if (glitchAnswer.trim().toLowerCase() === 'n') opts.features.glitch = false;
  else if (glitchAnswer.trim().toLowerCase() === 'y') opts.features.glitch = true;

  const midiAnswer = await askQuestion(rl,
    `  Include MIDI controller nodes? (Y/n) [${opts.features.midi ? 'Y' : 'n'}]: `);
  if (midiAnswer.trim().toLowerCase() === 'n') opts.features.midi = false;
  else if (midiAnswer.trim().toLowerCase() === 'y') opts.features.midi = true;

  const fxAnswer = await askQuestion(rl,
    `  FX level (basic/advanced) [${opts.features.fxLevel}]: `);
  if (fxAnswer.trim()) opts.features.fxLevel = fxAnswer.trim();

  rl.close();

  const f = opts.features;
  console.log(`\nGenerating with: fft=${f.fft ? 'yes' : 'no'} webcam=${f.webcam ? 'yes' : 'no'} ` +
    `glitch=${f.glitch ? 'yes' : 'no'} midi=${f.midi ? 'yes' : 'no'} fx=${f.fxLevel}\n`);
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

async function main() {
  const opts = parseArgs();
  const outputDir = opts.output;

  // Validate mode
  if (opts.validate) {
    const wireFiles = fs.readdirSync(outputDir)
      .filter(f => f.startsWith('vj-') && f.endsWith('.wire'))
      .sort()
      .map(f => path.join(outputDir, f));
    if (wireFiles.length === 0) {
      console.log('No vj-*.wire files found in', outputDir);
      return;
    }
    let allOk = true;
    for (const wf of wireFiles) {
      const errs = validateWire(wf);
      if (errs.length > 0) {
        allOk = false;
        console.log(`FAIL ${path.basename(wf)}:`);
        for (const e of errs) console.log(`  - ${e}`);
      } else {
        console.log(`  OK ${path.basename(wf)}`);
      }
    }
    process.exit(allOk ? 0 : 1);
  }

  // Generation mode

  // ---- Config-driven generation (from /api/wire/generate) ----
  if (opts.configShaders && opts.configShaders.length > 0) {
    const shaders = opts.configShaders.map(p => {
      const absPath = path.isAbsolute(p) ? p : path.join(WORKSPACE, p);
      const name = path.basename(absPath, path.extname(absPath));
      return { path: absPath, name, fixed_name: name, category: '', sets: [], source_root: '' };
    });
    const outputName = opts.configOutputName || `custom-${Date.now()}`;
    const topology = opts.configTopology || 'feedback';
    const outPath = path.join(outputDir, `${outputName}.wire`);
    const setName = 'custom';
    const results = { generated: [], skipped: [], errors: [] };

    try {
      // Choose generation method based on topology
      let patch;
      const groups = [];
      for (let i = 0; i < shaders.length; i += SHADERS_PER_PATCH) {
        groups.push(shaders.slice(i, i + SHADERS_PER_PATCH));
      }
      const group = groups[0] || shaders;

      if (['feedback', 'beat', 'glitch', 'geometric', 'colour'].includes(topology)) {
        // Use enhanced generation with the requested topology
        const origGetTopo = getTopologyForSet;
        // Temporarily override topology selection
        const fakeSet = `vj-${topology === 'colour' ? 'colour' : topology === 'beat' ? 'techno' : topology}`;
        patch = generateEnhancedPatch(group, fakeSet, outputName, opts.features);
      } else {
        // Default: standard mixer topology
        patch = generatePatchForGroup(group, setName, outputName, 0, opts.features);
      }

      if (!opts.dryRun) {
        patch.save(outPath);
        const errs = validateWire(outPath);
        if (errs.length > 0) results.errors.push(...errs);
      }

      results.generated.push({
        set: setName,
        file: outPath,
        shaders: group.length,
        mode: topology,
      });
    } catch (err) {
      results.errors.push(err.message);
    }

    if (opts.jsonOutput) {
      // Return the single-file result format expected by /api/wire/generate
      const gen = results.generated[0] || {};
      console.log(JSON.stringify({
        ok: results.errors.length === 0,
        file: gen.file || outPath,
        shaderCount: gen.shaders || 0,
        errors: results.errors,
      }));
    } else {
      console.log(`WROTE: ${outPath} (${shaders.length} shaders) [${topology}]`);
    }
    return;
  }

  // Interactive mode: prompt for options before generating
  if (opts.interactive) {
    await runInteractive(opts);
  }

  // Auto-seed VJ sets if requested
  if (opts.autoSeed) {
    autoSeedSets(opts.db, opts.jsonOutput);
  }

  let db;
  try {
    db = openDB(opts.db);
  } catch (e) {
    console.error(`ERROR: ${e.message}`);
    process.exit(1);
  }

  const setsToProcess = opts.set ? [opts.set] : VJ_SETS;
  const results = { generated: [], skipped: [], errors: [] };
  const doStandard = opts.mode === 'standard' || opts.mode === 'all';
  const doEnhanced = opts.mode === 'enhanced' || opts.mode === 'all';

  // Show available sets
  if (!opts.jsonOutput) {
    const counts = allSetsWithCounts(db);
    console.log(`Mode: ${opts.mode} | VJ Set ISF shader counts:`);
    for (const s of VJ_SETS) {
      const cnt = counts[s] || 0;
      const marker = (!opts.set || s === opts.set) ? ' *' : '';
      console.log(`  ${s}: ${cnt} ISF shaders${marker}`);
    }
    console.log();
  }

  for (const setName of setsToProcess) {
    const shaders = shadersForSet(db, setName);
    if (shaders.length === 0) {
      if (!opts.jsonOutput) console.log(`  SKIP ${setName}: no ISF shaders found`);
      results.skipped.push(setName);
      continue;
    }

    // Chunk into groups of SHADERS_PER_PATCH
    const groups = [];
    for (let i = 0; i < shaders.length; i += SHADERS_PER_PATCH) {
      groups.push(shaders.slice(i, i + SHADERS_PER_PATCH));
    }

    // ---- Standard patches (mixer topology, same as before) ----
    if (doStandard) {
      for (let idx = 0; idx < groups.length; idx++) {
        const group = groups[idx];
        const suffix = groups.length > 1 ? `-${idx + 1}` : '';
        const patchName = `${setName}${suffix}`;
        const outPath = path.join(outputDir, `${patchName}.wire`);

        if (opts.dryRun) {
          const names = group.map(s => s.fixed_name || s.name || '?');
          if (!opts.jsonOutput) {
            console.log(`  WOULD WRITE: ${patchName}.wire (${group.length} shaders) [standard]`);
            for (const sn of names) console.log(`    - ${sn}`);
          }
          results.generated.push({
            set: setName, file: outPath, shaders: group.length, names, mode: 'standard',
          });
        } else {
          const patch = generatePatchForGroup(group, setName, patchName, idx, opts.features);
          patch.save(outPath);
          const errs = validateWire(outPath);
          if (errs.length > 0) {
            results.errors.push(...errs);
            if (!opts.jsonOutput) {
              console.log(`  WROTE: ${path.basename(outPath)} (${group.length} shaders) [standard] [ERRORS]`);
              for (const e of errs) console.log(`    ! ${e}`);
            }
          } else {
            if (!opts.jsonOutput) {
              console.log(`  WROTE: ${path.basename(outPath)} (${group.length} shaders) [standard]`);
            }
          }
          results.generated.push({ set: setName, file: outPath, shaders: group.length, mode: 'standard' });
        }
      }
    }

    // ---- Enhanced patches (FX topology per mood) ----
    if (doEnhanced) {
      const topo = getTopologyForSet(setName);
      for (let idx = 0; idx < groups.length; idx++) {
        const group = groups[idx];
        const suffix = groups.length > 1 ? `-${idx + 1}` : '';
        const patchName = `${setName}-fx${suffix}`;
        const outPath = path.join(outputDir, `${patchName}.wire`);

        if (opts.dryRun) {
          const names = group.map(s => s.fixed_name || s.name || '?');
          if (!opts.jsonOutput) {
            console.log(`  WOULD WRITE: ${patchName}.wire (${group.length} shaders) [enhanced/${topo}]`);
            for (const sn of names) console.log(`    - ${sn}`);
          }
          results.generated.push({
            set: setName, file: outPath, shaders: group.length, names, mode: 'enhanced', topology: topo,
          });
        } else {
          const patch = generateEnhancedPatch(group, setName, patchName, opts.features);
          patch.save(outPath);
          const errs = validateWire(outPath);
          if (errs.length > 0) {
            results.errors.push(...errs);
            if (!opts.jsonOutput) {
              console.log(`  WROTE: ${path.basename(outPath)} (${group.length} shaders) [enhanced/${topo}] [ERRORS]`);
              for (const e of errs) console.log(`    ! ${e}`);
            }
          } else {
            if (!opts.jsonOutput) {
              console.log(`  WROTE: ${path.basename(outPath)} (${group.length} shaders) [enhanced/${topo}]`);
            }
          }
          results.generated.push({ set: setName, file: outPath, shaders: group.length, mode: 'enhanced', topology: topo });
        }
      }
    }
  }

  db.close();

  if (opts.jsonOutput) {
    console.log(JSON.stringify(results, null, 2));
  } else if (!opts.dryRun) {
    const standardCount = results.generated.filter(g => g.mode === 'standard').length;
    const enhancedCount = results.generated.filter(g => g.mode === 'enhanced').length;
    const total = results.generated.reduce((sum, g) => sum + g.shaders, 0);
    console.log(`\nDone: ${results.generated.length} patch(es), ${total} shader placements`);
    if (standardCount > 0) console.log(`  Standard mixer patches: ${standardCount}`);
    if (enhancedCount > 0) console.log(`  Enhanced FX patches: ${enhancedCount}`);
    if (results.skipped.length > 0) {
      console.log(`Skipped (no ISF shaders): ${results.skipped.join(', ')}`);
    }
    if (results.errors.length > 0) {
      console.log(`Validation errors: ${results.errors.length}`);
    }
  }
}

main().catch(e => { console.error(e.message); process.exit(1); });
