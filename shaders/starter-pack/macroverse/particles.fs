/*{
    "DESCRIPTION": "Particles",
    "CREDIT": "Aday / MacroVerse Origin",
    "ISFVSN": "2.0",
    "CATEGORIES": ["macroverse"],
    "TAGS": ["macroverse", "macroverse-origin", "chapter-02", "cosmic", "particles"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "density", "TYPE": "float", "DEFAULT": 180.0, "MIN": 40.0, "MAX": 400.0, "LABEL": "Particle density" },
        { "NAME": "turbulence", "TYPE": "float", "DEFAULT": 0.65, "MIN": 0.0, "MAX": 1.5, "LABEL": "Turbulence" },
        { "NAME": "heat", "TYPE": "float", "DEFAULT": 0.8, "MIN": 0.0, "MAX": 1.5, "LABEL": "Heat" },
        { "NAME": "glow", "TYPE": "float", "DEFAULT": 0.9, "MIN": 0.1, "MAX": 2.0, "LABEL": "Glow" }
    ]
}*/

#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
#define resolution RENDERSIZE

#ifdef GL_ES
precision highp float;
#endif

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float field(vec3 p, float s) {
    float accum = 0.0;
    float prev = 0.0;
    float tw = 0.0;
    for (int i = 0; i < 12; i++) {
        float fi = float(i);
        float mag = dot(p, p);
        p = abs(p) / max(mag, 0.001) + vec3(-0.55, -0.42, -1.3);
        float w = exp(-fi / 5.0);
        accum += w * exp(-8.0 * pow(abs(mag - prev), 2.0));
        tw += w;
        prev = mag;
    }
    return max(0.0, 3.5 * accum / tw - 0.4) * s;
}

vec3 particleLayer(vec2 uv, float layer, float t) {
    vec3 col = vec3(0.0);
    float scale = density * (0.4 + layer * 0.35);
    vec2 grid = floor(uv * scale);
    vec2 frac = fract(uv * scale) - 0.5;

    for (int dx = -1; dx <= 1; dx++) {
        for (int dy = -1; dy <= 1; dy++) {
            vec2 cell = grid + vec2(float(dx), float(dy));
            float h = hash(cell + layer * 17.0);
            if (h > 0.82) continue;

            vec2 pos = vec2(hash(cell * 1.7), hash(cell * 2.3 + 5.0)) - 0.5;
            pos += turbulence * 0.15 * vec2(
                sin(t * 2.5 + h * 20.0),
                cos(t * 2.1 + h * 15.0)
            );
            vec2 d = frac - vec2(float(dx), float(dy)) - pos * 0.7;
            float dist = length(d);
            float spark = exp(-dist * dist * (80.0 + layer * 40.0));
            float twinkle = 0.6 + 0.4 * sin(t * 4.0 + h * 40.0);
            vec3 pc = mix(vec3(1.0, 0.7, 0.3), vec3(0.4, 0.6, 1.0), h);
            col += pc * spark * twinkle * (0.3 + layer * 0.5);
        }
    }
    return col;
}

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float aspect = resolution.x / resolution.y;
    vec2 p = (uv - 0.5) * vec2(aspect, 1.0);
    float t = time * (0.3 + turbulence * 0.4);

    vec3 soup = vec3(0.02, 0.01, 0.04);
    vec3 fpos = vec3(p * (1.5 + turbulence), sin(t * 0.2) * 0.1);
    float fog = field(fpos, 0.35 + heat * 0.5);
    soup += vec3(1.0, 0.55, 0.2) * fog * heat * 0.35;
    soup += vec3(0.3, 0.5, 1.0) * pow(fog, 2.5) * 0.2;

    vec3 stars = vec3(0.0);
    for (int i = 0; i < 4; i++) {
        float fi = float(i);
        stars += particleLayer(p + vec2(sin(t + fi), cos(t * 0.7 + fi)) * 0.02 * turbulence, fi + 1.0, t);
    }

    vec3 col = soup + stars * glow;
    col = col / (1.0 + col * 0.6);
    col = pow(col, vec3(0.92));

    gl_FragColor = vec4(col, 1.0);
}