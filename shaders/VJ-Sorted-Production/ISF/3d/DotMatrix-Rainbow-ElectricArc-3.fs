/*{
    "DESCRIPTION": "DotMatrix-Rainbow-ElectricArc-3",
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
#ifdef GL_ES
precision mediump float;
#endif

const int MAX_ITER = 60;

#define pi 3.14159265359

vec3 rotateY(in vec3 v, in float a) {
	return vec3(cos(a)*v.x + sin(a)*v.z, v.y  ,-sin(a)*v.x + cos(a)*v.z);
}

vec3 rotateX(in vec3 v, in float a) {
	return vec3(v.x,cos(a)*v.y + sin(a)*v.z,-sin(a)*v.y + cos(a)*v.z);
}

float torus(in vec3 p,in float radius,in float dist){
	return max(pow(dist-length(p.xz),2.0)+p.y*p.y-radius*radius,0.0);
}

vec3 hsv(in float h, in float s, in float v) {
	return mix(vec3(1.0), clamp((abs(fract(h + vec3(3, 2, 1) / 3.0) * 6.0 - 3.0) - 1.0), 0.0 , 1.0), s) * v;
}

float angleBetween(vec3 a,vec3 b){
	float f=acos(dot(a,b));
	if(sign(a.y)<0.0){
	return 2.0*pi-f;
	}
	return f;
}

vec2 positionOnTorus(in vec3 p,in float dist){
		p=fract(p)-0.5;
		float i=(atan(p.x,p.z)+pi)/(2.0*pi);
		
		vec3 p2=normalize(vec3(p.x,0.0,p.z));
		vec3 p3=normalize(dist*p2-p);
		float j=angleBetween(p3,p2)/(2.0*pi);
		return vec2(i,j);
}

vec3 texture(vec2 p){
	float si1=0.55+0.01*sin(p.x*20.0*pi);
	float si2=0.95+0.01*sin(p.x*10.0*pi-0.3);
	if(p.y<si2&&p.y>si1){
		return vec3(1,1,1);
	}
	return mix(vec3(0.98,0.8,0.5),vec3(0.95,0.6,0.05),pow(abs(p.y-0.5)*2.0,0.5));
}

//rayMarcher by http://glsl.heroku.com/e#14543.0
vec3 intersect(in vec3 rayOrigin, in vec3 rayDir)
{
	float total_dist = 0.0;
	vec3 p = rayOrigin;
	float d = 1.0;
	float iter = 0.0;
	
	for (int i = 0; i < MAX_ITER; i++)
	{		
		if (d < 0.001) break;
		
		d = torus(fract(p)-0.5,0.12,0.2);
		p += d*rayDir;
		total_dist += d;
		iter++;
	}

	if (d < 0.001) {
		return texture(positionOnTorus(p,0.2))*vec3(1.0-iter/float(MAX_ITER));
	}
	return vec3(0.0);
}

void _userMain()
{
	vec2 screenPos=gl_FragCoord.xy/resolution-0.5;
	vec3 rayDir=normalize(vec3(screenPos.x*1.5,screenPos.y,0.5));
	rayDir=rotateX(rayDir,4.0*(mouse.y-0.5));
	rayDir=rotateY(rayDir,4.0*(mouse.x-0.5));
	vec3 cameraOrigin = vec3(0, 0, time);
	
	gl_FragColor = vec4(intersect(cameraOrigin, rayDir), 1.0);
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