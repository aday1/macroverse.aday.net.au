/*{
    "DESCRIPTION": "Plasma fire plasma-fire-2-1781758904635",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["plasma"],
    "TAGS": ["generated","vj","wired-atelier-2026","plasma","vj-colour"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0.010 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 58.200, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 0.970, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "intensity", "TYPE": "float", "DEFAULT": 1.659, "MIN": 0.3, "MAX": 2.0 }
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float x = uv.x * 8.0 * intensity;
    float y = uv.y * 8.0;
    float t = time * 0.6;
    float v = sin(x + t) + sin(y + t * 0.5) + sin(x + y + t);
    vec3 col = vec3(sin(v), sin(v + 2.1), sin(v + 4.2)) * 0.5 + 0.5;
    col = pow(col, vec3(1.4));
    gl_FragColor = vec4(col, 1.0);
}