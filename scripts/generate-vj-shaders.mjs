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
    vec3 col = mix(voidCol, copper, smoothstep(0.15, 0.75, n));
    col += vec3(0.22, 0.12, 0.04) * pow(n, 2.0);
    col += vec3(0.06, 0.08, 0.14) * (1.0 - n) * 0.35;
    col = max(col, vec3(0.04, 0.05, 0.09));
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
    float s = star(p * 36.0, time * warpSpeed);
    float neb = sin(p.x * 3.0 + time * 0.2) * cos(p.y * 2.5 - time * 0.15) * 0.5 + 0.5;
    vec3 col = vec3(0.08, 0.1, 0.22) + vec3(0.12, 0.08, 0.2) * neb;
    col += vec3(0.75, 0.88, 1.0) * s * 1.4;
    col += vec3(0.95, 0.55, 1.0) * s * 0.55;
    col = max(col, vec3(0.05, 0.06, 0.12));
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
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec2 p = uv * max(cells, 2.0);
    vec2 ip = floor(p);
    vec2 fp = fract(p) - 0.5;
    vec2 rnd = vec2(hash21(ip), hash21(ip + 19.0));
    vec2 ctr = 0.35 * sin(time * 0.25 + rnd * 6.283) * 0.5;
    float md = length(fp - ctr);
    float edge = smoothstep(0.22, 0.02, md);
    float fill = hash21(ip + floor(time * 0.15));
    vec3 col = vec3(0.1, 0.14, 0.22);
    col = mix(col, vec3(0.2, 0.55, 0.5), fill * 0.65);
    col += vec3(0.45, 0.98, 0.88) * edge;
    col = max(col, vec3(0.08, 0.1, 0.14));
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
    float vignette = smoothstep(0.8, 0.08, r);
    vec3 col = vec3(0.18, 0.08, 0.28) + vec3(0.55, 0.98, 0.65) * pipe * depth;
    col += vec3(0.35, 0.22, 0.48) * (1.0 - pipe) * 0.55;
    col += vec3(0.08, 0.12, 0.22) * depth * 0.4;
    col *= vignette * 0.75 + 0.25;
    col = max(col, vec3(0.08, 0.06, 0.14));
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'glitch-scanlines',
    category: 'glitch',
    tags: ['vj-glitch', 'digital'],
    build: (s) => {
      const h = hash(s);
      const density = (40 + h.i(0, 80)).toFixed(0);
      const jitter = (0.02 + h.f(1) * 0.08).toFixed(3);
      return `${isfHeader({ description: `Glitch scanlines ${s}`, category: 'glitch', tags: ['vj-glitch'], inputs: `${COMMON_INPUTS},
        { "NAME": "lineDensity", "TYPE": "float", "DEFAULT": ${density}.0, "MIN": 20.0, "MAX": 120.0 },
        { "NAME": "jitterAmp", "TYPE": "float", "DEFAULT": ${jitter}, "MIN": 0.0, "MAX": 0.15 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float line = step(0.5, fract(uv.y * lineDensity + sin(uv.x * 30.0 + time * 8.0) * jitterAmp * 10.0));
    float block = step(0.7, fract(uv.x * 12.0 + time * 2.5));
    vec3 col = mix(vec3(0.12, 0.85, 0.75), vec3(0.85, 0.15, 0.55), line);
    col = mix(col, vec3(0.95, 0.9, 0.2), block * 0.5);
    col += vec3(0.08, 0.05, 0.15) * (1.0 - line);
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'rgb-split',
    category: 'glitch',
    tags: ['vj-glitch', 'vj-colour'],
    build: (s) => {
      const h = hash(s);
      const split = (0.01 + h.f(0) * 0.04).toFixed(3);
      return `${isfHeader({ description: `RGB split ${s}`, category: 'glitch', tags: ['vj-glitch', 'vj-colour'], inputs: `${COMMON_INPUTS},
        { "NAME": "splitAmt", "TYPE": "float", "DEFAULT": ${split}, "MIN": 0.0, "MAX": 0.08 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float wave = sin(uv.y * 18.0 + time * 3.0) * splitAmt;
    float r = sin((uv.x + wave) * 24.0 + time) * 0.5 + 0.5;
    float g = sin(uv.x * 24.0 + time * 1.1) * 0.5 + 0.5;
    float b = sin((uv.x - wave) * 24.0 - time * 0.8) * 0.5 + 0.5;
    vec3 col = vec3(r, g, b);
    col = mix(vec3(0.1, 0.08, 0.14), col, 0.9);
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'wave-interference',
    category: 'organic',
    tags: ['vj-organic', 'vj-ambient'],
    build: (s) => {
      const h = hash(s);
      const freq = (6 + h.i(0, 10)).toFixed(0);
      return `${isfHeader({ description: `Wave interference ${s}`, category: 'organic', tags: ['vj-organic'], inputs: `${COMMON_INPUTS},
        { "NAME": "freq", "TYPE": "float", "DEFAULT": ${freq}.0, "MIN": 3.0, "MAX": 18.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec2 p = (uv - 0.5) * vec2(resolution.x / resolution.y, 1.0);
    float w1 = sin(length(p) * freq - time * 2.0);
    float w2 = sin(p.x * freq + time * 1.3) * cos(p.y * freq - time);
    float v = w1 * w2 * 0.5 + 0.5;
    v = v * 0.85 + 0.15;
    vec3 col = mix(vec3(0.1, 0.16, 0.24), vec3(0.35, 0.82, 0.9), v);
    col += vec3(0.9, 0.45, 0.2) * pow(v, 4.0) * 0.4;
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'starfield-parallax',
    category: 'cosmic',
    tags: ['vj-cosmic', 'space'],
    build: (s) => {
      const h = hash(s);
      const layers = (2 + h.i(0, 4)).toFixed(0);
      return `${isfHeader({ description: `Starfield parallax ${s}`, category: 'cosmic', tags: ['vj-cosmic'], inputs: `${COMMON_INPUTS},
        { "NAME": "layerCount", "TYPE": "float", "DEFAULT": ${layers}.0, "MIN": 2.0, "MAX": 6.0 }` })}
${COMMON_DEFINES}
float hash21(vec2 p) { return fract(sin(dot(p, vec2(41.2, 89.4))) * 1031.7); }
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec3 col = vec3(0.03, 0.04, 0.12);
    for (float i = 0.0; i < 6.0; i++) {
        if (i >= layerCount) break;
        float sc = 8.0 + i * 6.0;
        vec2 p = uv * sc + vec2(time * (0.05 + i * 0.02), 0.0);
        vec2 id = floor(p);
        float star = step(0.965, hash21(id + i));
        col += vec3(0.85, 0.92, 1.0) * star * (0.8 + 0.2 * sin(time + i));
    }
    col += vec3(0.12, 0.14, 0.28) * (0.4 + 0.2 * sin(uv.x * 6.0 + time));
    col = max(col, vec3(0.08, 0.09, 0.16));
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'hex-honeycomb',
    category: 'geometric',
    tags: ['vj-geometric', 'grid'],
    build: (s) => {
      const h = hash(s);
      const scale = (4 + h.i(0, 8)).toFixed(0);
      return `${isfHeader({ description: `Hex honeycomb ${s}`, category: 'geometric', tags: ['vj-geometric'], inputs: `${COMMON_INPUTS},
        { "NAME": "hexScale", "TYPE": "float", "DEFAULT": ${scale}.0, "MIN": 2.0, "MAX": 14.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy * hexScale;
    vec2 r = vec2(1.0, 1.732);
    vec2 h = r * 0.5;
    vec2 a = mod(uv, r) - h;
    vec2 b = mod(uv + h, r) - h;
    float d = min(dot(a,a), dot(b,b));
    float edge = smoothstep(0.2, 0.04, sqrt(d));
    float pulse = 0.5 + 0.5 * sin(time * 2.0 + uv.x + uv.y);
    vec3 col = mix(vec3(0.14, 0.16, 0.24), vec3(0.35, 0.95, 0.8), edge * pulse + 0.2);
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'beat-radar',
    category: 'techno',
    tags: ['vj-techno', 'pulse'],
    build: (s) => {
      const h = hash(s);
      const spokes = (4 + h.i(0, 8)).toFixed(0);
      return `${isfHeader({ description: `Beat radar ${s}`, category: 'techno', tags: ['vj-techno'], inputs: `${COMMON_INPUTS},
        { "NAME": "spokeCount", "TYPE": "float", "DEFAULT": ${spokes}.0, "MIN": 3.0, "MAX": 16.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy - 0.5;
    uv.x *= resolution.x / resolution.y;
    float a = atan(uv.y, uv.x);
    float r = length(uv);
    float sweep = fract(a / 6.28318 + time * 0.3);
    float spoke = 0.5 + 0.5 * cos(a * spokeCount + time * 4.0);
    float ring = smoothstep(0.02, 0.0, abs(fract(r * 8.0 - time) - 0.5));
    vec3 col = vec3(0.05, 0.02, 0.1);
    col += vec3(0.1, 0.95, 0.5) * ring * spoke;
    col += vec3(0.9, 0.2, 0.6) * sweep * smoothstep(0.5, 0.0, r);
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'liquid-mercury',
    category: 'organic',
    tags: ['vj-organic', 'vj-dark'],
    build: (s) => {
      const h = hash(s);
      const flow = (0.5 + h.f(0) * 2).toFixed(2);
      return `${isfHeader({ description: `Liquid mercury ${s}`, category: 'organic', tags: ['vj-organic'], inputs: `${COMMON_INPUTS},
        { "NAME": "flowSpeed", "TYPE": "float", "DEFAULT": ${flow}, "MIN": 0.2, "MAX": 3.0 }` })}
${COMMON_DEFINES}
float hash21(vec2 p) { return fract(sin(dot(p, vec2(12.9, 78.2))) * 43758.5); }
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec2 p = uv * 4.0;
    float n = hash21(floor(p + time * flowSpeed * 0.1));
    float blob = smoothstep(0.35, 0.0, length(fract(p + n) - 0.5));
    vec3 col = mix(vec3(0.08, 0.09, 0.12), vec3(0.75, 0.78, 0.82), blob);
    col += vec3(0.15, 0.2, 0.25) * (1.0 - blob);
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'aurora-curtain',
    category: 'ambient',
    tags: ['vj-ambient', 'vj-cosmic'],
    build: (s) => {
      const h = hash(s);
      const bands = (3 + h.i(0, 5)).toFixed(0);
      return `${isfHeader({ description: `Aurora curtain ${s}`, category: 'ambient', tags: ['vj-ambient', 'vj-cosmic'], inputs: `${COMMON_INPUTS},
        { "NAME": "bandCount", "TYPE": "float", "DEFAULT": ${bands}.0, "MIN": 2.0, "MAX": 10.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float curtain = 0.0;
    for (float i = 0.0; i < 10.0; i++) {
        if (i >= bandCount) break;
        curtain += sin(uv.x * (3.0 + i) + time * (0.3 + i * 0.05) + i) * 0.5 + 0.5;
    }
    curtain /= bandCount;
    vec3 col = mix(vec3(0.02, 0.05, 0.1), vec3(0.2, 0.9, 0.55), curtain * uv.y);
    col += vec3(0.5, 0.2, 0.9) * curtain * (1.0 - uv.y) * 0.4;
    col = max(col, vec3(0.04, 0.06, 0.1));
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'circuit-trace',
    category: 'techno',
    tags: ['vj-techno', 'vj-geometric'],
    build: (s) => {
      const h = hash(s);
      const grid = (6 + h.i(0, 10)).toFixed(0);
      return `${isfHeader({ description: `Circuit trace ${s}`, category: 'techno', tags: ['vj-techno'], inputs: `${COMMON_INPUTS},
        { "NAME": "gridSize", "TYPE": "float", "DEFAULT": ${grid}.0, "MIN": 4.0, "MAX": 18.0 }` })}
${COMMON_DEFINES}
float hash21(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy * gridSize;
    vec2 id = floor(uv);
    vec2 f = fract(uv);
    float path = step(0.55, hash21(id + floor(time * 0.5)));
    float trace = min(abs(f.x - 0.5), abs(f.y - 0.5));
    trace = smoothstep(0.12, 0.0, trace) * (0.35 + 0.65 * path);
    vec3 col = vec3(0.08, 0.1, 0.16);
    col += vec3(0.15, 0.95, 0.85) * trace;
    col += vec3(0.95, 0.35, 0.1) * trace * sin(time * 6.0 + id.x + id.y) * 0.5;
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'fractal-branch',
    category: 'organic',
    tags: ['vj-organic', 'vj-dark'],
    build: (s) => {
      const h = hash(s);
      const twist = (1.5 + h.f(0) * 3).toFixed(2);
      return `${isfHeader({ description: `Fractal branch ${s}`, category: 'organic', tags: ['vj-organic'], inputs: `${COMMON_INPUTS},
        { "NAME": "twistAmt", "TYPE": "float", "DEFAULT": ${twist}, "MIN": 0.5, "MAX": 5.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy - 0.5;
    uv.x *= resolution.x / resolution.y;
    float a = atan(uv.y, uv.x) + length(uv) * twistAmt;
    float branch = abs(sin(a * 5.0 + time * 0.5));
    branch = smoothstep(0.92, 0.98, branch);
    vec3 col = vec3(0.05, 0.08, 0.06);
    col += vec3(0.35, 0.85, 0.45) * branch;
    col += vec3(0.15, 0.25, 0.12) * (1.0 - branch) * 0.6;
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'chroma-bars',
    category: 'colour',
    tags: ['vj-colour', 'bars'],
    build: (s) => {
      const h = hash(s);
      const bars = (6 + h.i(0, 10)).toFixed(0);
      return `${isfHeader({ description: `Chroma bars ${s}`, category: 'colour', tags: ['vj-colour'], inputs: `${COMMON_INPUTS},
        { "NAME": "barCount", "TYPE": "float", "DEFAULT": ${bars}.0, "MIN": 4.0, "MAX": 20.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float bar = floor(uv.x * barCount);
    float hueVal = fract(bar * 0.17 + time * 0.2);
    vec3 col = vec3(abs(sin(hueVal * 6.28)), abs(sin(hueVal * 6.28 + 2.1)), abs(sin(hueVal * 6.28 + 4.2)));
    col = mix(vec3(0.08), col, 0.85 + 0.15 * sin(time * 3.0 + bar));
    gl_FragColor = vec4(col, 1.0);
}`;
    },
  },
  {
    id: 'void-portal',
    category: 'cosmic',
    tags: ['vj-cosmic', 'macroverse-origin'],
    build: (s) => {
      const h = hash(s);
      const rings = (3 + h.i(0, 6)).toFixed(0);
      return `${isfHeader({ description: `Void portal ${s}`, category: 'cosmic', tags: ['vj-cosmic', 'macroverse-origin'], inputs: `${COMMON_INPUTS},
        { "NAME": "ringCount", "TYPE": "float", "DEFAULT": ${rings}.0, "MIN": 2.0, "MAX": 10.0 }` })}
${COMMON_DEFINES}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy - 0.5;
    uv.x *= resolution.x / resolution.y;
    float r = length(uv);
    float ring = abs(fract(r * ringCount - time * 0.4) - 0.5);
    ring = smoothstep(0.15, 0.0, ring);
    vec3 col = vec3(0.06, 0.02, 0.14);
    col += vec3(0.65, 0.35, 1.0) * ring * 1.2;
    col += vec3(0.98, 0.6, 0.2) * ring * (0.5 + 0.5 * sin(atan(uv.y, uv.x) * 3.0 + time));
    col += vec3(0.15, 0.08, 0.25) * (1.0 - smoothstep(0.2, 0.7, r));
    col *= smoothstep(0.85, 0.08, r) * 0.7 + 0.3;
    col = max(col, vec3(0.05, 0.03, 0.1));
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
  const onlyIds = process.argv.find((a) => a.startsWith('--only='))?.split('=')[1]?.split(',').filter(Boolean) || null;
  for (const tpl of TEMPLATES) {
    if (onlyIds && !onlyIds.includes(tpl.id)) continue;
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