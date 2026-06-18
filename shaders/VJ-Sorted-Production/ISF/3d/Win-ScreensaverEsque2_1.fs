/*{
    "DESCRIPTION": "Win-ScreensaverEsque2",
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

#extension GL_OES_standard_derivatives : enable

float sdSphere(vec3 p, float r) {
	return length(p) - r;
}

float sdTorus(vec3 p, vec2 t) {
	vec2 q = vec2(length(p.xy) - t.x, p.z);
	return length(q) - t.y;}

float opS(float d1, float d2) {return max(-d1, d2);}//hg_sdf

//distance to torus.hollow() + blob()_displacement
//tosus shape makes the sinusoidial displacement more apparent
float df(vec3 p){
	float l=sdTorus(p,vec2(2.,.4));
	l*=2.;
		//l=length(p);
	float wallT=.1;//wall thickness in addition to rarius
	float 
	hollow=(min(l,-l+wallT));//NEGATIVE distance to hollow ed object
      //hollowSphere=-max(-(l-1.),l-1.5);//NEGATIVE distance to hollow sphere
      //hollowSphere=-opS(  l-1. ,l-1.5);//NEGATIVE distance to hollow sphere
	float d=sin(p.x)*sin(p.y +time)*sin(p.z);//blob() displacement. see hg_sdf
	//d=cos(length(p.yz))-p.x;
	float f=(sin(time)*.5+.5)*4.;
	return f*d-hollow;
}//from http://glslsandbox.com/e#38280.0
//lipschits roughly equal to 2., but 6. is also happening, 3. is a good compromise.

vec3 intersect(vec3 from, vec3 rayDir) {
	float totalDist = 0.0;
	vec3 p;
	for(int i = 0; i < 300; i++) {
		p = from + totalDist*rayDir;
		float d = df(p)*.3;
		totalDist += d;
		if(d < 0.001) {
			break;
		}
	}
	return p;
}

vec3 calcNormal(vec3 p) {
	float d = 0.001;
	return normalize(vec3(
		df(p + vec3(d, 0, 0)) - df(p + vec3(-d, 0, 0)),
		df(p + vec3(0, d, 0)) - df(p + vec3(0, -d, 0)),
		df(p + vec3(0, 0, d)) - df(p + vec3(0, 0, -d))
		));
}

void _userMain( void ) {
	vec2 uv = (2.0*gl_FragCoord.xy - resolution)/resolution.x;
	
	vec3 camPos = vec3(0, 0, -5);
	vec3 camFront = vec3(0, 0, 1.0);
	vec3 camUp = vec3(0, 1.0, 0);
	vec3 camRight = cross(camFront, camUp);
	
	vec3 rayDir = uv.x*camRight + uv.y*camUp + 1.0*camFront;
	
	gl_FragColor = vec4(-calcNormal(intersect(camPos, rayDir)), 1.0);
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