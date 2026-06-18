/*{
    "DESCRIPTION": "CrazyPortal",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
        },
        {
            "NAME": "timeScale",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Time speed"
        },
        {
            "NAME": "mouseX",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Mouse X"
        },
        {
            "NAME": "mouseY",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Mouse Y"
        }
    ],
    "TAGS": [
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// Crazy Portal
// By: Brandon Fogerty
// bfogerty at gmail dot com
// xdpixel.com

#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

// I didn't write this random function but I don't know who did... so thanks to whoever wrote this!
vec2 random2f(vec2 p) 
{
     p = mod( p, 4.0 );
	
     vec2 tmp = fract(vec2(sin(p.x * 591.32 + p.y * 154.077), cos(p.x * 391.32 + p.y * 49.077)));
	
     return vec2(.5+.5*sin(tmp.x*time+ p.y),.5+.5*cos(tmp.y*time + p.x));
}

float voronoi( vec2 uv )
{
	
	uv.x = mod(uv.x,4.0);
	
	vec2 p = floor( uv );
	vec2 f = fract( uv );
	
	float res = 8.0;
	const float i = 1.0;
	
	for( float y = -i; y <= i; ++y )
	{
		for( float x = -i; x <= i; ++x )
		{
			vec2 b = vec2( x, y );
			vec2 r = b - f + random2f( p + b );
			float d = length( r );
			
			res = min( res, d );
		}
	}
	
	return res;
}

void main( void ) 
{

	vec2 uv = ( gl_FragCoord.xy / resolution.xy ) * 2.0 - 1.0;
	uv.x *= resolution.x / resolution.y;

	vec2 originalUV = uv;
	
	float a = 2.0 * atan( uv.x, uv.y );
    	a *= (3.0 / 3.141596);
	
	float r = length( uv ) * 0.5;
	
	float t = sin( time * 0.2 ) * 0.5 + 0.5;
	
	float z = mix( 2.5, 3.5, t );
	uv = vec2( r * z, a );
	
	vec3 finalColor = vec3(0.0);
	
	finalColor += vec3( voronoi(uv * 4.0), voronoi(uv * 3.0), voronoi(uv * 7.0) );
	
	finalColor -= length(originalUV) * 0.3;
	
	finalColor *= 1.0-length(originalUV);
	
	gl_FragColor = vec4( finalColor, 1.0 );

}
