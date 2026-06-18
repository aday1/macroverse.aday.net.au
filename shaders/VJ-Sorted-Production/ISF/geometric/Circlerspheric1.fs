/*{
    "DESCRIPTION": "Circlerspheric1",
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

void main( void ) {

	vec2 pos = (gl_FragCoord.xy * 2.0 - resolution.xy) / min(resolution.x, resolution.y);

	vec2 Rpos = pos * (0.1 + sin(time * 0.5)) + (sin(time * 0.8) * 0.1);
	vec2 Gpos = pos * (0.26 + sin(time * 0.6)) + (cos(time * 0.2) * 0.1);
	vec2 Bpos = pos * (0.3 + sin(time * 0.4)) + (sin(time * 0.4) * 0.2);
	
	float r = length(fract(Rpos) * 2.0 - 1.0) * 0.2;
	float g = length(fract(Gpos) * 2.0 - 1.0) * 0.3;
	float b = length(fract(Bpos) * 2.0 - 1.0) * 0.25;
	r = r / (0.69 - length(pos)) - length(pos) * 0.3;
	g = g / (0.7 - length(pos)) - length(pos) * 0.3;
	b = b / (0.71 - length(pos)) - length(pos) * 0.3;
	gl_FragColor = vec4( vec3( r + sin(time * (g * 0.03)), g + sin(time * (b * 0.1)), b + cos(time * (g * 0.007))), 1.0 );
}
