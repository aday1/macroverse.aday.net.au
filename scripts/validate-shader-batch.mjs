#!/usr/bin/env node
/**
 * Compile + blank-check shader batch.
 * Usage: node scripts/validate-shader-batch.mjs [dir] [--move-failures] [--json report.json]
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  loadPrepScript,
  walkShaders,
  launchBrowser,
  createQaPage,
  qaShaderInPage,
  analyzePixels,
  THUMB_W,
  THUMB_H,
} from './lib/shader-qa-core.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, '..');
const targetDir = path.resolve(process.argv[2] || path.join(root, 'shaders', 'VJ-Generated'));
const moveFailures = process.argv.includes('--move-failures');
const jsonOut = process.argv.find((a) => a.startsWith('--json='))?.split('=')[1]
  || path.join(root, 'scripts', 'reports', `shader-qa-${new Date().toISOString().slice(0, 10)}.json`);

const quarantineRoot = path.join(root, 'shaders', '_quarantine');
const onlyWave = process.argv.find((a) => a.startsWith('--only-wave='))?.split('=')[1] || '';

async function main() {
  let files = walkShaders(targetDir);
  if (onlyWave) {
    const needle = `-${onlyWave}-`;
    files = files.filter((f) => path.basename(f).includes(needle));
  }
  if (!files.length) {
    console.error('No shaders in', targetDir);
    process.exit(1);
  }

  const puppeteer = await import('puppeteer');
  const prepSrc = loadPrepScript();
  const browser = await launchBrowser(puppeteer);
  const page = await createQaPage(browser, prepSrc);

  const report = { dir: targetDir, at: new Date().toISOString(), total: files.length, passed: [], failed: [] };

  for (const file of files) {
    const rel = path.relative(root, file).replace(/\\/g, '/');
    const src = fs.readFileSync(file, 'utf8');
    try {
      const result = await qaShaderInPage(page, src, { thumbnail: false });
      if (result.error) throw new Error(result.error);
      const analysis = analyzePixels(new Uint8Array(result.pixels), THUMB_W, THUMB_H);
      if (analysis.isBlank) throw new Error(`blank render var=${analysis.variance.toFixed(1)} black=${(analysis.blackRatio * 100).toFixed(1)}%`);
      report.passed.push({ path: rel, variance: analysis.variance });
      console.log('OK  ', rel);
    } catch (err) {
      const msg = String(err.message || err);
      report.failed.push({ path: rel, error: msg });
      console.error('FAIL', rel, msg);
      if (moveFailures) {
        const dest = path.join(quarantineRoot, path.relative(path.join(root, 'shaders'), file));
        fs.mkdirSync(path.dirname(dest), { recursive: true });
        if (fs.existsSync(file)) {
          fs.renameSync(file, dest);
        }
      }
    }
  }

  await browser.close();
  fs.mkdirSync(path.dirname(jsonOut), { recursive: true });
  fs.writeFileSync(jsonOut, JSON.stringify(report, null, 2), 'utf8');

  const passRate = report.passed.length / report.total;
  console.log(`\nQA: ${report.passed.length}/${report.total} passed (${(passRate * 100).toFixed(1)}%)`);
  console.log('Report:', jsonOut);
  if (report.failed.length) process.exit(1);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});