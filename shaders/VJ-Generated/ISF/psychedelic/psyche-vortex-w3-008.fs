/*{
    "DESCRIPTION": "Color vortex psyche-vortex-8-1781759867748",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["psychedelic"],
    "TAGS": ["generated","vj","wired-atelier-2026","psychedelic","vj-colour"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "twist", "TYPE": "float", "DEFAULT": 4.94, "MIN": 0.5, "MAX": 6.0 }
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
    float r = length(uv);
    float a = atan(uv.y, uv.x) + r * twist + time * 0.5;
    float v = sin(a * 5.0) * cos(r * 20.0 - time);
    vec3 col = 0.5 + 0.5 * cos(vec3(0, 1.5, 3.0) + v * 4.0 + time);
    col *= smoothstep(0.75, 0.0, r);
    gl_FragColor = vec4(col, 1.0);
}