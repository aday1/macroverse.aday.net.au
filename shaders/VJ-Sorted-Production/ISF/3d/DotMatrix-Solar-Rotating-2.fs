/*{
    "DESCRIPTION": "DotMatrix-Solar-Rotating-2",
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
        }
    ],
    "TAGS": [
        "geometric",
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
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

void main( void ) {
	
	vec2 pixel = gl_FragCoord.xy /resolution.xy;
	Ray r = getRay(pixel);
	vec3 color = intersectScene(r);

	gl_FragColor = vec4(color,1.0);
}

