/*{
    "DESCRIPTION": "GridPattern",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        "abstract"
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

//
// atan test pattern
//
// Tests behavior of atan(y,x) for a set of values
// 
// Correct results should show 7x7 grid of boxes of these colors:
//   Right: red
//   Top: yellow
//   Left: cyan
//   Bottom: purple
// Boxes along diagonals should be: 
//   UR: orange
//   UL: green
//   BL: blue
//   BR: magenta
// Center box corresponds to atan(0.,0.)==NaN, so its color is undefined

// emulate OpenGL 4.5's mix(,,bool)
float mix(float x, float y, bool a) {
  return a ? y : x;
}

// proposed solution from 
// http://stackoverflow.com/questions/26070410/robust-atany-x-on-glsl-for-converting-xy-coordinate-to-angle
// swaps params when |x| <= |y|
float atan2(in float y, in float x) {
    bool s = (abs(x) > abs(y));
    return mix(3.14159265358979/2.0 - atan(x,y), atan(y,x), s);
}

// convert angle to hue; returns RGB
// colors corresponding to (angle mod TWOPI):
// 0=red, PI/2=yellow-green, PI=cyan, -PI/2=purple
vec3 angle_to_hue(float angle) {
  angle /= TWOPI;
  return clamp((abs(fract(angle+vec3(3.0, 2.0, 1.0)/3.0)*6.0-3.0)-1.0), 0.0, 1.0);
}

#define BIG 1e4
#define SMALL 1e-4

// convert value between 0 and 1 to one of a set of discrete values
float quantify(float v) {
  v *= 7.;
  if(v<1.) return -BIG;
  if(v<2.) return -1.;
  if(v<3.) return -SMALL;
  if(v<4.) return  0.;
  if(v<5.) return  SMALL;
  if(v<6.) return  1.;
           return  BIG;
}

void main( void ) {
  vec2 z = (gl_FragCoord.xy/resolution);
  float x = quantify(z.x);
  float y = quantify(z.y);
  float a = atan2(y, x);
  gl_FragColor = vec4(angle_to_hue(a-TWOPI), 1.);
}

