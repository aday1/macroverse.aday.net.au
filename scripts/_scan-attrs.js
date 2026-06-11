#!/usr/bin/env node
// Scan ALL wire files for every unique attribute name + type combo
const fs = require('fs');
const path = require('path');

let fileCount = 0;
const allAttrTypes = new Map();
const nodeClassVersions = new Map();

function scan(dir) {
  for (const f of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, f.name);
    if (f.isDirectory()) { scan(p); continue; }
    if (f.name.indexOf('.wire') === -1) continue;
    if (!f.name.endsWith('.wire')) continue;
    fileCount++;
    try {
      const d = JSON.parse(fs.readFileSync(p, 'utf8'));
      const nodes = (d.patch && d.patch.nodes) || d.nodes || {};
      for (const [id, n] of Object.entries(nodes)) {
        // Track class versions
        if (n.class) {
          const cv = n.class.id + '@v' + n.class.version;
          if (!nodeClassVersions.has(cv)) nodeClassVersions.set(cv, 0);
          nodeClassVersions.set(cv, nodeClassVersions.get(cv) + 1);
        }
        // Track attribute name|type combos
        for (const [a, v] of Object.entries(n.attributes || {})) {
          const t = v && v.type ? v.type : '?';
          const key = a + ' | type=' + t;
          if (!allAttrTypes.has(key)) allAttrTypes.set(key, 0);
          allAttrTypes.set(key, allAttrTypes.get(key) + 1);
        }
      }
    } catch (e) { }
  }
}

scan('resolume');
scan('resolume-example');

console.log('Scanned', fileCount, 'files');
console.log('\n--- ALL Attribute Name|Type Combos ---');
for (const [key, count] of [...allAttrTypes.entries()].sort()) {
  console.log('  ' + key + '  x' + count);
}

console.log('\n--- Node Class Versions (top 30) ---');
const sorted = [...nodeClassVersions.entries()].sort((a, b) => b[1] - a[1]);
for (const [key, count] of sorted.slice(0, 30)) {
  console.log('  ' + key + '  x' + count);
}
