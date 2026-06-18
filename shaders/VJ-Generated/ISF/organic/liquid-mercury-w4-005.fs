/*{
    "DESCRIPTION": "Liquid mercury liquid-mercury-5-1781761128199",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["organic"],
    "TAGS": ["generated","vj","wired-atelier-2026","organic","vj-organic"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "flowSpeed", "TYPE": "float", "DEFAULT": 2.41, "MIN": 0.2, "MAX": 3.0 }
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

float hash21(vec2 p) { return fract(sin(dot(p, vec2(12.9, 78.2))) * 43758.5); }
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec2 p = uv * 4.0;
    float n = hash21(floor(p + time * flowSpeed * 0.1));
    float blob = smoothstep(0.35, 0.0, length(fract(p + n) - 0.5));
    vec3 col = mix(vec3(0.08, 0.09, 0.12), vec3(0.75, 0.78, 0.82), blob);
    col += vec3(0.15, 0.2, 0.25) * (1.0 - blob);
    gl_FragColor = vec4(col, 1.0);
}