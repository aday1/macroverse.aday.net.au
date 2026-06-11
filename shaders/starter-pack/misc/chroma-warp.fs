/*{
    "DESCRIPTION": "chroma warp",
    "CREDIT": "Macroverse V5",
    "ISFVSN": "2.0",
    "CATEGORIES": ["abstract"],
    "TAGS": ["texture-input", "fx", "chromatic", "warp", "vj"],
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image",
            "LABEL": "Input Texture"
        },
        {
            "NAME": "chromaAmount",
            "TYPE": "float",
            "DEFAULT": 0.01,
            "MIN": 0.0,
            "MAX": 0.1,
            "LABEL": "Chroma Aberration"
        },
        {
            "NAME": "warpAmount",
            "TYPE": "float",
            "DEFAULT": 0.3,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Warp Amount"
        },
        {
            "NAME": "warpSpeed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Warp Speed"
        },
        {
            "NAME": "zoomPulse",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 0.5,
            "LABEL": "Zoom Pulse"
        },
        {
            "NAME": "kalSegments",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 12.0,
            "LABEL": "Kaleidoscope Segments"
        },
        {
            "NAME": "vignetteAmt",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Vignette"
        }
    ]
}*/

uniform sampler2D inputImage;
uniform float chromaAmount; // @expose 0 0.1
uniform float warpAmount; // @expose 0 2
uniform float warpSpeed; // @expose 0 5
uniform float zoomPulse; // @expose 0 0.5
uniform float kalSegments; // @expose 0 12
uniform float vignetteAmt; // @expose 0 3

#define time TIME
#define resolution RENDERSIZE

vec3 colorBars(vec2 uv) {
    float x = uv.x;
    float y = uv.y;
    vec3 col = vec3(0.0);
    if (y > 0.33) {
        if      (x < 1.0/7.0) col = vec3(0.75, 0.75, 0.75);
        else if (x < 2.0/7.0) col = vec3(0.75, 0.75, 0.0);
        else if (x < 3.0/7.0) col = vec3(0.0,  0.75, 0.75);
        else if (x < 4.0/7.0) col = vec3(0.0,  0.75, 0.0);
        else if (x < 5.0/7.0) col = vec3(0.75, 0.0,  0.75);
        else if (x < 6.0/7.0) col = vec3(0.75, 0.0,  0.0);
        else                   col = vec3(0.0,  0.0,  0.75);
    } else if (y > 0.25) {
        if      (x < 1.0/7.0) col = vec3(0.0,  0.0,  0.75);
        else if (x < 2.0/7.0) col = vec3(0.0);
        else if (x < 3.0/7.0) col = vec3(0.75, 0.0,  0.75);
        else if (x < 4.0/7.0) col = vec3(0.0);
        else if (x < 5.0/7.0) col = vec3(0.0,  0.75, 0.75);
        else if (x < 6.0/7.0) col = vec3(0.0);
        else                   col = vec3(0.75, 0.75, 0.75);
    } else {
        if      (x < 1.0/6.0) col = vec3(0.0, 0.05, 0.15);
        else if (x < 2.0/6.0) col = vec3(1.0);
        else if (x < 3.0/6.0) col = vec3(0.19, 0.0, 0.42);
        else if (x < 4.0/6.0) col = vec3(0.0);
        else col = vec3((x - 4.0/6.0) / (2.0/6.0));
    }
    return col;
}

vec3 sampleInput(vec2 uv) {
    uv = clamp(uv, 0.0, 1.0);
    vec4 texCol = texture2D(inputImage, uv);
    if (texCol.r > 0.49 && texCol.r < 0.52 &&
        texCol.g > 0.49 && texCol.g < 0.52 &&
        texCol.b > 0.49 && texCol.b < 0.52) {
        return colorBars(uv);
    }
    return texCol.rgb;
}

void main() {
    vec2 uv = gl_FragCoord.xy / resolution;
    vec2 center = uv - 0.5;

    float z = 1.0 + sin(time * 2.0) * zoomPulse;
    center /= z;

    if (kalSegments > 1.0) {
        float segments = floor(kalSegments);
        float a = atan(center.y, center.x);
        float r = length(center);
        float seg = 6.28318 / segments;
        a = mod(a, seg);
        a = abs(a - seg * 0.5);
        center = vec2(cos(a), sin(a)) * r;
    }

    float dist = length(center);
    float warp = sin(dist * 10.0 - time * warpSpeed * 3.0) * warpAmount * 0.05;
    center += center * warp;

    vec2 sampleUV = center + 0.5;

    vec2 dir = normalize(center + 0.001) * chromaAmount;
    float r = sampleInput(sampleUV + dir).r;
    float g = sampleInput(sampleUV).g;
    float b = sampleInput(sampleUV - dir).b;

    vec3 col = vec3(r, g, b);

    col *= 0.95 + 0.05 * sin(gl_FragCoord.y * 3.14159);

    float vignette = 1.0 - dot(center, center) * vignetteAmt;
    col *= clamp(vignette, 0.0, 1.0);

    col = clamp(col, 0.0, 1.0);
    gl_FragColor = vec4(col, 1.0);
}
