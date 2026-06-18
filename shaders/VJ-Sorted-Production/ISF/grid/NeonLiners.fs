/*{
    "DESCRIPTION": "NeonLiners",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
//

#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

void main (){
    vec2 uv=(((gl_FragCoord.xy-(.5 * resolution)) / min (resolution.y,resolution.x)) * 4.0);
    float fline=0.0,fline2=0.0,y=0.0,t=0.0;
    for (float x=-4.69;x <=4.69;x+=0.95){
        float feigenb=uv.y * t * (1.0001 - t);
	      t=sqrt((feigenb * feigenb) + (x * x)) - 2.71828;
        fline2=fline2 + .0035 / length(abs(uv.x) + cos(t + time) - abs(uv.y));
        fline=fline + .002 / length(abs(uv.x) * -.6877663+sin((t + time)* .5) + .52) * -cos(4.799 +time);
    };
    float glitch=0.0035 / length(.6 + cos(t + time));
    gl_FragColor.xyz=vec3(fline2,-fline,fline)+glitch;
    gl_FragColor.w=1.0;
}
