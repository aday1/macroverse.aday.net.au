/*{
    "DESCRIPTION": "ColorOrbnaughticalXY",
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
//Robert Schütze (trirop) 17.04.2017
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

vec2 zpowz(vec2 z){
	float a = z.x;
	float b = z.y;
	float arg = atan(b,a);
	float powArg = 0.5*b*log(a*a+b*b)+a*arg;
	return pow(a*a+b*b,a/2.)*exp(-b*arg)*vec2(cos(powArg),sin(powArg));
}

void main ( void ){
	vec2 uv = (2.*gl_FragCoord.xy/resolution.y-vec2(resolution.x/resolution.y,1))/3.8;
	vec2 c = uv;
	for(int i = 0;i<4;i++){
		uv = zpowz(abs(vec2(length(c)*5.)*uv.yx)+vec2(i)*mouse.x*mouse.y).yx;
	}
	gl_FragColor = vec4(abs(uv)*2.,sign(uv.y),1.);	
}
