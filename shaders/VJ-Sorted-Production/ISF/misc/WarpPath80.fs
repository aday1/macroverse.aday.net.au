/*{
    "DESCRIPTION": "WarpPath80",
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
        }
    ],
    "TAGS": [
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define r resolution
#define c gl_FragCoord
varying vec2 surfacePosition;
#define xy surfacePosition
 
const float hpi = 3.14159265358979*0.5;
 
void main( void ) {
	gl_FragColor = vec4(0);
	
	//vec2 xy = ( c.xy / r.xy ) - 0.5;xy.y *= r.y/r.x;
	
	float s = length(xy)*pow(5., mouse.x+1.);
	float fs = fract(s);
	float t = atan(xy.x, xy.y);
	
	t += (s-fs)*time*mouse.y*2.;
	t = mod(t, hpi*4.)-hpi*2.;
	
	if(t > hpi) return;
	
	if(t < 0. && t > -hpi) return;
	
	if(fs < 0.9) gl_FragColor = vec4(1);
	
	if(t < 0.) gl_FragColor.xyz /= 2.;
	
}
//+pk

