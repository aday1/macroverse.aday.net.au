/*{
    "DESCRIPTION": "PhotonMirror35",
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
	vec2 position = 2.0 * gl_FragCoord.xy / resolution.xy - 1.0;
	position.x *= resolution.x / resolution.y;
	float d2D = 0.02 / length (position) + time;
	float a2D = atan (position.y, position.x) + sin (time * 0.5) * 3.14159;
	gl_FragColor = vec4 (0.0 + sin (d2D * 10.0) * 1.0, 0.5 + sin (a2D * 8.0) * 0.5, 0.5 + sin (d2D * 20.0) * sin(a2D * 20.0) * 0.5, 1.0);

}
