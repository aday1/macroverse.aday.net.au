/*{
    "DESCRIPTION": "Pulsing-2",
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
        }
    ],
    "TAGS": [
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

//outside inside

void main( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.xy );

	float color = 0.0;
	color += floor(sin( position.y * 10. ) * (2. * cos(time) * 4.)) / .01;
	color += floor(sin( position.x * 20. ) * (2. * sin(time) * 4.)) / .01;
	
	gl_FragColor = vec4( (floor(vec3( 0. - sin( color + time * 8.0 ), 0. + sin( color + time * 8.0 ), 1. + sin( color + time * 8.0 ) ) * 1.15) / 3.), 1.0 );
}
