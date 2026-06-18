/*{
    "DESCRIPTION": "SquareCircles",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "geometric"
    ],
    "INPUTS": [
        {
            "NAME": "useFrameIndex",
            "TYPE": "bool",
            "DEFAULT": 0,
            "LABEL": "Use frame index (timeline sync)"
        },
        {
            "NAME": "fps",
            "TYPE": "float",
            "DEFAULT": 60.0,
            "MIN": 24.0,
            "MAX": 120.0
        }
    ],
    "TAGS": [
        "geometric"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float; 
#endif

//uniform vec4 mouse;

const float pi = 3.141592653589793;

float sdHex(in vec2 p) {
    float t = time;
    float c = cos(t);
    float s = sin(t);
    mat2 m = mat2(c, -s, s, c);
    p = m * sin(p * pi * 1.0);
    return min(abs(p.x + p.y), abs(p.x - p.y)) - 0.1;
}

void main() { 
    vec2 p = gl_FragCoord.xy / resolution;
    p = 2.0 * p - 1.0;
    p.x *= resolution.x / resolution.y;
    float col1 = sdHex(p * 10.0*sin(time * 0.1));
    float col2 = smoothstep(0.01, 0.0, col1);
    float col = mix(col1, col2, sin(time) * 0.5 + 0.5);
    gl_FragColor = vec4(vec3(col), 1.0);
}
