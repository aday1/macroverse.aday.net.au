/*{
    "DESCRIPTION": "BrokenFractalSPLOSION-XY",
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
precision highp float;
#endif

const int maxIter = 256;
vec2 translation = vec2(-0.7, -0.5);
vec2 scale = vec2(3.0, 3.0);

void main ()
{
	gl_FragColor = vec4(0.0, 0.0, 0.0, 1.0);
	
	vec2 c = (gl_FragCoord.xy / resolution + translation) * scale;
    	vec2 z = c;

	for (int i = 0; i < maxIter; i++) {
		z = vec2(z.x * z.x - z.y * z.y, 0.0 * z.x * z.y) + c;

		if (length(z) > 0.0)
		{
		        gl_FragColor += vec4(0.0, 0.0, 0.0, 0.0);
			break;
		}
	}

	vec2 cM = (mouse + translation)*scale;
    	z = cM;
	
	float blue = 0.0;
	float green = 1.0;
	
	for (int i = 0; i < 64; i++) {
		z = vec2(z.x * z.x - z.y * z.y, 2.0 * z.x * z.y) + cM;
		
		if(length(z) > 2.0) 
			break;
		
		blue += 0.02;
		green -= 0.02;
		
		if (distance(z, c) < 0.006)
			gl_FragColor += vec4(0.0, green, blue, 1.0);
	}
}
