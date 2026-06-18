/*{
    "DESCRIPTION": "Soft plasma ambient-plasma-5-1781761127915",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["ambient"],
    "TAGS": ["generated","vj","wired-atelier-2026","ambient","vj-ambient"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "plasmaScale", "TYPE": "float", "DEFAULT": 3.88, "MIN": 1.0, "MAX": 10.0 }
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy * plasmaScale;
    float v = sin(uv.x + time * 0.3) + sin(uv.y + time * 0.2) + sin(uv.x + uv.y + time * 0.15);
    vec3 col = 0.55 + 0.45 * cos(vec3(0.2, 1.4, 2.6) + v * 1.8);
    col = mix(vec3(0.04, 0.06, 0.1), col, 0.85);
    gl_FragColor = vec4(col, 1.0);
}