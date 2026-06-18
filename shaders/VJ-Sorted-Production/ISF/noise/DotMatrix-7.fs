/*{
    "DESCRIPTION": "DotMatrix-7",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "noise"
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
        "noise"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

vec2 hash( vec2 p )
{
	p = vec2( dot(p,vec2(127.1,311.7+ (0.4*sin(0.1*time)))), dot(p,vec2(269.5,183.3)));
 
	return -1.0 + 2.0*fract(sin(p)*1.555313+4.*sin(3456.1234546436));
}
 
float noise( in vec2 p )
{
	vec2 i = floor( p );
	vec2 f = fract( p );
	vec2 u = f*f*f*(6.0*f*f - 15.0*f + 10.0);
	return mix( mix( dot( hash( i + vec2(0.0,0.0) ), f - vec2(0.0,0.0) ), 
		         dot( hash( i + vec2(1.0,0.0) ), f - vec2(1.0,0.0) ), u.x),
	            mix( dot( hash( i + vec2(0.0,1.0) ), f - vec2(0.0,1.0) ), 
		         dot( hash( i + vec2(1.0,1.0) ), f - vec2(1.0,1.0) ), u.x), u.y);
}

void main( void ) {
 
	vec2 uv = gl_FragCoord.xy / resolution.xy + mouse / 4.0;
	uv.x *= resolution.x / resolution.y;
	vec2 p = floor(uv);
	vec2 f = fract(uv);

	float c1 = smoothstep(0.1,0.5,(noise(1.0*uv * 20.0) + 1.0) * 0.5);
	float c2 = smoothstep(0.3,0.6,(noise(1.0*uv * 15.0) + 1.0) * 0.5);
	float c3 = smoothstep(0.5,0.9,(noise(1.00*uv * 20.0) + 1.0) * 0.5);
	gl_FragColor = vec4(vec3(c1,c2,c3), 1.0);
}

