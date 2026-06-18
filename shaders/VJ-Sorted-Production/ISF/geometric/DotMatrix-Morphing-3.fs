/*{
    "DESCRIPTION": "DotMatrix-Morphing-3",
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

float rand(vec2 co){
    return fract(sin(dot(co.xy ,vec2(12.989958,78.13233))) * 43758.15453);
}
float f(float x) {
	return abs(mod(x,2.)-1.);
}
void main( void ) {

	vec2 uv = ((gl_FragCoord.xy-(resolution.xy/2.))/min(resolution.x,resolution.y));
	vec3 a = vec3(rand(uv)<f(time),rand(uv)<f(time+((1./5.)*2.)),rand(uv)<f(time+((2./3.)*2.)));
	vec3 b = pow(vec3(f(uv.x-1.),f(uv.x+(1./3.)),f(uv.x-(1./3.))),vec3(1.4))*1.4;
	gl_FragColor = vec4(mix(a,b,-4.*(sin((uv.x+sin(uv.y-time))+time)*.2)), 1.0 );
}
