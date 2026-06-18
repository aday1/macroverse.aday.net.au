/*{
    "DESCRIPTION": "NovaBurst01-2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "misc"
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
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

// shorter version --novalis

void main(void) {
	vec2 p = gl_FragCoord.xy/resolution.xx*2.-vec2(1.,.5);
	gl_FragColor = length(p)*vec4(vec3((mod(.3/length(p)+time*.067,.1)>.05)^^(mod(atan(p.x/p.y)*7./44.+time*.05,.1)>.05)),3.)*.4;
	gl_FragColor /= vec4(0.3, 0.8, 0.2, 1.0);
	gl_FragColor *= vec4(0.8, 0.3, 0.9, 1.0);
}
