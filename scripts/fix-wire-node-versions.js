#!/usr/bin/env node
/**
 * fix-wire-node-versions.js
 * Scans all .wire files and corrects node class versions to match
 * the values supported by Resolume Wire.
 *
 * Usage: node scripts/fix-wire-node-versions.js [--dry-run]
 */

const fs = require('fs');
const path = require('path');

const dryRun = process.argv.includes('--dry-run');

// Correct version for each Wire node class UUID.
// Sourced from seed-wire-from-sets.js (known-good values).
const CORRECT_VERSIONS = {
  '77697265-4576-4C11-899B-6F11F3275D36': 4,  // ISF
  '77697265-B2A2-4C1C-8C4C-2915D78CC8E9': 1,  // Texture In
  '77697265-BEEA-4D38-8EE5-0EBA4CBD0AEE': 1,  // Texture Out
  '77697265-7BAA-481B-8F7C-A32F6DBE1518': 2,  // Crossfader / Mix A/B
  '77697265-D235-4A6A-B661-02ABE55C72FF': 4,  // Float In
  '77697265-999C-4F8B-8B9D-3646DC68AA69': 1,  // Bool In
  '77697265-3C1D-467D-A722-0FC566538374': 1,  // Comment
  '77697265-5275-4BB0-97B0-DBEEFD85997B': 1,  // MIDI In (APC40 MK II etc)
  '77697265-2F2C-47A5-8DA4-065DAE816AB2': 1,  // Filter CC
  '77697265-A80A-433D-A399-D4C4C5B43D0D': 1,  // Filter Note On
  '77697265-0B51-496E-A02A-4269686CB551': 1,  // FFT Spectrum
  '77697265-A270-4D60-911C-A88B1BE6369A': 3,  // Video Mixer / Feedback Mixer
  '77697265-9225-4009-9D2D-5F898E94CC33': 3,  // Transform
  '77697265-4BEE-483F-9F28-1BA5F21193DB': 5,  // Delay / Feedback Delay
  '77697265-B994-4A9A-94E7-B4E0FA77D8AF': 2,  // Bloom
  '77697265-5F11-465D-9E83-A3CF5A50D8E8': 2,  // Blur
  '77697265-8B97-4252-9A93-BF8028C5EDEC': 2,  // Color Offset
  '77697265-11DE-4F22-B268-C050B2C2BB30': 2,  // Hue Rotate
  '77697265-BB9A-4B60-83F3-D0E4DA6CFF86': 3,  // UV Offset
  '77697265-ACD2-4541-B139-88756DC29CA7': 1,  // Pixelate
  '77697265-9DF3-4B94-A36D-42F1C6FBE997': 1,  // Edge Detect
  '77697265-CE82-4714-8023-1953D9FAB7A9': 1,  // Vignette
  '77697265-2F93-4176-B1F9-774B39EC25F5': 1,  // Repeat
  '77697265-D65D-4BFE-AAD6-882493B313FD': 1,  // Ripple
  '77697265-BC76-4025-8195-07B2AA065E67': 1,  // Threshold
  '77697265-9518-4E69-899B-66B8C527C6EB': 1,  // Colorize
  '77697265-53CE-4FDD-BFCE-5F8269AA39DB': 1,  // Static Gen
  '77697265-E8EC-4F1B-901A-CFFC104D3B07': 1,  // Solid Color
  '77697265-E404-464F-A4C8-FF262AB15588': 1,  // Frac Noise
  '77697265-FCE2-4EBE-8C81-99C578524A24': 1,  // Gradient
  '77697265-7BAB-426B-1F7C-A32F6FBE1518': 1,  // Transition
  '77697265-ED9B-4BEC-B3D4-43AC04731CBA': 1,  // Sine
  '77697265-F95F-41D8-8FC4-DF0DC56E1051': 1,  // Saw
  '77697265-A48B-4AF9-8975-E3063B037D4C': 1,  // Linear
  '77697265-FD65-470F-9F75-ED878267980E': 1,  // Metronome
  '77697265-5581-4721-8519-0b8fa3d68b4d': 2,  // Random
  '77697265-A2EF-44DA-9A81-BB0385C1948C': 1,  // Perlin
  '77697265-A0D8-429A-A558-69BC58D0D425': 1,  // Multiply
  '77697265-A9AF-4CB4-B10F-3968B36BB63B': 1,  // Add
  '77697265-992D-4044-82DC-D2E0D30DF78E': 1,  // Map
  '77697265-86ce-4e85-a02d-34f915fca74e': 1,  // Smooth
  '77697265-AEE3-4B9D-BBD5-6Ae4CCD5aB30': 1,  // Hub
  '77697265-6899-4A9C-82AB-949346033440': 1,  // Switch
};

// Also match UUIDs case-insensitively (Wire UUIDs use mixed case)
const CORRECT_VERSIONS_LOWER = {};
for (const [k, v] of Object.entries(CORRECT_VERSIONS)) {
  CORRECT_VERSIONS_LOWER[k.toLowerCase()] = v;
}

function fixWireFile(filePath) {
  let raw;
  try {
    raw = fs.readFileSync(filePath, 'utf-8');
  } catch (e) {
    console.error(`  SKIP (read error): ${filePath}`);
    return { fixed: 0, file: filePath };
  }

  let data;
  try {
    data = JSON.parse(raw);
  } catch (e) {
    console.error(`  SKIP (parse error): ${filePath}`);
    return { fixed: 0, file: filePath };
  }

  const nodes = data?.patch?.nodes;
  if (!nodes) return { fixed: 0, file: filePath };

  let fixCount = 0;

  for (const [nodeId, node] of Object.entries(nodes)) {
    if (!node?.class?.id) continue;
    const classId = node.class.id;
    const correctVersion = CORRECT_VERSIONS[classId] || CORRECT_VERSIONS_LOWER[classId.toLowerCase()];
    if (correctVersion !== undefined && node.class.version !== correctVersion) {
      const oldVersion = node.class.version;
      node.class.version = correctVersion;
      fixCount++;
      console.log(`  Node ${nodeId} "${node.name || '?'}": class ${classId} version ${oldVersion} -> ${correctVersion}`);
    }

    // Remove unsupported 'flow' attribute (type "flow" is not recognised by
    // older Wire versions and causes "unknown attribute: flow" errors).
    if (node.attributes?.flow?.type === 'flow') {
      delete node.attributes.flow;
      fixCount++;
      console.log(`  Node ${nodeId} "${node.name || '?'}": removed unsupported 'flow' attribute`);
    }
    // Also strip 'flow' from the hidden array if present
    if (Array.isArray(node.hidden)) {
      const idx = node.hidden.indexOf('flow');
      if (idx !== -1) {
        node.hidden.splice(idx, 1);
        // (counted above already, no extra fixCount needed)
      }
    }
  }

  if (fixCount > 0 && !dryRun) {
    // Detect original indentation (tab vs spaces)
    const indent = raw.includes('\t') ? '\t' : '  ';
    fs.writeFileSync(filePath, JSON.stringify(data, null, indent), 'utf-8');
  }

  return { fixed: fixCount, file: filePath };
}

// Recursively find all .wire files
function findWireFiles(dir) {
  const results = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory() && !entry.name.startsWith('.') && entry.name !== 'node_modules') {
      results.push(...findWireFiles(full));
    } else if (entry.isFile() && entry.name.endsWith('.wire')) {
      results.push(full);
    }
  }
  return results;
}

// Main
const rootDir = path.join(__dirname, '..');
const wireFiles = findWireFiles(rootDir);

console.log(`Found ${wireFiles.length} .wire files`);
if (dryRun) console.log('DRY RUN - no files will be modified\n');

let totalFixed = 0;
let filesFixed = 0;

for (const f of wireFiles) {
  const result = fixWireFile(f);
  if (result.fixed > 0) {
    totalFixed += result.fixed;
    filesFixed++;
    console.log(`  => Fixed ${result.fixed} node(s) in ${path.basename(f)}\n`);
  }
}

console.log(`\nDone. Fixed ${totalFixed} node version(s) across ${filesFixed} file(s).`);
if (dryRun) console.log('(DRY RUN - no files were modified. Run without --dry-run to apply.)');
