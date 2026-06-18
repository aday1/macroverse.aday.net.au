/*{
    "DESCRIPTION": "Glitch scanlines glitch-scanlines-5-1781761128108",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["glitch"],
    "TAGS": ["generated","vj","wired-atelier-2026","glitch","vj-glitch"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "lineDensity", "TYPE": "float", "DEFAULT": 80.0, "MIN": 20.0, "MAX": 120.0 },
        { "NAME": "jitterAmp", "TYPE": "float", "DEFAULT": 0.096, "MIN": 0.0, "MAX": 0.15 }
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float line = step(0.5, fract(uv.y * lineDensity + sin(uv.x * 30.0 + time * 8.0) * jitterAmp * 10.0));
    float block = step(0.7, fract(uv.x * 12.0 + time * 2.5));
    vec3 col = mix(vec3(0.12, 0.85, 0.75), vec3(0.85, 0.15, 0.55), line);
    col = mix(col, vec3(0.95, 0.9, 0.2), block * 0.5);
    col += vec3(0.08, 0.05, 0.15) * (1.0 - line);
    gl_FragColor = vec4(col, 1.0);
}