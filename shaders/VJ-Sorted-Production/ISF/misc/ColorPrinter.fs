/*{
    "DESCRIPTION": "ColorPrinter",
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

float rand(float x){
    return fract(sin((x*12.9898*cos(x*8.523314)) * 43758.5453));
}

void main( void ) {

	vec2 uv = ( gl_FragCoord.xy / resolution.xy);
	uv *= resolution.xy/min(resolution.x, resolution.y);
	
	float a = float(mod(uv.x*20.,1.)>.5 ^^ mod(uv.y*20.,1.)>.5);
	float b = float(fract(uv.y*floor(rand(floor(uv.y*9.))*20.))>0.4)*a;
	float c = float(fract(uv.y*floor(rand(floor(uv.y*6.))*30.))>0.4)*a;
	float d = float(fract(uv.y*floor(rand(floor(uv.y*5.))*25.))>0.4)*a;
	
	gl_FragColor = vec4(b,c,d, 1.0 );
}
