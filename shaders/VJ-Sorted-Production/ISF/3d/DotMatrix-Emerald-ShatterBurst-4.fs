/*{
    "DESCRIPTION": "DotMatrix-Emerald-ShatterBurst-4",
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
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
/*
 * GLSL????
 * ?????????????????
 * ??: http://qiita.com/doxas/items/477fda867da467116f8d
 */
#ifdef GL_ES
precision mediump float;
#endif

struct Ray{
	vec3 origin;
	vec3 direction;
};

struct Intersection{
	vec3 hitPoint;
	vec3 normal; 
	vec3 color; 
	float dist;
};

struct Sphere{
	float radius;
	vec3  position;
	vec3  color;
};

struct Plane{
	vec3 position;
	vec3 normal;
	float xleft, xright;
	float zleft, zright;
	vec3 color;
};

void intersectSphere(Ray R, Sphere S, inout Intersection minInter){
	vec3  a = R.origin - S.position;
	float b = dot(a, R.direction);
	float c = dot(a, a) - (S.radius * S.radius);
	float d = b * b - c;
	if(d > 0.0){
		float t = -b - sqrt(d);
		if(t > 0.0 && t < minInter.dist){
			minInter.hitPoint = R.origin + R.direction * t;
			minInter.normal = normalize(minInter.hitPoint - S.position);
			float d = clamp(dot(normalize(vec3(1.0)), minInter.normal), 0.1, 1.0);
			minInter.color = S.color * d;
			minInter.dist = t;
			return;
		}
	}
}

void intersectPlane(Ray R, Plane P, inout Intersection I){
	float d = -dot(P.position, P.normal);
	float v = dot(R.direction, P.normal);
	float t = -(dot(R.origin, P.normal) + d) / v;
	if(t > 0.0 && t < I.dist){
		vec3 normal = P.normal;
		vec3 hitPoint = R.origin + R.direction * t;
		float d = clamp(dot(normal, vec3(0.577)), 0.1, 1.0);
		float m = mod(hitPoint.x, 2.0);
		float n = mod(hitPoint.z, 2.0);
		if (hitPoint.x < P.xleft || hitPoint.x > P.xright) {
			return;
		}
		if (hitPoint.z < P.zleft || hitPoint.z > P.zright) {
			return;
		}
		if((m > 1.0 && n > 1.0) || (m < 1.0 && n < 1.0)){
			d *= 0.5;
		}
		I.hitPoint = hitPoint;
		I.normal = normal;
		float f = 1.0 - min(abs(I.hitPoint.z), 25.0) * 0.04;
		I.color = P.color * d * f;
		I.dist = t;
	}
}

void main( void ) {
	// fragment pos.
	vec2 p = (gl_FragCoord.xy * 2.0 - resolution) / min(resolution.x, resolution.y);

	Ray ray;
	ray.origin = vec3(-3.0 * mouse.x, -4.0 * mouse.y + 0.5, 3.5);
	ray.direction = normalize(vec3(p.x, p.y, -1.0));
	Intersection inter;
	inter.hitPoint = vec3(0.0);
	inter.normal = vec3(0.0);
	inter.color = vec3(0.0);
	inter.dist = 1.0e+50;
	
		// sphere init
	Sphere sphere[4];
	sphere[0].radius = 0.5;
	sphere[0].position = vec3(0.0, -1.5, sin(time));
	sphere[0].color = vec3(1.0, 0.0, 0.0);
	sphere[1].radius = 1.0;
	sphere[1].position = vec3(2.0, 0.0, cos(time * 0.666));
	sphere[1].color = vec3(0.0, 1.0, 0.0);
	sphere[2].radius = 1.5;
	sphere[2].position = vec3(-2.0, 0.5, cos(time * 0.333));
	sphere[2].color = vec3(0.0, 0.0, 1.0);
	sphere[3].radius = 1.5;
	sphere[3].position = vec3(-2.0, -2.5 * cos(time), 1);
	sphere[3].color = vec3(0.0, 0.0, 1.0);
	
	Plane plane;
	plane.position = vec3(0.0, -2.0, 0.0);
	plane.normal = vec3(0.0, 1.0, 0.0);
	plane.color = vec3(0.6,0.5,1.0);
	plane.xleft = -3.0;
	plane.xright = 2.5;
	plane.zleft = -5.5;
	plane.zright = 2.5;
	
	for (int i = 0; i < 4; ++i)
		intersectSphere(ray, sphere[i], inter);
	intersectPlane(ray, plane, inter);
	float color = 0.0;
	gl_FragColor = vec4(vec3(inter.color), 1.0);

}
