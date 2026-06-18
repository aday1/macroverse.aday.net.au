/*{
    "DESCRIPTION": "FunWall1",
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

// modified by @hintz
// modified by @oblomobka

#define PI 3.14159
#define TWO_PI (PI*2.0)
#define N 6.0

void main(void) 
{
	vec2 v = (gl_FragCoord.xy - resolution/10.0) / min(resolution.y,resolution.x) * 9.0;

	float col = 0.15;

	for(float i = 0.0; i < N; i++) 
	{
	  	float a = i * (TWO_PI/N);
		col += cos(TWO_PI*(v.x * cos(a) + v.y * sin(a)+ mouse.y +i*mouse.x + sin(time*0.001)*100.0 ));
	}
	
	 col /= .10;

	gl_FragColor = vec4(col*30.0, col*2.95,-col*3.0, .70);

}
