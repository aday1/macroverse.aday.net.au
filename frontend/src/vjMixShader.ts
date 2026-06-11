export type MixMode =
  | 'crossfade'
  | 'alpha'
  | 'add'
  | 'multiply'
  | 'luma'
  | 'screen'
  | 'difference'
  | 'overlay'
  | 'hardcut'
  | 'wipe-h'
  | 'wipe-v'
  | 'dissolve'
  | 'zoom'
  | 'darken'
  | 'lighten'
  | 'exclusion'
  | 'random';

export type MixModeDef = { value: MixMode; label: string; modeInt: number };

export const MIX_MODES: MixModeDef[] = [
  { value: 'crossfade', label: 'Crossfade', modeInt: 0 },
  { value: 'alpha', label: 'Alpha Layer', modeInt: 1 },
  { value: 'add', label: 'Add', modeInt: 2 },
  { value: 'multiply', label: 'Multiply', modeInt: 3 },
  { value: 'luma', label: 'Luma Key', modeInt: 4 },
  { value: 'screen', label: 'Screen', modeInt: 5 },
  { value: 'difference', label: 'Difference', modeInt: 6 },
  { value: 'overlay', label: 'Overlay', modeInt: 7 },
  { value: 'hardcut', label: 'Hard Cut', modeInt: 8 },
  { value: 'wipe-h', label: 'Wipe H', modeInt: 9 },
  { value: 'wipe-v', label: 'Wipe V', modeInt: 10 },
  { value: 'dissolve', label: 'Dissolve', modeInt: 11 },
  { value: 'zoom', label: 'Zoom', modeInt: 12 },
  { value: 'darken', label: 'Darken', modeInt: 13 },
  { value: 'lighten', label: 'Lighten', modeInt: 14 },
  { value: 'exclusion', label: 'Exclusion', modeInt: 15 },
  { value: 'random', label: 'Random', modeInt: -1 },
];

/** Mode ints picked when mix mode is Random (excludes random itself). */
export const RANDOMIZABLE_MIX_INTS = MIX_MODES.filter((m) => m.modeInt >= 0).map((m) => m.modeInt);

export function mixModeLabelForInt(modeInt: number): string {
  return MIX_MODES.find((m) => m.modeInt === modeInt)?.label ?? 'Mix';
}

export function pickRandomMixModeInt(): number {
  const pool = RANDOMIZABLE_MIX_INTS;
  return pool[Math.floor(Math.random() * pool.length)] ?? 0;
}

export function resolveMixModeInt(mixMode: MixMode, activeRandomMixInt: number): number {
  if (mixMode === 'random') return activeRandomMixInt;
  const found = MIX_MODES.find((m) => m.value === mixMode);
  return found && found.modeInt >= 0 ? found.modeInt : 0;
}

export function isMixMode(value: string): value is MixMode {
  return MIX_MODES.some((m) => m.value === value);
}

export const VJ_MIX_FRAG_SRC = `precision highp float;
uniform sampler2D texA;
uniform sampler2D texB;
uniform float crossfader;
uniform int mixMode;
uniform float outputFlipV;
uniform float outputFlipH;
uniform int outputRotation;
varying vec2 v_uv;

float hash21(vec2 p) {
  return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

vec3 overlayRgb(vec3 base, vec3 blend) {
  return mix(
    2.0 * base * blend,
    vec3(1.0) - 2.0 * (vec3(1.0) - base) * (vec3(1.0) - blend),
    step(vec3(0.5), base)
  );
}

void main() {
  vec2 uv = vec2(v_uv.x, 1.0 - v_uv.y);
  if (outputFlipV > 0.5) uv.y = 1.0 - uv.y;
  if (outputFlipH > 0.5) uv.x = 1.0 - uv.x;
  if (outputRotation == 1) uv = vec2(1.0 - uv.y, uv.x);
  else if (outputRotation == 2) uv = vec2(1.0 - uv.x, 1.0 - uv.y);
  else if (outputRotation == 3) uv = vec2(uv.y, 1.0 - uv.x);

  vec4 a = texture2D(texA, uv);
  vec4 b = texture2D(texB, uv);
  float t = clamp(crossfader, 0.0, 1.0);

  if (mixMode == 1) {
    gl_FragColor = a * (1.0 - b.a * t) + b * b.a * t;
  } else if (mixMode == 2) {
    gl_FragColor = clamp(a + b * t, 0.0, 1.0);
  } else if (mixMode == 3) {
    gl_FragColor = mix(a, a * b, t);
  } else if (mixMode == 4) {
    float luma = dot(b.rgb, vec3(0.299, 0.587, 0.114));
    gl_FragColor = mix(a, b, luma * t);
  } else if (mixMode == 5) {
    vec4 screenCol = vec4(1.0) - (vec4(1.0) - a) * (vec4(1.0) - b);
    gl_FragColor = mix(a, screenCol, t);
  } else if (mixMode == 6) {
    gl_FragColor = mix(a, vec4(abs(a.rgb - b.rgb), mix(a.a, b.a, t)), t);
  } else if (mixMode == 7) {
    vec4 overlayCol = vec4(overlayRgb(a.rgb, b.rgb), mix(a.a, b.a, t));
    gl_FragColor = mix(a, overlayCol, t);
  } else if (mixMode == 8) {
    gl_FragColor = t < 0.5 ? a : b;
  } else if (mixMode == 9) {
    float edge = smoothstep(t - 0.02, t + 0.02, uv.x);
    gl_FragColor = mix(a, b, edge);
  } else if (mixMode == 10) {
    float edge = smoothstep(t - 0.02, t + 0.02, uv.y);
    gl_FragColor = mix(a, b, edge);
  } else if (mixMode == 11) {
    float n = hash21(floor(uv * vec2(640.0, 360.0)));
    gl_FragColor = mix(a, b, step(t, n));
  } else if (mixMode == 12) {
    vec2 centered = uv - 0.5;
    float scale = mix(1.0, 0.35, t);
    vec2 buv = centered / max(scale, 0.001) + 0.5;
    vec4 bZoom = texture2D(texB, clamp(buv, 0.0, 1.0));
    gl_FragColor = mix(a, bZoom, t);
  } else if (mixMode == 13) {
    gl_FragColor = mix(a, min(a, b), t);
  } else if (mixMode == 14) {
    gl_FragColor = mix(a, max(a, b), t);
  } else if (mixMode == 15) {
    vec4 excl = a + b - 2.0 * a * b;
    gl_FragColor = mix(a, excl, t);
  } else {
    gl_FragColor = mix(a, b, t);
  }
}`;
