/*{
    "DESCRIPTION": "Living Our Best Life",
    "CREDIT": "Aday / MacroVerse Origin",
    "ISFVSN": "2.0",
    "CATEGORIES": ["macroverse"],
    "TAGS": ["macroverse", "macroverse-origin", "chapter-06", "cosmic", "stars"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "warmth", "TYPE": "float", "DEFAULT": 0.75, "MIN": 0.0, "MAX": 1.0, "LABEL": "Warmth" },
        { "NAME": "starlight", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.2, "MAX": 1.8, "LABEL": "Starlight" },
        { "NAME": "serenity", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "Serenity" },
        { "NAME": "drift", "TYPE": "float", "DEFAULT": 0.25, "MIN": 0.0, "MAX": 1.0, "LABEL": "Drift" }
    ]
}*/

#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
#define resolution RENDERSIZE

#ifdef GL_ES
precision highp float;
#endif

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

vec3 starLayer(vec2 uv, float depth, float t) {
    vec3 col = vec3(0.0);
    float scale = 120.0 * depth;
    vec2 grid = floor(uv * scale);
    vec2 frac = fract(uv * scale) - 0.5;

    for (int dx = -1; dx <= 1; dx++) {
        for (int dy = -1; dy <= 1; dy++) {
            vec2 cell = grid + vec2(float(dx), float(dy));
            float h = hash(cell + depth * 50.0);
            if (h > 0.75) continue;

            vec2 starPos = vec2(hash(cell * 1.3), hash(cell * 2.1 + 3.0)) - 0.5;
            vec2 d = frac - vec2(float(dx), float(dy)) - starPos * 0.5;
            float dist = length(d);
            float bright = exp(-dist * dist * (60.0 + depth * 30.0));
            float twinkle = 0.7 + 0.3 * sin(t * 2.0 + h * 25.0);
            vec3 starCol = mix(vec3(1.0, 0.95, 0.85), vec3(1.0, 0.75, 0.45), warmth);
            col += starCol * bright * twinkle * (0.3 + depth * 0.5);
        }
    }
    return col;
}

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float aspect = resolution.x / resolution.y;
    vec2 p = (uv - 0.5) * vec2(aspect, 1.0);
    float t = time * (0.15 + serenity * 0.2);

    vec3 nebula = vec3(0.04, 0.02, 0.06);
    float n = noise(p * 2.0 + vec2(t * drift * 0.1, 0.0));
    nebula += mix(vec3(0.15, 0.08, 0.02), vec3(0.25, 0.12, 0.04), warmth) * n * 0.35;
    nebula += vec3(0.08, 0.04, 0.12) * noise(p * 4.0 - t * 0.05) * 0.2;

    vec3 stars = vec3(0.0);
    for (int i = 0; i < 4; i++) {
        float depth = (float(i) + 1.0) / 4.0;
        stars += starLayer(p + vec2(sin(t + float(i)) * 0.01 * drift, 0.0), depth, t + float(i));
    }

    vec3 col = nebula + stars * starlight;
    float glow = exp(-length(p) * (1.2 - serenity * 0.4));
    col += vec3(0.35, 0.22, 0.08) * glow * warmth * 0.25;
    col *= 0.9 + 0.1 * serenity;
    col = col / (1.0 + col * 0.35);

    gl_FragColor = vec4(col, 1.0);
}