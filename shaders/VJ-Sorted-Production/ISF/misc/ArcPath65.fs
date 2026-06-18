/*{
    "DESCRIPTION": "ArcPath65",
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

//Original code from http://pixelshaders.com/editor/
//Ported to GLSL Sandbox on 3/23/15

const float pi = 3.1415926;

float garb(vec2 v, float m) {
  return (mod(v.x*m + (1.-v.y)*m, (1.-v.x*m)+v.y*m));
}

void main() {
  vec2 pos = ( gl_FragCoord.xy / resolution.xy );
  float a = garb(vec2(pos.y, cos(tan(pos.y+pos.x+1000001.))), cos(time)+1.5);
  float c = garb(vec2(pos.y, cos(tan(pos.x+1000001.+time*.5))), cos(a+time)+1.5);
  gl_FragColor = vec4(.5*a+.15+.5*c,
                      .0*a+.2+.9*c,
                      .01*a+.3+.6*c,
                      1.);
}
