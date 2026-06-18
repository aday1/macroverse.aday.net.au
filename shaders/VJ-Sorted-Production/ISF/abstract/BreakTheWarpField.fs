/*{
    "DESCRIPTION": "BreakTheWarpField",
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
//--- torus
// by Catzpaw 2017
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

#define ITER 64
#define EPS 0.01
#define NEAR 0.
#define FAR 10.

float map(vec3 p){return 1.-(length(vec2(length(p.xz)-1.8,p.y))-.3);}

float trace(vec3 ro,vec3 rd){float t=NEAR,d;
	for(int i=0;i<ITER;i++){d=map(ro+rd*t);if(abs(d)<EPS||t>FAR)break;t+=step(d,1.)*d*.2+d*.5;}
	vec2 a=ro.xz+rd.xz*t;if(mod(atan(a.y,a.x)*4.+time*7.,1.)<.5)t+=3.;
	return min(t,FAR);}

void main(void){
	vec2 uv=(gl_FragCoord.xy-0.5*resolution.xy)/resolution.y;
	float si=sin(time*.3),co=cos(time*.3);uv*=mat2(co,-si,si,co);
	float v=1.-trace(vec3(cos(time),sin(time*.7)*.5,sin(time)),vec3(uv,-.5))/FAR;
	gl_FragColor=vec4(mix(vec3(1.5,1.3,1.1),vec3(1.1,1.5,1.3),sin(time*.2)*.5+.5)*v,1);
}
