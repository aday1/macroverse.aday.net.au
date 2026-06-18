/*{
    "DESCRIPTION": "DotMatrix-TextGlyph-7",
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
        "color",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// Text - @h3r3

#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D backbuffer;

bool rect(in vec2 pos, in vec2 upperLeft, in vec2 lowerRight) {
	return pos.x > upperLeft.x && pos.y > upperLeft.y && pos.x < lowerRight.x && pos.y < lowerRight.y;
}

bool tria(in vec2 pos, in vec2 a, in vec2 b, in vec2 c) {
	vec2 v0 = c - a, v1 = b - a, v2 = pos - a;
	float dot00 = dot(v0, v0), dot01 = dot(v0, v1), dot02 = dot(v0, v2), dot11 = dot(v1, v1), dot12 = dot(v1, v2);
	float invDenom = 1. / (dot00 * dot11 - dot01 * dot01);
	float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
	float v = (dot00 * dot12 - dot01 * dot02) * invDenom;
	return (u >= .0) && (v >= .0) && (u + v < 1.);
}

bool letterH(in vec2 pos) {
	return pos.x>.0 && pos.x<.6 && pos.y>.0 && pos.y<1.
		&& rect(pos, vec2(.0), vec2(.2,1.))
		|| rect(pos, vec2(.2,.4), vec2(.4,.6))
		|| rect(pos, vec2(.4,.0), vec2(.6,.5))
		|| tria(pos, vec2(.4,.5), vec2(.6,.5), vec2(.4,.6));
}

bool letter3(in vec2 pos) {
	return pos.x>.0 && pos.x<.6 && pos.y>.0 && pos.y<1.
		&& rect(pos, vec2(.0), vec2(.4, .2))
		|| rect(pos, vec2(.0,.8), vec2(.4, 1.))
		|| rect(pos, vec2(.2,.4), vec2(.4,.6))
		|| (pos.x <.6
		    && tria(pos, vec2(.4,.0), vec2(.4,1.), vec2(1.4,.5))
		    && !tria(pos, vec2(.6,.6), vec2(.6,.4), vec2(.5,.5)));
}

bool letterR(in vec2 pos) {
	return pos.x>.0 && pos.x<.6 && pos.y>.0 && pos.y<1.
		&& rect(pos, vec2(.0), vec2(.2, 1.))
		|| (pos.x > .2
		    && pos.x < .6
		    && tria(pos, vec2(-.2,.6), vec2(.4,.9), vec2(1.,.6))
		    && !tria(pos, vec2(.2,.6), vec2(.4,.7), vec2(.6,.6)));
}

void _userMain()
{
	vec3 color;
	vec2 p = vec2(gl_FragCoord.x - resolution.x/2., gl_FragCoord.y - resolution.y/2.) / resolution.y;
	p = p * (.5 + 1.0) * 164. + .5;
	float alpha = .1;
	for (float i = .0; i < 10.; i += 1.) {
		if (i > 8.5) { alpha = .9; }
		p.x += cos(time) * .3 + .4;
		p.y += sin(time) * .2 + .4;
		p *= cos(time * .5) * .05 + 0.6;
		if (letterH(vec2(p.x+1.,p.y))
		    || letter3(vec2(p.x+.2,p.y))
		    || letterR(vec2(p.x-.6,p.y))
		    || letter3(vec2(p.x-1.4,p.y))) { 
			color += alpha;
		}
	}
	gl_FragColor = vec4(color, 1.0);
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