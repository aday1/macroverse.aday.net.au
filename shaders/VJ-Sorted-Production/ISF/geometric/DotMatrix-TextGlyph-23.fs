/*{
    "DESCRIPTION": "DotMatrix-TextGlyph-23",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "geometric"
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
        "color",
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

float To16Bit(float a)
{
	return a - mod(a,0.0625);
	
}

// triangle check nicked from http://www.blackpawn.com/texts/pointinpoly/

bool SameSide(vec3 p1, vec3 p2, vec3 a, vec3 b)
{
    vec3 cp1 = cross(b-a, p1-a) ;
    vec3 cp2 = cross(b-a, p2-a) ;
    if (dot(cp1, cp2) >= 0.0)
	{
		return true;
	}
    return false;
}

bool PointInTriangle(vec3 p, vec3 a,vec3 b, vec3 c)
{
    if (SameSide(p,a, b,c) && SameSide(p,b, a,c) && SameSide(p,c, a,b) )
	{
		return true;
	}
    return false;
}

void _userMain( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.yy );//+ mouse / 4.0;
	vec3 colour = vec3( 1.2 - To16Bit(position.y/0.7), 1.2 - To16Bit(position.y/0.7), 0.8);

	if (position.y < 0.6)
	{
		colour.r = 1.0;
		colour.g = 1.0;
	}

	vec3 top = vec3(0.5, 0.8, 0.0);
	vec3 bottom [4];
	
	for (int i=0; i<4; i++)
	{
		bottom[i].x = 0.5 + 0.4*sin( time + float(i) * 3.14159/2.0);
		bottom[i].y = 0.3 + 0.05*cos( time + float(i) * 3.14159/2.0);
		bottom[i].z = 0.0;
	}
	
	for (int i=0; i<4; i++)
	{
		vec3 p1, p2;
		
		p1 = bottom[i];
		if (i==3)
		{
			p2 = bottom[0];	
		}
		else
		{
			p2 = bottom[i+1];
		}
		
		if (cross(top-p1, top-p2).z<0.0)
		{
			if (PointInTriangle(vec3(position,0.0), top, p1, p2))
			{
				colour = vec3(p1.x*1.2,p1.x*1.2,0.0);
			}
		}
	}

	gl_FragColor = vec4( colour, 1.0 );

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