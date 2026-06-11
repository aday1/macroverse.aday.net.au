#!/usr/bin/env node
// Check ISF shader files referenced in wire patches for issues
const fs = require('fs');
const path = require('path');

const wireFile = process.argv[2] || 'resolume/vj-ambient-17.wire';
const d = JSON.parse(fs.readFileSync(wireFile, 'utf8'));
const nodes = (d.patch && d.patch.nodes) || {};

console.log('=== ISF SHADER AUDIT:', wireFile, '===\n');

for (const [id, node] of Object.entries(nodes)) {
  const attrs = node.attributes || {};
  if (!attrs['fragment-shader']) continue;

  const shaderAttr = attrs['fragment-shader'];
  const shaderPath = shaderAttr.value && shaderAttr.value.main;

  console.log('Node', id, '(' + (node.name || '?') + ')');
  console.log('  Shader:', shaderPath);

  if (!shaderPath) {
    console.log('  ERROR: No shader path');
    continue;
  }

  if (!fs.existsSync(shaderPath)) {
    console.log('  ERROR: FILE NOT FOUND');
    continue;
  }

  // Read ISF file and check its metadata
  const content = fs.readFileSync(shaderPath, 'utf8');

  // ISF files have JSON metadata between /* and */
  const metaMatch = content.match(/\/\*\s*([\s\S]*?)\s*\*\//);
  if (!metaMatch) {
    console.log('  WARNING: No ISF metadata block found');
    continue;
  }

  try {
    const meta = JSON.parse(metaMatch[1]);
    const inputs = meta.INPUTS || [];
    const inputNames = inputs.map(i => i.NAME + ' (' + i.TYPE + ')').join(', ');
    console.log('  ISF Inputs:', inputNames || '(none)');

    // Check for any unusual types or names
    for (const inp of inputs) {
      if (inp.TYPE === 'event' || inp.TYPE === 'audio' || inp.TYPE === 'audioFFT') {
        console.log('    UNUSUAL TYPE: ' + inp.NAME + ' = ' + inp.TYPE);
      }
    }
  } catch (e) {
    console.log('  ERROR: ISF metadata parse fail:', e.message);
    console.log('  Raw metadata start:', metaMatch[1].substring(0, 200));
  }
}
