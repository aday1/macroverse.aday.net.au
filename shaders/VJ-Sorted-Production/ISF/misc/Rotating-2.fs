/*{
    "DESCRIPTION": "Rotating-2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "misc"
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
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
// Carpet_ by shezard 2015-10-01
// https://www.shadertoy.com/view/XtjXWW
 
#ifdef GL_ES 
precision mediump float;
#endif

mat2 rotate(in float theta) 
{
  return mat2(cos(theta), -sin(theta), sin(theta), cos(theta));
}
 
void main()
{
  vec2 uv = gl_FragCoord.xy / resolution.xy;
    
  vec2 p = 2. * uv - 1.;
    
  p.x *= resolution.x / resolution.y;
    
  p *= 2. + cos(time*.5 + length(p)) * 8.;
    
  p *= rotate(time * .5);
    
  float f = (length(p * p.x * p.y));
    
  f *= cos(p.x * 1.4);
  f *= cos(p.y * 1.4);
    
  vec3 c = vec3(1.)*f*f*.5;
  vec3 c2 = vec3(1.) - c;
    
  gl_FragColor = vec4(c2,1.0);
}
 

