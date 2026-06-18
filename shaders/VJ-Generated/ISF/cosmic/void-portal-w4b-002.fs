/*{
    "DESCRIPTION": "Void portal void-portal-2-1781761186775",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["cosmic"],
    "TAGS": ["generated","vj","wired-atelier-2026","cosmic","vj-cosmic","macroverse-origin"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "ringCount", "TYPE": "float", "DEFAULT": 6.0, "MIN": 2.0, "MAX": 10.0 }
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

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
}