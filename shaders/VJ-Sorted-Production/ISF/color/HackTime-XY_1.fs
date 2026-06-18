/*{
    "DESCRIPTION": "HackTime-XY",
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
        "geometric"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// http://glslsandbox.com/e#25857.0
// MORE NEON HACK TIME FROM THE 80'S

#ifdef GL_ES
precision mediump float;
#endif

// YOU'RE ABOUT
// TO HACK TIME,
// ARE YOU SURE?
//  >YES   NO

void glow(float d) {
	float br = 0.005 * resolution.y;
	gl_FragColor.rgb += vec3(0.3, 0.15, 0.45) * br / d;
}

void line( vec2 a, vec2 l ) {
	l.x *= resolution.y/resolution.x;
	l += 0.5;
	l *= resolution;
	
	vec2 P = gl_FragCoord.xy;
	
	float angle = length(mouse)/10.0;
    	mat2 rot = mat2(cos(angle), -sin(angle),
                    sin(angle),  cos(angle));
	P = rot * P;
	
	a.x *= resolution.y/resolution.x;
	a += 0.5;
	a *= resolution;
	
	vec2 aP = P-a;
	vec2 al = l-a;
	vec3 al3 = vec3(al, 0.0);
	vec3 aP3 = vec3(aP, 0.0);
	//float q = length(dot(aP,al))/length(al);
	float q = length(cross(aP3,al3))/length(al3);
	
	float d = q;
	if ( dot(al, aP) <= 0.0 ) { // before start
               d = distance(P, a);
	}
        else if ( dot(al, al) <= dot(al, aP) ) { // after end
               d = distance(P, l);
	}
	glow(d);
}

void point(vec2 a) {
	a.x *= resolution.y/resolution.x;
	a += 0.5;
	a *= resolution;

	vec2 P = gl_FragCoord.xy;
	float d = distance(P, a);
	glow(d);
}

float rand(int seed) {
	return fract(sin(float(seed)*15.234234) + sin(float(seed)*4.3456342) * 372.4532);
}

void _userMain( void ) {
	gl_FragColor = vec4(0.0, 0.0, 0.0, 1.0);

	// Horizontal grid lines
	float y = 0.0;
	for (int l=1; l<13; l++) {
		y = -1.0/(0.6 * sin(time * 0.73) + float(l)*1.2) + 0.25;
		line(vec2(-2.0, y), vec2(2.0, y));
	}
	
	// Perpendicular grid lines
	for (int l=-30; l<31; l++) {
		float x = float(l) + fract(time * 3.25);
		line(vec2(x * 0.025, y), vec2(x, -1.0));
	}
	
	// Starfield
	for (int l=1; l<70; l++) {
		float sx = (fract(rand(l+342) + time * (0.002 + 0.01*rand(l)))-0.5) * 3.0;
		float sy = y + 0.4 * rand(l+8324);
		point(vec2(sx,sy));
	}
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