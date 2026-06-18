/*{
    "DESCRIPTION": "Energy Field",
    "CREDIT": "Aday / MacroVerse Origin",
    "ISFVSN": "2.0",
    "CATEGORIES": ["macroverse"],
    "TAGS": ["macroverse", "macroverse-origin", "chapter-01", "cosmic", "ambient"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "rippleAmp", "TYPE": "float", "DEFAULT": 0.35, "MIN": 0.0, "MAX": 1.0, "LABEL": "Ripple amplitude" },
        { "NAME": "driftSpeed", "TYPE": "float", "DEFAULT": 0.12, "MIN": 0.0, "MAX": 0.5, "LABEL": "Drift speed" },
        { "NAME": "fieldScale", "TYPE": "float", "DEFAULT": 2.4, "MIN": 0.5, "MAX": 5.0, "LABEL": "Field scale" },
        { "NAME": "voidDepth", "TYPE": "float", "DEFAULT": 0.85, "MIN": 0.0, "MAX": 1.0, "LABEL": "Void depth" }
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

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p) {
    float v = 0.0;
    float a = 0.5;
    for (int i = 0; i < 5; i++) {
        v += a * noise(p);
        p *= 2.1;
        a *= 0.5;
    }
    return v;
}

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec2 p = (uv - 0.5) * vec2(resolution.x / resolution.y, 1.0) * fieldScale;
    float t = time * driftSpeed;

    vec2 warp = vec2(
        fbm(p * 0.7 + vec2(t * 0.15, 0.0)),
        fbm(p * 0.7 + vec2(0.0, t * 0.12) + 4.2)
    );
    p += (warp - 0.5) * rippleAmp * 0.6;

    float field = fbm(p + t * 0.08);
    float ripple = sin(p.x * 3.0 + t * 0.4) * sin(p.y * 2.5 - t * 0.35);
    field += ripple * rippleAmp * 0.08;

    vec3 deep = vec3(0.01, 0.005, 0.03) * voidDepth;
    vec3 energy = mix(
        vec3(0.02, 0.08, 0.12),
        vec3(0.15, 0.35, 0.55),
        smoothstep(0.35, 0.75, field)
    );
    vec3 glow = vec3(0.0, 0.45, 0.38) * pow(max(field - 0.5, 0.0), 3.0) * rippleAmp;

    vec3 col = deep + energy * (0.25 + 0.55 * field) + glow;
    col *= 0.92 + 0.08 * sin(t * 0.3 + field * 6.28);

    gl_FragColor = vec4(col, 1.0);
}