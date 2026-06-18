/*{
    "DESCRIPTION": "Particles (13.8B years ago)",
    "CREDIT": "Macroverse — Microvirtuosity",
    "ISFVSN": "2.0",
    "CATEGORIES": ["macroverse"],
    "TAGS": ["macroverse-set", "particles", "big-bang", "quark"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "density", "TYPE": "float", "DEFAULT": 120.0, "MIN": 40.0, "MAX": 300.0, "LABEL": "Particle density" },
        { "NAME": "heat", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.2, "MAX": 2.5, "LABEL": "Thermal glow" },
        { "NAME": "chaos", "TYPE": "float", "DEFAULT": 0.8, "MIN": 0.0, "MAX": 2.0, "LABEL": "Soup turbulence" }
    ]
}*/

#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
#define resolution RENDERSIZE

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(41.3, 289.5))) * 43758.5453);
}

float hash3(vec3 p) {
    return fract(sin(dot(p, vec3(12.9898, 78.233, 45.164))) * 43758.5453);
}

void main(void) {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec2 aspect = vec2(resolution.x / resolution.y, 1.0);
    vec2 p = (uv - 0.5) * aspect;
    float t = time;

    vec3 col = vec3(0.03, 0.01, 0.06);

    float fog = 0.0;
    for (int i = 0; i < 4; i++) {
        float fi = float(i);
        vec2 q = p * (1.5 + fi * 0.7) + vec2(t * 0.12 * (fi + 1.0), -t * 0.08);
        fog += abs(sin(q.x * 3.1 + sin(q.y * 2.7 + t))) * 0.15;
    }
    col += vec3(0.9, 0.25, 0.05) * fog * heat * 0.35;

    float grid = density;
    vec2 g = floor(p * grid);
    vec2 f = fract(p * grid) - 0.5;

    for (float dx = -1.0; dx <= 1.0; dx += 1.0) {
        for (float dy = -1.0; dy <= 1.0; dy += 1.0) {
            vec2 cell = g + vec2(dx, dy);
            float h = hash(cell);
            if (h < 0.55) continue;

            vec2 jitter = vec2(hash(cell + 1.7), hash(cell + 9.2)) - 0.5;
            jitter += vec2(sin(t * 4.0 + h * 20.0), cos(t * 3.5 + h * 15.0)) * chaos * 0.15;
            vec2 diff = f - vec2(dx, dy) - jitter * 0.6;
            float d = length(diff);

            float size = 0.04 + 0.08 * hash3(vec3(cell, 0.0));
            float spark = exp(-d * d / (size * size));

            vec3 particleCol = mix(
                vec3(1.0, 0.35, 0.1),
                mix(vec3(0.2, 0.6, 1.0), vec3(0.9, 0.2, 0.9), h),
                0.5 + 0.5 * sin(t * 6.0 + h * 40.0)
            );
            col += particleCol * spark * heat;
        }
    }

    col = pow(col, vec3(0.9));
    gl_FragColor = vec4(col, 1.0);
}