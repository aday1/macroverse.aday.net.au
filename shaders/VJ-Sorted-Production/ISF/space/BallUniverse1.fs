/*{
    "DESCRIPTION": "BallUniverse1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "space"
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
        "space"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float; //g
#endif

#define k time*0.3
#define m(x) length(mod(d*x+vec3(sin(k),cos(k),k), 0.4) - 0.2) - abs(sin(time)*0.07)

void main( void ) {
	vec2 uv = gl_FragCoord.xy / resolution.x;
	uv += vec2(mouse.x, mouse.y);
	float t=0.;
	
	vec3 d=normalize(vec3(1.0 + log(uv)*1.0-1.0, 1.0));
	
	for(int i=0;i<128;i++) t += m(t);
	
	gl_FragColor = vec4(vec3(t*0.05+ t*0.01),1);
}
