/*{
    "DESCRIPTION": "DotMatrix-Emerald-Zooming",
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
        "tunnel",
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
//@T_SRTX1911

#ifdef GL_ES
precision mediump float;
#endif

vec3 org = vec3(0.0, 0.0, 0.0);

const int raytraceDepth = 8;

struct Ray{
	vec3 org;
	vec3 dir;
};

struct Sphere{
	vec3 c;
	float r;
	vec3 col;
};

struct Plane{
	vec3 p;
	vec3 n;
	vec3 col;
};
	
struct Intersection{
	float t;
	vec3 p; //hit point
	vec3 n; //normal
	int hit;
	vec3 col;
};

void sphere_intersect(Sphere s, Ray ray, inout Intersection isect){
	vec3 rs = ray.org - s.c;
	float B = dot(rs, ray.dir);
	float C = dot(rs, rs) - (s.r * s.r);
	float D = B * B - C;
	
	if(D > 0.0){
		float t = -B - sqrt(D);
		if((t > 0.0) && (t < isect.t)){
			
			isect.t = t;
			isect.col = s.col;
			isect.hit = 1;
		}
	}
}

void plane_intersect(Plane pl, Ray ray, inout Intersection isect){
	//d = -(p . n);
	//t = -(ray.org . n + d) / (ray.dir . n);
	float d = -dot(pl.p, pl.n);
	float v = dot(ray.dir, pl.n);
	
	if(abs(v) < 1.0e-6) return; //the plane is parallel to the ray.
	
	float t = -(dot(ray.org, pl.n) + d) / v;
	
	if((t > 0.0) && (t < isect.t)){
		isect.hit = 1;
		isect.t = t;
		
		vec3 p = vec3(ray.org.x + t * ray.dir.x,
			      ray.org.y + t * ray.dir.y, 
			      ray.org.z + t * ray.dir.z);
		
		float offset = 0.2;
		vec3 dp = p + offset;

		if((mod(dp.x, 1.0) > 0.5 && mod(dp.z, 1.0) > 0.5) ||
		   (mod(dp.x, 1.0) < 0.5 && mod(dp.z, 1.0) < 0.5))
			isect.col = pl.col;
		else
			isect.col = pl.col * 0.5;
	}
}

Sphere sphere[3];
Plane plane;
void Intersect(Ray r, inout Intersection i){
	for(int c = 0; c < 3; c++){
		sphere_intersect(sphere[c], r, i);
	}
	plane_intersect(plane, r, i);
}

void _userMain( void ) {
	vec2 p= (( gl_FragCoord.xy / resolution.xy ) * 2.0) - 1.0;
	vec3 dir = normalize(vec3(p.x * (resolution.x / resolution.y), p.y, -1.0));
	
	sphere[0].c 	= vec3(-2.0, 0.0, -3.5);
	sphere[0].r	= 0.5;
	sphere[0].col	= vec3(1.0, 0.3, 0.3);
	
	sphere[1].c	= vec3(-0.5, 0.0, -3.0);
	sphere[1].r	= 0.5;
	sphere[1].col	= vec3(0.3, 1.0, 0.3);
	
	sphere[2].c	= vec3(1.0, 0.0, -2.2);
	sphere[2].r	= 0.5;
	sphere[2].col	= vec3(0.3, 0.3, 1.0);

	plane.p = vec3(0.0, -0.5, 0.0);
	plane.n = vec3(0.0, 1.0, 0.0);
	plane.col = vec3(1.0, 1.0, 1.0);
	
	Ray r;
	r.org = org;
	r.dir = normalize(dir);
	vec4 col = vec4(0.0, 0.0, 0.0, 1.0);
	
	Intersection i;
	i.hit = 0;
	i.t = 1.0e+30;
	i.col = vec3(0.0, 0.0, 0.0);
	
	Intersect(r, i);
	if(i.hit != 0)
		col.rgb = i.col;
	
	gl_FragColor = vec4(i.col,1);
	
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