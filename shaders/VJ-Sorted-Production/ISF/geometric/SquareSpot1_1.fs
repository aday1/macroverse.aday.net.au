/*{
    "DESCRIPTION": "SquareSpot1",
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
        }
    ],
    "TAGS": [
        "geometric"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

void main( void ) {
//	#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
	vec2 pos = ( gl_FragCoord.xy / resolution.xy );
	vec2 uv = pos;
	uv /= 1.+fract(time);
	gl_FragColor = vec4(fract(uv*10.),1.-((length(pos-((fract(time)/2.)+.5))-.1)*200.), 1.0 );
}
