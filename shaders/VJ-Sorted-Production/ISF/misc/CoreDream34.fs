/*{
    "DESCRIPTION": "CoreDream34",
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
//#ifdef GL_ES
//precision mediump float;
//#endif

#ifdef GL_ES
precision mediump float;
#endif
#define PI 3.1415926535897932384626433832795

void main() {
    float v = 0.0;
    vec2 c = (gl_FragCoord.xy / resolution.xy * 20.0);
    v += sin(c.x+time);
    v += sin(c.y+time);
    v += sin(c.x+time);
    v += sin(c.y+time);
    v  += sin(c.x+time*1.5 + c.y+time*2.0);
    v += sin(c.x+time + c.y+time);
    c += vec2(sin(time/3.0), cos(time/2.0));
    v += sin(sqrt(c.x*c.x+c.y*c.y+1.0)+time);
    vec3 col = vec3(tan(PI*v), sin(PI*v), cos(PI*v));
    gl_FragColor = vec4(col, 1.0);
}
