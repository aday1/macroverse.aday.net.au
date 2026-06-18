/*{
    "DESCRIPTION": "CosmicEye-2",
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
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// by @mnstrmnch

//sign me the FUCK up ???????????????????? good shit go?? sHit?? thats ? some good????shit right????th ?? ere?????? 
//right?there ??if i do ?a? so my self ?? i say so ?? thats what im talking about right there right there (chorus: ????? ?????) 
//mMMMM???? ???? ???O0??OOOOO???Oooo??????????? ???? ?? ?? ?? ?? ?? ?? ????Good shit

#ifdef GL_ES
precision highp float;
#endif

vec3 Face( vec3 c, vec2 p )
{
	if( length( p ) < 1.0 )
	{
		if( length( p ) < 0.9 )
		{
			c = vec3( 1.0 ) - c;
			if( length( p * vec2( 1.0, 1.0 ) ) < 0.7 && length( p ) > 0.6 && p.y < -0.125 ) c = vec3( 0.0 ); // smile
			if( length( ( p - vec2( -0.35, 0.35 ) ) * vec2( 1.0, 0.5 ) ) < 0.125 ) c = vec3( 0.0 ); // left eye
			if( length( ( p - vec2( +0.35, 0.35 ) ) * vec2( 1.0, 0.5 ) ) < 0.125 ) c = vec3( 0.0 ); // right eye
		}
		else
		{
			c = vec3( 0.0 );
		}
	}
	else
	{
		c *= 0.5;
	}

	return c;
}

float PI = 3.14159265;

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.xx ) * vec2( 2.0 ) - vec2( 1.0, resolution.y / resolution.x );

	float bounce = abs( sin( time * 0.5 ) );
	float bounceY = p.y + bounce;

	vec3 color = vec3( sin( fract( bounceY  * 20.0 ) * PI ) ) * vec3( sin( bounceY  * 10.0 ), sin( bounceY  * 20.0 ), sin( bounceY  * 40.0 ) );

	vec2 f = ( p + vec2( sin( time + p.y ), sin( -bounce ) ) ) * 6.0;

	gl_FragColor = vec4( vec3( Face( color, fract( f ) * 2.0 - 1.0 ) ), 1.0 );

}
