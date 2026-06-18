/*{
    "DESCRIPTION": "Pulsing-5",
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

#define PI 3.14159

void main(void) {

	//gl_FragCoord.xy
	
	float waveWidth =  4.0 + (abs(mouse.y - 0.5) * resolution.y) / 4.0;
	float lineSize = 5.0;
	//waveWidth = ((waveWidth * waveWidth) / 20.0) + 2.0;
	
	//float color = sin(clamp(gl_FragCoord.y - (resolution.y / 2.0), 0.0, PI));
	//float color = sin(clamp((gl_FragCoord.y / lineSize) - (resolution.y / (2.0 * lineSize)), 0.0, PI));
	float color = sin(clamp((gl_FragCoord.y / lineSize) - (resolution.y / (2.0 * lineSize)) + ((cos(clamp(gl_FragCoord.x - (mouse.x * resolution.x), (PI * -waveWidth), (PI * waveWidth)) / waveWidth)) + 1.0) * ((-(mouse.y * (resolution.y / 1.0) - (resolution.y / 2.0)) / 2.0) / lineSize), 0.0, PI/ 2.0));
	//color *= sin(clamp(gl_FragCoord.x - (resolution.x * mouse.x), 0.0, 3.14159));
	
	gl_FragColor = vec4(vec3(color * abs(sin((gl_FragCoord.x - (time * 50.0) - (sin(time * 0.2) * 60.0)) * 0.2)), color * abs(sin(((gl_FragCoord.x + (2.0 * PI)) - (time * 50.0) - (sin(time * 0.4) * 60.0)) * 0.2)), color * abs(sin(((gl_FragCoord.x - (2.0 * PI)) - (time * 50.0) - (sin(time * 1.0) * 60.0)) * 0.2))), 1.0);
}
