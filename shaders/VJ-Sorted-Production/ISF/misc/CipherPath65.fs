/*{
    "DESCRIPTION": "CipherPath65",
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

//replicating something from quartzcomposer iterator/sprite in glsl. -gtoledo

float rect( vec2 p, vec2 b, float smooth )
{
	vec2 v = abs(p) - b;
  	float d = length(max(v,0.0));
	return 1.0-pow(d, smooth);
}

void main( void ) {
	
	vec2 pos = -resolution/2.0 +gl_FragCoord.xy;
	pos.y -=50.;
	vec4 tx;
	float amplitude = 100.*sin(time*.1)/4.+50.;
		for (int i = 0; i < 20; ++i){
		pos.y +=sin(time*.2+float(i))*amplitude;
		pos.x +=cos(time*.2+float(i))*amplitude;
		
			for (int i = 0; i < 8; ++i){
			tx += vec4(rect(vec2(pos.x+sin(float(i)*time)*64.,pos.y+cos(float(i)*time)*64.),vec2(5.,5.),0.0005));
			}		

		}

	gl_FragColor = tx;
}
