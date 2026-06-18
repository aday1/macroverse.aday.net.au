/*{
    "DESCRIPTION": "PurpleTunnel1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        "abstract"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

void main( void ) {
	vec2 pos = (gl_FragCoord.xy / resolution.xy);
	pos -= 0.5;

	float radius = length(pos);
	float angle = degrees(mod(pos.y, pos.x));
	float amod = 45.0 - mod(angle + 5.0 * time - 128.0 * log(radius), 45.0);
	amod /= 128.0;
	float orangle = 45.0 - mod(angle * 1.5 + 10.0 * time - 128.0 * log(radius), 45.0);
	orangle /= 128.0;
	
	float dist = sqrt(dot(pos, pos));
	float t = smoothstep(0.25, 0.15, dist);
	
	gl_FragColor = vec4(amod / 3.0 + orangle - t, orangle / 4.0-t, amod - t, 1.0);
}
