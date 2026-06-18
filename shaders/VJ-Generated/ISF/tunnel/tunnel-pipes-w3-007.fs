/*{
    "DESCRIPTION": "Retro pipes tunnel-pipes-7-1781759867781",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["tunnel"],
    "TAGS": ["generated","vj","wired-atelier-2026","tunnel","vj-geometric"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "pipeCount", "TYPE": "float", "DEFAULT": 9.0, "MIN": 3.0, "MAX": 16.0 }
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
}