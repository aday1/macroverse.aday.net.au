/*{
    "DESCRIPTION": "InstantCircler1",
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
        "geometric",
        "texture-input"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

//dashxdr 20150605, expanding circles

uniform sampler2D bb;
varying vec2 surfacePosition;

void main( void )
{
	const float r1 = 0.05;

	const int NUM = 60;
	float numf = float(NUM);
	float slowtime = time * .125;
	const float timestep = .0125;
	float best = 0.125;
	for(int i=0;i<NUM;++i)
	{
		float a1 = 6.2831853*float(i)/float(NUM);
		vec2 pos = r1*vec2(cos(a1), sin(mod(r1,cos(1.0-a1))*a1));
		float tm = max(slowtime - float(i)*timestep,-1.0);
		tm = min(tm, mod(tm, numf*timestep));
		float d = 500.0*abs(length(pos - surfacePosition) - tm);
		if(d<3.0)
			best = max(best, 2.0-d);
	}

	vec4 color1 = vec4(1.000, 0.666, mod(2.0,best), 1.0);
	vec4 color2 = vec4(mod(best,1.0), 0.111, fract(cos(best)), 1.0);
	gl_FragColor = vec4(mix(color1, color2, pow(cos(best),sin(slowtime))+0.25));
}

