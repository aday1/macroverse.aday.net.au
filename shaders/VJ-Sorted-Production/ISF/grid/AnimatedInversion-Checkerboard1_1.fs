/*{
    "DESCRIPTION": "AnimatedInversion-Checkerboard1",
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
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Speed"
        },
        {
            "NAME": "mouseX",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse X"
        },
        {
            "NAME": "mouseY",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse Y"
        },
        {
            "NAME": "zoom",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Zoom"
        },
        {
            "NAME": "colorR",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Red"
        },
        {
            "NAME": "colorG",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Green"
        },
        {
            "NAME": "colorB",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Blue"
        },
        {
            "NAME": "brightness",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Brightness"
        },
        {
            "NAME": "saturation",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Saturation"
        },
        {
            "NAME": "contrast",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Contrast"
        },
        {
            "NAME": "hueShift",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Hue Shift"
        },
        {
            "NAME": "invert",
            "TYPE": "bool",
            "DEFAULT": 0,
            "LABEL": "Invert Colors"
        }
    ],
    "TAGS": [
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
/* Animated inversion of black and white checkerboard
   by wjbgrafx
   12-2-15
*/

#ifdef GL_ES
precision highp float;
#endif

// http://www.theorangeduck.com/page/avoiding-shader-conditionals
float xor( float a, float b ) 
{
	return mod( ( a + b ), 1.5 );
}

void _userMain( void ) 
{
	vec2 aspRat = vec2( resolution.x / resolution.y, 1.0 );
	vec2 curPix = gl_FragCoord.xy / resolution.xy * aspRat.xy - aspRat.xy / 2.0 ;

	// Calculate the inverted position of the current pixel, and assign its
	// color to the current pixel.
	float sqrSize = 0.1,
	      dblSqrSize = sqrSize * 2.0,
	      radius = mod( 1.0 - abs( 2.0 * fract( time * 0.025 ) - 1.0 ), 1.0 ),
	      a = 0.0,
	      // inversion center 
	      b = -0.5 + mod( 1.0 - abs( 2.0 * fract( time * 0.05 ) - 1.0 ), 1.0 ),	
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

void main() {
    _userMain();
    vec3 c = gl_FragColor.rgb;
    float a = gl_FragColor.a;
    float luma = dot(c, vec3(0.299, 0.587, 0.114));
    c = mix(vec3(luma), c, saturation);
    c = (c - 0.5) * contrast + 0.5;
    c *= vec3(colorR, colorG, colorB);
    c += brightness;
    if (hueShift > 0.001) {
        float cosH = cos(hueShift * 6.28318);
        float sinH = sin(hueShift * 6.28318);
        c = vec3(
            c.r * (0.299 + 0.701*cosH + 0.168*sinH) + c.g * (0.587 - 0.587*cosH + 0.330*sinH) + c.b * (0.114 - 0.114*cosH - 0.497*sinH),
            c.r * (0.299 - 0.299*cosH - 0.328*sinH) + c.g * (0.587 + 0.413*cosH + 0.035*sinH) + c.b * (0.114 - 0.114*cosH + 0.292*sinH),
            c.r * (0.299 - 0.300*cosH + 1.250*sinH) + c.g * (0.587 - 0.588*cosH - 1.050*sinH) + c.b * (0.114 + 0.886*cosH - 0.203*sinH)
        );
    }
    if (invert) c = 1.0 - c;
    gl_FragColor = vec4(clamp(c, 0.0, 1.0), a);
}