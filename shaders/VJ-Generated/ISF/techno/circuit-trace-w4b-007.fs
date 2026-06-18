/*{
    "DESCRIPTION": "Circuit trace circuit-trace-7-1781761186758",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["techno"],
    "TAGS": ["generated","vj","wired-atelier-2026","techno","vj-techno"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "gridSize", "TYPE": "float", "DEFAULT": 12.0, "MIN": 4.0, "MAX": 18.0 }
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

float hash21(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy * gridSize;
    vec2 id = floor(uv);
    vec2 f = fract(uv);
    float path = step(0.55, hash21(id + floor(time * 0.5)));
    float trace = min(abs(f.x - 0.5), abs(f.y - 0.5));
    trace = smoothstep(0.12, 0.0, trace) * (0.35 + 0.65 * path);
    vec3 col = vec3(0.08, 0.1, 0.16);
    col += vec3(0.15, 0.95, 0.85) * trace;
    col += vec3(0.95, 0.35, 0.1) * trace * sin(time * 6.0 + id.x + id.y) * 0.5;
    gl_FragColor = vec4(col, 1.0);
}