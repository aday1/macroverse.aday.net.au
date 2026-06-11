#!/usr/bin/env node
/**
 * seed-avenue-from-sets.js - Generate Resolume Avenue .avc composition from Macroverse VJ Sets.
 *
 * Reads macroverse.db, queries Wire patches per VJ set, and generates an Avenue
 * composition file (.avc XML) with:
 *   - One deck per VJ set (9 decks)
 *   - Wire Generator clips for each .wire patch
 *   - 3 layers per deck, filling row by row
 *   - Dashboard links for crossfader, speed, and mix controls
 *   - MIDI shortcut preset reference for APC40 MK II
 *
 * Usage:
 *   node seed-avenue-from-sets.js                     # generate composition
 *   node seed-avenue-from-sets.js --dry-run            # preview only
 *   node seed-avenue-from-sets.js --set vj-cosmic      # single set
 *   node seed-avenue-from-sets.js --name "My Show"     # composition name
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
const WIRE_DIR = path.join(WORKSPACE, 'resolume');

const VJ_SETS = [
  'vj-ambient', 'vj-techno', 'vj-cosmic', 'vj-glitch',
  'vj-geometric', 'vj-organic', 'vj-wire-ready', 'vj-dark', 'vj-colour',
  'vj-generative',
];

const LAYERS_PER_DECK = 3;
const COLUMNS_PER_DECK = 9;
const RES_W = 1920;
const RES_H = 1080;

// ---------------------------------------------------------------------------
// Unique ID generator (Resolume uses millisecond-based unique IDs)
// ---------------------------------------------------------------------------

let _uidCounter = Date.now();
function uid() { return _uidCounter++; }

// ---------------------------------------------------------------------------
// XML helpers
// ---------------------------------------------------------------------------

function escapeXml(str) {
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function indent(level) {
  return '\t'.repeat(level);
}

/**
 * Generate a Resolume <midiShortcut> XML element.
 * @param {'cc'|'note'} type - CC (continuous controller) or Note message
 * @param {number} byte2 - CC number or Note number
 * @param {number} channel - MIDI channel (0-15)
 * @param {number} level - indentation level
 */
function midiShortcutXml(type, byte2, channel, level) {
  const byte1 = type === 'cc' ? (176 + channel) : (144 + channel);
  return `${indent(level)}<midiShortcut byte1="${byte1}" byte2="${byte2}" channel="${channel}" enabled="1"/>`;
}

// ---------------------------------------------------------------------------
// Dashboard links
// ---------------------------------------------------------------------------

const DASHBOARD_LINKS = [
  // Links 1-8: Composition-level FX (mapped to APC40 knobs CC16-23)
  // Pattern from live set: effects default to 0 (off), brought in via MIDI fader
  { name: 'Link 1', altName: 'FFT Displace', default: 0 },
  { name: 'Link 2', altName: 'Mirror Quad', default: 0 },
  { name: 'Link 3', altName: 'Chromatic Shift', default: 0 },
  { name: 'Link 4', altName: 'Glitch', default: 0 },
  { name: 'Link 5', altName: 'Blackhole', default: 0 },
  { name: 'Link 6', altName: 'Goo Distort', default: 0 },
  { name: 'Link 7', altName: 'Twitch 1', default: 0 },
  { name: 'Link 8', altName: 'Twitch 2', default: 0 },
  // Links 9-16: Extended controls
  { name: 'Link 9', altName: 'Hue Rotate', default: 0 },
  { name: 'Link 10', altName: 'Bloom Intensity', default: 0 },
  { name: 'Link 11', altName: 'Blur Amount', default: 0 },
  { name: 'Link 12', altName: 'BPM Sync', default: 0.5 },
  { name: 'Link 13', altName: 'Edge Detect', default: 0 },
  { name: 'Link 14', altName: 'Pixelate', default: 0 },
  { name: 'Link 15', altName: 'RGB Split', default: 0 },
  { name: 'Link 16', altName: 'Tempo Rate', default: 1.0 },
];

// ---------------------------------------------------------------------------
// Built-in Resolume Generator Presets (for vj-generative deck)
// ---------------------------------------------------------------------------
// Derived from ioGenerative Sampler Pack R7 1.0.1 and Mini 1.00 training data.
// Shape types: 0=Circle, 1=Square, 2=Triangle, 3=Pentagon, 4=Hexagon, 5=Star, 6=Cross, 7=Heart
// Combine modes: 0=None, 1=Add, 2=Subtract, 3=Intersect, 4=XOR

const GENERATIVE_PRESETS = [
  // --- ShaperGenerator presets ---
  {
    name: 'Shape Morph',
    type: 'ShaperGenerator',
    params: {
      combine: 0, round: 0.048, phase: 0.59, scale: 0.49, scaleAnimBeats: 4,
      color: { r: 0.475, g: 0.904, b: 0.785, h: 0.454, s: 0.474, v: 0.904 },
      outline: 0.5,
      outlineColor: { r: 0.878, g: 0, b: 0.425, h: 0.919, s: 1, v: 0.878 },
      shape1: { size: 0.194, rotation: 130.7, animBeats: 4 },
      shape2: { type: 6, size: 0.226, rotation: 130.7, animBeats: 4 },
    },
    effects: ['mirror', 'bloom'],
  },
  {
    name: 'Shape Pulse',
    type: 'ShaperGenerator',
    params: {
      combine: 1, round: 0.1, phase: 0.5, scale: 0.6, scaleAnimBeats: 2,
      color: { r: 1, g: 0.2, b: 0.6, h: 0.93, s: 0.8, v: 1 },
      outline: 0.3,
      outlineColor: { r: 0.1, g: 0.8, b: 1, h: 0.53, s: 0.9, v: 1 },
      shape1: { size: 0.3, rotation: 0, animBeats: 8 },
      shape2: { type: 5, size: 0.2, rotation: 45, animBeats: 8 },
    },
    effects: ['polarkaleido', 'bloom'],
  },
  {
    name: 'Shape Intersect',
    type: 'ShaperGenerator',
    params: {
      combine: 3, round: 0.02, phase: 0.75, scale: 0.35,
      color: { r: 0, g: 1, b: 0.8, h: 0.47, s: 1, v: 1 },
      outline: 0.15,
      outlineColor: { r: 1, g: 1, b: 0, h: 0.167, s: 1, v: 1 },
      shape1: { size: 0.4, rotation: 0, animBeats: 4 },
      shape2: { type: 2, size: 0.35, rotation: 60, animBeats: 4 },
    },
    effects: ['mirror', 'bloom'],
  },
  {
    name: 'Shape XOR Spin',
    type: 'ShaperGenerator',
    params: {
      combine: 4, round: 0.15, phase: 0.3, scale: 0.55, scaleAnimBeats: 8,
      color: { r: 0.8, g: 0.2, b: 1, h: 0.78, s: 0.8, v: 1 },
      outline: 0.4,
      outlineColor: { r: 1, g: 0.5, b: 0, h: 0.083, s: 1, v: 1 },
      shape1: { size: 0.25, rotation: 0, animBeats: 2 },
      shape2: { type: 4, size: 0.3, rotation: 0, animBeats: 4 },
    },
    effects: ['wavewarp', 'bloom'],
  },
  {
    name: 'Shape Stars',
    type: 'ShaperGenerator',
    params: {
      combine: 2, round: 0, phase: 1, scale: 0.4,
      color: { r: 1, g: 0.9, b: 0.3, h: 0.15, s: 0.7, v: 1 },
      outline: 0.6,
      outlineColor: { r: 1, g: 0.3, b: 0.1, h: 0.03, s: 0.9, v: 1 },
      shape1: { size: 0.35, rotation: 0, animBeats: 4 },
      shape2: { type: 5, size: 0.15, rotation: 36, animBeats: 8 },
    },
    effects: ['mirror', 'polarkaleido', 'bloom'],
  },
  // --- Lines presets (from Mini 1.00 training data) ---
  {
    name: 'Cascade Lines',
    type: 'Lines',
    params: {
      fuzzyness: 0, amount: 2, width: 0.204, rotation: 0,
      position: 0.5, posAnimBeats: 4,
      color: { r: 1, g: 0.999, b: 0.999, h: 0, s: 0, v: 1 },
      widthDashLink: '/link1',
    },
    effects: ['mirror', 'bloom'],
  },
  {
    name: 'Sharp Lines',
    type: 'Lines',
    params: {
      fuzzyness: 0, amount: 3, width: 0.15, rotation: 45,
      position: 0.5, posAnimBeats: 8,
      color: { r: 1, g: 0, b: 0.5, h: 0.92, s: 1, v: 1 },
    },
    effects: ['mirror', 'bloom'],
  },
  {
    name: 'Soft Bars',
    type: 'Lines',
    params: {
      fuzzyness: 0.5, amount: 5, width: 0.3, rotation: 90,
      position: 0.5, posAnimBeats: 4,
      color: { r: 0, g: 0.8, b: 1, h: 0.53, s: 1, v: 1 },
      widthDashLink: '/link1',
    },
    effects: ['bloom'],
  },
  {
    name: 'Cross Lines',
    type: 'Lines',
    params: {
      fuzzyness: 0.1, amount: 4, width: 0.1, rotation: 0,
      position: 0.5, posAnimBeats: 2,
      color: { r: 0.3, g: 1, b: 0.3, h: 0.33, s: 0.7, v: 1 },
    },
    effects: ['mirror', 'polarkaleido', 'bloom'],
  },
  {
    name: 'Diagonal Sweep',
    type: 'Lines',
    params: {
      fuzzyness: 0.2, amount: 2, width: 0.5, rotation: 135,
      position: 0.5, posAnimBeats: 4,
      color: { r: 1, g: 0.6, b: 0, h: 0.1, s: 1, v: 1 },
    },
    effects: ['wavewarp', 'bloom'],
  },
  {
    name: 'Thin Scanner',
    type: 'Lines',
    params: {
      fuzzyness: 0, amount: 1, width: 0.08, rotation: 0,
      position: 0.5, posAnimBeats: 2,
      color: { r: 1, g: 1, b: 1, h: 0, s: 0, v: 1 },
      widthDashLink: '/link1',
    },
    effects: ['mirror', 'bloom'],
  },
  {
    name: 'Fuzzy Wash',
    type: 'Lines',
    params: {
      fuzzyness: 0.8, amount: 2, width: 0.6, rotation: 0,
      position: 0.5, posAnimBeats: 8,
      color: { r: 0.5, g: 0, b: 1, h: 0.75, s: 1, v: 1 },
    },
    effects: ['bloom'],
  },
  {
    name: 'Vertical Bars',
    type: 'Lines',
    params: {
      fuzzyness: 0, amount: 6, width: 0.12, rotation: 90,
      position: 0.5, posAnimBeats: 4,
      color: { r: 0, g: 1, b: 0.6, h: 0.43, s: 1, v: 1 },
      widthDashLink: '/link2',
    },
    effects: ['mirror', 'bloom'],
  },
  // --- Gradient presets ---
  {
    name: 'Linear Gradient',
    type: 'Gradient',
    params: {
      type: 0,
      color1: { r: 0, g: 0, b: 1, h: 0.667, s: 1, v: 1 },
      color2: { r: 1, g: 0, b: 0.5, h: 0.917, s: 1, v: 1 },
    },
    effects: ['colorize', 'bloom'],
  },
  {
    name: 'Radial Gradient',
    type: 'Gradient',
    params: {
      type: 1,
      color1: { r: 1, g: 0.8, b: 0, h: 0.133, s: 1, v: 1 },
      color2: { r: 0.2, g: 0, b: 0.5, h: 0.767, s: 1, v: 0.5 },
    },
    effects: ['polarkaleido', 'bloom'],
  },
  {
    name: 'Diamond Gradient',
    type: 'Gradient',
    params: {
      type: 2,
      color1: { r: 0, g: 1, b: 1, h: 0.5, s: 1, v: 1 },
      color2: { r: 0, g: 0, b: 0, h: 0, s: 0, v: 0 },
    },
    effects: ['mirror', 'bloom'],
  },
  // --- Rings presets ---
  {
    name: 'Ring Cascade',
    type: 'Rings',
    params: {
      size: 1.352, spacing: 0.494, lineWidth: 0.398, gap: 190.2,
      rotation: -87.6, rotAnimBeats: 4, rotationStep: 26.7,
      color1: { r: 1, g: 1, b: 1, h: 0, s: 0, v: 1 },
      color2: { r: 1, g: 1, b: 1, h: 0, s: 0, v: 1 },
      widthDashLink: '/link2', gapDashLink: '/link1',
    },
    effects: ['mirror', 'bloom'],
  },
  {
    name: 'Tight Rings',
    type: 'Rings',
    params: {
      size: 0.8, spacing: 0.15, lineWidth: 0.1, gap: 60,
      rotation: 0, rotAnimBeats: 8, rotationStep: 45,
      color1: { r: 1, g: 0, b: 0.5, h: 0.917, s: 1, v: 1 },
      color2: { r: 0, g: 0.5, b: 1, h: 0.583, s: 1, v: 1 },
    },
    effects: ['polarkaleido', 'bloom'],
  },
  {
    name: 'Ring Kaleidoscope',
    type: 'Rings',
    params: {
      size: 1.5, spacing: 0.3, lineWidth: 0.25, gap: 120,
      rotation: 30, rotAnimBeats: 4, rotationStep: 60,
      color1: { r: 0, g: 1, b: 0.3, h: 0.38, s: 1, v: 1 },
      color2: { r: 1, g: 0, b: 1, h: 0.833, s: 1, v: 1 },
    },
    effects: ['mirror', 'polarkaleido', 'bloom'],
  },
  // --- Feedback preset ---
  {
    name: 'Feedback Loop',
    type: 'Feedback',
    params: {},
    effects: ['invertrgb', 'bloom'],
  },
  // --- Spiral presets (from Training_Set.avc) ---
  {
    name: 'Spiral Bloom',
    type: 'Spiral',
    params: {
      controlMode: 0, spreadDetail: 0.1, detail: 2, speed: 1, zoom: 1,
      leafs: 1, distortion: 1, fuzzyness: 0.1, ramification: 0,
      button: 0, buttonSize: 1,
      color: { r: 0.2, g: 0.8, b: 1, h: 0.53, s: 0.8, v: 1 },
      bgColor: { r: 0, g: 0, b: 0, h: 0, s: 0, v: 0 },
    },
    effects: ['bloom'],
  },
  {
    name: 'Spiral Kaleido',
    type: 'Spiral',
    params: {
      controlMode: 0, spreadDetail: 0.2, detail: 3, speed: 0.8, zoom: 1.2,
      leafs: 3, distortion: 0.5, fuzzyness: 0.05, ramification: 0.3,
      button: 0, buttonSize: 1,
      color: { r: 1, g: 0.4, b: 0.8, h: 0.89, s: 0.6, v: 1 },
      bgColor: { r: 0.05, g: 0, b: 0.1, h: 0.75, s: 1, v: 0.1 },
    },
    effects: ['kaleidoscope', 'bloom'],
  },
  {
    name: 'Spiral Glitch',
    type: 'Spiral',
    params: {
      controlMode: 0, spreadDetail: 0.15, detail: 4, speed: 1.5, zoom: 0.8,
      leafs: 2, distortion: 1.8, fuzzyness: 0.2, ramification: 0.5,
      button: 0, buttonSize: 1,
      color: { r: 0, g: 1, b: 0.5, h: 0.42, s: 1, v: 1 },
      bgColor: { r: 0, g: 0, b: 0, h: 0, s: 0, v: 0 },
    },
    effects: ['shiftglitch', 'shiftrgb', 'bloom'],
  },
  {
    name: 'Spiral Tunnel',
    type: 'Spiral',
    params: {
      controlMode: 0, spreadDetail: 0.05, detail: 5, speed: 0.5, zoom: 1.5,
      leafs: 5, distortion: 0.3, fuzzyness: 0.02, ramification: 0.8,
      button: 1, buttonSize: 0.5,
      color: { r: 1, g: 0.9, b: 0.3, h: 0.15, s: 0.7, v: 1 },
      bgColor: { r: 0.1, g: 0, b: 0.2, h: 0.75, s: 1, v: 0.2 },
    },
    effects: ['tunnel', 'bloom'],
  },
  // --- New effect combo presets using existing generators ---
  {
    name: 'Shape Kaleido',
    type: 'ShaperGenerator',
    params: {
      combine: 1, round: 0.05, phase: 0.7, scale: 0.5,
      color: { r: 1, g: 0.3, b: 0.7, h: 0.91, s: 0.7, v: 1 },
      outline: 0.2,
      outlineColor: { r: 0, g: 0.9, b: 1, h: 0.51, s: 1, v: 1 },
      shape1: { size: 0.3, rotation: 0, animBeats: 4 },
      shape2: { type: 3, size: 0.2, rotation: 60, animBeats: 8 },
    },
    effects: ['kaleidoscope', 'bloom'],
  },
  {
    name: 'Lines Tunnel',
    type: 'Lines',
    params: {
      fuzzyness: 0.1, amount: 3, width: 0.2, rotation: 0,
      position: 0.5, posAnimBeats: 4,
      color: { r: 0, g: 1, b: 0.8, h: 0.47, s: 1, v: 1 },
    },
    effects: ['tunnel', 'trails', 'bloom'],
  },
  {
    name: 'Ring Tiles',
    type: 'Rings',
    params: {
      size: 1.0, spacing: 0.25, lineWidth: 0.2, gap: 90,
      rotation: 0, rotAnimBeats: 4, rotationStep: 30,
      color1: { r: 1, g: 0.5, b: 0, h: 0.083, s: 1, v: 1 },
      color2: { r: 0, g: 0.5, b: 1, h: 0.583, s: 1, v: 1 },
    },
    effects: ['tile', 'bloom'],
  },
  {
    name: 'Gradient Ripple',
    type: 'Gradient',
    params: {
      type: 1,
      color1: { r: 0, g: 0.6, b: 1, h: 0.57, s: 1, v: 1 },
      color2: { r: 1, g: 0, b: 0.4, h: 0.93, s: 1, v: 1 },
    },
    effects: ['ripples', 'bloom'],
  },
  {
    name: 'Shape Glitch',
    type: 'ShaperGenerator',
    params: {
      combine: 4, round: 0, phase: 0.5, scale: 0.6,
      color: { r: 0, g: 1, b: 0, h: 0.33, s: 1, v: 1 },
      outline: 0.5,
      outlineColor: { r: 1, g: 0, b: 0, h: 0, s: 1, v: 1 },
      shape1: { size: 0.35, rotation: 0, animBeats: 2 },
      shape2: { type: 1, size: 0.25, rotation: 45, animBeats: 4 },
    },
    effects: ['shiftglitch', 'shiftrgb', 'bloom'],
  },
  {
    name: 'Lines DotScreen',
    type: 'Lines',
    params: {
      fuzzyness: 0, amount: 6, width: 0.08, rotation: 45,
      position: 0.5, posAnimBeats: 4,
      color: { r: 1, g: 1, b: 0, h: 0.167, s: 1, v: 1 },
    },
    effects: ['dotscreen', 'bloom'],
  },
  {
    name: 'Ring VideoWall',
    type: 'Rings',
    params: {
      size: 1.2, spacing: 0.4, lineWidth: 0.15, gap: 120,
      rotation: 15, rotAnimBeats: 8, rotationStep: 45,
      color1: { r: 1, g: 0, b: 1, h: 0.833, s: 1, v: 1 },
      color2: { r: 0, g: 1, b: 1, h: 0.5, s: 1, v: 1 },
    },
    effects: ['videowall', 'mirror', 'bloom'],
  },
  {
    name: 'Shape Terrain',
    type: 'ShaperGenerator',
    params: {
      combine: 2, round: 0.1, phase: 0.8, scale: 0.45,
      color: { r: 0.4, g: 0.8, b: 0.2, h: 0.28, s: 0.75, v: 0.8 },
      outline: 0.3,
      outlineColor: { r: 0.9, g: 0.6, b: 0.1, h: 0.1, s: 0.89, v: 0.9 },
      shape1: { size: 0.4, rotation: 0, animBeats: 8 },
      shape2: { type: 4, size: 0.3, rotation: 30, animBeats: 8 },
    },
    effects: ['terrain', 'bloom'],
  },
];

// ---------------------------------------------------------------------------
// APC40 MK II — Complete MIDI Specification
// ---------------------------------------------------------------------------
// Every physical control on the AKAI APC40 MK II and its Resolume binding.
//
// CC Controls (43 total):
//   Track Faders 1-8:    CC7  Ch0-7   → Layer 1-3 opacity (+ 5 spare)
//   Master Fader:        CC14 Ch0     → Master opacity
//   Crossfader:          CC15 Ch0     → Composition crossfader
//   Device Knobs 1-8:    CC16-23 Ch0  → Dashboard Links 1-8 (Comp FX)
//   Track Knobs Pan 1-8: CC48-55 Ch0  → Dashboard Links 9-16 (Extended FX)
//   Track Knobs SendA:   CC16-23 Ch1  → (available for MIDI-learn)
//   Track Knobs SendB:   CC16-23 Ch2  → (available for MIDI-learn)
//   Cue Level:           CC47 Ch0     → Cue level
//
// Note Buttons (93 total):
//   Clip Grid:     Notes 0-4  Ch0-7   → Clip launch (5 scenes × 8 tracks)
//   Scene Launch:  Notes 82-86 Ch0    → Column trigger (5 scenes)
//   Clip Stop:     Note 52 Ch0-7      → Stop clips per track
//   Solo:          Note 49 Ch0-7      → Solo layers
//   Mute:          Note 50 Ch0-7      → Mute layers
//   Track Select:  Note 51 Ch0-7      → Select layers
//   Transport:     Notes 91-93 Ch0    → Play/Stop/Record
//   Navigation:    Notes 94-101 Ch0   → Bank select, Shift, Tap, Nudge
//   Mode Select:   Notes 87-90 Ch0    → Pan/SendA/SendB/Metronome
//   Master Select: Note 80 Ch0        → Master track select
const APC40_MIDI = {
  // --- Continuous Controls (CC) ---
  crossfader:      { cc: 15, ch: 0 },
  masterFader:     { cc: 14, ch: 0 },
  cueLevel:        { cc: 47, ch: 0 },
  trackFaders:     [0, 1, 2, 3, 4, 5, 6, 7].map(ch => ({ cc: 7, ch })),
  deviceKnobs:     [16, 17, 18, 19, 20, 21, 22, 23].map(cc => ({ cc, ch: 0 })),
  trackKnobsPan:   [48, 49, 50, 51, 52, 53, 54, 55].map(cc => ({ cc, ch: 0 })),
  trackKnobsSendA: [16, 17, 18, 19, 20, 21, 22, 23].map(cc => ({ cc, ch: 1 })),
  trackKnobsSendB: [16, 17, 18, 19, 20, 21, 22, 23].map(cc => ({ cc, ch: 2 })),
  // --- Note Buttons ---
  clipGrid: (() => {
    const grid = [];
    for (let scene = 0; scene < 5; scene++)
      for (let track = 0; track < 8; track++)
        grid.push({ note: scene, ch: track, label: `Clip S${scene + 1}T${track + 1}` });
    return grid;
  })(),
  sceneLaunch:  [82, 83, 84, 85, 86].map((n, i) => ({ note: n, ch: 0, label: `Scene ${i + 1}` })),
  clipStop:     [0, 1, 2, 3, 4, 5, 6, 7].map(ch => ({ note: 52, ch })),
  solo:         [0, 1, 2, 3, 4, 5, 6, 7].map(ch => ({ note: 49, ch })),
  mute:         [0, 1, 2, 3, 4, 5, 6, 7].map(ch => ({ note: 50, ch })),
  trackSelect:  [0, 1, 2, 3, 4, 5, 6, 7].map(ch => ({ note: 51, ch })),
  // Transport
  play:         { note: 91, ch: 0 },
  stop:         { note: 92, ch: 0 },
  record:       { note: 93, ch: 0 },
  // Navigation & utility
  masterSelect: { note: 80, ch: 0 },
  bankUp:       { note: 94, ch: 0 },
  bankDown:     { note: 95, ch: 0 },
  bankLeft:     { note: 96, ch: 0 },
  bankRight:    { note: 97, ch: 0 },
  shift:        { note: 98, ch: 0 },
  tapTempo:     { note: 99, ch: 0 },
  nudgeMinus:   { note: 100, ch: 0 },
  nudgePlus:    { note: 101, ch: 0 },
  // Mode select
  panMode:      { note: 87, ch: 0 },
  sendAMode:    { note: 88, ch: 0 },
  sendBMode:    { note: 89, ch: 0 },
  metronome:    { note: 90, ch: 0 },
};

// ---------------------------------------------------------------------------
// Wire Patch Discovery
// ---------------------------------------------------------------------------

function discoverWirePatches(setName) {
  const files = [];
  try {
    const all = fs.readdirSync(WIRE_DIR);
    for (const f of all) {
      if (f.startsWith(setName + '-') && f.endsWith('.wire') ||
          f === setName + '.wire') {
        files.push(path.join(WIRE_DIR, f));
      }
    }
  } catch { /* empty */ }
  files.sort();
  return files;
}

function getWirePatchName(wirePath) {
  try {
    const data = JSON.parse(fs.readFileSync(wirePath, 'utf-8'));
    return data.patch?.meta?.displayName || path.basename(wirePath, '.wire');
  } catch {
    return path.basename(wirePath, '.wire');
  }
}

function getWirePatchIdentifier(wirePath) {
  try {
    const data = JSON.parse(fs.readFileSync(wirePath, 'utf-8'));
    return data.patch?.meta?.identifier || crypto.randomUUID();
  } catch {
    return crypto.randomUUID();
  }
}

// ---------------------------------------------------------------------------
// AVC XML Generation
// ---------------------------------------------------------------------------

function generateClipXml(wirePath, layerIndex, columnIndex, level) {
  const clipName = getWirePatchName(wirePath);
  const wireId = getWirePatchIdentifier(wirePath);
  const clipUid = uid();
  // Always use absolute path so Resolume can find the .wire file regardless of working directory
  const absPath = path.resolve(wirePath);
  const winPath = absPath.replace(/\//g, '\\');

  const lines = [];
  const L = (n, txt) => lines.push(indent(n + level) + txt);

  L(0, `<Clip name="Clip" uniqueId="${clipUid}" layerIndex="${layerIndex}" columnIndex="${columnIndex}">`);
  L(1, `<Params name="Params">`);
  L(2, `<Param name="Name" T="STRING" default="${escapeXml(clipName)}" value="${escapeXml(clipName)}"/>`);
  L(2, `<ParamChoice name="TransportType" default="0" value="0" storeChoices="0"/>`);
  L(1, `</Params>`);

  // Transport
  L(1, `<Transport name="Transport">`);
  L(2, `<Params name="Params">`);
  L(3, `<ParamRange name="Position" T="DOUBLE" default="0" value="0">`);
  L(4, `<DurationSource name="DurationSource"/>`);
  L(4, `<PhaseSourceTransportTimeline name="PhaseSourceTransportTimeline" phase="0" defaultBeatsDuration="8">`);
  L(5, `<Params name="Params">`);
  L(6, `<ParamRange name="Max Distance" altName="Distance" T="DOUBLE" default="5" value="2">`);
  L(7, `<PhaseSourceStatic name="PhaseSourceStatic" phase="0.4"/>`);
  L(6, `</ParamRange>`);
  L(5, `</Params>`);
  L(5, `<Beats_d name="Beats_d" mode="0" numDetectedBeats="-1" numManualBeats="8" detectedTempo="-1" manualTempo="120" detected="0"/>`);
  L(4, `</PhaseSourceTransportTimeline>`);
  L(4, `<ValueRange name="minMax" min="0" max="5000"/>`);
  L(3, `</ParamRange>`);
  L(2, `</Params>`);
  L(1, `</Transport>`);

  L(1, `<ClipView name="ClipView"><FoldParams name="FoldParams"/></ClipView>`);

  // VideoTrack with Wire Generator
  L(1, `<VideoTrack name="VideoTrack" manualDuration="0">`);
  L(2, `<Params name="Params">`);
  L(3, `<ParamRange name="Width" T="DOUBLE" default="${RES_W}" value="${RES_W}"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange>`);
  L(3, `<ParamRange name="Height" T="DOUBLE" default="${RES_H}" value="${RES_H}"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange>`);
  L(3, `<Param name="RScale" T="BOOL" default="1" value="1"/>`);
  L(3, `<Param name="GScale" T="BOOL" default="1" value="1"/>`);
  L(3, `<Param name="BScale" T="BOOL" default="1" value="1"/>`);
  L(3, `<Param name="AScale" T="BOOL" default="1" value="1"/>`);
  L(2, `</Params>`);

  // Clip-level effect chain: Transform + HueRotate + Bloom
  const rpChainUid = uid();
  const transformUid = uid();
  const hueRotUid = uid();
  const bloomUid = uid();
  L(2, `<RenderPass name="RenderPassChain" type="RenderPassChain" uniqueTypeId="RenderPassChain" uniqueId="${rpChainUid}" baseType="RenderPassChain">`);
  L(3, `<RenderPass storage="0" name="Transform" type="TransformEffect" uniqueTypeId="17122039101699797593" uniqueId="${transformUid}" baseType="Effect" index="0">`);
  L(4, `<View name="View" bCanBeDisabled="0" bCanBeRemoved="0"/>`);
  L(3, `</RenderPass>`);
  L(3, `<RenderPass storage="0" name="Hue Rotate" type="HueRotateEffect" uniqueTypeId="7956530275428853165" uniqueId="${hueRotUid}" baseType="Effect" index="1">`);
  L(4, `<View name="View" bCanBeDisabled="1" bCanBeRemoved="1"/>`);
  L(4, `<Params name="Params"><ParamRange name="Rotation" T="DOUBLE" default="0" value="0"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange></Params>`);
  L(3, `</RenderPass>`);
  L(3, `<RenderPass storage="0" name="Bloom" type="BloomEffect" uniqueTypeId="8024591325012876965" uniqueId="${bloomUid}" baseType="Effect" index="2">`);
  L(4, `<View name="View" bCanBeDisabled="1" bCanBeRemoved="1"/>`);
  L(4, `<Params name="Params"><ParamRange name="Intensity" T="DOUBLE" default="0" value="0"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange></Params>`);
  L(3, `</RenderPass>`);
  L(2, `</RenderPass>`);

  L(2, `<ChoosableMixer name="Blend"/>`);

  // Primary Source: Wire Generator with file path reference
  const srcRpChainUid = uid();
  const wireGenUid = uid();
  L(2, `<PrimarySource>`);
  L(3, `<VideoSource storage="0" name="VideoSource" width="${RES_W}" height="${RES_H}" type="GeneratorVideoSource">`);
  L(4, `<RenderPass name="RenderPassChain" type="RenderPassChain" uniqueTypeId="RenderPassChain" uniqueId="${srcRpChainUid}" baseType="RenderPassChain"/>`);
  L(4, `<RenderPass name="${escapeXml(clipName)}" type="WireGenerator" uniqueTypeId="c_${wireId}" uniqueId="${wireGenUid}" baseType="Generator">`);
  L(5, `<WireRenderPass name="WireRenderPass">`);
  L(6, `<SliceInputs/>`);
  L(6, `<WirePatchFile path="${escapeXml(winPath)}"/>`);
  L(5, `</WireRenderPass>`);
  L(4, `</RenderPass>`);
  L(3, `</VideoSource>`);
  L(2, `</PrimarySource>`);

  L(1, `</VideoTrack>`);
  L(1, `<Params name="AutoPilot"/>`);
  L(0, `</Clip>`);

  return lines.join('\n');
}

// ---------------------------------------------------------------------------
// Built-in Generator XML Helpers
// ---------------------------------------------------------------------------

function hsbToColorInt(c) {
  // Convert r,g,b (0-1) to Resolume ARGB integer (alpha=255)
  const r = Math.round((c.r || 0) * 255);
  const g = Math.round((c.g || 0) * 255);
  const b = Math.round((c.b || 0) * 255);
  return ((255 << 24) | (r << 16) | (g << 8) | b) >>> 0;
}

function generateParamColorXml(name, color, L, level, dashLink) {
  const colorInt = hsbToColorInt(color);
  L(level, `<ParamColor name="${escapeXml(name)}" T="COLOR" default="4294967295" value="${colorInt}" channelmode="0" paletteEnabled="0" color="${colorInt}" interpolated="0">`);
  L(level + 1, `<Params name="Channels">`);
  L(level + 2, `<ParamRange name="Red" T="DOUBLE" default="1" value="${color.r || 0}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${color.r || 0}"/></ParamRange>`);
  L(level + 2, `<ParamRange name="Green" T="DOUBLE" default="1" value="${color.g || 0}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${color.g || 0}"/></ParamRange>`);
  L(level + 2, `<ParamRange name="Blue" T="DOUBLE" default="1" value="${color.b || 0}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${color.b || 0}"/></ParamRange>`);
  if (dashLink) {
    L(level + 2, `<ParamRange name="Hue" T="DOUBLE" default="0" value="${color.h || 0}"><PhaseSourceDashboardLink name="PhaseSourceDashboardLink" phase="${color.h || 0}" canSetLinkedParam="1" linkId="${dashLink}"/></ParamRange>`);
  } else {
    L(level + 2, `<ParamRange name="Hue" T="DOUBLE" default="0" value="${color.h || 0}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${color.h || 0}"/></ParamRange>`);
  }
  L(level + 2, `<ParamRange name="Saturation" T="DOUBLE" default="0" value="${color.s || 0}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${color.s || 0}"/></ParamRange>`);
  L(level + 2, `<ParamRange name="Brightness" T="DOUBLE" default="1" value="${color.v || 1}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${color.v || 1}"/></ParamRange>`);
  L(level + 1, `</Params>`);
  L(level, `</ParamColor>`);
}

function generateBpmAnimatedParam(name, value, beats, L, level, altName) {
  const nameAttr = altName ? `name="${escapeXml(name)}" altName="${escapeXml(altName)}"` : `name="${escapeXml(name)}"`;
  L(level, `<ParamRange ${nameAttr} T="DOUBLE" default="${value}" value="${value}">`);
  L(level + 1, `<BehaviourDouble name="BehaviourDouble">`);
  L(level + 2, `<PhaseSourceTimeline name="PhaseSourceTimeline" globalSpeedEnabled="0">`);
  L(level + 3, `<AdaptiveDuration name="AdaptiveDuration"/>`);
  L(level + 3, `<Beats_double name="Beats_double" mode="0" numDetectedBeats="-1" numManualBeats="${beats}" detectedTempo="-1" manualTempo="120" detected="0"/>`);
  L(level + 2, `</PhaseSourceTimeline>`);
  L(level + 1, `</BehaviourDouble>`);
  L(level, `</ParamRange>`);
}

function generateDashLinkedParam(name, value, dashLink, L, level, altName) {
  const nameAttr = altName ? `name="${escapeXml(name)}" altName="${escapeXml(altName)}"` : `name="${escapeXml(name)}"`;
  L(level, `<ParamRange ${nameAttr} T="DOUBLE" default="${value}" value="${value}">`);
  L(level + 1, `<BehaviourDouble name="BehaviourDouble">`);
  L(level + 2, `<PhaseSourceDashboardLink name="PhaseSourceDashboardLink" canSetLinkedParam="1" linkId="${dashLink}"/>`);
  L(level + 1, `</BehaviourDouble>`);
  L(level, `</ParamRange>`);
}

function generateShaperScaleAnimParam(value, beats, L, level) {
  L(level, `<ParamRange name="Scale" T="DOUBLE" default="1" value="${value}">`);
  L(level + 1, `<DurationSource name="DurationSource" scale="0.12"/>`);
  L(level + 1, `<PhaseSourceTimeline name="PhaseSourceTimeline" phase="1">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="Speed" T="DOUBLE" default="1" value="1"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.25"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Max Distance" altName="Distance" T="DOUBLE" default="0.6" value="0.6"><PhaseSourceStatic name="PhaseSourceStatic" phase="1"/></ParamRange>`);
  L(level + 3, `<ParamChoice name="PlayMode" T="INT32" default="0" value="3" storeChoices="0"/>`);
  L(level + 2, `</Params>`);
  L(level + 2, `<Beats_double name="Beats_double" mode="0" numDetectedBeats="-1" numManualBeats="${beats}" detectedTempo="-1" manualTempo="120" detected="0"/>`);
  L(level + 1, `</PhaseSourceTimeline>`);
  L(level + 1, `<ValueRange name="startStop" min="0.01" max="${value}"/>`);
  L(level, `</ParamRange>`);
}

function generateShaperRotationAnimParam(value, beats, L, level) {
  L(level, `<ParamRange name="Rotation" T="DOUBLE" default="0" value="${value}">`);
  L(level + 1, `<DurationSource name="DurationSource"/>`);
  L(level + 1, `<PhaseSourceTimeline name="PhaseSourceTimeline" phase="${Math.abs(value) / 360}">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="Speed" T="DOUBLE" default="1" value="1"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.25"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Max Distance" altName="Distance" T="DOUBLE" default="2" value="2"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.4"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 2, `<Beats_double name="Beats_double" mode="0" numDetectedBeats="-1" numManualBeats="${beats}" detectedTempo="-1" manualTempo="120" detected="0"/>`);
  L(level + 1, `</PhaseSourceTimeline>`);
  L(level, `</ParamRange>`);
}

// ---------------------------------------------------------------------------
// Generator Source XML Builders
// ---------------------------------------------------------------------------

function generateShaperGeneratorXml(preset, L, level) {
  const p = preset.params;
  const genUid = uid();
  L(level, `<RenderPass name="ShaperGenerator" type="ShaperGenerator" uniqueId="${genUid}" baseType="Generator">`);
  L(level + 1, `<Params name="Params">`);
  L(level + 2, `<ParamChoice name="Combine" T="INT32" default="1" value="${p.combine}" storeChoices="0"/>`);
  L(level + 2, `<ParamRange name="Round" T="DOUBLE" default="0" value="${p.round}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.round}"/></ParamRange>`);
  L(level + 2, `<ParamRange name="Phase" T="DOUBLE" default="1" value="${p.phase}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.phase}"/></ParamRange>`);
  if (p.scaleAnimBeats) {
    generateShaperScaleAnimParam(p.scale, p.scaleAnimBeats, L, level + 2);
  } else {
    L(level + 2, `<ParamRange name="Scale" T="DOUBLE" default="1" value="${p.scale}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.scale}"/></ParamRange>`);
  }
  generateParamColorXml('Color', p.color, L, level + 2, '/link3');
  L(level + 2, `<ParamRange name="Outline" T="DOUBLE" default="0.1" value="${p.outline}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${Math.min(p.outline * 2, 1)}"/></ParamRange>`);
  generateParamColorXml('Outline Color', p.outlineColor, L, level + 2, '/link4');
  L(level + 1, `</Params>`);

  // Shape 1
  const s1 = p.shape1;
  L(level + 1, `<Params name="Shape 1">`);
  L(level + 2, `<ParamRange name="Size" T="DOUBLE" default="0.25" value="${s1.size}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${s1.size}"/></ParamRange>`);
  if (s1.animBeats) {
    generateShaperRotationAnimParam(s1.rotation, s1.animBeats, L, level + 2);
  } else {
    L(level + 2, `<ParamRange name="Rotation" T="DOUBLE" default="0" value="${s1.rotation}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${s1.rotation / 360}"/></ParamRange>`);
  }
  L(level + 1, `</Params>`);

  // Shape 2
  const s2 = p.shape2;
  L(level + 1, `<Params name="Shape 2">`);
  L(level + 2, `<ParamChoice name="Type" T="INT32" default="2" value="${s2.type || 0}" storeChoices="0"/>`);
  L(level + 2, `<ParamRange name="Size" T="DOUBLE" default="0.125" value="${s2.size}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${s2.size}"/></ParamRange>`);
  if (s2.animBeats) {
    generateShaperRotationAnimParam(s2.rotation, s2.animBeats, L, level + 2);
  } else {
    L(level + 2, `<ParamRange name="Rotation" T="DOUBLE" default="0" value="${s2.rotation}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${s2.rotation / 360}"/></ParamRange>`);
  }
  L(level + 1, `</Params>`);
  L(level, `</RenderPass>`);
}

function generateLinesGeneratorXml(preset, L, level) {
  const p = preset.params;
  const genUid = uid();
  L(level, `<RenderPass name="Lines" type="Lines" uniqueId="${genUid}" baseType="Generator">`);
  L(level + 1, `<Params name="Params">`);
  L(level + 2, `<ParamRange name="Fuzzyness" T="DOUBLE" default="0.5" value="${p.fuzzyness}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.fuzzyness}"/></ParamRange>`);
  L(level + 2, `<ParamRange name="Amount" T="DOUBLE" default="5" value="${p.amount}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.amount / 10}"/></ParamRange>`);
  if (p.widthDashLink) {
    generateDashLinkedParam('Width', p.width, p.widthDashLink, L, level + 2);
  } else {
    L(level + 2, `<ParamRange name="Width" T="DOUBLE" default="0.3" value="${p.width}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.width}"/></ParamRange>`);
  }
  if (p.rotation !== undefined) {
    L(level + 2, `<ParamRange name="Rotation" T="DOUBLE" default="0" value="${p.rotation}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.rotation / 360}"/></ParamRange>`);
  }
  if (p.posAnimBeats) {
    generateBpmAnimatedParam('Position', p.position, p.posAnimBeats, L, level + 2);
  } else {
    L(level + 2, `<ParamRange name="Position" T="DOUBLE" default="0.5" value="${p.position}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.position}"/></ParamRange>`);
  }
  generateParamColorXml('Color', p.color, L, level + 2);
  L(level + 1, `</Params>`);
  L(level, `</RenderPass>`);
}

function generateGradientGeneratorXml(preset, L, level) {
  const p = preset.params;
  const genUid = uid();
  L(level, `<RenderPass name="Gradient" type="Gradient" uniqueId="${genUid}" baseType="Generator">`);
  L(level + 1, `<Params name="Params">`);
  generateParamColorXml('Color 1', p.color1, L, level + 2);
  generateParamColorXml('Color 2', p.color2, L, level + 2);
  L(level + 2, `<ParamChoice name="Type" T="INT32" default="0" value="${p.type}" storeChoices="0"/>`);
  L(level + 1, `</Params>`);
  L(level, `</RenderPass>`);
}

function generateRingsGeneratorXml(preset, L, level) {
  const p = preset.params;
  const genUid = uid();
  L(level, `<RenderPass name="Rings" type="Rings" uniqueId="${genUid}" baseType="Generator">`);
  L(level + 1, `<Params name="Params">`);
  L(level + 2, `<ParamRange name="Size" T="DOUBLE" default="1" value="${p.size}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.size / 2}"/></ParamRange>`);
  L(level + 2, `<ParamRange name="Spacing" T="DOUBLE" default="0.3" value="${p.spacing}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.spacing}"/></ParamRange>`);
  if (p.widthDashLink) {
    generateDashLinkedParam('Line Width', p.lineWidth, p.widthDashLink, L, level + 2, 'Width');
  } else {
    L(level + 2, `<ParamRange name="Line Width" altName="Width" T="DOUBLE" default="0.2" value="${p.lineWidth}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.lineWidth}"/></ParamRange>`);
  }
  if (p.gapDashLink) {
    generateDashLinkedParam('Gap', p.gap, p.gapDashLink, L, level + 2);
  } else {
    L(level + 2, `<ParamRange name="Gap" T="DOUBLE" default="60" value="${p.gap}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.gap / 360}"/></ParamRange>`);
  }
  if (p.rotAnimBeats) {
    generateBpmAnimatedParam('Rotation', p.rotation, p.rotAnimBeats, L, level + 2);
  } else {
    L(level + 2, `<ParamRange name="Rotation" T="DOUBLE" default="0" value="${p.rotation}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.rotation / 360}"/></ParamRange>`);
  }
  L(level + 2, `<ParamRange name="Rotation Step" altName="Step" T="DOUBLE" default="60" value="${p.rotationStep}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.rotationStep / 360}"/></ParamRange>`);
  generateParamColorXml('Color 1', p.color1, L, level + 2);
  if (p.color2) {
    generateParamColorXml('Color 2', p.color2, L, level + 2);
  }
  L(level + 1, `</Params>`);
  L(level, `</RenderPass>`);
}

function generateFeedbackSourceXml(L, level) {
  L(level, `<VideoSource storage="0" name="VideoSource" width="${RES_W}" height="${RES_H}" type="VideoSourceFeedback">`);
  L(level + 1, `<RenderPass name="RenderPassChain" type="RenderPassChain" uniqueId="RenderPassChain" baseType="RenderPassChain"/>`);
  L(level, `</VideoSource>`);
}

function generateSpiralGeneratorXml(preset, L, level) {
  const p = preset.params;
  const genUid = uid();
  L(level, `<RenderPass name="Spiral" type="Spiral" uniqueTypeId="884102460919" uniqueId="${genUid}" baseType="Generator">`);
  L(level + 1, `<Params name="Params">`);
  L(level + 2, `<ParamChoice name="Control Mode" T="INT32" default="0" value="${p.controlMode || 0}" storeChoices="0"/>`);
  L(level + 2, `<ParamRange name="Spread Detail" T="DOUBLE" default="0.1" value="${p.spreadDetail || 0.1}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.spreadDetail || 0.1}"/></ParamRange>`);
  L(level + 2, `<ParamRange name="Detail" T="DOUBLE" default="2" value="${p.detail || 2}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${(p.detail || 2) / 10}"/></ParamRange>`);
  L(level + 2, `<ParamRange name="Speed" T="DOUBLE" default="1" value="${p.speed || 1}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${(p.speed || 1) / 2}"/></ParamRange>`);
  L(level + 2, `<ParamRange name="Zoom" T="DOUBLE" default="1" value="${p.zoom || 1}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${(p.zoom || 1) / 2}"/></ParamRange>`);
  L(level + 2, `<ParamRange name="Leafs" T="DOUBLE" default="1" value="${p.leafs || 1}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${(p.leafs || 1) / 10}"/></ParamRange>`);
  L(level + 2, `<ParamRange name="Distortion" T="DOUBLE" default="1" value="${p.distortion || 1}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${(p.distortion || 1) / 2}"/></ParamRange>`);
  L(level + 2, `<ParamRange name="Fuzzyness" T="DOUBLE" default="0.1" value="${p.fuzzyness || 0.1}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.fuzzyness || 0.1}"/></ParamRange>`);
  L(level + 2, `<ParamRange name="Ramification" T="DOUBLE" default="0" value="${p.ramification || 0}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${p.ramification || 0}"/></ParamRange>`);
  L(level + 2, `<Param name="Button" T="BOOL" default="0" value="${p.button || 0}"/>`);
  L(level + 2, `<ParamRange name="Button Size" T="DOUBLE" default="1" value="${p.buttonSize || 1}"><PhaseSourceStatic name="PhaseSourceStatic" phase="${(p.buttonSize || 1) / 2}"/></ParamRange>`);
  generateParamColorXml('Color', p.color || { r: 1, g: 1, b: 1, h: 0, s: 0, v: 1 }, L, level + 2);
  generateParamColorXml('BG Color', p.bgColor || { r: 0, g: 0, b: 0, h: 0, s: 0, v: 0 }, L, level + 2);
  L(level + 1, `</Params>`);
  L(level, `</RenderPass>`);
}

// ---------------------------------------------------------------------------
// Effect XML Builders (DryWetEffect pattern from training data)
// ---------------------------------------------------------------------------

function generateMirrorEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="Mirror" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="Mirror" type="Mirror" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="X" T="DOUBLE" default="0.5" value="0.5"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Y" T="DOUBLE" default="0.5" value="0.5"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>`);
  L(level + 3, `<Param name="Flip X" T="BOOL" default="1" value="1"/>`);
  L(level + 3, `<Param name="Flip Y" T="BOOL" default="1" value="1"/>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generatePolarKaleidoEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="PolarKaleido" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="PolarKaleido" type="PolarKaleido" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="Rings" T="DOUBLE" default="1" value="2"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.2"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Parts" T="DOUBLE" default="6" value="6"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.6"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateWaveWarpEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="WaveWarp" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="WaveWarp" type="WaveWarp" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamChoice name="Mode" T="INT32" default="0" value="0" storeChoices="0"/>`);
  L(level + 3, `<ParamRange name="Height" T="DOUBLE" default="0.1" value="0.15"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.15"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Width" T="DOUBLE" default="0.5" value="0.5"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Speed" T="DOUBLE" default="1" value="1"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateColorizeEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="Colorize" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="Colorize" type="Colorize" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamColor name="Color" T="COLOR" default="4278190335" value="4278190335" channelmode="2" paletteEnabled="1" color="4278190335" interpolated="0"/>`);
  L(level + 3, `<ParamChoice name="Mode" T="INT32" default="0" value="1" storeChoices="0"/>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateInvertRGBEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="InvertRGB" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="InvertRGB" type="InvertRGB" uniqueId="${fxUid}" baseType="Effect" dwType="Effect"/>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateBloomEffectXml(index, L, level) {
  const bloomUid = uid();
  L(level, `<RenderPass storage="0" name="Bloom" type="BloomEffect" uniqueTypeId="8024591325012876965" uniqueId="${bloomUid}" baseType="Effect" index="${index}">`);
  L(level + 1, `<View name="View" bCanBeDisabled="1" bCanBeRemoved="1"/>`);
  L(level + 1, `<Params name="Params"><ParamRange name="Intensity" T="DOUBLE" default="0" value="0.3"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.3"/></ParamRange></Params>`);
  L(level, `</RenderPass>`);
}

function generateKaleidoscopeEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="Kaleidoscope" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="Kaleidoscope" type="Kaleidoscope" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="Sides" T="DOUBLE" default="6" value="6"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.6"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Angle" T="DOUBLE" default="0" value="0"><PhaseSourceStatic name="PhaseSourceStatic" phase="0"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateTunnelEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="Tunnel" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="Tunnel" type="Tunnel" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="Speed" T="DOUBLE" default="1" value="1"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Rotation" T="DOUBLE" default="0" value="0"><PhaseSourceStatic name="PhaseSourceStatic" phase="0"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Zoom" T="DOUBLE" default="1" value="1"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateTileEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="TileEffect" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="TileEffect" type="TileEffect" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="X" T="DOUBLE" default="2" value="2"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.2"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Y" T="DOUBLE" default="2" value="2"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.2"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateTrailsEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="Trails" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="Trails" type="Trails" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="Decay" T="DOUBLE" default="0.5" value="0.5"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateShiftGlitchEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="ShiftGlitch" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="ShiftGlitch" type="ShiftGlitch" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="Amount" T="DOUBLE" default="0.1" value="0.1"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.1"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Size" T="DOUBLE" default="0.5" value="0.5"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateShiftRGBEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="ShiftRGB" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="ShiftRGB" type="ShiftRGB" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="Amount" T="DOUBLE" default="0.02" value="0.02"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.02"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Angle" T="DOUBLE" default="0" value="0"><PhaseSourceStatic name="PhaseSourceStatic" phase="0"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateRipplesEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="Ripples" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="Ripples" type="Ripples" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="Amount" T="DOUBLE" default="0.1" value="0.1"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.1"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Speed" T="DOUBLE" default="1" value="1"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Size" T="DOUBLE" default="0.5" value="0.5"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateVideoWallEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="VideoWall" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="VideoWall" type="VideoWall" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="X" T="DOUBLE" default="2" value="2"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.2"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Y" T="DOUBLE" default="2" value="2"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.2"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateTwitchEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="TwitchEffect" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="TwitchEffect" type="TwitchEffect" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="Amount" T="DOUBLE" default="0.1" value="0.1"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.1"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Speed" T="DOUBLE" default="1" value="1"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateDotScreenEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="DotScreen" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="DotScreen" type="DotScreen" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="Size" T="DOUBLE" default="4" value="4"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.4"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Angle" T="DOUBLE" default="0" value="0"><PhaseSourceStatic name="PhaseSourceStatic" phase="0"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateDisplaceEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="Displace" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="Displace" type="Displace" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="Amount" T="DOUBLE" default="0.1" value="0.1"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.1"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateTintEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="Tint" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="Tint" type="Tint" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamColor name="Color" T="COLOR" default="4294901760" value="4294901760" channelmode="0" paletteEnabled="0" color="4294901760" interpolated="0"/>`);
  L(level + 3, `<ParamRange name="Amount" T="DOUBLE" default="0.5" value="0.5"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateBendoscopeEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="Bendoscope" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="Bendoscope" type="Bendoscope" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="Amount" T="DOUBLE" default="0.5" value="0.5"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Zoom" T="DOUBLE" default="1" value="1"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

function generateTerrainEffectXml(index, L, level) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  L(level, `<RenderPass storage="0" name="Terrain" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  L(level + 1, `<RenderPass name="Terrain" type="Terrain" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  L(level + 2, `<Params name="Params">`);
  L(level + 3, `<ParamRange name="Height" T="DOUBLE" default="0.5" value="0.5"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Detail" T="DOUBLE" default="1" value="1"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>`);
  L(level + 3, `<ParamRange name="Rotation" T="DOUBLE" default="0" value="0"><PhaseSourceStatic name="PhaseSourceStatic" phase="0"/></ParamRange>`);
  L(level + 2, `</Params>`);
  L(level + 1, `</RenderPass>`);
  L(level + 1, `<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  L(level, `</RenderPass>`);
}

// ---------------------------------------------------------------------------
// Composition-Level Effect Chain (Master FX - derived from live set data)
// ---------------------------------------------------------------------------
// Pattern: Each effect is a DryWetEffect whose opacity is linked to a
// dashboard parameter. Effects default to 0 (off) and are brought in via
// MIDI fader during performance. APC40 knobs CC16-23 map to Links 1-8.

/**
 * Composition-level FX chain matching live set "Lysdexic vs aday - CURRENT":
 *   Link 1 → Displace (FFT Displace)
 *   Link 2 → Mirror Quad
 *   Link 3 → Color Offset / ShiftRGB (Chromatic Shift)
 *   Link 4 → ShiftGlitch (Glitch)
 *   Link 5 → Mirror (Blackhole)
 *   Link 6 → Ripples (Goo Distort substitute)
 *   Link 7 → Twitch 1
 *   Link 8 → Twitch 2
 *   Link 9 → Hue Rotate
 *   Link 10 → Bloom
 */

function generateCompDashLinkedDryWetEffect(name, type, dashLinkId, innerParamsXml, index, lines, baseLevel) {
  const dwUid = uid();
  const fxUid = uid();
  const mixUid = uid();
  const t = (n) => '\t'.repeat(n + baseLevel);

  // DryWetEffect wrapper with opacity linked to dashboard
  lines.push(`${t(0)}<RenderPass storage="0" name="${escapeXml(name)}" type="DryWetEffect" uniqueId="${dwUid}" baseType="DryWetEffect" index="${index}">`);
  lines.push(`${t(1)}<View name="View" bCanBeDisabled="1" bCanBeRemoved="1"/>`);
  // Opacity param linked to dashboard
  lines.push(`${t(1)}<Params name="Params">`);
  lines.push(`${t(2)}<ParamRange name="Opacity" altName="Mix" T="DOUBLE" default="0" value="0">`);
  lines.push(`${t(3)}<PhaseSourceDashboardLink name="PhaseSourceDashboardLink" phase="0" canSetLinkedParam="1" linkId="${dashLinkId}"/>`);
  lines.push(`${t(2)}</ParamRange>`);
  lines.push(`${t(1)}</Params>`);
  // Inner effect
  lines.push(`${t(1)}<RenderPass name="${escapeXml(name)}" type="${type}" uniqueId="${fxUid}" baseType="Effect" dwType="Effect">`);
  lines.push(innerParamsXml);
  lines.push(`${t(1)}</RenderPass>`);
  // Mixer
  lines.push(`${t(1)}<ChoosableMixer name="Mixer"><RenderPass name="Alpha" type="Alpha" uniqueId="${mixUid}" baseType="Mixer"/></ChoosableMixer>`);
  lines.push(`${t(0)}</RenderPass>`);
}

function generateCompositionEffectChainXml(lines, baseLevel) {
  const t = (n) => '\t'.repeat(n + baseLevel);
  const chainUid = uid();

  lines.push(`${t(0)}<RenderPass name="RenderPassChain" type="RenderPassChain" uniqueTypeId="RenderPassChain" uniqueId="${chainUid}" baseType="RenderPassChain">`);

  // 0: Transform (always present, cannot be disabled)
  const transformUid = uid();
  lines.push(`${t(1)}<RenderPass storage="0" name="Transform" type="TransformEffect" uniqueTypeId="17122039101699797593" uniqueId="${transformUid}" baseType="Effect" index="0">`);
  lines.push(`${t(2)}<View name="View" bCanBeDisabled="0" bCanBeRemoved="0"/>`);
  lines.push(`${t(1)}</RenderPass>`);

  let fxIdx = 1;

  // 1: Color Offset / Chromatic Shift → Link 3
  generateCompDashLinkedDryWetEffect('Color Offset', 'ColorOffset', '/link3',
    `${t(2)}<Params name="Params">\n` +
    `${t(3)}<ParamRange name="Amount" T="DOUBLE" default="0.02" value="0.02"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.02"/></ParamRange>\n` +
    `${t(3)}<ParamRange name="Angle" T="DOUBLE" default="0" value="0"><PhaseSourceStatic name="PhaseSourceStatic" phase="0"/></ParamRange>\n` +
    `${t(2)}</Params>`,
    fxIdx++, lines, baseLevel + 1);

  // 2: ShiftGlitch (Glitch) → Link 4
  generateCompDashLinkedDryWetEffect('ShiftGlitch', 'ShiftGlitch', '/link4',
    `${t(2)}<Params name="Params">\n` +
    `${t(3)}<ParamRange name="Amount" T="DOUBLE" default="0.15" value="0.15"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.15"/></ParamRange>\n` +
    `${t(3)}<ParamRange name="Size" T="DOUBLE" default="0.5" value="0.5"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>\n` +
    `${t(2)}</Params>`,
    fxIdx++, lines, baseLevel + 1);

  // 3: Twitch 1 → Link 7
  generateCompDashLinkedDryWetEffect('Twitch', 'TwitchEffect', '/link7',
    `${t(2)}<Params name="Params">\n` +
    `${t(3)}<ParamRange name="Amount" T="DOUBLE" default="0.2" value="0.2"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.2"/></ParamRange>\n` +
    `${t(3)}<ParamRange name="Speed" T="DOUBLE" default="0.6" value="0.6"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.3"/></ParamRange>\n` +
    `${t(2)}</Params>`,
    fxIdx++, lines, baseLevel + 1);

  // 4: Displace (FFT Displace) → Link 1
  generateCompDashLinkedDryWetEffect('Displace', 'Displace', '/link1',
    `${t(2)}<Params name="Params">\n` +
    `${t(3)}<ParamRange name="Amount" T="DOUBLE" default="0.1" value="0.1"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.1"/></ParamRange>\n` +
    `${t(2)}</Params>`,
    fxIdx++, lines, baseLevel + 1);

  // 5: Ripples (Goo substitute) → Link 6
  generateCompDashLinkedDryWetEffect('Ripples', 'Ripples', '/link6',
    `${t(2)}<Params name="Params">\n` +
    `${t(3)}<ParamRange name="Amount" T="DOUBLE" default="0.15" value="0.15"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.15"/></ParamRange>\n` +
    `${t(3)}<ParamRange name="Speed" T="DOUBLE" default="0.3" value="0.3"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.15"/></ParamRange>\n` +
    `${t(3)}<ParamRange name="Size" T="DOUBLE" default="0.5" value="0.5"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>\n` +
    `${t(2)}</Params>`,
    fxIdx++, lines, baseLevel + 1);

  // 6: Twitch 2 → Link 8
  generateCompDashLinkedDryWetEffect('Twitch 2', 'TwitchEffect', '/link8',
    `${t(2)}<Params name="Params">\n` +
    `${t(3)}<ParamRange name="Amount" T="DOUBLE" default="0.1" value="0.1"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.1"/></ParamRange>\n` +
    `${t(3)}<ParamRange name="Speed" T="DOUBLE" default="1" value="1"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>\n` +
    `${t(2)}</Params>`,
    fxIdx++, lines, baseLevel + 1);

  // 7: Mirror (Blackhole) → Link 5
  generateCompDashLinkedDryWetEffect('Mirror', 'Mirror', '/link5',
    `${t(2)}<Params name="Params">\n` +
    `${t(3)}<ParamRange name="X" T="DOUBLE" default="0.5" value="0.5"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>\n` +
    `${t(3)}<ParamRange name="Y" T="DOUBLE" default="0.5" value="0.5"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>\n` +
    `${t(3)}<Param name="Flip X" T="BOOL" default="1" value="1"/>\n` +
    `${t(3)}<Param name="Flip Y" T="BOOL" default="1" value="1"/>\n` +
    `${t(2)}</Params>`,
    fxIdx++, lines, baseLevel + 1);

  // 8: Mirror Quad → Link 2
  generateCompDashLinkedDryWetEffect('Mirror Quad', 'Mirror', '/link2',
    `${t(2)}<Params name="Params">\n` +
    `${t(3)}<ParamRange name="X" T="DOUBLE" default="0.5" value="0.5"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>\n` +
    `${t(3)}<ParamRange name="Y" T="DOUBLE" default="0.5" value="0.5"><PhaseSourceStatic name="PhaseSourceStatic" phase="0.5"/></ParamRange>\n` +
    `${t(3)}<Param name="Flip X" T="BOOL" default="1" value="1"/>\n` +
    `${t(3)}<Param name="Flip Y" T="BOOL" default="0" value="0"/>\n` +
    `${t(2)}</Params>`,
    fxIdx++, lines, baseLevel + 1);

  // 9: Hue Rotate → Link 9
  const hueUid = uid();
  lines.push(`${t(1)}<RenderPass storage="0" name="Hue Rotate" type="HueRotateEffect" uniqueTypeId="7956530275428853165" uniqueId="${hueUid}" baseType="Effect" index="${fxIdx++}">`);
  lines.push(`${t(2)}<View name="View" bCanBeDisabled="1" bCanBeRemoved="1"/>`);
  lines.push(`${t(2)}<Params name="Params">`);
  lines.push(`${t(3)}<ParamRange name="Rotation" T="DOUBLE" default="0" value="0">`);
  lines.push(`${t(4)}<PhaseSourceDashboardLink name="PhaseSourceDashboardLink" phase="0" canSetLinkedParam="1" linkId="/link9"/>`);
  lines.push(`${t(3)}</ParamRange>`);
  lines.push(`${t(2)}</Params>`);
  lines.push(`${t(1)}</RenderPass>`);

  // 10: Bloom → Link 10
  const bloomUid = uid();
  lines.push(`${t(1)}<RenderPass storage="0" name="Bloom" type="BloomEffect" uniqueTypeId="8024591325012876965" uniqueId="${bloomUid}" baseType="Effect" index="${fxIdx++}">`);
  lines.push(`${t(2)}<View name="View" bCanBeDisabled="1" bCanBeRemoved="1"/>`);
  lines.push(`${t(2)}<Params name="Params">`);
  lines.push(`${t(3)}<ParamRange name="Intensity" T="DOUBLE" default="0" value="0">`);
  lines.push(`${t(4)}<PhaseSourceDashboardLink name="PhaseSourceDashboardLink" phase="0" canSetLinkedParam="1" linkId="/link10"/>`);
  lines.push(`${t(3)}</ParamRange>`);
  lines.push(`${t(2)}</Params>`);
  lines.push(`${t(1)}</RenderPass>`);

  lines.push(`${t(0)}</RenderPass>`);
}

// ---------------------------------------------------------------------------
// Generative Clip XML Generator
// ---------------------------------------------------------------------------

function generateGenerativeClipXml(preset, layerIndex, columnIndex, level) {
  const clipUid = uid();
  const clipName = preset.name;

  const lines = [];
  const L = (n, txt) => lines.push(indent(n + level) + txt);

  L(0, `<Clip name="Clip" uniqueId="${clipUid}" layerIndex="${layerIndex}" columnIndex="${columnIndex}">`);
  L(1, `<Params name="Params">`);
  L(2, `<Param name="Name" T="STRING" default="${escapeXml(clipName)}" value="${escapeXml(clipName)}"/>`);
  L(2, `<ParamChoice name="TransportType" default="0" value="0" storeChoices="0"/>`);
  L(1, `</Params>`);

  // Transport (BPM-synced timeline for generative clips)
  L(1, `<Transport name="Transport">`);
  L(2, `<Params name="Params">`);
  L(3, `<ParamRange name="Position" T="DOUBLE" default="0" value="0">`);
  L(4, `<BehaviourDouble name="BehaviourDouble">`);
  L(5, `<PhaseSourceTimeline name="PhaseSourceTimeline" globalSpeedEnabled="1">`);
  L(6, `<Params name="Params">`);
  L(7, `<ParamRange storage="3" name="Beats" T="DOUBLE" default="8" value="8"/>`);
  L(7, `<ParamRange storage="3" name="BPM" T="DOUBLE" default="120" value="120"/>`);
  L(7, `<ParamRange storage="3" name="Speed" T="DOUBLE" default="1" value="1"/>`);
  L(7, `<ParamChoice storage="3" name="Syncmode" T="INT32" default="0" value="0" storeChoices="0"/>`);
  L(7, `<ParamChoice storage="3" name="PlayDirection" T="INT32" default="1" value="1" storeChoices="0"/>`);
  L(7, `<ParamChoice storage="3" name="PlayMode" T="INT32" default="0" value="0" storeChoices="0"/>`);
  L(7, `<ParamChoice storage="3" name="PlayModeAway" T="INT32" default="0" value="0" storeChoices="0"/>`);
  L(7, `<Param storage="3" name="Finished" T="BOOL" default="0" value="0"/>`);
  L(6, `</Params>`);
  L(6, `<ParamBasedDuration name="ParamBasedDuration"/>`);
  L(6, `<Beats_double name="Beats_double" mode="0" numDetectedBeats="-1" numManualBeats="8" detectedTempo="-1" manualTempo="120" detected="0"/>`);
  L(5, `</PhaseSourceTimeline>`);
  L(4, `</BehaviourDouble>`);
  L(3, `</ParamRange>`);
  L(2, `</Params>`);
  L(1, `</Transport>`);

  L(1, `<ClipView name="ClipView"><FoldParams name="FoldParams"/></ClipView>`);

  // VideoTrack
  L(1, `<VideoTrack name="VideoTrack" manualDuration="0">`);
  L(2, `<Params name="Params">`);
  L(3, `<ParamRange name="Width" T="DOUBLE" default="${RES_W}" value="${RES_W}"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange>`);
  L(3, `<ParamRange name="Height" T="DOUBLE" default="${RES_H}" value="${RES_H}"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange>`);
  L(3, `<Param name="RScale" T="BOOL" default="1" value="1"/>`);
  L(3, `<Param name="GScale" T="BOOL" default="1" value="1"/>`);
  L(3, `<Param name="BScale" T="BOOL" default="1" value="1"/>`);
  L(3, `<Param name="AScale" T="BOOL" default="1" value="1"/>`);
  L(2, `</Params>`);

  // Clip-level effect chain: Transform (always) + preset effects
  const rpChainUid = uid();
  const transformUid = uid();
  L(2, `<RenderPass name="RenderPassChain" type="RenderPassChain" uniqueTypeId="RenderPassChain" uniqueId="${rpChainUid}" baseType="RenderPassChain">`);
  L(3, `<RenderPass storage="0" name="Transform" type="TransformEffect" uniqueTypeId="17122039101699797593" uniqueId="${transformUid}" baseType="Effect" index="0">`);
  L(4, `<View name="View" bCanBeDisabled="0" bCanBeRemoved="0"/>`);
  L(3, `</RenderPass>`);

  // Add effects from preset
  let fxIndex = 1;
  for (const fx of preset.effects) {
    switch (fx) {
      case 'mirror':
        generateMirrorEffectXml(fxIndex, L, 3);
        break;
      case 'polarkaleido':
        generatePolarKaleidoEffectXml(fxIndex, L, 3);
        break;
      case 'wavewarp':
        generateWaveWarpEffectXml(fxIndex, L, 3);
        break;
      case 'colorize':
        generateColorizeEffectXml(fxIndex, L, 3);
        break;
      case 'invertrgb':
        generateInvertRGBEffectXml(fxIndex, L, 3);
        break;
      case 'bloom':
        generateBloomEffectXml(fxIndex, L, 3);
        break;
      case 'kaleidoscope':
        generateKaleidoscopeEffectXml(fxIndex, L, 3);
        break;
      case 'tunnel':
        generateTunnelEffectXml(fxIndex, L, 3);
        break;
      case 'tile':
        generateTileEffectXml(fxIndex, L, 3);
        break;
      case 'trails':
        generateTrailsEffectXml(fxIndex, L, 3);
        break;
      case 'shiftglitch':
        generateShiftGlitchEffectXml(fxIndex, L, 3);
        break;
      case 'shiftrgb':
        generateShiftRGBEffectXml(fxIndex, L, 3);
        break;
      case 'ripples':
        generateRipplesEffectXml(fxIndex, L, 3);
        break;
      case 'videowall':
        generateVideoWallEffectXml(fxIndex, L, 3);
        break;
      case 'twitch':
        generateTwitchEffectXml(fxIndex, L, 3);
        break;
      case 'dotscreen':
        generateDotScreenEffectXml(fxIndex, L, 3);
        break;
      case 'displace':
        generateDisplaceEffectXml(fxIndex, L, 3);
        break;
      case 'tint':
        generateTintEffectXml(fxIndex, L, 3);
        break;
      case 'bendoscope':
        generateBendoscopeEffectXml(fxIndex, L, 3);
        break;
      case 'terrain':
        generateTerrainEffectXml(fxIndex, L, 3);
        break;
    }
    fxIndex++;
  }

  L(2, `</RenderPass>`);

  // Blend mode: Add (matching training data)
  L(2, `<ChoosableMixer name="Blend">`);
  L(3, `<Params name="Params"><ParamChoice storage="3" name="Blend Mode" T="UINT64" default="67464114" value="67464114" storeChoices="0"/></Params>`);
  L(3, `<RenderPass name="Add" type="Add" uniqueId="${uid()}" baseType="Mixer"/>`);
  L(2, `</ChoosableMixer>`);

  // Primary Source: Built-in generator
  const srcRpChainUid = uid();
  L(2, `<PrimarySource>`);

  if (preset.type === 'Feedback') {
    generateFeedbackSourceXml(L, 3);
  } else {
    L(3, `<VideoSource storage="0" name="VideoSource" width="${RES_W}" height="${RES_H}" type="GeneratorVideoSource">`);
    L(4, `<RenderPass name="RenderPassChain" type="RenderPassChain" uniqueTypeId="RenderPassChain" uniqueId="${srcRpChainUid}" baseType="RenderPassChain"/>`);

    switch (preset.type) {
      case 'ShaperGenerator':
        generateShaperGeneratorXml(preset, L, 4);
        break;
      case 'Lines':
        generateLinesGeneratorXml(preset, L, 4);
        break;
      case 'Gradient':
        generateGradientGeneratorXml(preset, L, 4);
        break;
      case 'Rings':
        generateRingsGeneratorXml(preset, L, 4);
        break;
      case 'Spiral':
        generateSpiralGeneratorXml(preset, L, 4);
        break;
    }

    L(3, `</VideoSource>`);
  }

  L(2, `</PrimarySource>`);

  L(1, `</VideoTrack>`);
  L(1, `<Params name="AutoPilot"/>`);
  L(0, `</Clip>`);

  return lines.join('\n');
}

function generateDeckXml(setName, wirePatches, deckIndex, level) {
  const deckUid = uid();
  const displayName = setName.replace('vj-', '').replace(/-/g, ' ')
    .replace(/\b\w/g, c => c.toUpperCase());

  const isGenerative = setName === 'vj-generative';
  const itemCount = isGenerative ? GENERATIVE_PRESETS.length : wirePatches.length;

  const numWithContent = Math.min(itemCount, LAYERS_PER_DECK * COLUMNS_PER_DECK);
  const numColsWithContent = Math.min(
    Math.ceil(itemCount / LAYERS_PER_DECK),
    COLUMNS_PER_DECK,
  );
  const numLayersWithContent = Math.min(itemCount, LAYERS_PER_DECK);

  const lines = [];
  const L = (n, txt) => lines.push(indent(n + level) + txt);

  L(0, `<Deck name="Deck" uniqueId="${deckUid}" closed="0" numLayersWithContent="${numLayersWithContent}" numColumnsWithContent="${numColsWithContent}" numLayers="${LAYERS_PER_DECK}" numColumns="${COLUMNS_PER_DECK}" deckIndex="${deckIndex}">`);
  L(1, `<Params name="Params"><Param name="Name" T="STRING" value="${escapeXml(displayName)}"/></Params>`);

  // Columns
  for (let col = 0; col < COLUMNS_PER_DECK; col++) {
    const colUid = uid();
    L(1, `<Column uniqueId="${colUid}" columnIndex="${col}"/>`);
  }

  // Layers
  for (let layer = 0; layer < LAYERS_PER_DECK; layer++) {
    const layerUid = uid();
    L(1, `<Layer name="Layer" uniqueId="${layerUid}" layerIndex="${layer}">`);
    L(2, `<LayerView/>`);
    L(1, `</Layer>`);
  }

  // Clips - fill column by column, row (layer) by row
  if (isGenerative) {
    let presetIdx = 0;
    for (let col = 0; col < COLUMNS_PER_DECK && presetIdx < GENERATIVE_PRESETS.length; col++) {
      for (let layer = 0; layer < LAYERS_PER_DECK && presetIdx < GENERATIVE_PRESETS.length; layer++) {
        lines.push(generateGenerativeClipXml(GENERATIVE_PRESETS[presetIdx], layer, col, level + 1));
        presetIdx++;
      }
    }
  } else {
    let patchIdx = 0;
    for (let col = 0; col < COLUMNS_PER_DECK && patchIdx < wirePatches.length; col++) {
      for (let layer = 0; layer < LAYERS_PER_DECK && patchIdx < wirePatches.length; layer++) {
        lines.push(generateClipXml(wirePatches[patchIdx], layer, col, level + 1));
        patchIdx++;
      }
    }
  }

  L(0, `</Deck>`);
  return lines.join('\n');
}

function generateCompositionXml(compName, setsToProcess) {
  const lines = [];

  lines.push('<?xml version="1.0" encoding="utf-8"?>');

  const compUid = uid();
  const numDecks = setsToProcess.length;

  lines.push(`<Composition name="Composition" uniqueId="${compUid}" numDecks="${numDecks}" currentDeckIndex="0" numLayers="${LAYERS_PER_DECK}" numColumns="${COLUMNS_PER_DECK}" compositionIsRelative="0">`);

  // Version info
  lines.push(`\t<versionInfo name="Resolume Avenue" majorVersion="7" minorVersion="24" microVersion="3" revision="0"/>`);

  // Composition info with deck names
  lines.push(`\t<CompositionInfo name="${escapeXml(compName)}" description="Generated by Macroverse seed-avenue-from-sets.js" width="${RES_W}" height="${RES_H}">`);
  for (let i = 0; i < setsToProcess.length; i++) {
    const setName = setsToProcess[i];
    const displayName = setName.replace('vj-', '').replace(/-/g, ' ')
      .replace(/\b\w/g, c => c.toUpperCase());
    const infoId = uid();
    lines.push(`\t\t<DeckInfo name="${escapeXml(displayName)}" id="${infoId}" closed="0"/>`);
  }
  lines.push(`\t</CompositionInfo>`);

  // Global params
  lines.push(`\t<Params name="Params">`);
  lines.push(`\t\t<Param name="Name" T="STRING" default="" value="${escapeXml(compName)}"/>`);
  lines.push(`\t\t<ParamRange name="Speed" T="DOUBLE" default="1" value="1"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange>`);
  lines.push(`\t\t<ParamChoice name="Beat Snap" default="1" value="5" storeChoices="0"/>`);
  lines.push(`\t\t<Param name="GlobalTransportTimelineControl" altName="Control Timelines" T="BOOL" default="1" value="0"/>`);
  lines.push(`\t\t<Param name="KeyboardShortcutPreset" T="STRING" default="" value="Default"/>`);
  lines.push(`\t\t<Param name="MidiShortcutPreset" T="STRING" default="" value="Akai APC40 Mk II"/>`);
  lines.push(`\t\t<Param name="OscShortcutPreset" T="STRING" default="" value="OutputAllMessages"/>`);
  lines.push(`\t</Params>`);

  // Dashboard — with MIDI shortcuts binding APC40 knobs
  lines.push(`\t<Params name="Dashboard">`);
  for (let i = 0; i < DASHBOARD_LINKS.length; i++) {
    const link = DASHBOARD_LINKS[i];
    lines.push(`\t\t<ParamRange name="${escapeXml(link.name)}" altName="${escapeXml(link.altName)}" T="DOUBLE" default="${link.default}" value="0">`);
    // Links 1-8  → Device Control Knobs (CC16-23 Ch0)
    // Links 9-16 → Track Knobs Pan mode (CC48-55 Ch0)
    if (i < 8) {
      lines.push(midiShortcutXml('cc', APC40_MIDI.deviceKnobs[i].cc, APC40_MIDI.deviceKnobs[i].ch, 3));
    } else {
      lines.push(midiShortcutXml('cc', APC40_MIDI.trackKnobsPan[i - 8].cc, APC40_MIDI.trackKnobsPan[i - 8].ch, 3));
    }
    lines.push(`\t\t\t<PhaseSourceStatic name="PhaseSourceStatic"/>`);
    lines.push(`\t\t</ParamRange>`);
  }
  lines.push(`\t</Params>`);

  // Composition View
  lines.push(`\t<CompositionView name="CompositionView">`);
  lines.push(`\t\t<FoldParams name="FoldParams">`);
  lines.push(`\t\t\t<FoldState component="/compositionproperties/dashboard/" folded="0"/>`);
  lines.push(`\t\t</FoldParams>`);
  lines.push(`\t</CompositionView>`);

  // Global AudioTrack (minimal)
  lines.push(`\t<AudioTrack name="AudioTrack">`);
  lines.push(`\t\t<Params name="Params">`);
  lines.push(`\t\t\t<ParamRange name="Volume" T="DOUBLE" default="1" value="1"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange>`);
  lines.push(`\t\t</Params>`);
  lines.push(`\t</AudioTrack>`);

  // Global Video Track (master effects - composition-level FX chain)
  // Master Fader → CC14 Ch0 controls master opacity
  lines.push(`\t<VideoTrack name="VideoTrack">`);
  lines.push(`\t\t<Params name="Params">`);
  lines.push(`\t\t\t<ParamRange name="Opacity" altName="V" T="DOUBLE" default="1" value="1">`);
  lines.push(midiShortcutXml('cc', APC40_MIDI.masterFader.cc, APC40_MIDI.masterFader.ch, 4));
  lines.push(`\t\t\t\t<PhaseSourceStatic name="PhaseSourceStatic"/>`);
  lines.push(`\t\t\t</ParamRange>`);
  lines.push(`\t\t\t<ParamRange name="Width" T="DOUBLE" default="${RES_W}" value="${RES_W}"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange>`);
  lines.push(`\t\t\t<ParamRange name="Height" T="DOUBLE" default="${RES_H}" value="${RES_H}"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange>`);
  lines.push(`\t\t</Params>`);
  generateCompositionEffectChainXml(lines, 2);
  lines.push(`\t</VideoTrack>`);

  // Global Layers (shared across all decks) with FX chains
  for (let layer = 0; layer < LAYERS_PER_DECK; layer++) {
    const layerUid = uid();
    const fader = APC40_MIDI.trackFaders[layer]; // CC7 on Ch0, Ch1, Ch2
    const layerSide = layer === 0 ? 'A' : layer === 1 ? 'B' : '';
    const layerLabel = layer === 2 ? 'Layer 3 (FX Overlay)' : `Layer ${layer + 1} (${layerSide})`;
    lines.push(`\t<Layer name="Layer" uniqueId="${layerUid}" layerIndex="${layer}">`);
    lines.push(`\t\t<LayerView/>`);
    lines.push(`\t\t<Params name="Params">`);
    lines.push(`\t\t\t<Param name="Name" T="STRING" default="" value="${layerLabel}"/>`);
    if (layer === 0) {
      // Layer 1 = Side A: crossfader left (0.0) → full, right (1.0) → hidden
      lines.push(`\t\t\t<ParamRange name="Opacity" altName="V" T="DOUBLE" default="1" value="0.5">`);
      lines.push(midiShortcutXml('cc', fader.cc, fader.ch, 4));
      lines.push(`\t\t\t\t<PhaseSourceLinkLoops name="PhaseSourceLinkLoops" phase="0.5" linkId="/composition/crossfaderphase"/>`);
      lines.push(`\t\t\t\t<ValueRange name="minMax" min="1" max="0"/>`);
      lines.push(`\t\t\t</ParamRange>`);
    } else if (layer === 1) {
      // Layer 2 = Side B: crossfader left (0.0) → hidden, right (1.0) → full
      lines.push(`\t\t\t<ParamRange name="Opacity" altName="V" T="DOUBLE" default="1" value="0.5">`);
      lines.push(midiShortcutXml('cc', fader.cc, fader.ch, 4));
      lines.push(`\t\t\t\t<PhaseSourceLinkLoops name="PhaseSourceLinkLoops" phase="0.5" linkId="/composition/crossfaderphase"/>`);
      lines.push(`\t\t\t</ParamRange>`);
    } else {
      // Layer 3 = Always visible (FX overlay), manual fader only
      lines.push(`\t\t\t<ParamRange name="Opacity" altName="V" T="DOUBLE" default="1" value="1">`);
      lines.push(midiShortcutXml('cc', fader.cc, fader.ch, 4));
      lines.push(`\t\t\t\t<PhaseSourceStatic name="PhaseSourceStatic"/>`);
      lines.push(`\t\t\t</ParamRange>`);
    }
    lines.push(`\t\t</Params>`);
    lines.push(`\t\t<AudioTrack name="AudioTrack">`);
    lines.push(`\t\t\t<Params name="Params"><ParamRange name="Volume" T="DOUBLE" default="1" value="1"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange></Params>`);
    lines.push(`\t\t</AudioTrack>`);
    lines.push(`\t\t<VideoTrack name="VideoTrack">`);
    lines.push(`\t\t\t<Params name="Params">`);
    lines.push(`\t\t\t\t<ParamRange name="Width" T="DOUBLE" default="${RES_W}" value="${RES_W}"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange>`);
    lines.push(`\t\t\t\t<ParamRange name="Height" T="DOUBLE" default="${RES_H}" value="${RES_H}"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange>`);
    lines.push(`\t\t\t</Params>`);
    // Layer-level FX chain: Transform + HueRotate + Bloom + Blur
    lines.push(`\t\t\t<RenderPass name="RenderPassChain" type="RenderPassChain" uniqueTypeId="RenderPassChain" uniqueId="${uid()}" baseType="RenderPassChain">`);
    lines.push(`\t\t\t\t<RenderPass storage="0" name="Transform" type="TransformEffect" uniqueTypeId="17122039101699797593" uniqueId="${uid()}" baseType="Effect">`);
    lines.push(`\t\t\t\t\t<View name="View" bCanBeDisabled="0" bCanBeRemoved="0"/>`);
    lines.push(`\t\t\t\t</RenderPass>`);
    lines.push(`\t\t\t\t<RenderPass storage="0" name="Hue Rotate" type="HueRotateEffect" uniqueTypeId="7956530275428853165" uniqueId="${uid()}" baseType="Effect">`);
    lines.push(`\t\t\t\t\t<View name="View" bCanBeDisabled="1" bCanBeRemoved="1"/>`);
    lines.push(`\t\t\t\t\t<Params name="Params"><ParamRange name="Rotation" T="DOUBLE" default="0" value="0"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange></Params>`);
    lines.push(`\t\t\t\t</RenderPass>`);
    lines.push(`\t\t\t\t<RenderPass storage="0" name="Bloom" type="BloomEffect" uniqueTypeId="8024591325012876965" uniqueId="${uid()}" baseType="Effect">`);
    lines.push(`\t\t\t\t\t<View name="View" bCanBeDisabled="1" bCanBeRemoved="1"/>`);
    lines.push(`\t\t\t\t\t<Params name="Params"><ParamRange name="Intensity" T="DOUBLE" default="0" value="0"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange></Params>`);
    lines.push(`\t\t\t\t</RenderPass>`);
    lines.push(`\t\t\t\t<RenderPass storage="0" name="Blur" type="BlurEffect" uniqueTypeId="9247814352619285231" uniqueId="${uid()}" baseType="Effect">`);
    lines.push(`\t\t\t\t\t<View name="View" bCanBeDisabled="1" bCanBeRemoved="1"/>`);
    lines.push(`\t\t\t\t\t<Params name="Params"><ParamRange name="Amount" T="DOUBLE" default="0" value="0"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange></Params>`);
    lines.push(`\t\t\t\t</RenderPass>`);
    lines.push(`\t\t\t\t<RenderPass storage="0" name="Edge Detection" type="EdgeDetectionEffect" uniqueTypeId="12837564209152736321" uniqueId="${uid()}" baseType="Effect">`);
    lines.push(`\t\t\t\t\t<View name="View" bCanBeDisabled="1" bCanBeRemoved="1"/>`);
    lines.push(`\t\t\t\t\t<Params name="Params"><ParamRange name="Strength" T="DOUBLE" default="0" value="0"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange></Params>`);
    lines.push(`\t\t\t\t</RenderPass>`);
    lines.push(`\t\t\t\t<RenderPass storage="0" name="Pixelate" type="PixelateEffect" uniqueTypeId="5693219847234101865" uniqueId="${uid()}" baseType="Effect">`);
    lines.push(`\t\t\t\t\t<View name="View" bCanBeDisabled="1" bCanBeRemoved="1"/>`);
    lines.push(`\t\t\t\t\t<Params name="Params"><ParamRange name="Size" T="DOUBLE" default="0" value="0"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange></Params>`);
    lines.push(`\t\t\t\t</RenderPass>`);
    lines.push(`\t\t\t\t<RenderPass storage="0" name="Color Offset" type="ColorOffsetEffect" uniqueTypeId="3847291056738214509" uniqueId="${uid()}" baseType="Effect">`);
    lines.push(`\t\t\t\t\t<View name="View" bCanBeDisabled="1" bCanBeRemoved="1"/>`);
    lines.push(`\t\t\t\t\t<Params name="Params">`);
    lines.push(`\t\t\t\t\t\t<ParamRange name="Amount" T="DOUBLE" default="0" value="0"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange>`);
    lines.push(`\t\t\t\t\t\t<ParamRange name="Angle" T="DOUBLE" default="0" value="0"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange>`);
    lines.push(`\t\t\t\t\t</Params>`);
    lines.push(`\t\t\t\t</RenderPass>`);
    lines.push(`\t\t\t</RenderPass>`);
    lines.push(`\t\t\t<ChoosableMixer name="Blend"><VideoMixerStateID uniqueId="${uid()}"/></ChoosableMixer>`);
    lines.push(`\t\t</VideoTrack>`);
    lines.push(`\t</Layer>`);
  }

  // CrossFader → CC15 Ch0 (A/B crossfade between Layer 1 and Layer 2)
  lines.push(`\t<CrossFader name="CrossFader">`);
  lines.push(`\t\t<Params name="Params">`);
  lines.push(`\t\t\t<ParamRange name="Position" T="DOUBLE" default="0.5" value="0.5">`);
  lines.push(midiShortcutXml('cc', APC40_MIDI.crossfader.cc, APC40_MIDI.crossfader.ch, 4));
  lines.push(`\t\t\t\t<PhaseSourceStatic name="PhaseSourceStatic"/>`);
  lines.push(`\t\t\t</ParamRange>`);
  lines.push(`\t\t</Params>`);
  lines.push(`\t\t<AudioTrack name="AudioTrack">`);
  lines.push(`\t\t\t<AudioEffectChain name="AudioEffectChain"/>`);
  lines.push(`\t\t</AudioTrack>`);
  lines.push(`\t\t<VideoTrack name="VideoTrack">`);
  lines.push(`\t\t\t<Params name="Params">`);
  lines.push(`\t\t\t\t<ParamRange name="Width" T="DOUBLE" default="${RES_W}" value="${RES_W}"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange>`);
  lines.push(`\t\t\t\t<ParamRange name="Height" T="DOUBLE" default="${RES_H}" value="${RES_H}"><PhaseSourceStatic name="PhaseSourceStatic"/></ParamRange>`);
  lines.push(`\t\t\t</Params>`);
  lines.push(`\t\t\t<RenderPass name="RenderPassChain"/>`);
  lines.push(`\t\t\t<ChoosableMixer name="Blend mode">`);
  lines.push(`\t\t\t\t<Params name="Params">`);
  lines.push(`\t\t\t\t\t<ParamChoice name="Blend Mode" T="UINT64" default="0" value="67464115" storeChoices="0"/>`);
  lines.push(`\t\t\t\t</Params>`);
  lines.push(`\t\t\t\t<VideoMixerStateID uniqueId="${uid()}"/>`);
  lines.push(`\t\t\t\t<RenderPass name="Alpha" type="Alpha" uniqueTypeId="A006" uniqueId="${uid()}" baseType="Mixer"/>`);
  lines.push(`\t\t\t</ChoosableMixer>`);
  lines.push(`\t\t</VideoTrack>`);
  lines.push(`\t</CrossFader>`);

  // Decks (one per set)
  const deckResults = [];
  for (let di = 0; di < setsToProcess.length; di++) {
    const setName = setsToProcess[di];
    const isGenerative = setName === 'vj-generative';
    const wirePatches = isGenerative ? [] : discoverWirePatches(setName);
    const count = isGenerative ? GENERATIVE_PRESETS.length : wirePatches.length;
    deckResults.push({ setName, count });

    const deckXml = generateDeckXml(setName, wirePatches, di, 1);
    lines.push(deckXml);
  }

  lines.push('</Composition>');

  return { xml: lines.join('\n'), deckResults };
}

// ---------------------------------------------------------------------------
// CLI
// ---------------------------------------------------------------------------

function parseArgs() {
  const args = process.argv.slice(2);
  const opts = {
    name: 'Macroverse VJ',
    set: null,
    output: OUTPUT_DIR,
    dryRun: false,
  };
  for (let i = 0; i < args.length; i++) {
    switch (args[i]) {
      case '--name':    opts.name = args[++i]; break;
      case '--set':     opts.set = args[++i]; break;
      case '--output':  opts.output = args[++i]; break;
      case '--dry-run': opts.dryRun = true; break;
      case '--help': case '-h':
        console.log(`Macroverse Avenue Composition Generator
========================================
Generates a Resolume Avenue composition (.avc) from Wire patches in resolume/.
Creates decks per VJ set, with dashboard controls and MIDI mapping.

Usage: node seed-avenue-from-sets.js [options]

Options:
  --name <name>    Composition name (default: "Macroverse VJ")
  --set <name>     Generate for specific VJ set (default: all)
  --output <dir>   Output directory (default: resolume/)
  --dry-run        Preview without writing files

Related scripts:
  seed-wire-from-sets.js   Generate Resolume Wire patches from VJ sets
  gen-factory-index.js     Generate factory shader index`);
        process.exit(0);
      default:
        console.error(`Unknown option: ${args[i]}`);
        process.exit(1);
    }
  }
  return opts;
}

function main() {
  const opts = parseArgs();
  const setsToProcess = opts.set ? [opts.set] : VJ_SETS;
  const outPath = path.join(opts.output, 'macroverse-vj.avc');

  console.log(`Generating Resolume Avenue composition: "${opts.name}"`);
  console.log(`Resolution: ${RES_W}x${RES_H}`);
  console.log(`Decks: ${setsToProcess.length} (${setsToProcess.join(', ')})`);
  console.log();

  // Discover Wire patches / generative presets for each set
  let totalClips = 0;
  for (const setName of setsToProcess) {
    if (setName === 'vj-generative') {
      const usable = Math.min(GENERATIVE_PRESETS.length, LAYERS_PER_DECK * COLUMNS_PER_DECK);
      console.log(`  ${setName}: ${GENERATIVE_PRESETS.length} generative presets (${usable} clips in deck)`);
      totalClips += usable;
    } else {
      const patches = discoverWirePatches(setName);
      const usable = Math.min(patches.length, LAYERS_PER_DECK * COLUMNS_PER_DECK);
      console.log(`  ${setName}: ${patches.length} wire patches (${usable} clips in deck)`);
      totalClips += usable;
    }
  }
  console.log(`\nTotal clips: ${totalClips}`);
  console.log();

  if (opts.dryRun) {
    console.log(`DRY RUN: would write ${outPath}`);
    return;
  }

  const { xml, deckResults } = generateCompositionXml(opts.name, setsToProcess);

  const dir = path.dirname(outPath);
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(outPath, xml, 'utf-8');

  const fileSize = (fs.statSync(outPath).size / 1024).toFixed(1);
  console.log(`WROTE: ${path.resolve(outPath)} (${fileSize} KB)`);
  console.log(`  ${deckResults.length} decks, ${totalClips} clips`);
  console.log(`  Dashboard: ${DASHBOARD_LINKS.length} linked parameters (all MIDI-bound)`);
  console.log(`  MIDI: ${countMidiShortcuts(xml)} <midiShortcut> bindings in output`);
  console.log(`  MIDI preset: Akai APC40 Mk II`);
  console.log(`  Layer FX: Transform + Hue Rotate + Bloom + Blur + Edge Detect + Pixelate + Color Offset per layer`);
  console.log(`  Clip FX: Transform + Hue Rotate + Bloom per clip (Wire clips)`);
  console.log(`  Generative FX: Transform + preset effects per generative clip`);

  // Write MIDI map diagram
  const midiMap = generateMidiMapDiagram();
  const midiMapPath = path.join(opts.output, 'MIDI-MAP.txt');
  fs.writeFileSync(midiMapPath, midiMap, 'utf-8');
  console.log(`\nWROTE: ${path.resolve(midiMapPath)}`);

  console.log(`\n${midiMap}`);

  console.log(`Open in Resolume Avenue/Arena to use.`);
  console.log(`Full path: ${path.resolve(outPath)}`);
}

// ---------------------------------------------------------------------------
// MIDI Map Diagram
// ---------------------------------------------------------------------------

function countMidiShortcuts(xml) {
  return (xml.match(/<midiShortcut /g) || []).length;
}

function generateMidiMapDiagram() {
  const lines = [];
  const p = (s) => lines.push(s);

  p('AKAI APC40 MK II -> Resolume Macroverse VJ');
  p('=============================================');
  p('All bindings are pre-configured in the .avc file.');
  p('No manual MIDI-learn required.');
  p('');
  p('+----------- DEVICE CONTROL KNOBS (top row) -----------+');
  p('|  CC16     CC17     CC18     CC19     CC20     CC21     CC22     CC23  |');
  p('|  (o)1     (o)2     (o)3     (o)4     (o)5     (o)6     (o)7     (o)8 |');
  p('|  FFT      Mirror   Chroma   Glitch   Black-   Goo      Twitch   Twitch|');
  p('|  Displace Quad     Shift             hole     Distort  1        2     |');
  p('|  Link 1   Link 2   Link 3   Link 4   Link 5   Link 6   Link 7   Lk 8|');
  p('|  CompFX:  CompFX:  CompFX:  CompFX:  CompFX:  CompFX:  CompFX:  Comp:|');
  p('|  Displace Mirror   ColorOff ShftGltch Mirror   Ripples  Twitch   Twtch|');
  p('+----------------------------------------------------------------------+');
  p('');
  p('+-------- TRACK KNOBS (press [Pan] button for this bank) --------+');
  p('|  CC48     CC49     CC50     CC51     CC52     CC53     CC54     CC55  |');
  p('|  (o)1     (o)2     (o)3     (o)4     (o)5     (o)6     (o)7     (o)8 |');
  p('|  Hue      Bloom    Blur     BPM      Edge     Pixel-   RGB      Tempo|');
  p('|  Rotate   Intens.  Amount   Sync     Detect   ate      Split    Rate |');
  p('|  Link 9   Link 10  Link 11  Link 12  Link 13  Link 14  Link 15  Lk16|');
  p('+----------------------------------------------------------------------+');
  p('');
  p('+--- TRACK FADERS ---+     +--------+  +-----------+');
  p('|  CC7    CC7    CC7  |     | MASTER |  | CROSSFADE |');
  p('|  Ch0    Ch1    Ch2  |     | CC14   |  | CC15 Ch0  |');
  p('|  | |1   | |2   | |3|     | | |    |  | A===X===B |');
  p('|  Layer  Layer  Layer|     | Master |  | L1  <>  L2|');
  p('|  1(A)   2(B)   3(FX)|     | Opacity|  |           |');
  p('+---------------------+     +--------+  +-----------+');
  p('');
  p('+---- CROSSFADER A/B LAYER ASSIGNMENT -----+');
  p('|  Layer 1 (A): Visible when fader LEFT    |');
  p('|  Layer 2 (B): Visible when fader RIGHT   |');
  p('|  Layer 3 (FX): Always visible (overlay)  |');
  p('|  Linked via /composition/crossfaderphase  |');
  p('|  Track faders override when touched       |');
  p('+-------------------------------------------+');
  p('');
  p('+---- CLIP GRID (5 scenes x 8 tracks) -----+');
  p('|  Notes 0-4 on Channels 0-7                |');
  p('|  [x][x][x][x][x][x][x][x]  Scene 1 (N82) |');
  p('|  [x][x][x][x][x][x][x][x]  Scene 2 (N83) |');
  p('|  [x][x][x][x][x][x][x][x]  Scene 3 (N84) |');
  p('|  [x][x][x][x][x][x][x][x]  Scene 4 (N85) |');
  p('|  [x][x][x][x][x][x][x][x]  Scene 5 (N86) |');
  p('+--------------------------------------------+');
  p('');
  p('+---- TRACK BUTTONS (per track, Ch0-7) -----+');
  p('| [CLIP STOP]  Note 52 -> Stop clips        |');
  p('| [SOLO]       Note 49 -> Solo layers       |');
  p('| [MUTE]       Note 50 -> Mute layers       |');
  p('| [SELECT]     Note 51 -> Select layers     |');
  p('+--------------------------------------------+');
  p('');
  p('+--- TRANSPORT ---+  +---- UTILITY --------+');
  p('| [>]  Note 91    |  | [TAP]   Note 99     |');
  p('| [||] Note 92    |  | [<<]    Note 100    |');
  p('| [O]  Note 93    |  | [>>]    Note 101    |');
  p('| [CUE] CC47 Ch0  |  | [SHIFT] Note 98     |');
  p('+-----------------+  +----------------------+');
  p('');
  p('+--- MODE SELECT ---+  +--- BANK SELECT ---+');
  p('| [Pan]   Note 87   |  | [^] Note 94       |');
  p('| [SndA]  Note 88   |  | [v] Note 95       |');
  p('| [SndB]  Note 89   |  | [<] Note 96       |');
  p('| [Metro] Note 90   |  | [>] Note 97       |');
  p('+--------------------+  +-------------------+');
  p('');
  p('MIDI BINDING SUMMARY');
  p('====================');
  p('');
  p('Composition Master FX (Device Knobs CC16-23):');
  for (let i = 0; i < 8; i++) {
    const link = DASHBOARD_LINKS[i];
    const knob = APC40_MIDI.deviceKnobs[i];
    p(`  Knob ${i + 1} (CC${knob.cc} Ch${knob.ch}) -> ${link.altName} (Link ${i + 1})`);
  }
  p('');
  p('Extended FX (Track Knobs CC48-55, press [Pan]):');
  for (let i = 8; i < DASHBOARD_LINKS.length; i++) {
    const link = DASHBOARD_LINKS[i];
    const knob = APC40_MIDI.trackKnobsPan[i - 8];
    p(`  Knob ${i - 7} (CC${knob.cc} Ch${knob.ch}) -> ${link.altName} (Link ${i + 1})`);
  }
  p('');
  p('Layer Opacities (Track Faders CC7):');
  const sideLabels = ['(A) crossfader-linked', '(B) crossfader-linked', '(FX) always visible'];
  for (let i = 0; i < LAYERS_PER_DECK; i++) {
    const fader = APC40_MIDI.trackFaders[i];
    p(`  Fader ${i + 1} (CC${fader.cc} Ch${fader.ch}) -> Layer ${i + 1} Opacity ${sideLabels[i]}`);
  }
  p('');
  p(`Master + Crossfader:`);
  p(`  Master Fader (CC${APC40_MIDI.masterFader.cc} Ch${APC40_MIDI.masterFader.ch}) -> Master Opacity`);
  p(`  Crossfader   (CC${APC40_MIDI.crossfader.cc} Ch${APC40_MIDI.crossfader.ch}) -> Layer 1(A) <> Layer 2(B)`);
  p(`  Cue Level    (CC${APC40_MIDI.cueLevel.cc} Ch${APC40_MIDI.cueLevel.ch}) -> Cue Level`);
  p('');
  p('Spare Controllers (available for MIDI-learn in Resolume):');
  p('  Track Faders 4-8 (CC7 Ch3-7) - unused');
  p('  Track Knobs Send A (CC16-23 Ch1) - unused');
  p('  Track Knobs Send B (CC16-23 Ch2) - unused');

  return lines.join('\n');
}

main();
