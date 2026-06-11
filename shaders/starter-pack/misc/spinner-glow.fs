/*{
    "DESCRIPTION": "spinner glow",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": ["Misc"],
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
    ]
}*/
uniform float val_n0_5; // @expose -0.5 1.5
uniform float val_n0_7; // @expose -0.30000000000000004 1.7
uniform float val_n3_14; // @expose 0 4.71
uniform float val_n3_1; // @expose 0 4.5
uniform float val_n3; // @expose 0 4.5
uniform float val_n2; // @expose 0 3

#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
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
  vec2 p = gl_FragCoord.xy / resolution.y * scale - val_n2;
  float t = val_n3 * abs(sin(time) * val_n3_1) * val_n3_14;
  p = fract(val_n0_7*p)-val_n0_5;                    // mirrors
  float d = 2.*length(p);                  // distance
  float a = t + 3.0 * atan(p.x, p.y);      // angle
  float r = 0.5 + 0.2 * pow(cos(a), 0.08)*min(t/6.28,2.5); // radius
  float f = smoothstep(d, d+0.012, r);     // aa
  vec3 c = vec3(.9+0.5*sin(time), 0.7-0.2*cos(time), 0.0);
  gl_FragColor = vec4 (mix(vec3(0.0), c, f), 1.0);
}
 

