/*{
    "DESCRIPTION": "ShapeCutout1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "geometric"
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
        "geometric"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

float sph(in vec3 p, float r) 
{
	return length(p) - r + sin(p.x*40.0+time)*0.01;  	
}
float scene(in vec3 p) 
{
	float sph0 = sph(p,0.5); 
	float sph1 = sph(p+vec3(0.2,0.2,-0.2+sin(time)*0.1), 0.5); 
	return max(sph0,-sph1); 
}
vec3 get_normal(in vec3 p) {
	vec3 eps = vec3(0.001,0,0); 
	float nx = scene(p+eps.xyy)-scene(p-eps.xyy); 
	float ny = scene(p+eps.yxy)-scene(p-eps.yxy); 
	float nz = scene(p+eps.yyx)-scene(p-eps.yyx);
	return normalize(vec3(nx,ny,nz)); 
}

void main( void ) {

	vec2 p = 2.0*( gl_FragCoord.xy / resolution.xy ) -1.0;
	p.x *= resolution.x/resolution.y; 
	
	vec3 col = vec3(0.2,0.2,0.2); 
	vec3 ro = vec3(0,0,1.0); 
	vec3 rd = normalize(vec3(-p.x,-p.y,-1.0)); 
	
	vec3 pos = ro; 
	float d, dist = 0.0; 
	for (int i = 0; i < 64; i++) {
		d = scene(pos); 
		pos += d*rd; 
		dist += d; 
	}
	if (abs(d) < 0.01 && dist < 10.0) {
		vec3 n = get_normal(pos); 
		
		vec3 l = normalize(vec3(1,-1,1)); 
		float dt = clamp(dot(n,l),0.0,1.0);
		col = vec3(1,1,1)*dt; 

	}
	gl_FragColor = vec4(col, 1.0); 
}
