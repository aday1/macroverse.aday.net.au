/*{
    "DESCRIPTION": "DotMatrix-Zooming-76",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "color"
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
        "color"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
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

void _userMain( void ) 
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