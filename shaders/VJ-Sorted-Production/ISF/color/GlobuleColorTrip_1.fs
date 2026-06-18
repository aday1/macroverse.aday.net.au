/*{
    "DESCRIPTION": "GlobuleColorTrip",
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
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

vec2 scene(vec3 p) {
	float d = sin(p.x + time)*sin(p.y + time)*sin(p.z);
	vec2 d1 = vec2(length(p) - 1.5 + d, 1.0);
	
	return d1;
}

vec2 map(vec3 p) {
	vec2 d1 = vec2(p.y + 1.0 + 0.5*(cos(p.x+sin(time/10.)*time) + sin(p.z)), 0.0);
	vec2 d2 = scene(p);
	
	return d1.x < d2.x ? d1 : d2;
}

vec2 intersect(vec3 ro, vec3 rd) {
	float td = 0.0;
	float mid = 0.0;
	
	for(int i = 0; i < 64; i++) {
		vec2 s = map(ro + rd*td);
		if(s.x == 0.0) break;
		td += s.x;
		mid = s.y;
	}
	
	if(td > 20.0) mid = -1.0;
	return vec2(td, mid);
}

vec3 normal(vec3 p) {
	vec2 h = vec2(0.001, 0.0);
	vec3 n = vec3(
		map(p + h.xyy).x - map(p - h.xyy).x,
		map(p + h.yxy).x - map(p - h.yxy).x,
		map(p + h.yyx).x - map(p - h.yyx).x
	);
	
	return normalize(n);
}

float shadow(vec3 p, vec3 lig) {
	float res = 1.0;
	float td = 0.2;
	
	for(int i = 0; i < 16; i++) {
		float h = map(p + lig*td).x;
		td += h;
		res = min(res, 8.0*h/td);
		if(h == 0.0 || td > 10.0) break;
	}
	
	return clamp(res, 0.0, 1.0);
}

vec3 lighting(vec3 p, vec3 l, vec3 rd) {
	vec3 lig = normalize(l);
	vec3 n = normal(p);
	vec3 ref = reflect(rd, n);
	
	float amb = clamp(0.5+0.5*n.y, 0.0, 0.2);
	float dif = clamp(dot(n, lig*2.), 0.0, 1.0);
	float spe = pow(clamp(dot(ref, lig), 0.0, 1.0), 100.0);
	
	dif *= shadow(p, lig);
	
	vec3 lin = vec3(0);
	
	lin += 0.6*amb*vec3(1);
	lin += 0.825*dif*vec3(1.0, 0.97, 0.85);
	lin += 1.20*spe*vec3(0.55, 0.87, 0.55)*dif;
	
	return lin;
}

mat3 camera(vec3 e, vec3 la) {
	vec3 f = normalize(la - e);
	vec3 r = normalize(cross(vec3(0, 1, 0), f));
	vec3 u = normalize(cross(f, r));
	
	return mat3(r, u, f);
}

void _userMain( void ) {
	vec2 uv = -1.0+2.0*(gl_FragCoord.xy/resolution);
	uv.x *= resolution.x/resolution.y;
	
	float a = time*0.3;
	vec3 ro = 7.0*vec3(sin(a), 0.4, cos(a));
	vec3 rd = camera(ro, vec3(sin(time)))*normalize(vec3(uv, 2.0));
	
	vec2 i = intersect(ro, rd);
	vec3 col = mix(vec3(0, .99, .97), vec3(0, .67, .97), uv.y)*0.7;
	
	if(i.y > -1.0) {
		vec3 p = ro + rd*i.x;
		if(i.y == 0.0) {
			col = mix(vec3(abs(1. * sin(time * 8. * mouse.x/resolution.x)),abs(1.27*sin(time)), abs(1.0*cos(time*2.4))), vec3(0, 0.0, 0.8), fract(scene(p).x*3.0));
		}
		if(i.y == 1.0) col = vec3(.75, 0, .82);
		
		col *= lighting(p, 5.0*vec3(cos(time), 1.0, sin(time)), rd);
	}
	
	gl_FragColor = vec4(col, 1.0);
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