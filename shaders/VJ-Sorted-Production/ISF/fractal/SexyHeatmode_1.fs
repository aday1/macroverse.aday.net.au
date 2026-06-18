/*{
    "DESCRIPTION": "SexyHeatmode",
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
#ifdef GL_ES
precision mediump float;
#endif

// "Fractal Orgy" by Pablo Román Andrioli (Kali)

//#define VOYEUR_MODE 

float orgy(vec2 p) {
  float pl=0., expsmo=1.;
  float t=sin(sin(time)*888.0)*2.0;
  float a=-.35+t*.02;
  p*=mat2(cos(a),sin(a),-sin(a),cos(a));
  p=p*.07+vec2(.728,-.565)+t*.017+vec2(0.,t*.014);
  for (int i=0; i<13; i++) {
    p.x=abs(p.x);
    p=p*2.+vec2(-2.,.85)-t*.04;
    p/=min(dot(p,p),1.06);  
    float l=length(p*p);
    expsmo+=exp(-1.2/abs(l-pl));
    pl=l;
  }
  return expsmo;
}

void main( void )
{
  vec2 uv = gl_FragCoord.xy/resolution.xy-.5;
    uv.x*=resolution.x/resolution.y;
  vec2 p=uv; p.x*=1.2;
  float o=clamp(orgy(p)*.07,.20,1.); o=pow(o,1.8);
  vec3 col=sin(vec3(o*o*.82,o*.5,o*.9)*time);
  float hole=length(uv+vec2(.1,0.05))-.25;
  #ifdef VOYEUR_MODE 
    col*=pow(abs(1.-max(0.,hole)),80.);
  #endif
  gl_FragColor = vec4(col*2.0, 1.0 );
}

