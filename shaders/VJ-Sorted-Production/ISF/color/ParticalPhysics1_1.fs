/*{
    "DESCRIPTION": "ParticalPhysics1",
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
precision highp float;
#endif

float  iGlobalTime;

#define ITEMAX 75

float QuadSphere(vec3 p, vec3 pos, vec3 size, float radius) {
	return length(max(abs(p - pos), 0.0) - size) - radius;
}

vec2 rot(vec2 p, float r) {
	float c = cos(r), s = sin(r);
	return vec2(p.x * c - p.y * s, p.x * s + p.y * c);
}

float map(vec3 p) {
	float t = 1000.;
	vec3 pos   = vec3(0.0, 0.0, 0.0);
	float gt = iGlobalTime;
	vec3 scale = vec3(0.2 + sin(gt) * 0.1, 0.1, 0.1);
	for(int i = 0 ; i < 3; i++) {
		vec3 r = mod(p, 1.0) - 0.5;
		r.xy = rot(r.xy, gt * 0.7);
		r.yz = rot(r.yz, gt);
		t = min(t, QuadSphere(r, pos, scale, 0.05));
		pos   = pos.yzx;
		scale = scale.yzx;
	}
	return t;
}

void _userMain( void ) {
	iGlobalTime = 1.65*time;
	float gt = iGlobalTime;
	float d  = 0.0, dt = 0.0, ite = 0.0;
	vec2 uv  = -1.0 + 2.0 * ( gl_FragCoord.xy / resolution );
	vec3 dir = normalize(vec3(uv * vec2(resolution.x/resolution.y, 1.0), 1.0));
	vec3 pos = vec3(0,gt,gt).zxy * 0.2;
	dir.xy   = rot(dir.xy, gt * 0.1);
	dir.yz   = rot(dir.yz, gt * 0.1);

	for(int i = 0 ; i < ITEMAX; i++) {
		dt = map(pos + dir * d);
		if(dt < .001) break;
		d += dt;
		ite++;
	}

	vec3 col = vec3(d * 0.05);
	if(dt < 0.001) {
		float  www = pow(1.0 - (ite / float(ITEMAX)), 10.0);
		col += www * (vec3(0,1,3).zyx * 0.5);
	}
	gl_FragColor = vec4(sqrt(col) + dir * 0.02, 1.0);
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