/*{
    "DESCRIPTION": "GlassFractalMask",
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
//Robert Schütze (trirop) 17.04.2017
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

// see also Collatz by iq
// https://www.shadertoy.com/view/llcGDS 

vec2 zpowz(vec2 z)
{
	float a = z.x;
	float b = z.y;
	float arg = atan(b,a-0.5+0.4*sin(time));
	float powArg = 0.5*b*log(a*a+b*b)+a*arg;
	return pow(a*a+b*b,a/2.)*exp(-b*arg)*vec2(cos(powArg),sin(powArg));
}

void main ( void )
{
	vec2 uv = (2.*gl_FragCoord.xy/resolution.y-vec2(resolution.x/resolution.y,1))*-3.;
	for(int i = 0;i<14;i++){
		uv = zpowz(uv.yx);
	}
	
	if (gl_FragCoord.y > mouse.y*400.0*sin(gl_FragCoord.x/30.0 + time)) discard;
	
	gl_FragColor = vec4(sign(uv.y),abs(uv),1.);	
}


