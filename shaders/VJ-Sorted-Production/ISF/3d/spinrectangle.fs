/*{
    "DESCRIPTION": "spinrectangle",
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
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

#define PI 3.14159265359
#define SEGMENTS 10

void main( void ) {
	float scale = min(resolution.x, resolution.y);
	vec2 pos = gl_FragCoord.xy / scale;
	vec2 center = (resolution.xy/2.) / scale;
	
	vec2 dir = pos - center;
	
	float angle = atan(dir.y, dir.x) + time*0.4;
	
	if (mod(length(dir), 0.2) < 0.1) {
		angle += cos(sin(time));	
	}
	
	if (mod(angle, PI/float(SEGMENTS)) < PI/float(SEGMENTS*2)) {
		gl_FragColor = vec4(0);
	} else {
		gl_FragColor = vec4(distance(pos, center)) * vec4(sin(time*0.7)*0.5 + 0.5, sin(time*0.3)*0.5 + 0.5, sin(time*0.5)*0.5 + 0.5, 1);
	}

}
