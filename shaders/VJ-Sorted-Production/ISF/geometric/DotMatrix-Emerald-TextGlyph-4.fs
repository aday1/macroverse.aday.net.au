/*{
    "DESCRIPTION": "DotMatrix-Emerald-TextGlyph-4",
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
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float rand(vec2 co){
    return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

float char(vec2 outer, vec2 inner) {
	//return float(rand(floor(inner * 2.0) + outer) > 0.9);
	
	vec2 seed = floor(inner * 4.0) + outer.y;
	if (rand(vec2(outer.y, 23.0)) > 0.98) {
		seed += floor((time + rand(vec2(outer.y, 41.0))) * 3.0);
	}
	
	return float(rand(seed) > .4);
}

void main( void ) {

	float angle = 0.0;
	float px = gl_FragCoord.x;
	float py = gl_FragCoord.y;
	
	float cosa = cos(-angle);
	float sina = sin(-angle);

		px = (px * cosa) + (py * sina);
		py = (py * cosa) - (px * sina);	

	//vec2 p = (( gl_FragCoord.xy / resolution.xy )) * 500.0;
	
	float rx = px ;
	float ry = py ;
	
	float mx = mod(rx, 10.0);
	
	if (mx > 7.0) {
		gl_FragColor = vec4(0);
	} else {
        	float x = floor(rx);
		float ry = ry + rand(vec2(x, x * 13.0)) * 100000.0 + time * rand(vec2(x, 23.0)) * 120.0;
		float my = mod(ry, 15.0);
		if (my > 12.0) {
			gl_FragColor = vec4(0);
		} else {
		
			float y = floor(ry / 15.0);
			
			float b = char(vec2(rx, floor((ry) / 15.0)), vec2(mx, my) / 12.0);
			float col = max(mod(-y, 24.0) - 4.0, 0.0) / 20.0;
			vec3 c = col < 0.8 ? vec3(0.0, col / 0.8, 0.0) : mix(vec3(0.0, 1.0, 0.0), vec3(1.0), (col - 0.8) / 0.2);
			
			gl_FragColor = vec4(c * b, 1.0);
		}
	}
}
