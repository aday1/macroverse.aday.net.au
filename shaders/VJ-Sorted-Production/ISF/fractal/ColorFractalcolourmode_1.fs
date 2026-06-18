/*{
    "DESCRIPTION": "ColorFractalcolourmode",
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
        }
    ],
    "TAGS": [
        "fractal"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

//Robert Schütze (trirop) 05.12.2015

void main(){
	vec3 p = vec3((gl_FragCoord.xy-resolution/2.0)/(resolution.y),mouse.x);
	vec2 p2 = vec2(gl_FragCoord.xy/resolution);
	
	for (int i = 0; i < 2; i++){
	   p = abs((abs(p)/dot(p, p)-1.0));
	   if(length(p) > 1.0 && length(p) < 1.01)break;
	}
	gl_FragColor.rgb = p;
	gl_FragColor.a = 1.0;
}
