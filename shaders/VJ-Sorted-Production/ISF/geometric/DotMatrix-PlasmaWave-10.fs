/*{
    "DESCRIPTION": "DotMatrix-PlasmaWave-10",
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
#define PI 3.1415926535

vec3 Check(vec2 pos) {
	float b = dot(sin(pos*3.), cos(pos*2.))<0.?1.:0.;
	return vec3(b);	
}
vec2 Rotate(vec2 pos, float angle) {
	return vec2(cos(angle)*pos.x - sin(angle)*pos.y, sin(angle)*pos.x + cos(angle)*pos.y);
}
void main( void ) {
	vec2 uv = gl_FragCoord.xy/resolution.y*2.-1.;
	uv.x -= resolution.x/resolution.y*.5;
	uv = Rotate(uv, sin(time)*.1);
	vec2 pos = vec2(0., 0.);
	float y = abs(uv.y+sin(uv.x+time*3.)/6.*(cos(time/3.)+1.) + sin((time+PI*.3)*2.)*.5 - .2);
	pos.y = 1./y + time*5.;
	pos.x = uv.x/y + sin(time+PI*.4)*5.;
	vec3 col = Check(pos);
	gl_FragColor = vec4(col, 10.)*y;
}
