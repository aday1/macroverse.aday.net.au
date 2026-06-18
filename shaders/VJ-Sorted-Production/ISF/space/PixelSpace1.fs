/*{
    "DESCRIPTION": "PixelSpace1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "space"
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
        "space"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

vec2 rotate(float x, float y, float deg) {
	return vec2(x * cos(deg) - y * sin(deg), x * sin(deg) + y * cos(deg));
}

#define phase 10

void main(void) {
	vec2 position = (gl_FragCoord.xy / resolution.xy);
	vec3 color;
	
	for(int p = 0; p < phase; p++) {
		if(position.x > float(p)/float(phase) && position.x < float(p+1)/float(phase)) {
			if(sin(time) < 0.) position = rotate(position.x, position.y, time);
			else position = rotate(position.y, position.x, time/10.);
			color = vec3(tan(position.x * float(p)  + time*2.5), tan(position.y + 0.9), tan(position.x * position.y + time*5. + 0.2));	
		}
	}
	
	gl_FragColor = vec4(color, 1.0);
}
