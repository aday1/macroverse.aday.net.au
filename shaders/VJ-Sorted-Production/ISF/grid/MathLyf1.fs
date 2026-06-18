/*{
    "DESCRIPTION": "MathLyf1",
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
        }
    ],
    "TAGS": [
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

//ulam spiral in progress

float extract_bit(float n, float b);
float sprite(float n, vec2 p);
float digit(float n, vec2 p);
float print_index(float index, vec2 position);

//65 64 63 62 61 60 59 58 57
//66 37 36 35 34 33 32 31 56
//67 38 17 16 15 14 13 30 55
//68 39 18 5  4  3  12 29 54
//69 40 19 6  1  2  11 28 53
//70 41 20 7  8  9  10 27 52
//71 42 21 22 23 24 25 26 51
//72 43 44 45 46 47 48 49 50
//73 74 75 76 77 78 79 80 81

//I always thought if somebody would do it in a mathematical way instead. 
/* Psuedo code:
	dir = 0; //right, up, left, down
	steps = 0;
	x = 0; y = 0;
	put(x,y)
	while (!outside) {
	    repeat(floor(steps/2)+1) {
		x += ((dir = 0)-(dir = 2));
		y += ((dir = 3)-(dir = 1));
		put(x,y)
	    }
	    steps++;
	    dir = (dir+1) mod 4;
	}
*/

float fp(float x)
{
	return floor(sqrt(4.*x+1.));	
}

float fq(float x, float p)
{
	return x-floor(pow(p, 2.)/4.);
}

void main( void ) 
{
	float divisor	= 16.;	
	
	vec2 fc		= gl_FragCoord.xy-resolution*.5;
	
	vec2 fc_print	= fc;
	fc_print.x	= mod(fc_print.x, divisor) - divisor / 2.;
	fc_print.y	= mod(fc_print.y, divisor) - divisor / 2.;
	
	float grid	= float(mod(fc.x, divisor) < 1.) + float(mod(fc.y, divisor) < 1.);
	
	vec2 field	= floor(fc/divisor);			
	float angle	= fract(atan(field.x, field.y)/(8.*atan(1.)));
	bool phase	= abs(fract(angle * 2.)-.5) < .25;
	bool parity	= abs(field.x) > abs(field.y);
	field		= parity ? field.xy : field.yx;

	float s		= (parity ?  -1. : 1.);
	field.y		= parity ? abs(field.x-field.y) : abs(field.x+field.y);
	
	float quadrant	= phase ? (parity ? 3. : 1.) : (parity ? 4. : -2.);
	
	float ulam	= abs((quadrant*abs(field.x))/4.-abs(field.y+(field.x/field.y)*(field.y*field.x)));

	vec3 print	= vec3(0.);
	print.xyz	+= print_index(ulam, fc_print - vec2(0., -2.));	
	print 		*= 64.;

//	float hilight	= floor(mod(time*4., divisor*4.)) == mod(ulam, divisor*4.) ? 1. : 0.;
	float hilight	= abs(floor(mod(time*64., 512.))-ulam) < 2. ? 1. : 0.;	
	
	vec4 result	= vec4(0.);

	result		+= fract(ulam/divisor/field.x);
	result		+= grid/4.;
	result.yz	-= hilight;
	result.xyz	+= result.y < .5 ? print : -print;	
	//result.x 	-= float(quadrant==0.);
	//result.y 	-= float(quadrant==1.);
	//result.z 	-= float(quadrant==2.);
	
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
