/*{
    "DESCRIPTION": "FrostCrystal-Fire-GradientShift",
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
        }
    ],
    "TAGS": [
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
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

void main( void ) {

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
