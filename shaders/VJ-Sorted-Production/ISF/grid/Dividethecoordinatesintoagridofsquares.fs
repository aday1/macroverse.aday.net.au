/*{
    "DESCRIPTION": "Dividethecoordinatesintoagridofsquares",
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
#ifdef GL_ES
precision highp float;
#endif

float rand(vec2 co){
  return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453);
}

void main (void) {
	// Divide the coordinates into a grid of squares
	vec2 v = gl_FragCoord.xy  / 10.0;
	// Calculate a pseudo-random brightness value for each square
	float brightness = fract(rand(floor(vec2(v.x+floor(sin(time*2.)*5.),v.y+floor(sin(time*0.7)*15.)))) + time*1.1);
	// Reduce brightness in pixels away from the square center
	//brightness *= 0.5 - length(fract(v) - vec2(0.5, 0.5));
	brightness *= pow(1. - length((gl_FragCoord.xy / resolution) - vec2(0.50, 0.50)), 3.);
	vec4 otr = vec4(brightness * v.x / resolution.x * 100., brightness * v.y / resolution.y * 100. * (cos(time*4.123)+1.), brightness * v.y / resolution.y * 100., 1.0);
	otr *= vec4(1.12, 0.98, 0.87, 1);
	//otr = 1.0 - otr;
	gl_FragColor = otr;
}
