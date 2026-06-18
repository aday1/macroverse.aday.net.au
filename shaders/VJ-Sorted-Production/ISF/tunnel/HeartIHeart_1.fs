/*{
    "DESCRIPTION": "HeartIHeart",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "tunnel"
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
        "tunnel"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

// * You feel like you're going to have a bad time..

// Created by inigo quilez - iq/2014
// Updated with love <3
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.

#define _ 0.
#define R 3.
#define G 1.
#define Y 2.

#define PI 3.14159265359

#define DD(id,a,b,c,d,e,f,g,h,i,j,k,l) if(y==id)m=(a+4.*(b+4.*(c+4.*(d+4.*(e+4.*(f+4.*(g+4.*(h+4.*(i+4.*(j+4.*(k+4.*(l))))))))))));

vec3 heart( in vec3 col, in vec2 p ) 
{
	float x =     floor(p.x*10.0+5.5);
	int   y = int( floor( p.y*10.0+6.1 ));

	float m = 0.0;
	float taym = sin(mod(time*1.8, 3.));
	float Z = R;

DD( 14, _,_,_,_,_,_,_,_,_,_,_,_)
DD( 13, _,_,_,_,_,_,_,_,_,_,_,_)
DD( 12, _,_,_,_,_,_,_,_,_,_,_,_)
DD( 11, _,_,R,R,_,_,_,R,R,_,_,_)
DD( 10, _,R,G,G,R,_,R,G,G,R,_,_)
DD( 9,  R,G,Y,Y,G,R,G,Y,Y,G,R,_)
DD( 8,  R,G,Y,Z,G,G,G,Z,Y,G,R,_)
DD( 7,  R,G,G,Z,Z,Z,Z,Z,G,G,R,_)
DD( 6,  _,R,G,G,Z,Z,Z,G,G,R,_,_)
DD( 5,  _,_,R,G,Y,Z,Y,G,R,_,_,_)
DD( 4,  _,_,_,R,G,G,G,R,_,_,_,_)
DD( 3,  _,_,_,_,R,G,R,_,_,_,_,_)
DD( 2,  _,_,_,_,_,R,_,_,_,_,_,_)
DD( 1,  _,_,_,_,_,_,_,_,_,_,_,_)
DD( 0,  _,_,_,_,_,_,_,_,_,_,_,_)

	float c = mod(floor(m/pow(4.,x)),4.);
	
	if( c>0.5 ) col = vec3(1.,.2,.2); // g
	if( c>1.5 ) col = vec3(1.,.3,.2); // y
	if( c>2.5 ) col = vec3(1.,.0,.0); // r
	//if( c>0. ) col = vec3(1.,1.,1.); // z
	
	// border
	float f = step(0.2,c); 
	col += floor(f*taym*3.) *.095;
	
	return col;
}

vec3 bone( in vec3 col, in vec2 p ) 
{
	float x =     floor(p.x*10.0+5.5);
	int   y = int( floor( p.y*10.0+6.1 ));

	float m = 0.0;
	float taym = sin(mod(time*1.8, 3.));
	float Z = R;

DD( 14, _,_,_,_,_,_,_,_,_,_,_,_)
DD( 13, _,_,_,_,_,_,_,_,_,_,_,_)
DD( 12, _,_,_,_,_,_,_,_,_,_,_,_)
DD( 11, _,_,_,_,_,_,_,_,_,_,_,_)
DD( 10, _,_,_,_,_,_,_,_,_,_,_,_)
DD( 9,  _,_,_,_,_,_,_,_,_,_,_,_)
DD( 8,  _,_,_,Y,Y,_,Y,Y,_,_,_,_)
DD( 7,  _,_,Y,R,R,Y,R,R,Y,_,_,_)
DD( 6,  _,_,_,Y,R,R,R,Y,_,_,_,_)
DD( 5,  _,_,_,_,Y,R,Y,_,_,_,_,_)
DD( 4,  _,_,_,_,Y,R,Y,_,_,_,_,_)
DD( 3,  _,_,_,_,Y,R,Y,_,_,_,_,_)
DD( 2,  _,_,_,Y,R,R,R,Y,_,_,_,_)
DD( 1,  _,_,Y,R,R,Y,R,R,Y,_,_,_)
DD( 0,  _,_,_,Y,Y,_,Y,Y,_,_,_,_)

	float c = mod(floor(m/pow(4.,x)),4.);
	
	if( c>0.5 ) col = vec3(.8); // g
	if( c>1.5 ) col = vec3(.8); // y
	if( c>2.5 ) col = vec3(1.); // r
	//if( c>0. ) col = vec3(1.,1.,1.); // z
	
	// border
	float f = step(0.2,c); 
	//col += floor(f*taym*3.) *.095;
	
	return col;
}

void _userMain( void ) {

	vec2 p = (-resolution.xy+2.0*gl_FragCoord.xy)/resolution.y;

    // background	
	vec2 q = vec2( atan(p.y, p.x+.05), length(p) );
	float f = smoothstep( -0.1, 0.1, sin(q.x*22.0 + time*3.0) );
	//vec3 col = mix( vec3(0.42,0.55,1.0), vec3(0.6,0.7,1.0), f );
	vec3 col = vec3(0.);
	
	// soft shadow
	float sha = 0.0;
	for( int j=0; j<5; j++ )
	for(int i=0; i<5; i++ )
	{		
		vec3 s = heart( vec3(0.0), p + 10.0*vec2(float(i)-4.0,float(j)+1.0)/resolution.y );
		sha += step(0.1,p.x);
    }			

	float t = time*8.;
	// color
	col = heart( col, p+vec2(0.05,0.1-2.5*(sin(t)/5.)));
	for (float i = 2.5; i <= 14.5; i += 6.) {
		col = bone( col, p+vec2(-4.0 + mod((t)/(PI/3.) + i, 12.),0.4));
		col = bone( col, p+vec2(-4.0 + mod((t)/(PI/3.) + i - 3., 12.),-0.7));
		//col = bone( col, p+vec2(4.0 - mod((t*2.5)/(PI/3.) + i, 9.),0.4));
	}
	//col = bone( col, p+vec2(-2.0 + mod((time*2.5)/(PI/2.) + 3., 4.),1.1));

    // vigneting	
	//col *= 1.0 - 0.2*length(p);

    // fade in/out	
	//col *=       smoothstep(  0.0,  2.0, time );
       //col *= 1.0 - smoothstep( 55.0, 60.0, time );

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