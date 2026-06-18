/*{
    "DESCRIPTION": "Starfield parallax starfield-parallax-3-1781761186729",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["cosmic"],
    "TAGS": ["generated","vj","wired-atelier-2026","cosmic","vj-cosmic"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "layerCount", "TYPE": "float", "DEFAULT": 2.0, "MIN": 2.0, "MAX": 6.0 }
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

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
}