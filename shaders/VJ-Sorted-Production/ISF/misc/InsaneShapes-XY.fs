/*{
    "DESCRIPTION": "InsaneShapes-XY",
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
        "misc"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

// ORIGINAL by - 2016 David A Roberts <https://davidar.io>
//   https://www.shadertoy.com/view/Xs3GWj
// BPT.2016 - pulled out the threesome to tinker/explore it more

#extension GL_OES_standard_derivatives : enable

// ------------------------------------------------------------------------------

float atanp(in vec2 p) { return atan(p.y,p.x); }

vec3 threesome(in vec2 p) {
    p /= 3.;
    float z = 1.;
    z *= sin(length(p + vec2(5,0))) * cos(8.*atanp(p + vec2(5,0)));
    z *= sin(length(p - vec2(5,5))) * cos(8.*atanp(p - vec2(5,5)));
    z *= sin(length(p + vec2(0,5))) * cos(8.*atanp(p + vec2(0,5)));
    if(-0.1 < z && z < 0. || 0.2 < z) return vec3(0);
    return vec3(1);
}

// ------------------------------------------------------------------------------

void main( void ) {

	vec2 position = ((gl_FragCoord.xy / resolution.xy ) - (mouse)) * 50.;
	gl_FragColor = vec4( threesome( position ), 1.0 );

}

