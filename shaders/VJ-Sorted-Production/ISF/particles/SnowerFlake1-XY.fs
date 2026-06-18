/*{
    "DESCRIPTION": "SnowerFlake1-XY",
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
        }
    ],
    "TAGS": [
        "particles"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define MAX_DEPTH 5
vec4 GetColor(vec2 p)
{

	for(int depth=0;depth<MAX_DEPTH;++depth)
	{
		if(p.x<0.0||p.y<0.0||p.x>=1.0||p.y>=1.0){return vec4(0.0);}
		ivec2 coord=ivec2(p*3.0);
		int factor=coord.x+coord.y;
		//is there any ways to find if this is even or odd?
		if(factor==1||factor==3||factor==5||factor==7){return vec4(0.0);}
		
		p=mod(p,vec2(0.33777333333333))*3.0;
	}
	return vec4(1.0);
}

void main( void ) {
	float width=mouse.x*1000.0+100.0;
	vec2 startpoint=resolution*0.5 - vec2(width*0.5);
	
	vec2 p=(gl_FragCoord.xy-startpoint)/width;
	//if(p.x>0.0&&p.y>0.0&&p.x<1.0&&p.y<1.0){gl_FragColor=vec4(1.0);}
	
	gl_FragColor=GetColor(p);
}
