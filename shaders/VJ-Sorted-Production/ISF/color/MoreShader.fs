/*{
    "DESCRIPTION": "MoreShader",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "color"
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
        "color"
    ]
}*/
uniform float brightness;
uniform float speed;
#define E 2.71828182846

varying vec2 position;

uniform vec4 color;
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

uniform vec4 inputColour;

void main( void ) {
	vec3 light_color = vec3(inputColour.x,mouse.y,mouse.x);
	
	float t = time*20.0;
	vec2 position = ( gl_FragCoord.xy -  resolution.xy*.5) / resolution.x;

	// 256 angle steps
	float angle = atan(position.y,position.x)/(2.*3.14159265359);
	angle -= floor(angle);
	float rad = length(position);
	
	float color = inputColour.w;
	float brightness = 0.015;
	float speed = inputColour.y;
	
	for (int i = 0; i < 10; i++) {
		float angleRnd = floor(angle*14.)+1.;
		float angleRnd1 = fract(angleRnd*fract(angleRnd*.7235)*45.1);
		float angleRnd2 = fract(angleRnd*fract(angleRnd*.82657)*13.724);
		float t = t*speed + angleRnd1*inputColour.w;
		float radDist = sqrt(angleRnd2+float(i));
		
		float adist = radDist/rad*.1;
		float dist = (t*.1+adist);
		dist = abs(fract(dist)-.5);
		color +=  (inputColour.z / (dist))*cos(0.7*(sin(t)))*adist/radDist * brightness;
		angle = fract(angle+.61);
	}

	gl_FragColor = vec4(color,color,color,inputColour.y)*vec4(light_color,inputColour.z);
}
