/*{
    "DESCRIPTION": "BlackIteration1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "fractal"
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
        "fractal"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
//--- marine snow
// by Catzpaw 2017
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

#define ITER 96
#define EPS 0.01
#define NEAR 1.
#define FAR 34.

float map(vec3 p){vec3 p2=floor((p*4.+1.)*.5);p=mod(p*4.+1.,2.)-1.;
	float v=fract(sin(p2.x*133.3)*19.9+sin(p2.y*177.7)*13.3+sin(p2.z*199.9)*17.7);
	if(v<.99)return .8;return length(p)-4.4+v*4.;}

float trace(vec3 ro,vec3 rd){float t=NEAR,d;
	for(int i=0;i<ITER;i++){d=map(ro+rd*t);if(abs(d)<EPS||t>FAR)break;t+=step(d,1.)*d*.1+d*.3;}
	return min(t,FAR);}

void main(void){
	vec2 uv=(gl_FragCoord.xy-0.5*resolution.xy)/resolution.y;
	float si=sin(time*.1),co=cos(time*.1);uv*=mat2(si,-co,co,si);
	float v=1.-trace(vec3(0,time*7.,-time*10.),vec3(uv,-.8))/FAR;
	gl_FragColor=vec4(vec3(1.1,1.1,1.6)*v,1);
}

