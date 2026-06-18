/*{
    "DESCRIPTION": "FractalSplat1",
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
        "fractal"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float extract_bit(float n, float b);
float fbm(vec2 p);
float irnd(vec2 p);
float rnd(vec2 p);
float interpolate(float a, float b, float x);

void main( void ) 
{
	vec3 noise_composition 	= vec3(0.);
	
	//rgb noise values to inspect
	vec3 noise		= vec3(fbm(gl_FragCoord.xy), fbm(-gl_FragCoord.xy), fbm(gl_FragCoord.yx));
	noise += vec3(sin(time / 100.0));
	noise 			*= noise;
	
	//now, get the same bit values, add them up and divide by eight to create a "composition"
	for(int i = 0;i < 8; i++)
	{
		noise_composition.x += extract_bit(noise.x*255., float(i))/8.;
		noise_composition.y += extract_bit(noise.y*255., float(i))/8.;
		noise_composition.z += extract_bit(noise.z*255., float(i))/8.;
	}	
	
	//compositing
	vec4 result	= vec4(0.);
	
	result.xyz	= noise_composition;
	result.w	= 1.;
	
	gl_FragColor	= result; 
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

