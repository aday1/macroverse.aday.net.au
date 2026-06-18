/*{
    "DESCRIPTION": "ShapesBejewledish",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
        },
        {
            "NAME": "timeScale",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Time speed"
        },
        {
            "NAME": "mouseX",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Mouse X"
        },
        {
            "NAME": "mouseY",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Mouse Y"
        }
    ],
    "TAGS": [
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// RandomSuperellipsoidGrid.glsl 2015-11-17 by RenoM
// original:  https://www.shadertoy.com/view/4sdGzn

#ifdef GL_ES
precision mediump float;
#endif

#define PI 3.14159265
#define scale 33.

float reso = resolution.x / resolution.y;

float hash21(vec2 co)
{
    return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

vec3 hash23(vec2 co)
{
    float ret = hash21(co);
    return (vec3(ret, fract(PI * ret * ret), fract(PI * PI * ret)));
}

float isInSuperellipse(vec2 uv, vec2 o, float r, float n)
{
    float res = pow(abs((uv.x - o.x) / r), n) + pow(abs((uv.y - o.y) / r), n);
    return (res <= 1. ? 1. - res : -1.);
}

void main()
{
    vec2 uv = mouse.y*gl_FragCoord.xy / resolution.xy;
    uv.x *= reso;
    uv *= scale;
    vec2 frac = fract(uv);
    uv = floor(uv);
    vec3 col = vec3(.0);
    float time2 = floor(mouse.x*time);
    float res = isInSuperellipse(frac, vec2(.5), .5, 4. * hash21(uv * time2));
    vec3 hash = hash23(uv);
    if (res <= 1.)
	col = mod(time, 2.) == .0 ? hash * res : (vec3(1.) - hash) * res;
    gl_FragColor = vec4(col, 1.);
}

