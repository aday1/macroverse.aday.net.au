/*{
    "DESCRIPTION": "Deep AA packet starfield",
    "CREDIT": "AdLibitum / Clan Analogue promo VJ — aday",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "Generator",
        "adlibitum",
        "vj",
        "opl",
        "chiptune"
    ],
    "INPUTS": [
        {
            "NAME": "bass",
            "TYPE": "float",
            "DEFAULT": 0.3,
            "MIN": 0,
            "MAX": 1,
            "LABEL": "Bass / Sub (map FFT)"
        },
        {
            "NAME": "mid",
            "TYPE": "float",
            "DEFAULT": 0.3,
            "MIN": 0,
            "MAX": 1,
            "LABEL": "Mid (map FFT)"
        },
        {
            "NAME": "treb",
            "TYPE": "float",
            "DEFAULT": 0.3,
            "MIN": 0,
            "MAX": 1,
            "LABEL": "Treb / High (map FFT)"
        },
        {
            "NAME": "amp",
            "TYPE": "float",
            "DEFAULT": 0.4,
            "MIN": 0,
            "MAX": 1,
            "LABEL": "Amp / Energy"
        },
        {
            "NAME": "dens",
            "TYPE": "float",
            "DEFAULT": 2,
            "MIN": 1,
            "MAX": 3,
            "LABEL": "Density"
        },
        {
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 1,
            "MIN": 0.1,
            "MAX": 4,
            "LABEL": "Time speed"
        }
    ],
    "TAGS": [
        "adlibitum",
        "vj",
        "audio",
        "opl3",
        "fm",
        "adlib-starfield-aa"
    ]
}*/


#ifdef GL_ES
precision mediump float;
#endif

#define t (TIME * speed)
#define B clamp(bass, 0.0, 1.0)
#define Md clamp(mid, 0.0, 1.0)
#define Tr clamp(treb, 0.0, 1.0)
#define Amp clamp(amp, 0.0, 1.0)
#define kick max(B, Amp * 0.7)

float hash(vec2 p){ return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453); }
float noise(vec2 p){
  vec2 i=floor(p), f=fract(p);
  float a=hash(i), b=hash(i+vec2(1,0)), c=hash(i+vec2(0,1)), d=hash(i+vec2(1,1));
  vec2 u=f*f*(3.-2.*f);
  return mix(a,b,u.x)+(c-a)*u.y*(1.-u.x)+(d-b)*u.x*u.y;
}
float fbm(vec2 p){
  float v=0., a=0.5;
  for(int i=0;i<4;i++){ v+=a*noise(p); p*=2.03; a*=0.5; }
  return v;
}
vec3 sheet(float v){
  v = clamp(v,0.,1.);
  vec3 paper = vec3(0.07,0.09,0.06);
  vec3 ink = vec3(0.78,0.90,0.55);
  vec3 rule = vec3(0.45,0.55,0.35);
  return mix(paper, mix(rule, ink, v), v);
}

void main(){
  vec2 p = (gl_FragCoord.xy - 0.5*RENDERSIZE.xy) / min(RENDERSIZE.x, RENDERSIZE.y);
  float v = 0.;
  for(int i=0;i<40;i++){
    float fi=float(i);
    vec2 sp = vec2(hash(vec2(fi,1.)), hash(vec2(fi,2.)))*2.0-1.0;
    float z = fract(hash(vec2(fi,3.))+t*(0.1+B*0.3));
    vec2 q = sp/(z+0.05);
    v += exp(-length(p*1.2 - q)* (40.0 + Amp*30.0)) * (1.0-z);
  }
  float neb = fbm(p*2.0 + t*0.05)*0.2;
  vec3 col = vec3(0.01,0.02,0.05) + vec3(0.7,0.9,1.0)*v + vec3(0.3,0.5,0.8)*neb + vec3(1.,0.7,0.2)*kick*0.25;
  gl_FragColor = vec4(col, 1.0);
}
