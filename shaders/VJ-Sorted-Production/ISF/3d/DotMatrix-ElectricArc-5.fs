/*{
    "DESCRIPTION": "DotMatrix-ElectricArc-5",
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
        "color",
        "geometric",
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

// my first raymarching \o/
// thanks to iq and his wonderful tools

// object transformation
vec3 rotateX(vec3 p, float phi) {
	float c = cos(phi);
	float s = sin(phi);
	return vec3(p.x, c*p.y - s*p.z, s*p.y + c*p.z);
}

vec3 rotateY(vec3 p, float phi) {
	float c = cos(phi);
	float s = sin(phi);
	return vec3(c*p.x + s*p.z, p.y, c*p.z - s*p.x);
}

vec3 rotateZ(vec3 p, float phi) {
	float c = cos(phi);
	float s = sin(phi);
	return vec3(c*p.x - s*p.y, s*p.x + c*p.y, p.z);
}

// ray marching objects
float obj_udRoundBox(vec3 p) {
	vec3 b = vec3(.3);
	p = rotateZ(rotateY(rotateX(p, 0.22*time), 0.33*time), 0.11*time);
	return length(max(abs(p)-b,0.0))-.01;
}

void _userMain(void) {
	vec2 q = gl_FragCoord.xy/max(resolution.x, resolution.y);
	vec2 vPos = 2.*q;
	vPos += vec2(-1., -.5);

	// Camera setup
	vec3 camUp = vec3(10.,10.,0.);
	vec3 camlookAt = vec3(0.);
	vec3 camPos = vec3(1.);
	vec3 camDir = normalize(camlookAt - camPos);
	vec3 u = normalize(cross(camUp, camDir));
	vec3 v = cross(camDir, u);
	vec3 vcv = camPos + camDir;
	vec3 scrCoord = vPos.x*u*1. + vPos.y*v*1.;
	vec3 scp = normalize(scrCoord - camPos);

	// Raymarching
	const vec3 e = vec3(0.0005, 0.005, 0.0005);
	const float maxd = 6.;
	float d = .05;
	vec3 p;

	float f = 0.5;
	for(int i = 0; i < 50; i++) {
	    	if ((abs(d) < .005) || (f > maxd)) break;
	    	f += d;
	    	p = vec3(2.) + scp*f;
	    	d = obj_udRoundBox(p);
	}
  
	if (f < maxd) { // cube
		vec3 col = vec3(abs(sin(time))*.2+.5, abs(sin(time-3.1416/8.))*.2+.5, abs(sin(time+3.1416/8.))*.2+.5);
		vec3 n = vec3(d - obj_udRoundBox(p - e.xyy), d - obj_udRoundBox(p - e.yxy), d - obj_udRoundBox(p - e.yyx));
		float b = dot(normalize(n), normalize(camPos - p));
		gl_FragColor=vec4((p*-b*col + pow(b, 16.))*(1. - f*.01), 1.);
	} else { // background, thanks to: http://glsl.heroku.com/e#15441.0
		vec2 uv = gl_FragCoord.xy/resolution.xy;
		vec3 c = vec3(sin(uv.x*5.-0.+time*1.), sin(uv.x*5.-4.0-time*1.), sin(uv.x*5.-4.+time*1.));
		float a = pow(sin(uv.x*3.1416),.9)*pow(sin(uv.y*3.1416),.9);
		gl_FragColor=mix(vec4(-normalize(p),1.0), vec4(c,1.), a);
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