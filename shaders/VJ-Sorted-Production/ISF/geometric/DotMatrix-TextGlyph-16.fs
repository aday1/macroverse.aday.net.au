/*{
    "DESCRIPTION": "DotMatrix-TextGlyph-16",
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
        "geometric",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

uniform vec4 inputColour;

// mouse.x
// mouse.y
// inputColour.x
// inputColour.y
// inputColour.z
// inputColour.w

uniform sampler2D backbuffer;
varying vec2 surfacePosition;
void main( void ) {
	float d = 7.*cos(-length(surfacePosition)+time*time/3.)*length(mouse-.5);
	vec4 cPrev = (texture2D(backbuffer, gl_FragCoord.xy / resolution) * 4.
		+ texture2D(backbuffer, fract((gl_FragCoord.xy + d*vec2( 1,0)) / resolution))
		+ texture2D(backbuffer, fract((gl_FragCoord.xy + d*vec2(-1,0)) / resolution))
		+ texture2D(backbuffer, fract((gl_FragCoord.xy + d*vec2(0, 1)) / resolution))
		+ texture2D(backbuffer, fract((gl_FragCoord.xy + d*vec2(0,-1)) / resolution)))*.125-.5;
	float rnd = fract(fract(time*2.15461234)*fract(time*6.634512)*fract(gl_FragCoord.x*.17673+2.1454)*fract(gl_FragCoord.y*.72435+.1672)*10000.);
	vec2 m = mouse-gl_FragCoord.xy/resolution;
	float l = length(cPrev.xz);
	l = min(l*1.01,.5);
	float a = atan(cPrev.x,-cPrev.z)+rnd*.05;
	a = cPrev.w < .5 ? -max(fract(dot(m,m)*4.)-.5,0.)*12.*min(max(abs(m.x*10.-2.),0.),1.) : dot(m,m) < .0001 ? 1. :  min(a, a*a*8.);
	cPrev = vec4(sin(a)*l+.5,sin(a-1.)*l+.5,-cos(a)*l+.5,1);
	gl_FragColor = cPrev;
}
