#!/usr/bin/env node
// Comprehensive wire file audit - find ALL potential issues
const fs = require('fs');
const path = require('path');

const wireFile = process.argv[2] || 'resolume/vj-ambient-17.wire';
const j = JSON.parse(fs.readFileSync(wireFile, 'utf8'));

console.log('=== WIRE FILE AUDIT:', wireFile, '===\n');
console.log('Top-level keys:', Object.keys(j).sort().join(', '));
console.log('Format version:', JSON.stringify(j.formatVersion));

// Wire files use patch.nodes, not root nodes
const nodes = (j.patch && j.patch.nodes) || j.nodes || {};
const connections = (j.patch && j.patch.connections) || j.connections || [];

console.log('Node count:', Object.keys(nodes).length);
console.log('Connection count:', connections.length);
console.log();

// All unique node class IDs and versions
const classes = new Map();
for (const [id, node] of Object.entries(nodes)) {
  const cid = node.class ? node.class.id : '?';
  const ver = node.class ? node.class.version : '?';
  const key = cid + '@v' + ver;
  if (!classes.has(key)) classes.set(key, { name: node.name, count: 0 });
  classes.get(key).count++;
}
console.log('--- Node Classes ---');
for (const [key, val] of [...classes.entries()].sort()) {
  console.log('  ' + key + '  name="' + val.name + '"  x' + val.count);
}
console.log();

// All unique attribute keys across all nodes
const attrKeys = new Map();
for (const [id, node] of Object.entries(nodes)) {
  for (const [key, val] of Object.entries(node.attributes || {})) {
    if (!attrKeys.has(key)) attrKeys.set(key, { type: val.type, count: 0, examples: [] });
    const entry = attrKeys.get(key);
    entry.count++;
    if (entry.examples.length < 2) entry.examples.push((node.name || id));
  }
}
console.log('--- All Attribute Keys ---');
for (const [key, val] of [...attrKeys.entries()].sort()) {
  console.log('  "' + key + '" type=' + val.type + ' x' + val.count + ' (e.g. ' + val.examples.join(', ') + ')');
}
console.log();

// All unique constant keys
const constKeys = new Map();
for (const [id, node] of Object.entries(nodes)) {
  for (const [key, val] of Object.entries(node.constants || {})) {
    if (!constKeys.has(key)) constKeys.set(key, { type: val.type, count: 0 });
    constKeys.get(key).count++;
  }
}
console.log('--- All Constant Keys ---');
for (const [key, val] of [...constKeys.entries()].sort()) {
  console.log('  "' + key + '" type=' + val.type + ' x' + val.count);
}
console.log();

// Check for ISF shader paths
let missingShaders = 0;
for (const [id, node] of Object.entries(nodes)) {
  const consts = node.constants || {};
  if (consts.file && consts.file.value) {
    const filePath = consts.file.value;
    if (typeof filePath === 'object' && filePath.main) {
      const exists = fs.existsSync(filePath.main);
      if (!exists) {
        console.log('MISSING SHADER: node="' + (node.name || id) + '" path=' + filePath.main);
        missingShaders++;
      }
    }
  }
}
console.log('Missing shader files:', missingShaders);

// Check for structural issues
let issues = 0;
for (const [id, node] of Object.entries(nodes)) {
  if (!node.class || !node.class.id) {
    console.log('ISSUE: node ' + id + ' missing class.id');
    issues++;
  }
  if (node.class && node.class.version === undefined) {
    console.log('ISSUE: node ' + id + ' (' + node.class.id + ') missing version');
    issues++;
  }
}
console.log('Structural issues:', issues);
