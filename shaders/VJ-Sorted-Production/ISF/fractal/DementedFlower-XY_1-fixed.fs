/*{
    "DESCRIPTION": "DementedFlower-XY",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "fractal"
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
        "fractal",
        "texture-input"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
//arithmetic encoder / decoder
//change resolution from 2. to .5 in the top left dropdown menu

//still could optimized more, and less iterative (http://glslsandbox.com/e#27191.0) maybe..?

#ifdef GL_ES
precision highp float;
#endif

uniform sampler2D renderbuffer;

#define SYMBOLS		5				//number of unique colored symbols
#define ELEMENTS        5				//number of symbols in the set to be encoded / decoded

//comment the following defines on and off to change the visualization
#define POLAR						//display with polar coordinates

//#define DISPLAY_LINE_AT_SET_ENCODING                  //show the line crossing all the set elements within the space of probabilities
#define DISPLAY_PROBABILITY_SPACE		      	//show the space of all sets with the same probability (ratio) of symbols occuring
//#define DISPLAY_SETS				      	//show the initial set on the far left, and the decoded set next to it
#define DISPLAY_BRANCHES			      	//draw the branching tree structure of the sets as lines, rather than color
//#define DISPLAY_PROBABILITY_SPACE_COORDINATES	      	//show the coordinates of the subdivided probability space - 0-1 red is x, 0-1 green is y
//#define DISPLAY_DECODING_TEST                       	//test the encoding accuracy - turn on stream random input, turn off polar and only display sets - green is a match, red is a miss

//#define STREAM_RANDOM_INPUT				//change random input over time

#define MIN_RESOLUTION min(resolution.x, resolution.y)  //used for formatting
#define LINE_WIDTH max(128./MIN_RESOLUTION*probability[i], iteration/MIN_RESOLUTION)+32.*(1.-p.y)/MIN_RESOLUTION;

#//HEADER
//encoding / decoding functions
float	encode(in float probability[SYMBOLS], in float set[ELEMENTS]);
void	decode(float phase, in float probability[SYMBOLS], out float set[ELEMENTS]);
float	sum(in float probability[SYMBOLS]);
void	count(in float series[ELEMENTS], out float probability[SYMBOLS]);
void	derive(in float set[ELEMENTS], out float probability[SYMBOLS]);

//these functions are all used for display
vec4 	display(in float encoded_phase, in float set[ELEMENTS],  in float probability[SYMBOLS], in float decoded_set[ELEMENTS]);
vec3 	display_set(vec2 uv, in float set[ELEMENTS]);
vec3	symbol_color(float i);
vec3	hsv(float h,float s,float v);
void	assign_random_symbols(out float set[ELEMENTS]);
float	hash(vec2 uv);
mat2 	rmat(in float r);
float 	line(vec2 p, vec2 a, vec2 b, float w);

//global uv coordinates - a hack for displaying the tree - not required for encoding/decoding
vec2	g_uv = vec2(0.);

#//MAIN
void main( void ) 
{
	//assign some random symbols to the series based on mouse position and a random hash
	float input_set[ELEMENTS];
	assign_random_symbols(input_set);

	//derive the probability distribution from the set
	float probability[SYMBOLS];
	derive(input_set, probability);

	//encode the set into a phase within it's probabilities
	float encoded_phase 		= encode(probability, input_set);

	//using the probabilities and this encoded phase, decode it into a new copy of the origional set
	float decoded_set[ELEMENTS];
	decode(encoded_phase, probability, decoded_set);

	gl_FragColor 			= display(encoded_phase, input_set, probability, decoded_set);
}//sphinx

#//ENCODING/DECODING FUNCTIONS
float encode(in float probability[SYMBOLS], in float set[ELEMENTS])
{
	float phase	= 0.;

	float interval[SYMBOLS];
	for(int i = 0; i < SYMBOLS; i++)
	{
		interval[i] = probability[i];
	}
	
	for(int i = 0; i < ELEMENTS; i++)
	{
		bool halt = false;
		for(int j = 0; j < SYMBOLS; j++)
		{
			if(set[i] != float(j) && !halt)
			{
				phase += interval[j];
				
			}
			else if(!halt)
			{
				for(int k = 0; k < SYMBOLS; k++)
				{
					interval[k] *= probability[j];
				}
				
				halt = true;
			}
		}
	}
	
	return phase;
}

void decode(float phase, in float probability[SYMBOLS], out float set[ELEMENTS])
{
	float period 	= 1.;
	for(int i = 0; i < ELEMENTS; i++)
	{
		float theta = phase;

		for(int j = SYMBOLS-1; j >= 0; j--)
		{		
			if(theta < period)
			{
				
				set[i]		= float(j);
				phase 		= abs(theta-period)/probability[j];	
				period		-= probability[j];		
			}
		}
		
		period = phase + phase;
		
		if(floor(g_uv.y*float(ELEMENTS))==float(i))
		{
			g_uv.x = 1.-period/2.;
		}
	}
}

float sum(in float probability[SYMBOLS])
{
	float sum = 0.;
	for(int i = 0; i < SYMBOLS; i++)
	{
		sum += probability[i];
	}
	return sum;
}

void count(in float set[ELEMENTS], out float probability[SYMBOLS])
{
	for(int i = 0; i < ELEMENTS; i++)
	{
		for(int j = 0; j < SYMBOLS; j++)
		{
			probability[j] += float(set[i] == float(j));
		}
	}
}

void derive(in float set[ELEMENTS], out float probability[SYMBOLS])
{	
	count(set, probability);
	float s = sum(probability);
	for(int i = 0; i < SYMBOLS; i++)
	{
		probability[i] /= s;
	}
}

void assign_random_symbols(out float set[ELEMENTS])
{
	vec2 seed = mouse;
	
	#ifdef STREAM_RANDOM_INPUT
	seed -= time;
	#endif 
	
	for(int i = 0; i < ELEMENTS; i++)
	{
		float symbol 	= floor(hash(seed*float(i+1)/float(ELEMENTS))*float(SYMBOLS));
		set[i] 	= symbol;
	}
}

#//DISPLAY FUNCTIONS
//all functions past here are just for display
vec3 display_set(vec2 uv, in float set[ELEMENTS])
{
	float index		= floor(uv.x * float(ELEMENTS));
	vec3 visualization	= vec3(0.);
	for(int i = 0; i < ELEMENTS; i++)
	{
		if(float(i) == index)
		{
			visualization 	= symbol_color(set[i]);
		}
	}

	return visualization;
}

vec3 symbol_color(float i)
{
	return hsv(fract(i/float(SYMBOLS)), 1., 1.);		
}

vec3 hsv(float h,float s,float v)
{
	return mix(vec3(1.),clamp((abs(fract(h+vec3(3.,2.,1.)/3.)*6.-3.)-1.),0.,1.),s)*v;
}

float hash(vec2 uv)
{
	return fract(cos(uv.x-uv.y*123.456789)*12345.6789);
}

mat2 rmat(in float r)
{
	float c = cos(r);
	float s = sin(r);
	return mat2(c, s, -s, c);
}

float line(vec2 p, vec2 a, vec2 b, float w)
{
	if(a==b) return(0.);
	float d = distance(a, b);
	vec2  n = normalize(b - a);
   	vec2  l = vec2(0.);
	l.x = max(abs(dot(p - a, n.yx * vec2(-1.0, 1.0))), 0.0);
	l.y = max(abs(dot(p - a, n) - d * 0.5) - d * 0.5, 0.0);
	return smoothstep(w, 0., l.x+l.y);
}

vec4 display(in float encoded_phase, in float set[ELEMENTS], in float probability[SYMBOLS], in float decoded_set[ELEMENTS])
{
	vec2 uv 			= gl_FragCoord.xy/resolution.xy;
	
	vec2 p				= uv;
	
	#ifdef POLAR
	p 				= uv * 2. - 1.;
	p				*= resolution.xy/MIN_RESOLUTION;
	p				= vec2((atan(p.x, p.y)+(4.*atan(1.)))/(8.*atan(1.)), length(p));
	#endif
	
	//display the results
	vec4 result			= vec4(0.);

	float scaled_uv			= uv.x * 128.;
	float column			= floor(scaled_uv);
	float display_initial_set	= float(column == 0.);
	float display_decoded_set	= float(column == 1.);
	float display_set_verification	= float(column == 2.);
	float display_probability_set	= float(column > 4.);
	
	#ifdef DISPLAY_LINE_AT_SET_ENCODING
	float phase_line		= 0.;
	
	#ifdef POLAR
	vec2 pl				= (uv * 2. - 1.) * resolution.xy/MIN_RESOLUTION;
	pl				*= rmat(encoded_phase);
	phase_line			= line(pl, vec2(0., 0.), vec2(0., -1.), 2./MIN_RESOLUTION);
	#else 
	phase_line			= line(p, vec2(encoded_phase, -1.), vec2(encoded_phase, 1.), 1./MIN_RESOLUTION);
	#endif
	
	result.xyz			+= phase_line;
	#endif
	
	g_uv.y				= p.y;
	
	//decode the entire space for visualization (not a particular set, but all sets with the matching symbol probabilities)
	float all_sets[ELEMENTS];
	float all_phases		= p.x;
	decode(all_phases, probability, all_sets);	

	#ifdef DISPLAY_PROBABILITY_SPACE	
	vec3 probability_set 		= display_set(p.yx, all_sets);
	result.xyz			+= probability_set;
	#endif

	#ifdef DISPLAY_SETS
	result				*= display_probability_set;
	result.xyz			+= (display_set(uv.yx, set)  * display_initial_set + display_set(uv.yx, decoded_set) * display_decoded_set) * (1.-display_probability_set);
	#endif

	#ifdef DISPLAY_BRANCHES
	g_uv.y				= fract(p.y*float(ELEMENTS))*float(p.y*float(ELEMENTS)<float(ELEMENTS));

	float branch 			= 0.;
	float offset			= 0.;
	float iteration			= 1.+floor(p.y*float(ELEMENTS))*float(p.y*float(ELEMENTS)<float(ELEMENTS));
	p 				= g_uv;
	p 				= p * 2. - 1.;
	p 				*= resolution/MIN_RESOLUTION;
	for(int i = 0; i < SYMBOLS; i++)
	{
		offset += probability[i];
		if(probability[i] > 0.)
		{
			float width 	= LINE_WIDTH;
			float x		= ((offset - probability[i]*.5)*2.-1.);
			branch 		+= line(p, vec2(0., -1.), vec2(x, 1.)*resolution/MIN_RESOLUTION, width);
		}
	}
	vec3 tree = vec3(branch);
	#ifdef DISPLAY_PROBABILITY_SPACE
	tree 		*= result.xyz;
	result.xyz 	*= 0.;
	#endif
	result += vec4(tree*float(length(p.y)<1.), 1.);
	#endif

	#ifdef DISPLAY_PROBABILITY_SPACE_COORDINATES
	result				= vec4(g_uv.xy, 0., 1.) * float(length(p.y)<1.);
	#endif

	#ifdef DISPLAY_DECODING_TEST
	float row			= floor(p.y * float(ELEMENTS));
	result 				*= 1.-display_set_verification;
	if(display_set_verification != 0.)
	{
		for(int i = 0; i < ELEMENTS; i++)
		{
			if(row == float(i))
			{
				result.xyz = set[i] == decoded_set[i] ? vec3(0.,1.,0.) : vec3(1.,0.,0.);
			}
		}
	}

	float grid			= 1.-float(fract(p.y*float(ELEMENTS)+8./MIN_RESOLUTION) < 32./MIN_RESOLUTION);
	
	float display_buffer		= float(column > 2.);
	vec4 buffer			= texture2D(renderbuffer, uv - vec2(1./resolution.x, 0.));
	result 				*= 1.-display_buffer;
	result				+= buffer * display_buffer * grid; 
	
//	buffer				= texture2D(renderbuffer, uv);
//	result				= mix(result, buffer, .78);
	result				*= mouse.x + mouse.y > .02 ? 1. : 0.;
	#endif
	
	result.w 			= 1.;
	
	return result;
}
