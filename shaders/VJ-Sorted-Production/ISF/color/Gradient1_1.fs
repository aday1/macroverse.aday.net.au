/*{
    "DESCRIPTION": "Gradient1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "color"
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
        "color"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float upY = 1.0 - mouse.y;
float up = 1.0 / (upY*resolution.y);
float down = 1.0 / (mouse.y*resolution.y);
 
void main(void){
    float r = gl_FragCoord.y > mouse.y * resolution.y ? (resolution.y - gl_FragCoord.y)*up: gl_FragCoord.y*down;
    gl_FragColor = vec4(r, 0.0, 1.0 - r, 1.0);
}

