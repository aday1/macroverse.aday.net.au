/*{
    "DESCRIPTION": "Aurora curtain aurora-curtain-2-1781761128209",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["ambient"],
    "TAGS": ["generated","vj","wired-atelier-2026","ambient","vj-ambient","vj-cosmic"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "bandCount", "TYPE": "float", "DEFAULT": 7.0, "MIN": 2.0, "MAX": 10.0 }
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float curtain = 0.0;
    for (float i = 0.0; i < 10.0; i++) {
        if (i >= bandCount) break;
        curtain += sin(uv.x * (3.0 + i) + time * (0.3 + i * 0.05) + i) * 0.5 + 0.5;
    }
    curtain /= bandCount;
    vec3 col = mix(vec3(0.02, 0.05, 0.1), vec3(0.2, 0.9, 0.55), curtain * uv.y);
    col += vec3(0.5, 0.2, 0.9) * curtain * (1.0 - uv.y) * 0.4;
    col = max(col, vec3(0.04, 0.06, 0.1));
    gl_FragColor = vec4(col, 1.0);
}