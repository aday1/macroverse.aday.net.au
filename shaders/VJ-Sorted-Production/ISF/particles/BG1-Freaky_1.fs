/*{
    "DESCRIPTION": "BG1-Freaky",
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
        }
    ],
    "TAGS": [
        "particles"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE

#extension GL_OES_standard_derivatives : enable

//synthetic aperture, aka beamforming

//cribbed from this : https://www.shadertoy.com/view/ldlSzX

#define EMITTERS 		32
#define SCALE			32.
#define WAVELENGTH		4.
#define AFFINE			cos(time)+1.5
#define VELOCITY		.25
#define CONFIGURATION		1.//floor(mod(time/8., 2.))
#define AMPLITUDE		.5
#define RADIUS			32.
#define TAU (8.*atan(1.))

float minkowski(vec2 a, vec2 b, float p) 
{
	return pow(
		pow(abs(a.x - b.x),p) 
	      + pow(abs(a.y - b.y),p)
	   	,1./p);
}

mat2 rmat(float t)
{
	float c = cos(t);
	float s = sin(t);
	
	return mat2(c, s, -s ,c);
}

float wave(float x)
{
	bool p  = fract(x*.5)<.5;
	x	= fract(x)*2.;
	x 	*= 2.-x;
	x 	*= 1.-abs(1.-x)*.25;
	return  p ? x : -x;
}

void main( void ) 
{
	float scale		= SCALE;
	vec2 aspect		= resolution/min(resolution.x, resolution.y);
	vec2 uv			= gl_FragCoord.xy/resolution;
	vec2 position		= (uv * 2. - 1.) * aspect * scale;
	vec2 mpos = (mouse * 2. - 1.) * aspect * scale;

	vec2 target		= mpos;
	
	float wavelength 	= WAVELENGTH;
	float velocity		= VELOCITY;
	float radius		= RADIUS;
	float affine		= AFFINE;
	float energy		= 0.;
	for(int i = 0; i < EMITTERS; i++)
	{
		float interval	= float(EMITTERS-i)/float(EMITTERS);
		
		vec2 source 	= vec2(0.);
		source 		= CONFIGURATION == 0. ? vec2((interval*radius/4.-radius/8.)*2., -scale*.75) 	: source;		
		source 		= CONFIGURATION == 1. ? vec2(radius, 0.) * rmat(TAU * interval) 			: source;
		
		float theta  	= minkowski(source, position, affine);	
		float range 	= minkowski(source, target,   affine);
		
	        float shift  	= theta - velocity * time;
		float phase 	= wave(affine * wavelength * (shift - range));
//		float amplitude 	= pow(theta, sqrt(abs(theta-range)))*AMPLITUDE;
		float amplitude 	= theta*AMPLITUDE;

		energy 		+= phase/amplitude;
	}

	vec4 result	= vec4(0.);

	result.x		= energy;
	result.z		= 1.-energy;
	result 		*= abs(energy);
	
	result.w		= 1.;
	
	gl_FragColor	= result;
}//sphinx

