/*{
    "DESCRIPTION": "Psychedelia",
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

#extension GL_OES_standard_derivatives : enable

#define clamps(x) clamp(x,0.,1.)
void main( void ) {
	vec2 uv = ( gl_FragCoord.xy / resolution.xy );
	uv -= .5;
	float a = atan(uv.x,uv.y);
	float r = length(uv);
	vec2 w = vec2(cos(a)/r,sin(a)/r);
	w = fract(vec2(w.x-time,w.y+time));
	float f = mix(mouse.x,0.45,(sin(a-time+(r*5.))+1.)/mouse.y); //.25;
	vec3 c = vec3(clamps((length(w-.5)-f)*r*100.));
	gl_FragColor = vec4(c, 1.0 );
}
