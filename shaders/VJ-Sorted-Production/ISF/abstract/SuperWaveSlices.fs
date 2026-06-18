/*{
    "DESCRIPTION": "SuperWaveSlices",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        },
        {
            "NAME": "scale",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Scale"
        }
    ],
    "TAGS": [
        "abstract"
    ]
}*/
#define E 2.71828182846
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

// TODO - find/fix accuracy bug!
// mouse right of center to see problem areas in red
// alternative exploration http://glslsandbox.com/e#39962.0

#extension GL_OES_standard_derivatives : enable

uniform vec2 surfacePosition;

// ----------------------------------------------------

#if 1
  #define PI 3.14159265358979323
  #define HALFPI 1.57079632679
  #define TAU 6.28318530718
  #define PISQR 9.86960440109
#else
#endif

// ----------------------------------------------------

// https://en.wikipedia.org/wiki/Bhaskara_I%27s_sine_approximation_formula

const vec4 cv4PISQR = vec4(PISQR);
const vec4 cv4HALFPI = vec4(HALFPI,HALFPI,HALFPI,HALFPI);
const vec4 cv4HALFPI0 = vec4(HALFPI,0.0,HALFPI,0.0);
const vec4 cv4TAU = vec4(TAU);
const vec4 cv4PI = vec4(PI);

float bhaskara_cos_approximation(float x)
{
        x *= x; return (PISQR - 4.0*x) / (PISQR + x);
}

vec4 bhaskara_cos_approximation(vec4 x)
{
        x *= x; return ((vec4(cv4PISQR) - 4.0*x) / (vec4(cv4PISQR) + x));
}

// ----------------------------------------------------

float sin_approximation_b(float x);

vec4 bhaskara_coscoscoscos_approximation( vec4 xxxx )
{
	xxxx -= cv4HALFPI0;
        vec4 mp = mod(xxxx,cv4TAU);
	
	return 1.-mix( 1.-1./-bhaskara_cos_approximation((mp)), bhaskara_cos_approximation(mp), 1./smoothstep(-mp,mp,cv4PI) );
}

// these each have very interesting useful looking curves
//xxxx -= cv4HALFPI0; // + one of the next lines
//return mix( 1.-1./-bhaskara_cos_approximation(PI-(mp-PI)), 1./bhaskara_cos_approximation(mp), 1./smoothstep(-mp,mp,cv4PI) );
//return mix( 1.-1./-bhaskara_cos_approximation((mp-PI)), 1./bhaskara_cos_approximation(mp), 1./smoothstep(-mp,mp,cv4PI) );
//return 1.-mix( 1.-1./-bhaskara_cos_approximation((mp)), bhaskara_cos_approximation(mp), 1./smoothstep(-mp,mp,cv4PI) ); // when undersampled interesting

vec4 bhaskara_working_but_not_efficient_approximation( vec2 ab )
{
	// goal :-)
	return vec4( sin_approximation_b(ab.x), sin_approximation_b(ab.x+HALFPI), sin_approximation_b(ab.y), sin_approximation_b(ab.y+HALFPI) );
		
}

vec4 bhaskara_sinacosa_sinbcosb_approximation( vec2 ab )
{
	return bhaskara_working_but_not_efficient_approximation(ab);
//		vec2(sin_approximation_a(ab.x),vec2(sin(ab.y)) );
	// TODO - find/fix accuracy bug!
	
	return bhaskara_coscoscoscos_approximation( ab.xxyy - cv4HALFPI0 );
}

// ----------------------------------------------------

float sin_approximation_a(float x)
{
        return bhaskara_cos_approximation( x - HALFPI );
}

float sin_approximation_b(float x)
{
	// using mix instead of branch
	if ( 0.0 < cos(time * 2.0) ) {
		
		float mp = mod(x,TAU);
		return mix( -bhaskara_cos_approximation( PI-(mp-PI)-HALFPI), bhaskara_cos_approximation(mp-HALFPI), step(mp,PI) );
	}
	
	// using branch
        float mp = mod((x),TAU);
        if ( mp < PI) {
                return sin_approximation_a(mp);
        }
        return -sin_approximation_a(PI-(mp-PI));
}

// ----------------------------------------------------

float sin_approximation_c(float x)
{
        return cos(x-HALFPI);
}

// ----------------------------------------------------

float sin_approximation_(float x)
{
	float st = sin(time);
	if ( 0.0 > st ) return sin_approximation_b(x);
        //return sin_approximation_a(x);
        return sin_approximation_c(x);
}

float sin_approximation(float x)
{
        return (sin_approximation_(x));
}

float cos_approximation(float x)
{
        return sin_approximation(HALFPI-x);
}

float scaleAbsError(float e,float scale)
{
        return abs(e) * scale;
}

float range(float x) { return x*0.5+0.5; }
vec4 range(vec4 x) { return x*0.5+0.5; }

void mainImage( out vec4 fragColor, vec2 fragCoord )
{
        vec2 uv = ( fragCoord.xy / resolution.xy );
	
	float aspect = resolution.x / (resolution.x/resolution.y);
	uv.x *= aspect;

        fragColor = vec4(0.0);

        float errorScale = 40.0 + 20.0*cos(time*4.0);
	
	float tx = (uv.y * 2.0 - 1.0) * TAU * (8.0 + 6.5*cos(mouse.y*4.0));
        float c = range( cos(tx) );
        float s = range( sin(tx) );

        vec4 scsc = range( bhaskara_sinacosa_sinbcosb_approximation( vec2(tx,tx) ) );
        float se = scsc.x;
        float ce = scsc.y;
	
	if ( uv.y > 0.5 ) {
	
		if ( mouse.x < 0.5 ) {
			se = range( sin_approximation(tx) );
			ce = range( cos_approximation(tx) );
		}
	
		if ( uv.x < 0.5 ) {
	
			if ( uv.x < 0.25 ) {
	
				fragColor.xyz = vec3(0.0,s,0.0);
	
			} else if ( uv.x < 0.375 ) {
	
				fragColor.xyz = vec3(0.0,se,0.0);
	
			} else {
	
				fragColor.xyz = vec3(scaleAbsError(s-se,errorScale),0.0,0.0);
	
			}
	
		} else {
	
			if ( uv.x < 0.75 ) {
	
				fragColor.xyz = vec3(0.0,0.0,c);
	
			} else if ( uv.x < 0.875 ) {
	
				fragColor.xyz = vec3(0.0,0.0,ce);
	
			} else {
	
				fragColor.xyz = vec3(scaleAbsError(c-ce,errorScale),0.0,0.0);
	
			}
	
		}
		
	} else {
		
		uv.x = uv.x*2.0*aspect;
		uv.y = uv.y*2.0;//*1.0-.5;
		
		vec4 counts = vec4(0.0);
		
		const float samples = 2.0;
		float samplingDelta = 1./(resolution.y);
		
		vec2 sxx = vec2( uv.x * TAU * 3.0 * mouse.x, uv.x * TAU * 2.0 );
		
		vec4 base = bhaskara_sinacosa_sinbcosb_approximation(sxx);
		
		for ( float i = 0.0; i < samples; i++ ) {
			
			vec2 st = sxx+i*samplingDelta;
			
			vec4 samp = bhaskara_sinacosa_sinbcosb_approximation( st )*0.5+0.5;
			
			for ( float j = 0.0; j < samples; j++ ) {
				
				vec4 v = samp - (uv.y+j*samplingDelta);
				
				counts += step(0.0,v);
				
			}
		}
		
		fragColor.rgb = base.rgb;
		fragColor.gb = abs(counts.xy)/(samples*samples);
		fragColor.r = (mouse.x < 0.5) ? (base.x - fragColor.x) : (base.y - fragColor.y);
		
	}
	
	fragColor.a = 1.0;
	
}

void main( void )
{
        mainImage( gl_FragColor, gl_FragCoord.xy );
}

