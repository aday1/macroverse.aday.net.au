/*{
    "DESCRIPTION": "ShardMatrix50",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "3d"
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
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

//why is it magically red?

vec2 format(vec2 uv)
{
	uv 	= uv * 2. - 1.;
	uv.x	*= resolution.x/resolution.y;
	return uv;
}

float binary_code(vec2 uv, vec2 scale)
{
	uv 		+= vec2(-.5, .5)/scale;
	vec2 position	= uv * scale;
	position	= ceil(position);
	
	float bit 	= position.x;
	float exponent 	= pow(2., position.y);
	
	return mod(bit, exponent)/exponent;
}

float binary_fractal(vec2 uv, vec2 scale)
{
	uv 		+= vec2(.5, .5)/scale;
	vec2 position	= uv * scale;
	position	= (position);
	
	float bit 	= position.x;
	
	float code	= 0.;
	float a		= .5;
	for(int i = 0; i < 8; i++)
	{
		
		float exponent 	= pow(2., float(i));
		code		+= (mod(bit,exponent)/exponent)/pow(2., 8.-float(i));
	}
	return code;
}

float gray_code(vec2 uv, vec2 scale)
{
	uv 		+= vec2(-.5, .5)/scale;
	vec2 position	= uv * scale;
	position	= ceil(position);
	
	float bit 	= position.x;
	float exponent 	= pow(2., position.y);
	float shift	= pow(2., position.y+1.);
	
	return mod(bit+exponent/2., shift)/(exponent*2.);
}

float gray_fractal(vec2 uv, vec2 scale)
{
	uv 		+= vec2(.5, .5)/scale;
	vec2 position	= uv * scale;
	position	= (position);
	
	float bit 	= position.x;
	
	float code	= 0.;
	for(int i = 0; i < 8; i++)
	{
		float exponent 	= pow(2., float(i));
		float shift	= pow(2., float(i)+1.);
		code		+= (mod(bit + exponent/2., shift)/(exponent*2.))/pow(2., 8.-float(i));
	}
	return code;
}

void main( void ) 
{
	vec2 uv 	= gl_FragCoord.xy/resolution.xy;

	bool top	= uv.y > .5;
	uv.y		= fract(uv.y * 2.);
	
	vec2 scale	= vec2(256., 7.5);

	vec4 binary 	= vec4(0.);
	binary.x 	= binary_code(uv.xy, scale);
	binary.y 	= binary_code(uv.yx, scale);
	binary.z 	= binary_fractal(uv.xy, scale);
	binary.w 	= binary_fractal(uv.yx, scale);
	
	vec4 gray	= vec4(0.);
	gray.x		= gray_code(uv.xy, scale);
	gray.y		= gray_code(uv.yx, scale);
	gray.z		= gray_fractal(uv.xy, scale);
	gray.w		= gray_fractal(uv.yx, scale);

	float fractal	= top ? binary.z : gray.z;
	fractal		= float(fractal > uv.y) * fractal;
	
	float code	= top ? binary.x : gray.x;
	code		= float(code >= .5) * code;

	vec4 result 	= vec4(0.);
	result		+= fractal 	* .5;
	result		+= code 	* .5;
	result.w	= 1.;
	
	gl_FragColor 	= result;
}//sphinx
