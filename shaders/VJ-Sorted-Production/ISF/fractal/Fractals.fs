/*{
    "DESCRIPTION": "Fractals",
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

void main( void ) {

    vec2 uv = (gl_FragCoord.xy / resolution.xy)-0.5;
    uv.x *= resolution.x / resolution.y;
    float t = time*.02;
    float k = cos(t);
    float l = sin(t);        

    float s = .99;
	
    s = fract(0.8+length(uv));
    //s = cos(cos(fract(time*length(uv))));
	
    for(int i=0; i<96; ++i) {
        uv  = abs(uv) - s;    // Mirror
        uv *= mat2(k,-l,l,k); // Rotate
        s  *= (1.-0.15*(0.4));         // Scale
    }
    
    float x = cos(6.28318*(1200.*length(uv)));
    float j = sin(5000.*length(uv));
    gl_FragColor = vec4(vec3(x,j/1.5,0),1);
}
