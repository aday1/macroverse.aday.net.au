/*{
    "DESCRIPTION": "ColourThing",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "particles"
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
        "particles"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
//Rainbow cell colors by TLM

#ifdef GL_ES
precision mediump float;
#endif

uniform vec4 inputColour;

#define pi 3.1415926
#define c mouse.x*5.0
#define levels mouse.y*13

float rand(vec2 co)
{
	return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

void main( void ) 
{
	float t = time / 1.0;
	vec2 p = ( gl_FragCoord.xy / resolution.xy );

	float colr = 0.0, colg = 0.0, colb = 0.0;
	float stepcolr = 0.0, stepcolg = 0.0, stepcolb = 0.0;

	//Red
	for (float x = 0.0; x < c; x++)
	{
		float randomo = rand(vec2(x/c, 1.0));
		float randommo = rand(vec2(1.0, x/c));
		colr += sin(randomo*t*x/c + p.x*15.0*randomo + p.y*15.0*randommo);
	}
	for (float x = -c*0.5; x < c*0.5; x+=c/float(levels))
	{
		
		stepcolr += step(colr,float(x));
	}
	stepcolr = stepcolr / float(levels);
	
	//Green
	for (float x = 0.0; x < c; x++)
	{
		float randomo = rand(vec2(x/c, 0.5));
		float randommo = rand(vec2(0.5, x/c));
		colg += sin(randomo*t*x/c + p.x*15.0*randomo + p.y*15.0*randommo);
	}
	for (float x = -c*0.5; x < c*0.5; x+=c/float(levels))
	{
		
		stepcolg += step(colg,float(x));
	}
	stepcolg = stepcolg / float(levels);
	
	//blue
	for (float x = 0.0; x < c; x++)
	{
		float randomo = rand(vec2(x/c, 0.0));
		float randommo = rand(vec2(0.0, x/c));
		colb += sin(randomo*t*x/c + p.x*15.0*randomo + p.y*15.0*randommo);
	}
	for (float x = -c*0.5; x < c*0.5; x+=c/float(levels))
	{
		
		stepcolb += step(colb,float(x));
	}
	stepcolb = stepcolb / float(levels);

	gl_FragColor = vec4(stepcolr, stepcolg, stepcolb, 1.0);

}
