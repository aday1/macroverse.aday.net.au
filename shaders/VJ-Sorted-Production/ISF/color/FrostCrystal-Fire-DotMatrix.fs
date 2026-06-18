/*{
    "DESCRIPTION": "FrostCrystal-Fire-DotMatrix",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "color"
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
        "color"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

// Created by inigo quilez - iq/2014
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.

#define _ 0.
#define R 3.
#define G 1.
#define Y 2.

#define DD(id,a,b,c,d,e,f,g,h,i,j,k,l) if(y==id)m=(a+4.*(b+4.*(c+4.*(d+4.*(e+4.*(f+4.*(g+4.*(h+4.*(i+4.*(j+4.*(k+4.*(l))))))))))));

vec3 mario( in vec3 col, in vec2 p ) 
{
	float x =      floor( p.x*10.0+5.0 );
	int   y = int( floor( p.y*10.0+7.0 ));

	float m = 0.0;

	DD( 14, _,_,_,_,_,_,_,_,_,_,_,_)
	DD( 13, _,_,_,_,_,_,_,_,_,_,_,_)
	DD( 12, _,_,_,_,_,_,_,_,_,_,_,_)
	DD( 11, _,_,_,_,_,_,_,_,_,_,_,_)
	DD( 10, _,_,_,_,_,_,_,_,_,_,_,_)
	DD( 9, _,_,_,_,_,_,_,_,_,_,_,_)
	DD( 8, _,_,_,R,R,_,_,R,R,_,_,_)
	DD( 7, _,_,_,_,R,_,_,R,_,_,_,_)
	DD( 6, _,_,Y,Y,Y,Y,Y,Y,Y,Y,_,_)
	DD( 5, _,Y,Y,R,R,Y,Y,R,R,Y,Y,_)
	DD( 4, Y,Y,Y,Y,Y,Y,Y,Y,Y,Y,Y,Y)
	DD( 3, R,_,R,R,R,R,R,R,R,R,_,R)	
	DD( 2, R,_,R,R,R,R,R,R,R,R,_,R)
	DD( 1, R,_,R,_,_,_,_,_,_,R,_,R)
	DD( 0, _,_,_,R,R,_,_,R,R,_,_,_)

	float c = mod(floor(m/pow(4.,x)),4.);
	
	if( c>0.5 ) col = vec3(0.3,0.4,0.1);
	if( c>1.5 ) col = vec3(1.0,0.6,0.0);
	if( c>2.5 ) col = vec3(1.0,0.0,0.0);
	
	// border
	float f = step(0.5,c); col += 0.3*(dFdx(f) - dFdy(f));
	
	return col;
}

void _userMain( void ) {

	vec2 p = (-resolution.xy+2.0*gl_FragCoord.xy)/resolution.y;

    // background	
	vec2 q = vec2( atan(p.y,p.x), length(p) );
	float f = smoothstep( -0.1, 0.1, sin(q.x*10.0 + time) );
	vec3 col = mix( vec3(0.42,0.55,1.0), vec3(0.6,0.7,1.0), f );
	
	// soft shadow
	float sha = 0.0;
	for( int j=0; j<5; j++ )
	for(int i=0; i<5; i++ )
	{		
		vec3 s = mario( vec3(0.0), p + 10.0*vec2(float(i)-4.0,float(j)+1.0)/resolution.y );
		sha += step(0.1,p.x);
    }			

	// color
	col = mario( col, p+vec2(0.0+sin(time*3.0),-abs(sin(time))));

    // vigneting	
	col *= 1.0 - 0.2*length(p);

    // fade in/out	
	col *=       smoothstep(  0.0,  2.0, time );
       col *= 1.0 - smoothstep( 55.0, 60.0, time );

	gl_FragColor = vec4(  col , 1.0 );

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