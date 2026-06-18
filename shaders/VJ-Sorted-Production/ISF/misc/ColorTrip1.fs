/*{
    "DESCRIPTION": "ColorTrip1",
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

vec2 R = resolution;
vec2 Offset;
vec2 Scale=vec2(0.01,0.01);
float Saturation = 0.3; // 0 - 1;

vec3 lungth(vec2 x,vec3 c){
       return vec3(length(x+c.r),length(x+c.g),length(x+c.b));
}

void main( void ) {
    Offset = vec2(1.);
    vec2 x = (gl_FragCoord.xy/resolution)*10.;
    vec4 c=vec4(0,0,0,0);
    x=x*Scale*R/R.x+Offset;
    x+=sin(x.yx*sqrt(vec2(13,9)))/5.;
    c.rgb=lungth(sin(x*sqrt(vec2(33,43))),vec3(5,6,7)*Saturation);
    x+=sin(x.yx*sqrt(vec2(73,53)))/3.;
    c.rgb=2.*lungth(sin(time+x*sqrt(vec2(33.,23.))),c.rgb/9.);
    x+=sin(x.yx*sqrt(vec2(93,73)))/2.;
    c.rgb=lungth(sin(x*sqrt(vec2(13.,1.))),c.rgb/2.0);
    c=.5+.5*sin(c*8.);
    c.rgb=lungth(sin(x*sqrt(vec2(11.,4.))),c.rgb/2.0);
    c=.5+.5*sin(c*8.);
    c.a=1.;
    gl_FragColor = vec4(mix(vec3((c.r+c.g+c.b)/3.),c.rgb,vec3(1000.)),1.);
}
