/*{
    "DESCRIPTION": "Blue Giants",
    "CREDIT": "Aday / MacroVerse Origin",
    "ISFVSN": "2.0",
    "CATEGORIES": ["macroverse"],
    "TAGS": ["macroverse", "macroverse-origin", "chapter-03", "cosmic", "stars"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "starMass", "TYPE": "float", "DEFAULT": 3.0, "MIN": 1.0, "MAX": 6.0, "LABEL": "Star mass" },
        { "NAME": "coreIntensity", "TYPE": "float", "DEFAULT": 1.2, "MIN": 0.3, "MAX": 2.5, "LABEL": "Core intensity" },
        { "NAME": "blueBias", "TYPE": "float", "DEFAULT": 0.85, "MIN": 0.0, "MAX": 1.0, "LABEL": "Blue bias" },
        { "NAME": "collapse", "TYPE": "float", "DEFAULT": 0.4, "MIN": 0.0, "MAX": 1.0, "LABEL": "Collapse" }
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

vec3 blueGiant(vec2 uv, vec2 center, float radius, float phase) {
    vec2 d = uv - center;
    float dist = length(d);
    float core = exp(-dist * dist / (radius * radius * 0.08));
    float corona = exp(-dist / (radius * 0.35)) * 0.4;
    float flare = exp(-abs(d.x) * 12.0 / radius) * exp(-abs(d.y) * 8.0 / radius) * 0.15;
    float pulse = 0.85 + 0.15 * sin(time * 1.5 + phase);

    vec3 hot = mix(vec3(0.6, 0.75, 1.0), vec3(0.35, 0.55, 1.0), blueBias);
    vec3 white = vec3(0.85, 0.92, 1.0);
    return mix(hot, white, core) * (core * 2.5 + corona + flare) * pulse * coreIntensity;
}

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float aspect = resolution.x / resolution.y;
    vec2 p = (uv - 0.5) * vec2(aspect, 1.0);

    vec3 col = vec3(0.005, 0.008, 0.025);

    float baseR = 0.12 * starMass;
    col += blueGiant(p, vec2(-0.35, 0.15) + vec2(sin(time * 0.08), cos(time * 0.06)) * 0.02,
        baseR * 0.7 * (1.0 - collapse * 0.15 * sin(time * 0.4)), 0.0);
    col += blueGiant(p, vec2(0.28, -0.22) + vec2(sin(time * 0.08 + 1.0), cos(time * 0.06 + 2.0)) * 0.02,
        baseR * 0.85 * (1.0 - collapse * 0.15 * sin(time * 0.4 + 1.7)), 2.1);
    col += blueGiant(p, vec2(0.05, 0.32) + vec2(sin(time * 0.08 + 2.0), cos(time * 0.06 + 4.0)) * 0.02,
        baseR * 1.0 * (1.0 - collapse * 0.15 * sin(time * 0.4 + 3.4)), 4.2);
    col += blueGiant(p, vec2(-0.15, -0.35) + vec2(sin(time * 0.08 + 3.0), cos(time * 0.06 + 6.0)) * 0.02,
        baseR * 1.15 * (1.0 - collapse * 0.15 * sin(time * 0.4 + 5.1)), 6.3);
    col += blueGiant(p, vec2(0.42, 0.08) + vec2(sin(time * 0.08 + 4.0), cos(time * 0.06 + 8.0)) * 0.02,
        baseR * 1.3 * (1.0 - collapse * 0.15 * sin(time * 0.4 + 6.8)), 8.4);

    float vign = 1.0 - length(p) * collapse * 0.35;
    col *= max(vign, 0.4);
    col = col / (1.0 + col * 0.5);

    gl_FragColor = vec4(col, 1.0);
}