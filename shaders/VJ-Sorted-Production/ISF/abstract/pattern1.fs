/*{
    "DESCRIPTION": "pattern1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        "abstract"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

// by @hintz

void main(void)
{

	vec2 xy = (gl_FragCoord.xy / resolution.xy) * 2.0 / mouse;
	
	float x = xy.x - 2.0;
	float y = xy.y - 1.0;
	
	float t = time;
	
	vec3 color = vec3(0.0);
	
	for (float i = 0.0; i < 24.0; i++)
	{
		float yy = y + cos(x*i*0.5 + t+i*0.5) * 0.1;
		x += sin(y*i*0.5 + t+i*0.4321) * 0.1;
		y = yy;
		float value = abs(0.005 / (y*y+x*x));
		color += vec3(value*(i+10.0*sin(x*6.0+i*0.1+t))*0.1, value*i*0.25*cos(y*5.0+i*0.1+t), value*i*sin(x*y*5.0+i*0.1+t));
	}
	
	gl_FragColor = vec4(color, 1.0);
}


