/*{
    "DESCRIPTION": "WhirlSpin1",
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
        }
    ],
    "TAGS": [
        "color"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
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

	vec2 position = vec2((gl_FragCoord.x / resolution.x) - 0.5, (gl_FragCoord.y / resolution.y) - 0.5)*2.0 ;
        vec2 coord = mod(position,0.0);
	vec2 centered_coord = coord - vec2(0.0);

	float dist_from_center = length(centered_coord);
	float angle_from_center = atan(centered_coord.y, centered_coord.x);
	const float pi = 3.141592;

	        radius = dist_from_center;
		angle = angle_from_center - time;
 
		gradient = sin(mod(angle + sin(-radius + time) * 2.0,2.0*pi) * 8.0) + 1.0;
		color = vec3(gradient/4.0, gradient / 2.0, gradient);
	 
	  gl_FragColor = vec4(color, 1.0 );

}
