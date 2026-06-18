/*{
    "DESCRIPTION": "PlainScale1",
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
        "geometric",
        "3d",
        "texture-input"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D backbuffer;

//Constants

//> END Constants

void translate2D (inout vec2 v, vec2 d)     { v += d;}

void scale2D     (inout vec2 v, vec2 scale) { v *= scale;}

void mixColor    (inout vec3 target, vec3 color_add, inout float mixFactor) { 
			mixFactor++; 
			target += color_add;
		}

float intersectLinePlain(vec3 startVec, vec3 dirVec, vec4 plain){
	
	return ( plain.w - plain.x*startVec.x - plain.y*startVec.y - plain.z*startVec.z )
		/ ( plain.x*dirVec.x + plain.y*dirVec.y + plain.z*dirVec.z );

}

//point has to be on plain
vec2 project3D (vec3 point, vec4 plain){
	vec3 u = normalize( vec3( -plain.z ,  plain.x ,     0.0 ) );
	vec3 v = normalize( vec3(      0.0 , -plain.z , plain.x ) );
	vec3 p = plain.xyz * plain.w;
	
	return  vec2( dot( point , u ) / dot( u , u ) ,
		    	dot( point , v ) / dot( v , v ) );
}

float saw (float x) { return mod(x , 2.0 ) -1.0; }

float triangle ( float x ) {
	return abs( saw (x) ) * 0.5 - 0.5; 	

}

float sqr (float x) { return dot( x , x ); }

float amp ( float fq ) {
	
	return  1.0 /  sqrt( fq );
}

float freq (float harmonic ) {
	
	return pow( 2.0 , harmonic );
}

float func (float x ) {
	return triangle ( x );	
}

float damping ( float dist ) { 
	return 1.0 /   ( dist * 3.15 ) ; 
}

const float max_harmonics = 8.0 ;

float height ( float x , float dist) {
	
	float mx    = 0.0;
	float res   = 0.0;
	//float fq    = 1.0;
	
	for ( float h = 0.0 ; h < max_harmonics ; h++ ) {
		float fq  = freq( h  ) ;

		float  a  =   clamp( amp( fq ) /(dist * fq *0.01 ) , 0.0 , 1.0 );// * damping ( dist * fq );
		//float  a  =    amp( fq );
		      mx  += a;		
		     res  +=  0.5 * a * func( x * fq );
		
	}
	
	return res+ 0.9 ;//* mx;
}

void _userMain()
{
//Initialisation
	vec3 col = vec3(0.0);
	
	vec2 uv = ( gl_FragCoord.xy / resolution.xy );
	vec3 col_old = texture2D( backbuffer, uv ).rgb;

	vec2 xy = uv;
	
	scale2D     ( uv , vec2(  2.0 ) );
	translate2D ( uv , vec2( -1.0 ) );	
	
	vec3  up      = vec3( 0.0 , 1.0 ,  0.0 );
	vec3  right   = vec3( 1.0 , 0.0 ,  0.0 );
	vec3  forward = vec3( 0.0 , 0.0 ,  1.0 );
	vec3  eye     = vec3( 0.0 , 0.0 , -1.0 );
	float foc     = 1.0;
		
	vec3 dir      = normalize( right * uv.x + up * uv.y + forward * foc - eye );

	//pseudo-mouse-movement
	//dir.x += ( mouse.x - 0.5 )*10.0;
	//dir.y += ( mouse.y - 0.5 )*2.0;
	
	//dir = normalize( dir );

//Plain
	vec3  plainNormVec   = normalize( vec3( 0.0 , -2.5 , 1.0 )); 
	float plainDistance  = length(    plainNormVec ) -.15;	
	vec4  plain          = vec4(      plainNormVec , plainDistance );
	
	float distToPlain    = intersectLinePlain(  eye , dir , plain );

	vec3 col_Plain = vec3(1.0);//

	vec2 proj      = project3D( eye + dir * distToPlain , plain);
	col_Plain     += triangle( proj.y ) + triangle( proj.x );	

//paint on Plain
	
	float tri    = triangle( 1.0 * proj.x)  - 0.5;
	
	tri          =  0.01 / sqr(tri - proj.y );
	
	vec3 col_tri = vec3( 0.0 , tri , 0.0 );
	
	vec2 h = vec2 ( height( proj.x , distToPlain) , height( proj.y - time * 1.0, distToPlain ));
	
	//vec3 col_height = vec3 ( h.x  , h.y , 0.2 * ( h.x - h.y ) );
	//vec3 col_height = vec3 ( h.x , h.y, 0.0 );
	vec3 col_height = vec3 ( ( h.x + h.y ) * 0.5);
	
//> END paint on Plain
	
//< END plain	

	vec3 col_uv   = vec3( abs(uv.x) , abs(uv.y) , 0.0 );
	
	vec3 col_dir  = dir;

//Fog
	//float fogFactor = 1.0 / pow(2.0, distToPlain * 0.001);
	float fogFactor = clamp( 1.0 / ( distToPlain / 10.0 ) , 0.0 , 1.0 );
	
//> END Fog
	
// ColorMixing
	
	float mixFactor = 0.0;
		
	//mixColor( col , col_tri , mixFactor );
	
	mixColor( col , col_height, mixFactor );

	col /= mixFactor;

	//col *= fogFactor ;

	mixFactor = 1.0;

	gl_FragColor = vec4( col , 1.0 );

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