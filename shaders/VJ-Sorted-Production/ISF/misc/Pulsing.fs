/*{
    "DESCRIPTION": "Pulsing",
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

void main( void ) {
	vec2 p = (gl_FragCoord.xy * 2.0 - resolution) / min(resolution.x, resolution.y);
	
	p = mod(p, 0.2) - 0.1;
	
	float c = cos(time);
	float s = sin(time);
	mat2 m = mat2(c, s, -s, c);

	float f = 0.0001  / abs(p.x * p.y);

	float r = f * abs(sin(time * 5.0));
	float g = f * abs(sin(time * 3.0));
	float b = f * abs(sin(time * 1.5));

	gl_FragColor = vec4(r, g, b, 1.0);
}

