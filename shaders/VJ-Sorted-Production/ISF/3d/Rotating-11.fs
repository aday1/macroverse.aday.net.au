/*{
    "DESCRIPTION": "Rotating-11",
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
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/
#define E 2.71828182846





#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

// inertia rotation --joltz0r

uniform vec4 inputColour;

// mouse.x
// mouse.y
// inputColour.x
// inputColour.y
// inputColour.z
// inputColour.w

float check(vec2 p, float size) {
	return mod(floor(p.x * size) + floor(p.y * size),2.0);
}

void main( void ) {

	vec2 p = ((gl_FragCoord.xy / resolution) - inputColour.w) * 2.0;
	p.x *= resolution.x/resolution.y;	

	//inertia towards the edges
	float t = sin(time - distance(p, vec2(0.0)))*2.0;
	p *= mat2(cos(t), -sin(t),
		  sin(t),  cos(t)
	);

	gl_FragColor = vec4(check(p, 3.0) * (mouse.x/length(p))*mouse.y);
}
