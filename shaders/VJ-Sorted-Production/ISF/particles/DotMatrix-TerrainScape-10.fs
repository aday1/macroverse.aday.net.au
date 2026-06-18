/*{
    "DESCRIPTION": "DotMatrix-TerrainScape-10",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "particles"
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
        "particles"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif
 
#extension GL_OES_standard_derivatives : enable
//anashtashia dunbaii

float random (in float x) {
    return fract(sin(x)*1e4);
}
float smooth_curve(float x, float a) {
    float b = pow(x*2.,a)/2.;
    if (x > .5) {
        b = 1.-pow(2.-(x*2.),a)/2.;
    }
    return b;
}
float smooth_mix(float a,float b,float c) { return mix(a,b,smooth_curve(c,2.)); }
float random(vec2 co, float shft){
    co += 10.;
    return smooth_mix(fract(sin(dot(co.xy ,vec2(12.9898+(floor(shft)*.5),78.233))) * 43758.5453),fract(sin(dot(co.xy ,vec2(12.9898+(floor(shft+1.)*.5),78.233))) * 43758.5453),fract(shft));
}
float smooth_random(vec2 co, float shft) {
	return smooth_mix(smooth_mix(random(floor(co),shft),random(floor(co+vec2(1.,0.)),shft),fract(co.x)),smooth_mix(random(floor(co+vec2(0.,1.)),shft),random(floor(co+vec2(1.,1.)),shft),fract(co.x)),fract(co.y));
}
float terrain_noise(vec2 p,float shft) {
	const int loops = 11;
	float a = 0.;
	float s = 0.;
	for (int i = 0; i < loops; i++) {
		float fi = float(i);
		a += smooth_random((p*(fi+1.))+vec2(random(fi),random(fi+16.245)),fi+shft)*(1./(fi+1.)); //Just because of this ? (1/(n+1)) ? I found out about Harmonic number.
		s += (1./(fi+1.));
	}
	return a/s;
}
void main( void ) {
 
	vec2 uv = ( gl_FragCoord.xy / resolution.xy ) ;
	gl_FragColor = vec4(vec3(smooth_random(uv*20.,0.),smooth_random(uv*20.,1.),smooth_random(uv*20.,2.)), 1.0 );
}

