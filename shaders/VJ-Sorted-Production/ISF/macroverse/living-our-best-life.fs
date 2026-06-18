/*{
    "DESCRIPTION": "Living Our Best Life",
    "CREDIT": "Macroverse — Microvirtuosity",
    "ISFVSN": "2.0",
    "CATEGORIES": ["macroverse"],
    "TAGS": ["macroverse-set", "stars", "life", "golden"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "starLayers", "TYPE": "float", "DEFAULT": 4.0, "MIN": 2.0, "MAX": 6.0, "LABEL": "Star layers" },
        { "NAME": "warmth", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.3, "MAX": 2.0, "LABEL": "Golden warmth" },
        { "NAME": "shimmer", "TYPE": "float", "DEFAULT": 0.8, "MIN": 0.0, "MAX": 2.0, "LABEL": "Star shimmer" }
    ]
}*/

#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
#define resolution RENDERSIZE

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

vec3 starLayer(vec2 uv, float depth, float t) {
    vec3 col = vec3(0.0);
    float scale = 80.0 + depth * 60.0;
    vec2 g = floor(uv * scale);
    vec2 f = fract(uv * scale) - 0.5;

    for (float dx = -1.0; dx <= 1.0; dx += 1.0) {
        for (float dy = -1.0; dy <= 1.0; dy += 1.0) {
            vec2 cell = g + vec2(dx, dy);
            float h = hash(cell + depth * 13.0);
            if (h < 0.72) continue;

            vec2 pos = vec2(hash(cell + 1.1), hash(cell + 4.4)) - 0.5;
            vec2 diff = f - vec2(dx, dy) - pos * 0.5;
            float d = length(diff);
            float twinkle = 0.6 + 0.4 * sin(t * (2.0 + h * 5.0) * shimmer + h * 40.0);
            float star = exp(-d * d * (500.0 + depth * 200.0)) * twinkle;

            vec3 gold = mix(vec3(1.0, 0.75, 0.35), vec3(1.0, 0.95, 0.7), h);
            col += gold * star * (0.4 + depth * 0.25) * warmth;
        }
    }
    return col;
}

void main(void) {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float t = time;

    vec3 sky = mix(vec3(0.04, 0.03, 0.12), vec3(0.12, 0.06, 0.22), uv.y);
    vec3 col = sky;

    float n = floor(starLayers);
    for (float layer = 0.0; layer < 6.0; layer += 1.0) {
        if (layer >= n) break;
        col += starLayer(uv, layer + 1.0, t);
    }

    float horizon = smoothstep(0.15, 0.55, uv.y);
    col += vec3(0.9, 0.45, 0.15) * horizon * 0.15 * warmth;

    float aura = exp(-length((uv - 0.5) * vec2(1.0, 0.7)) * 1.5);
    col += vec3(1.0, 0.85, 0.5) * aura * 0.08 * warmth;

    col = col / (col + vec3(0.25));
    gl_FragColor = vec4(pow(col, vec3(0.9)), 1.0);
}