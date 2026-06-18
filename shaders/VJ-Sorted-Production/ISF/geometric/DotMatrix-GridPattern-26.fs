/*{
    "DESCRIPTION": "DotMatrix-GridPattern-26",
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
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
precision mediump float;

#extension GL_OES_standard_derivatives : enable

//wip...

float extract_bit(float n, float b);
float sprite(float n, vec2 p);
float digit(float n, vec2 p);
float print_index(float index, vec2 position);
float display_results(float a, float b, float c, vec2 p);

void main( void ) 
{
	vec2 aspect	= resolution/min(resolution.x, resolution.y);
	vec2 uv		= gl_FragCoord.xy/resolution;
	float scale	= 128.;	
	vec2 p		= (uv-.5)*aspect*scale;

	float a		= mouse.x*1.77777; // corrected the x scale
	float b		= mouse.y;
	float c		= 0.;

	//works - needs to be better now...
	vec2 n	= vec2(a,b);
	const int bits	= 8;
	float word	= pow(2., float(bits));
	for(int i=0; i < bits; i++) 
	{

		c 	+= mod(dot(floor(n * word), vec2(1.)), 2.);		//xor
		c 	+= mod(floor(n.x * word) * floor(n.y * word), 2.);	//and
		c	*= .5;
		n 	*= .5;
 	}	

	p		= (uv-.5) * aspect * scale;
	
	vec4 result	= vec4(0.);
	result		+= display_results(a, b, c, p);
	float h		= min(resolution.x, resolution.y)/max(resolution.x, resolution.y);

	p		= uv * aspect * scale;
	vec2 m		= vec2(mouse.x*1.77777, mouse.y)*aspect*scale*vec2(h,1.); // corrected the x scale
	float n_pos	= float(abs(p.x-m.x)<.2||abs(p.y-m.y)<.2);
	n 		= floor(p.xy)/128.;
	c 		= 0.;
	for(int i=0; i < bits; i++) 
	{
		c 	+= mod(dot(floor(n * word), vec2(1.)), 2.);		//xor
		c 	+= mod(floor(n.x * word) * floor(n.y * word), 2.);	//and
		c	*= .5;
		n 	*= .5;
 	}	
	
	result.x 	+= uv.x < h ? n_pos 	 : 0.;
	result		+= uv.x < h ? c	- n_pos	 : 0.;	
	
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
	a *= 256.;
	b *= 256.;
	c *= 256.;
	p.x -= 32.;
	
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
			(fract(p.x/bit_scale + .05) < .02 || fract(p.y/bit_scale+.05) < .075) 
			&& bits_uv.x > 0. 
			&& bits_uv.x < 9.
			&& bits_uv.y > -3.
			&& bits_uv.y < 1. );
	
	return print + bits - grid;
}


