/*{
    "DESCRIPTION": "CheckerZoomer1-XY",
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
uniform float brightness;
uniform float speed;
#define E 2.71828182846
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
//http://glslsandbox.com/e#24233.0
// Checkerboard tunnel

#ifdef GL_ES
precision mediump float;
#endif

// shorter version --novalis 
// add new stuff -- Gigatron

uniform vec4 inputColour;

float rot=0.00; // rot/speed -;0 fixed ;+

void main(void) {
	vec2 p = gl_FragCoord.xy/resolution.xx*2.-vec2(1.,.5);
	
	float dir=inputColour.x;  // dir/speed - back ;0 stay; + forward
	float brightness=1.0;
	
	if (mouse.x>gl_FragCoord.x/resolution.x*mouse.x)
	{
		rot=mouse.x;
	}
	else
	{
		rot=-mouse.y;
	}
	
	gl_FragColor = length(p)*vec4(vec3((mod(inputColour.y/length(p)+time*dir,.1)>.02)^^(mod(atan(p.x/p.y)*7./44.+time*rot,inputColour.w)>inputColour.z)),1.)*brightness;
}
