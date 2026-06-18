/*{
    "DESCRIPTION": "Cloud drift ambient-cloud-2-1781759867758",
    "CREDIT": "Macroverse Wired Atelier / generated",
    "ISFVSN": "2.0",
    "CATEGORIES": ["ambient"],
    "TAGS": ["generated","vj","wired-atelier-2026","ambient","vj-ambient"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0 },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "timeScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0 },
        { "NAME": "cloudScale", "TYPE": "float", "DEFAULT": 2.55, "MIN": 0.5, "MAX": 6.0 }
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

float hash21(vec2 p) { return fract(sin(dot(p, vec2(12.3, 45.6))) * 43758.5453); }
float noise(vec2 p) {
    vec2 i = floor(p); vec2 f = fract(p);
    f = f*f*(3.0-2.0*f);
    return mix(mix(hash21(i),hash21(i+vec2(1,0)),f.x),mix(hash21(i+vec2(0,1)),hash21(i+vec2(1,1)),f.x),f.y);
}
void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float n = noise(uv * cloudScale + vec2(time * 0.03, 0.0));
    n += 0.5 * noise(uv * cloudScale * 2.0 - time * 0.02);
    vec3 col = mix(vec3(0.05, 0.07, 0.12), vec3(0.5, 0.65, 0.85), n);
    gl_FragColor = vec4(col, 1.0);
}