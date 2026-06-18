/*{
    "DESCRIPTION": "DotMatrix-Emerald-ElectricArc-2",
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
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
//frorked by T_S / RTX1911
#ifdef GL_ES
precision mediump float;
#endif

#define EPSM  0.9999

//iq's tools.
float hash( float n )
{
    return fract(sin(n)*43758.5453123);
}

float hexp( vec2 p, vec2 h )
{
    vec2 q = abs(p);
    return max(q.x-h.y,max(q.x+q.y*0.57735,q.y*1.1547)-h.x);
}

float plane(vec3 p, float D) {
	return D - dot(abs(p), normalize(vec3(0.0, 1.0, 0.0)));
}

vec2 rot(vec2 p, float a) {
	return vec2(
		cos(a) * p.x - sin(a) * p.y,
		sin(a) * p.x + cos(a) * p.y);
}

vec3 map(vec3 p) {
	vec3 pp = p;
	
	//ground
	float k = plane(pp, 0.7);
	
	//PYRAMID
	pp = mod(-abs(p), 1.0) - 0.5;
	
	float tim = time * 2.0 + sin(time * 1.14) * 4.0;
	for(int e = 0 ; e < 6; e++) {
		//rotate
		if( floor(float(e) / 2.0) > 0.0 ) tim = -tim;
		vec3 ppp = pp;
		
		//get fake unique angle.
		ppp.xz = rot(ppp.xz, tim * 0.3 + ceil(p.z) / (1.5));
		k = min(k, max(ppp.y - 0.08 * float(e), hexp(ppp.xz, vec2(0.48 - 0.08 * float(e)))) );
	}
	return vec3(pp.xz, k);
}
	
void _userMain( void ) {
	vec2 uv    = -1.0 + 2.0 * ( gl_FragCoord.xy / resolution.xy );
	
	//ray
	vec3 dir   = normalize(vec3(uv * vec2(resolution.x / resolution.y, 1.0), 1.0)).xyz;
	
	//rotate
	dir.xz = rot(dir.xz, cos(time * 0.25) * 0.5);
	dir.yz = rot(dir.yz, sin(time * 0.25) * 0.25);
	//dir.xy = rot(dir.xy, time * 0.005);
	
	//camera 
	vec3 pos   = vec3(0.0);
	pos.z     += time * 0.5;
	pos.x     += sin(time * 0.5) * 0.125;
	
	//col
	vec3  col  = vec3(0.0);
	float t    = 0.01;

	//raymarching
	vec3  gm   = vec3(0.0);
	for(int i  = 0 ; i < 64; i++) {
		gm = map(pos + dir * t * EPSM);
		t += gm.z;
	}
	vec3  IP   = pos + dir * t;
	
	//fake shadow and fake ao : http://pouet.net/topic.php?which=7535&page=1
	vec3  L    = vec3(-0.5, 1.2, -0.7); //norm...
	float Sef  = 0.05;
	float S1   = max(map(IP + Sef * L).z, 0.005);
	col        = max(vec3(S1), 0.0);
	
	//sun
	vec3 sun   = mix(vec3(1, 2, 3), vec3(1, 2, 3).zyx, t * 0.5) * 0.04;
	col        = sqrt(col) + dir.zxy * 0.005+ t * 0.05 + sun;
	gl_FragColor = vec4(col.gbr, 1.0);
	gl_FragColor *= mod(gl_FragCoord.y, 2.0);
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