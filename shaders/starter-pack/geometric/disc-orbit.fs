/*{
    "DESCRIPTION": "disc orbit",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": ["Misc"],
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
    ]
}*/

#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

// tried to model lorentz transform

float color(vec2 pos, float _time) {
	float r = length(pos);
	float w = atan(pos.y,pos.x);
	w = (fract(w/3.14159265*16.+.5)-.5)*r/16.*3.14159265;
	r = fract(r-_time)-.5;
	
	return step(w*w+r*r,mouse.x);
}

void main( void ) {

	vec2 p = ( gl_FragCoord.xy - resolution.xy*.5 )/resolution.x * 40.;

	float a = 0.;//(mouse.x*2.-1.)*.99;

	p.x -= floor(a*time*.05+.5)*20.;

	vec2 l = vec2(p.x,time);
	l *= mat2(1,a,a,1)/sqrt(1.-a*a);
	p.x = l.x;
	
	gl_FragColor = vec4(mouse.y > .5 ? color(p,l.y) : 1.-color(p,l.y));
	
}
