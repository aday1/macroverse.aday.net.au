precision mediump float;

uniform vec2 resolution;
uniform float time;
uniform float bass; // @expose 0 1
uniform float mid; // @expose 0 1
uniform float treb; // @expose 0 1
uniform float amp; // @expose 0 1
uniform float dens; // @expose 1 3
uniform float speed; // @expose 0.1 4

/* AdLibitum Vector star — map FFT bands to bass/mid/treb in Macroverse Params */
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
  vec2 p = (gl_FragCoord.xy - 0.5*resolution.xy) / min(resolution.x, resolution.y);
  float v = 0.;
  for(int i=0;i<32;i++){
    float fi=float(i)/32.0;
    float lx = sin(t*(1.5+B*2.0) + fi*6.2831853*(2.0+Md))* (0.25+Amp*0.2);
    float ly = sin(t*(2.2+Tr) + fi*6.2831853*(3.0+B))* (0.22+Md*0.15);
    v += exp(-length(p-vec2(lx,ly))*(18.0+dens*5.0));
  }
  vec3 col = vec3(0.01,0.03,0.05) + vec3(0.4,0.95,1.0)*v*0.55 + vec3(1.,0.8,0.3)*kick*0.3;
  gl_FragColor = vec4(col, 1.0);
}
