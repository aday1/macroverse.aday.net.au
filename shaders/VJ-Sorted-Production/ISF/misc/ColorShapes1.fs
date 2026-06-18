/*{
    "DESCRIPTION": "ColorShapes1",
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

#define PI 3.14159265

const float it =  100.0;

void main( void ) {
	float mx = max(resolution.x, resolution.y);
	vec2 scrs = resolution/mx;
	vec2 uv = gl_FragCoord.xy/mx;
	uv -= scrs/2.0;
	vec2 m = vec2(mouse.x/scrs.x,mouse.y*(scrs.y/scrs.x))*50.0;
	
	float v = it;
	
	vec3 spacing = vec3(6.0, 6.0, 6.0*length(uv));
	
	// Fix your bullshit!!!!!!!		
	gl_FragColor = vec4(1.0);
	
	for (float i = 6.0; i < it; i++)
	{
		v--;
		if(floor(mod(uv.x*spacing.z*v+m.x, spacing.x))==1.0 && floor(mod(uv.y*spacing.z*v+m.y, spacing.y)) == 1.0){
			
			gl_FragColor = vec4(i/it*(sin(i/5.0-time*5.0+2.0*PI/3.0)+1.0)/2.0,
					    i/it*(sin(i/5.0-time*5.0+4.0*PI/3.0)+1.0)/2.0,
					    i/it*(sin(i/5.0-time*5.0+6.0*PI/3.0)+1.0)/2.0,
					    1.0);
		}
		
	}

}
