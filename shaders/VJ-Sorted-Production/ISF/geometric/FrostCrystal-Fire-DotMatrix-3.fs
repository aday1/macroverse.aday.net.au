/*{
    "DESCRIPTION": "FrostCrystal-Fire-DotMatrix-3",
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
        "grid",
        "geometric",
        "texture-input"
    ]
}*/#define iResolution vec3(RENDERSIZE, 1.0)
#define iGlobalTime TIME





#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
//
// Shadertoy example 4
//
// LJ - Example of revised specification using "mainImage" instead of "main"
// A fix can be made to include a main function right here, but it is included
// before compilation in "LoadShader" to be consistent with "ShaderLoader"
//
// https://www.shadertoy.com/view/ldl3W8#
//
// Voronoi - distances
//
// Created by inigo quilez - iq/2013
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.

// I've not seen anybody out there computing correct cell interior distances for Voronoi
// patterns yet. That's why they cannot shade the cell interior correctly, and why you've
// never seen cell boundaries rendered correctly. 

// However, here's how you do mathematically correct distances (note the equidistant and non
// degenerated grey isolines inside the cells) and hence edges (in yellow):

// http://www.iquilezles.org/www/articles/voronoilines/voronoilines.htm

// LJ - must avoid hash charaters in the code when using stringify. They are OK in comments
// #define ANIMATE
bool bAnimate = true;

vec2 hash2( vec2 p )
{
    // texture based white noise
    // return texture2D( iChannel0, (p+0.5)/256.0, -100.0 ).xy;
	
    // procedural white noise	
    return fract(sin(vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3))))*43758.5453);
}

vec3 voronoi( in vec2 x )
{
    vec2 n = floor(x);
    vec2 f = fract(x);

    //----------------------------------
    // first pass: regular voronoi
    //----------------------------------
	// LJ - stringify does not like the comma here !
	// Declare variables separately
	// vec2 mg, mr;
	vec2 mg;
	vec2 mr;

    float md = 8.0;

    for( int j=-1; j<=1; j++ ) {
        for( int i=-1; i<=1; i++ ) {
            vec2 g = vec2(float(i),float(j));
            vec2 o = hash2( n + g );
			// LJ avoid the hash character here
			// #ifdef ANIMATE
			if(bAnimate)
			    o = 0.5 + 0.5*sin( iGlobalTime + 6.2831*o );
			// #endif
            vec2 r = g + o - f;
            float d = dot(r,r);

            if( d<md ) {
                md = d;
                mr = r;
                mg = g;
            }
        }
	}

    //----------------------------------
    // second pass: distance to borders
    //----------------------------------
    md = 8.0;
    for( int j=-2; j<=2; j++ ) {
        for( int i=-2; i<=2; i++ ) {
            vec2 g = mg + vec2(float(i),float(j));
            vec2 o = hash2( n + g );
			// LJ avoid the hash character here
			// #ifdef ANIMATE
			if(bAnimate)
				o = 0.5 + 0.5*sin( iGlobalTime + 6.2831*o );
			// #endif
            vec2 r = g + o - f;

            if( dot(mr-r,mr-r)>0.00001 )
                md = min( md, dot( 0.5*(mr+r), normalize(r-mr) ) );
        }
	}

    return vec3( md, mr );
}

void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
    vec2 p = fragCoord.xy/iResolution.xx;

    vec3 c = voronoi( 8.0*p );

    // isolines
    vec3 col = c.x*(0.5 + 0.5*sin(64.0*c.x))*vec3(1.0);
    
    // borders	
    col = mix( vec3(1.0,0.6,0.0), col, smoothstep( 0.04, 0.07, c.x ) );
    
    // feature points
    float dd = length( c.yz );
    col = mix( vec3(1.0,0.6,0.1), col, smoothstep( 0.0, 0.12, dd) );
    col += vec3(1.0,0.6,0.1)*(1.0-smoothstep( 0.0, 0.04, dd));

    fragColor = vec4(col,1.0);
}

