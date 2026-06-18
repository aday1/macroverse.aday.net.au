#!/usr/bin/env node
// Generates shader-index.factory.json from tracked factory shaders.
// Run: node scripts/gen-factory-index.js
const fs = require('fs');
const path = require('path');

function walk(dir, exts) {
  const results = [];
  if (!fs.existsSync(dir)) return results;
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) results.push(...walk(full, exts));
    else if (exts.some(e => entry.name.endsWith(e))) results.push(full);
  }
  return results;
}

const root = path.resolve(__dirname, '..');
const dirOrder = ['shaders/starter-pack', 'shaders/glsl', 'shaders/isf', 'shaders/core', 'shaders/debug'];
const exts = ['.fs', '.frag', '.glsl'];
const baseDirs = new Set(['core', 'debug', 'glsl', 'isf', 'starter-pack']);
const BANNED_SHADER_NAMES = new Set(['core-text-template']);

const MACROVERSE_SETS = ['macroverse-origin', 'vj-cosmic', 'vj-ambient', 'vj-wire-ready'];
const MACROVERSE_CHAPTERS = {
  'energy-field': 'chapter-01',
  'particles': 'chapter-02',
  'blue-giants': 'chapter-03',
  'orbits': 'chapter-04',
  'life-as-we-know-it': 'chapter-05',
  'living-our-best-life': 'chapter-06',
};

const preferFirst = 'shaders/starter-pack/misc/chroma-warp.fs';

function categoryFromPath(rel) {
  const parts = rel.split('/');
  if (parts[0] === 'shaders' && parts[1] === 'starter-pack' && parts.length >= 4) {
    return parts[2];
  }
  if (parts.length > 3) return parts[2];
  return parts[1] || 'misc';
}

function tagsFor(rel, category, name) {
  if (category === 'macroverse') {
    const ch = MACROVERSE_CHAPTERS[name] || 'macroverse';
    return ['macroverse', ch];
  }
  if (baseDirs.has(category)) return [];
  return [category];
}

function setsFor(category) {
  if (category === 'macroverse') return [...MACROVERSE_SETS];
  return [];
}

const byPath = new Map();

for (const d of dirOrder) {
  const abs = path.join(root, d);
  const files = walk(abs, exts).sort();
  for (const file of files) {
    const rel = file.replace(/\\/g, '/').replace(root.replace(/\\/g, '/') + '/', '');
    const category = categoryFromPath(rel);
    const name = path.basename(file, path.extname(file));
    if (BANNED_SHADER_NAMES.has(name)) continue;
    const entry = {
      id: 0,
      path: rel,
      name,
      category,
      tags: tagsFor(rel, category, name),
    };
    const sets = setsFor(category);
    if (sets.length) entry.sets = sets;
    byPath.set(rel, entry);
  }
}

let entries = [...byPath.values()];

const prefIdx = entries.findIndex(e => e.path === preferFirst);
if (prefIdx > 0) {
  const [pref] = entries.splice(prefIdx, 1);
  entries.unshift(pref);
}

entries.forEach((e, i) => { e.id = i + 1; });

const out = path.join(root, 'shader-index.factory.json');
fs.writeFileSync(out, JSON.stringify(entries, null, 2));
console.log(`Generated ${entries.length} entries → shader-index.factory.json`);
console.log(`First entry: ${entries[0].path}`);
const mv = entries.filter(e => e.category === 'macroverse');
if (mv.length) console.log(`MacroVerse origin: ${mv.length} shader(s)`);