/*{
    "DESCRIPTION": "DoomTunnel",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
            "NAME": "val_n1",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0,
            "MAX": 1
        },
        {
            "NAME": "val_n0_25",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0,
            "MAX": 1
        },
        {
            "NAME": "val_n128",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0,
            "MAX": 1
        }
    ],
    "TAGS": [
        "abstract"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

void main( void ) {
	vec2 pos = (gl_FragCoord.xy / resolution.xy);
	pos -= 0.5;

	float radius = length(pos);
	float angle = degrees(mod(pos.y, pos.x));
	float amod = 45.0 - mod(angle + 5.0 * time - val_n128 * log(radius), 45.0);
	amod /= 128.0;
	float orangle = 45.0 - mod(angle * 1.5 + 10.0 * time - 128.0 * log(radius), 45.0);
	orangle /= 128.0;
	
	float dist = sqrt(dot(pos, pos));
	float t = smoothstep(val_n0_25, 0.15, dist);
	
	gl_FragColor = vec4(amod / 3.0 + orangle - t, orangle / 4.0-t, amod - t, val_n1);
}
