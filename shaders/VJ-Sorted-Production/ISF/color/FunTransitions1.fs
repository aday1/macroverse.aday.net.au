/*{
    "DESCRIPTION": "FunTransitions1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "color"
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
            "NAME": "val_n2_1",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0,
            "MAX": 1
        },
        {
            "NAME": "val_n8",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0,
            "MAX": 1
        },
        {
            "NAME": "val_n2",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0,
            "MAX": 1
        },
        {
            "NAME": "val_n0_5",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0,
            "MAX": 1
        }
    ],
    "TAGS": [
        "color"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

// from optimus multiple fx ; 
	float gradient = 0.0;
	vec3 color = vec3(0.0);
 
	float angle = 0.0;
	float radius = 0.0;

void main( void ) {

	vec2 position = vec2((gl_FragCoord.x / resolution.x) - val_n0_5, (gl_FragCoord.y / resolution.y) - 0.5)*2.0 ;
        vec2 coord = mod(position,0.0);
	vec2 centered_coord = coord - vec2(0.0);

	float dist_from_center = length(centered_coord);
	float angle_from_center = atan(centered_coord.y, centered_coord.x);
	const float pi = 3.141592;

	        radius = dist_from_center;
		angle = angle_from_center - time;
 
		gradient = sin(mod(angle + sin(-radius + time) * val_n2,val_n2_1*pi) * val_n8) + 1.0;
		color = vec3(gradient/4.0, gradient / 2.0, gradient);
	 
	  gl_FragColor = vec4(color, 1.0 );

}
