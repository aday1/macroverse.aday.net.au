/*{
    "DESCRIPTION": "FrostCrystal-DotMatrix",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "noise"
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
        "geometric",
        "noise",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D renderbuffer;

float extract_bit(float n, float b);
float fbm(vec2 p);
float irnd(vec2 p);
float rnd(vec2 p);
float interpolate(float a, float b, float x);

void main( void ) 
{	
	vec2 uv			= gl_FragCoord.xy/resolution;
	
	bool mouse_debug	= mouse.x < .5;
	bool far_left		= uv.x < .5;
	bool left_side		= uv.x < .75;
	bool bit_display	= uv.x > .875; 
	bool sieve_display	= bit_display && uv.y < 1./128.; 
	
	vec2 position		= left_side ? gl_FragCoord.yx : vec2(gl_FragCoord.y, resolution.x * .75);
	position		= floor((position+vec2(time*32., 0.))/2.)*2.;
	position 		+= vec2(0., 0.);

	float levels		= pow(2., 8.);
	
	float signal		= mouse_debug ? fbm(position) : uv.y;		
	
	signal			= floor(signal*levels)/levels;
	
	float bits		= 0.;
	float sieve		= floor(mouse.y * levels);

	bool aligned		= true;
	for(float i = 0.; i < 8.; i++)
	{
		float bit 	= extract_bit(signal * levels, float(i));	
		float target 	= extract_bit(sieve, float(i));	
		bool match	= bit == target;
		aligned		= match && aligned;
	}

	float x		= floor((1.-uv.x)*levels)*.25;
	if(bit_display)
	{
		bits 	*= 0.;
		bits 	= extract_bit(floor(signal * levels), x)*.5;	
		sieve 	= extract_bit(          floor(sieve), x);
	}

	vec4 result		= vec4(0.);
	result.xyz		+= bit_display ? 	            0. : signal;
	result.xyz		+= bit_display ?   	          bits : 0.;
	result.xy		+= bit_display ? 	  sieve * .125 : 0.;
	result.xy		+= sieve_display ? 	   vec2(sieve) : vec2(0.);
	result.xy		+= float(aligned);
	result.w		= 1.;
	
	gl_FragColor		= result;
}//sphinx

//used for generating noisy input for testing
const int oct = 8;
const float per = 0.5;
const float PI = 3.1415926;
const float cCorners = 1.0/16.0;
const float cSides = 1.0/8.0;
const float cCenter = 1.0/4.0;

//interpolates a and b across x using a cosine curve
float interpolate(float a, float b, float x){
	float f = (1.0 - cos(x*PI))*0.5;
	return a * (1.0 - f) + b * f;
}

//returns a random number
float rnd(vec2 p){
	return fract(sin(dot(p, vec2(12.9898, 78.233)))*43758.5453);
}

//generates a randomized set of values for lattice points and the domain
float irnd(vec2 p){
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec4 v = vec4(rnd(vec2(i.x, i.y)),
		     rnd(vec2(i.x+1.0, i.y)),
		     rnd(vec2(i.x, i.y+1.0)),
		     rnd(vec2(i.x+1.0, i.y+1.0)));
	return interpolate(interpolate(v.x, v.y, f.x), interpolate(v.z, v.w, f.x), f.y);
}

//fractal harmonic brownian motion - pink spectrum
float fbm(vec2 p){
	float t = 0.0;
	float b	= 0.;
	for(int i = 0; i < oct; i++){
		float freq = pow(2.0, float(i));
		float amp = pow(per, float(oct-i));
		t += irnd(vec2(p.x/freq, p.y/freq))*amp;

	}

	return t;
}

float extract_bit(float n, float b)
{
	n = floor(n);
	b = floor(b);
	b = floor(n/pow(2.,b));
	return float(mod(b,2.) == 1.);
}
