/*{
    "DESCRIPTION": "Rotating-4",
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

#extension GL_OES_standard_derivatives : enable

void main( void ) {

	vec2 pos = ( gl_FragCoord.xy - resolution.xy*.5 ) / resolution.y*3.;
	
	pos *= mat2(cos(time),sin(time),-sin(time),cos(time));
	
	pos.x *= 1.3;
	pos.y /= 1.3;
	
	pos *= mat2(cos(time),-sin(time),sin(time),cos(time));

	if (length(pos) > 1.) {
		gl_FragColor = vec4(0);
		return;
	}

	float color = 0.0;
	color += sin( pos.x*pos.y *10.);
	color += sin( pos.x *10.);
	color += sin( pos.y*13. + color*3. );

	gl_FragColor = vec4( vec3( .5, color * 0.5, sin( color + time / 3.0 ) * 0.75 ), 1.0 );

}
