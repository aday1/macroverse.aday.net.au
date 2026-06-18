#!/usr/bin/env node
/**
 * Offline thumbnail baker -> thumbnails-baked.json
 * Usage: node scripts/bake-thumbnails-offline.mjs [--dir path] [--merge] [--concurrency=2]
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
const outFile = path.join(root, 'thumbnails-baked.json');
const dirArg = process.argv.find((a) => a.startsWith('--dir='))?.split('=')[1];
const positional = process.argv.slice(2).filter((a) => !a.startsWith('--') && !a.startsWith('-') && !a.endsWith('.mjs'));
const targetDir = path.resolve(dirArg || positional[0] || path.join(root, 'shaders', 'VJ-Generated'));
const merge = process.argv.includes('--merge');
const concurrency = parseInt(process.argv.find((a) => a.startsWith('--concurrency='))?.split('=')[1] || '2', 10);

function pathKey(p) {
  return p.replace(/\\/g, '|');
}

async function main() {
  let cache = {};
  if (merge && fs.existsSync(outFile)) {
    try {
      cache = JSON.parse(fs.readFileSync(outFile, 'utf8'));
    } catch {
      cache = {};
    }
  }

  const files = walkShaders(targetDir);
  const todo = files.filter((f) => !cache[pathKey(path.relative(root, f).replace(/\\/g, '/'))]);
  console.log(`Baking ${todo.length}/${files.length} thumbnails (merge=${merge})`);

  if (!todo.length) {
    console.log('Nothing to bake.');
    return;
  }

  const puppeteer = await import('puppeteer');
  const prepSrc = loadPrepScript();
  const browser = await launchBrowser(puppeteer);
  const pages = [];
  for (let i = 0; i < concurrency; i++) {
    pages.push(await createQaPage(browser, prepSrc));
  }

  let done = 0;
  let failed = 0;
  let pageIdx = 0;

  async function bakeOne(file) {
    const rel = path.relative(root, file).replace(/\\/g, '/');
    const key = pathKey(rel);
    const page = pages[pageIdx++ % pages.length];
    const src = fs.readFileSync(file, 'utf8');
    try {
      const result = await qaShaderInPage(page, src, { thumbnail: true });
      if (result.error) throw new Error(result.error);
      const analysis = analyzePixels(new Uint8Array(result.pixels), THUMB_W, THUMB_H);
      if (analysis.isBlank) throw new Error('blank');
      if (result.dataUrl) {
        cache[key] = result.dataUrl;
        done++;
      }
    } catch {
      failed++;
    }
    if ((done + failed) % 20 === 0) console.log(`${done + failed}/${todo.length} ok=${done} fail=${failed}`);
  }

  const queue = [...todo];
  const workers = Array.from({ length: concurrency }, async () => {
    while (queue.length) {
      const f = queue.shift();
      if (f) await bakeOne(f);
    }
  });
  await Promise.all(workers);
  await browser.close();

  fs.writeFileSync(outFile, JSON.stringify(cache, null, 0), 'utf8');
  console.log(`Wrote ${outFile} entries=${Object.keys(cache).length} new_ok=${done} fail=${failed}`);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});