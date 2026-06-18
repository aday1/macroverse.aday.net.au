#!/usr/bin/env node
/**
 * Parametric VJ ISF shader generator (Tier A + mutations).
 * Usage: node scripts/generate-vj-shaders.mjs [--batch N] [--mutate dir]
 */
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, '..');
const outRoot = path.join(root, 'shaders', 'VJ-Generated', 'ISF');

const COMMON_INPUTS = `        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 }`;

const COMMON_DEFINES = `
#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif
`;

function isfHeader({ description, category, tags, inputs }) {
  const tagList = ['generated', 'vj', 'wired-atelier-2026', category, ...tags];
  return `/*{
    "DESCRIPTION": "${description}",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["${category}"],
    "TAGS": ${JSON.stringify(tagList)},
    "INPUTS": [
${inputs || COMMON_INPUTS}
    ]
}*/`;
}

function hash(seed) {
  const h = crypto.createHash('sha256').update(String(seed)).digest();
  return {
    f: (i) => (h[i % 32] / 255),
    i: (i, max) => Math.floor((h[i % 32] / 255) * max),
  };
}

const TEMPLATES = [
  {
    id: 'copper-field',
    category: 'macroverse',
    tags: ['macroverse-origin', 'cosmic', 'ambient'],
    build: (s) => {
      const h = hash(s);
      const ripple = (0.2 + h.f(0) * 0.5).toFixed(3);
      const drift = (0.08 + h.f(1) * 0.2).toFixed(3);
      const scale = (1.5 + h.f(2) * 3).toFixed(2);
      return `${isfHeader({ description: `Copper field ${s}`, category: 'macroverse', tags: ['macroverse-origin', 'cosmic'], inputs: `${COMMON_INPUTS},
        { "NAME": "rippleAmp", "TYPE": "float", "DEFAULT": ${ripple}, "MIN": 0.0, "MAX": 1.0 },
        { "NAME": "driftSpeed", "TYPE": "float", "DEFAULT": ${drift}, "MIN": 0.0, "MAX": 0.5 },
        { "NAME": "fieldScale", "TYPE": "float", "DEFAULT": ${scale}, "MIN": 0.5, "MAX": 5.0 }` })}
${COMMON_DEFINES}
float hash21(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float noise(vec2 p) {
    vec2 i = floor(p); vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash21(i), hash21(i + vec2(1,0)), f.x), mix(hash21(i+vec2(0,1)), hash21(i+vec2(1,1)), f.x), f.y);
}
float fbm(vec2 p) {
    float v = 0.0; float a = 0.5;
    for (int i = 0; i < 5; i++) { v += a * noise(p); p *= 2.1; a *= 0.5; }
    return v;
}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec2 p = (uv - 0.5) * vec2(resolution.x / resolution.y, 1.0) * fieldScale;
    p += vec2(fbm(p + time * driftSpeed), fbm(p + vec2(4.2, 1.7) - time * driftSpeed * 0.7)) * rippleAmp * 0.5 - rippleAmp * 0.25;
    float n = fbm(p);
    vec3 copper = vec3(0.85, 0.45, 0.12);
    vec3 voidCol = vec3(0.02, 0.04, 0.12);
    vec3 col = mix(voidCol, copper, smoothstep(0.25, 0.85, n));
    col += vec3(0.15, 0.08, 0.02) * pow(n, 3.0);
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'techno-grid',
    category: 'techno',
    tags: ['vj-techno', 'grid'],
    build: (s) => {
      const h = hash(s);
      const cells = 4 + h.i(0, 12);
      const pulse = (0.5 + h.f(1) * 2).toFixed(2);
      return `${isfHeader({ description: `Techno grid ${s}`, category: 'techno', tags: ['vj-techno'], inputs: `${COMMON_INPUTS},
        { "NAME": "cellCount", "TYPE": "float", "DEFAULT": ${cells}.0, "MIN": 2.0, "MAX": 24.0 },
        { "NAME": "pulseRate", "TYPE": "float", "DEFAULT": ${pulse}, "MIN": 0.2, "MAX": 4.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec2 p = uv * cellCount;
    vec2 g = abs(fract(p - 0.5) - 0.5);
    float line = smoothstep(0.02, 0.0, min(g.x, g.y));
    float beat = 0.5 + 0.5 * sin(time * pulseRate * 6.283);
    vec3 cyan = vec3(0.0, 0.9, 0.85);
    vec3 magenta = vec3(0.9, 0.0, 0.5);
    vec3 col = mix(vec3(0.03), mix(cyan, magenta, beat), line);
    col += vec3(0.08, 0.02, 0.12) * (1.0 - line) * beat;
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'cosmic-streak',
    category: 'cosmic',
    tags: ['vj-cosmic', 'space'],
    build: (s) => {
      const h = hash(s);
      const speed = (0.3 + h.f(0) * 1.2).toFixed(2);
      return `${isfHeader({ description: `Cosmic streak ${s}`, category: 'cosmic', tags: ['vj-cosmic'], inputs: `${COMMON_INPUTS},
        { "NAME": "warpSpeed", "TYPE": "float", "DEFAULT": ${speed}, "MIN": 0.1, "MAX": 2.0 }` })}
${COMMON_DEFINES}
float star(vec2 uv, float t) {
    vec2 id = floor(uv);
    float n = fract(sin(dot(id, vec2(12.9898, 78.233))) * 43758.5453);
    vec2 ctr = id + vec2(n, fract(n * 7.13));
    float d = length(uv - ctr);
    float streak = smoothstep(0.08, 0.0, abs(uv.y - ctr.y + sin(uv.x * 3.0 + t) * 0.02));
    return streak * smoothstep(0.15, 0.0, d) * step(0.92, n);
}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec2 p = (uv - 0.5) * vec2(resolution.x / resolution.y, 1.0);
    p.x += time * warpSpeed * 0.15;
    float s = star(p * 40.0, time * warpSpeed);
    vec3 col = vec3(0.02, 0.03, 0.08);
    col += vec3(0.7, 0.85, 1.0) * s;
    col += vec3(0.9, 0.5, 1.0) * s * 0.4;
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'psyche-kaleido',
    category: 'psychedelic',
    tags: ['vj-colour', 'kaleidoscope'],
    build: (s) => {
      const h = hash(s);
      const seg = 3 + h.i(0, 9);
      return `${isfHeader({ description: `Kaleido pulse ${s}`, category: 'psychedelic', tags: ['vj-colour'], inputs: `${COMMON_INPUTS},
        { "NAME": "segments", "TYPE": "float", "DEFAULT": ${seg}.0, "MIN": 3.0, "MAX": 16.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy - 0.5;
    uv.x *= resolution.x / resolution.y;
    float a = atan(uv.y, uv.x);
    float r = length(uv);
    float seg = max(segments, 3.0);
    a = mod(a, 6.28318 / seg);
    a = abs(a - 3.14159 / seg);
    vec2 k = vec2(cos(a), sin(a)) * r;
    float v = sin(k.x * 12.0 + time) * cos(k.y * 10.0 - time * 0.7);
    vec3 col = 0.5 + 0.5 * cos(vec3(0.0, 2.1, 4.2) + v * 3.0 + time);
    col *= smoothstep(0.8, 0.1, r);
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'geo-rings',
    category: 'geometric',
    tags: ['vj-geometric'],
    build: (s) => {
      const h = hash(s);
      const rings = 3 + h.i(0, 8);
      return `${isfHeader({ description: `Neon rings ${s}`, category: 'geometric', tags: ['vj-geometric'], inputs: `${COMMON_INPUTS},
        { "NAME": "ringCount", "TYPE": "float", "DEFAULT": ${rings}.0, "MIN": 2.0, "MAX": 14.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy - 0.5;
    uv.x *= resolution.x / resolution.y;
    float r = length(uv) * ringCount * 2.0;
    float ring = abs(fract(r - time * 0.2) - 0.5);
    ring = smoothstep(0.08, 0.0, ring);
    vec3 col = vec3(0.02, 0.05, 0.08);
    col += vec3(0.1, 0.8, 0.95) * ring;
    col += vec3(0.95, 0.35, 0.1) * ring * sin(r * 6.0 + time);
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'ambient-plasma',
    category: 'ambient',
    tags: ['vj-ambient', 'plasma'],
    build: (s) => {
      const h = hash(s);
      const scale = (2 + h.f(0) * 6).toFixed(2);
      return `${isfHeader({ description: `Soft plasma ${s}`, category: 'ambient', tags: ['vj-ambient'], inputs: `${COMMON_INPUTS},
        { "NAME": "plasmaScale", "TYPE": "float", "DEFAULT": ${scale}, "MIN": 1.0, "MAX": 10.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy * plasmaScale;
    float v = sin(uv.x + time * 0.3) + sin(uv.y + time * 0.2) + sin(uv.x + uv.y + time * 0.15);
    vec3 col = 0.55 + 0.45 * cos(vec3(0.2, 1.4, 2.6) + v * 1.8);
    col = mix(vec3(0.04, 0.06, 0.1), col, 0.85);
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'plasma-fire',
    category: 'plasma',
    tags: ['vj-colour', 'plasma'],
    build: (s) => {
      const h = hash(s);
      const intensity = (0.6 + h.f(0) * 1.4).toFixed(2);
      return `${isfHeader({ description: `Plasma fire ${s}`, category: 'plasma', tags: ['vj-colour'], inputs: `${COMMON_INPUTS},
        { "NAME": "intensity", "TYPE": "float", "DEFAULT": ${intensity}, "MIN": 0.3, "MAX": 2.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float x = uv.x * 8.0 * intensity;
    float y = uv.y * 8.0;
    float t = time * 0.6;
    float v = sin(x + t) + sin(y + t * 0.5) + sin(x + y + t);
    vec3 col = vec3(sin(v), sin(v + 2.1), sin(v + 4.2)) * 0.5 + 0.5;
    col = pow(col, vec3(1.4));
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'tunnel-zoom',
    category: 'tunnel',
    tags: ['vj-geometric', 'tunnel'],
    build: (s) => {
      const h = hash(s);
      const depth = (0.4 + h.f(0) * 1.2).toFixed(2);
      return `${isfHeader({ description: `Lightspeed tunnel ${s}`, category: 'tunnel', tags: ['vj-geometric'], inputs: `${COMMON_INPUTS},
        { "NAME": "zoomSpeed", "TYPE": "float", "DEFAULT": ${depth}, "MIN": 0.1, "MAX": 2.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy - 0.5;
    uv.x *= resolution.x / resolution.y;
    float a = atan(uv.y, uv.x);
    float r = length(uv);
    float tunnel = fract(1.0 / (r + 0.05) - time * zoomSpeed);
    float spokes = 0.5 + 0.5 * sin(a * 8.0 + time);
    vec3 col = mix(vec3(0.02, 0.0, 0.08), vec3(0.2, 0.9, 0.7), tunnel * spokes);
    col *= smoothstep(0.7, 0.05, r);
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'techno-bars',
    category: 'techno',
    tags: ['vj-techno', 'color'],
    build: (s) => {
      const h = hash(s);
      const bars = 5 + h.i(0, 8);
      return `${isfHeader({ description: `Pulse bars ${s}`, category: 'techno', tags: ['vj-techno'], inputs: `${COMMON_INPUTS},
        { "NAME": "barCount", "TYPE": "float", "DEFAULT": ${bars}.0, "MIN": 3.0, "MAX": 16.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float bar = floor(uv.x * barCount);
    float hbar = 0.35 + 0.35 * sin(bar * 1.7 + time * 3.0);
    float mask = step(uv.y, hbar);
    vec3 cols = 0.5 + 0.5 * cos(vec3(0, 1.2, 2.4) + bar * 0.8);
    vec3 col = mix(vec3(0.02), cols, mask);
    col *= 0.8 + 0.2 * sin(time * 8.0 + bar);
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'cosmic-nebula',
    category: 'cosmic',
    tags: ['vj-cosmic', 'vj-ambient'],
    build: (s) => {
      const h = hash(s);
      const swirl = (0.5 + h.f(0) * 2).toFixed(2);
      return `${isfHeader({ description: `Nebula swirl ${s}`, category: 'cosmic', tags: ['vj-cosmic'], inputs: `${COMMON_INPUTS},
        { "NAME": "swirl", "TYPE": "float", "DEFAULT": ${swirl}, "MIN": 0.2, "MAX": 3.0 }` })}
${COMMON_DEFINES}
float hash21(vec2 p) { return fract(sin(dot(p, vec2(41.3, 89.7))) * 1031.73); }
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec2 p = uv - 0.5;
    float a = atan(p.y, p.x) + length(p) * swirl + time * 0.1;
    float n = hash21(vec2(a * 2.0, length(p) * 5.0 - time * 0.05));
    vec3 col = mix(vec3(0.02, 0.01, 0.08), vec3(0.5, 0.15, 0.9), n);
    col += vec3(0.9, 0.4, 0.2) * pow(n, 4.0);
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'psyche-vortex',
    category: 'psychedelic',
    tags: ['vj-colour'],
    build: (s) => {
      const h = hash(s);
      const twist = (1 + h.f(0) * 4).toFixed(2);
      return `${isfHeader({ description: `Color vortex ${s}`, category: 'psychedelic', tags: ['vj-colour'], inputs: `${COMMON_INPUTS},
        { "NAME": "twist", "TYPE": "float", "DEFAULT": ${twist}, "MIN": 0.5, "MAX": 6.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy - 0.5;
    uv.x *= resolution.x / resolution.y;
    float r = length(uv);
    float a = atan(uv.y, uv.x) + r * twist + time * 0.5;
    float v = sin(a * 5.0) * cos(r * 20.0 - time);
    vec3 col = 0.5 + 0.5 * cos(vec3(0, 1.5, 3.0) + v * 4.0 + time);
    col *= smoothstep(0.75, 0.0, r);
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'geo-voronoi',
    category: 'geometric',
    tags: ['vj-geometric', 'grid'],
    build: (s) => {
      const h = hash(s);
      const cells = 3 + h.i(0, 6);
      return `${isfHeader({ description: `Voronoi glow ${s}`, category: 'geometric', tags: ['vj-geometric'], inputs: `${COMMON_INPUTS},
        { "NAME": "cells", "TYPE": "float", "DEFAULT": ${cells}.0, "MIN": 2.0, "MAX": 12.0 }` })}
${COMMON_DEFINES}
float hash21(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy * cells;
    vec2 ip = floor(uv);
    vec2 fp = fract(uv);
    float md = 1.0;
    for (int y = -1; y <= 1; y++) {
      for (int x = -1; x <= 1; x++) {
        vec2 g = vec2(float(x), float(y));
        vec2 o = vec2(hash21(ip + g), hash21(ip + g + 17.0));
        o = 0.5 + 0.5 * sin(time * 0.3 + 6.283 * o);
        float d = length(g + o - fp);
        md = min(md, d);
      }
    }
    vec3 col = vec3(0.03, 0.06, 0.1);
    col += vec3(0.2, 0.85, 0.75) * smoothstep(0.08, 0.0, md);
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'ambient-cloud',
    category: 'ambient',
    tags: ['vj-ambient', 'noise'],
    build: (s) => {
      const h = hash(s);
      const scale = (1.5 + h.f(0) * 4).toFixed(2);
      return `${isfHeader({ description: `Cloud drift ${s}`, category: 'ambient', tags: ['vj-ambient'], inputs: `${COMMON_INPUTS},
        { "NAME": "cloudScale", "TYPE": "float", "DEFAULT": ${scale}, "MIN": 0.5, "MAX": 6.0 }` })}
${COMMON_DEFINES}
float hash21(vec2 p) { return fract(sin(dot(p, vec2(12.3, 45.6))) * 43758.5453); }
float noise(vec2 p) {
    vec2 i = floor(p); vec2 f = fract(p);
    f = f*f*(3.0-2.0*f);
    return mix(mix(hash21(i),hash21(i+vec2(1,0)),f.x),mix(hash21(i+vec2(0,1)),hash21(i+vec2(1,1)),f.x),f.y);
}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float n = noise(uv * cloudScale + vec2(time * 0.03, 0.0));
    n += 0.5 * noise(uv * cloudScale * 2.0 - time * 0.02);
    vec3 col = mix(vec3(0.05, 0.07, 0.12), vec3(0.5, 0.65, 0.85), n);
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'macroverse-orbit',
    category: 'macroverse',
    tags: ['macroverse-origin', 'vj-cosmic'],
    build: (s) => {
      const h = hash(s);
      const orbs = 2 + h.i(0, 5);
      return `${isfHeader({ description: `Orbit glow ${s}`, category: 'macroverse', tags: ['macroverse-origin'], inputs: `${COMMON_INPUTS},
        { "NAME": "orbCount", "TYPE": "float", "DEFAULT": ${orbs}.0, "MIN": 1.0, "MAX": 8.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy - 0.5;
    uv.x *= resolution.x / resolution.y;
    vec3 col = vec3(0.01, 0.02, 0.06);
    for (float i = 0.0; i < 8.0; i++) {
        if (i >= orbCount) break;
        float a = time * (0.2 + i * 0.07) + i * 1.2;
        vec2 c = vec2(cos(a), sin(a)) * (0.15 + i * 0.08);
        float d = length(uv - c);
        col += vec3(0.9, 0.5, 0.15) * 0.08 / (d + 0.02);
        col += vec3(0.2, 0.5, 0.95) * 0.04 / (d + 0.05);
    }
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'tunnel-pipes',
    category: 'tunnel',
    tags: ['vj-geometric'],
    build: (s) => {
      const h = hash(s);
      const pipes = 4 + h.i(0, 8);
      return `${isfHeader({ description: `Retro pipes ${s}`, category: 'tunnel', tags: ['vj-geometric'], inputs: `${COMMON_INPUTS},
        { "NAME": "pipeCount", "TYPE": "float", "DEFAULT": ${pipes}.0, "MIN": 3.0, "MAX": 16.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy - 0.5;
    uv.x *= resolution.x / resolution.y;
    float a = atan(uv.y, uv.x);
    float r = length(uv);
    float seg = 6.28318 / pipeCount;
    float pipe = smoothstep(0.04, 0.0, abs(mod(a + time * 0.2, seg) - seg * 0.5));
    float depth = fract(1.0 / (r + 0.08) - time * 0.4);
    vec3 col = vec3(0.12, 0.04, 0.18) + vec3(0.45, 0.95, 0.55) * pipe * depth;
    col += vec3(0.25, 0.15, 0.35) * (1.0 - pipe) * 0.4;
    col *= smoothstep(0.75, 0.02, r) * 0.85 + 0.15;
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
];

function existingNames(dir) {
  const names = new Set();
  if (!fs.existsSync(dir)) return names;
  for (const f of walkAllFs(dir)) names.add(path.basename(f));
  return names;
}

function walkAllFs(dir) {
  const out = [];
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, ent.name);
    if (ent.isDirectory()) out.push(...walkAllFs(full));
    else if (ent.name.endsWith('.fs')) out.push(full);
  }
  return out;
}

function writeShader(category, filename, body) {
  const dir = path.join(outRoot, category);
  fs.mkdirSync(dir, { recursive: true });
  const dest = path.join(dir, filename);
  if (fs.existsSync(dest)) return null;
  fs.writeFileSync(dest, body, 'utf8');
  return dest;
}

function generateTierA(variantsPerTemplate = 10) {
  const written = [];
  const globalNames = existingNames(outRoot);
  for (const tpl of TEMPLATES) {
    for (let v = 0; v < variantsPerTemplate; v++) {
      const seed = `${tpl.id}-${v}-${Date.now()}`;
      const wavePart = wave ? `-${wave}` : '';
      const name = `${tpl.id}${wavePart}-${String(v).padStart(3, '0')}.fs`;
      if (globalNames.has(name)) continue;
      const body = tpl.build(seed);
      const dest = writeShader(tpl.category, name, body);
      if (dest) {
        written.push(dest);
        globalNames.add(name);
      }
    }
  }
  return written;
}

function mutateFromPasses(srcDir, count = 30) {
  const written = [];
  const files = fs.readdirSync(srcDir).filter((f) => f.endsWith('.fs'));
  for (let i = 0; i < count && files.length; i++) {
    const base = files[i % files.length];
    const body = fs.readFileSync(path.join(srcDir, base), 'utf8');
    const mutated = body.replace(/"DEFAULT": ([0-9.]+)/g, (m, n) => {
      const v = parseFloat(n);
      const nv = Math.max(0.01, v * (0.85 + (i % 5) * 0.06));
      return `"DEFAULT": ${nv.toFixed(3)}`;
    });
    const name = base.replace('.fs', `-m${i}.fs`);
    const cat = path.basename(srcDir);
    const dest = writeShader(cat, name, mutated);
    if (dest) written.push(dest);
  }
  return written;
}

const args = process.argv.slice(2);
const batch = parseInt(args.find((a) => a.startsWith('--batch='))?.split('=')[1] || '10', 10);
const mutateOnly = args.includes('--mutate');
const wave = args.find((a) => a.startsWith('--wave='))?.split('=')[1] || '';

let written = [];
if (mutateOnly) {
  for (const cat of fs.readdirSync(outRoot, { withFileTypes: true }).filter((d) => d.isDirectory())) {
    written.push(...mutateFromPasses(path.join(outRoot, cat.name), batch));
  }
} else {
  written = generateTierA(batch);
}

console.log(`Generated ${written.length} shader(s) under shaders/VJ-Generated/ISF/`);
for (const w of written.slice(0, 5)) console.log('  ' + path.relative(root, w));
if (written.length > 5) console.log(`  ... +${written.length - 5} more`);