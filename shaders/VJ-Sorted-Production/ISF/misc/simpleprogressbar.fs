/*{
    "DESCRIPTION": "simpleprogressbar",
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

// simple progress bar

#define progress mouse.x-.5	

void main( void ) 
{
	vec4 bg = vec4(.2,.2,.2,1.);
	vec4 fg1 = vec4(.1,.6,.1,1.);
	vec4 fg2 = vec4(.4,.8,.3,1.);
	vec2 sz = vec2(.25,0.016);
	
	vec2 position = vec2((gl_FragCoord.x - resolution.x/2.0)/resolution.x, (gl_FragCoord.y - resolution.y/2.0)/resolution.x);

	vec4 color = vec4(0);
	if(abs(position.x)<sz.x-sz.y && abs(position.y) < sz.y || length(position-vec2(sz.x-sz.y,0))<sz.y || length(position-vec2(-sz.x+sz.y,0))<sz.y)
	{
		color = bg;
		if(progress>position.x)
		if(mod(position.x-position.y*.75-time/8.,4.*sz.y)>2.*sz.y)color = fg1; else color = fg2;
	}

	gl_FragColor = color;
	
}
