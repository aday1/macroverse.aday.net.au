/*{
    "DESCRIPTION": "Spinner",
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
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif
 
#extension GL_OES_standard_derivatives : enable

// ventilator_malfunction a bit zoom
 
const float scale = 6.0;
 
void main( void ) 
{
  vec2 p = gl_FragCoord.xy / resolution.y * scale - 2.0;
  float t = 3.*abs(sin(time)*3.)*3.14;	
  p = fract(0.7*p)-0.5;                    // mirrors
  float d = 2.*length(p);                  // distance
  float a = t + 3.0 * atan(p.x, p.y);      // angle
  float r = 0.5 + 0.2 * pow(cos(a), 0.08)*min(t/6.28,2.5); // radius
  float f = smoothstep(d, d+0.012, r);     // aa
  vec3 c = vec3(.9+0.5*sin(time), 0.7-0.2*cos(time), 0.0);
  gl_FragColor = vec4 (mix(vec3(0.0), c, f), 1.0);
}
 

