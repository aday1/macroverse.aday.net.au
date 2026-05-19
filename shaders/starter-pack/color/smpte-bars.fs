/*{
    "DESCRIPTION": "smpte bars",
    "CREDIT": "Macroverse V5",
    "ISFVSN": "2.0",
    "CATEGORIES": ["generator"],
    "TAGS": ["test", "color-bars", "broadcast", "signal"],
    "INPUTS": [
        {
            "NAME": "scanlineIntensity",
            "TYPE": "float",
            "DEFAULT": 0.3,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Scanline Intensity"
        },
        {
            "NAME": "noiseAmount",
            "TYPE": "float",
            "DEFAULT": 0.05,
            "MIN": 0.0,
            "MAX": 0.5,
            "LABEL": "Noise Amount"
        },
        {
            "NAME": "vhsWobble",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "VHS Wobble"
        }
    ]
}*/

uniform float scanlineIntensity; // @expose 0 1
uniform float noiseAmount; // @expose 0 0.5
uniform float vhsWobble; // @expose 0 1

#define time TIME
#define resolution RENDERSIZE

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

void main() {
    vec2 uv = gl_FragCoord.xy / resolution;

    float wobble = sin(uv.y * 40.0 + time * 3.0) * vhsWobble * 0.01;
    uv.x += wobble;

    vec3 col = vec3(0.0);
    float x = uv.x;
    float y = uv.y;

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
        else if (x < 2.0/7.0) col = vec3(0.0,  0.0,  0.0);
        else if (x < 3.0/7.0) col = vec3(0.75, 0.0,  0.75);
        else if (x < 4.0/7.0) col = vec3(0.0,  0.0,  0.0);
        else if (x < 5.0/7.0) col = vec3(0.0,  0.75, 0.75);
        else if (x < 6.0/7.0) col = vec3(0.0,  0.0,  0.0);
        else                   col = vec3(0.75, 0.75, 0.75);
    } else {
        if      (x < 1.0/6.0) col = vec3(0.0, 0.05, 0.15);
        else if (x < 2.0/6.0) col = vec3(1.0, 1.0,  1.0);
        else if (x < 3.0/6.0) col = vec3(0.19, 0.0, 0.42);
        else if (x < 4.0/6.0) col = vec3(0.0);
        else {
            float ramp = (x - 4.0/6.0) / (2.0/6.0);
            col = vec3(ramp);
        }
    }

    float scanline = 1.0 - scanlineIntensity * 0.5 * (1.0 + sin(gl_FragCoord.y * 3.14159 * 2.0));
    float n = hash(uv * 1000.0 + time * 100.0) * noiseAmount;
    col = col * scanline + n;
    col = clamp(col, 0.0, 1.0);
    gl_FragColor = vec4(col, 1.0);
}
