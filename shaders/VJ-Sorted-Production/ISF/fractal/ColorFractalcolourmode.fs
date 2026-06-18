/*{
    "DESCRIPTION": "ColorFractalcolourmode",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "fractal"
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
        "fractal"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
precision mediump float;

//Robert Schütze (trirop) 05.12.2015

void main(){
	vec3 p = vec3((gl_FragCoord.xy-resolution/2.0)/(resolution.y),mouse.x);
	vec2 p2 = vec2(gl_FragCoord.xy/resolution);
	
	for (int i = 0; i < 2; i++){
	   p = abs((abs(p)/dot(p, p)-1.0));
	   if(length(p) > 1.0 && length(p) < 1.01)break;
	}
	gl_FragColor.rgb = p;
	gl_FragColor.a = 1.0;
}
