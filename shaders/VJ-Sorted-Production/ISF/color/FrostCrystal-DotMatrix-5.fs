/*{
    "DESCRIPTION": "FrostCrystal-DotMatrix-5",
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

//  This program is free software. It comes without any warranty, to
// the extent permitted by applicable law. You can redistribute it
// and/or modify it under the terms of the Do What The Fuck You Want
// To Public License, Version 2, as published by Sam Hocevar. See
// http://sam.zoy.org/wtfpl/COPYING for more details.

#define PI (3.14159265358979323)

#define FLOAT_BIT 40.0
#define EPS pow(2.0, -FLOAT_BIT)

vec2 add(vec2 a, vec2 b)
{
	float c = a.y + b.y;
	float d = mod(c, 1.0);
	return vec2(a.x + b.x + EPS * (c - d), d);
}

vec2 mul(vec2 a, vec2 b)
{
	float c = a.x * b.y + a.y * b.x;
	float d = mod(c, 1.0);
	return vec2(a.x * b.x + EPS * (c - d), d);
}

vec2 div_f(vec2 a, float b)
{
	return vec2(a.x/b , a.y);
	float c = (a.x / EPS + a.y) / b;
	float d = mod(c, 1.0);
	return vec2(c - d, 0.0);
}

vec2 new(float a)
{
	return vec2(a, 0.0);
}

void _userMain( void )
{
	vec2 position = ( gl_FragCoord.xy / resolution.xy ) - 0.5;
	float hue = 0.0;
	vec2 center = vec2(0.0, 1.0);
	float zoom = exp((sin(time / 2.0) + 1.0) * 8.0);
	vec2 c_x = add(div_f(new(position.x), zoom), new(center.x));
	vec2 c_y = add(div_f(new(position.y), zoom), new(center.y));
	vec2 z_x = new(0.0);
	vec2 z_y = new(0.0);
	bool broken = false;
	for(int lvl = 0; lvl < 40; lvl++)
	{
		if(add(mul(z_x, z_x), mul(z_y, z_y)).x > 4.0)
		{
			broken = true;
			break;
		}
		vec2 n_x = add(add(mul(z_x, z_x), -mul(z_y, z_y)), c_x);
		z_y = mul(z_x, z_y);
		z_y = add(add(z_y, z_y), c_y);
		z_x = n_x;
		hue += 0.4;
	}
	if(broken)
		gl_FragColor = vec4(sin(hue) + 0.5, sin(hue + PI / 1.5) + 0.5, sin(hue + PI / 0.75) + 0.5, 1.0);
	else
		gl_FragColor = vec4(0.0, 0.0, 0.0, 1.0);
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