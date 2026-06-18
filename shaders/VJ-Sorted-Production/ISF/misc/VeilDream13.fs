/*{
    "DESCRIPTION": "VeilDream13",
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
//check board by uggway
#ifdef GL_ES
precision mediump float;
#endif

//#define CM 

vec3 check(vec2 p, float s)
{
	#ifdef CM
	return  vec3(clamp(ceil(sin(p.x/s)*sin(p.y/s))*s,0.1,0.9));
	#else
	return vec3(clamp(floor(mod(p.x/s+floor(p.y/s),2.0))*s,0.1,0.9));
	#endif
}

void main( void ) {

	vec2 p = -1.0 + 2.0 * ( gl_FragCoord.xy/ resolution.xy  );
	p.x *=  resolution.x/resolution.y;

	vec3 col = vec3(1.0);
	
	float y = p.y + sin(p.x);// + sin(p.x*20.)*0.05;
	vec2 uv;
	uv.x = p.x/y;
	uv.y = 1.0/abs(y)+time/5.0;
	col = check(uv, 0.3);
	float t = pow(abs(y),2.0);

	gl_FragColor = vec4( col*t, 1.0 );

}
