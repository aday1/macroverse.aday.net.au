/*{
    "DESCRIPTION": "MagicColorTrip1",
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
// http://glslsandbox.com/e#18467.0
// Hypno ns

#ifdef GL_ES
precision mediump float;
#endif

vec2 rotate(vec2 p){
	return vec2(atan(p.y / p.x), 1. / length(p));
}
void main(){
	vec2 pos = gl_FragCoord.xy / resolution * 2. - 1.;
	//pos = abs(pos);
	pos = rotate(pos);
	vec2 f = floor(pos * 10.0);
	float d = f.x + f.y;
	gl_FragColor = vec4(sin(d + time), sin(d + time * 2.), sin(d+ time * 3.), 1.0);
}
