/*{
    "DESCRIPTION": "Fractal branch fractal-branch-8-1781761128248",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["organic"],
    "TAGS": ["generated","vj","wired-atelier-2026","organic","vj-organic"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "twistAmt", "TYPE": "float", "DEFAULT": 4.46, "MIN": 0.5, "MAX": 5.0 }
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
    float a = atan(uv.y, uv.x) + length(uv) * twistAmt;
    float branch = abs(sin(a * 5.0 + time * 0.5));
    branch = smoothstep(0.92, 0.98, branch);
    vec3 col = vec3(0.05, 0.08, 0.06);
    col += vec3(0.35, 0.85, 0.45) * branch;
    col += vec3(0.15, 0.25, 0.12) * (1.0 - branch) * 0.6;
    gl_FragColor = vec4(col, 1.0);
}