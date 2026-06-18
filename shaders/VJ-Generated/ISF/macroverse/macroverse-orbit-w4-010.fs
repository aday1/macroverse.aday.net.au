/*{
    "DESCRIPTION": "Orbit glow macroverse-orbit-10-1781761128076",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["macroverse"],
    "TAGS": ["generated","vj","wired-atelier-2026","macroverse","macroverse-origin"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "orbCount", "TYPE": "float", "DEFAULT": 6.0, "MIN": 1.0, "MAX": 8.0 }
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
    vec3 col = vec3(0.01, 0.02, 0.06);
    for (float i = 0.0; i < 8.0; i++) {
        if (i >= orbCount) break;
        float a = time * (0.2 + i * 0.07) + i * 1.2;
        vec2 c = vec2(cos(a), sin(a)) * (0.15 + i * 0.08);
        float d = length(uv - c);
        col += vec3(0.9, 0.5, 0.15) * 0.08 / (d + 0.02);
        col += vec3(0.2, 0.5, 0.95) * 0.04 / (d + 0.05);
    }
    gl_FragColor = vec4(col, 1.0);
}