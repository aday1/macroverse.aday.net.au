/*{
    "DESCRIPTION": "DotMatrix-Emerald-MirrorSurface-2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "noise"
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
        "noise"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

vec3 light = vec3(3.0, 4.0, -5.0);

float hash(float t) {
	return fract(cos(t * 65537.0) * 65521.0);
}

float noise(vec3 p) {
	p = floor(p);
	return fract( hash(sin(p.x)) + hash(sin(p.y)) + hash(sin(p.z)) - hash((sin(p.x * 2.0) + sin(p.y * 3.0) + sin(p.z * 5.0)))); 	
}

float snow(vec2 p) {
	vec2 f = fract(p);
	p -= f;
	return mix(
		mix(noise(p.xyx), noise(vec2(p.x + 1.0, p.y).xyx), f.x),
		mix(noise(vec2(p.x, p.y + 1.0).xyx), noise(p.xyx + 1.0), f.x),
		f.y	
	);
}

float perlin(vec2 p) {
	float r = 0.0;
	r += 0.500000 * snow(p); p *= 5.0/2.0;
	r += 0.250000 * snow(p); p *= 7.0/3.0;
	r += 0.125000 * snow(p); p *= 11.0/5.0;
	r += 0.062500 * snow(p); p *= 15.0/7.0;
	r += 0.031250 * snow(p); p *= 23.0/11.0;
	r += 0.031250 * snow(p); p *= 27.0/13.0;
	return r;
}

vec3 trace(vec3 ori, vec3 dir, int iter) {
	if (iter < 0)
		return vec3(0.0);

	float t = -2.0 / dir.y;
	
	if (t > 0.0) {
		vec3 gnd = ori + t * dir;
	
		mat2 roty = mat2(cos(time/20.), -sin(time/20.), sin(time/20.), cos(time/20.));
		
		gnd.xz = roty * gnd.xz;
		ori.xz = roty * ori.xz;

		vec2 uv = gnd.xz;

		float c = mod(floor(uv.x) + floor(uv.y), 2.0);
	
		return
			vec3(0.4 + 0.2 * c, 1.0-c, c) *
			(0.6 + 0.4 * noise(gnd * 65536.0)) *
			(0.5 + 0.5 * dot(
				vec3(0.0, 1.0, 0.0),
				normalize(light - gnd)
			) + 
			0.0//0.2 * trace(gnd, reflect(dir, vec3(0.0, 1.0, 0.0)), iter - 1)
			);
	}

	t = 50.0 / dir.y;
	
	if (t > 0.0) {
		vec3 gnd = ori + t * dir;
	
		vec2 uv = gnd.xz / 20.0 + vec2(-time, 0.0);
		
		float c = mod(floor(uv.x) + floor(uv.y), 2.0);
	
		return vec3(.6, .6, .5) + (0.1 * clamp(perlin(uv) * 7.0 - 3.0, 0.0, 1.0));
	}
			
	return vec3(0);
}

void _userMain( void ) {
	vec2 pos = ( gl_FragCoord.xy / resolution.xy ) * 2.0 - 1.0;
	pos.x *= resolution.x / resolution.y;
	
	gl_FragColor = vec4(0.0, 0.0, 0.0, 1.0);
	
	vec3 ori = vec3(10.0, 0.0, 5.0);
	vec3 dir = normalize(vec3(2.0 * pos, -1.0));
	
	gl_FragColor = vec4(trace(ori, dir, 5), 1.0);
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