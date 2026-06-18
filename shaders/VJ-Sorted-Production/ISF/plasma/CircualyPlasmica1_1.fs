/*{
    "DESCRIPTION": "CircualyPlasmica1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "plasma"
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
        "plasma"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_FRAGMENT_PRECISION_HIGH
#ifdef GL_ES
	precision highp float;
#endif
#else
#ifdef GL_ES
	precision mediump float;
#endif
#endif

varying vec2 surfacePosition;
#define PI 3.14159

void main( void ) {
	vec2 pos = surfacePosition;
	float three = 1.0 - ((pos.x + pos.y) * sin(time*0.5)-2.0);
	vec3 color = vec3(1.0,-1.0,0.0);
	color -= vec3(three, pos.x, pos.y);
	
	float angle = atan(pos.x,pos.y);
	float radius = length(pos);
	float amplitude = 0.6;
	for (int i=0; i<4; i++) {
		color *=  vec3(abs(1.0+float(i) / (sin( pos.y
					      +sin( pos.x + time + sin(angle*6.) + float(i)*PI/2. ) * amplitude
					     ) * sin(radius*33.)
					 ) / 9.
				  )
			      );
	}
	color /= 30.;

	gl_FragColor = vec4(0.1-color, 1.0 );
}
