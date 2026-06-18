/*{
    "DESCRIPTION": "CityScape-ParticleField-2",
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
        "3d",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE

#extension GL_OES_standard_derivatives : enable

//synthetic aperture, aka beamforming

//cribbed from this : https://www.shadertoy.com/view/ldlSzX

uniform sampler2D renderbuffer;

#define EMITTERS 		24
#define SCALE			32.
#define WAVELENGTH		2.
#define VELOCITY		1.
#define CONFIGURATION		1.//floor(mod(time/8., 2.))
#define AMPLITUDE		1.
#define RADIUS			32.
#define TAU (8.*atan(1.))

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

float cube(vec2 position, vec2 scale)
{
	vec2 vertex 	= abs(position) - scale;
	vec2 edge 	= max(vertex, 0.);
	float interior	= max(vertex.x, vertex.y);
	return min(interior, 0.) + length(edge);
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
	
	float energy		= 0.;
	for(int i = 0; i < EMITTERS; i++)
	{
		float interval	= float(EMITTERS-i)/float(EMITTERS);
		
		vec2 source 	= vec2(0.);
		source 		= CONFIGURATION == 0. ? vec2((interval*radius/4.-radius/8.)*2., -scale*.75) 	: source;		
		source 		= CONFIGURATION == 1. ? vec2(radius, 0.) * rmat(TAU * interval) 		: source;
		
		float theta  	= distance(source, position);	
		float range 	= cube(source-target, vec2(8.));

	        float shift  	= theta - velocity * time;
		float phase 	= wave(1. * wavelength * (shift - range));
		float amplitude 	= pow(theta, .25*sqrt(abs(theta-range)))*AMPLITUDE;
	
		energy 		+= phase/amplitude;
	}
	
	vec4 buffer	= texture2D(renderbuffer, uv + vec2(.35/resolution.x, 0.)*rmat(energy*3123126.28));
	vec4 result	= vec4(0.);

	result.x		= energy;
	result.z		= 1.-energy;
	result 		*= abs(energy)/4.;
	//result 		= buffer+result*.1;
	result		+= buffer*.65;
	result		= mix(result, buffer, .73);
	result.w		= 1.;
	
	gl_FragColor	= result;
}//sphinx
