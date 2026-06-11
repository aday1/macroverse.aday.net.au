/*{
    "DESCRIPTION": "brush strokes",
    "CREDIT": "Inspired by abstract expressionist paintings",
    "ISFVSN": "2.0",
    "CATEGORIES": ["Generator", "Abstract"],
    "INPUTS": [
        {
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 0.1,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Animation speed"
        },
        {
            "NAME": "strokeDensity",
            "TYPE": "float",
            "DEFAULT": 12.0,
            "MIN": 4.0,
            "MAX": 24.0,
            "LABEL": "Stroke density"
        },
        {
            "NAME": "canvasTexture",
            "TYPE": "float",
            "DEFAULT": 0.15,
            "MIN": 0.0,
            "MAX": 0.5,
            "LABEL": "Canvas weave"
        },
        {
            "NAME": "blueIntensity",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Blue cluster intensity"
        },
        {
            "NAME": "purpleIntensity",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Purple cluster intensity"
        }
    ]
}*/

#ifdef GL_ES
precision highp float;
#endif

#define PI 3.14159265359

float hash21(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

vec2 hash22(vec2 p) {
    vec2 h = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return fract(sin(h) * 43758.5453);
}

float hash23(vec2 p, float z) {
    return fract(sin(dot(p, vec2(127.1, 311.7)) + z) * 43758.5453);
}

float sdSegment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

float strokeLayer(vec2 uv, float scale, float time, float seed) {
    vec2 id = floor(uv * scale);
    vec2 gv = fract(uv * scale) - 0.5;
    float d = 1.0;
    for (int j = -1; j <= 1; j++) {
        for (int i = -1; i <= 1; i++) {
            vec2 cellId = id + vec2(float(i), float(j));
            float rnd = hash21(cellId + seed);
            float angle = rnd * PI * 2.0 + time * 0.3;
            float len = 0.2 + rnd * 0.4;
            vec2 dir = vec2(cos(angle), sin(angle));
            vec2 a = hash22(cellId) - 0.5;
            vec2 b = a + dir * len;
            vec2 cellUv = gv - vec2(float(i), float(j));
            float segD = sdSegment(cellUv, a, b);
            float w = 0.08 + hash23(cellId, 1.0) * 0.06;
            d = min(d, segD - w);
        }
    }
    return 1.0 - smoothstep(0.0, 0.02, d);
}

float fbm(vec2 p, float time) {
    float f = 0.0;
    float a = 0.5;
    vec2 pp = p;
    for (int i = 0; i < 5; i++) {
        float n = hash21(pp + float(i) * 0.1);
        f += a * n;
        a *= 0.5;
        pp = pp * 2.0 + vec2(sin(time * 0.2 + float(i)), cos(time * 0.15 + float(i) * 1.3));
    }
    return f;
}

void main() {
    vec2 uv = (gl_FragCoord.xy - 0.5 * RENDERSIZE.xy) / min(RENDERSIZE.x, RENDERSIZE.y);
    uv.x *= RENDERSIZE.x / RENDERSIZE.y;
    float t = TIME * speed;

    float layer1 = strokeLayer(uv, strokeDensity, t, 0.0);
    float layer2 = strokeLayer(uv * 1.3 + 0.7, strokeDensity * 0.8, t * 1.1, 10.0);
    float layer3 = strokeLayer(uv * 0.9 - 0.3, strokeDensity * 1.1, t * 0.9, 20.0);

    float strokeMask = max(max(layer1, layer2), layer3);
    float chaos = fbm(uv * 4.0, t);

    float blueZone = smoothstep(0.3, 0.8, uv.x + 0.2) * smoothstep(-0.5, 0.5, uv.y);
    float purpleZone = smoothstep(0.5, 0.0, uv.x) * smoothstep(-0.8, 0.2, uv.y);
    float neutralBase = 1.0 - blueZone * 0.7 - purpleZone * 0.7;

    vec3 blueLow = vec3(0.1, 0.2, 0.5);
    vec3 blueMid = vec3(0.2, 0.5, 0.85);
    vec3 blueHigh = vec3(0.5, 0.75, 1.0);
    vec3 purpleLow = vec3(0.25, 0.1, 0.35);
    vec3 purpleMid = vec3(0.5, 0.25, 0.6);
    vec3 purpleHigh = vec3(0.7, 0.5, 0.85);
    vec3 neutralDark = vec3(0.15, 0.15, 0.18);
    vec3 neutralMid = vec3(0.5, 0.48, 0.52);
    vec3 neutralLight = vec3(0.85, 0.82, 0.78);
    vec3 beige = vec3(0.72, 0.68, 0.62);

    float strokeVal = strokeMask * (0.6 + chaos * 0.4);
    float zoneMix = chaos * 0.3 + strokeVal * 0.7;

    vec3 blueCol = mix(blueLow, mix(blueMid, blueHigh, zoneMix), strokeVal);
    vec3 purpleCol = mix(purpleLow, mix(purpleMid, purpleHigh, zoneMix), strokeVal);
    vec3 neutralCol = mix(neutralDark, mix(neutralMid, mix(neutralLight, beige, chaos), zoneMix), strokeVal);

    vec3 col = neutralCol;
    col = mix(col, blueCol, blueZone * blueIntensity * strokeVal);
    col = mix(col, purpleCol, purpleZone * purpleIntensity * strokeVal);

    float blackStroke = smoothstep(0.3, 0.7, strokeMask) * (0.3 + chaos * 0.5) * neutralBase;
    col = mix(col, vec3(0.08, 0.08, 0.1), blackStroke * 0.5);

    float canvasWeave = (hash21(floor(gl_FragCoord.xy * 3.0)) - 0.5) * 2.0;
    col += canvasWeave * canvasTexture;

    col = clamp(col, 0.0, 1.0);
    gl_FragColor = vec4(col, 1.0);
}
