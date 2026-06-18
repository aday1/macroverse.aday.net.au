/*{
    "DESCRIPTION": "GridLiners1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

vec3 loadLight(float s, float g, vec2 q) {
  return vec3(smoothstep(s, s + .1, g) / length(q + mouse));
}

float cdist(vec2 v0, vec2 v1) {
  v0 = abs(v0 - v1);
  return max(v0.x,v0.y);
}

void main( void ) {
  vec2 p = (gl_FragCoord.xy * 2.0 - resolution) / min(resolution.x, resolution.y);
  vec4 c = vec4(0.);

  // load
  vec3 loadColor = vec3(.01, .015, .2);
  const float loadLightNum = 24.;

  vec2 q = vec2(p.x * (16.0 / p.y), (-mouse.x / p.y) - 3. + (time * .3)) * .23;

  float grid = 2. * cdist(vec2(.5), mod(vec2(q.x, q.y), 1.));

  for (float i = 0.; i < loadLightNum; i++) {
    float ls = abs(sin(time * .3 + i)) * .1 + .8;
    loadColor = loadColor + loadLight(ls, grid, q);
    q.y-= 9.;
  }
  c = c + vec4(loadColor, 1);
  
  gl_FragColor = c;
}
