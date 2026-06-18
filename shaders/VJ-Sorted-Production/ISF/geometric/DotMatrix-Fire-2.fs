/*{
    "DESCRIPTION": "DotMatrix-Fire-2",
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
        }
    ],
    "TAGS": [
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
precision mediump float;

#define PI 3.1415926536
const mat3 mRot = mat3(0.9553, -0.2955, 0.0, 0.2955, 0.9553, 0.0, 0.0, 0.0, 1.0);
const vec3 ro = vec3(0.0,0.0,-4.0);
const vec3 cRed = vec3(1.0,0.0,0.0);
const float maxx = 0.378;
void main() {
	gl_FragColor=vec4(0.);
	vec2 uv = (gl_FragCoord.xy / resolution.xy);
	vec2 uvR = floor(uv*resolution);
	vec2 g = step(2.0,mod(uvR,16.0));
	uv = uvR/resolution;
	float xt = mod(time+1.0,6.0);
	float dir = (step(xt,3.0)-.5)*-2.0;
	uv.x -= (maxx*2.0*dir)*mod(xt,3.0)/3.0+(-maxx*dir);
	uv.y -= abs(sin(4.5+time*1.3))*0.5-0.3;
	vec3 rd = normalize(vec3((uv*2.0-1.0)*vec2(1.0,resolution.y/resolution.x),1.5));
	float b = dot(rd,ro);
	float t1 = b*b-15.6;
	vec3 nor = normalize(ro+rd*(-b-sqrt(t1))*mRot);
	vec2 tuv = floor(vec2(atan(nor.x,nor.z)/PI+((floor((time*-dir)*60.0)/60.0)*0.5),acos(nor.y)/PI)*8.0);
	gl_FragColor += vec4(mix(vec3(0.),mix(cRed,vec3(1.),clamp(mod(tuv.x+tuv.y,2.0),0.0,1.0)),1.0-step(t1,0.0)),1.0);  
} 
