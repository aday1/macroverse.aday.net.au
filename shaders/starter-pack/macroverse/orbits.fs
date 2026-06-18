/*{
    "DESCRIPTION": "Orbits",
    "CREDIT": "Aday / MacroVerse Origin",
    "ISFVSN": "2.0",
    "CATEGORIES": ["macroverse"],
    "TAGS": ["macroverse", "macroverse-origin", "chapter-04", "cosmic", "orbit"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "orbitSpeed", "TYPE": "float", "DEFAULT": 0.35, "MIN": 0.0, "MAX": 1.5, "LABEL": "Orbit speed" },
        { "NAME": "bodyCount", "TYPE": "float", "DEFAULT": 5.0, "MIN": 2.0, "MAX": 8.0, "LABEL": "Body count" },
        { "NAME": "trailLength", "TYPE": "float", "DEFAULT": 0.55, "MIN": 0.0, "MAX": 1.0, "LABEL": "Trail length" },
        { "NAME": "galaxyTilt", "TYPE": "float", "DEFAULT": 0.25, "MIN": -0.5, "MAX": 0.5, "LABEL": "Galaxy tilt" }
    ]
}*/

#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
#define resolution RENDERSIZE

#ifdef GL_ES
precision highp float;
#endif

const float PI = 3.14159265;

float hash(float n) {
    return fract(sin(n) * 43758.5453);
}

float orbitRing(vec2 p, vec2 center, float rx, float ry, float angle, float width) {
    vec2 d = p - center;
    float c = cos(angle);
    float s = sin(angle);
    d = mat2(c, -s, s, c) * d;
    float e = length(vec2(d.x / rx, d.y / ry)) - 1.0;
    return smoothstep(width, 0.0, abs(e));
}

vec3 planet(vec2 p, vec2 pos, float size, vec3 col) {
    float d = length(p - pos);
    float body = smoothstep(size, size * 0.3, d);
    float glow = exp(-d * d / (size * size * 4.0)) * 0.3;
    return col * (body + glow);
}

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float aspect = resolution.x / resolution.y;
    vec2 p = (uv - 0.5) * vec2(aspect, 1.0);
    float t = time * orbitSpeed;

    vec3 col = vec3(0.01, 0.015, 0.04);

    float tilt = galaxyTilt;
    vec2 gp = p;
    gp.x += gp.y * tilt;
    float spiral = sin(atan(gp.y, gp.x) * 3.0 + length(gp) * 8.0 - t * 0.5);
    col += vec3(0.08, 0.05, 0.15) * smoothstep(0.2, 0.9, spiral * 0.5 + 0.5) * exp(-length(gp) * 1.2) * 0.4;

    int bodies = int(floor(bodyCount + 0.5));
    for (int i = 0; i < 8; i++) {
        if (i >= bodies) break;
        float fi = float(i);
        float seed = hash(fi * 13.7);
        vec2 center = vec2(cos(seed * 6.28) * 0.1, sin(seed * 4.2) * 0.08);
        float rx = 0.15 + fi * 0.08;
        float ry = rx * (0.65 + seed * 0.3);
        float ang = t * (0.8 + seed) + fi * 1.2;

        float ring = orbitRing(p, center, rx, ry, ang * 0.15, 0.008 + trailLength * 0.012);
        col += vec3(0.2, 0.35, 0.55) * ring * (0.4 + 0.6 * trailLength);

        vec2 orbitPos = center + vec2(cos(ang), sin(ang)) * rx;
        vec3 pcol = mix(vec3(0.5, 0.7, 1.0), vec3(1.0, 0.8, 0.5), hash(fi + 2.0));
        col += planet(p, orbitPos, 0.012 + seed * 0.01, pcol);
    }

    col += vec3(0.15, 0.2, 0.35) * exp(-length(p) * 0.8) * 0.15;
    col = col / (1.0 + col * 0.4);

    gl_FragColor = vec4(col, 1.0);
}