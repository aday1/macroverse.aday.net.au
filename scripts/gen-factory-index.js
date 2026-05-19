#!/usr/bin/env node
// Generates shader-index.factory.json from only the tracked factory shaders.
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
// glsl first (simplest, most reliable — no ISF JSON, no custom uniforms)
// then isf, core, debug
const dirOrder = ['shaders/glsl', 'shaders/isf', 'shaders/core', 'shaders/debug'];
const exts = ['.fs', '.frag', '.glsl'];
const baseDirs = new Set(['core', 'debug', 'glsl', 'isf']);

// Put TextureFX-ChromaWarp first as the Fly.io demo default
const preferFirst = 'shaders/isf/TextureFX-ChromaWarp.fs';

let entries = [];
let id = 1;

for (const d of dirOrder) {
  const abs = path.join(root, d);
  const files = walk(abs, exts).sort();
  for (const file of files) {
    const rel = file.replace(/\\/g, '/').replace(root.replace(/\\/g, '/') + '/', '');
    const parts = rel.split('/');
    const category = parts.length > 3 ? parts[2] : parts[1] || 'misc';
    const name = path.basename(file, path.extname(file));
    const tags = baseDirs.has(category) ? [] : [category];
    entries.push({ id: 0, path: rel, name, category, tags });
  }
}

// Move preferFirst to position 0
const prefIdx = entries.findIndex(e => e.path === preferFirst);
if (prefIdx > 0) {
  const [pref] = entries.splice(prefIdx, 1);
  entries.unshift(pref);
}

// Assign sequential IDs
entries.forEach((e, i) => { e.id = i + 1; });

const out = path.join(root, 'shader-index.factory.json');
fs.writeFileSync(out, JSON.stringify(entries, null, 2));
console.log(`Generated ${entries.length} entries → shader-index.factory.json`);
console.log(`First entry: ${entries[0].path}`);
