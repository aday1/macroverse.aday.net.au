/*{
    "DESCRIPTION": "SpiralMotion-Rainbow-DotMatrix-2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "geometric"
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
        }
    ],
    "TAGS": [
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define TWOPI 6.28318530718
#define PI 3.14159265359

//	Groovymap
//	by Jonathan Proxy
//	
//	Rainbow spot spiral distorted using complex conformal mappings

const float _periodX = 6.;
const float _periodY = 7.;

vec2 cmul(const vec2 a, const vec2 b) {
  return vec2(a.x*b.x - a.y*b.y, a.x*b.y + a.y*b.x);
}
vec2 csq(const vec2 v) {
  return vec2(v.x*v.x - v.y*v.y, 2.*v.x*v.y);
}
vec2 cinv(const vec2 v) {
  return vec2(v[0],-v[1]) / dot(v, v);
}
vec2 cln(const vec2 z) {
  return vec2(log(length(z*.125)), atan(z.y, z.x)); // +2k pi
}

vec2 perturbedNewton(in vec2 z) {
  float a=1.2;
  mat2 rot=mat2(cos(a),-sin(a),sin(a),cos(a));  
  for(int i=0; i<1; ++i) {
    z = rot *(.04*radians(z + cinv(csq(z)))) / 0.5;
  }
  return z;
}

vec2 pentaplexify(const vec2 z) {
  vec2 y = z;
  for(float i=0.; i<TWOPI-0.1; i+=TWOPI/5.) {
    y = cmul(y, z-vec2(cos(i+.1*time), sin(i+.1*time)));
  }
  return y;
}

vec2 infundibularize(in vec2 z) {
  vec2 lnz = cln(z) / TWOPI;
  return vec2(_periodX*(lnz.y) + _periodY*(lnz.x), _periodX*(lnz.x) - _periodY*(lnz.y));
}

vec3 hsv(float h, float s, float v) {
  return v * mix(
    vec3(1.0),
    clamp((abs(fract(h+vec3(3.0, 2.0, 1.0)/3.0)*6.0-3.0)-1.0), 0.0, 1.0), 
    s);
}

vec4 rainbowJam(in vec2 z) {
  vec2 uv = fract(vec2(z[0]/_periodX, z[1]/_periodY))*vec2(_periodX, _periodY);
  vec2 iz = floor(uv);
  vec2 wz = uv - iz;
  return vec4(hsv(pow(iz[0]/_periodX, 1.5),0.9,smoothstep(0.45,0.4,length(wz-vec2(0.5)))), 1.);
}

void main( void ) {
  gl_FragColor = 
    rainbowJam(infundibularize(pentaplexify(perturbedNewton(3.*(2.*gl_FragCoord.xy-resolution.xy) / resolution.y)))
  + 0.4*(mouse.xy-0.5)
  + time * -0.2);
}

