/*{
    "DESCRIPTION": "GlowBloom-FrostCrystal-2",
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
// stripey cube thing
// by gngbng
// this is mostly just a terrible mishmash of stolen code

#ifdef GL_ES
precision mediump float;
#endif

float scene(vec3 p, float t) {
	float coobs = 999.; // wat the fuck am i doing
	for(int i = -33; i < 3; i++) {
		t += float(i+2);
		
		// help how do i matrice
		mat3 r = mat3(
			1, 0, 0,
			0, cos(t), -sin(t),
			0, sin(t), cos(t)
		);
		
		r *= mat3(
			cos(t), 0, sin(t),
			0, 1, 0,
			-sin(t), 0, cos(t)
		);
		
		r *= mat3 (
			cos(t/2.), -sin(t/2.), 0,
			sin(t/2.), cos(t/2.), 0,
			0, 0, 1
		);
				
		coobs = min(length(max(abs((p+vec3(i*2,0,0))*r)-1.,0.)),coobs);
	}
	
	return min(coobs,-p.z);	
}

float stripes(vec2 pos, float ratio) {
	return mod(pos.x - pos.y + time * 2., 1.) > ratio ? 1. : .75;
}

vec3 normal(vec3 p, float t)
{
	float d = 0.001;	
	float dx = scene(p + vec3(d, 0.0, 0.0), t) - scene(p + vec3(-d, 0.0, 0.0), t);
	float dy = scene(p + vec3(0.0, d, 0.0), t) - scene(p + vec3(0.0, -d, 0.0), t);
	float dz = scene(p + vec3(0.0, 0.0, d), t) - scene(p + vec3(0.0, 0.0, -d), t);
	return normalize(vec3(dx, dy, dz));
}

vec4 render(float t) {
	vec3 pos = vec3(0,0,-5);
	vec3 dir = normalize(vec3((gl_FragCoord.xy - resolution.xy * .5) / resolution.x, .5));
	
	float what = 0.;
	for(int i = 0; i < 16; i++) {
		float dist = scene(pos, t);
		pos += dist*dir;
		what += (1./(1.+dist))*dist; // cool occlusion shit stolen from kabuto i think
	}

	vec3 nrm = normal(pos, t);
	return vec4(vec3(stripes(gl_FragCoord.xy / resolution.y * 20. - nrm.xy, 1.25-1./what))/(what*3.)*(1.+nrm.y*.5), 1.);
}

float rand(vec2 co){
    return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

void _userMain(void) {
	// lameshit stochastic moblur
	vec4 final = vec4(0);
	for(int i = 0; i < 4; i++) {
		final += render(time + rand(vec2(time+float(i)*.01+gl_FragCoord)) *  0.05);	
	}
	gl_FragColor = final / 4.;
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