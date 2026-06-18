/*{
    "DESCRIPTION": "Retro pipes tunnel-pipes-0-1781758991496",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["tunnel"],
    "TAGS": ["generated","vj","wired-atelier-2026","tunnel","vj-geometric"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "pipeCount", "TYPE": "float", "DEFAULT": 8.0, "MIN": 3.0, "MAX": 16.0 }
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
    vec3 col = vec3(0.12, 0.04, 0.18) + vec3(0.45, 0.95, 0.55) * pipe * depth;
    col += vec3(0.25, 0.15, 0.35) * (1.0 - pipe) * 0.4;
    col *= smoothstep(0.75, 0.02, r) * 0.85 + 0.15;
    gl_FragColor = vec4(col, 1.0);
}