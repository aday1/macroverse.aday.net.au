/*{
    "DESCRIPTION": "PsychedelicaWall2",
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
// Fractals: MRS
// by Nikos Papadopoulos, 4rknova / 2015
// Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
//
// Adapted from https://www.shadertoy.com/view/4lSSRy by J.

#ifdef GL_ES
precision mediump float;
#endif

#define mod(x, k) (1. - abs((x) - floor((x) / (k)) * (k) - 1.))

vec4 hsv2rgb(float h, float s, float v)
{
    h *= 6.;
    float c = v * s;
    float x = c * mod(h, 2.);
    float m = v - c;
    vec4  r = vec4(m, m, m, 0);
    if(h < 1.)  return r + vec4(c, x, 0, 1);
    if(h < 2.)  return r + vec4(x, c, 0, 1);
    if(h < 3.)  return r + vec4(0, c, x, 1);
    if(h < 4.)  return r + vec4(0, x, c, 1);
    if(h < 5.)  return r + vec4(x, 0, c, 1);
    return r + vec4(c, 0, x, 1);
}

void main( void ) {

    vec2 uv = .275 * gl_FragCoord.xy / resolution.y;
    float t = time*.01, k = cos(t), l = sin(t);        
    
    float s = .2;
    for(int i=0; i<128; ++i) {
        uv  = abs(uv) - s;    // Mirror
        uv *= mat2(k,-l,l,k); // Rotate
        s  *= .95156;         // Scale
    }
    
    float x = .5 + .5*cos(6.28318*(800.*length(uv)));
    gl_FragColor = hsv2rgb(x, 0.9, 1.);
}
