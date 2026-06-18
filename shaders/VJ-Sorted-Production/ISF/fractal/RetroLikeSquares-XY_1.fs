/*{
    "DESCRIPTION": "RetroLikeSquares-XY",
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
/*** Sierpinski carpet ***/

#ifdef GL_ES
precision mediump float;
#endif

#define PI 3.14159

const float it = 5.0; // Number of iterations

void main( void ) {

	float mx = max(resolution.x, resolution.y);
	vec2 scrs = resolution/mx;
	vec2 uv = gl_FragCoord.xy/mx;
	vec2 m = vec2(mouse.x/scrs.x,mouse.y*(scrs.y/scrs.x));
	
	uv+=m;
	float v = pow(3.0,it)+100.0;
	
	gl_FragColor = vec4(0.0); // Background color
	
	for (float i = 0.0; i < it; i++)
	{
		if(floor(mod(uv.x*v,3.0))==1.0 && floor(mod(uv.y*v,3.0))==1.0){
			
			gl_FragColor = vec4(((sin(i*5.0-time*0.5+2.0*PI/3.0)+1.0))/2.0, // RED
					    ((sin(i*5.0-time*0.5+4.0*PI/3.0)+1.0))/2.0, // GREEN
					    ((sin(i*5.0-time*0.5+6.0*PI/3.0)+1.0))/2.0, // BLUE
					    1.0);					// ALPHA
		}
		v/=3.0;	
	}
}
