/*{
    "DESCRIPTION": "Life As We Know It",
    "CREDIT": "Aday / MacroVerse Origin",
    "ISFVSN": "2.0",
    "CATEGORIES": ["macroverse"],
    "TAGS": ["macroverse", "macroverse-origin", "chapter-05", "organic", "life"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "coherence", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.0, "LABEL": "Coherence" },
        { "NAME": "chaos", "TYPE": "float", "DEFAULT": 0.45, "MIN": 0.0, "MAX": 1.0, "LABEL": "Chaos" },
        { "NAME": "networkDensity", "TYPE": "float", "DEFAULT": 24.0, "MIN": 8.0, "MAX": 48.0, "LABEL": "Network density" },
        { "NAME": "pulseRate", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 2.0, "LABEL": "Pulse rate" }
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

vec2 cellPoint(vec2 cell, float t) {
    return cell + 0.5 + 0.35 * vec2(
        sin(hash(cell) * 6.28 + t * pulseRate),
        cos(hash(cell + 1.7) * 6.28 + t * pulseRate * 0.8)
    ) * chaos;
}

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float aspect = resolution.x / resolution.y;
    vec2 pos = uv * vec2(aspect, 1.0);
    float t = time;

    float M = networkDensity;
    vec2 cuadrant = floor(pos * M) / M;
    vec2 frac = fract(pos * M);

    float minDist = 10000.0;
    vec2 nearest = vec2(0.0);
    vec2 nearestCell = vec2(0.0);

    for (int i = -1; i <= 1; i++) {
        for (int j = -1; j <= 1; j++) {
            vec2 offset = vec2(float(i), float(j)) / M;
            vec2 cell = cuadrant + offset;
            vec2 point = cellPoint(cell, t) / M;
            vec2 local = offset + point / M - frac / M;
            float d = length(local);
            if (d < minDist) {
                minDist = d;
                nearest = local;
                nearestCell = cell;
            }
        }
    }

    float edge = minDist * M;
    float cellId = hash(nearestCell);
    float pulse = 0.5 + 0.5 * sin(t * pulseRate * 3.0 + cellId * 12.0);

    vec3 bioA = vec3(0.05, 0.35, 0.28);
    vec3 bioB = vec3(0.1, 0.65, 0.55);
    vec3 cellCol = mix(bioA, bioB, cellId);
    cellCol += vec3(0.2, 0.9, 0.7) * pulse * coherence;

    float membrane = smoothstep(0.08, 0.02, edge);
    float network = smoothstep(0.15, 0.0, abs(edge - 0.06)) * coherence;

    vec3 col = vec3(0.01, 0.02, 0.04);
    col += cellCol * membrane;
    col += vec3(0.15, 0.85, 0.65) * network * 0.6;

    float link = sin(nearest.x * 40.0 + t) * sin(nearest.y * 40.0 - t * 0.7);
    col += vec3(0.0, 0.4, 0.35) * max(link, 0.0) * coherence * 0.15 * membrane;

    col = pow(col, vec3(0.95));

    gl_FragColor = vec4(col, 1.0);
}