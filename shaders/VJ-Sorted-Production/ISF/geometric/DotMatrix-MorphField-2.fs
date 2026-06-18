/*{
    "DESCRIPTION": "DotMatrix-MorphField-2",
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

#define STEP 0.2
#define R_STEP 0.1

float color(vec2 p){
	
	if(mod(p.x, R_STEP) < R_STEP * 0.4){
		p.y += STEP * 0.1;
	}
	if(mod(p.y, STEP) < STEP * 0.5){
		return 1.0;
	}
	else{
		return 0.0;
	}
}

vec2 transform(vec2 p){
	return vec2(atan(p.y, p.x) + time * 0.5, 1.0 / length(p) + time * 1.3);
}

void main( void ) {

	vec2 pos = (gl_FragCoord.xy * 2.0 - resolution) / resolution.y ;

	gl_FragColor = vec4(color(transform(pos))) - max((1.0 - dot(pos, pos)), 0.0);

}
