/*{
    "DESCRIPTION": "PCB traces + vias — kick vias",
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
        "adlib-circuit-pulse"
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
  float grid = step(0.94, fract(p.x*8.0)) + step(0.94, fract(p.y*8.0));
  float trace = 0.;
  for(int i=0;i<5;i++){
    float fi=float(i);
    float yy = -0.4 + fi*0.2;
    float xx = fract(t*(0.1+B*0.2)+fi*0.2)*2.0-1.0;
    trace += exp(-abs(p.y-yy)*40.0)*smoothstep(0.5,0.,abs(p.x-xx));
    trace += exp(-length(p-vec2(xx,yy))*25.0)*kick;
  }
  vec3 col = vec3(0.05,0.07,0.04) + vec3(0.6,0.85,0.35)*grid*0.15 + vec3(0.4,0.95,0.8)*trace;
  gl_FragColor = vec4(col, 1.0);
}
