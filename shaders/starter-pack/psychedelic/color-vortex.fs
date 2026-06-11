/*{
    "DESCRIPTION": "color vortex",
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
//--- capillary
// by Catzpaw 2017
#ifdef GL_ES
precision mediump float;
#endif

// edited by EVERYTHINGING

//#extension GL_OES_standard_derivatives : enable

#define ITER 60
#define EPS 0.1
#define NEAR 1.0
#define FAR 80.

vec3 hueToRGB(float hue) {
    return clamp(abs(mod(hue * 6.0 + vec3(0.0, 4.0, 2.0), 6.0) - 3.0) - 1.0, 0.0, 1.0);
}

float map(vec3 p) {
  return dot(cos(p.xyz), sin(p.yzx)) + cos(time) + sin(time);
}

float trace(vec3 ro, vec3 rd) {
  float t = NEAR, d;
  for (int i = 0; i < ITER; i++) {
    d = map(ro + rd * t);
    if (abs(d) < EPS || t > FAR) break;
    t += (10.*mouse.x);
  }
  return min(t, FAR);
}

void main(void) {
  vec2 uv = gl_FragCoord.xy / resolution.xy*2.-1.;
  float t = trace(vec3(0, 1, time), vec3(uv, 0.5));
	
  vec3 col = vec3(0.);	
  col = vec3(t/FAR); //black and white
  col = hueToRGB((t/FAR)+(t*mouse.y)+dot(uv, uv)+time); //rainbow

  gl_FragColor = vec4(1.-col, 1.);
}
