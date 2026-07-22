precision mediump float;

uniform vec2 resolution;
uniform float time;
uniform float bass; // @expose 0 1
uniform float mid; // @expose 0 1
uniform float treb; // @expose 0 1
uniform float amp; // @expose 0 1
uniform float dens; // @expose 1 3
uniform float speed; // @expose 0.1 4

/* AdLibitum Hex storm — map FFT bands to bass/mid/treb in Macroverse Params */
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
  float cell = 14.0 - dens*2.0;
  vec2 g = floor(gl_FragCoord.xy / cell);
  float n = hash(g + floor(t*(2.0+B*6.0)));
  float on = step(0.55 - Amp*0.2, n);
  float glow = exp(-length(fract(gl_FragCoord.xy/cell)-0.5)*5.0)*on;
  float flash = kick * step(0.8, hash(g+floor(t*10.0)));
  vec3 col = vec3(0.04,0.06,0.03) + vec3(0.75,0.95,0.45)*(on*0.35+glow) + vec3(1.,0.6,0.2)*flash;
  gl_FragColor = vec4(col, 1.0);
}
