/*
 * Vaporwave Bliss - Windows XP Bliss wallpaper reimagined
 * Rolling green hills, fuchsia sky, palm trees, dissolving Windows logo
 */

#ifdef GL_ES
precision highp float;
#endif

uniform float time;
uniform vec2 resolution;
uniform vec2 mouse;

#define iTime time
#define iResolution resolution
#define iMouse vec4(mouse, 0.0, 0.0)

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
    vec2 q = p;
    for (int i = 0; i < 4; i++) {
        v += a * noise(q);
        q *= 2.0;
        a *= 0.5;
    }
    return v;
}

void mainImage(out vec4 fragColor, in vec2 fragCoord) {
    vec2 p = (fragCoord.xy - 0.5 * iResolution.xy) / iResolution.y;

    vec3 col = vec3(0.0);

    float skyFade = smoothstep(-0.1, 0.5, p.y);
    vec3 skyTop = vec3(0.85, 0.25, 0.65);
    vec3 skyHorizon = vec3(0.95, 0.55, 0.75);
    vec3 sky = mix(skyHorizon, skyTop, skyFade);

    float hill = 0.0;
    float hillY = -0.2 + 0.12 * sin(p.x * 2.5);
    hill = p.y - hillY - 0.35 * exp(-p.x * p.x * 1.5);
    hill += 0.12 * fbm(p * 6.0 + vec2(iTime * 0.015, 0.0));
    hill += 0.04 * sin(p.x * 15.0) * exp(-p.x * p.x * 2.0);

    float onHill = smoothstep(0.025, -0.025, hill);

    vec3 grassBase = vec3(0.15, 0.45, 0.2);
    vec3 grassLight = vec3(0.25, 0.55, 0.25);
    float grassNoise = fbm(p * 40.0 + 100.0);
    vec3 grass = mix(grassBase, grassLight, grassNoise * 0.5 + 0.5);
    float lightDir = dot(normalize(vec2(1.0, 1.0)), normalize(vec2(p.x, hill)));
    grass = mix(grass * 0.85, grass, smoothstep(-0.3, 0.5, lightDir));

    col = mix(sky, grass, onHill);

    float distMount = 0.0;
    for (float i = 0.0; i < 3.0; i++) {
        float mx = p.x * (1.0 + i * 0.3) + i * 0.5 + iTime * 0.01;
        float my = p.y + 0.1 * i - 0.15 * noise(vec2(mx * 2.0, i));
        distMount += smoothstep(0.0, -0.1, my - 0.3 * exp(-mx * mx * 0.5));
    }
    vec3 mountColor = vec3(0.6, 0.35, 0.55);
    col = mix(col, mountColor, distMount * 0.4 * smoothstep(0.0, 0.3, p.y));

    float cloud = fbm(p * 3.0 + vec2(iTime * 0.03, 0.2)) * fbm(p * 2.0 - 50.0);
    cloud = smoothstep(0.45, 0.55, cloud);
    col = mix(col, vec3(1.0, 0.95, 1.0), cloud * 0.6 * smoothstep(-0.2, 0.5, p.y));

    vec2 palmBase1 = vec2(-0.4, 0.02);
    vec2 palmBase2 = vec2(-0.18, 0.06);
    vec2 palmBase3 = vec2(0.02, 0.0);
    float palmShadow = 0.0;
    for (int i = 0; i < 3; i++) {
        vec2 base = i == 0 ? palmBase1 : (i == 1 ? palmBase2 : palmBase3);
        vec2 toP = p - base;
        float trunk = smoothstep(0.018, 0.012, abs(toP.x)) * smoothstep(base.y + 0.35, base.y - 0.02, p.y);
        vec3 trunkColor = vec3(0.45, 0.38, 0.32);
        col = mix(col, trunkColor, trunk);

        float frond = 0.0;
        for (float j = 0.0; j < 6.0; j++) {
            float a = j * 1.05 - 1.5;
            vec2 fp = toP - vec2(sin(a) * 0.08, 0.18 + cos(a) * 0.06);
            frond = max(frond, smoothstep(0.04, 0.02, length(fp)) * smoothstep(base.y + 0.4, base.y, p.y));
        }
        vec3 frondColor = vec3(0.08, 0.3, 0.12);
        col = mix(col, frondColor, frond);

        float shad = smoothstep(0.0, 0.12, p.x - base.x) * smoothstep(0.35, 0.0, p.y - base.y);
        shad *= smoothstep(-0.05, 0.15, hill);
        palmShadow = max(palmShadow, shad * 0.55);
    }
    col = mix(col, grassBase * 0.55, palmShadow);

    vec2 logoCenter = vec2(0.5, 0.6);
    vec2 logoUV = (p - logoCenter) * 7.0;
    float pixelSize = 5.0 + 4.0 * (0.5 + 0.5 * sin(iTime * 1.8));
    vec2 pixelUV = floor(logoUV * pixelSize) / pixelSize;
    float dissolve = 0.4 + 0.5 * sin(iTime * 1.2);
    float pixelate = step(hash(pixelUV + 12.34), dissolve);

    float q1 = smoothstep(0.38, 0.28, max(abs(logoUV.x - 0.22), abs(logoUV.y - 0.22)));
    float q2 = smoothstep(0.38, 0.28, max(abs(logoUV.x + 0.22), abs(logoUV.y - 0.22)));
    float q3 = smoothstep(0.38, 0.28, max(abs(logoUV.x - 0.22), abs(logoUV.y + 0.22)));
    float q4 = smoothstep(0.38, 0.28, max(abs(logoUV.x + 0.22), abs(logoUV.y + 0.22)));
    vec3 red = vec3(0.92, 0.18, 0.18);
    vec3 green = vec3(0.18, 0.58, 0.18);
    vec3 blue = vec3(0.18, 0.28, 0.88);
    vec3 yellow = vec3(0.98, 0.82, 0.18);
    vec3 logoCol = vec3(0.0);
    logoCol = mix(logoCol, red, q1);
    logoCol = mix(logoCol, green, q2);
    logoCol = mix(logoCol, blue, q3);
    logoCol = mix(logoCol, yellow, q4);
    float logoFrame = smoothstep(0.48, 0.42, max(abs(logoUV.x), abs(logoUV.y)));
    logoCol = mix(logoCol, vec3(0.22, 0.22, 0.28), logoFrame);
    float logoMask = max(max(q1, q2), max(q3, q4)) + logoFrame;
    logoMask *= pixelate;
    col = mix(col, logoCol, logoMask * smoothstep(-0.1, 0.4, p.y));

    float vignette = 1.0 - 0.3 * length(p);
    col *= vignette;

    fragColor = vec4(col, 1.0);
}

void main(void) {
    mainImage(gl_FragColor, gl_FragCoord.xy);
    gl_FragColor.a = 1.0;
}
