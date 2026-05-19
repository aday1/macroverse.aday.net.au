#!/usr/bin/env node
/**
 * _audit-isf-content.js
 *
 * Deep audit of every .fs ISF shader file under shaders/.
 *
 * Checks performed per file:
 *  1. Does a /* ... * / JSON metadata block exist?
 *  2. Does the JSON parse without error?
 *  3. Every INPUT must have at least NAME (string) and TYPE (string).
 *  4. TYPE must be one of the known ISF types.
 *  5. Every INPUT NAME should appear somewhere in the GLSL body below the
 *     metadata block  (unused-input warning).
 *  6. "image" / "IMPORTED" type inputs that reference external files -
 *     flag file-existence issues.
 *  7. Report any other JSON oddities (duplicate keys detected via
 *     character-level scan, trailing commas, etc.).
 *
 * Usage:  node scripts/_audit-isf-content.js [--verbose]
 */

const fs   = require('fs');
const path = require('path');

// ---- configuration -------------------------------------------------------

const SHADER_ROOT = path.resolve(__dirname, '..', 'shaders');
const KNOWN_ISF_TYPES = new Set([
  'float', 'bool', 'long', 'color', 'point2D', 'image',
  'event', 'audio', 'audioFFT',
]);

const verbose = process.argv.includes('--verbose');

// ---- helpers -------------------------------------------------------------

/** Recursively collect every .fs file under `dir`. */
function collectFS(dir) {
  let results = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      results = results.concat(collectFS(full));
    } else if (entry.isFile() && entry.name.endsWith('.fs')) {
      results.push(full);
    }
  }
  return results;
}

/** Try to extract the first block-comment that looks like ISF JSON. */
function extractMetaBlock(content) {
  const m = content.match(/\/\*\s*([\s\S]*?)\s*\*\//);
  if (!m) return null;
  return { raw: m[1], endIndex: m.index + m[0].length };
}

/**
 * Detect common JSON problems that JSON.parse swallows or rejects:
 *  - trailing commas before ] or }
 *  - single-quoted strings
 *  - unescaped control characters
 *  Returns an array of warning strings.
 */
function lintJSON(raw) {
  const warnings = [];

  // trailing commas
  if (/,\s*[\]\}]/.test(raw)) {
    warnings.push('Trailing comma before ] or } (non-strict JSON)');
  }

  // single-quoted strings
  if (/(?<![\\])'/.test(raw)) {
    // only warn if single quotes are used as string delimiters
    const singleQuoted = raw.match(/'[^']*'/g);
    if (singleQuoted && singleQuoted.length > 0) {
      warnings.push('Single-quoted strings detected (not valid JSON)');
    }
  }

  // NaN / Infinity literals
  if (/\bNaN\b|\bInfinity\b/.test(raw)) {
    warnings.push('NaN or Infinity literal (not valid JSON)');
  }

  // JavaScript-style comments inside JSON
  if (/\/\//.test(raw)) {
    warnings.push('Line comment (//) inside JSON metadata block');
  }

  return warnings;
}

/**
 * Check whether GLSL body references a given identifier.
 * We look for the identifier as a whole word (\b boundary).
 */
function glslUsesIdentifier(glslBody, name) {
  const re = new RegExp('\\b' + name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\b');
  return re.test(glslBody);
}

// ---- main audit ----------------------------------------------------------

function auditFile(filePath) {
  const issues = [];  // { severity: 'ERROR'|'WARN', msg: string }

  let content;
  try {
    content = fs.readFileSync(filePath, 'utf8');
  } catch (e) {
    issues.push({ severity: 'ERROR', msg: 'Cannot read file: ' + e.message });
    return issues;
  }

  // 0. empty file
  if (content.trim().length === 0) {
    issues.push({ severity: 'ERROR', msg: 'File is empty' });
    return issues;
  }

  // 1. Extract metadata block
  const block = extractMetaBlock(content);
  if (!block) {
    // Not every .fs file is ISF -- core shaders might be plain GLSL.
    // Mark as INFO, not error.
    issues.push({ severity: 'INFO', msg: 'No ISF metadata block (/* { ... } */) found -- plain GLSL shader' });
    return issues;
  }

  // 2. JSON lint warnings
  const lintWarnings = lintJSON(block.raw);
  for (const w of lintWarnings) {
    issues.push({ severity: 'WARN', msg: 'JSON lint: ' + w });
  }

  // 3. Parse JSON
  let meta;
  try {
    meta = JSON.parse(block.raw);
  } catch (e) {
    issues.push({ severity: 'ERROR', msg: 'JSON parse error: ' + e.message });
    // Try to show approximate location
    const match = e.message.match(/position (\d+)/);
    if (match) {
      const pos = parseInt(match[1]);
      const snippet = block.raw.substring(Math.max(0, pos - 30), pos + 30);
      issues.push({ severity: 'ERROR', msg: 'Near: ...' + snippet + '...' });
    }
    return issues;
  }

  // 4. Validate INPUTS
  const inputs = meta.INPUTS;
  if (!inputs) {
    issues.push({ severity: 'INFO', msg: 'No INPUTS array in ISF metadata (generator-only shader)' });
  } else if (!Array.isArray(inputs)) {
    issues.push({ severity: 'ERROR', msg: 'INPUTS is not an array' });
  } else {
    const glslBody = content.substring(block.endIndex);

    for (let i = 0; i < inputs.length; i++) {
      const inp = inputs[i];
      const label = 'INPUT[' + i + ']';

      // Must be an object
      if (typeof inp !== 'object' || inp === null || Array.isArray(inp)) {
        issues.push({ severity: 'ERROR', msg: label + ' is not an object' });
        continue;
      }

      // NAME required
      if (!inp.NAME || typeof inp.NAME !== 'string') {
        issues.push({ severity: 'ERROR', msg: label + ' missing or invalid NAME' });
        continue;
      }

      // TYPE required
      if (!inp.TYPE || typeof inp.TYPE !== 'string') {
        issues.push({ severity: 'ERROR', msg: label + ' (' + inp.NAME + ') missing or invalid TYPE' });
        continue;
      }

      // TYPE must be known
      if (!KNOWN_ISF_TYPES.has(inp.TYPE)) {
        issues.push({ severity: 'WARN', msg: label + ' (' + inp.NAME + ') unknown TYPE "' + inp.TYPE + '"' });
      }

      // float range sanity: MIN should be <= DEFAULT <= MAX when all present
      if (inp.TYPE === 'float' && inp.MIN !== undefined && inp.MAX !== undefined) {
        if (inp.MIN > inp.MAX) {
          issues.push({ severity: 'ERROR', msg: label + ' (' + inp.NAME + ') MIN (' + inp.MIN + ') > MAX (' + inp.MAX + ')' });
        }
        if (inp.DEFAULT !== undefined) {
          if (inp.DEFAULT < inp.MIN || inp.DEFAULT > inp.MAX) {
            issues.push({ severity: 'WARN', msg: label + ' (' + inp.NAME + ') DEFAULT (' + inp.DEFAULT + ') outside MIN/MAX range [' + inp.MIN + ', ' + inp.MAX + ']' });
          }
        }
      }

      // Check if the INPUT name is actually used in the GLSL body
      if (!glslUsesIdentifier(glslBody, inp.NAME)) {
        // ISF built-in image types might be accessed via ISF macros like IMG_NORM_PIXEL
        if (inp.TYPE === 'image') {
          // image inputs might be accessed via IMG_NORM_PIXEL(name,...) or IMG_PIXEL(name,...)
          // or via texture2D(name,...) or as sampler2D uniform
          if (!glslUsesIdentifier(glslBody, inp.NAME)) {
            issues.push({ severity: 'WARN', msg: label + ' (' + inp.NAME + ') image input not referenced in GLSL body' });
          }
        } else {
          issues.push({ severity: 'WARN', msg: label + ' (' + inp.NAME + ') declared but not referenced in GLSL body' });
        }
      }
    }

    // Check for duplicate INPUT names
    const names = inputs.filter(i => i && i.NAME).map(i => i.NAME);
    const seen = new Set();
    for (const n of names) {
      if (seen.has(n)) {
        issues.push({ severity: 'ERROR', msg: 'Duplicate INPUT NAME: "' + n + '"' });
      }
      seen.add(n);
    }
  }

  // 5. Check for IMPORTED resources (external image files)
  if (meta.IMPORTED) {
    if (typeof meta.IMPORTED === 'object' && !Array.isArray(meta.IMPORTED)) {
      for (const [key, val] of Object.entries(meta.IMPORTED)) {
        if (val && val.PATH) {
          const importPath = path.resolve(path.dirname(filePath), val.PATH);
          if (!fs.existsSync(importPath)) {
            issues.push({ severity: 'ERROR', msg: 'IMPORTED resource "' + key + '" file not found: ' + val.PATH });
          } else {
            issues.push({ severity: 'INFO', msg: 'IMPORTED resource "' + key + '" exists: ' + val.PATH });
          }
        }
      }
    }
  }

  // 6. Check for PERSISTENT buffers or PASSES referencing resources
  if (meta.PASSES) {
    if (!Array.isArray(meta.PASSES)) {
      issues.push({ severity: 'ERROR', msg: 'PASSES is not an array' });
    } else {
      for (let i = 0; i < meta.PASSES.length; i++) {
        const p = meta.PASSES[i];
        if (p && p.TARGET) {
          // Check that the target buffer is referenced in GLSL
          const glslBody = content.substring(block.endIndex);
          if (!glslUsesIdentifier(glslBody, p.TARGET)) {
            issues.push({ severity: 'WARN', msg: 'PASSES[' + i + '] TARGET "' + p.TARGET + '" not referenced in GLSL' });
          }
        }
      }
    }
  }

  // 7. Metadata block not at very start could indicate issues
  //    (content before the metadata that is NOT just #ifdef / uniform lines)
  const beforeMeta = content.substring(0, content.indexOf('/*'));
  const beforeMetaTrimmed = beforeMeta.replace(/#ifdef\s+\w+[\s\S]*?#endif/g, '')
    .replace(/uniform\s+.+?;[^\n]*/g, '')
    .replace(/precision\s+\w+\s+\w+;/g, '')
    .trim();
  if (beforeMetaTrimmed.length > 0 && block) {
    // There is significant non-boilerplate code before the ISF block
    issues.push({ severity: 'INFO', msg: 'Content before ISF metadata block (may affect ISF parsers)' });
  }

  return issues;
}

// ---- run -----------------------------------------------------------------

console.log('=== ISF Content Audit ===');
console.log('Scanning:', SHADER_ROOT);
console.log();

const allFiles = collectFS(SHADER_ROOT);
console.log('Total .fs files found:', allFiles.length);
console.log();

const stats = { total: 0, withErrors: 0, withWarnings: 0, plainGLSL: 0, clean: 0 };
const errorFiles = [];
const warnFiles = [];

for (const fp of allFiles) {
  stats.total++;
  const issues = auditFile(fp);
  const rel = path.relative(SHADER_ROOT, fp);

  const errors = issues.filter(i => i.severity === 'ERROR');
  const warns  = issues.filter(i => i.severity === 'WARN');
  const infos  = issues.filter(i => i.severity === 'INFO');

  const isPlainGLSL = infos.some(i => i.msg.includes('No ISF metadata block'));

  if (errors.length > 0) {
    stats.withErrors++;
    errorFiles.push({ file: rel, issues });
    console.log('[ERROR] ' + rel);
    for (const e of errors) console.log('        ' + e.msg);
    for (const w of warns) console.log('   WARN ' + w.msg);
    if (verbose) for (const ii of infos) console.log('   INFO ' + ii.msg);
  } else if (warns.length > 0) {
    stats.withWarnings++;
    warnFiles.push({ file: rel, issues });
    if (verbose) {
      console.log('[ WARN] ' + rel);
      for (const w of warns) console.log('        ' + w.msg);
    }
  } else if (isPlainGLSL) {
    stats.plainGLSL++;
    if (verbose) console.log('[ GLSL] ' + rel + ' (no ISF metadata)');
  } else {
    stats.clean++;
  }
}

// ---- summary -------------------------------------------------------------
console.log();
console.log('=== SUMMARY ===');
console.log('Total .fs files scanned: ', stats.total);
console.log('Clean ISF shaders:       ', stats.clean);
console.log('Plain GLSL (no ISF meta):', stats.plainGLSL);
console.log('Files with ERRORS:       ', stats.withErrors);
console.log('Files with WARNINGS only:', stats.withWarnings);
console.log();

if (errorFiles.length > 0) {
  console.log('=== FILES WITH ERRORS (potential bad-optional-access triggers) ===');
  for (const ef of errorFiles) {
    console.log();
    console.log('  ' + ef.file);
    for (const i of ef.issues) {
      if (i.severity === 'ERROR') console.log('    [ERROR] ' + i.msg);
    }
    for (const i of ef.issues) {
      if (i.severity === 'WARN') console.log('    [ WARN] ' + i.msg);
    }
  }
  console.log();
}

if (warnFiles.length > 0) {
  console.log('=== FILES WITH WARNINGS (non-critical) ===');
  for (const wf of warnFiles) {
    console.log('  ' + wf.file);
    for (const i of wf.issues.filter(x => x.severity === 'WARN')) {
      console.log('    ' + i.msg);
    }
  }
  console.log();
}

if (stats.withErrors > 0) {
  console.log('!! ' + stats.withErrors + ' file(s) have errors that could cause "bad optional access" in Resolume Wire.');
  console.log('   Malformed JSON metadata -> ISF parser returns empty optional -> crash on .value()');
  process.exit(1);
} else {
  console.log('No critical ISF metadata errors found.');
  process.exit(0);
}
