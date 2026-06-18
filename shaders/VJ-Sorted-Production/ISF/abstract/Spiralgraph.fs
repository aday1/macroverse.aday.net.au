/*{
    "DESCRIPTION": "Spiralgraph",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        "abstract"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

varying vec2 surfacePosition;

void main(){
	vec2 pos = surfacePosition;
	//pos += 0.01*cos(133.*time);

	const float pi = 3.14159;
	const float n = 32.0;
	
	float radius = length(pos)*4.0 - 1.6;
	float t = atan(pos.y, pos.x)/pi;
	
	float color = 0.0;
	for (float i = 0.0; i < n; i++){
		color += 0.001/abs(0.2*sin(6.0*pi*(t + i/n*10.*time*0.01)) - radius*(1.+0.8*sin(time)));
	}
	
	gl_FragColor = vec4(vec3((1.-radius)*1.5, 1.5, (1.-radius)*1.15) * color, color);
	
}
