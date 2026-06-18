/*{
    "DESCRIPTION": "FrostCrystal-DotMatrix-17",
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
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision highp float;
#endif

//max at medium precision - breaks in bits (should be all 1's), but good up to this point
#define NUMBER pow(2., 24.)-1. 

//binary and gray code display

//mouse left  	: binary
//mouse right 	: gray code

//red   > .5
//blue  < .5
//green = .5

//anything below .5 is considered a 1.
//values above .5 shoot to infinity pretty quick (the red stuff)

//text display functions
float extract_bit(float n, float b);
float sprite(float n, vec2 s, vec2 p);
float digit(float n, vec2 s, vec2 p);
	
//characters 
float c_0 = 31599.;
float c_1 =  9362.;
float c_2 = 29671.;
float c_3 = 29391.;
float c_4 = 23497.;
float c_5 = 31183.;
float c_6 = 31215.;
float c_7 = 29257.;
float c_8 = 31727.;
float c_9 = 31695.;

void _userMain( void ) 
{
	////
	//display formatting
	////
	vec2 uv 	= gl_FragCoord.xy/resolution.xy;
	vec2 scale	= vec2(92., 32.);
	vec2 offset	= vec2(-64., 0.);	
	vec2 position	= floor(uv * scale + offset);	
	
	bool mouse_left = mouse.x < .5;
	
	////
	//code creation stuff
	//
	//anything resulting from the code function that's <= .5 counts as a 1, otherwise the position is a 0
	//just made this stuff up to draw the picture - seems to work - not sure why
	//really looks like it could be refactored (see: extract_bit below) but drawing them like this is nice too
	////
	float number 	= floor(position.y) + floor((mouse.y-.5)*255.) + NUMBER;
	float exponent	= floor(position.x);	
	float type	= mouse_left ? 1. : 2.;

	//this can prolly be refactored...
	float code 	= .5/(mod(number + pow(type, exponent) + type-2., pow(2., exponent + type))/pow(2., exponent)/type);
	code 		= code;

	////
	//everything beyond this is just display
	////
	float code_high    	= float(code  > .5);
	float code_half     	= float(code == .5);
	float code_low     	= float(code  < .5);
	float code_floor	= float(code <= .5);

	//setup text 
	vec2 sprite_scale	= vec2(3., 5.);		
	vec2 char_scale		= vec2(4., 8.);
	vec2 char_position 	= (vec2(uv.x, clamp(uv.y + floor(mouse.y * 192.), 0., 255.)) * scale + offset) * char_scale;
	vec2 char_offset	= vec2(4., 0.);
	
	char_position.y	 	= mod(char_position.y, char_scale.y) - 1.;

	//write base 10 digits
	float decimal_value	= number;
	float decimal_digits 	= 0.;
	for(int i = 0; i < 8; i++)
	{
		if(pow(2., float(i)) <= decimal_value + 1. || number > pow(2., 23.))
		{
			char_offset.x		+= 4.;
			decimal_digits		+= digit(decimal_value, sprite_scale, char_position + char_offset);
			decimal_value		/= 10.;
		}
	}

	//write binary digits
	char_position.x	 	= mod(char_position.x, char_scale.x);
	float code_bit_char	= code_floor == 0. ? c_0 : c_1;
	float code_digits	= sprite(code_bit_char, vec2(3., 5.), char_position);
	float code_digits_mask	= float(position.x > -1. && position.x < 25.) * float(number>=0.);	

	//composite results
	vec4 result 		= vec4(0.,0.,0.,1.);
	result 			+= code_floor * .5;
	result			= mouse.x < .125 || mouse.x > .875 ? result * .5 + .5 * (vec4(code_high, code_half, code_low, 1.)) : result;
	result 			+= code_digits * .125;
	result			+= float(number == NUMBER)*.25;
	result 			*= code_digits_mask;
	result 			+= decimal_digits;
	gl_FragColor = result;
}//sphinx

float extract_bit(float n, float b)
{
	n = floor(n);
	b = floor(b);
	b = floor(n/pow(2.,b));
	return float(mod(b,2.) == 1.);
}

float sprite(float n, vec2 s, vec2 p)
{
	p = floor(p);
	float bounds = float(all(lessThan(p,s)) && all(greaterThanEqual(p,vec2(0,0))));
	return extract_bit(n,(2.0 - p.x) + 3.0 * p.y) * bounds;
}

float digit(float num, vec2 s, vec2 p)
{
	num = mod(floor(num),10.0);
	
	if(num == 0.0) return sprite(c_0, s, p);
	if(num == 1.0) return sprite(c_1, s, p);
	if(num == 2.0) return sprite(c_2, s, p);
	if(num == 3.0) return sprite(c_3, s, p);
	if(num == 4.0) return sprite(c_4, s, p);
	if(num == 5.0) return sprite(c_5, s, p);
	if(num == 6.0) return sprite(c_6, s, p);
	if(num == 7.0) return sprite(c_7, s, p);
	if(num == 8.0) return sprite(c_8, s, p);
	if(num == 9.0) return sprite(c_9, s, p);
	
	return 0.0;
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