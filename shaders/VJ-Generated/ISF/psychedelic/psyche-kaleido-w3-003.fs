/*{
    "DESCRIPTION": "Kaleido pulse psyche-kaleido-3-1781759867682",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["psychedelic"],
    "TAGS": ["generated","vj","wired-atelier-2026","psychedelic","vj-colour"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "segments", "TYPE": "float", "DEFAULT": 7.0, "MIN": 3.0, "MAX": 16.0 }
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
    float seg = max(segments, 3.0);
    a = mod(a, 6.28318 / seg);
    a = abs(a - 3.14159 / seg);
    vec2 k = vec2(cos(a), sin(a)) * r;
    float v = sin(k.x * 12.0 + time) * cos(k.y * 10.0 - time * 0.7);
    vec3 col = 0.5 + 0.5 * cos(vec3(0.0, 2.1, 4.2) + v * 3.0 + time);
    col *= smoothstep(0.8, 0.1, r);
    gl_FragColor = vec4(col, 1.0);
}