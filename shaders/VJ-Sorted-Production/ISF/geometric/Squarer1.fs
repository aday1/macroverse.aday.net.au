/*{
    "DESCRIPTION": "Squarer1",
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
        "geometric"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
precision mediump float;

varying vec2 surfacePosition;

float function(float x) {
	float sinY = 0.0;
	for(int i=0;i<50;i++){ //don't use a lot of loops plsssss xddd
		if(mouse.x*50.0<float(i))break;
		float n = float(i*2+1)+float(mouse.y<0.5);
		sinY+=(1.0/n)*sin(x*n);
	}
	return sinY;
}//by Robert Schütze - trirop(2016)

float graph_f(vec2 uv, vec2 resolution, float scale) {
    float graph = step(uv.y,function(uv.x))-step(uv.y+(scale/resolution.y),function(uv.x));
    graph += step(uv.y,function(uv.x+(scale/resolution.y)))-step(uv.y+(scale/resolution.y),function(uv.x));
    graph += step(uv.y,function(uv.x-(scale/resolution.y)))-step(uv.y+(scale/resolution.y),function(uv.x));
    return clamp(graph,0.,1.);
}//by anastadunbar

void main(){gl_FragColor = vec4(graph_f(surfacePosition*10.0,resolution.xy,10.0));}
