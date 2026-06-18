/*{
    "DESCRIPTION": "fractal3",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "fractal"
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
        "fractal"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

//this is some crazy fractal shit!
//MrOMGWTF
void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.xy  * 2.0) - 1.0;
	float color = 0.0;
	float x,y,m,cx,cy;
	x = p.x;
	y = p.y;
	cx = -(sin(time * 0.5) * 0.5 + 0.5);
	cy = -(cos(time * 0.5) * 0.5 + 0.5);
	for(int i = 0; i < 7; i++)
	{
		x = abs(x);
		y = abs(y);
		m = x * x + y * y;
		x = x / m * (mouse.x) + cx;
		y = y / m * (1.+mouse.y) + cy;
	}
	gl_FragColor = vec4( m );

}
