#!/usr/bin/env node
/**
 * Compile-test Macroverse ISF shaders via headless Chrome + shader-prep.
 * Usage: node scripts/test-macroverse-shaders.mjs [glob-dir]
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, '..');
const prepPath = path.join(
  process.env.MACROVERSE_SHADER_PREP ||
    'C:/Users/aday/Desktop/Obsidian/YomikosPapers/09-network-homelab/macroverse-shaders/standalone/shader-prep.js'
);


const targetDir = process.argv[2]
  ? path.resolve(process.argv[2])
  : path.join(root, 'shaders/starter-pack/macroverse');

function walkFs(dir) {
  const out = [];
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, ent.name);
    if (ent.isDirectory()) out.push(...walkFs(full));
    else if (ent.name.endsWith('.fs')) out.push(full);
  }
  return out;
}

const prepSrc = fs.readFileSync(prepPath, 'utf8');
const vertSrc = `precision highp float;
attribute vec2 a_pos;
varying vec2 v_uv;
varying vec2 surfacePosition;
void main() {
  vec2 uv = a_pos * 0.5 + 0.5;
  v_uv = uv;
  surfacePosition = uv;
  gl_Position = vec4(a_pos, 0.0, 1.0);
}`;
const files = walkFs(targetDir);
if (files.length === 0) {
  console.error('No .fs files in', targetDir);
  process.exit(1);
}

const browser = await puppeteer.launch({ headless: true, args: ['--no-sandbox'] });
const page = await browser.newPage();
await page.setContent('<!doctype html><canvas id="c"></canvas>');
await page.addScriptTag({ content: prepSrc });

let failed = 0;
for (const file of files) {
  const src = fs.readFileSync(file, 'utf8');
  const rel = path.relative(root, file).replace(/\\/g, '/');
  try {
    const err = await page.evaluate((fragment, vert) => {
      const gl = document.createElement('canvas').getContext('webgl');
      if (!gl) return 'WebGL unavailable';
      const prep = window.ShaderPrep;
      const prepared = prep.prepareFragmentForOffscreenRender(prep.stripLeadingGarbage(fragment || ''));
      function compile(type, source) {
        const s = gl.createShader(type);
        gl.shaderSource(s, source);
        gl.compileShader(s);
        if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) {
          return gl.getShaderInfoLog(s) || 'compile failed';
        }
        return null;
      }
      const ve = compile(gl.VERTEX_SHADER, vert);
      if (ve) return ve;
      const fe = compile(gl.FRAGMENT_SHADER, prepared);
      if (fe) return fe;
      const p = gl.createProgram();
      const v = gl.createShader(gl.VERTEX_SHADER);
      gl.shaderSource(v, vert);
      gl.compileShader(v);
      const f = gl.createShader(gl.FRAGMENT_SHADER);
      gl.shaderSource(f, prepared);
      gl.compileShader(f);
      gl.attachShader(p, v);
      gl.attachShader(p, f);
      gl.linkProgram(p);
      if (!gl.getProgramParameter(p, gl.LINK_STATUS)) {
        return gl.getProgramInfoLog(p) || 'link failed';
      }
      return null;
    }, src, vertSrc);
    if (err) throw new Error(err);
    console.log('OK  ', rel);
  } catch (err) {
    failed++;
    console.error('FAIL', rel, String(err.message || err));
  }
}

await browser.close();
if (failed) process.exit(1);
console.log(`All ${files.length} shader(s) compiled.`);