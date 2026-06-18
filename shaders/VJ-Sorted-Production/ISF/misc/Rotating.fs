/*{
    "DESCRIPTION": "Rotating",
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

vec2 rotate(float x, float y, float deg) {
	return vec2(x * cos(deg) - y * sin(deg), x * sin(deg) + y * cos(deg));
}

void main( void ) {
	vec2 pos = (gl_FragCoord.xy / resolution.xy) - vec2(.5, .5);
	vec2 s = rotate(pos.x / pos.y * .1, .05 / pos.y, time / 6. + sin(time));
	gl_FragColor = vec4(vec3(sin(time) * pos.y * pos.y * 1.0, pos.y < .0 ? sign((mod(s.x, .1) - .05) * (mod(s.y, .1) - .05)) * pos.y * pos.y * 20. : sin(time) * pos.y * pos.y * 1.0, sin(time) * pos.y * pos.y * 1.0), 1.0);
}
