/*{
    "DESCRIPTION": "BallSurfaceLenseFlare",
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
#ifdef GL_ES
precision mediump float;
#endif

float sphere (vec3 center, float radius, vec3 rayOrigin, vec3 rayDir) {
	vec3 dist = rayOrigin - center;
	float a = 1.0;
	float b = 2.0 * dot(dist, rayDir);
	float c = dot(dist, dist) - dot(radius, radius);
	float D = b * b - 4.0 * a * c;
	if (D < 0.0) {
		return 0.0;
	}
	
	float t = (-b - sqrt(D)) / (2.0 * a);
	if (t < 0.0) {
		t = (-b + sqrt(D)) / (2.0 * a);
	}
	if (t < 0.0) {
		return 0.0;
	}
	return t;
}
 
vec3 background (vec3 r, float t) {
	vec3 light = normalize(vec3(cos(t), 0.6, sin(t)));
	float sun = max(0.0, dot(r, light));
	float sky = max(0.0, dot(r, vec3(1.0, 1.0, 0.0)));
	float ground = max(0.0, dot(r, vec3(0.0, -1.0, 0.0)));
	return pow(sun, 250.0) * vec3(2.0, 2.0, 1.0) + sky * vec3(0.5, 0.5, 1.0) + ground * vec3(0.6, 0.4, 0.5);
}
	
void main () {
	vec2 p = gl_FragCoord.xy / resolution;
	p = 2.0 * p - 1.0;
	p.x *= resolution.x / resolution.y;
	
	vec3 rayDir = normalize(vec3(p, -1.0));
	vec3 rayOrigin = vec3(0.0, 0.0, 2.0);
	vec3 sphereCenter = vec3(0.0);
	float t = sphere(sphereCenter, 1.0, rayOrigin, rayDir);
	vec3 bgColor = background(rayDir, time);
	if (t == 0.0) {
		gl_FragColor = vec4(bgColor, 1.0);
		return;
	}
	
	vec3 normal = normalize(rayOrigin + rayDir * t);
	rayDir = reflect(rayDir, normal);
	vec3 color = background(rayDir, time);
	gl_FragColor = vec4(color * vec3(2.0, 1.0, 0.6), 1.0);
	
}

