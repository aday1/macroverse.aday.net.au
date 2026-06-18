#!/usr/bin/env node
/**
 * Extract ISF seeds/tags from VJ-Generated and quarantined shaders.
 * Writes scripts/reports/quarantine-manifest.json (committed) for unshipped failures.
 *
 * Usage:
 *   node scripts/seed-generated-metadata.mjs
 *   node scripts/seed-generated-metadata.mjs --quarantine-only
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, '..');

const ISF_HEADER_RE = /\/\*\s*(\{[\s\S]*?\})\s*\*/;
const PRESET_SETS = new Set([
  'vj-ambient', 'vj-techno', 'vj-cosmic', 'vj-glitch', 'vj-geometric',
  'vj-organic', 'vj-wire-ready', 'vj-dark', 'vj-colour',
  'macroverse-origin', 'macroverse-set',
]);

function parseISFHeader(text) {
  const m = text.match(ISF_HEADER_RE);
  if (!m) return null;
  try {
    return JSON.parse(m[1]);
  } catch {
    return null;
  }
}

function seedFromDescription(desc) {
  if (!desc || typeof desc !== 'string') return null;
  const parts = desc.trim().split(/\s+/);
  return parts.length > 1 ? parts.slice(1).join(' ') : desc;
}

function templateFromFilename(name) {
  const base = name.replace(/\.fs$/i, '');
  return base.replace(/-w2-\d+.*$/, '').replace(/-\d+-m\d+$/, '').replace(/-\d+.*$/, '');
}

function metadataFromFile(filePath) {
  const text = fs.readFileSync(filePath, 'utf8');
  const header = parseISFHeader(text);
  const rel = path.relative(root, filePath).replace(/\\/g, '/');
  const filename = path.basename(filePath);
  const category = path.basename(path.dirname(filePath));
  const rawTags = header?.TAGS || header?.tags || [];
  const tags = [];
  const sets = [];
  for (const t of rawTags) {
    if (PRESET_SETS.has(t)) sets.push(t);
    else tags.push(t);
  }
  if (!sets.includes('vj-wire-ready')) sets.push('vj-wire-ready');
  return {
    path: rel,
    filename,
    template: templateFromFilename(filename),
    category: header?.CATEGORIES?.[0] || category,
    seed: seedFromDescription(header?.DESCRIPTION),
    description: header?.DESCRIPTION || null,
    tags: [...new Set(tags)],
    sets: [...new Set(sets)],
  };
}

function walkFs(dir) {
  const out = [];
  if (!fs.existsSync(dir)) return out;
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, ent.name);
    if (ent.isDirectory()) out.push(...walkFs(full));
    else if (ent.name.endsWith('.fs')) out.push(full);
  }
  return out;
}

function loadQaFailures() {
  const reportsDir = path.join(root, 'scripts', 'reports');
  const failures = new Map();
  if (!fs.existsSync(reportsDir)) return failures;
  for (const name of fs.readdirSync(reportsDir)) {
    if (!name.startsWith('shader-qa') || !name.endsWith('.json')) continue;
    try {
      const rep = JSON.parse(fs.readFileSync(path.join(reportsDir, name), 'utf8'));
      for (const f of rep.failed || []) {
        if (f.path) failures.set(f.path.replace(/\\/g, '/'), f.error || 'qa-failed');
      }
    } catch {
      // ignore bad report
    }
  }
  return failures;
}

const quarantineOnly = process.argv.includes('--quarantine-only');
const generatedDir = path.join(root, 'shaders', 'VJ-Generated');
const quarantineDir = path.join(root, 'shaders', '_quarantine');

const qaFailures = loadQaFailures();
const generated = quarantineOnly ? [] : walkFs(generatedDir).map(metadataFromFile);
const quarantined = walkFs(quarantineDir).map((file) => {
  const meta = metadataFromFile(file);
  const origPath = meta.path.replace(/^shaders\/_quarantine\//, 'shaders/');
  meta.originalPath = origPath;
  meta.qaError = qaFailures.get(origPath) || qaFailures.get(meta.path) || 'blank-or-compile-failure';
  meta.shipped = false;
  meta.thumbnail = false;
  meta.status = 'quarantined-unfixable';
  return meta;
});

const byTemplate = {};
for (const q of quarantined) {
  byTemplate[q.template] = (byTemplate[q.template] || 0) + 1;
}

const manifest = {
  generatedAt: new Date().toISOString(),
  summary: {
    generatedShipped: generated.length,
    quarantined: quarantined.length,
    quarantineByTemplate: byTemplate,
    note: 'Quarantined shaders compile but fail blank-pixel QA; excluded from Docker image; no thumbnails.',
  },
  quarantined,
};

if (!quarantineOnly) {
  manifest.generatedSample = generated.slice(0, 5);
  manifest.generatedWithMetadata = generated.filter((g) => g.tags.length || g.sets.length).length;
}

const outPath = path.join(root, 'scripts', 'reports', 'quarantine-manifest.json');
fs.mkdirSync(path.dirname(outPath), { recursive: true });
fs.writeFileSync(outPath, JSON.stringify(manifest, null, 2), 'utf8');

console.log(`Quarantine manifest: ${quarantined.length} entries → ${path.relative(root, outPath)}`);
if (!quarantineOnly) {
  console.log(`VJ-Generated scanned: ${generated.length} (${manifest.generatedWithMetadata} with ISF tags/sets)`);
}
for (const [tpl, n] of Object.entries(byTemplate).sort((a, b) => b[1] - a[1])) {
  console.log(`  ${tpl}: ${n}`);
}