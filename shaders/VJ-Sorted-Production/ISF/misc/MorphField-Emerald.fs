/*{
    "DESCRIPTION": "MorphField-Emerald",
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
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

vec3 color(vec2 pos, float d){
	vec2 uv = abs(fract(pos*40.)-.5)*d*40.;
	return vec3(1.-min(uv.x, uv.y));
}

vec2 transform(vec2 pos){
	float x = pow(length(pos), 0.15);
	float y = atan(pos.x, pos.y) / (3.1415926*2.0);
	return vec2(x, y);
}

void main( void ) {
	vec2 pos = (gl_FragCoord.xy * 2.0 - resolution) / resolution.y;
	float d = length(pos);
	gl_FragColor = vec4(color(transform(pos)+mouse*.5, d)*vec3(0.0, 1.0,0.0), 1.)*d;
}
