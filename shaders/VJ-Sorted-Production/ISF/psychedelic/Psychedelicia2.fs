/*{
    "DESCRIPTION": "Psychedelicia2",
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
/** PSYCHEDELIA **/
// by byteManiak
// contact info: bytemaniak 98 at gmail dot com

#ifdef GL_ES
precision highp float;
#endif

#extension GL_OES_standard_derivatives : enable

float f(float x)
{ return exp(mod(cos(time/500.), pow(x, mix(2., 3., sin(time))))); }

bool cmp(float a, float b, float epsilon)
{ return (abs(a-b))<epsilon; }

void main() {
	vec2 p = gl_FragCoord.xy / resolution.xy - .5;
	
	if(cmp(1., f(p.x*p.y), .00003))
		gl_FragColor += vec4(1.);
	
	gl_FragColor *= vec4(cos(time), sin(time), 1.-sin(time), 1.0) * tan(resolution.x/p.x / resolution.y*p.y);
}
