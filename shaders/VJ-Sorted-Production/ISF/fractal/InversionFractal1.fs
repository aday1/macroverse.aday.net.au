/*{
    "DESCRIPTION": "InversionFractal1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "fractal"
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "fractal"
    ]
}*/
#define E 2.71828182846

varying vec2 position;

uniform vec4 color;
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// MOUSE.x = INVERSSION?
// mouse.y =
// inputColour.w Stretch
// inputColour.zxy Offset

/* Animated inversion of black and white checkerboard
   by wjbgrafx
   12-2-15
*/

#ifdef GL_ES
precision highp float;
#endif

uniform vec4 inputColour;

// http://www.theorangeduck.com/page/avoiding-shader-conditionals
float xor( float a, float b ) 
{
	return mod( ( a + b ), mouse.x );
}

void main( void ) 
{
	vec2 aspRat = vec2( resolution.x / resolution.y, inputColour.w );
	vec2 curPix = gl_FragCoord.xy / resolution.xy * aspRat.xy - aspRat.xy / 2.0 ;

	// Calculate the inverted position of the current pixel, and assign its
	// color to the current pixel.
	float sqrSize = inputColour.x,
	      dblSqrSize = sqrSize * 2.0,
	      radius = mod( 1.0 - abs( 2.0 * fract( time * 0.025 ) - inputColour.y ), inputColour.z ),
	      a = 0.0,
	      // inversion center 
	      b = -0.5 + mod( mouse.y - abs( 2.0 * fract( time * 0.05 ) - 1.0 ), 1.0 ),	
	      x = curPix.x,
	      y = curPix.y;
	
	// Inversion transform
	// newX=a + (r^2*(-a + x))/((a - x)^2 + (b - y)^2)
	// newY=b + (r^2*(-b + y))/((a - x)^2 + (b - y)^2) 
			  
	vec2 invPix = vec2( 0.0, 0.0 );

	invPix.x = a + ( radius * radius * ( -a + x ) ) /
	                         ( ( a - x ) * ( a - x ) + ( b - y ) * ( b - y ) );
	                         
	invPix.y = b + ( radius * radius * ( -b + y ) ) /
	                         ( ( a - x ) * ( a - x ) + ( b - y ) * ( b - y ) );
	                         
	float clr = step( mod( invPix.x, dblSqrSize ), sqrSize );
	clr = xor( clr, step( mod( invPix.y, dblSqrSize ), sqrSize ) );	
	gl_FragColor = vec4( vec3( clr ), 1.0 );
	
}
