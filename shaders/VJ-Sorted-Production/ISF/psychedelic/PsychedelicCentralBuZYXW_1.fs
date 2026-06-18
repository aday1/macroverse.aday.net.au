/*{
    "DESCRIPTION": "PsychedelicCentralBuZYXW",
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
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "psychedelic"
    ]
}*/
#define E 2.71828182846




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
// Fractals: MRS
// by Nikos Papadopoulos, 4rknova / 2015
// Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
//
// Adapted from https://www.shadertoy.com/view/4lSSRy by J.

// best on 1 resolution

//the universe is a dance of relationships
//you are a dance the universe is having to experience itself
//it is but one mind experiencing itself in infinite ways, one infinite fractal holographic consciousness
//mmmm yeaaa baby get with it ur infinite

#ifdef GL_ES
precision highp float;
#endif

uniform vec4 inputColour;

// mouse.x
// mouse.y
// inputColour.x
// inputColour.y
// inputColour.z
// inputColour.w

void main( void ) {

    vec2 uv = (gl_FragCoord.xy / resolution.xy)-0.5;
    uv.x *= resolution.x / resolution.y;
    float t = time*inputColour.y;
    float k = cos(t);
    float l = sin(t);        

    float s = .99;
	
    s = fract(inputColour.w+length(uv));
    //s = cos(cos(fract(time*length(uv))));
	
    for(int i= int(inputColour.x); i<96; ++i) {
        uv  = abs(uv) - s;    // Mirror
        uv *= mat2(k,-l,l,k); // Rotate
        s  *= (1.-inputColour.z*(mouse.x));         // Scale
    }
    
    float x = cos(6.28318*(1200.*length(uv)));
    float j = sin(5000.*length(uv));
    gl_FragColor = vec4(vec3(x,j/1.5,mouse.y),inputColour.w);
}
