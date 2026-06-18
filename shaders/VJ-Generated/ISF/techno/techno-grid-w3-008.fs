/*{
    "DESCRIPTION": "Techno grid techno-grid-8-1781759867670",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["techno"],
    "TAGS": ["generated","vj","wired-atelier-2026","techno","vj-techno"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "cellCount", "TYPE": "float", "DEFAULT": 8.0, "MIN": 2.0, "MAX": 24.0 },
        { "NAME": "pulseRate", "TYPE": "float", "DEFAULT": 2.04, "MIN": 0.2, "MAX": 4.0 }
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec2 p = uv * cellCount;
    vec2 g = abs(fract(p - 0.5) - 0.5);
    float line = smoothstep(0.02, 0.0, min(g.x, g.y));
    float beat = 0.5 + 0.5 * sin(time * pulseRate * 6.283);
    vec3 cyan = vec3(0.0, 0.9, 0.85);
    vec3 magenta = vec3(0.9, 0.0, 0.5);
    vec3 col = mix(vec3(0.03), mix(cyan, magenta, beat), line);
    col += vec3(0.08, 0.02, 0.12) * (1.0 - line) * beat;
    gl_FragColor = vec4(col, 1.0);
}