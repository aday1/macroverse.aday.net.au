/*{
    "DESCRIPTION": "CosmicTerrain",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "noise"
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
        "space",
        "particles",
        "noise"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
//by maximilian4990
//creative commons by-nc
//Comic terrain  gigatron for glslsandbox using computed noise instead texture ; 
//thanks to iq for his great article about terrain raymarching
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

#define DELTA 0.01//use 0.0005 for higher quality
#define minIt 0.8
#define maxIt 3.0
#define EPS 0.025
float rand(vec2 n) { 
	return fract(sin(dot(n, vec2(12.9898, 4.1414))) * 43758.5453);
}

float noise(vec2 p){
	vec2 ip = floor(p);
	vec2 u = fract(p);
	u = u*u*(3.0-2.0*u);
	
	float res = mix(
		mix(rand(ip),rand(ip+vec2(1.0,0.0)),u.x),
		mix(rand(ip+vec2(0.0,1.0)),rand(ip+vec2(1.0,1.0)),u.x),u.y);
	return res*res;
}
 
float fbm(vec2 uv){
    float n = 8.0*noise(uv); uv *= 2.01;
    n += 4.0*noise(uv); uv *= 2.03;
    n += 2.0*noise(uv); uv *= 1.98;
    n += 1.0*noise(uv);
    return n/15.0;
}
float terrain(vec2 p){
    return fbm(2.8*p);
}
bool intersect(vec3 ro, vec3 rd, out float T){
    float h = 0.0;
    vec3 p = vec3(0.0);
    float t = 0.0;
    for(float t=minIt; t<maxIt; t+=DELTA){
        p = ro+rd*t;
        h = terrain(p.xz);
        if(p.y < h){
        	T = t-0.5*DELTA;
            return true;
        }
    }
    return false;
}
vec4 shade(vec3 p, float T){
	if(p.y<0.2){
    	return vec4(0.0,0.0,0.0,1.0);
    }
    vec4 col = vec4(terrain(p.xz));
    float x = (terrain(vec2(p.x-EPS,p.z))-terrain(vec2(p.x+EPS,p.z)))/EPS;
    bool nw=true;
    if(p.y>0.6){
    	col = vec4(1.0);
        nw = false;
    }
    if(x<-0.7067){
    	 col*=vec4(0.0,0.2,0.2,1.0);
    }
   
    return col;
}
vec4 fog(vec4 col, float T){
	return mix(col, vec4(0.4,0.4,0.6,1.0), T/maxIt);
}
void main()
{
	vec2 uv = -1.0+2.0*gl_FragCoord.xy / resolution.xy;
    
    uv   =  floor(uv*64.0)/64.0;
    
    float scl =   mod(gl_FragCoord.x ,4.0)-mod(gl_FragCoord.y ,4.0);
	
    vec3 ro = vec3(-time,0.8,0.0);
    vec3 rd = vec3(uv,-1.0);
    float T = 0.0;
    gl_FragColor = vec4(0.4,uv.y,.6,1.0);
    if(intersect(ro,rd, T))
    {
        gl_FragColor = fog(shade(ro+rd*T, T), T);// scan line // *scl;
    }
}
