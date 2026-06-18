/*{
    "DESCRIPTION": "FrostCrystal-Rainbow-DotMatrix-2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "particles"
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
        "particles",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision highp float;
#endif

uniform sampler2D renderbuffer;

//rainbow workshop

//trying to suss out why a value like 12.1 is a good "frequency" for the angle cutting sequence
//different frequencies make different lines - the goal is of course to make them as straight as possible
//aliasing from passing through the 8 bit buffer is an issue, but hopefully mitigated here with the floor(f * 256.)/256. (or should that be something else?)
//aliasing is generally the magic, though, thus the complications of the back buffer and attempts at controlling it via quantization

//update - more elaborate visuals
//ive noticed values like 1234.5678 (used in hash functions and known for high bit entropy) do well 
//this makes sense, given it has a high degree of randomness, thereby "normalizing" the distrobution of steps
//still not "perfect" though

//visuals:
//particle lines are on the top half

//excessively elaborate bottom half:
//bottom: 	cutting sequence values (used to tell automata whether to go into the cell they are most pointed into, or the next one over)
//mid row: 	bit pattern from cutting sequence values
//mid top row:	bit pattern of frequency

//note, the bit visualizations are floored discretized versions of the floats, not precisely the mantissa, but do often help to reveal patterns

//mouse to the bottom of the screen to clear the back buffer

#define FREQUENCY (mouse.x < .5 ? 1. : 1234.5678)

//moore = 8 neighbor samples || von_neumann = 4.
#define MOORE			
//#define VON_NEUMANN	

vec3 hsv(in float h, in float s, in float v);
float extract_bit(float n, float b);

#define QUANTIZATION pow(2., 8.)

void _userMain() 
{   
	//normalized screen coordinate
	vec2 uv  = gl_FragCoord.xy / resolution.xy;  

	////frame timestep (in lieu of using coordinates, which work but present problems due to the aspect ratio and sampling errors)
	vec4 memory	= texture2D(renderbuffer, vec2(0.));	
	float frame 	= memory.x;
	if(floor(gl_FragCoord.x) < 1./resolution.x && floor(gl_FragCoord.y) < 1./resolution.y)
	{
		frame		= mod(frame * QUANTIZATION + 1., QUANTIZATION)/QUANTIZATION;
		frame 		= max(frame, 1./QUANTIZATION);
		gl_FragColor 	= vec4(frame, 0., 0., 0.);
		return;
	}
	////

	float sequence	 	= mod( frame * FREQUENCY, .125);		
	vec4 sequence_color	= vec4(hsv(sequence * 8., 1., 1.), 0.);

	//256 uv steps to match the 256 frames from the frame timer
	//bitwise display of bits from these values as well
	if(uv.y < .5)
	{
		float uv_frame		= floor(uv.x * QUANTIZATION)/QUANTIZATION;
	sequence		= mod(uv_frame * FREQUENCY, .125);
		float sequence_steps	= float(sequence > uv.y/2.);
		float raw_sequence 	= mod(uv.x * FREQUENCY, .125);
		float raw_steps		= float(uv.y/2. < raw_sequence) * .25;
		
		float time_frame_line 	= float(floor(uv_frame * QUANTIZATION) == floor(frame * QUANTIZATION));
		sequence_color 		= vec4(hsv(sequence * 8., 1., 1.), 1.);
	
		if(uv.y < .25)
		{
			gl_FragColor 	+= sequence_color * .75 * sequence_steps * .5;
			gl_FragColor	+= raw_steps;
			gl_FragColor 	+= time_frame_line;
			return;
		}
		else if(uv.y < .495)
		{
			float position 	= mod(floor(uv.y * 32.), 8.);
			vec4 bit	= vec4(0.); 
			bit 		+= extract_bit(sequence * QUANTIZATION, position);
			bit 		= bit * .25 + bit * sequence_color * .75;
			
			gl_FragColor 	= vec4(vec3(bit), 0.);
			return;	
		}
		if(uv.y < .5)
		{
			float position = mod(floor(uv.x * 64.), 8.);
			gl_FragColor = vec4(vec3(extract_bit(FREQUENCY, position)), 0.);
			return;
		}
		
	}
	#ifdef MOORE
	vec2 neighbor_offset[8]; 
	neighbor_offset[0] = vec2(  0., -1. );
	neighbor_offset[1] = vec2( -1., -1. );	
    	neighbor_offset[2] = vec2( -1.,  0. );
	neighbor_offset[3] = vec2( -1.,  1. );
	neighbor_offset[4] = vec2(  0.,  1. );
	neighbor_offset[5] = vec2(  1.,  1. );
	neighbor_offset[6] = vec2(  1.,  0. );
	neighbor_offset[7] = vec2(  1., -1. );
	#endif

	#ifdef VON_NEUMANN
    	vec2 neighbor_offset[4]; 
	if(true)
	{
    		neighbor_offset[0] = vec2(  0., -1. );
    		neighbor_offset[1] = vec2( -1.,  0. );
		neighbor_offset[2] = vec2(  0.,  1. );
		neighbor_offset[3] = vec2(  1.,  0. );
	}
	else
	{
		neighbor_offset[0] = vec2( -1., -1. );	
		neighbor_offset[1] = vec2( -1.,  1. );
		neighbor_offset[2] = vec2(  1.,  1. );
		neighbor_offset[3] = vec2(  1., -1. );
	}
	#endif

	//check neighbors to see if any are angled to this current position
	vec4 cell = vec4( 0. );
	#ifdef MOORE
	const int iterations = 8;
	#endif
	
	#ifdef VON_NEUMANN
	const int iterations = 4;
	#endif 

	for ( int i = 0; i < iterations; i++ )
    	{
		//create neighbor cell position, offset and wrapped at the edges, normalized for sample hardware
		vec2 neighbor_uv 	= fract((gl_FragCoord.xy + neighbor_offset[i])/resolution);

		//get neighbor cell
		vec4 neighbor_cell 	= texture2D(renderbuffer, neighbor_uv); 
		float neighbor_angle 	= neighbor_cell.w;

		//check to see if there is an angle stored in cell.w
        	bool is_occupied	= neighbor_angle != 0.;
		
		if (is_occupied)
		{   
			float angle		= mod(neighbor_angle + sequence, 1.);
		    	
			//add it to pick between the two possible neighbor cells that the angle might enter
			#ifdef MOORE
			bool approaching        = floor(angle * 8.) == float(i);
                	#endif
			
			#ifdef VON_NEUMANN
			bool approaching        = floor(angle * 4.) == float(i);      
			#endif
        	     
			if ( approaching ) 
			{
				cell 		= neighbor_cell;
				break;
			}
		}
        }

	//optional - sample prior cell for trails (and adding new automata)
	vec4 prior_cell		= texture2D(renderbuffer, uv);

	//show trails
	float trail_fade 	= .0;
	cell.xyz 		= cell.w != 0. ? sequence_color.xyz : prior_cell.xyz;

	//add new automata at the center of the screen, increment angle from previous center cell
    	bool is_center_pixel   	= floor(gl_FragCoord.x) == floor(resolution.x * .5) && floor(gl_FragCoord.y) == floor(resolution.y * .5);
	float initial_angle	= frame;
    	cell	 		+= is_center_pixel ? vec4(1., 1., 1., initial_angle) : vec4(0.);

	//reset on mouse in corner
	cell *= float( mouse.y > .1 );

	//stop at screen edges
	cell *= float(gl_FragCoord.x > 1. && gl_FragCoord.y > 1. && gl_FragCoord.x < resolution.x-1. && gl_FragCoord.y < resolution.y-1.);
	
	gl_FragColor = cell;
}//sphinx

vec3 hsv(in float h, in float s, in float v){
    return mix(vec3(1.),clamp((abs(fract(h+vec3(3.,2.,1.)/3.)*6.-3.)-1.),0.,1.),s)*v;
}

float extract_bit(float n, float b)
{
	n = floor(n);
	b = floor(b);
	b = floor(n/pow(2.,b));
	return float(mod(b, 2.) == 1.);
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