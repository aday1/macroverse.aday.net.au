/*{
    "DESCRIPTION": "Blue Giants",
    "CREDIT": "Macroverse — Microvirtuosity",
    "ISFVSN": "2.0",
    "CATEGORIES": ["macroverse"],
    "TAGS": ["macroverse-set", "stars", "blue", "fusion"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "starCount", "TYPE": "float", "DEFAULT": 18.0, "MIN": 6.0, "MAX": 40.0, "LABEL": "Giant count" },
        { "NAME": "ferocity", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.3, "MAX": 2.5, "LABEL": "Surface ferocity" },
        { "NAME": "filament", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.5, "LABEL": "Gas filaments" }
    ]
}*/

#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
#define resolution RENDERSIZE

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

vec2 starPos(vec2 id, float t) {
    vec2 seed = vec2(hash(id), hash(id + 17.3));
    float pulse = sin(t * 0.7 + seed.x * 12.0) * 0.03;
    return (seed - 0.5) * 1.6 + vec2(pulse, -pulse * 0.5);
}

void main(void) {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec2 aspect = vec2(resolution.x / resolution.y, 1.0);
    vec2 p = (uv - 0.5) * aspect;
    float t = time;

    vec3 col = vec3(0.01, 0.02, 0.08);

    float fil = 0.0;
    for (int i = 0; i < 3; i++) {
        float fi = float(i);
        vec2 q = p * (2.0 + fi) + vec2(t * 0.05, t * 0.03);
        fil += abs(sin(q.x * 5.0 + sin(q.y * 4.0))) * 0.08;
    }
    col += vec3(0.1, 0.25, 0.9) * fil * filament;

    float n = floor(starCount);
    for (float i = 0.0; i < 40.0; i += 1.0) {
        if (i >= n) break;
        vec2 id = vec2(i, floor(i * 0.37));
        vec2 sp = starPos(id, t);
        float mass = 0.5 + hash(id + 3.1);
        vec2 d = p - sp;
        float dist = length(d);

        float core = exp(-dist * dist / (0.002 + 0.004 * mass));
        float corona = exp(-dist / (0.08 + 0.12 * mass)) * 0.35;
        float flare = abs(sin(atan(d.y, d.x) * 8.0 + t * 3.0 * ferocity)) * corona;

        vec3 blue = mix(vec3(0.55, 0.75, 1.0), vec3(0.15, 0.45, 1.0), mass);
        vec3 hot = vec3(0.85, 0.95, 1.0);
        col += mix(blue, hot, core) * (core * 2.5 + corona + flare) * ferocity;
    }

    col = col / (col + vec3(0.6));
    gl_FragColor = vec4(pow(col, vec3(0.95)), 1.0);
}