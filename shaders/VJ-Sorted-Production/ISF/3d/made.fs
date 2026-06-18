/*{
    "DESCRIPTION": "made",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "3d"
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
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// made by darkstalker
#ifdef GL_ES
precision mediump float;
#endif

#define M_PI 3.14159265358979323846

#define calcRotationMat2(ang)  mat2(cos(ang), -sin(ang), sin(ang), cos(ang))

mat2 rotMatrix = calcRotationMat2(M_PI*0.25);

void main(void)
{
	vec2 screen_pos = gl_FragCoord.xy;
	vec2 mouse_pos = mouse*resolution;

	vec2 p = rotMatrix*screen_pos * 0.1;
	float value = clamp((cos(p.x) + cos(p.y)) * 10., .1, 1.);
	float light = 0.08 + clamp(1. - distance(screen_pos, mouse_pos) / 150., 0., 1.);

	gl_FragColor = vec4(vec3(value*light), 1.);
}
