precision mediump float;

uniform vec2 resolution;
uniform float time;
uniform float bass; // @expose 0 1
uniform float mid; // @expose 0 1
uniform float treb; // @expose 0 1
uniform float amp; // @expose 0 1
uniform float dens; // @expose 1 3
uniform float speed; // @expose 0.1 4

/* AdLibitum RGB glitch — map FFT bands to bass/mid/treb in Macroverse Params */
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
  float tear = step(0.9 - Amp*0.15, noise(vec2(uv.y*40.0, t*(3.0+B*4.0))));
  float shift = tear * (0.02 + Tr*0.04);
  float n = fbm(uv*8.0 + t*0.5);
  vec3 col = sheet(n*0.5 + kick*0.2);
  col.r = sheet(fbm((uv+vec2(shift,0.))*8.0 + t)*0.5 + B*0.3).r;
  col.b = sheet(fbm((uv-vec2(shift,0.))*8.0 + t)*0.5 + Tr*0.3).b;
  col += vec3(1.,0.2,0.2)*tear*0.35;
  gl_FragColor = vec4(col, 1.0);
}
