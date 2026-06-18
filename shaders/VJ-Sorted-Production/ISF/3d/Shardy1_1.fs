/*{
    "DESCRIPTION": "Shardy1",
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
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

bool RayTriangleIntersection(out float t, vec3 v0, vec3 edge1, vec3 edge2, vec3 rayOrigin, vec3 rayDir)		 
{  
	vec3 tvec = rayOrigin - v0;  
	vec3 pvec = cross(rayDir, edge2);  
	float  det  = dot(edge1, pvec);  

	det = 1.0 / det;
	float u = dot(tvec, pvec) * det;  
	vec3 qvec = cross(tvec, edge1); 
	float v = dot(rayDir, qvec) * det;  
	
	t = dot(edge2, qvec) * det;  
	
	return u >= 0.0 && u <= 1.0 && v >= 0.0 && (u+v) <= 1.0 && t >= 0.0;
}

void raytrace(out float t, vec3 o, vec3 d) {
	for(int i = 0; i < 128; i++) {
		float temp;
		float seed = float(i);
		float seed2 = time + float(i / 2) * 5.0;
		float seed3 = time + float(i / 4) * 3.0;
		float scale = float(i);
		vec3 bary = vec3( sin(seed2 * 0.2), -cos(seed3 * 0.3), -sin(seed3 * 0.4)) * 5.0;
		vec3 p0 = vec3( sin(seed), cos(seed), -sin(seed2));
		vec3 p1 = vec3( cos(seed2),-cos(seed),  sin(seed));
		vec3 p2 = vec3( -sin(seed3),-cos(seed2), -sin(seed));
		if(RayTriangleIntersection(temp, p0 + bary, p1 + bary, p2 + bary, o, d)) {
			t = min(t, temp);
		}
	}
}

void main( void ) {

	vec2 uv = ( gl_FragCoord.xy / resolution.xy ) * 2.0  - 1.0;

	vec3 dir = normalize(vec3(uv, 1.0));
	vec3 pos = vec3(sin(time), 2, cos(time) - 15.0);
	float t = 10000.0;
	raytrace(t, pos, dir);
	gl_FragColor = 1.0 - vec4(t * 0.01);
}
