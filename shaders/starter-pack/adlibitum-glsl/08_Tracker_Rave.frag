precision mediump float;

uniform vec2 resolution;
uniform float time;
uniform float bass; // @expose 0 1
uniform float mid; // @expose 0 1
uniform float treb; // @expose 0 1
uniform float amp; // @expose 0 1
uniform float dens; // @expose 1 3
uniform float speed; // @expose 0.1 4

/* AdLibitum Tracker rave — map FFT bands to bass/mid/treb in Macroverse Params */
#define t (time * speed)
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
  vec2 uv = gl_FragCoord.xy / resolution.xy;
  float row = floor(uv.y*(20.0+dens*6.0));
  float coln = floor(uv.x*(10.0+dens*4.0));
  float cell = step(0.45 - B*0.15, hash(vec2(coln, row+floor(t*(3.0+Md*8.0)))));
  float cur = exp(-abs(uv.y - fract(t*(0.2+B)))*30.0)*0.6;
  float vu = 0.;
  for(int i=0;i<8;i++){
    float fi=float(i);
    float hgt = 0.1 + (fi<3.?B:fi<6.?Md:Tr)*(0.4+hash(vec2(fi,floor(t*4.)))*0.3);
    vu += step(abs(uv.x-(0.06+fi*0.12)),0.025)*step(uv.y,hgt);
  }
  vec3 col = vec3(0.02,0.02,0.16) + vec3(0.95,0.9,0.2)*cell*0.35 + vec3(1.,1.,0.)*cur + vec3(0.2,0.9,0.3)*vu;
  gl_FragColor = vec4(col, 1.0);
}
