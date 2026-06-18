/*{
    "DESCRIPTION": "EmberThread91",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "misc"
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
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define MOUSE 1

void colorize(in float x, in float y, out vec4 color) {
	if (int(x * 8.0) == 0) {
		color = vec4( vec3( 0.0, 0.0, 0.0 ), 1.0 );
	} else if (int(y * 8.0) == 0) {
		color = vec4( vec3( 0.0, 0.0, 0.0 ), 1.0 );
	} else {
		color = vec4( vec3( 1.0, 1.0, 1.0 ), 1.0 );
	}
}

void main( void ) {
	float yd = ((gl_FragCoord.y) - resolution.y / 2.0);
	#if MOUSE
	yd -= resolution.y * (mouse.y-0.5);
	#endif
	if (yd < 0.0) {
		yd = 0.0 - yd;
	}
	float z = 20.0 * resolution.y / yd;
	float xd = ((gl_FragCoord.x) - resolution.x / 2.0) / resolution.x;
	#if MOUSE
	xd -= 1.0 * (mouse.x-0.5);
	#endif
	xd *= z;
	
	float xx = float(mod(xd + 00.0 * time, 10.0)) / 10.0;
	float zz = float(mod( z + 00.0 * time, 10.0)) / 10.0;
	
	colorize(xx, zz, gl_FragColor);
	gl_FragColor /= z / 20.0;
}
