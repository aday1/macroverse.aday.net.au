/*{
    "DESCRIPTION": "HorizonLight-Fire-DotMatrix-5",
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
precision highp float;

struct Ray {
	vec3 origin;
    	vec3 direction;
};

struct Light {
    	vec3 color;
    	vec3 direction;
};

struct Material {
    	vec3 color;
    	float diffuse;
    	float specular;
};

struct Intersect {
    	float len;
    	vec3 normal;
    	Material material;
};

struct Sphere {
    	float radius;
    	vec3 position;
    	Material material;
};

struct Plane {
    	vec3 normal;
    	Material material;
};
	
const float epsilon = 0.001;

const int iterations = 6;

const float exposure = 0.7;
const float gamma = 2.2;

const float intensity = 100.0;
const vec3 ambient = vec3(0.6, 0.8, 1.0) * intensity / gamma;

Light light;
Intersect miss = Intersect(0.0, vec3(0.0),Material(vec3(0.0), 0.0, 0.0));

Intersect intersect(Ray ray, Sphere sphere) {
    	vec3 oc = sphere.position - ray.origin;
    	float l = dot(ray.direction, oc);
    	float det = pow(l, 2.0) - dot(oc, oc) + pow(sphere.radius, 2.0);
    	if (det < 0.0) return miss;

    	float len = l - sqrt(det);
    	if (len < 0.0) len = l + sqrt(det);
    	if (len < 0.0) return miss;
    
	vec3 norm = (ray.origin + len*ray.direction - sphere.position) / sphere.radius;
	return Intersect(len, norm, sphere.material);
}

Intersect intersect(Ray ray, Plane plane) {
    	float len = -dot(ray.origin, plane.normal) / dot(ray.direction, plane.normal);
    	if (len < 0.0) return miss;
	
    	vec3 hitp = ray.origin + ray.direction * len;
    	float m = mod(hitp.x, 2.0);
    	float n = mod(hitp.z, 2.0);
    	float d = 1.0;
    	if((m > 1.0 && n > 1.0) || (m < 1.0 && n < 1.0)){
            	d *= 0.5;
     	}
	
   	plane.material.color*= d;
    	return Intersect(len, plane.normal, plane.material);
}

Intersect trace(Ray ray) {
    	const int num_spheres = 3;
    	Sphere spheres[3];
    	spheres[0] = Sphere(2.0, vec3(-3.-sin(time), 3.0 + sin(time), 0), Material(vec3(1.0, 0.0, 0.0), 0.01, 0.01));
    	spheres[1] = Sphere(3.0, vec3( 3.0 + cos(time), 3.0, 0), Material(vec3(0.0, 0.0, 1.0), 0.02, 0.001));
    	spheres[2] = Sphere(1.0, vec3( 0.5, 1.0, 6.0),            Material(vec3(1.0, 1.0, 1.0), 0.0001, 0.01));

    	Intersect intersection = miss;
	
    	Intersect plane = intersect(ray, Plane(vec3(0, 1, 0), Material(vec3(1.0, 1.0, 1.0), 0.01, 0.0001)));
    	if (plane.material.diffuse > 0.0 || plane.material.specular > 0.0) { 
	    	intersection = plane;   
    	}

    	//for (int i = 0; i < num_spheres; i++) {
	int i = 0;
        Intersect sphere0 = intersect(ray, spheres[0]);
        if (sphere0.material.diffuse > 0.0 || sphere0.material.specular > 0.0) {
            intersection = sphere0;
	}
	Intersect sphere1 = intersect(ray, spheres[1]);
        if (sphere1.material.diffuse > 0.0 || sphere1.material.specular > 0.0) {
            intersection = sphere1;
	}
	Intersect sphere2 = intersect(ray, spheres[2]);
        if (sphere2.material.diffuse > 0.0 || sphere2.material.specular > 0.0) {
            intersection = sphere2;
	}   
    
    	return intersection;
}

vec3 radiance(Ray ray) {
    	vec3 color=vec3(0.);
	vec3 fresnel = vec3(1.0);
    	vec3 mask = vec3(1.0);
	Intersect hit;
	vec3 reflection;
	vec3 spotLight;
    	for (int i = 0; i <= iterations; ++i) {
        	hit = trace(ray);
         	// if we encounter a material bounce again, otherwise return the sky color
        	if (hit.material.diffuse > 0.0 || hit.material.specular > 0.0) {

            		// Schlick's Approximation (Specular)
            		vec3 r0 = hit.material.color.rgb + hit.material.specular;
            		float hv = clamp(dot(hit.normal, -ray.direction), 0.0, 1.0);
            		fresnel = r0 + (1.0 - r0) * pow(1.0 - hv, 5.0);

            		// Diffuse
            		if (trace(Ray(ray.origin + hit.len * ray.direction + epsilon * light.direction, light.direction)) == miss) {
                		color += clamp(dot(hit.normal, light.direction), 0.0, 1.0) * light.color
                       		   * hit.material.color.rgb * hit.material.diffuse  // Diffuse
                                   * (1.0 - fresnel) * mask;                        // Specular
            		}

            	mask *= fresnel;
            	reflection = reflect(ray.direction, hit.normal);
            	ray = Ray(ray.origin + hit.len * ray.direction + epsilon * reflection, reflection);

        	} else {

            		// flare/glare
            		spotLight = vec3(1e6) * pow(abs(dot(ray.direction, light.direction)), 250.0);
            		color *= mask * (ambient + spotLight);
	   		break;
        	}
    	}
	
    return color;
}

void main() {
	light.color = vec3(1.0) * intensity;
	light.direction = normalize(vec3(-1.0 + 4.0 * cos(time), 4.75, 1.0 + 4.0 ));
    	
	vec2 viewport = resolution.xy;
    	vec2 uv    = gl_FragCoord.xy / viewport.xy - vec2(0.5);
        uv.x *= viewport.x / viewport.y;
    	Ray ray = Ray(vec3(0.0, 2.5, 12.0), normalize(vec3(uv.x, uv.y, -1.0)));
    	gl_FragColor = vec4(pow(radiance(ray) * exposure, vec3(1.0 / gamma)), 1.0);     // linear tone mapping
}
