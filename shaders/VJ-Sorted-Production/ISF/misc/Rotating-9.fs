/*{
    "DESCRIPTION": "Rotating-9",
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

#ifndef M_PI
#define M_PI 3.141592653589793
#endif

mat2 mat_rotate_xy(float a) {
	float ca = cos(a);
	float sa = sin(a);
	return mat2(ca,-sa,sa,ca);
}

void main( void ) {
	vec2 uv = ( gl_FragCoord.xy / resolution.xy );

	vec2 center = vec2(0.5, 0.5);
	uv -= center;
	uv.x *= resolution.x / resolution.y;

	uv = abs(uv);
	uv = mat_rotate_xy(M_PI / 128.0) * uv;
	uv = abs(uv);
	uv = mat_rotate_xy(M_PI / 64.0) * uv;
	uv = abs(uv);
	uv = mat_rotate_xy(M_PI / 3.0) * uv;
	uv = abs(uv);
	uv = mat_rotate_xy(M_PI / 64.0) * uv;
	uv = abs(uv);
	uv = mat_rotate_xy(M_PI / 128.0) * uv;
	uv = abs(uv);

	float d = length(uv);
	uv += 1.0 / (1.0 + d*4.0);
	
	uv = mat_rotate_xy(time*0.1) * uv;
	//uv.x = abs(uv.x);
	
	uv += center;
		
	uv = mix(uv, step(vec2(1.0), mod(uv * 32.0, vec2(2.0))), 1.0);
	
	uv = vec2(mod(uv.x + uv.y,2.0));
	
	gl_FragColor = vec4( vec3(uv.x), 1.0 );

}
