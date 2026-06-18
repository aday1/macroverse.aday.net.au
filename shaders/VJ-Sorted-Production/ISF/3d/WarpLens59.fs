/*{
    "DESCRIPTION": "WarpLens59",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "3d"
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
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
//check board by uggway

// yay faux perspective -jz
#ifdef GL_ES
precision mediump float;
#endif

varying vec2 surfacePosition;
//#define CM 

vec3 check(vec2 p, float y, float s)
{
	float c = clamp(floor(mod(p.x/s+floor(p.y/s),2.0))*s,0.1,0.9)*2.0;
	c *= c;
	return vec3(c);
}

void main( void ) {

	vec2 p = -1.0 + 2.0 * ( gl_FragCoord.xy/ resolution.xy  );
	p.x *=  resolution.x/resolution.y;

	vec3 col = vec3(1.0);
	
	float y = p.y + (p.y + (cos((cos(time*0.2+p.y)-time+p.x))*0.5));// + sin(p.x*20.)*0.05;
	vec2 uv;
	uv.x = p.x/y;
	uv.y = 1.0/abs(y)+time/3.0;
	col = check(uv, y, 0.50)*length(y);
	float t = pow(abs(y),0.0);

	gl_FragColor = vec4( col*t, 1.0 );

}
