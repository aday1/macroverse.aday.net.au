/*{
    "DESCRIPTION": "DotMatrixPointer1",
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
        "geometric"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

//bitwise op emulation via fract in progress

float extract_bit(float n, float b);
float sprite(float n, vec2 p);
float digit(float n, vec2 p);
float print_index(float index, vec2 position);
float display_results(float a, float b, float c, vec2 p);

float ulam_spiral(vec2 uv)
{
	vec2 ring		= abs(vec2(uv.x, uv.y));
	vec2 interval		= vec2(abs(uv.x+uv.y), abs(uv.x-uv.y));

	if(ring.x>ring.y)
	{
		float phase 	= uv.x > 0. ? ring.x * -4. : 0.;
		float square 	= 4. * ring.x * ring.x + 1.;
		return interval.x + square + phase;
	}
	else 
	{
		float phase 	= uv.y > 0. ? ring.y * -3. + ring.y : ring.y * 2.;		
		float square 	= 4. * ring.y * ring.y + 1.;
		return interval.y + square + phase;
	}
}

void main( void ) 
{
	vec2 aspect	= resolution/min(resolution.x, resolution.y);
	vec2 uv		= gl_FragCoord.xy/resolution;
	float scale	= 128.;	
	vec2 p		= (uv-.5)*aspect*scale;

	float u		= 1./256.;

	float a		= mouse.x;
	float b		= mouse.y;
	float c		= 0.;

	float d 		= a;
	float e 		= b;
	float f		= 0.;

	//works - needs to be better now...
	vec2 n	 	= vec2(a, b);
	for( int i=0; i < 8; i++ ) 
	{
		c 	+= mod(floor(n.x*256.)+floor(n.y*256.), 2.);    			//xor
		//c 	+= mod(1.-floor(n.x*256.)+floor(n.y*256.), 2.); 		//and
		//c 	+= max(mod(floor(n.x*256.), 2.), mod(floor(n.y*256.), 2.)); 	//or
		c	*= .5;
		n 	*= .5;
 	}	

	a 		*= 256.;
	b 		*= 256.;
	c 		*= 256.;

	p		= (uv-.5) * aspect * scale;
	
	vec4 result	= vec4(0.);
	result		+= display_results(a, b, c, p);
	
	// good date
	
	gl_FragColor	= result;
}//sphinx

float extract_bit(float n, float b)
{
	n = floor(n);
	b = floor(b);
	b = floor(n/pow(2.,b));
	return float(mod(b,2.) == 1.);
}

float sprite(float n, vec2 p)
{
	p = floor(p);
	float bounds = float(all(lessThan(p, vec2(3., 5.))) && all(greaterThanEqual(p,vec2(0,0))));
	return extract_bit(n, (2. - p.x) + 3. * p.y) * bounds;
}

float digit(float n, vec2 p)
{
	n = mod(floor(n), 10.0);
	if(n == 0.) return sprite(31599., p);
	else if(n == 1.) return sprite( 9362., p);
	else if(n == 2.) return sprite(29671., p);
	else if(n == 3.) return sprite(29391., p);
	else if(n == 4.) return sprite(23497., p);
	else if(n == 5.) return sprite(31183., p);
	else if(n == 6.) return sprite(31215., p);
	else if(n == 7.) return sprite(29257., p);
	else if(n == 8.) return sprite(31727., p);
	else if(n == 9.) return sprite(31695., p);
	else return 0.0;
}

float print_index(float index, vec2 position)
{	
	float result	= 0.;
	result 		+= index < 0. ? sprite(24., position+vec2(4., 0.)) : 0.;		
	for(int i = 0; i < 8; i++)
	{
		float place = pow(10., float(i));
		if(index >= place || float(i) < 1.)
		{
			result	 	+= digit(abs(index/place), position);
			position.x 	+= 4.;
		}
	}
	return result;
}

float display_results(float a, float b, float c, vec2 p)
{
	float print	= 0.;
	vec2 print_uv	= floor(p);
	print		+= print_index(a, print_uv);	
	print		+= print_index(b, print_uv + vec2(0.,6.));		
	print		+= print_index(c, print_uv + vec2(0.,12.));	
	
	float bits	= 0.;
	float bit_scale	= 6.;	
	vec2 bits_uv	= floor(p/bit_scale);
	for(float i = 0.; i < 8.; i++)
	{
		if(bits_uv.x - 1. == i && bits_uv.y == 0.)
		{
			bits += extract_bit(a, i);
		}
		
		if(bits_uv.x - 1. == i && bits_uv.y == -1.)
		{
			bits += extract_bit(b, i);
		}
		
		if(bits_uv.x - 1. == i && bits_uv.y == -2.)
		{
			bits += extract_bit(c, i);
		}
	}

	float grid	= float(
			(fract(p.x/bit_scale + .025) < .05 || fract(p.y/bit_scale+.025) < .05) 
			&& bits_uv.x > 0. 
			&& bits_uv.x < 9.
			&& bits_uv.y > -3.
			&& bits_uv.y < 1. );
	
	return print + bits - grid;
}


