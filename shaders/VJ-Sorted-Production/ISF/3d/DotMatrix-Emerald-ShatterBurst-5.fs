/*{
    "DESCRIPTION": "DotMatrix-Emerald-ShatterBurst-5",
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
 
//??
//??????????
//??????
#define NUMBER_OF_TRIANGLE 2
struct Ray{
	vec3 origin;//??????
	vec3 direction;//??
};
 
struct Sphere{
	float radius;//??
	vec3  position;//??
	vec3  color;//?
};
 
struct Intersection {
	bool hit;//?????
	vec3 hitPoint;//?????
	vec3 normal;//??
	vec3 color;//?
};
	
struct Triangle{
	vec3 position[3];
	vec3 color;
	float reflection;
};
	
float pow2(float x)
{
	return x*x;
}
 
Intersection intersectSphere(Ray R, Sphere S, vec2 light)
{
	Intersection i;
	vec3 a = R.origin - S.position;
	float b = dot(a, R.direction);
	float c = dot(a,a) - pow2(S.radius);
	float d = b*b -c;
	if(d>0.0){
		float _t = -b - sqrt(d);
		if(_t > 0.0){
		i.hit = true;
		i.hitPoint = R.origin + R.direction * _t;
		i.normal = normalize(i.hitPoint - S.position);
		float d = clamp(dot(normalize(vec3(light,0.5)), i.normal), 0.1, 1.0);
		i.color = S.color * d;
		return i;
	}
	}
	i.hit = false;
	i.hitPoint = vec3(0.0);
	i.normal = vec3(0.0);
	i.color = vec3(0.0);
	return i;	
}

void main( void ) {
	//fragment position
	vec2 p = (gl_FragCoord.xy * 2.0 - resolution) / min(resolution.x, resolution.y);
	
	//mouse = light_origin
	vec2 position = (gl_FragCoord.xy * 2.0 - resolution) / min(resolution.x,resolution.y);
	vec2 mousepos = vec2((mouse.x*2.0-1.0)*resolution.x/min(resolution.x,resolution.y),
			     (mouse.y*2.0-1.0)*resolution.y/min(resolution.x,resolution.y));
	
	//CamRay init
	Ray ray;
	ray.origin = vec3(0.0, 0.0, 5.0);
	ray.direction = normalize(vec3(p.x, p.y, -1.0));
	
	Triangle triangle[NUMBER_OF_TRIANGLE];
	triangle[0].position[0] = vec3(-10.0,10.0,-5.0);
	triangle[0].position[1] = vec3(-10.0,-10.0,-5.0);
	triangle[0].position[2] = vec3(10.0,-10.0,-5.0);
	triangle[0].color = vec3(0.5,1.0,0.5);
	triangle[0].reflection = 1.0;
	triangle[1].position[0] = vec3(10.0,10.0,-5.0);
	triangle[1].position[1] = vec3(-10.0,10.0,-5.0);
	triangle[1].position[2] = vec3(10.0,-10.0,-5.0);
	triangle[1].color = vec3(0.5,1.0,0.5);
	triangle[1].reflection = 0.0;

	//Sphere init
	Sphere sphere;
	sphere.radius = 1.0;
	sphere.position = vec3(cos(time*3.0), sin(time*2.0), cos(time * 1.0));
	sphere.color = vec3(1.0);
	
	//hit check
	Intersection i = intersectSphere(ray, sphere,mousepos);
	gl_FragColor = vec4(i.color, 1.0);
}

