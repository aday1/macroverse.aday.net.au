/*{
    "DESCRIPTION": "Duke / DOS war CRT — red yellow blast",
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
        "adlib-blood-pulse"
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
  vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
  vec2 p = (gl_FragCoord.xy - 0.5*RENDERSIZE.xy) / min(RENDERSIZE.x, RENDERSIZE.y);
  float blast = exp(-length(p)* (3.0 - B*2.0))* (0.4+kick);
  float scan = step(0.97, fract(uv.y*60.0 - t*(1.0+Tr*2.0)));
  float imf = exp(-abs(uv.y-0.5)*20.0)*step(0.4, fract(uv.x*30.0+t*4.0+Md));
  vec3 col = vec3(0.1,0.,0.) + vec3(1.,0.15,0.05)*blast + vec3(1.,0.85,0.1)*imf*0.5 + vec3(1.,0.3,0.)*scan*0.3;
  gl_FragColor = vec4(col, 1.0);
}
