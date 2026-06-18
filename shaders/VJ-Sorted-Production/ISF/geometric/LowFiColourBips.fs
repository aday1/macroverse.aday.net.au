/*{
    "DESCRIPTION": "LowFiColourBips",
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "geometric"
    ]
}*/
uniform float brightness;
#define E 2.71828182846




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

uniform vec4 inputColour;

// mouse.x
// mouse.y
// inputColour.x
// inputColour.y
// inputColour.z
// inputColour.w

float rand(vec2 co){
  return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453);
}

void main (void) {
	// Divide the coordinates into a grid of squares
	vec2 v = gl_FragCoord.xy  / 10.0;
	// Calculate a pseudo-random brightness value for each square
	float brightness = fract(rand(floor(vec2(v.x+floor(sin(time*2.)*5.),v.y+floor(sin(time*inputColour.y)*15.)))) + time*1.1);
	// Reduce brightness in pixels away from the square center
	//brightness *= 0.5 - length(fract(v) - vec2(0.5, 0.5));
	brightness *= pow(1. - length((gl_FragCoord.xy / resolution) - vec2(mouse.x, 0.50)), 3.);
	vec4 otr = vec4(brightness * v.x / resolution.x * 100., brightness * v.y / resolution.y * 100. * (cos(time*4.123)+1.), brightness * v.y / resolution.y * 100., mouse.y);
	otr *= vec4(inputColour.w, inputColour.z, inputColour.x, 1);
	//otr = 1.0 - otr;
	gl_FragColor = otr;
}
