/*{
    "DESCRIPTION": "HorizonLight-Fire-AtmosphericHaze",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
        }
    ],
    "TAGS": [
        "grid"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

void main()
{	
	//mouse ranges from 0 to 1 (origin bottom left)
	//resolution is 600
	//fragCoord ranges from 0 to 600 (origin bottom left)
	//mx ranges from 0 to 600 (origin left)
	//my .......................... right
	float mx = mouse.x * resolution.x;
	float my = mouse.y * resolution.y;
	vec4 grey = vec4(0.2,0.2,0.2,1);
	vec4 blue = vec4(0.0,0.0,1.0,1);
	vec4 gold = vec4(1.0,0.7,0.0,1);
	vec4 fogcolor = mix(blue, gold, .5);
	
	// Horizon
	if ((gl_FragCoord.y/resolution.y)/mouse.y > 1.15) {
		gl_FragColor = grey;
		return;
	}
	
	float outcolor = 0.0;
	float yRange = 0.0;
	//[0, 1) from bottom to cursor
	for (int i = 0; i < 16; i++) {
		yRange = (gl_FragCoord.y/resolution.y)/mouse.y + .0004*float(i);
		//[-1,1) from left to right
		float diffX = ((gl_FragCoord.x/resolution.x) - 0.5) * 2.0 + .0004*float(i);
		float tileWidth = 0.2 - ((yRange - 0.15) * 0.2);
		bool xFlip = mod(diffX, tileWidth * 2.0) > tileWidth;
		bool yFlip = mod(yRange + (tileWidth*time*10.0), tileWidth * 2.0) < tileWidth;
		
		bool finalColor = xFlip != yFlip;
		if (finalColor) outcolor += 1.0/16.0;
	}
	
	gl_FragColor = mix(mix(blue, gold, outcolor), fogcolor, pow(yRange, 2.0));
}
