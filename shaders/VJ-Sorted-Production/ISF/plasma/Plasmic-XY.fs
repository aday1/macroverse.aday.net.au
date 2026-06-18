/*{
    "DESCRIPTION": "Plasmic-XY",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "plasma"
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
        "plasma"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

vec2 R = resolution;
vec2 Offset;
vec2 Scale=vec2(0.005,0.005);
float Saturation = 0.4; // 0 - 1;

vec3 lungth(vec2 x,vec3 c){
       return vec3(length(x+c.r),length(x+c.g),length(x+c.b));
}

void main( void ) {
    Offset = mouse.xy;
    vec2 x = gl_FragCoord.xy;
    vec4 c=vec4(0,0,0,0);
    x=x*Scale*R/R.x+Offset;
    x+=sin(x.yx*sqrt(vec2(13,9)))/5.4;
    c.rgb=lungth(sin(x*sqrt(vec2(33,43))),vec3(3,1,9)*Saturation);
    x+=sin(x.yx*sqrt(vec2(17,19)))/5.1;
    c.rgb=1.5*lungth(sin(time+x*sqrt(vec2(13.7,47.7))),c.rgb/9.2);
    x+=sin(x.yx*sqrt(vec2(89,51)))/2.2;
    c.rgb=lungth(sin(x*sqrt(vec2(11.1,1.1))),c.rgb/3.1);
    c=.4+.4*sin(c*8.);
    c.a=1.;
    gl_FragColor = c;
}
