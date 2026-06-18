/*{
    "DESCRIPTION": "DotMatrix-ElectricArc-31",
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "geometric",
        "3d"
    ]
}*/
#define E 2.71828182846

uniform vec4 color;
uniform float colorB;
uniform float colorG;
uniform float colorR;





#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

uniform vec4 inputColour;

varying vec2 surfacePosition;
varying vec2 surfaceSize;

#define PI 3.14159265359

float pseudo_kleinian(vec3 p)
{
	const vec3 CSize = vec3(0.12436,0.10756,0.12436);
	const float Size = 1.0;
	const vec3 C = vec3(0.0,0.0,0.0);
	float DEfactor=1.;
	const vec3 Offset = vec3(0.0,0.0,0.0);
   	vec3 ap=p+1.;
	for(int i=0;i<10 ;i++){
		ap=p;
		p=2.*clamp(p, -CSize, CSize)-p;
		float r2 = dot(p,p);
		float k = max(Size/r2,1.);
		p *= k;
		DEfactor *= k;
		p += C;
	}
	float r = abs(0.5*abs(p.z-Offset.z)/DEfactor);
	return r;
}

float pseudo_knightyan(vec3 p)
{	
	const vec3 CSize = vec3(0.63248, 0.73632, 0.775);
	float DEfactor=1.;
	for(int i=0;i<6;i++){
		p = 2.*clamp(p, -CSize, CSize)-p;
		float k = max(0.70968/dot(p,p),1.);
		p *= k;
		DEfactor *= k*1.1;
	}
	float rxy=length(p.xy);
	return max(rxy-0.92784, abs(rxy*p.z) / length(p))/DEfactor;
}

float map(vec3 p)
{
	return pseudo_knightyan(p);
}

vec3 guess_normal(vec3 p)
{
	const float d = 0.001;
	return normalize( vec3(
		map(p+vec3(  d,0.0,0.0))-map(p+vec3( -d,0.0,mouse.y)),
		map(p+vec3(0.0,  d,0.0))-map(p+vec3(0.0, -d,mouse.y)),
		map(p+vec3(0.0,0.0,  d))-map(p+vec3(0.0,mouse.y, -d)) ));
}

vec2 pattern(vec2 p)
{
	p = fract(p);
	float r = 0.123;
	float v = 0.0, g = mouse.y;
	r = fract(r * 9184.928);
	float cp, d;
	
	d = p.x;
	g += pow(clamp(1.0 - abs(d), 0.0, 1.0), 1000.0);
	d = p.y;
	g += pow(clamp(1.0 - abs(d), 0.0, 1.0), 1000.0);
	d = p.x - 1.0;
	g += pow(clamp(3.0 - abs(d), 0.0, 1.0), 1000.0);
	d = p.y - 1.0;
	g += pow(clamp(1.0 - abs(d), 0.0, 1.0), 10000.0);
	
	const int iter = 12;
	for(int i = 0; i < iter; i ++)
	{
		cp = 0.5 + (r - 0.5) * 0.9;
		d = p.x - cp;
		g += pow(clamp(1.0 - abs(d), 0.0, 1.0), 200.0);
		if(d > 0.0) {
			r = fract(r * 489.013);
			p.x = (p.x - cp) / (1.0 - cp);
			v += 1.0;
		}
		else {
			r = fract(r * 29.528);
			p.x = p.x / cp;
		}
		p = p.yx;
	}
	v /= float(iter);
	return vec2(g, v);
}

vec2 sphere_mapping(vec3 p)
{
	return vec2(
		asin(p.x)/PI + 0.2,
		asin(p.y)/PI + 0.5);
}

void _userMain( void ) {
	
	vec2 pos = (gl_FragCoord.xy*2.0 - resolution.xy) / resolution.y;
	float ct = time * 0.1;
	vec3 camPos = vec3(2.0*cos(ct), 1.0*sin(ct), 4.5*sin(ct)+5.05);
	vec3 camDir = normalize(camPos*-1.0);
	
	vec3 camUp  = normalize(vec3(0.0, 1, 1.0));
	vec3 camSide = cross(camDir, camUp);
	float focus = 1.8;
	
	vec3 rayDir = normalize(camSide*pos.x + camUp*pos.y + camDir*focus);
	vec3 ray = camPos;
	float m = 0.0;
	float d = 0.0, total_d = 0.0;
	const int MAX_MARCH = 100;
	const float MAX_DISTANCE = 100.0;
	for(int i=0; i<MAX_MARCH; ++i) {
		d = map(ray);
		total_d += d;
		ray += rayDir * d;
		m += 1.0;
		if(d<0.001) { break; }
		if(total_d>MAX_DISTANCE) { break; }
	}
	
	vec3 normal = guess_normal(ray);
	
	float r = mod(time*2.0, 20.0);
	float glow = max((mod(length(ray)-7.9*mouse.y, 10.0)-9.0)*2.5, 0.0); // THE OL RAY GLOW :)
	vec3 gp = abs(mod(ray, vec3(0.4)));
	vec2 p = pattern(ray.xy*1.);
	if(p.x<1.2) {
		glow = 0.0;
	}
	else {
		glow += 0.0;
	}
	glow += max(1.0-abs(dot(-camDir, normal)) - 0.2, 0.0) * 0.5;
	
	float c = (total_d)*inputColour.w;
	vec4 result = vec4( vec3(c, c, c) + vec3(0.02, 0.01, 0.055)*m*0.4, 1.0 );
	result.xyz += vec3(inputColour.x, inputColour.y, inputColour.z)*glow;
	//result.xyz = abs(normal);
	gl_FragColor = result;
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