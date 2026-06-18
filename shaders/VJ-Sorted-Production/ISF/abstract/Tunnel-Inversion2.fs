/*{
    "DESCRIPTION": "Tunnel-Inversion2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        }
    ],
    "TAGS": [
        "abstract"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

float ASPECT = resolution.y / resolution.x;

const float PI = 3.14159265359;
const float FOV = 90.0 / 180.0 * PI;
const int ITER = 32;
const vec3 POS = vec3(0.0, 0.0, 0.0);

float VFOV = 2.0 * atan(tan(FOV/2.0) * ASPECT);

float SCREEN_WIDTH = 2.0 * tan(FOV/2.0);
float SCREEN_HEIGHT = 2.0 * tan(VFOV/2.0);

float sphere(vec3 pos, float radius) {
	return length(pos) - radius;
}

float spheres(vec3 pos, float radius) {
	pos = mod(pos, 1.0) - 0.5;
	return sphere(pos, radius);
}

float cube(vec3 pos, float length) {
	return max(pos.x, max(pos.y, pos.z)) - length;
}

float scene1(vec3 pos, vec3 ray) {
	pos.z += time * 2.0;
	
	float dist = 0.0;
	for(int i = 0; i < ITER; ++i) {
		float c = 1e99;
		c = min(c, spheres(vec3(0.0, 0.0, 2.0) - pos, 0.4));
		c = min(c, spheres(vec3(0.0, 0.5, 2.0) - pos, 0.1));
		c = min(c, spheres(vec3(0.5, 0.0, 2.0) - pos, 0.1));
		
		dist += c;
		pos += ray * c;
	}
	
	return dist;
}

void main( void ) {
	vec2 uv = (gl_FragCoord.xy / resolution) - 0.5;
	vec3 ray = normalize(vec3(uv.x * SCREEN_WIDTH, uv.y * SCREEN_HEIGHT, 1.0));
	
	float dist = scene1(POS, ray);
	float brightness = 1.0/(pow(dist, 1.5));
	
	vec3 kk = POS + ray * dist;
	gl_FragColor = vec4( vec3(brightness) + scene1(kk + 0.1, ray) + scene1(kk + 0.02, ray + 0.3) - dist * ray * 0.3, 1.0 );
}
