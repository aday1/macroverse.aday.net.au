/*{
    "DESCRIPTION": "PhaseFlow29",
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

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.xy ) - 1.0;
	p.y *= resolution.y/resolution.x; 
		
	vec3 col = vec3(0);
	
	p.x = floor(p.x*20.0)/20.0;
	p.y += time*1.0*(p.x+1.03);
	p.y = floor(p.y*20.0)/20.0;
	float n1 = fract(123123.25423*sin(31231.23123*p.x*p.y)); 
	float n2 = fract(123432.42543*sin(21231.23123*p.x*p.y)); 
	float n3 = fract(123544.43242*sin(11231.23123*p.x*p.y)); 
	col = vec3(n1,n2,n3);
	gl_FragColor = vec4(col, 1.0); 
}
