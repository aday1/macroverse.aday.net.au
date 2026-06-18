/*{
    "DESCRIPTION": "Copper field copper-field-5-1781759867657",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["macroverse"],
    "TAGS": ["generated","vj","wired-atelier-2026","macroverse","macroverse-origin","cosmic"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "rippleAmp", "TYPE": "float", "DEFAULT": 0.314, "MIN": 0.0, "MAX": 1.0 },
        { "NAME": "driftSpeed", "TYPE": "float", "DEFAULT": 0.144, "MIN": 0.0, "MAX": 0.5 },
        { "NAME": "fieldScale", "TYPE": "float", "DEFAULT": 2.24, "MIN": 0.5, "MAX": 5.0 }
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

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
}