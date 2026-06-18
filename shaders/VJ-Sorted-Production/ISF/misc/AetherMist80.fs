/*{
    "DESCRIPTION": "AetherMist80",
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
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "misc"
    ]
}*/
#define E 2.71828182846

varying vec2 position;

uniform vec4 color;





#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

uniform vec4 inputColour;

void main( void ) {

	vec2 position = vec2( gl_FragCoord.x * 0.05, gl_FragCoord.y * 0.05) + time;

	float color = 0.6;
	color = mod( sin( position.y), cos( position.x ) ) * mod( position.x * position.y, position.y * mouse.x );
	color += mouse.y * sin( position.x ) * inputColour.x * cos( position.y ) / cos( inputColour.y * position.x ) * sin( inputColour.z * position.y );
	gl_FragColor = vec4( color * 0.25, color * 0.5, color * 0.75, 1.0 );

}
