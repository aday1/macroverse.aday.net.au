/*{
    "DESCRIPTION": "Illusionary",
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
// illusory // expanded convertered into resolume plugin by aday // April/17 //

#ifdef GL_ES
precision mediump float;
#endif

// #extension GL_OES_standard_derivatives : enable

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)

float dCircle(vec2 p, float r) {
	return length(p) - r;
}

float opOr(float a, float b) {
	return min(a, b);
}

float opAnd(float a, float b) {
	return max(a, b);
}

float opXor(float a, float b) {
	return min(max(a, -b), max(b, -a));
}

void main( void ) {
	vec2 p = (gl_FragCoord.xy * 2.0 - resolution) / min(resolution.x, resolution.y);
	float a = dCircle(p - vec2(-0.5+sin(time*41.000)/7., sin(time*40.000)/7.), mouse.x);
	float b = dCircle(p - vec2(0.5-sin(time*39.000)/7., sin(time*36.000)/7.), mouse.y);
	float dist = opXor(a, b);
	gl_FragColor = vec4(vec3(sign(dist))*tan(time*100.)*10000., 1.0);

}
