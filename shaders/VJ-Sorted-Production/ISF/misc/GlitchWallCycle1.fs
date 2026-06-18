/*{
    "DESCRIPTION": "GlitchWallCycle1",
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
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

uniform vec4 inputColour;

// mouse.x
// mouse.y
// inputColour.x
// inputColour.y
// inputColour.z
// inputColour.w

void main( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.xy ); // + mouse / 4.0;

	float color = inputColour.x;
	color += sin( position.x * cos( time / 15.0 ) * 180.0 ) + cos( position.y * cos( time / 55.0 ) * 1310.0 );
	color += sin( position.y * sin( time / 22.0 ) * 40.0 ) + cos( position.x * sin( time / 5.0 ) * 240.0 );
	color += sin( position.y * sin( time / mouse.y ) * 10.0 ) + sin( position.x * sin( time / 35.0 ) * 80.0 );
	color *= sin( time / 100.0 ) * inputColour.z;

	gl_FragColor = vec4( vec3( color, color * inputColour.y, sin( color + time / inputColour.w ) * mouse.x ), inputColour.x );

}
