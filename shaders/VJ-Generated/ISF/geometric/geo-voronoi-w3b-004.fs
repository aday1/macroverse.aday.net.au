/*{
    "DESCRIPTION": "Voronoi glow geo-voronoi-4-1781760280243",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["geometric"],
    "TAGS": ["generated","vj","wired-atelier-2026","geometric","vj-geometric"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "cells", "TYPE": "float", "DEFAULT": 8.0, "MIN": 2.0, "MAX": 12.0 }
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

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
}