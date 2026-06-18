/*{
    "DESCRIPTION": "GRID-BUILDER-MODE7EMULATOR-XYZ",
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "grid"
    ]
}*/
#define E 2.71828182846
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

uniform vec4 inputColour;

vec2 rotate(float x, float y, float deg) {
	return vec2(x * cos(deg) - y * sin(deg), x * sin(deg) + y * cos(deg));
}

void main( void ) {
	vec2 pos = (gl_FragCoord.xy / resolution.xy) - vec2(.5, .5);
	vec2 s = rotate(pos.x / pos.y * inputColour.x, .05 / pos.y, time / mouse.x);
	gl_FragColor = vec4(vec3(sin(time) * pos.y * pos.y * mouse.y, pos.y < inputColour.y ? sign((mod(s.x, .1) - mouse.y) * (mod(s.y, inputColour.z) - inputColour.w)) * pos.y * pos.y * 20. : sin(time) * pos.y * pos.y * inputColour.z, sin(time) * pos.y * pos.y * inputColour.x), inputColour.y);
}
