/*{
    "DESCRIPTION": "Lightspeed tunnel tunnel-zoom-11-1781758904647",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["tunnel"],
    "TAGS": ["generated","vj","wired-atelier-2026","tunnel","vj-geometric"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "zoomSpeed", "TYPE": "float", "DEFAULT": 1.56, "MIN": 0.1, "MAX": 2.0 }
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
    float a = atan(uv.y, uv.x);
    float r = length(uv);
    float tunnel = fract(1.0 / (r + 0.05) - time * zoomSpeed);
    float spokes = 0.5 + 0.5 * sin(a * 8.0 + time);
    vec3 col = mix(vec3(0.02, 0.0, 0.08), vec3(0.2, 0.9, 0.7), tunnel * spokes);
    col *= smoothstep(0.7, 0.05, r);
    gl_FragColor = vec4(col, 1.0);
}