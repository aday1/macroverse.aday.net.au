/*{
    "DESCRIPTION": "Life As We Know It",
    "CREDIT": "Macroverse — Microvirtuosity",
    "ISFVSN": "2.0",
    "CATEGORIES": ["macroverse"],
    "TAGS": ["macroverse-set", "organic", "life", "cells"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "cellScale", "TYPE": "float", "DEFAULT": 8.0, "MIN": 4.0, "MAX": 16.0, "LABEL": "Cell scale" },
        { "NAME": "coherence", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.3, "MAX": 2.0, "LABEL": "Living coherence" },
        { "NAME": "pulse", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 2.0, "LABEL": "Metabolic pulse" }
    ]
}*/

#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
#define resolution RENDERSIZE

vec2 hash2(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return fract(sin(p) * 43758.5453);
}

float voronoi(vec2 p, out vec2 cellId) {
    vec2 n = floor(p);
    vec2 f = fract(p);
    float md = 8.0;
    for (int j = -1; j <= 1; j++) {
        for (int i = -1; i <= 1; i++) {
            vec2 g = vec2(float(i), float(j));
            vec2 o = hash2(n + g);
            o = 0.5 + 0.5 * sin(time * 0.4 * pulse + 6.2831 * o);
            vec2 r = g + o - f;
            float d = dot(r, r);
            if (d < md) {
                md = d;
                cellId = n + g;
            }
        }
    }
    return sqrt(md);
}

void main(void) {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec2 aspect = vec2(resolution.x / resolution.y, 1.0);
    vec2 p = (uv - 0.5) * aspect;

    vec2 cellId;
    float v = voronoi(p * cellScale, cellId);
    float edge = smoothstep(0.0, 0.08, v);
    float nucleus = exp(-v * v * 120.0);

    float h = hash2(cellId).x;
    float breathe = 0.5 + 0.5 * sin(time * (1.2 + h * 2.0) * pulse);

    vec3 membrane = mix(vec3(0.05, 0.35, 0.22), vec3(0.1, 0.65, 0.45), breathe);
    vec3 cytoplasm = mix(vec3(0.02, 0.12, 0.18), vec3(0.15, 0.4, 0.55), h);
    vec3 core = vec3(0.9, 0.95, 0.4) * nucleus;

    vec3 col = mix(cytoplasm, membrane, edge);
    col += core * coherence;
    col += vec3(0.05, 0.2, 0.15) * (1.0 - edge) * 0.4;

    float helix = abs(sin(p.x * 12.0 + p.y * 8.0 + time * 0.5));
    col += vec3(0.2, 0.8, 0.5) * helix * 0.06 * coherence * (1.0 - edge);

    gl_FragColor = vec4(pow(col, vec3(0.95)), 1.0);
}