/*{
    "DESCRIPTION": "KinaRetroPixel1",
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

#define PULSE(a,b,x) (step((a),(x)) - step((b),(x)))

float f(vec2 uv) {
	return PULSE(0.09, 0.1 + 0.06, fract(20.0 * (uv.x / uv.y) + 200.0 * uv.x + 0.1 * time));
}

void main(void)
{
	vec2 uv = ceil(100.0 * gl_FragCoord.xy / (0.5 * resolution.xy));
	
	gl_FragColor = vec4(0, 0, 0, 1);
	float light = f(uv) + f(uv + vec2(0.1, 0.1));
	gl_FragColor += vec4(light, 0.8 * light, 0, 0);
	float back = 0.2 * atan(6.0 * uv.x + time) + smoothstep(0.1, 0.2, sin(0.1 * time)) * cos(5.0 * dot(uv.y, uv.x) + 3.0 + 0.2 * time); 
	gl_FragColor += vec4(back, back, back, 0);
}
