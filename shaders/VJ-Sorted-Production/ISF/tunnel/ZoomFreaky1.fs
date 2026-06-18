/*{
    "DESCRIPTION": "ZoomFreaky1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "tunnel"
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        },
        {
            "NAME": "contrast",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Contrast"
        }
    ],
    "TAGS": [
        "tunnel"
    ]
}*/
#define E 2.71828182846
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

uniform vec4 inputColour;

vec2 mash(vec2 x)
	{
	return mod(mod(x, -sin(time*inputColour.y))*(inputColour.w/sqrt(5.)), x*5.61803);	
	}

vec2 bash(vec2 x)
	{
	return mod(mod(x, cos(time*x)), sin(x*6.2831853)*x), cos(x*3.141592653);		
	}

void main( void ) 
{
float t=time*mouse.x;
	
vec2 p=-1.0+2.0*gl_FragCoord.xy/resolution;
p+=mash(p*sin(bash(p*mouse.y)+t*inputColour.y)-p*cos(mash(p*0.1)+t*0.5));
p*=bash(p);	
vec2 col=abs(fract((p)*1.61803)+t*(mash(p)));
p=mod(p,0.2)*inputColour.w;
col*=mash(col);	
float contrast=1.0+abs(cos(t))*40.0;
gl_FragColor=length(300.0*p*p-normalize(p)*0.2)*vec4(0.05,0.05,0.05,1);
gl_FragColor=(gl_FragColor*contrast)-vec4(fract(exp(mod(col.x,p.y))),fract(exp(mod(col.y,p.x))),0.125,1.0)*contrast*0.75;

}
