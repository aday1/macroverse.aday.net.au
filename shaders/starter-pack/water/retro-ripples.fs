/*{
    "DESCRIPTION": "retro ripples",
    "CREDIT": "Macroverse After Dark Collection",
    "ISFVSN": "2.0",
    "CATEGORIES": ["Retro"],
    "TAGS": ["after-dark", "ripples", "interference", "retro", "screensaver", "water"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "rippleSpeed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 3.0, "LABEL": "Ripple speed" },
        { "NAME": "rippleFreq", "TYPE": "float", "DEFAULT": 20.0, "MIN": 5.0, "MAX": 60.0, "LABEL": "Ripple frequency" },
        { "NAME": "sourceCount", "TYPE": "float", "DEFAULT": 3.0, "MIN": 1.0, "MAX": 6.0, "LABEL": "Source count" },
        { "NAME": "damping", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.0, "MAX": 1.0, "LABEL": "Damping" },
        { "NAME": "colorMode", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 3.0, "LABEL": "Color (0=blue,1=rainbow,2=green,3=fire)" },
        { "NAME": "amplitude", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.1, "MAX": 1.5, "LABEL": "Wave amplitude" }
    ]
}*/

#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
#define resolution RENDERSIZE

float hash(float n) { return fract(sin(n) * 43758.5453); }

// Ripple source position: drifts slowly over time
vec2 sourcePos(float idx, float t) {
    float a1 = hash(idx * 7.3) * 6.28;
    float a2 = hash(idx * 13.7) * 6.28;
    float r1 = 0.15 + hash(idx * 23.1) * 0.2;
    float r2 = 0.15 + hash(idx * 31.7) * 0.2;
    float s1 = 0.3 + hash(idx * 41.3) * 0.4;
    float s2 = 0.3 + hash(idx * 47.1) * 0.4;
    return vec2(
        0.5 + r1 * sin(t * s1 + a1),
        0.5 + r2 * cos(t * s2 + a2)
    );
}

void main() {
    vec2 uv = gl_FragCoord.xy / resolution;
    float aspect = resolution.x / resolution.y;
    float t = time * rippleSpeed;

    vec2 scaled = vec2(uv.x * aspect, uv.y);

    // Accumulate wave interference from all sources
    float wave = 0.0;
    float sources = floor(sourceCount);

    for (float i = 0.0; i < 6.0; i++) {
        if (i >= sources) break;

        vec2 src = sourcePos(i, time * 0.3);
        src.x *= aspect;

        float dist = length(scaled - src);

        // Expanding concentric ripple with time offset per source
        float phase = hash(i * 17.0) * 6.28;
        float ripple = sin(dist * rippleFreq - t * 6.0 + phase);

        // Distance-based damping
        float damp = exp(-dist * damping * 3.0);

        // Each source has slightly different frequency
        float freqMod = 1.0 + hash(i * 29.3) * 0.3;
        ripple = sin(dist * rippleFreq * freqMod - t * 6.0 + phase);

        wave += ripple * damp * amplitude;
    }

    // Normalize
    wave /= sources;

    // Color mapping
    vec3 col;
    float mode = floor(colorMode);

    if (mode < 0.5) {
        // Deep blue water
        vec3 deep = vec3(0.02, 0.05, 0.15);
        vec3 mid = vec3(0.1, 0.3, 0.6);
        vec3 bright = vec3(0.4, 0.7, 1.0);
        float w = wave * 0.5 + 0.5;
        col = mix(deep, mid, smoothstep(0.3, 0.5, w));
        col = mix(col, bright, smoothstep(0.6, 0.9, w));
        // Specular highlights on crests
        col += vec3(0.3, 0.35, 0.4) * pow(max(wave, 0.0), 4.0);
    } else if (mode < 1.5) {
        // Rainbow interference
        float hue = fract(wave * 0.3 + time * 0.05);
        float sat = 0.7 + 0.3 * abs(wave);
        float val = 0.3 + 0.7 * (wave * 0.5 + 0.5);
        vec3 c = abs(mod(hue * 6.0 + vec3(0.0, 4.0, 2.0), 6.0) - 3.0) - 1.0;
        col = val * mix(vec3(1.0), clamp(c, 0.0, 1.0), sat);
    } else if (mode < 2.5) {
        // Phosphor green (retro terminal)
        float w = wave * 0.5 + 0.5;
        col = vec3(0.0, w * 0.9, w * 0.3);
        col += vec3(0.0, 0.15, 0.05) * pow(max(wave, 0.0), 3.0);
    } else {
        // Fire
        float w = wave * 0.5 + 0.5;
        col = mix(vec3(0.1, 0.0, 0.0), vec3(1.0, 0.3, 0.0), w);
        col = mix(col, vec3(1.0, 0.9, 0.4), pow(w, 3.0));
    }

    // Subtle ambient ripple on background
    float bgRipple = sin(uv.x * 40.0 + uv.y * 40.0 + time * 2.0) * 0.02;
    col += bgRipple;

    // Vignette
    vec2 vc = uv - 0.5;
    float vignette = 1.0 - dot(vc, vc) * 0.8;
    col *= vignette;

    gl_FragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
