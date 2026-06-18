/*{
    "DESCRIPTION": "AetherPath66",
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
        "misc",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

uniform sampler2D bb;
 
#define pi 3.14
 
void main() 
{
      vec2 uv = gl_FragCoord.xy / resolution.xy;
      vec2  o = uv - .5;
      float p = length(o);
      float t = time * 99999.;
      float r = atan(o.x, o.y);
      
      float rot = fract(t-o.x)+fract(t+o.y);
      
      float s = fract(atan(o.x, o.y)*rot);

      gl_FragColor = vec4(1.) * s;
}//a nifty test of your drivers - sphinx

