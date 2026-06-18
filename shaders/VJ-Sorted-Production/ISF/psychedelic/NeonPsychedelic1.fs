/*{
    "DESCRIPTION": "NeonPsychedelic1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "psychedelic"
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
        "psychedelic"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define PI 3.14159265359

void main( void ) {
	vec3 light_color = vec3(1.2, 0.8, 0.6);
	
	float t = time;

	vec2 position = ( gl_FragCoord.xy / resolution.xy *.5 ) / resolution.x;
	
	float angle = atan(position.y, position.x)/(2. * PI);
	angle -= floor(angle);
	
	float rad = length(position);
	float color = 0.0;
	float brightness = 0.015;
	float speed = .3;
	
	float adist = .2/rad;
	float dist  = (t*1.5 + adist);
	dist = abs(fract(dist)-.5);
	color = (1.0/(dist)) * brightness;
	
	gl_FragColor =  vec4(color,color,color,1.0)*vec4(light_color,1.0);
}
