/*{
    "DESCRIPTION": "HorizonLight-Emerald-DotMatrix-2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "3d"
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
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

vec2 rotate(vec2 k, float t) {
  return vec2(cos(t)*k.x - sin(t)*k.y, sin(t)*k.x + cos(t)*k.y);
}

float cast_sky(vec3 p) {
  vec3   d = abs( mod((p + vec3(0, -time * 0.5, 0.0)),2.0)) - 1.0;
  float t1 =  length( abs(mod(vec3(p.x, p.y - time, p.z - time), 2.0) - 1.0)) - 0.2;
  float t2 =  dot(p, normalize(vec3(0.0, 1.0, 0.0)));
  float t3 =  length(max(abs(d).xz - 0.1, 0.0)) - 0.05;
  float t4 =  length(abs(mod(p.xz,2.0)) - 1.0) - 0.9;
  float t5 =  length(abs(mod(p.zy,0.2)) - 0.1) - 0.05;
  return min(min(max(-t5, max(-t4, t2)), t1), t3);
}

void main( void ) {
  float pp = 0.0;//pow(1.0 - fract(time), 2.0);
  vec2 position = ( gl_FragCoord.xy / resolution.xy );
  vec2 uv = -1.0 + 2.0 * position;
  vec3 dir = normalize(vec3(uv * vec2(1.25, 1.0), 1.0));
  
  dir.xy = rotate(dir.xy, time * 0.2);
  dir.zx = rotate(dir.zx, time * 0.01);
  vec3 pos = vec3(0.0, 2.0, time * 3.0 + pp);
  vec3 ray = pos;
  float  t = 0.0;
  for(int i = 0 ; i < 70; i++) {
    float k = cast_sky(ray + dir * t);
    t += k * 0.75;
  }
  vec3 hit = ray + dir * t;
  vec2 h   = vec2(0.01, 0.00);

  vec3 N   = normalize(vec3(
    cast_sky(hit + h.xyy),
    cast_sky(hit + h.yxy),
    cast_sky(hit + h.yyx)) - cast_sky(hit));
    gl_FragColor = vec4(N + t * 0.07 + pp, 1);
}
