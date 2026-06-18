/*{
    "DESCRIPTION": "DotMatrix-Solar-Rotating-4",
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

struct Ray {
	vec3 origin;
	vec3 dir;
};
	
struct Sphere {
	vec3 origin;
	float radius;
	vec3 color;
};
	
struct Intersection {
	vec3 color;
	float distance;
};

#define MISS Intersection(vec3(0.0),-1.0)

Intersection minPosIntersection(Intersection a, Intersection b) {
	if(a.distance < 0.0) return b;
	if(b.distance < 0.0) return a;
	if (a.distance < b.distance) return a;
	else return b;
}
	
Intersection intersect(Ray r, Sphere s) {
	vec3 sr = r.origin - s.origin;
	float a = dot(r.dir,r.dir);
	float b = 2.0 * dot(sr,r.dir);
	float c = dot(sr,sr) - (s.radius * s.radius);
	
	float det = (b * b) - (4.0 * a * c);
	if(det < 0.001) {
		return MISS;
	} else {
		float t1 = (-b - sqrt(det)) / (2.0 * a);
		float t2 = (-b + sqrt(det)) / (2.0 * a);
		
		float t = t1 > 0.0 ? t1 : t2;
		return Intersection(s.color, t);
	}
}
vec3 intersectScene(Ray r) {
	Intersection back = intersect(r,Sphere(vec3(0,0,1000.0),995.0, vec3(1.0,0.0,0.0)));
	Intersection front = intersect(r, Sphere(vec3(0,0,-1000),995.0,vec3(1.0, 0.0, 1.0)));
	Intersection bottom = intersect(r,Sphere(vec3(0,-1000,0.0),995.0, vec3(0.0,1.0,0.0)));
	Intersection top = intersect(r,Sphere(vec3(0,1000,0.0),995.0, vec3(0.0,0.0,1.0)));
	Intersection left = intersect(r,Sphere(vec3(-1000,0,0.0),995.0, vec3(1.0,1.0,0.0)));
	Intersection right = intersect(r,Sphere(vec3(1000,0,0.0),995.0, vec3(0.0,1.0,1.0)));
	Intersection center  = intersect(r, Sphere(vec3(0), 0.8, vec3(1.0,1.0,1.0)));

	Intersection minIntersect =  minPosIntersection(minPosIntersection(minPosIntersection(minPosIntersection(bottom, top), minPosIntersection(left, right)), minPosIntersection(back,front)),center);
	return minIntersect.color;
}

Ray getRay(vec2 pixel) {
	vec2 uv = (pixel - vec2(0.5)) * 2.0;
	uv.x *= resolution.x / resolution.y;
	vec3 rotate =  vec3(cos(time), 0.0,sin(time));
	vec3 origin = 5.0 * rotate;
	vec3 forward = -rotate;
	vec3 up = vec3(0.0,1.0,0.0);
	vec3 right = cross(forward,up);
	vec3 dir = normalize(-rotate + (up * uv.y) + (right * uv.x));
	
	return Ray(origin, dir);
}

void _userMain( void ) {
	
	vec2 pixel = gl_FragCoord.xy /resolution.xy;
	Ray r = getRay(pixel);
	vec3 color = intersectScene(r);

	gl_FragColor = vec4(color,1.0);
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