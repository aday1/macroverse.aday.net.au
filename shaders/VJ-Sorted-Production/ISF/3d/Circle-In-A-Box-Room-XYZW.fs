/*{
    "DESCRIPTION": "Circle-In-A-Box-Room-XYZW",
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
            "NAME": "timeScale",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Time speed"
        },
        {
            "NAME": "mouseX",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Mouse X"
        },
        {
            "NAME": "mouseY",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Mouse Y"
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/
#define E 2.71828182846

uniform vec4 color;
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

uniform vec4 inputColour;

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
	float b = mouse.x+2.0 * dot(sr,r.dir);
	float c = dot(sr,sr) - (s.radius * s.radius + mouse.y + 4);
	
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
	Intersection center  = intersect(r, Sphere(vec3(0), 0.4, vec3(1.0,1.0,1.0)));

	Intersection minIntersect =  minPosIntersection(minPosIntersection(minPosIntersection(minPosIntersection(bottom, top), minPosIntersection(left, right)), minPosIntersection(back,front)),center);
	return minIntersect.color;
}

Ray getRay(vec2 pixel) {
	vec2 uv = (pixel - vec2(0.5)) * 2.0;
	uv.x *= resolution.x / resolution.y;
	vec3 rotate =  vec3(cos(time), inputColour.w,sin(time));
	vec3 origin = 5.0 * rotate;
	vec3 forward = -rotate;
	vec3 up = vec3(inputColour.x,inputColour.y,inputColour.z);
	vec3 right = cross(forward,up);
	vec3 dir = normalize(-rotate + (up * uv.y) + (right * uv.x));
	
	return Ray(origin, dir);
}

void main( void ) {
	
	vec2 pixel = gl_FragCoord.xy /resolution.xy;
	Ray r = getRay(pixel);
	vec3 color = intersectScene(r);

	gl_FragColor = vec4(color,1.0);
}

