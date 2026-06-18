/*{
    "DESCRIPTION": "StarField-DotMatrix-20",
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

float bump(float a)
{
	float q=a*2.0;
	if(q>1.0||q<-1.0){return 0.0;}
	float h=1.0-q*q;
	//return h;
	return h*h*h;
}
vec3 colormap(float l)
{
	return vec3(bump(l-0.25),bump(l-0.5),bump(l-0.75));
}

#define MAX_DEPTH 5
vec3 GetColor(vec2 p)
{
	int k;
	for(int depth=0;depth<MAX_DEPTH;++depth)
	{
		k=depth;
		if(p.x<0.0||p.y<0.0||p.x>=1.0||p.y>=1.0){break;}
		ivec2 coord=ivec2(p*4.0);
		int factor=coord.x+coord.y;
		//is there any ways to find if this is even or odd?
		if(factor==1||factor==3||factor==5||factor==7){break;}
		
		p=mod(p,vec2(0.25))*4.0;
	}
	return colormap(float(k)*0.2);
}

void main( void ) {
	float width=mouse.x*300.0+100.0;
	vec2 startpoint=resolution*0.5;
	
	vec2 p=(gl_FragCoord.xy-startpoint)/width;
	vec2 p2=vec2(length(p),(atan(p.y,p.x)+3.14)/6.28);
	//if(p.x>0.0&&p.y>0.0&&p.x<1.0&&p.y<1.0){gl_FragColor=vec4(1.0);}
	
	gl_FragColor=vec4(GetColor(p2),1.0);
}
