/*{
    "DESCRIPTION": "DotMatrix-34",
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
        }
    ],
    "TAGS": [
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

void main(  ) {
	vec2 p =(8.0*( gl_FragCoord.xy / resolution.xy )-4.0) ;
	p.x*=resolution.x/resolution.y;
	vec2 c=2.0*vec2(cos(time*0.2),sin(time*0.2));	
	vec2 d=2.0*vec2(cos(time*0.2+3.14),sin(time*0.2+3.14));		
	//mix inversions
	p=(p-c)/dot(p-c,p-c)-(p-d)/dot(p-d,p-d) ;
	//RGB
	float r= mod(exp(p.y),exp(p.y/p.x));
	float g=mod(exp(p.x),exp(dot(p,c*p)));
	float b=mod(exp(p.x)*exp(p.y),exp(dot(p,d*p)));

	gl_FragColor = vec4 (0.5*clamp(abs(vec3(r,g,b )),0.,2.) , 1.);
}
