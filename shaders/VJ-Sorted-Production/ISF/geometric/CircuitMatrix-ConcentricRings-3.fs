/*{
    "DESCRIPTION": "CircuitMatrix-ConcentricRings-3",
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
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

////Arithmetic Coding https://en.wikipedia.org/wiki/Arithmetic_coding

//this technique encodes an ordered series of symbols into a single number, along with the probabilility of each symbol occuring in the sequence
//using the probability, it maximizes the potential encoding space for that unique series

//during encoding, it recursively subdivides to cover the space of possible orderings, one of which encodes the target series

//decoding takes in the code for the target sequence and the ratios, then reverses the process

////visualization key
//encoding
//top left circle 	- probability mass distrobution (the ratio of symbol occurances in the sequence)
//top left row		- the series of symbols read directly from the array
//bottom left circle	- the encoding steps that determine the code (phase) for the series 

//decoding
//bottom right 		- the entire space of possible series for that given set of ratios
//top right circle	- the same set as the bottom right, but mapped to polar coordinates
//top right row		- the decoded series

////encoding and decoding seem to mostly work now =) - sphinx

//number of elements in the series
#define ELEMENTS 8

//symbols
#define RED	vec4(1., 0., 0., 0.)
#define GREEN	vec4(0., 1., 0., 0.)
#define BLUE	vec4(0., 0., 1., 0.)
#define YELLOW	vec4(0., 0., 0., 1.)

void  arithmetic_decode( in float code, in vec4 probability, out vec4 series[ELEMENTS]);
void  arithmetic_encode(out float code, out vec4 probability, in vec4 series[ELEMENTS]);
void allocate_series(out vec4 series[ELEMENTS]);

void  debug_arithmetic_encode(out float code, out vec4 probability, in vec4 series[ELEMENTS], out vec4 plot[ELEMENTS], out float debug_code[ELEMENTS]);
float sum(vec2 v);
float sum(vec3 v);
float sum(vec4 v);

void arithmetic_encode(out float code, out vec4 probability, in vec4 series[ELEMENTS])
{
	//generate the probability by counting the occurances of each symbol
	probability 	= vec4(0.);
	for(int i = 0; i < ELEMENTS; i++)
	{
		probability 	+= series[i];
	}
	probability = probability/sum(probability);
	
	//encode the interval
	vec4 interval	= probability;
	for(int i = 0; i < ELEMENTS; i++)
	{
		float x = interval.x;
		float y = sum(interval.xy);
		float z = sum(interval.xyz);
		
		code	+= sum(series[i].yzw * vec3(x,y,z));

		if(series[i].x >= sum(series[i].yzw)) { interval *= probability.x; } else
		if(series[i].y >= sum(series[i].xzw)) { interval *= probability.y; } else
		if(series[i].z >= sum(series[i].xyw)) { interval *= probability.z; } else
		if(series[i].w >= sum(series[i].xyz)) { interval *= probability.w; }	
	}
}

void arithmetic_decode(in float phase, in vec4 probability, out vec4 series[ELEMENTS])
{
	//decode the symbol at the specified phase from the probability mass distribution
	vec4 mass	= vec4(0.);
	vec4 interval	= probability;
	for(int i = 0; i < ELEMENTS; i++)
	{
		float x = interval.x;
		float y = sum(interval.xy);
		float z = sum(interval.xyz);
		
		mass.x	= float(phase >= 0. && phase < x);
		mass.y	= float(phase >= x  && phase < y);
		mass.z	= float(phase >= y  && phase < z);
		mass.w	= float(phase >= z  && phase < 1.);

		phase	-= sum(mass.yzw * vec3(x,y,z));
		
		if(mass.x >= sum(mass.yzw)) { interval *= probability.x; } else
		if(mass.y >= sum(mass.xzw)) { interval *= probability.y; } else
		if(mass.z >= sum(mass.xyw)) { interval *= probability.z; } else
		if(mass.w >= sum(mass.xyz)) { interval *= probability.w; }
		
		series[i] = mass;
	}
}

void main( void ) 
{

	vec4 series[ELEMENTS];
	vec4 probability	= vec4(0.);
	float code		= 0.;
	
	//assign a series of symbols to the series array
	allocate_series(series);

	vec4 debug_plot[ELEMENTS];
	float debug_code[ELEMENTS];	

	debug_arithmetic_encode(code, probability, series, debug_plot, debug_code);
	arithmetic_decode(code, probability, series);

	//everything past this is visualization
	vec2 uv 		= gl_FragCoord.xy/resolution.xy;
	float elements 		= float(ELEMENTS);
	vec4 result 		= vec4(0.);
	bool left		= uv.x < .5;
	if(left)
	{
		float row		= floor(uv.y * 13.);
		bool series_row		= row == 9.;
		float element_position	= floor(uv.x * 2. * elements * 2. - 8.);
		vec4 series_plot 	= vec4(0.);
		for(int i = 0; i < ELEMENTS; i++)
		{
			if(float(i) == 	element_position && series_row)
			{
				series_plot 	+= debug_plot[i];
				series_plot.xy 	+= max(result.xy, debug_plot[i].ww);
			}
		}
		
		vec4 probability_plot = vec4(0.);
		if(row < 13. && row > 0.)
		{
			vec2 polar_uv 	= uv - vec2(.13, .8);
			polar_uv	*= resolution.xy/resolution.yy;
			
			polar_uv = vec2((atan(polar_uv.y, polar_uv.x)+(4.*atan(1.)))/(8.*atan(1.)), length(polar_uv));
			polar_uv = abs(polar_uv);
			if(polar_uv.y < .167)
			{
				float x = probability.x;
				float y = sum(probability.xy);
				float z = sum(probability.xyz);	
				probability_plot.x	= float(polar_uv.x > 0. && polar_uv.x < x);
				probability_plot.y	= float(polar_uv.x > x  && polar_uv.x < y);
				probability_plot.z	= float(polar_uv.x > y  && polar_uv.x < z);
				probability_plot.w	= float(polar_uv.x > z  && polar_uv.x < 1.);
				probability_plot.xy	= max(probability_plot.xy, probability_plot.ww);
			}
		}
		
		vec2 polar_uv 	= uv - vec2(.25, .3);
		polar_uv	*= resolution.xy/resolution.yy;
			
		polar_uv = vec2((atan(polar_uv.y, polar_uv.x)+(4.*atan(1.)))/(8.*atan(1.)), length(polar_uv)*3.5);
		polar_uv = abs(polar_uv);
		
		if(row < 8. && row > -1.)
		{
			row	 = floor(polar_uv.y * elements);
			for(int i = 0; i < ELEMENTS; i++)
			{
				if(float(i) == row)
				{
					series_plot 	+= debug_plot[i];
					series_plot.xy 	+= max(result.xy, debug_plot[i].ww);
					series_plot.xyz *= .25+float(debug_code[i+1] > polar_uv.x);
				}
			}	
		}
		float encoding_line = float(abs(fract(polar_uv.x)-code-.004) < .002) * float(row <  float(ELEMENTS));;
	
		result += encoding_line;
		result += series_plot;
		result += probability_plot;
	}
	else
	{
		float row		= floor(uv.y * 13.);
		
		float element_position	= floor(fract(uv.x * 2.) * 2. * elements - 7.5);
		
		vec4 decoding_plot = vec4(0.);

		for(int i = 0; i < ELEMENTS; i++)
		{
			if(float(i) == element_position && row == 9.)
			{
				decoding_plot 		+= series[i];
				decoding_plot.xy 	+= max(result.xy, series[i].ww);
			}	
		}

		vec4 all_series_plot 	= vec4(0.);
		vec4 all_series[ELEMENTS];
		arithmetic_decode(fract(uv.x*2.), probability, all_series);
	
		for(int i = 0; i < ELEMENTS; i++)
		{
			
			if(float(i)==row)
			{
				all_series_plot 	+= all_series[i];
				all_series_plot.xy 	+= max(result.xy, all_series[i].ww);
			}
		}
		float encoding_line = float(abs(fract(uv.x*2.)-code-.008) < .004) * float(row <  float(ELEMENTS));
	
		vec4 probability_plot = vec4(0.);
		if(row < 13.)
		{
			vec2 polar_uv 	= uv - vec2(.62, .8);
			polar_uv	*= resolution.xy/resolution.yy;
			
			polar_uv = vec2((atan(polar_uv.y, polar_uv.x)+(4.*atan(1.)))/(8.*atan(1.)), length(polar_uv * 1.5));
			polar_uv = abs(polar_uv);
			
			row = floor(polar_uv.y * elements * 4.);
			
			arithmetic_decode(fract(polar_uv.x), probability, all_series);
			
			for(int i = 0; i < ELEMENTS; i++)
			{
				if(polar_uv.y < .3 && row == float(i))
				{
					probability_plot += all_series[i];
					probability_plot.xy += all_series[i].w;
				}
			}
			
			encoding_line += float(abs(fract(polar_uv.x)-code-.008) < .004) * float(polar_uv.y < .25);
		}

		result += encoding_line;
		result += all_series_plot;
		result += probability_plot;
		
		result += decoding_plot;
	}

	gl_FragColor 	= result;
}//sphinx

void debug_arithmetic_encode(out float code, out vec4 probability, in vec4 series[ELEMENTS], out vec4 plot[ELEMENTS], out float debug_code[ELEMENTS])
{
	probability = vec4(0.);
	for(int i = 0; i < ELEMENTS; i++)
	{
		probability 	+= series[i];
	}
	probability = probability/sum(probability);
	
	vec4 interval	= probability;
	for(int i = 0; i < ELEMENTS; i++)
	{
		float x = interval.x;
		float y = sum(interval.xy);
		float z = sum(interval.xyz);
		
		code	+= sum(series[i].yzw * vec3(x,y,z));

		if(series[i].x >= sum(series[i].yzw)) { interval *= probability.x; } else
		if(series[i].y >= sum(series[i].xzw)) { interval *= probability.y; } else
		if(series[i].z >= sum(series[i].xyw)) { interval *= probability.z; } else
		if(series[i].w >= sum(series[i].xyz)) { interval *= probability.w; }	
		
		debug_code[i] 	= code;
		plot[i] 	= series[i];
	
	}
}

float sum(vec2 v)
{
	return dot(v, vec2(1.));	
}

float sum(vec3 v)
{
	return dot(v, vec3(1.));	
}

float sum(vec4 v)
{
	return dot(v, vec4(1.));	
}

void allocate_series(out vec4 series[ELEMENTS])
{
	float m = floor(mouse.x * 8.);
	
	if(m == 0.)
	{
		series[0] = RED;
		series[1] = RED;
		series[2] = GREEN;
		series[3] = GREEN;
		series[4] = BLUE;
		series[5] = BLUE;
		series[6] = YELLOW;
		series[7] = YELLOW;
	} 
	else if(m == 1.)
	{
		series[0] = RED;
		series[1] = RED;
		series[2] = RED;
		series[3] = GREEN;
		series[4] = GREEN;
		series[5] = BLUE;
		series[6] = BLUE;
		series[7] = BLUE;
	}
	else if(m == 2.)
	{
		series[0] = RED;
		series[1] = GREEN;
		series[2] = BLUE;
		series[3] = GREEN;
		series[4] = GREEN;
		series[5] = RED;
		series[6] = YELLOW;
		series[7] = BLUE;
	}
	else if(m == 3.)
	{
		series[0] = RED;
		series[1] = GREEN;
		series[2] = YELLOW;
		series[3] = RED;
		series[4] = BLUE;
		series[5] = RED;
		series[6] = RED;
		series[7] = BLUE;
	}
	else if(m == 4.)
	{
		series[0] = RED;
		series[1] = RED;
		series[2] = RED;
		series[3] = RED;
		series[4] = RED;
		series[5] = RED;
		series[6] = RED;
		series[7] = BLUE;
	}
	else if(m == 5.)
	{
		series[0] = YELLOW;
		series[1] = YELLOW;
		series[2] = BLUE;
		series[3] = RED;
		series[4] = GREEN;
		series[5] = GREEN;
		series[6] = YELLOW;
		series[7] = GREEN;
	}
	else if(m == 6.)
	{
		series[0] = RED;
		series[1] = BLUE;
		series[2] = YELLOW;
		series[3] = YELLOW;
		series[4] = GREEN;
		series[5] = RED;
		series[6] = YELLOW;
		series[7] = YELLOW;
	}
	else if(m == 7.)
	{
		series[0] = BLUE;
		series[1] = BLUE;
		series[2] = BLUE;
		series[3] = RED;
		series[4] = GREEN;
		series[5] = GREEN;
		series[6] = GREEN;
		series[7] = BLUE;
	}
}


