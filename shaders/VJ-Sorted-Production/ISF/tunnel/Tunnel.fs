/*{
    "DESCRIPTION": "Tunnel",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "tunnel"
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
        "tunnel"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

#define M_PI 3.141592653

void main( void ) {

	vec2 frag2center = (gl_FragCoord.xy - (resolution.xy / 2.0));
	float dist = length(frag2center);
	
	float ang = 2.0*time/(1.0 - (1.0/dist));
	vec3 tunnel = clamp(vec3(cos(ang), cos(ang+(2.0*M_PI*0.33)), asin(ang+(2.0*M_PI*0.66)))*1.0 - (1.0/dist*20.0), 0.0, 1.0);
	gl_FragColor = vec4(clamp(tunnel+vec3(0.7, 1.5, 1)/(dist*0.125), 0.0, 1.0), 1.0);

}
