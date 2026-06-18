/*{
    "DESCRIPTION": "RingTrace11",
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
// LEDs, https://twitter.com/#!/baldand/status/160081733180604417
// Originally designed to fit in a tweet with just a bit of boilerplate
#ifdef GL_ES
precision mediump float;
#endif

void main( void ) {
	vec2 v = ( gl_FragCoord.xy / resolution.xy ) ;
	float w = 0.0;
	float x = 0.0;
	float z = 0.0;
	vec2 u;
	vec3 c;
	v*=35.;
	u=floor(v)*.1+vec2(20.,11.);
	u=u*u;
	x=fract(u.x*u.y*9.1+time);
	x*=(1.-length(fract(v)-vec2(.5,.5))*(2.+x));
	c=vec3(v*x,x);
	gl_FragColor = vec4(c,1.);
}
