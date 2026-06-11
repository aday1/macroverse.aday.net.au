#!/usr/bin/env node
/**
 * generate-wire-templates.js
 * Creates curated Wire patch templates for Resolume Wire.
 * Each template is a clean, focused starting point for VJ work.
 *
 * Usage: node scripts/generate-wire-templates.js [--output <dir>] [--shaders <dir>]
 */

const fs = require('fs');
const path = require('path');

// ---------------------------------------------------------------------------
// CLI args
// ---------------------------------------------------------------------------
const args = process.argv.slice(2);
let outputDir = path.join(__dirname, '..', 'resolume');
let shaderDir = path.join(__dirname, '..', 'shaders', 'isf');

for (let i = 0; i < args.length; i++) {
  if (args[i] === '--output') outputDir = args[++i];
  if (args[i] === '--shaders') shaderDir = args[++i];
}

// ---------------------------------------------------------------------------
// Wire node class UUIDs
// ---------------------------------------------------------------------------
const CLASS = {
  ISF:          '77697265-4576-4C11-899B-6F11F3275D36',
  TEXTURE_IN:   '77697265-B2A2-4C1C-8C4C-2915D78CC8E9',
  TEXTURE_OUT:  '77697265-BEEA-4D38-8EE5-0EBA4CBD0AEE',
  CROSSFADER:   '77697265-7BAA-481B-8F7C-A32F6DBE1518',
  FLOAT_IN:     '77697265-D235-4A6A-B661-02ABE55C72FF',
  BOOL_IN:      '77697265-999C-4F8B-8B9D-3646DC68AA69',
  COMMENT:      '77697265-3C1D-467D-A722-0FC566538374',
  MIDI_IN:      '77697265-5275-4BB0-97B0-DBEEFD85997B',
  FILTER_CC:    '77697265-2F2C-47A5-8DA4-065DAE816AB2',
  SPECTRUM_IN:  '77697265-0B51-496E-A02A-4269686CB551',
  VIDEO_MIXER:  '77697265-A270-4D60-911C-A88B1BE6369A',
  TRANSFORM:    '77697265-9225-4009-9D2D-5F898E94CC33',
  DELAY:        '77697265-4BEE-483F-9F28-1BA5F21193DB',
  BLOOM:        '77697265-B994-4A9A-94E7-B4E0FA77D8AF',
  BLUR:         '77697265-5F11-465D-9E83-A3CF5A50D8E8',
  HUE_ROTATE:   '77697265-11DE-4F22-B268-C050B2C2BB30',
  PIXELATE:     '77697265-ACD2-4541-B139-88756DC29CA7',
  THRESHOLD:    '77697265-BC76-4025-8195-07B2AA065E67',
  SINE:         '77697265-ED9B-4BEC-B3D4-43AC04731CBA',
  PERLIN:       '77697265-A2EF-44DA-9A81-BB0385C1948C',
  EDGE_DETECT:  '77697265-9DF3-4B94-A36D-42F1C6FBE997',
  VIGNETTE:     '77697265-CE82-4714-8023-1953D9FAB7A9',
  COLOR_OFFSET: '77697265-8B97-4252-9A93-BF8028C5EDEC',
  UV_OFFSET:    '77697265-BB9A-4B60-83F3-D0E4DA6CFF86',
  RIPPLE:       '77697265-D65D-4BFE-AAD6-882493B313FD',
  REPEAT:       '77697265-2F93-4176-B1F9-774B39EC25F5',
  COLORIZE:     '77697265-9518-4E69-899B-66B8C527C6EB',
  TRANSITION:   '77697265-7BAB-426B-1F7C-A32F6FBE1518',
  SAW:          '77697265-F95F-41D8-8FC4-DF0DC56E1051',
  METRONOME:    '77697265-FD65-470F-9F75-ED878267980E',
};

// Correct max-supported version for each Wire node class.
// Must match what the target Resolume Wire version accepts.
const NODE_VERSION = {
  [CLASS.ISF]:          4,
  [CLASS.TEXTURE_IN]:   1,
  [CLASS.TEXTURE_OUT]:  1,
  [CLASS.CROSSFADER]:   2,
  [CLASS.FLOAT_IN]:     4,
  [CLASS.BOOL_IN]:      1,
  [CLASS.COMMENT]:      1,
  [CLASS.MIDI_IN]:      1,
  [CLASS.FILTER_CC]:    1,
  [CLASS.SPECTRUM_IN]:  1,
  [CLASS.VIDEO_MIXER]:  3,
  [CLASS.TRANSFORM]:    3,
  [CLASS.DELAY]:        5,
  [CLASS.BLOOM]:        2,
  [CLASS.BLUR]:         2,
  [CLASS.HUE_ROTATE]:   2,
  [CLASS.PIXELATE]:     1,
  [CLASS.THRESHOLD]:    1,
  [CLASS.SINE]:         1,
  [CLASS.PERLIN]:       1,
  [CLASS.EDGE_DETECT]:  1,
  [CLASS.VIGNETTE]:     1,
  [CLASS.COLOR_OFFSET]: 2,
  [CLASS.UV_OFFSET]:    3,
  [CLASS.RIPPLE]:       1,
  [CLASS.REPEAT]:       1,
  [CLASS.COLORIZE]:     1,
  [CLASS.TRANSITION]:   1,
  [CLASS.SAW]:          1,
  [CLASS.METRONOME]:    1,
};

// ---------------------------------------------------------------------------
// ISF shader metadata parser
// ---------------------------------------------------------------------------
function parseISFMetadata(filePath) {
  const src = fs.readFileSync(filePath, 'utf-8');
  const m = src.match(/\/\*\s*(\{[\s\S]*?\})\s*\*\//);
  if (!m) return null;
  try {
    const meta = JSON.parse(m[1]);
    meta._path = filePath.replace(/\\/g, '/');
    meta._winPath = filePath.replace(/\//g, '\\');
    meta._name = path.basename(filePath, '.fs');
    return meta;
  } catch { return null; }
}

// Scan all ISF shaders
function loadShaders() {
  const files = fs.readdirSync(shaderDir).filter(f => f.endsWith('.fs'));
  const shaders = [];
  for (const f of files) {
    const meta = parseISFMetadata(path.join(shaderDir, f));
    if (meta) shaders.push(meta);
  }
  return shaders;
}

// ---------------------------------------------------------------------------
// Wire patch builder helpers
// ---------------------------------------------------------------------------
let _nextId = 0;

function resetIds() { _nextId = 0; }
function nextId() { return _nextId++; }

function uuid() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
    const r = Math.random() * 16 | 0;
    return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
  });
}

function makeNode(id, classId, name, x, y, constants = {}, attributes = {}, hidden = []) {
  return {
    attributes: {
      instances: { type: 'integer', value: 1 },
      ...attributes,
    },
    bounds: { x, y, width: 160, height: 100 },
    class: { id: classId, version: NODE_VERSION[classId] || 1 },
    clock: 'video',
    color: 'ffff6a00',
    constants: constants,
    hidden: ['instances', ...hidden].sort(),
    name,
    thumbnail_visible: true,
  };
}

function makeISFNode(id, shaderMeta, x, y) {
  const inputs = (shaderMeta.INPUTS || []).map(inp => {
    const entry = {};
    if (inp.TYPE === 'float') {
      entry.defaultValue = inp.DEFAULT !== undefined ? inp.DEFAULT : 0.5;
      if (inp.LABEL && inp.LABEL !== inp.NAME) entry.label = inp.LABEL;
      entry.max = inp.MAX !== undefined ? inp.MAX : 1;
      entry.min = inp.MIN !== undefined ? inp.MIN : 0;
      entry.name = inp.NAME;
      entry.type = 'float';
    } else if (inp.TYPE === 'bool') {
      entry.defaultValue = inp.DEFAULT !== undefined ? !!inp.DEFAULT : false;
      if (inp.LABEL && inp.LABEL !== inp.NAME) entry.label = inp.LABEL;
      entry.name = inp.NAME;
      entry.type = 'bool';
    } else if (inp.TYPE === 'image') {
      entry.name = inp.NAME;
      entry.type = 'texture2d';
    } else if (inp.TYPE === 'color') {
      entry.defaultValue = inp.DEFAULT || [1, 1, 1, 1];
      if (inp.LABEL && inp.LABEL !== inp.NAME) entry.label = inp.LABEL;
      entry.name = inp.NAME;
      entry.type = 'color';
    } else if (inp.TYPE === 'long') {
      entry.defaultValue = inp.DEFAULT !== undefined ? inp.DEFAULT : 0;
      if (inp.LABEL && inp.LABEL !== inp.NAME) entry.label = inp.LABEL;
      entry.name = inp.NAME;
      entry.type = 'long';
    } else {
      entry.name = inp.NAME;
      entry.type = inp.TYPE === 'image' ? 'texture2d' : inp.TYPE;
    }
    return entry;
  });

  const constants = {
    bypass: { type: 'bool', value: false },
    time: { type: 'float', value: 0 },
  };
  for (const inp of shaderMeta.INPUTS || []) {
    if (inp.TYPE === 'float') {
      constants[inp.NAME] = { type: 'float', value: inp.DEFAULT !== undefined ? inp.DEFAULT : 0.5 };
    } else if (inp.TYPE === 'bool') {
      constants[inp.NAME] = { type: 'bool', value: !!inp.DEFAULT };
    } else if (inp.TYPE === 'color') {
      constants[inp.NAME] = { type: 'color', value: inp.DEFAULT || [1, 1, 1, 1] };
    } else if (inp.TYPE === 'image') {
      constants[inp.NAME] = { type: 'texture2d', value: null };
    }
  }

  return makeNode(id, CLASS.ISF, 'ISF: ' + shaderMeta._name, x, y, constants, {
    bitdepth: { type: 'integer', value: 0 },
    'fragment-shader': {
      type: 'resourceIsf',
      value: {
        path: { main: shaderMeta._path, sub: '' },
        value: { inputs },
      },
    },
    'resolution-absolute': { type: 'float2', value: [1920, 1080] },
    'resolution-mode': { type: 'integer', value: 0 },
    'resolution-relative': { type: 'float2', value: [1, 1] },
  }, ['bitdepth', 'bypass', 'resolution-absolute', 'resolution-mode', 'resolution-relative']);
}

function makeTextureOut(id, x, y) {
  return makeNode(id, CLASS.TEXTURE_OUT, 'Texture Out', x, y, {
    input: { type: 'texture2d', value: null },
  });
}

function makeTextureIn(id, x, y) {
  return makeNode(id, CLASS.TEXTURE_IN, 'Texture In', x, y, {
    input: { type: 'texture2d', value: null },
  }, {
    flow: { type: 'flow', value: 'signal' },
  }, ['flow']);
}

function makeFloatIn(id, name, x, y, defaultVal = 0.5, min = 0, max = 1) {
  return makeNode(id, CLASS.FLOAT_IN, name, x, y, {
    input: { type: 'float', value: defaultVal },
  }, {
    flow: { type: 'flow', value: 'signal' },
    'has-max': { type: 'bool', value: true },
    'has-min': { type: 'bool', value: true },
    max: { type: 'float', value: max },
    min: { type: 'float', value: min },
    'options-count': { type: 'integer', value: 0 },
    unit: { type: 'integer', value: 0 },
    widget: { type: 'integer', value: 0 },
  }, ['flow', 'has-max', 'has-min', 'input', 'max', 'min', 'options-count', 'widget']);
}

function makeCrossfader(id, x, y) {
  return makeNode(id, CLASS.CROSSFADER, 'Crossfader', x, y, {
    input1: { type: 'texture2d', value: null },
    input2: { type: 'texture2d', value: null },
    mix: { type: 'float', value: 0.5 },
  }, {
    'input1-dimensions': { type: 'integer', value: 1 },
    'input1-type': { type: 'type', value: 'texture2d' },
    'input2-dimensions': { type: 'integer', value: 1 },
    'input2-type': { type: 'type', value: 'texture2d' },
    'mix-dimensions': { type: 'integer', value: 1 },
  }, ['input1-dimensions', 'input1-type', 'input2-dimensions', 'input2-type', 'mix-dimensions']);
}

function makeBloom(id, x, y) {
  return makeNode(id, CLASS.BLOOM, 'Bloom', x, y, {
    input: { type: 'texture2d', value: null },
    intensity: { type: 'float', value: 0.5 },
    threshold: { type: 'float', value: 0.6 },
  });
}

function makeBlur(id, x, y) {
  return makeNode(id, CLASS.BLUR, 'Blur', x, y, {
    input: { type: 'texture2d', value: null },
    amount: { type: 'float', value: 0.3 },
  });
}

function makeHueRotate(id, x, y) {
  return makeNode(id, CLASS.HUE_ROTATE, 'Hue Rotate', x, y, {
    input: { type: 'texture2d', value: null },
    rotation: { type: 'float', value: 0.0 },
  });
}

function makeTransform(id, x, y) {
  return makeNode(id, CLASS.TRANSFORM, 'Transform', x, y, {
    input: { type: 'texture2d', value: null },
    rotation: { type: 'float', value: 0.0 },
    scale: { type: 'float2', value: [1, 1] },
    translate: { type: 'float2', value: [0, 0] },
  });
}

function makeDelay(id, x, y) {
  return makeNode(id, CLASS.DELAY, 'Delay', x, y, {
    input: { type: 'texture2d', value: null },
    feedback: { type: 'float', value: 0.7 },
  });
}

function makePixelate(id, x, y) {
  return makeNode(id, CLASS.PIXELATE, 'Pixelate', x, y, {
    input: { type: 'texture2d', value: null },
    size: { type: 'float', value: 0.02 },
  });
}

function makeThreshold(id, x, y) {
  return makeNode(id, CLASS.THRESHOLD, 'Threshold', x, y, {
    input: { type: 'texture2d', value: null },
    threshold: { type: 'float', value: 0.5 },
  });
}

function makeSine(id, x, y, freq = 1.0) {
  return makeNode(id, CLASS.SINE, 'Sine LFO', x, y, {
    frequency: { type: 'float', value: freq },
    phase: { type: 'float', value: 0 },
  });
}

function makePerlin(id, x, y) {
  return makeNode(id, CLASS.PERLIN, 'Perlin Noise', x, y, {
    speed: { type: 'float', value: 0.3 },
    scale: { type: 'float', value: 1.0 },
  });
}

function makeComment(id, x, y, text) {
  return makeNode(id, CLASS.COMMENT, text, x, y, {}, {
    text: { type: 'string', value: text },
  });
}

function makeEdgeDetect(id, x, y) {
  return makeNode(id, CLASS.EDGE_DETECT, 'Edge Detect', x, y, {
    input: { type: 'texture2d', value: null },
    strength: { type: 'float', value: 1.0 },
  });
}

function makeVignette(id, x, y) {
  return makeNode(id, CLASS.VIGNETTE, 'Vignette', x, y, {
    input: { type: 'texture2d', value: null },
    amount: { type: 'float', value: 0.5 },
    softness: { type: 'float', value: 0.5 },
  });
}

function makeColorOffset(id, x, y) {
  return makeNode(id, CLASS.COLOR_OFFSET, 'Color Offset', x, y, {
    input: { type: 'texture2d', value: null },
    amount: { type: 'float', value: 0.01 },
    angle: { type: 'float', value: 0.0 },
  });
}

function makeUVOffset(id, x, y) {
  return makeNode(id, CLASS.UV_OFFSET, 'UV Offset', x, y, {
    input: { type: 'texture2d', value: null },
    'offset-x': { type: 'float', value: 0.0 },
    'offset-y': { type: 'float', value: 0.0 },
  });
}

function makeRipple(id, x, y) {
  return makeNode(id, CLASS.RIPPLE, 'Ripple', x, y, {
    input: { type: 'texture2d', value: null },
    amount: { type: 'float', value: 0.3 },
    speed: { type: 'float', value: 1.0 },
  });
}

function makeRepeat(id, x, y) {
  return makeNode(id, CLASS.REPEAT, 'Repeat', x, y, {
    input: { type: 'texture2d', value: null },
    'repeat-x': { type: 'float', value: 2.0 },
    'repeat-y': { type: 'float', value: 2.0 },
  });
}

function makeColorize(id, x, y) {
  return makeNode(id, CLASS.COLORIZE, 'Colorize', x, y, {
    input: { type: 'texture2d', value: null },
    hue: { type: 'float', value: 0.0 },
    saturation: { type: 'float', value: 1.0 },
  });
}

function makeSaw(id, x, y, freq = 1.0) {
  return makeNode(id, CLASS.SAW, 'Saw LFO', x, y, {
    frequency: { type: 'float', value: freq },
    phase: { type: 'float', value: 0 },
  });
}

function makeMetronome(id, x, y) {
  return makeNode(id, CLASS.METRONOME, 'Metronome', x, y, {
    bpm: { type: 'float', value: 120 },
    multiplier: { type: 'float', value: 1 },
  });
}

function makeSpectrumIn(id, x, y) {
  return makeNode(id, CLASS.SPECTRUM_IN, 'Spectrum In (FFT)', x, y, {});
}

function makeMidiIn(id, x, y) {
  return makeNode(id, CLASS.MIDI_IN, 'MIDI In', x, y, {});
}

function makeFilterCC(id, x, y, channel = 0, cc = 1) {
  return makeNode(id, CLASS.FILTER_CC, 'CC ' + cc, x, y, {
    channel: { type: 'integer', value: channel },
    controller: { type: 'integer', value: cc },
    input: { type: 'midi', value: null },
  });
}

function conn(fromId, fromPort, toId, toPort) {
  return { from: [fromId, fromPort], to: [toId, toPort] };
}

// ---------------------------------------------------------------------------
// Wire patch envelope
// ---------------------------------------------------------------------------
function buildPatch(name, description, category, nodes, connections) {
  return {
    formatVersion: { major: 1, minor: 1, patch: 0 },
    patch: {
      connections,
      inputOrder: { groups: {}, roots: [] },
      meta: {
        author: 'Macroverse',
        category,
        deploymentTarget: { branch: 'unknown', name: 'Wire', version: { major: 7, minor: 23, patch: 0 } },
        description,
        displayName: name,
        identifier: uuid(),
        licenseName: '', mail: '',
        note: { text: '', textColorIndex: 1, textSizeMultiplier: 3 },
        originalTarget: { branch: 'unknown', name: 'Wire', version: { major: 7, minor: 23, patch: 0 } },
        quality: 32856,
        resolution: { height: 1080, width: 1920 },
        saveTarget: { branch: '', name: 'Wire', version: { major: 7, minor: 24, patch: 3 } },
        thumbnail: '', url: 'aday@aday.net.au', vendor: 'aday.net.au',
      },
      nextNodeId: _nextId,
      nodes,
      ui: { camera: { x: -400, y: -100, zoom: 0.75 }, selection: [] },
    },
    ui: {
      audio: { routing: { in: {}, out: [] } },
      deviceConnections: { input: {} },
      transport: { bpm: 120, 'time-signature': [4, 4] },
      video: { routing: { out: { 'Display 1': null, 'Display 2': null } } },
    },
  };
}

// ---------------------------------------------------------------------------
// Template definitions
// ---------------------------------------------------------------------------

function tmplSimpleGenerator(shader) {
  resetIds();
  const nodes = {};
  const conns = [];

  const isfId = nextId(); // 0
  const outId = nextId(); // 1

  nodes[isfId] = makeISFNode(isfId, shader, -150, 0);
  nodes[outId] = makeTextureOut(outId, 250, 0);
  conns.push(conn(isfId, 'output', outId, 'input'));

  return buildPatch(
    'Generator: ' + shader._name,
    shader._name + ' - simple generator, paste into Wire and connect',
    'source',
    nodes, conns,
  );
}

function tmplGeneratorWithBloom(shader) {
  resetIds();
  const nodes = {};
  const conns = [];

  const isfId = nextId();
  const bloomId = nextId();
  const outId = nextId();

  nodes[isfId] = makeISFNode(isfId, shader, -200, 0);
  nodes[bloomId] = makeBloom(bloomId, 100, 0);
  nodes[outId] = makeTextureOut(outId, 400, 0);

  conns.push(conn(isfId, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));

  return buildPatch(
    'Generator+Bloom: ' + shader._name,
    shader._name + ' with bloom glow effect',
    'source',
    nodes, conns,
  );
}

function tmplGeneratorFeedback(shader) {
  resetIds();
  const nodes = {};
  const conns = [];

  const isfId = nextId();   // 0
  const xfadeId = nextId(); // 1
  const delayId = nextId(); // 2
  const bloomId = nextId(); // 3
  const outId = nextId();   // 4
  const fbAmt = nextId();   // 5

  nodes[isfId] = makeISFNode(isfId, shader, -300, 0);
  nodes[xfadeId] = makeCrossfader(xfadeId, 0, 0);
  nodes[delayId] = makeDelay(delayId, 250, 0);
  nodes[bloomId] = makeBloom(bloomId, 500, 0);
  nodes[outId] = makeTextureOut(outId, 750, 0);
  nodes[fbAmt] = makeFloatIn(fbAmt, 'Feedback Amount', 0, 200, 0.7, 0, 0.98);

  // ISF → crossfader input1, delay output → crossfader input2 (feedback loop)
  conns.push(conn(isfId, 'output', xfadeId, 'input1'));
  conns.push(conn(delayId, 'output', xfadeId, 'input2'));
  conns.push(conn(fbAmt, 'output', xfadeId, 'mix'));
  conns.push(conn(xfadeId, 'output0', delayId, 'input'));
  conns.push(conn(delayId, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));

  return buildPatch(
    'Feedback: ' + shader._name,
    shader._name + ' with feedback delay loop + bloom. Adjust Feedback Amount to control evolving visuals.',
    'source',
    nodes, conns,
  );
}

function tmplGeneratorBeatSync(shader) {
  resetIds();
  const nodes = {};
  const conns = [];

  const isfId = nextId();       // 0
  const hueId = nextId();       // 1
  const bloomId = nextId();     // 2
  const outId = nextId();       // 3
  const sineId = nextId();      // 4 - LFO for hue
  const intensityId = nextId(); // 5

  nodes[isfId] = makeISFNode(isfId, shader, -300, 0);
  nodes[hueId] = makeHueRotate(hueId, 0, 0);
  nodes[bloomId] = makeBloom(bloomId, 250, 0);
  nodes[outId] = makeTextureOut(outId, 500, 0);
  nodes[sineId] = makeSine(sineId, -100, 200, 0.5);
  nodes[intensityId] = makeFloatIn(intensityId, 'Bloom Intensity', 250, 200, 0.6, 0, 1);

  conns.push(conn(isfId, 'output', hueId, 'input'));
  conns.push(conn(sineId, 'output', hueId, 'rotation'));
  conns.push(conn(hueId, 'output', bloomId, 'input'));
  conns.push(conn(intensityId, 'output', bloomId, 'intensity'));
  conns.push(conn(bloomId, 'output', outId, 'input'));

  return buildPatch(
    'Beat Hue: ' + shader._name,
    shader._name + ' with LFO-driven hue rotation and controllable bloom.',
    'source',
    nodes, conns,
  );
}

function tmplDualMixer(shaderA, shaderB) {
  resetIds();
  const nodes = {};
  const conns = [];

  const isfA = nextId();    // 0
  const isfB = nextId();    // 1
  const xfade = nextId();   // 2
  const bloom = nextId();   // 3
  const out = nextId();     // 4
  const mixCtrl = nextId(); // 5

  nodes[isfA] = makeISFNode(isfA, shaderA, -300, -100);
  nodes[isfB] = makeISFNode(isfB, shaderB, -300, 150);
  nodes[xfade] = makeCrossfader(xfade, 50, 25);
  nodes[bloom] = makeBloom(bloom, 300, 25);
  nodes[out] = makeTextureOut(out, 550, 25);
  nodes[mixCtrl] = makeFloatIn(mixCtrl, 'Mix A/B', 50, 250, 0.5, 0, 1);

  conns.push(conn(isfA, 'output', xfade, 'input1'));
  conns.push(conn(isfB, 'output', xfade, 'input2'));
  conns.push(conn(mixCtrl, 'output', xfade, 'mix'));
  conns.push(conn(xfade, 'output0', bloom, 'input'));
  conns.push(conn(bloom, 'output', out, 'input'));

  return buildPatch(
    'Mixer: ' + shaderA._name + ' + ' + shaderB._name,
    'Crossfade between ' + shaderA._name + ' and ' + shaderB._name + ' with bloom.',
    'mixer',
    nodes, conns,
  );
}

function tmplEffectChain(effectShader) {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();  // 0
  const isfId = nextId();  // 1
  const outId = nextId();  // 2

  nodes[texIn] = makeTextureIn(texIn, -300, 0);
  nodes[isfId] = makeISFNode(isfId, effectShader, 0, 0);
  nodes[outId] = makeTextureOut(outId, 300, 0);

  // Find the image input name
  const imageInput = (effectShader.INPUTS || []).find(i => i.TYPE === 'image');
  const inputPort = imageInput ? imageInput.NAME : 'inputImage';

  conns.push(conn(texIn, 'output', isfId, inputPort));
  conns.push(conn(isfId, 'output', outId, 'input'));

  return buildPatch(
    'Effect: ' + effectShader._name,
    effectShader._name + ' - connect Texture In to your source',
    'effect',
    nodes, conns,
  );
}

function tmplGlitchChain(shader) {
  resetIds();
  const nodes = {};
  const conns = [];

  const isfId = nextId();     // 0
  const pixId = nextId();     // 1
  const transId = nextId();   // 2
  const threshId = nextId();  // 3
  const bloomId = nextId();   // 4
  const outId = nextId();     // 5
  const glitchAmt = nextId(); // 6

  nodes[isfId] = makeISFNode(isfId, shader, -400, 0);
  nodes[pixId] = makePixelate(pixId, -100, 0);
  nodes[transId] = makeTransform(transId, 150, 0);
  nodes[threshId] = makeThreshold(threshId, 400, 0);
  nodes[bloomId] = makeBloom(bloomId, 650, 0);
  nodes[outId] = makeTextureOut(outId, 900, 0);
  nodes[glitchAmt] = makeFloatIn(glitchAmt, 'Glitch Amount', -100, 200, 0.05, 0.001, 0.2);

  conns.push(conn(isfId, 'output', pixId, 'input'));
  conns.push(conn(glitchAmt, 'output', pixId, 'size'));
  conns.push(conn(pixId, 'output', transId, 'input'));
  conns.push(conn(transId, 'output', threshId, 'input'));
  conns.push(conn(threshId, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));

  return buildPatch(
    'Glitch: ' + shader._name,
    shader._name + ' through pixelate → transform → threshold → bloom glitch chain.',
    'source',
    nodes, conns,
  );
}

function tmplColorDrift(shader) {
  resetIds();
  const nodes = {};
  const conns = [];

  const isfId = nextId();    // 0
  const hueId = nextId();    // 1
  const bloomId = nextId();  // 2
  const outId = nextId();    // 3
  const perlinId = nextId(); // 4

  nodes[isfId] = makeISFNode(isfId, shader, -300, 0);
  nodes[hueId] = makeHueRotate(hueId, 0, 0);
  nodes[bloomId] = makeBloom(bloomId, 250, 0);
  nodes[outId] = makeTextureOut(outId, 500, 0);
  nodes[perlinId] = makePerlin(perlinId, -100, 200);

  conns.push(conn(isfId, 'output', hueId, 'input'));
  conns.push(conn(perlinId, 'output', hueId, 'rotation'));
  conns.push(conn(hueId, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));

  return buildPatch(
    'Color Drift: ' + shader._name,
    shader._name + ' with Perlin noise-driven organic hue drift + bloom.',
    'source',
    nodes, conns,
  );
}

function tmplFXBlurBloom() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();    // 0
  const blurId = nextId();   // 1
  const bloomId = nextId();  // 2
  const outId = nextId();    // 3
  const blurAmt = nextId();  // 4

  nodes[texIn] = makeTextureIn(texIn, -300, 0);
  nodes[blurId] = makeBlur(blurId, 0, 0);
  nodes[bloomId] = makeBloom(bloomId, 250, 0);
  nodes[outId] = makeTextureOut(outId, 500, 0);
  nodes[blurAmt] = makeFloatIn(blurAmt, 'Blur Amount', 0, 200, 0.3, 0, 1);

  conns.push(conn(texIn, 'output', blurId, 'input'));
  conns.push(conn(blurAmt, 'output', blurId, 'amount'));
  conns.push(conn(blurId, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));

  return buildPatch(
    'FX: Blur + Bloom',
    'Texture input through blur and bloom chain. Connect your source to Texture In.',
    'effect',
    nodes, conns,
  );
}

function tmplFXGlitch() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();    // 0
  const pixId = nextId();    // 1
  const threshId = nextId(); // 2
  const hueId = nextId();    // 3
  const outId = nextId();    // 4
  const sineId = nextId();   // 5

  nodes[texIn] = makeTextureIn(texIn, -350, 0);
  nodes[pixId] = makePixelate(pixId, -100, 0);
  nodes[threshId] = makeThreshold(threshId, 150, 0);
  nodes[hueId] = makeHueRotate(hueId, 400, 0);
  nodes[outId] = makeTextureOut(outId, 650, 0);
  nodes[sineId] = makeSine(sineId, 200, 200, 2.0);

  conns.push(conn(texIn, 'output', pixId, 'input'));
  conns.push(conn(pixId, 'output', threshId, 'input'));
  conns.push(conn(threshId, 'output', hueId, 'input'));
  conns.push(conn(sineId, 'output', hueId, 'rotation'));
  conns.push(conn(hueId, 'output', outId, 'input'));

  return buildPatch(
    'FX: Glitch Processor',
    'Pixelate → threshold → hue rotate with LFO. Feed any source.',
    'effect',
    nodes, conns,
  );
}

function tmplFXFeedback() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();   // 0
  const xfade = nextId();   // 1
  const delay = nextId();   // 2
  const hue = nextId();     // 3
  const bloom = nextId();   // 4
  const out = nextId();     // 5
  const fbAmt = nextId();   // 6

  nodes[texIn] = makeTextureIn(texIn, -400, 0);
  nodes[xfade] = makeCrossfader(xfade, -100, 0);
  nodes[delay] = makeDelay(delay, 150, 0);
  nodes[hue] = makeHueRotate(hue, 400, 0);
  nodes[bloom] = makeBloom(bloom, 650, 0);
  nodes[out] = makeTextureOut(out, 900, 0);
  nodes[fbAmt] = makeFloatIn(fbAmt, 'Feedback Mix', -100, 200, 0.7, 0, 0.98);

  conns.push(conn(texIn, 'output', xfade, 'input1'));
  conns.push(conn(delay, 'output', xfade, 'input2'));
  conns.push(conn(fbAmt, 'output', xfade, 'mix'));
  conns.push(conn(xfade, 'output0', delay, 'input'));
  conns.push(conn(delay, 'output', hue, 'input'));
  conns.push(conn(hue, 'output', bloom, 'input'));
  conns.push(conn(bloom, 'output', out, 'input'));

  return buildPatch(
    'FX: Feedback Loop',
    'Feedback delay loop with hue drift + bloom. Classic evolving VJ effect.',
    'effect',
    nodes, conns,
  );
}

function tmplMidiGenerator(shader) {
  resetIds();
  const nodes = {};
  const conns = [];

  const isfId = nextId();   // 0
  const bloomId = nextId(); // 1
  const outId = nextId();   // 2
  const midiId = nextId();  // 3
  const cc1Id = nextId();   // 4 - brightness/intensity
  const cc2Id = nextId();   // 5 - bloom
  const hueId = nextId();   // 6
  const cc3Id = nextId();   // 7 - hue

  nodes[isfId] = makeISFNode(isfId, shader, -150, 0);
  nodes[hueId] = makeHueRotate(hueId, 150, 0);
  nodes[bloomId] = makeBloom(bloomId, 400, 0);
  nodes[outId] = makeTextureOut(outId, 650, 0);
  nodes[midiId] = makeMidiIn(midiId, -400, 300);
  nodes[cc1Id] = makeFilterCC(cc1Id, -200, 250, 0, 1);
  nodes[cc2Id] = makeFilterCC(cc2Id, -200, 350, 0, 2);
  nodes[cc3Id] = makeFilterCC(cc3Id, -200, 450, 0, 3);

  conns.push(conn(isfId, 'output', hueId, 'input'));
  conns.push(conn(hueId, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));
  conns.push(conn(midiId, 'output', cc1Id, 'input'));
  conns.push(conn(midiId, 'output', cc2Id, 'input'));
  conns.push(conn(midiId, 'output', cc3Id, 'input'));
  // CC1 → first shader param, CC2 → bloom intensity, CC3 → hue
  const firstFloat = (shader.INPUTS || []).find(i => i.TYPE === 'float');
  if (firstFloat) {
    conns.push(conn(cc1Id, 'output', isfId, firstFloat.NAME));
  }
  conns.push(conn(cc2Id, 'output', bloomId, 'intensity'));
  conns.push(conn(cc3Id, 'output', hueId, 'rotation'));

  return buildPatch(
    'MIDI: ' + shader._name,
    shader._name + ' with MIDI CC1/CC2/CC3 mapped to param, bloom, hue.',
    'source',
    nodes, conns,
  );
}

function tmplFFTGenerator(shader) {
  resetIds();
  const nodes = {};
  const conns = [];

  const isfId = nextId();    // 0
  const bloomId = nextId();  // 1
  const outId = nextId();    // 2
  const fftId = nextId();    // 3

  nodes[isfId] = makeISFNode(isfId, shader, -150, 0);
  nodes[bloomId] = makeBloom(bloomId, 200, 0);
  nodes[outId] = makeTextureOut(outId, 450, 0);
  nodes[fftId] = makeSpectrumIn(fftId, -400, 200);

  conns.push(conn(isfId, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));
  // FFT → first float param of shader
  const firstFloat = (shader.INPUTS || []).find(i => i.TYPE === 'float');
  if (firstFloat) {
    conns.push(conn(fftId, 'output', isfId, firstFloat.NAME));
  }

  return buildPatch(
    'FFT: ' + shader._name,
    shader._name + ' driven by audio FFT spectrum input + bloom.',
    'source',
    nodes, conns,
  );
}

// ---------------------------------------------------------------------------
// New FX Effect Templates (Texture In → FX → Texture Out)
// ---------------------------------------------------------------------------

function tmplFXEdgeDetect() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const edgeId = nextId();
  const bloomId = nextId();
  const outId = nextId();
  const strengthId = nextId();

  nodes[texIn] = makeTextureIn(texIn, -300, 0);
  nodes[edgeId] = makeEdgeDetect(edgeId, 0, 0);
  nodes[bloomId] = makeBloom(bloomId, 250, 0);
  nodes[outId] = makeTextureOut(outId, 500, 0);
  nodes[strengthId] = makeFloatIn(strengthId, 'Edge Strength', 0, 200, 1.0, 0, 2);

  conns.push(conn(texIn, 'output', edgeId, 'input'));
  conns.push(conn(strengthId, 'output', edgeId, 'strength'));
  conns.push(conn(edgeId, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));

  return buildPatch('FX: Edge Detection', 'Edge detection with bloom glow. Great for wireframe looks.', 'effect', nodes, conns);
}

function tmplFXColorOffsetRGB() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const colorOff = nextId();
  const outId = nextId();
  const amtId = nextId();
  const sineId = nextId();

  nodes[texIn] = makeTextureIn(texIn, -350, 0);
  nodes[colorOff] = makeColorOffset(colorOff, 0, 0);
  nodes[outId] = makeTextureOut(outId, 300, 0);
  nodes[amtId] = makeFloatIn(amtId, 'RGB Split Amount', 0, 200, 0.01, 0, 0.05);
  nodes[sineId] = makeSine(sineId, -150, 200, 0.3);

  conns.push(conn(texIn, 'output', colorOff, 'input'));
  conns.push(conn(amtId, 'output', colorOff, 'amount'));
  conns.push(conn(sineId, 'output', colorOff, 'angle'));
  conns.push(conn(colorOff, 'output', outId, 'input'));

  return buildPatch('FX: RGB Color Offset', 'Chromatic aberration / RGB split with rotating angle for VHS/glitch look.', 'effect', nodes, conns);
}

function tmplFXHueRotateLFO() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const hueId = nextId();
  const outId = nextId();
  const sineId = nextId();
  const speedId = nextId();

  nodes[texIn] = makeTextureIn(texIn, -350, 0);
  nodes[hueId] = makeHueRotate(hueId, 0, 0);
  nodes[outId] = makeTextureOut(outId, 300, 0);
  nodes[sineId] = makeSine(sineId, -150, 200, 0.25);
  nodes[speedId] = makeFloatIn(speedId, 'Rotation Speed', -150, 300, 0.25, 0.01, 4.0);

  conns.push(conn(texIn, 'output', hueId, 'input'));
  conns.push(conn(sineId, 'output', hueId, 'rotation'));
  conns.push(conn(speedId, 'output', sineId, 'frequency'));
  conns.push(conn(hueId, 'output', outId, 'input'));

  return buildPatch('FX: Hue Rotate LFO', 'Smooth rainbow hue cycling with adjustable speed. Great for psychedelic effects.', 'effect', nodes, conns);
}

function tmplFXDelayFeedbackHue() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const xfade = nextId();
  const delay = nextId();
  const hue = nextId();
  const colorOff = nextId();
  const bloom = nextId();
  const out = nextId();
  const fbAmt = nextId();

  nodes[texIn] = makeTextureIn(texIn, -500, 0);
  nodes[xfade] = makeCrossfader(xfade, -200, 0);
  nodes[delay] = makeDelay(delay, 50, 0);
  nodes[hue] = makeHueRotate(hue, 300, 0);
  nodes[colorOff] = makeColorOffset(colorOff, 550, 0);
  nodes[bloom] = makeBloom(bloom, 800, 0);
  nodes[out] = makeTextureOut(out, 1050, 0);
  nodes[fbAmt] = makeFloatIn(fbAmt, 'Feedback', -200, 200, 0.8, 0, 0.98);

  conns.push(conn(texIn, 'output', xfade, 'input1'));
  conns.push(conn(delay, 'output', xfade, 'input2'));
  conns.push(conn(fbAmt, 'output', xfade, 'mix'));
  conns.push(conn(xfade, 'output0', delay, 'input'));
  conns.push(conn(delay, 'output', hue, 'input'));
  conns.push(conn(hue, 'output', colorOff, 'input'));
  conns.push(conn(colorOff, 'output', bloom, 'input'));
  conns.push(conn(bloom, 'output', out, 'input'));

  return buildPatch('FX: Delay RGB Feedback', 'Feedback delay with hue rotation and chromatic split for trippy trails.', 'effect', nodes, conns);
}

function tmplFXShiftGlitch() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const uvOff = nextId();
  const pixId = nextId();
  const colorOff = nextId();
  const threshId = nextId();
  const outId = nextId();
  const sawId = nextId();
  const glitchAmt = nextId();

  nodes[texIn] = makeTextureIn(texIn, -500, 0);
  nodes[uvOff] = makeUVOffset(uvOff, -200, 0);
  nodes[pixId] = makePixelate(pixId, 50, 0);
  nodes[colorOff] = makeColorOffset(colorOff, 300, 0);
  nodes[threshId] = makeThreshold(threshId, 550, 0);
  nodes[outId] = makeTextureOut(outId, 800, 0);
  nodes[sawId] = makeSaw(sawId, -350, 200, 3.0);
  nodes[glitchAmt] = makeFloatIn(glitchAmt, 'Glitch Intensity', -200, 300, 0.05, 0.001, 0.2);

  conns.push(conn(texIn, 'output', uvOff, 'input'));
  conns.push(conn(sawId, 'output', uvOff, 'offset-x'));
  conns.push(conn(uvOff, 'output', pixId, 'input'));
  conns.push(conn(glitchAmt, 'output', pixId, 'size'));
  conns.push(conn(pixId, 'output', colorOff, 'input'));
  conns.push(conn(colorOff, 'output', threshId, 'input'));
  conns.push(conn(threshId, 'output', outId, 'input'));

  return buildPatch('FX: Shift Glitch', 'UV shift + pixelate + RGB split + threshold for digital glitch/databend look.', 'effect', nodes, conns);
}

function tmplFXRippleBloom() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const ripId = nextId();
  const bloomId = nextId();
  const outId = nextId();
  const amtId = nextId();

  nodes[texIn] = makeTextureIn(texIn, -300, 0);
  nodes[ripId] = makeRipple(ripId, 0, 0);
  nodes[bloomId] = makeBloom(bloomId, 250, 0);
  nodes[outId] = makeTextureOut(outId, 500, 0);
  nodes[amtId] = makeFloatIn(amtId, 'Ripple Amount', 0, 200, 0.3, 0, 1);

  conns.push(conn(texIn, 'output', ripId, 'input'));
  conns.push(conn(amtId, 'output', ripId, 'amount'));
  conns.push(conn(ripId, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));

  return buildPatch('FX: Ripple + Bloom', 'Water ripple distortion with bloom glow. Organic liquid feel.', 'effect', nodes, conns);
}

function tmplFXVignetteColorize() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const vigId = nextId();
  const colId = nextId();
  const outId = nextId();
  const hueCtrl = nextId();
  const sineId = nextId();

  nodes[texIn] = makeTextureIn(texIn, -350, 0);
  nodes[vigId] = makeVignette(vigId, -50, 0);
  nodes[colId] = makeColorize(colId, 200, 0);
  nodes[outId] = makeTextureOut(outId, 450, 0);
  nodes[hueCtrl] = makeFloatIn(hueCtrl, 'Color Hue', 200, 200, 0.0, 0, 1);
  nodes[sineId] = makeSine(sineId, -50, 200, 0.1);

  conns.push(conn(texIn, 'output', vigId, 'input'));
  conns.push(conn(vigId, 'output', colId, 'input'));
  conns.push(conn(sineId, 'output', colId, 'hue'));
  conns.push(conn(colId, 'output', outId, 'input'));

  return buildPatch('FX: Vignette + Colorize', 'Cinematic vignette with colour tinting. Smooth hue drift via LFO.', 'effect', nodes, conns);
}

function tmplFXRepeatKaleidoscope() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const repId = nextId();
  const transId = nextId();
  const bloomId = nextId();
  const outId = nextId();
  const sineId = nextId();

  nodes[texIn] = makeTextureIn(texIn, -400, 0);
  nodes[repId] = makeRepeat(repId, -100, 0);
  nodes[transId] = makeTransform(transId, 150, 0);
  nodes[bloomId] = makeBloom(bloomId, 400, 0);
  nodes[outId] = makeTextureOut(outId, 650, 0);
  nodes[sineId] = makeSine(sineId, -100, 200, 0.2);

  conns.push(conn(texIn, 'output', repId, 'input'));
  conns.push(conn(repId, 'output', transId, 'input'));
  conns.push(conn(sineId, 'output', transId, 'rotation'));
  conns.push(conn(transId, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));

  return buildPatch('FX: Repeat Kaleidoscope', 'Tile repeat + slow rotation + bloom for kaleidoscopic VJ effect.', 'effect', nodes, conns);
}

function tmplFXEdgeBloomHue() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const edgeId = nextId();
  const hueId = nextId();
  const bloomId = nextId();
  const outId = nextId();
  const perlinId = nextId();

  nodes[texIn] = makeTextureIn(texIn, -400, 0);
  nodes[edgeId] = makeEdgeDetect(edgeId, -100, 0);
  nodes[hueId] = makeHueRotate(hueId, 150, 0);
  nodes[bloomId] = makeBloom(bloomId, 400, 0);
  nodes[outId] = makeTextureOut(outId, 650, 0);
  nodes[perlinId] = makePerlin(perlinId, -100, 200);

  conns.push(conn(texIn, 'output', edgeId, 'input'));
  conns.push(conn(edgeId, 'output', hueId, 'input'));
  conns.push(conn(perlinId, 'output', hueId, 'rotation'));
  conns.push(conn(hueId, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));

  return buildPatch('FX: Edge + Bloom + Hue', 'Edge detection with wandering color and bloom glow. Neon wireframe look.', 'effect', nodes, conns);
}

function tmplFXBPMStrobe() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const xfade = nextId();
  const outId = nextId();
  const sawId = nextId();
  const threshId = nextId();
  const bpmId = nextId();

  nodes[texIn] = makeTextureIn(texIn, -400, 0);
  nodes[xfade] = makeCrossfader(xfade, 100, 0);
  nodes[outId] = makeTextureOut(outId, 350, 0);
  nodes[sawId] = makeSaw(sawId, -200, 200, 2.0);
  nodes[threshId] = makeThreshold(threshId, -100, 0);
  nodes[bpmId] = makeFloatIn(bpmId, 'Strobe Rate', -200, 300, 2.0, 0.25, 16.0);

  // Saw LFO → Threshold creates hard on/off strobe
  conns.push(conn(bpmId, 'output', sawId, 'frequency'));
  conns.push(conn(texIn, 'output', xfade, 'input1'));
  conns.push(conn(texIn, 'output', threshId, 'input'));
  conns.push(conn(sawId, 'output', xfade, 'mix'));
  conns.push(conn(xfade, 'output0', outId, 'input'));

  return buildPatch('FX: BPM Strobe', 'Beat-synced strobe flash using Saw LFO. Adjust rate to match BPM.', 'effect', nodes, conns);
}

function tmplFXDelayRGBSplit() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const delay = nextId();
  const colorOff = nextId();
  const hueId = nextId();
  const bloomId = nextId();
  const outId = nextId();
  const splitAmt = nextId();
  const sineId = nextId();

  nodes[texIn] = makeTextureIn(texIn, -500, 0);
  nodes[delay] = makeDelay(delay, -200, 0);
  nodes[colorOff] = makeColorOffset(colorOff, 50, 0);
  nodes[hueId] = makeHueRotate(hueId, 300, 0);
  nodes[bloomId] = makeBloom(bloomId, 550, 0);
  nodes[outId] = makeTextureOut(outId, 800, 0);
  nodes[splitAmt] = makeFloatIn(splitAmt, 'RGB Split', 50, 200, 0.015, 0, 0.05);
  nodes[sineId] = makeSine(sineId, 100, 300, 0.5);

  conns.push(conn(texIn, 'output', delay, 'input'));
  conns.push(conn(delay, 'output', colorOff, 'input'));
  conns.push(conn(splitAmt, 'output', colorOff, 'amount'));
  conns.push(conn(sineId, 'output', colorOff, 'angle'));
  conns.push(conn(colorOff, 'output', hueId, 'input'));
  conns.push(conn(hueId, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));

  return buildPatch('FX: Delay + RGB Split', 'Frame delay + chromatic split + hue shift + bloom for psychedelic trail effect.', 'effect', nodes, conns);
}

function tmplFXMegaChain() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const edgeId = nextId();
  const hueId = nextId();
  const colorOff = nextId();
  const xfade = nextId();
  const delay = nextId();
  const bloomId = nextId();
  const vigId = nextId();
  const outId = nextId();
  const perlinId = nextId();
  const fbAmt = nextId();
  const dryWet = nextId();

  nodes[texIn] = makeTextureIn(texIn, -600, 0);
  nodes[edgeId] = makeEdgeDetect(edgeId, -300, 0);
  nodes[hueId] = makeHueRotate(hueId, -50, 0);
  nodes[colorOff] = makeColorOffset(colorOff, 200, 0);
  nodes[xfade] = makeCrossfader(xfade, 450, 0);
  nodes[delay] = makeDelay(delay, 700, 0);
  nodes[bloomId] = makeBloom(bloomId, 950, 0);
  nodes[vigId] = makeVignette(vigId, 1200, 0);
  nodes[outId] = makeTextureOut(outId, 1450, 0);
  nodes[perlinId] = makePerlin(perlinId, -50, 250);
  nodes[fbAmt] = makeFloatIn(fbAmt, 'Feedback', 450, 250, 0.6, 0, 0.95);
  nodes[dryWet] = makeFloatIn(dryWet, 'Edge Mix', -300, 250, 0.5, 0, 1);

  conns.push(conn(texIn, 'output', edgeId, 'input'));
  conns.push(conn(edgeId, 'output', hueId, 'input'));
  conns.push(conn(perlinId, 'output', hueId, 'rotation'));
  conns.push(conn(hueId, 'output', colorOff, 'input'));
  conns.push(conn(colorOff, 'output', xfade, 'input1'));
  conns.push(conn(delay, 'output', xfade, 'input2'));
  conns.push(conn(fbAmt, 'output', xfade, 'mix'));
  conns.push(conn(xfade, 'output0', delay, 'input'));
  conns.push(conn(delay, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', vigId, 'input'));
  conns.push(conn(vigId, 'output', outId, 'input'));

  return buildPatch('FX: Mega Chain', 'Edge detect → Hue → RGB split → Feedback delay → Bloom → Vignette. The ultimate VJ effect chain.', 'effect', nodes, conns);
}

// ---------------------------------------------------------------------------
// APC40 MKII MIDI-Controlled FX Effects
// ---------------------------------------------------------------------------

function tmplFXMidiHueBloom() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const hueId = nextId();
  const bloomId = nextId();
  const outId = nextId();
  const midiId = nextId();
  const cc16 = nextId();   // APC40 Knob 1 → hue rotation
  const cc17 = nextId();   // APC40 Knob 2 → bloom intensity
  const cc18 = nextId();   // APC40 Knob 3 → bloom threshold

  nodes[texIn] = makeTextureIn(texIn, -400, 0);
  nodes[hueId] = makeHueRotate(hueId, -50, 0);
  nodes[bloomId] = makeBloom(bloomId, 200, 0);
  nodes[outId] = makeTextureOut(outId, 450, 0);
  nodes[midiId] = makeMidiIn(midiId, -500, 300);
  nodes[cc16] = makeFilterCC(cc16, -300, 250, 0, 16);
  nodes[cc17] = makeFilterCC(cc17, -300, 350, 0, 17);
  nodes[cc18] = makeFilterCC(cc18, -300, 450, 0, 18);

  conns.push(conn(texIn, 'output', hueId, 'input'));
  conns.push(conn(hueId, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));
  conns.push(conn(midiId, 'output', cc16, 'input'));
  conns.push(conn(midiId, 'output', cc17, 'input'));
  conns.push(conn(midiId, 'output', cc18, 'input'));
  conns.push(conn(cc16, 'output', hueId, 'rotation'));
  conns.push(conn(cc17, 'output', bloomId, 'intensity'));
  conns.push(conn(cc18, 'output', bloomId, 'threshold'));

  return buildPatch('FX: MIDI Hue + Bloom', 'APC40 MKII knobs 1-3 control hue rotation, bloom intensity, bloom threshold.', 'effect', nodes, conns);
}

function tmplFXMidiGlitchRack() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const pixId = nextId();
  const colorOff = nextId();
  const uvOff = nextId();
  const bloomId = nextId();
  const outId = nextId();
  const midiId = nextId();
  const cc16 = nextId();   // Knob 1 → pixelate size
  const cc17 = nextId();   // Knob 2 → RGB split amount
  const cc18 = nextId();   // Knob 3 → UV distort
  const cc19 = nextId();   // Knob 4 → bloom

  nodes[texIn] = makeTextureIn(texIn, -500, 0);
  nodes[uvOff] = makeUVOffset(uvOff, -200, 0);
  nodes[pixId] = makePixelate(pixId, 50, 0);
  nodes[colorOff] = makeColorOffset(colorOff, 300, 0);
  nodes[bloomId] = makeBloom(bloomId, 550, 0);
  nodes[outId] = makeTextureOut(outId, 800, 0);
  nodes[midiId] = makeMidiIn(midiId, -600, 350);
  nodes[cc16] = makeFilterCC(cc16, -400, 250, 0, 16);
  nodes[cc17] = makeFilterCC(cc17, -400, 350, 0, 17);
  nodes[cc18] = makeFilterCC(cc18, -400, 450, 0, 18);
  nodes[cc19] = makeFilterCC(cc19, -400, 550, 0, 19);

  conns.push(conn(texIn, 'output', uvOff, 'input'));
  conns.push(conn(uvOff, 'output', pixId, 'input'));
  conns.push(conn(pixId, 'output', colorOff, 'input'));
  conns.push(conn(colorOff, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));
  conns.push(conn(midiId, 'output', cc16, 'input'));
  conns.push(conn(midiId, 'output', cc17, 'input'));
  conns.push(conn(midiId, 'output', cc18, 'input'));
  conns.push(conn(midiId, 'output', cc19, 'input'));
  conns.push(conn(cc16, 'output', pixId, 'size'));
  conns.push(conn(cc17, 'output', colorOff, 'amount'));
  conns.push(conn(cc18, 'output', uvOff, 'offset-x'));
  conns.push(conn(cc19, 'output', bloomId, 'intensity'));

  return buildPatch('FX: MIDI Glitch Rack', 'APC40 MKII knobs 1-4: pixelate, RGB split, UV shift, bloom. Full glitch control.', 'effect', nodes, conns);
}

function tmplFXMidiFeedbackDelay() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const xfade = nextId();
  const delay = nextId();
  const hue = nextId();
  const bloom = nextId();
  const out = nextId();
  const midiId = nextId();
  const cc16 = nextId();   // Knob 1 → feedback amount
  const cc17 = nextId();   // Knob 2 → hue rotation
  const cc18 = nextId();   // Knob 3 → bloom

  nodes[texIn] = makeTextureIn(texIn, -500, 0);
  nodes[xfade] = makeCrossfader(xfade, -200, 0);
  nodes[delay] = makeDelay(delay, 50, 0);
  nodes[hue] = makeHueRotate(hue, 300, 0);
  nodes[bloom] = makeBloom(bloom, 550, 0);
  nodes[out] = makeTextureOut(out, 800, 0);
  nodes[midiId] = makeMidiIn(midiId, -600, 350);
  nodes[cc16] = makeFilterCC(cc16, -400, 250, 0, 16);
  nodes[cc17] = makeFilterCC(cc17, -400, 350, 0, 17);
  nodes[cc18] = makeFilterCC(cc18, -400, 450, 0, 18);

  conns.push(conn(texIn, 'output', xfade, 'input1'));
  conns.push(conn(delay, 'output', xfade, 'input2'));
  conns.push(conn(cc16, 'output', xfade, 'mix'));
  conns.push(conn(xfade, 'output0', delay, 'input'));
  conns.push(conn(delay, 'output', hue, 'input'));
  conns.push(conn(hue, 'output', bloom, 'input'));
  conns.push(conn(bloom, 'output', out, 'input'));
  conns.push(conn(midiId, 'output', cc16, 'input'));
  conns.push(conn(midiId, 'output', cc17, 'input'));
  conns.push(conn(midiId, 'output', cc18, 'input'));
  conns.push(conn(cc17, 'output', hue, 'rotation'));
  conns.push(conn(cc18, 'output', bloom, 'intensity'));

  return buildPatch('FX: MIDI Feedback Delay', 'APC40 MKII knobs: feedback mix, hue drift, bloom. Evolving trail effect with MIDI control.', 'effect', nodes, conns);
}

function tmplFXWebcamMixer() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn1 = nextId();   // Webcam / external source
  const texIn2 = nextId();   // Second source (layer below)
  const xfade = nextId();
  const hueId = nextId();
  const bloomId = nextId();
  const outId = nextId();
  const mixCtrl = nextId();
  const sineId = nextId();
  const commentId = nextId();

  nodes[texIn1] = makeTextureIn(texIn1, -400, -100);
  nodes[texIn2] = makeTextureIn(texIn2, -400, 150);
  nodes[xfade] = makeCrossfader(xfade, -50, 25);
  nodes[hueId] = makeHueRotate(hueId, 200, 25);
  nodes[bloomId] = makeBloom(bloomId, 450, 25);
  nodes[outId] = makeTextureOut(outId, 700, 25);
  nodes[mixCtrl] = makeFloatIn(mixCtrl, 'Webcam Mix', -50, 250, 0.5, 0, 1);
  nodes[sineId] = makeSine(sineId, 50, 250, 0.15);
  nodes[commentId] = makeComment(commentId, -400, -200, 'Connect Texture In 1 to webcam, Texture In 2 to another source');

  conns.push(conn(texIn1, 'output', xfade, 'input1'));
  conns.push(conn(texIn2, 'output', xfade, 'input2'));
  conns.push(conn(mixCtrl, 'output', xfade, 'mix'));
  conns.push(conn(xfade, 'output0', hueId, 'input'));
  conns.push(conn(sineId, 'output', hueId, 'rotation'));
  conns.push(conn(hueId, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));

  return buildPatch('FX: Webcam Mixer', 'Mix webcam with another source, add hue drift and bloom. Dual Texture In for layering.', 'effect', nodes, conns);
}

function tmplFXPixelateBPM() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const pixId = nextId();
  const bloomId = nextId();
  const outId = nextId();
  const metroId = nextId();
  const sineId = nextId();

  nodes[texIn] = makeTextureIn(texIn, -400, 0);
  nodes[pixId] = makePixelate(pixId, 0, 0);
  nodes[bloomId] = makeBloom(bloomId, 250, 0);
  nodes[outId] = makeTextureOut(outId, 500, 0);
  nodes[metroId] = makeMetronome(metroId, -300, 200);
  nodes[sineId] = makeSine(sineId, -100, 200, 2.0);

  conns.push(conn(texIn, 'output', pixId, 'input'));
  conns.push(conn(sineId, 'output', pixId, 'size'));
  conns.push(conn(pixId, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));

  return buildPatch('FX: Pixelate BPM', 'BPM-synced pixelation that pulses with the beat. Retro digital VJ look.', 'effect', nodes, conns);
}

function tmplFXTransformMirror() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const transId = nextId();
  const repId = nextId();
  const hueId = nextId();
  const bloomId = nextId();
  const outId = nextId();
  const sineId = nextId();
  const perlinId = nextId();

  nodes[texIn] = makeTextureIn(texIn, -500, 0);
  nodes[transId] = makeTransform(transId, -200, 0);
  nodes[repId] = makeRepeat(repId, 50, 0);
  nodes[hueId] = makeHueRotate(hueId, 300, 0);
  nodes[bloomId] = makeBloom(bloomId, 550, 0);
  nodes[outId] = makeTextureOut(outId, 800, 0);
  nodes[sineId] = makeSine(sineId, -200, 200, 0.1);
  nodes[perlinId] = makePerlin(perlinId, 150, 200);

  conns.push(conn(texIn, 'output', transId, 'input'));
  conns.push(conn(sineId, 'output', transId, 'rotation'));
  conns.push(conn(transId, 'output', repId, 'input'));
  conns.push(conn(repId, 'output', hueId, 'input'));
  conns.push(conn(perlinId, 'output', hueId, 'rotation'));
  conns.push(conn(hueId, 'output', bloomId, 'input'));
  conns.push(conn(bloomId, 'output', outId, 'input'));

  return buildPatch('FX: Transform Mirror', 'Rotating mirror tile with colour drift. Kaleidoscopic transformation effect.', 'effect', nodes, conns);
}

function tmplFXFFTReactive() {
  resetIds();
  const nodes = {};
  const conns = [];

  const texIn = nextId();
  const bloomId = nextId();
  const hueId = nextId();
  const pixId = nextId();
  const outId = nextId();
  const fftId = nextId();

  nodes[texIn] = makeTextureIn(texIn, -400, 0);
  nodes[hueId] = makeHueRotate(hueId, -100, 0);
  nodes[pixId] = makePixelate(pixId, 150, 0);
  nodes[bloomId] = makeBloom(bloomId, 400, 0);
  nodes[outId] = makeTextureOut(outId, 650, 0);
  nodes[fftId] = makeSpectrumIn(fftId, -400, 250);

  conns.push(conn(texIn, 'output', hueId, 'input'));
  conns.push(conn(fftId, 'output', hueId, 'rotation'));
  conns.push(conn(hueId, 'output', pixId, 'input'));
  conns.push(conn(fftId, 'output', pixId, 'size'));
  conns.push(conn(pixId, 'output', bloomId, 'input'));
  conns.push(conn(fftId, 'output', bloomId, 'intensity'));
  conns.push(conn(bloomId, 'output', outId, 'input'));

  return buildPatch('FX: FFT Audio Reactive', 'Audio spectrum drives hue rotation, pixelation, and bloom. Reacts to music in real-time.', 'effect', nodes, conns);
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------
function main() {
  const shaders = loadShaders();
  if (shaders.length === 0) {
    console.error('No ISF shaders found in', shaderDir);
    process.exit(1);
  }

  console.log('Found', shaders.length, 'ISF shaders');

  // Separate sources and effects
  const effects = shaders.filter(s => (s.INPUTS || []).some(i => i.TYPE === 'image'));
  const sources = shaders.filter(s => !(s.INPUTS || []).some(i => i.TYPE === 'image'));

  console.log('Sources:', sources.length, '| Effects:', effects.length);

  const templates = [];

  // 1. Simple generators - one per source shader
  for (const s of sources) {
    templates.push({ name: `tmpl-gen-${s._name}`, patch: tmplSimpleGenerator(s) });
  }

  // 2. Generator + Bloom - select good ones
  for (const s of sources) {
    templates.push({ name: `tmpl-gen-bloom-${s._name}`, patch: tmplGeneratorWithBloom(s) });
  }

  // 3. Feedback generators - moody shaders
  const feedbackPicks = sources.filter(s =>
    /nebula|cosmic|plasma|wave|bubble|retro|star|pipes|mystify/i.test(s._name)
  );
  for (const s of feedbackPicks) {
    templates.push({ name: `tmpl-feedback-${s._name}`, patch: tmplGeneratorFeedback(s) });
  }

  // 4. Beat-synced - rhythmic shaders
  const beatPicks = sources.filter(s =>
    /wave|sine|spinner|disc|allwave|matrix|bouncing|toaster|fairy/i.test(s._name)
  );
  for (const s of beatPicks) {
    templates.push({ name: `tmpl-beat-${s._name}`, patch: tmplGeneratorBeatSync(s) });
  }

  // 5. Glitch chains
  const glitchPicks = sources.filter(s =>
    /matrix|retro|pipes|starfield|bouncing|mystify/i.test(s._name)
  );
  for (const s of glitchPicks) {
    templates.push({ name: `tmpl-glitch-${s._name}`, patch: tmplGlitchChain(s) });
  }

  // 6. Color drift
  const colorPicks = sources.filter(s =>
    /plasma|nebula|gradient|colorbleed|cosmic|abstract|organic/i.test(s._name)
  );
  for (const s of colorPicks) {
    templates.push({ name: `tmpl-colordrift-${s._name}`, patch: tmplColorDrift(s) });
  }

  // 7. Dual mixers - interesting combinations
  const mixPairs = [
    ['Nebula', 'StarfieldWarp'],
    ['plasma5', 'CosmicJellyfish'],
    ['MatrixRain', 'PipesRetro'],
    ['MystifyLines', 'RetroRipples'],
    ['Bubbles', 'plasma-converted'],
    ['FairyBreadToasters', 'FlyingToastersTribute'],
    ['abstract-brushstrokes', 'Colorbleed1'],
    ['Sinewaves1', 'AllWaveForms'],
    ['BouncingLogo', 'StarfieldWarp'],
    ['discs', 'Spinner'],
  ];
  for (const [a, b] of mixPairs) {
    const sa = sources.find(s => s._name === a);
    const sb = sources.find(s => s._name === b);
    if (sa && sb) {
      templates.push({ name: `tmpl-mix-${sa._name}-${sb._name}`, patch: tmplDualMixer(sa, sb) });
    }
  }

  // 8. Effect templates (ISF texture effects)
  for (const e of effects) {
    templates.push({ name: `tmpl-fx-isf-${e._name}`, patch: tmplEffectChain(e) });
  }

  // 9. Built-in FX templates
  templates.push({ name: 'tmpl-fx-blur-bloom', patch: tmplFXBlurBloom() });
  templates.push({ name: 'tmpl-fx-glitch-processor', patch: tmplFXGlitch() });
  templates.push({ name: 'tmpl-fx-feedback-loop', patch: tmplFXFeedback() });

  // 10. MIDI generators - select a few
  const midiPicks = sources.filter(s =>
    /nebula|cosmic|plasma|matrix|starfield|mystify|pipes|fairy|bouncing/i.test(s._name)
  );
  for (const s of midiPicks) {
    templates.push({ name: `tmpl-midi-${s._name}`, patch: tmplMidiGenerator(s) });
  }

  // 11. FFT generators
  const fftPicks = sources.filter(s =>
    /wave|sine|spinner|disc|allwave|bubbles|plasma|nebula/i.test(s._name)
  );
  for (const s of fftPicks) {
    templates.push({ name: `tmpl-fft-${s._name}`, patch: tmplFFTGenerator(s) });
  }

  // 12. Additional FX effect patches (Texture In → FX chain → Texture Out)
  templates.push({ name: 'tmpl-fx-edge-detect', patch: tmplFXEdgeDetect() });
  templates.push({ name: 'tmpl-fx-color-offset-rgb', patch: tmplFXColorOffsetRGB() });
  templates.push({ name: 'tmpl-fx-hue-rotate-lfo', patch: tmplFXHueRotateLFO() });
  templates.push({ name: 'tmpl-fx-delay-feedback-hue', patch: tmplFXDelayFeedbackHue() });
  templates.push({ name: 'tmpl-fx-shift-glitch', patch: tmplFXShiftGlitch() });
  templates.push({ name: 'tmpl-fx-ripple-bloom', patch: tmplFXRippleBloom() });
  templates.push({ name: 'tmpl-fx-vignette-colorize', patch: tmplFXVignetteColorize() });
  templates.push({ name: 'tmpl-fx-repeat-kaleidoscope', patch: tmplFXRepeatKaleidoscope() });
  templates.push({ name: 'tmpl-fx-edge-bloom-hue', patch: tmplFXEdgeBloomHue() });
  templates.push({ name: 'tmpl-fx-bpm-strobe', patch: tmplFXBPMStrobe() });
  templates.push({ name: 'tmpl-fx-delay-rgb-split', patch: tmplFXDelayRGBSplit() });
  templates.push({ name: 'tmpl-fx-mega-chain', patch: tmplFXMegaChain() });

  // 13. MIDI-controlled FX effects (APC40 MKII mapped)
  templates.push({ name: 'tmpl-fx-midi-hue-bloom', patch: tmplFXMidiHueBloom() });
  templates.push({ name: 'tmpl-fx-midi-glitch-rack', patch: tmplFXMidiGlitchRack() });
  templates.push({ name: 'tmpl-fx-midi-feedback-delay', patch: tmplFXMidiFeedbackDelay() });

  // 14. Webcam / multi-input effects
  templates.push({ name: 'tmpl-fx-webcam-mixer', patch: tmplFXWebcamMixer() });

  // 15. BPM / tempo effects
  templates.push({ name: 'tmpl-fx-pixelate-bpm', patch: tmplFXPixelateBPM() });

  // 16. Transform / mirror effects
  templates.push({ name: 'tmpl-fx-transform-mirror', patch: tmplFXTransformMirror() });

  // 17. FFT audio-reactive effects
  templates.push({ name: 'tmpl-fx-fft-reactive', patch: tmplFXFFTReactive() });

  // Write templates
  if (!fs.existsSync(outputDir)) fs.mkdirSync(outputDir, { recursive: true });

  let written = 0;
  for (const t of templates) {
    const outPath = path.join(outputDir, t.name + '.wire');
    fs.writeFileSync(outPath, JSON.stringify(t.patch, null, 2));
    console.log(`  WROTE: ${outPath}`);
    written++;
  }

  console.log(`\nGenerated ${written} Wire templates in ${path.resolve(outputDir)}`);

  // Summary by type
  const byType = {};
  for (const t of templates) {
    const type = t.name.split('-')[1];
    byType[type] = (byType[type] || 0) + 1;
  }
  for (const [type, count] of Object.entries(byType).sort()) {
    console.log(`  ${type}: ${count}`);
  }
}

main();
