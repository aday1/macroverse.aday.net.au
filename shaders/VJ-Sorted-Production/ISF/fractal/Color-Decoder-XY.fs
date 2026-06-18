/*{
    "DESCRIPTION": "Color-Decoder-XY",
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
        }
    ],
    "TAGS": [
        "fractal"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
//arithmetic encoder / decoder

//left side  - input series of symbols
//center     - probability space of all potential series with matching symbol ratios
//right side - series decoded from probability set and encoded phase

//the space is preserved as a fractal, which is handy
//every pixel could be running it's own set, if you were into that for whatever reason

//it still could optimized more, and less iterative (http://glslsandbox.com/e#27191.0) maybe..?

precision highp float;

varying vec2 surfacePosition;

#define SYMBOLS		7
#define ELEMENTS	8

#define POLAR

#define MIN_RESOLUTION min(resolution.x, resolution.y)

#define LINE_WIDTH MIN_RESOLUTION/16./MIN_RESOLUTION

//#define DEBUG_SPACE
#ifdef DEBUG_SPACE
vec2	g_debug = vec2(0.);
#endif

float	encode(in float probability[SYMBOLS], in float set[ELEMENTS]);
void	decode(float phase, in float probability[SYMBOLS], out float set[ELEMENTS]);
float	sum(in float probability[SYMBOLS]);
void	count(in float series[ELEMENTS], out float probability[SYMBOLS]);
void	derive(in float set[ELEMENTS], out float probability[SYMBOLS]);
void	zero(out float series[ELEMENTS]);
vec3	symbol_color(float i);
vec3	hsv(float h,float s,float v);
vec3 	display_set(vec2 uv, in float series[ELEMENTS]);
void	assign_random_symbols(out float series[ELEMENTS]);
float	hash(vec2 uv);

void main( void ) 
{
	//formatting
	vec2 uv 			= gl_FragCoord.xy/resolution.xy;
	vec2 p				= uv;
	
	#ifdef POLAR
	p 				= uv * 2. - 1.;
	p				*= resolution.xy/MIN_RESOLUTION;
	p				= vec2((atan(p.x, p.y)+(4.*atan(1.)))/(8.*atan(1.)), length(p));
	#endif
	
	#ifdef DEBUG_SPACE
	g_debug.y			= p.y;
	#endif
	
	//assign some random symbols to the series based on mouse position and a random hash
	float set[ELEMENTS];
	assign_random_symbols(set);
	vec3 initial_set 		= display_set(uv.yx, set);

	//encode the set into a phase within it's probabilities
	float probability[SYMBOLS];
	derive(set, probability);
	float phase 			= encode(probability, set);

	//wipe the set (no cheating!)
	zero(set);

	//using the probabilities and this encoded phase, decode back into the origional set
	decode(phase, probability, set);
	vec3 decoded_set 		= display_set(uv.yx, set);
	float phase_line		= float(abs(p.x-phase)<.001);

	//decode the entire space for visualization (not a particular set, but all sets with the matching symbol probabilities)
	phase				= p.x;
	decode(phase, probability, set);
	vec3 probability_set 		= display_set(p.yx, set);

	//display the results
	vec4 result			= vec4(0.);
	float scaled_uv			= uv.x * 64.;
	float column			= floor(scaled_uv);
	float display_initial_set	= float(column < 1.);
	float display_decoded_set	= float(column > 62.);
	float display_probability_set	= float(scaled_uv < 63. - LINE_WIDTH && scaled_uv > 1. + LINE_WIDTH);
	
	initial_set			*= display_initial_set;
	decoded_set			*= display_decoded_set;
	probability_set			*= display_probability_set;
	
	result.xyz			= initial_set + decoded_set + probability_set;
	result.xyz			+= phase_line * float(length(probability_set) > 0.);
	result.w 			= 1.;
	
	gl_FragColor 			= result;
	
	#ifdef DEBUG_SPACE
	g_debug.y			= fract(p.y*float(ELEMENTS))*float(p.y*float(ELEMENTS)<float(ELEMENTS));
	gl_FragColor			= vec4(g_debug.xy, 0., 1.);
	#endif
}//sphinx

float encode(in float probability[SYMBOLS], in float set[ELEMENTS])
{
	float period 	= 0.;
	float phase	= 0.;

	float interval[SYMBOLS];
	for(int i = 0; i < SYMBOLS; i++)
	{
		interval[i] = probability[i];
	}
	
	for(int i = 0; i < ELEMENTS; i++)
	{
		for(int j = 0; j < SYMBOLS; j++)
		{
			if(set[i] == float(j))
			{
				period  = probability[j];
				break;
			}
			phase	+= interval[j];
		}
		
		for(int j = 0; j < SYMBOLS; j++)
		{
			interval[j] *= period;
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
			set[i]		= theta < period ? float(j)				: set[i];
			phase 		= theta < period ? abs(theta-period)/probability[j]	: phase;					
			period		-= probability[j];

		}
		period = phase + phase;
		
		#ifdef DEBUG_SPACE
		if(floor(g_debug.y*float(ELEMENTS))==float(i))
		{
			g_debug.x = 1.-period/2.;
		}
		#endif

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

void count(in float series[ELEMENTS], out float probability[SYMBOLS])
{
	for(int i = 0; i < ELEMENTS; i++)
	{
		for(int j = 0; j < SYMBOLS; j++)
		{
			probability[j] += float(series[i] == float(j));
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

void zero(out float series[ELEMENTS])
{
	for(int i = 0; i < ELEMENTS; i++)
	{
		series[i] = 0.;
	}
}

vec3 display_set(vec2 uv, in float series[ELEMENTS])
{
	float index		= floor(uv.x * float(ELEMENTS));
	vec3 visualization	= vec3(0.);
	for(int i = 0; i < ELEMENTS; i++)
	{
		if(float(i) == index)
		{
			visualization 	= symbol_color(series[i]);
		}
	}
	
	float grid		= float(fract(uv.x * float(ELEMENTS)+LINE_WIDTH*.5)>LINE_WIDTH);

	return visualization * grid;
}

vec3 symbol_color(float i)
{
	return hsv(i/float(SYMBOLS), 1., 1.);		
}

vec3 hsv(float h,float s,float v)
{
	return mix(vec3(1.),clamp((abs(fract(h+vec3(3.,2.,1.)/3.)*6.-3.)-1.),0.,1.),s)*v;
}

float hash(vec2 uv)
{
	return fract(cos(uv.x+sin(uv.y))*12345.6789);
}

void assign_random_symbols(out float series[ELEMENTS])
{
	for(int i = 0; i < ELEMENTS; i++)
	{
		float symbol 	= floor(hash(mouse+float(i)/float(ELEMENTS))*float(SYMBOLS));
		series[i] 	= symbol;
	}
}

