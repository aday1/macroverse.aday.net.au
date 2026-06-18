/*{
    "DESCRIPTION": "CombinatorSineFX",
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

uniform vec4 color;
uniform float colorB;
uniform float colorG;
uniform float colorR;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision highp float;
#endif

// TODO - simplify - there are extra operations going on

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
const vec4 negcv4HALFPI = vec4(-HALFPI,-HALFPI,-HALFPI,-HALFPI);
const vec4 cv4HALFPI0 = vec4(HALFPI,0.0,HALFPI,0.0);
const vec4 cv4TAU = vec4(TAU);
const vec4 cv4PI = vec4(PI);

float bhaskara_cos_approximation(float x)
{
        x *= x; return (PISQR - 4.0*x) / (PISQR + x);
}

vec4 bhaskara_cos_approximation(vec4 x)
{
        x *= x; return (cv4PISQR - 4.0*x) / (cv4PISQR + x);
}

// ----------------------------------------------------

float sin_approximation(float x);
float cos_approximation(float x);

vec4 bhaskara_goal_approximation( vec2 ab )
{
	return vec4( sin_approximation(ab.x), cos_approximation(ab.x), sin_approximation(ab.y), cos_approximation(ab.y) );
}

vec4 bhaskara_coscoscoscos_approximation( vec4 xxxx )
{
        vec4 mp = mod(xxxx,cv4TAU);
	
	return mix( -bhaskara_cos_approximation((cv4TAU-HALFPI)-mp), bhaskara_cos_approximation(mp-HALFPI), step(mp,cv4PI) );
}

vec4 bhaskara_sinacosa_sinbcosb_approximation( vec2 ab )
{
	//return bhaskara_goal_approximation( ab ); // force it to use the 1 at a time approximation
	
	// TODO - simplify - there are extra operations going on
	
#if 0
	
	vec4 abab = vec4( ab.x, HALFPI-ab.x, ab.y, HALFPI-ab.y );
	return bhaskara_coscoscoscos_approximation( abab );
	
#elif 0
	
	vec4 abab = vec4( ab.x, HALFPI-ab.x, ab.y, HALFPI-ab.y );
        vec4 mp = mod(abab,cv4TAU);
	return mix( -bhaskara_cos_approximation((cv4TAU-HALFPI)-mp), bhaskara_cos_approximation(mp-HALFPI), step(mp,cv4PI) );
	
#else
	
	vec4 mp = mod( vec4( ab.x, HALFPI-ab.x, ab.y, HALFPI-ab.y ),TAU);
	vec4 a = (TAU-HALFPI)-mp;
	vec4 b = mp-HALFPI;
	a *= a;
	b *= b;
	return mix( -((PISQR - 4.0*a) / (PISQR + a)), (PISQR - 4.0*b) / (PISQR + b), step(mp,cv4PI) );
	
#endif
	
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
		return mix( -bhaskara_cos_approximation(TAU-mp-HALFPI), bhaskara_cos_approximation(mp-HALFPI), step(mp,PI) );
	}
	
	// using branch
        float mp = mod((x),TAU);
        if ( mp < PI) {
                return sin_approximation_a(mp);
        }
        return -sin_approximation_a(TAU-mp);
}

// ----------------------------------------------------

float sin_approximation_c(float x)
{
        return cos(HALFPI-x);
}

// ----------------------------------------------------

float sin_approximation_(float x)
{
	float st = sin(time);
	if ( 0.0 > st ) return sin_approximation_b(x);
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
	
	float aspect = ((resolution.x/resolution.x));
	uv.x /= aspect;

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
		
		uv.x = uv.x*(mouse.x*2.0-1.0)*2.0/aspect;
		uv.y = uv.y*2.0;
		
		vec4 counts = vec4(0.0);
		
		const float samples = 4.0;
		float samplingDelta = 1./(resolution.y);
		
		vec2 sxx = vec2( uv.x * TAU, uv.x * TAU * 2.0 );
		
		vec4 base = bhaskara_goal_approximation(sxx);
		
		for ( float i = 0.0; i < samples; i++ ) {
			
			vec2 st = sxx+i*samplingDelta;
			
			vec4 samp = bhaskara_sinacosa_sinbcosb_approximation( st )*0.5+0.5;
			
			for ( float j = 0.0; j < samples; j++ ) {
				
				vec4 v = samp - (uv.y+j*samplingDelta);
				
				counts += step(vec4(0.0),v);
				
			}
		}
		
		//fragColor.rgb = base.rgb;
		fragColor.gb = abs(counts.xy)/(samples*samples);
		fragColor.r = 0.0;//abs( (mouse.x < 0.5) ? (base.x - fragColor.x) : (base.y - fragColor.y) );
		
	}
	
	fragColor.a = 1.0;
	
}

void _userMain( void )
{
        mainImage( gl_FragColor, gl_FragCoord.xy );
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