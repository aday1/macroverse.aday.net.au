/*{
    "DESCRIPTION": "Neon rings geo-rings-4-1781761127901",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["geometric"],
    "TAGS": ["generated","vj","wired-atelier-2026","geometric","vj-geometric"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "ringCount", "TYPE": "float", "DEFAULT": 10.0, "MIN": 2.0, "MAX": 14.0 }
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
    float r = length(uv) * ringCount * 2.0;
    float ring = abs(fract(r - time * 0.2) - 0.5);
    ring = smoothstep(0.08, 0.0, ring);
    vec3 col = vec3(0.02, 0.05, 0.08);
    col += vec3(0.1, 0.8, 0.95) * ring;
    col += vec3(0.95, 0.35, 0.1) * ring * sin(r * 6.0 + time);
    gl_FragColor = vec4(col, 1.0);
}