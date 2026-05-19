/**
 * Bulk thumbnail generator: runs outside Macroverse exe, drives the app in headless
 * Chrome to render each shader and POST thumbnails to the API. Much faster than
 * generating one-by-one in the UI.
 *
 * Prereqs: Macroverse server running (e.g. http://localhost:8765). Node with puppeteer.
 * Usage: from repo root, run:
 *   node scripts/bulk-thumbnails.js
 *   BASE_URL=http://localhost:8765 node scripts/bulk-thumbnails.js
 *   node scripts/bulk-thumbnails.js --concurrency 4
 *
 * Path keys in thumbnails.json use pipe (|) not backslash; the script sends paths in that form.
 */

const fs = require('fs');
const path = require('path');

const BASE = process.env.BASE_URL || 'http://localhost:8765';
const concurrency = parseInt(process.argv.find(a => a.startsWith('--concurrency='))?.split('=')[1] || '1', 10) || 1;

function findSystemChrome() {
  const candidates = [];
  const local = process.env.LOCALAPPDATA || '';
  const programFiles = process.env['ProgramFiles'] || 'C:\\Program Files';
  const programFilesX86 = process.env['ProgramFiles(x86)'] || 'C:\\Program Files (x86)';
  if (local) {
    candidates.push(path.join(local, 'Google', 'Chrome', 'Application', 'chrome.exe'));
    candidates.push(path.join(local, 'Microsoft', 'Edge', 'Application', 'msedge.exe'));
  }
  candidates.push(path.join(programFiles, 'Google', 'Chrome', 'Application', 'chrome.exe'));
  candidates.push(path.join(programFilesX86, 'Microsoft', 'Edge', 'Application', 'msedge.exe'));
  candidates.push(path.join(programFiles, 'Microsoft', 'Edge', 'Application', 'msedge.exe'));
  for (const p of candidates) {
    if (fs.existsSync(p)) return p;
  }
  return null;
}

async function launchBrowser(puppeteer) {
  const opts = {
    headless: true,
    args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage', '--disable-gpu']
  };
  try {
    return await puppeteer.default.launch(opts);
  } catch (err) {
    if (err.message && err.message.includes('Could not find Chrome')) {
      const exe = findSystemChrome();
      if (exe) {
        console.log('Using system browser: ' + exe);
        opts.executablePath = exe;
        return await puppeteer.default.launch(opts);
      }
      console.error('Install Chrome for Puppeteer: npx puppeteer browsers install chrome');
    }
    throw err;
  }
}

async function main() {
  let puppeteer;
  try {
    puppeteer = await import('puppeteer');
  } catch {
    console.error('puppeteer not installed. From repo root run: npm install puppeteer');
    process.exit(1);
  }

  const indexUrl = BASE.replace(/\/$/, '') + '/api/index';
  const res = await fetch(indexUrl);
  if (!res.ok) {
    console.error('GET ' + indexUrl + ' failed: ' + res.status);
    process.exit(1);
  }
  const entries = await res.json();
  if (!Array.isArray(entries) || entries.length === 0) {
    console.log('No entries in index. Nothing to do.');
    return;
  }

  const paths = entries.map(e => (e.path || '').trim()).filter(Boolean);
  if (paths.length === 0) {
    console.log('No paths in index.');
    return;
  }

  console.log('Bulk thumbnails: ' + paths.length + ' shaders, concurrency=' + concurrency);

  const browser = await launchBrowser(puppeteer);

  const pageUrl = BASE.replace(/\/$/, '') + '/?bulk=1';
  const pages = [];
  for (let i = 0; i < concurrency; i++) {
    const page = await browser.newPage();
    await page.goto(pageUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForFunction(
      () => typeof window.renderThumbnailSyncForBulk === 'function',
      { timeout: 60000 }
    );
    pages.push(page);
  }

  let nextPage = 0;
  let done = 0;
  let failed = 0;

  async function processOne(path) {
    const page = pages[nextPage % pages.length];
    nextPage += 1;
    const pathPipe = path.replace(/\\/g, '|');
    const shaderUrl = BASE.replace(/\/$/, '') + '/api/shader?path=' + encodeURIComponent(pathPipe);
    let src;
    try {
      const r = await fetch(shaderUrl);
      if (!r.ok) return;
      src = await r.text();
    } catch {
      return;
    }
    const dataUrl = await page.evaluate((code) => {
      return window.renderThumbnailSyncForBulk ? window.renderThumbnailSyncForBulk(code) : null;
    }, src);
    if (!dataUrl) return;
    const postUrl = BASE.replace(/\/$/, '') + '/api/thumbnail';
    try {
      const pr = await fetch(postUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ path: pathPipe, dataUrl })
      });
      if (pr.ok) return true;
    } catch {}
    return false;
  }

  const queue = [...paths];
  const workers = [];
  for (let w = 0; w < concurrency; w++) {
    workers.push((async () => {
      while (queue.length > 0) {
        const path = queue.shift();
        if (!path) break;
        try {
          const ok = await processOne(path);
          if (ok) done += 1; else failed += 1;
        } catch (_) {
          failed += 1;
        }
        const total = done + failed;
        if (total % 10 === 0 || total === paths.length) {
          console.log(total + '/' + paths.length + ' done, ok=' + done + ' fail=' + failed);
        }
      }
    })());
  }
  await Promise.all(workers);

  await browser.close();
  console.log('Done. ok=' + done + ' failed=' + failed);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
