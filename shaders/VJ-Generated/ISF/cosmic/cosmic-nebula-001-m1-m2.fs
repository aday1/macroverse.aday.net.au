/*{
    "DESCRIPTION": "Nebula swirl cosmic-nebula-1-1781758904654",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["cosmic"],
    "TAGS": ["generated","vj","wired-atelier-2026","cosmic","vj-cosmic"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0.010 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 52.962, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 0.883, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "swirl", "TYPE": "float", "DEFAULT": 1.182, "MIN": 0.2, "MAX": 3.0 }
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

float hash21(vec2 p) { return fract(sin(dot(p, vec2(41.3, 89.7))) * 1031.73); }
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec2 p = uv - 0.5;
    float a = atan(p.y, p.x) + length(p) * swirl + time * 0.1;
    float n = hash21(vec2(a * 2.0, length(p) * 5.0 - time * 0.05));
    vec3 col = mix(vec3(0.02, 0.01, 0.08), vec3(0.5, 0.15, 0.9), n);
    col += vec3(0.9, 0.4, 0.2) * pow(n, 4.0);
    gl_FragColor = vec4(col, 1.0);
}