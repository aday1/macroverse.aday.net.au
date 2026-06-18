/*{
    "DESCRIPTION": "Pulse bars techno-bars-2-1781758991456",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["techno"],
    "TAGS": ["generated","vj","wired-atelier-2026","techno","vj-techno"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "barCount", "TYPE": "float", "DEFAULT": 6.0, "MIN": 3.0, "MAX": 16.0 }
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float bar = floor(uv.x * barCount);
    float hbar = 0.35 + 0.35 * sin(bar * 1.7 + time * 3.0);
    float mask = step(uv.y, hbar);
    vec3 cols = 0.5 + 0.5 * cos(vec3(0, 1.2, 2.4) + bar * 0.8);
    vec3 col = mix(vec3(0.02), cols, mask);
    col *= 0.8 + 0.2 * sin(time * 8.0 + bar);
    gl_FragColor = vec4(col, 1.0);
}