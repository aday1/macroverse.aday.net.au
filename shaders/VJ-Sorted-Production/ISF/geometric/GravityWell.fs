/*{
    "DESCRIPTION": "GravityWell",
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "geometric"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE

// An attempt to visualize my gravity well if I keep eating all this junkfood 

#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

uniform vec4 inputColour;

float scene(in vec3 p) 
{
	return dot(p, vec3(0,1,0))+1.5+1.0/(0.1+mouse.x*length(p.xz-vec2(2.0*cos(0.8*time),-4.0+sin(time))));
}

float rep(float x, float h) { return mod(x+h,2.0*h)-h; }
vec3 get_tex(in vec3 p)
{
	vec3 col1 = vec3(1); 
	if (abs(rep(p.z,0.2)) < 0.01 || abs(rep(p.x,0.2)) < 0.01) return col1; 
	return vec3(0); 
}
void main( void ) {

	vec2 p = 2.0*( gl_FragCoord.xy / resolution.xy ) -1.0;
	p.x *= resolution.x/resolution.y; 
	vec3 col = vec3(0); 
	
	vec3 ro = vec3(0,0,1); 
	vec3 rd = normalize(vec3(p.x,p.y,-1)); 
	
	vec3 pos = ro; 
	float d, dist = 0.0; 
	
	for (int i = 0; i < 80; i++) {
		d = scene(pos)*0.5;
		pos += rd*d; 
		dist += d; 
	}
	if (dist < 1000.0 && abs(d) < 0.001) {
		vec3 tex = get_tex(pos); 
		col = tex*vec3(1)*clamp(1.3-0.10*dist, 0.0, 1.0);  
	}
	
	gl_FragColor = vec4(col, 1.0); 
}
