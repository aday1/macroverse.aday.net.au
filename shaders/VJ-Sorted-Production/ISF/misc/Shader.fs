/*{
    "DESCRIPTION": "Shader",
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
// Shader by Nicolas Robert [NRX]
// bqq

#ifdef GL_ES
precision mediump float;
#endif

void main (void) {
	vec2 position = 2.0 * gl_FragCoord.xy / resolution.xy - 1.0;
	position.x *= resolution.x / resolution.y;
	float d2D = 0.8 / length (position) + time  * 5.0;
	float a2D = atan (position.y, position.x);
	float qq = d2D * 0.1 + sin(d2D) * 0.2 * cos(a2D * 3.0) + sin(d2D * 0.2) * 0.3 * cos(a2D * 8.0)
		+ max(0.0, sin(d2D * 0.1 + 100.0) - 0.5) * cos(a2D * 20.0 + sin(d2D * 0.2) * 5.0)
		+ max(0.0, sin(d2D * 0.03 + 200.0) - 0.5) * cos(a2D * 15.0 + sin(d2D * 0.2) * 5.0);
	vec3 col = vec3(sin(qq * 2.0), sin(qq * 3.0), sin(qq * 5.0));
	
	col = col * 0.5 + 0.5;
	
	gl_FragColor = vec4(col, 1.0);
}
