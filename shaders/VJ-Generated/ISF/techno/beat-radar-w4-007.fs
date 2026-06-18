/*{
    "DESCRIPTION": "Beat radar beat-radar-7-1781761128189",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["techno"],
    "TAGS": ["generated","vj","wired-atelier-2026","techno","vj-techno"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "spokeCount", "TYPE": "float", "DEFAULT": 9.0, "MIN": 3.0, "MAX": 16.0 }
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
    float sweep = fract(a / 6.28318 + time * 0.3);
    float spoke = 0.5 + 0.5 * cos(a * spokeCount + time * 4.0);
    float ring = smoothstep(0.02, 0.0, abs(fract(r * 8.0 - time) - 0.5));
    vec3 col = vec3(0.05, 0.02, 0.1);
    col += vec3(0.1, 0.95, 0.5) * ring * spoke;
    col += vec3(0.9, 0.2, 0.6) * sweep * smoothstep(0.5, 0.0, r);
    gl_FragColor = vec4(col, 1.0);
}