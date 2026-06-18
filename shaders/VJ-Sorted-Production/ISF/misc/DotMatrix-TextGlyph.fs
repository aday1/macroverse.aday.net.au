/*{
    "DESCRIPTION": "DotMatrix-TextGlyph",
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
        "misc",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D 	renderbuffer;

//in progress...

float hash(float u)
{
    return fract(fract(u*9876.5432)*(u+u)*12345.678);
}

float hash(vec2 uv)
{
	return hash(uv.x+hash(uv.y));
}

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

float print(float n, vec2 position)
{	
	float offset	= 4.;
	
	float result	= 0.;
	for(int i = 0; i < 8; i++)
	{
		float place	= pow(10., float(i));
		if(n > place || float(i) == 0.)
		{
			result	 	+= digit(n/place, position + vec2(8., 0.));
			position.x 	+= offset;
		}
		else
		{
			break;
		}
		
	}
	return result;
}

vec4 symbolize(float s)
{
	s = s < .5 ? floor(s * 256.)/256. : floor(s * 256. + s)/256.;
	s = floor(s * 3.)+1.;
	return vec4(s==2., s==3., s==1., s);
}

vec2 aspect(vec2 uv)
{
	return uv * resolution.xy/resolution.yy;	
}

void main( void ) 
{
	vec2 uv		= gl_FragCoord.xy/resolution.xy;

	float columns	= 32.;
	float rows	= 9.;
	float column	= floor(uv.x * columns);
	float row	= floor(uv.y * rows);
	
	vec2 print_uv	=  vec2(mod(gl_FragCoord.x, resolution.x/columns), mod(gl_FragCoord.y, resolution.y*row/rows))-vec2(resolution.x/columns, 4.);
	
	float rate	= 8.;
	float phase	= fract(floor(time * rate)/columns);
	float raw_signal= hash(uv.x + phase);
	float signal 	= hash(column/columns + phase);
	
	float debug	= 0.;
	
	vec4 result 	= vec4(0.);
	
	if(row == 8.)
	{
		//raw input
		result += raw_signal;
	}
	if(row == 7.)
	{
		//temporal quantization
		result += signal;
		debug  += print(signal * 256., print_uv);
	}
	if(row == 6.)
	{
		//symbol quantization
		vec4 symbol 	= symbolize(signal);
		result		+= symbol;
		debug 		+= print(symbol.w, print_uv);
		
		//debug
		vec2 symbol_uv	 	= vec2(column/columns + .5/columns, row/rows + .5/rows);
		float debug_symbol_uv	= float(length(aspect(symbol_uv)-aspect(uv))<.005);
		//result			+= debug_symbol_uv;
	}
	if(row == 5.)
	{
		//count symbols and integrate to provide an occupancy ratio (symbol probability) (is sorting necessary?)
		float index  			= floor(fract(time * .5)*columns);
		
		vec2 integral_uv		= vec2(column/columns + .5/columns, row/rows + .5/rows);
		vec4 prior_integral_sample	= texture2D(renderbuffer, integral_uv - 1./columns);
		vec4 next_integral_sample	= texture2D(renderbuffer, integral_uv + 1./columns);
		
		vec2 symbol_uv	 	= vec2(column/columns + .5/columns, row/rows + 1.5/rows);
		vec4 symbol_sample	= texture2D(renderbuffer, symbol_uv);
			
		if(column == columns - 1. && prior_integral_sample.w == 0.)
		{			
			result			= texture2D(renderbuffer, uv);
			result.xyz 		= abs(normalize(result.xyz));
			
		}
		else if(floor(prior_integral_sample.w * columns) <= column)
		{
			result			= prior_integral_sample + symbol_sample/columns;
		}
		else if(column == 0.)
		{
			result			= symbol_sample/columns;
			result.w 		= 0.;
		}
		else 
		{
			result 			*= 0.;//texture2D(renderbuffer, uv);	
		}
		
		//debug
		//counts 
		debug  			+= print(result.x/columns * 1000., print_uv - vec2(0., 8.));
		debug  			+= print(result.y/columns * 1000., print_uv - vec2(0., 16.));
		debug  			+= print(result.z/columns * 1000., print_uv - vec2(0., 24.));
		debug  			+= print(dot(result.xyz, vec3(1.))/columns * 1000., print_uv);
				
   		//float debug_integral_uv		= float(length(aspect(integral_uv)-aspect(uv))<.005);
		//result 				+= debug_integral_uv;
	}
	if(row == 4.)
	{
		//plot the ratio 
		vec2 ratio_uv	 	= vec2(1. - .5/columns, row/rows + 1.5/rows);
		vec4 ratio_sample	= texture2D(renderbuffer, ratio_uv);
		
		vec3 ratio_plot		= vec3(0.);
		ratio_plot.x		= float(ratio_sample.x > uv.x); 
		ratio_plot.y		= float(ratio_sample.y + ratio_sample.x > uv.x); 
		ratio_plot.z		= float(ratio_sample.x + ratio_sample.y + ratio_sample.z > uv.x); 

		ratio_plot.z		-= ratio_plot.y;
		ratio_plot.y		-= ratio_plot.x;

		result.xyz 		+= ratio_plot;
		
		//debug 
		debug  			+= print(ratio_sample.x * 100., vec2(gl_FragCoord.x - ratio_sample.x * resolution.x, print_uv.y));
		debug  			+= print(ratio_sample.y * 100., vec2(gl_FragCoord.x - (ratio_sample.x + ratio_sample.y) * resolution.x, print_uv.y));
		debug  			+= print(ratio_sample.z * 100., vec2(gl_FragCoord.x + 4. - (ratio_sample.x + ratio_sample.y + ratio_sample.z) * resolution.x, print_uv.y));
	}
	if(row == 3.)
	{
	}		
	if(row == 2.)
	{
	}		
	if(row == 1.)
	{
	}		
	if(row == 0.)
	{
	}		

	//debug
	result += dot(result, vec4(1.)) < 2. ? debug : -debug;
	
	bool clear = mouse.x + mouse.y > .02;
	result *= float(clear);
	
	gl_FragColor	= result;
}//sphinx
