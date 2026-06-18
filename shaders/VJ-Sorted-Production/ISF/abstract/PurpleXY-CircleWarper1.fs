/*{
    "DESCRIPTION": "PurpleXY-CircleWarper1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        "abstract"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

void main( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.xy );
	
	position.x = position.x * 1.7;
	
	vec2 mouse2 = mouse;
	
	mouse2.x = mouse.x * 1.7;
	
	float radius = 0.25*time;
	
	float posDist = distance(position, mouse2);
	
	float dist2 = 20.0*abs(radius - posDist);
	
	float distort = 1.0-(sin(dist2)+1.0)/2.0;
	
	vec4 color = vec4(distort, 0, distort, 1);
	
	gl_FragColor = color;
}
