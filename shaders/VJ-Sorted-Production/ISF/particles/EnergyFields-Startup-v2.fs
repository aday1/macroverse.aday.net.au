/*{
    "DESCRIPTION": "EnergyFields-Startup-v2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "particles"
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
        "space",
        "particles"
    ]
}*/

#extension GL_OES_standard_derivatives : enable

precision highp float;

uniform float time;
uniform vec2 mouse;
uniform vec2 resolution;
varying vec2 surfacePosition;

void main( void ) {

	vec2 position = surfacePosition + mouse * 4.0;

	float color = 0.0;
	color += sin( position.x * cos( time * 15.0 ) * 80.0 ) + cos( position.y * cos( time * 15.0 ) * 10.0 );
	color += sin( position.y * sin( time * 10.0 ) * 40.0 ) + cos( position.x * sin( time * 25.0 ) * 40.0 );
	color += sin( position.x * sin( time * 5.0 ) * 10.0 ) + sin( position.y * sin( time * 35.0 ) * 80.0 );
	color = sin( time * 10.0 ) * 0.5;

	gl_FragColor = vec4( vec3( color, color * 6.4, sin( color + time * 3.0 ) * 0.75 ), 1.0 );

}