/*{
    "DESCRIPTION": "PsychedelicDMGScreen",
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
// Author @patriciogv - 2015
// Title: Matrix

// funky grid? -- novalis

#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

float random(in float x){ return fract(sin(x)*43758.5453); }
float random(in vec2 st){ return fract(sin(dot(st.xy ,vec2(12.9898,78.233))) * 43758.5453); }
mat2 rot(float p) { return mat2(cos(p),sin(p),-sin(p),cos(p)); }

float randomChar(vec2 outer,vec2 inner){
    float grid = 5.;
    vec2 ipos = floor(inner*grid);
    vec2 fpos = fract(inner*grid);
    return step(.5,random(outer*64.+ipos)) * step(0.01,fpos.x) * step(0.01,fpos.y);
}

void main(){
    vec2 st = gl_FragCoord.st/resolution.xy;
    st.y *= resolution.y/resolution.x;
    st += vec2(3e-4*time);
    st *= rot(0.4*time);
    st *= 1.+.4*sin(1.3*time);
    st *= 12.;

    vec2 ipos = floor(st);
    vec2 fpos = fract(st)+vec2(floor(time*5.),0.);

    ipos += vec2(0.,floor(time*6.*random(ipos.x+1.)));

    float pct = 1.0;
    pct *= randomChar(ipos,fpos);

    gl_FragColor = vec4(cos(ipos.x+pct+time),sin(ipos.y+pct+time),0.5, 1.);
}
