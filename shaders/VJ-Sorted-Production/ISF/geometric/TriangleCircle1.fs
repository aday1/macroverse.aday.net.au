/*{
    "DESCRIPTION": "TriangleCircle1",
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
#ifdef GL_ES
precision mediump float;
#endif

void main(void){
    vec2 p = (gl_FragCoord.xy * 2.0 - resolution) / min(resolution.x, resolution.y);
    
    // ring
	float t;
	if(p.y > 0.452)
	{
		t = 0.0;
	}
	else
	{
		if(p.y < -0.21)
		{
			t=0.0;
		}
		else if(p.x < -0.33)
		{
			t = 0.0;
		}
		else if(p.x > 0.33)
		{
			t = 0.0;
		}
		else
		{
    			t = 0.002/abs(p.x)+ 0.002/abs(0.2+p.y) + 0.002/abs(-0.23 +0.5* p.y - p.x) + 0.002/abs(-0.23 +0.5* p.y + p.x) + 0.002/abs(0.2 - length(p));
    	
		}
	}
    gl_FragColor = vec4(vec3(t,0.3, 0.3), 1.0);
}
