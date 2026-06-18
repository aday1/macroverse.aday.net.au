/*{
    "DESCRIPTION": "Energy Field",
    "CREDIT": "Macroverse — Microvirtuosity",
    "ISFVSN": "2.0",
    "CATEGORIES": ["macroverse"],
    "TAGS": ["macroverse-set", "cosmic", "energy", "origin"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "waveIntensity", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 3.0, "LABEL": "Wave intensity" },
        { "NAME": "rippleSpeed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 4.0, "LABEL": "Ripple speed" },
        { "NAME": "colorIntensity", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.2, "MAX": 2.5, "LABEL": "Color intensity" },
        { "NAME": "mouseInfluence", "TYPE": "float", "DEFAULT": 0.35, "MIN": 0.0, "MAX": 1.0, "LABEL": "Mouse drift" }
    ]
}*/

#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
#define resolution RENDERSIZE

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

void main(void) {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec2 aspect = vec2(resolution.x / resolution.y, 1.0);
    vec2 p = (uv - 0.5) * aspect;

    float t = time * rippleSpeed;
    vec2 mouse = vec2(mouseX, mouseY) * 2.0 - 1.0;
    vec2 pos = p + mouse * mouseInfluence * 0.25;

    float field = 0.0;
    field += sin(pos.x * cos(t * 0.8) * 18.0 + pos.y * 2.5);
    field += cos(pos.y * sin(t * 0.6) * 14.0 - pos.x * 3.0);
    field += sin((pos.x + pos.y) * sin(t * 0.35) * 9.0);
    field += noise(pos * 4.0 + t * 0.15) * 2.0 - 1.0;
    field *= sin(t * 0.5) * 0.35 + 0.65;
    field *= waveIntensity;

    float glow = smoothstep(0.2, 1.4, abs(field));
    vec3 deep = vec3(0.02, 0.04, 0.14);
    vec3 teal = vec3(0.05, 0.55, 0.72);
    vec3 violet = vec3(0.45, 0.12, 0.65);
    vec3 col = mix(deep, mix(teal, violet, 0.5 + 0.5 * sin(field * 1.7 + t)), glow);
    col += vec3(0.08, 0.18, 0.35) * (0.35 + 0.65 * noise(uv * 3.0 + t * 0.05));
    col *= colorIntensity;

    gl_FragColor = vec4(col, 1.0);
}