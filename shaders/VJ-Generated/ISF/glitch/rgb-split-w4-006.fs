/*{
    "DESCRIPTION": "RGB split rgb-split-6-1781761128130",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["glitch"],
    "TAGS": ["generated","vj","wired-atelier-2026","glitch","vj-glitch","vj-colour"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "splitAmt", "TYPE": "float", "DEFAULT": 0.039, "MIN": 0.0, "MAX": 0.08 }
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float wave = sin(uv.y * 18.0 + time * 3.0) * splitAmt;
    float r = sin((uv.x + wave) * 24.0 + time) * 0.5 + 0.5;
    float g = sin(uv.x * 24.0 + time * 1.1) * 0.5 + 0.5;
    float b = sin((uv.x - wave) * 24.0 - time * 0.8) * 0.5 + 0.5;
    vec3 col = vec3(r, g, b);
    col = mix(vec3(0.1, 0.08, 0.14), col, 0.9);
    gl_FragColor = vec4(col, 1.0);
}