/*{
    "DESCRIPTION": "ColorSpinX",
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

#define PI 3.1415926
#define COLOR_STEP PI*2./3.
#define FREQ mouse.x*400.
#define ROT_SPD 4.

vec3 getColor(float angle) {
	return vec3(.5 + .5 * cos(angle + 0. * COLOR_STEP), 
		    .5 + .5 * cos(angle + 1. * COLOR_STEP), 
		    .5 + .5 * cos(angle + 2. * COLOR_STEP));
}

void main( void ) {

	vec2 uv = ( gl_FragCoord.xy / resolution.xy -vec2(.5)) *25.;
	
	uv *= distance(uv/25., 1. *vec2(cos(sin(time / 50.) * 2. * PI), sin(cos(time / 50.) * 2. * PI))) * 1. + 1.;
	
	uv.y *= resolution.y / resolution.x;
	
	vec3 color = vec3(0.);
	
	float angle = atan(uv.y, uv.x);
	
	float altangle = angle * FREQ + time * ROT_SPD;

	color = getColor(altangle) * 1.;
			 
	gl_FragColor = vec4(color, 0.5 );

}
