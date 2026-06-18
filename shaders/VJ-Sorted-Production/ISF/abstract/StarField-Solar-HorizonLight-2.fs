/*{
    "DESCRIPTION": "StarField-Solar-HorizonLight-2",
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
        }
    ],
    "TAGS": [
        "abstract",
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
/**
* Flaw's GLSL test bed.
*/

#define GAP 3.1			// the coordinates gap for all squares
#define PI 3.14159265359
#define COUNT 4.0		// number of squares - in width if you use resolution.x, in height if you use resolution.y -

#ifdef GL_ES
precision highp float;
#endif

/***** VARIABLES *****/

	// screen size
		// delta ?

/***** PROTOTYPES *****/

float func (float f);
float func (vec2);
vec4 horizontalGradient (vec4 startColor, vec4 endColor, vec4 point);
float linearInterpolation (float x1, float y1, float x2, float y2, float x);
float rand (vec2 co);
float sine(float var, float var2, float count);
vec4 solid(float color);
vec4 solid(vec4 color);
vec4 verticalGradient(vec4 startColor, vec4 endColor, vec4 point);

/***** FUNCTIONS *****/

/**
* Returns the color for 'point' to be part of the gradient (startColor, endColor).
*/
vec4 horizontalGradient(vec4 startColor, vec4 endColor, vec4 point) {
	return vec4(
		linearInterpolation(0.0, startColor.x, resolution.x, endColor.x, point.x),
		linearInterpolation(0.0, startColor.y, resolution.x, endColor.y, point.x),
		linearInterpolation(0.0, startColor.z, resolution.x, endColor.z, point.x),
		linearInterpolation(0.0, startColor.w, resolution.x, endColor.w, point.x)
	);
}

/**
* Returns the interpolation value 'y' from the parameter value 'x', which can be a duration (delta), screen coordinates ...
* The formula comes from Wikipedia : http://en.wikipedia.org/wiki/Linear_interpolation
* 
* We know that :
* - f(x1) = y1
* - f(x2) = y2
*
* So using the formula from Wikipedia we can compute the weighted mean 'f(x)' which will then be returned.
*/
float linearInterpolation(float x1, float y1, float x2, float y2, float x) {
	return (((x2 - x) * y1) / (x2 - x1)) + (((x - x1) * y2) / (x2 - x1));
}

/**
* Returns a pseudo-random number. Formula found on StackOverflow: http://stackoverflow.com/questions/4200224/random-noise-functions-for-glsl
*/
float rand(vec2 co) {
    return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

/**
* Sine wave sinusoid : http://en.wikipedia.org/wiki/Sine_wave
*/
float sine(float var, float var2, float count) {
	return (1.0 * sin(2.0 * PI * (count / 2.0 / var2) * var + GAP));
}

/**
* Returns a color with all values set to 'color'.
*/
vec4 solid(float color) {
	return vec4(color);
}

/**
* Returns nothing else than 'color' itself.
*/
vec4 solid(vec4 color) {
	return color;
}

/**
* Returns the color for 'point' to be part of the gradient (startColor, endColor).
*/
vec4 verticalGradient(vec4 startColor, vec4 endColor, vec4 point) {
	return vec4(
		linearInterpolation(0.0, startColor.x, resolution.y, endColor.x, point.y),
		linearInterpolation(0.0, startColor.y, resolution.y, endColor.y, point.y),
		linearInterpolation(0.0, startColor.z, resolution.y, endColor.z, point.y),
		linearInterpolation(0.0, startColor.w, resolution.y, endColor.w, point.y)
	);
}

void _userMain( void ) {
	/***** TV noise *****/
	/*
	gl_FragColor = vec4(rand(gl_FragCoord.xy / time));
	*/
	
	/***** Horizontal gradient *****/
	/*
	gl_FragColor = horizontalGradient(
		vec4(1.0, 0.0, 0.0, 1.0),
		vec4(0.0, 0.0, 1.0, 0.0),
		gl_FragCoord
	);
	*/
	
	/***** Vertical gradient *****/
	/*
	gl_FragColor = verticalGradient(
		vec4(1.0, 1.0, 0.0, 1.0),
		vec4(0.8, 0.0, 0.6, 0.0),
		gl_FragCoord
	);
	*/
	
	/***** Gray *****/
	/*
	gl_FragColor = solid(0.7);
	*/
	
	/***** Multicolor *****/
	/*
	gl_FragColor = vec4(cos(time),
			    cos(time * 0.5),
			    cos(time * 1.0),
			    cos(time * 0.75));
	*/
	
	/***** Checkerboard *****/
	///*
	gl_FragColor = vec4(sine(gl_FragCoord.x, resolution.x, COUNT)
			   * sine(gl_FragCoord.y, resolution.x, COUNT)); // black & white - not animated
	//gl_FragColor.x *= cos(time); // cyan & red - animate color
	gl_FragColor.y *= cos(time); // purple & green - animate color
	//gl_FragColor.z *= cos(time); // yellow & blue - animate color
	gl_FragColor *= cos(time); // animate alpha
	//*/
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