/*{
    "DESCRIPTION": "DotMatrix-Zooming-45",
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
// author Sander from shadertoy
// gigatron for glslsandbox 
#ifdef GL_ES
precision mediump float;
#endif

const vec3 black = vec3(0);
const vec3 white = vec3(1);
const vec3 blue = vec3(0,0,192./255.);
const vec3 red = vec3(1,0,0);

bool cosign(vec2 t) {
	return (t.x > 0. && t.x < 49. && t.y > 0. && t.y < 20.) &&
		! (t.x > 26. && (t.x - 26.) > t.y);
}

vec3 commodore(vec2 p) {
	
	if(length(p) < 62.0 && length(p) > 34.0 && p.x < 17.0) {
		return blue;
	}
	
	vec2 t = p - vec2(20., 2.);
	if(cosign(t)) {
		return blue;
	}

	p.y *= -1.;
	
	vec2 t2 = p - vec2(20., 2.);
	if(cosign(t2)) {
		return red;
	}
	return white;
}

const float wave = 10.0;

void _userMain()
{
	vec3 color;
	
	vec2 p = (gl_FragCoord.xy/resolution.xy)-vec2(0.5);
	p.x *= resolution.x/resolution.y;

	float t = time * 2.0;
	
	vec2 zp = p *150.*mod(time,5.);
	
	vec2 displace = vec2( sin(t - (p.y*wave)), cos(t - (p.x*wave)) );
	zp += 5. * displace;
	
	color = commodore(zp);

	if(color == white) {
		float interlace = mod(gl_FragCoord.y,2.);
		color = mix(black, white, 0.5 + 0.5 * interlace);
	}
	
	// stolen from:
	// https://www.shadertoy.com/view/4djGz1
	vec2 uv = gl_FragCoord.xy / resolution.xy*2.-1.;
	color = mix(blue, color, 0.5 + pow(max(0.,1.0-length(uv*uv*uv*vec2(1.00,1.1))),1.));
	
	gl_FragColor = vec4(color, 1.);
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