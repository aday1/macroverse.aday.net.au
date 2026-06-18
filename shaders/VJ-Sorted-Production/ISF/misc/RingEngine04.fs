/*{
    "DESCRIPTION": "RingEngine04",
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
        }
    ],
    "TAGS": [
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.xy );

	float color = p.x;

	float x = 2.0+fract(p.x*2.0)*6.0;
	
	float r = -(clamp(x-3.0,0.0,1.0)+clamp(-x+1.0,0.0,1.0))+1.0 -(clamp(x-9.0,0.0,1.0)+clamp(-x+7.0,0.0,1.0))+1.0;
	float g = -(clamp(x-5.0,0.0,1.0)+clamp(-x+3.0,0.0,1.0))+1.0;
	float b = -(clamp(x-7.0,0.0,1.0)+clamp(-x+5.0,0.0,1.0))+1.0;

	gl_FragColor = vec4( vec3( r, g , b), 1.0 );

}
