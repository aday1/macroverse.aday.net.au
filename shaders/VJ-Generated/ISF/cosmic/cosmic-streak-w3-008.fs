/*{
    "DESCRIPTION": "Cosmic streak cosmic-streak-8-1781759867678",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["cosmic"],
    "TAGS": ["generated","vj","wired-atelier-2026","cosmic","vj-cosmic"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "warpSpeed", "TYPE": "float", "DEFAULT": 0.46, "MIN": 0.1, "MAX": 2.0 }
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

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
}