/*{
    "DESCRIPTION": "DotMatrix-30",
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
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

#extension GL_OES_standard_derivatives : enable

uniform vec4 inputColour;

const float pi = 3.14;
const float tau = 6.28;
const float freqX = 55.0;
const float FUN_FACTOR = 1405.0 * tau;

void main( void ) {
	
	vec2 position = gl_FragCoord.xy / resolution.xy;
	vec2 relPos = position - 0.5;
	float freqY = freqX * (resolution.y / resolution.x);

	// colors
	vec3 color;
	color.r = dot(relPos, relPos) * 10.0;
	color.g = cos(position.x * tau * freqX);
	color.b = sin(position.y * tau * freqY);
	
	// animation
	color *= vec3(cos(time * relPos.x * FUN_FACTOR));
	color *= vec3(sin(time * relPos.y * FUN_FACTOR));
	
	// circle mouse thing -- but the zero means nothings
	color = mix(color, vec3(1.0) - color, 1.0 - smoothstep(0.0, 0.0, distance(position, mouse)));
	
	gl_FragColor = vec4(color, 1.0);

}
