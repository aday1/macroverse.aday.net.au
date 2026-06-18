/*{
    "DESCRIPTION": "GridPattern-6",
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
precision mediump float;
#endif

const int count = 5;
vec2 pos[5];
float radius = 0.1;

void main (void) {
	
	/*for (int i = 0; i < count; ++i) { // normalement c'est envoyé en uniform, mais faudra / par la résolution
		float fi = float(i);
		float fc = float(count);
		pos[i] = vec2(fi / fc, .25);
	}*/
	pos[0] = vec2(.1, .1);
	pos[1] = vec2(.5, .2);
	pos[2] = vec2(.6, .4);
	pos[3] = vec2(.2, .3);
	pos[4] = vec2(.8, .2);
	
	vec2 uv = gl_FragCoord.xy / resolution.x;
	
	bool is_opaque = true;
	
	for (int i = 0; i < count; ++i) {
		if (length(pos[i] - uv) < radius) {
			is_opaque = false;
			break;
		}
	}
	if (is_opaque) {
		gl_FragColor = vec4(0.);
	} else {
		
		vec2 grid = vec2(cos(gl_FragCoord.x * sin(time)), sin(gl_FragCoord.y * cos(time))); // met l'image à la place
		gl_FragColor = vec4(grid, .5, 1.);
	}
}

