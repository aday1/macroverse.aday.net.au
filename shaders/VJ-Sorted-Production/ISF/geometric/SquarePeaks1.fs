/*{
    "DESCRIPTION": "SquarePeaks1",
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

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / (resolution.xy) * 2. );
	vec3 result;
	float x_pos_in_hex = floor(p.x * 8.) / 2.;
	vec3 white = vec3((1. + sin(time + x_pos_in_hex)) / 2.,
			  (1. + asin(time + x_pos_in_hex)) / 2.,
			  (1. + cos(time + x_pos_in_hex)) / 2.);
	float xPosition_16 = p.x / 16.;
	float restraint = (1. + cos(time + x_pos_in_hex)) / 2.;
	restraint *= 2.;
	if (p.y < restraint)
	{
		result = white;
	}
	
	gl_FragColor = vec4( result, 1.0 );

}
