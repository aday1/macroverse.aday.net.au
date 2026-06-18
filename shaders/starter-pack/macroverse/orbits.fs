/*{
    "DESCRIPTION": "Orbits",
    "CREDIT": "Macroverse — Microvirtuosity",
    "ISFVSN": "2.0",
    "CATEGORIES": ["macroverse"],
    "TAGS": ["macroverse-set", "galaxy", "orbit", "planets"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "armCount", "TYPE": "float", "DEFAULT": 3.0, "MIN": 2.0, "MAX": 5.0, "LABEL": "Spiral arms" },
        { "NAME": "spin", "TYPE": "float", "DEFAULT": 0.35, "MIN": 0.05, "MAX": 1.5, "LABEL": "Galaxy spin" },
        { "NAME": "orbitGlow", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.3, "MAX": 2.0, "LABEL": "Orbit glow" }
    ]
}*/

#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
#define resolution RENDERSIZE

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

void main(void) {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec2 aspect = vec2(resolution.x / resolution.y, 1.0);
    vec2 p = (uv - 0.5) * aspect * 2.0;
    float t = time * spin;

    float r = length(p);
    float a = atan(p.y, p.x);

    float spiral = 0.0;
    for (float arm = 0.0; arm < 5.0; arm += 1.0) {
        if (arm >= armCount) break;
        float offset = arm * 6.28318 / armCount;
        float twist = a + offset - r * 3.5 + t * 0.8;
        spiral += pow(0.55 + 0.45 * sin(twist * 5.0), 3.0) * exp(-r * 1.2);
    }

    vec3 col = vec3(0.01, 0.02, 0.06);
    col += vec3(0.15, 0.25, 0.55) * spiral * 0.25 * orbitGlow;

    float rings = 0.0;
    for (float i = 1.0; i <= 6.0; i += 1.0) {
        float rad = i * 0.22;
        float band = exp(-pow((r - rad) * 12.0, 2.0));
        float wobble = sin(a * (3.0 + i) + t * (0.5 + i * 0.1)) * 0.5 + 0.5;
        rings += band * wobble * 0.12;
    }
    col += vec3(0.4, 0.55, 0.95) * rings * orbitGlow;

    float core = exp(-r * r * 8.0);
    col += vec3(1.0, 0.9, 0.7) * core * 0.9;

    float dots = 0.0;
    vec2 g = floor(p * 35.0);
    vec2 f = fract(p * 35.0) - 0.5;
    float h = hash(g);
    if (h > 0.82 && r > 0.15 && r < 1.3) {
        float d = length(f);
        dots = exp(-d * d * 80.0) * (0.6 + 0.4 * sin(t * 2.0 + h * 30.0));
    }
    col += vec3(0.7, 0.85, 1.0) * dots;

    col = col / (col + vec3(0.35));
    gl_FragColor = vec4(pow(col, vec3(0.92)), 1.0);
}