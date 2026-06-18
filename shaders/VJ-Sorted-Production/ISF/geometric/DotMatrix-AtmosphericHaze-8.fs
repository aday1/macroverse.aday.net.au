/*{
    "DESCRIPTION": "DotMatrix-AtmosphericHaze-8",
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

bool checker( vec2 uv )
{
	return( mod( uv.x, 0.5 ) < 0.25 ) ^^
	      ( mod( uv.y, 0.5 ) < 0.25 ) ;
}

vec3 checkerBoard( vec2 uv ) 
{
	//uv += time * .01;
	uv = mod( uv, 3. ) - 1.5;
	float L = length(uv);
	vec3 c1 = vec3( .3, .6, .9 );
	vec3 c2 = vec3( .6, .9, .6 );
	vec3 c = mix( c1, c2, L );
	if( checker( uv ) ) c *= .5/L;
	return c;
}

float det(mat2 mat)
{
	return (mat[0][0]*mat[1][1])-(mat[0][1]*mat[1][0]);
}

vec2 lineXline(vec2 p11,vec2 p12,vec2 p21,vec2 p22)
{	
	vec2 n0 = (p12-p11).yx*vec2(-1,1);
	vec2 n1 = (p22-p21).yx*vec2(-1,1);
	
	float d0 = dot(n0,p11);
	float d1 = dot(n1,p21);

	mat2 d = mat2(n0.x,n0.y,
		      n1.x,n1.y);
	
	mat2 x = mat2(d0,n0.y,
	              d1,n1.y);
	
	mat2 y = mat2(n0.x,d0,
		      n1.x,d1);
	
	return vec2(det(x)/det(d),det(y)/det(d));	
}

vec3 lineXplane( vec3 a1, vec3 a2, vec3 pO, vec3 pN ) // I have no idea what I'm doing! o_O
{
	a1 -= pO;
	a2 -= pO;
	vec3 aV = normalize( a2-a1 );
	
	vec2 yz = lineXline( a1.zy, a2.zy, vec2(0.), vec2( -1., 0. ) );
	return distance( yz, a1.yz ) * aV + a1 + pO;
}

float cloud( vec2 a, float s, vec2 p )
{
	return smoothstep( s,s-.3, distance( a,p ) );
}

void main( void ) {
	vec2 p = gl_FragCoord.xy / resolution.xy * 2. - 1.;
	p.x *= resolution.x/resolution.y;
	
	vec3 ro = vec3( 10. * mouse.x, 5., 10. * mouse.y );
	vec3 rd = normalize( vec3(p, 1.) ) + ro;
	
	vec2 uv = ( lineXplane( ro, rd, vec3(0.), vec3(0.,1.,0.) )  ).xz;
	
	if( p.y < 0. )
	gl_FragColor = vec4(  .1/checkerBoard(uv), 1. );
	else
	gl_FragColor = vec4( 0., .1/p.y, .5/p.y, 1. );
	
	gl_FragColor += cloud( vec2( .2, .5 ), .32, p );
	gl_FragColor += cloud( vec2( .6, .6 ), .5, p );
	gl_FragColor += cloud( vec2( 1., .55 ), .38, p );
	gl_FragColor += cloud( vec2( -.65, .35 ), .2, p );
	gl_FragColor += cloud( vec2( -.9, .45 ), .35, p );
	gl_FragColor += cloud( vec2( -1.3, .5 ), .4, p );
	gl_FragColor += cloud( vec2( -1.7, .55 ), .45, p );
}
