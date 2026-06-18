/*{
    "DESCRIPTION": "MeltedColorsFRACT-XY",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "psychedelic"
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
        "psychedelic"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

//Robert Sch�tze (trirop) 05.12.2015
//evolved by PsyReCo
void main(){
	vec3 p = vec3((gl_FragCoord.xy-resolution/2.0)/(resolution.y),mouse.x);
	vec2 p2 = vec2(gl_FragCoord.xy/resolution);
	
	for (int i = 0; i < 4; i++){
	   p = abs(tan(atan(abs(atan(p)))/dot(p,sin( .5*p))));
	   if(length(p) > 0.9 && length(p) < 1.04)break;
	}
	gl_FragColor.rgb = p;
	gl_FragColor.a = 1.0;
}
