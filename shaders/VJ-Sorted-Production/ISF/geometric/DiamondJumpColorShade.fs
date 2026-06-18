/*{
    "DESCRIPTION": "DiamondJumpColorShade",
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

#define PI 3.14159265359

//ppppu.swf - Minus8
void main( void ) {
	vec2 position = ( gl_FragCoord.xy / resolution.xy );
	position.x = (position.x - 0.5) * (resolution.x / resolution.y) + 0.5;
	position.x = (position.x - 0.5) / (1.1 - 0.2 * position.y) + 0.5;
	
	position.y -= 0.05 * abs(sin(time*2.0*PI));
	float topleft = step(fract(position.x*4.0), 0.5) * step(fract(position.y*4.0), 0.5);
	float bottomright = step(0.5, fract(position.x*4.0)) * step(0.5, fract(position.y*4.0));
	float diamond = 1.0 - mod(floor((position.x+position.y)*4.0) + floor((position.x-position.y)*4.0), 2.0);

	float color = 0.75 + (0.5 + 0.4 * topleft - 0.4 * bottomright) * diamond;
	float flash = step(2.5, mod(time, 4.0)) * max(0.0, (1.0 - mod(time*2.0, 1.0)))*diamond;
	vec4 finalcolor = mix(vec4(1.0, 0.25, 0.25, 1.0), vec4(0.25, 0.25, 1.0, 1.0), step(4.0, mod(time, 8.0)));
	float whiteflash = max(0.0, 1.0 - mod(time, 4.0)*2.0) * step(1.0, time);

	gl_FragColor = mix((finalcolor * color) - vec4(0.25, 0.25, 0.25, 1.0) * flash*0.25, vec4(1.0, 1.0, 1.0, 1.0), whiteflash);
}
