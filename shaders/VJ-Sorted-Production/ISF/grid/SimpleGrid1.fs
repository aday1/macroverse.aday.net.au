/*{
    "DESCRIPTION": "SimpleGrid1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// SimpleGrid

#ifdef GL_ES
precision mediump float;
#endif

#define CELL_SIZE 30.0
#define LINE_WIDTH 1.0

void main( void ) 
{
	// simple
	float x = gl_FragCoord.x - gl_FragCoord.y;
	float y = gl_FragCoord.y + gl_FragCoord.x;

	// rotation
	x = gl_FragCoord.x - gl_FragCoord.y * (sin(time));
	y = gl_FragCoord.y + gl_FragCoord.x * (sin(time));
	
	// mouse
	//x = gl_FragCoord.x - gl_FragCoord.y * (sin(mouse.x) * 2.0 - 1.0);
	//y = gl_FragCoord.y + gl_FragCoord.x * (sin(-mouse.y) * 2.0 + 1.0);
	
	bool grid = mod(x, CELL_SIZE) < LINE_WIDTH || mod(y, CELL_SIZE) < LINE_WIDTH;
	
	float color = grid ? 0.0 : 0.5;

	gl_FragColor = vec4( vec3( color ), 1.0 );
}
