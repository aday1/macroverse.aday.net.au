/*{
    "DESCRIPTION": "BlueWavepaper",
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
        "abstract"
    ]
}*/

#define time TIME




#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

#define PI 3.14159265359
#define TWO_PI 6.28318530718

float clampa(float x, float minVal, float maxVal)
{
return min(max(x, minVal), maxVal);
}

float smoothstepa(float edge0,float edge1, float x)
{
	
  float t;  /* Or genDType t; */
    t = clampa((x - edge0) / (edge1 - edge0), 0.0, 1.0);
    return t * t * (3.0 - 2.0 * t);
}

void main( void ) 
{
  vec2 p = 32. * ((gl_FragCoord.xy / resolution.y) * mouse.y - 1.0);
	p.x += time*0.2*cos(time*mouse.x+p.y-mod(p.y, -1.6));
  p = 0.8 + mod(p,-1.6);                   // mirrors
	float time = time + length(p)*1e1;
  float d = length(p);                     // distance
  float a = time + 3.0 * atan(p.x, p.y)+100./(2.+cos(time*.1));   // angle
  float r = 0.2 * pow(.5+.5*cos(a), -2.); // radius
  float f = smoothstepa(d, d+0.012, r);     // aa
  vec3 col = mix(vec3(1.0), vec3(0.0, 0.0, 0.9), f);
  gl_FragColor = vec4 ( col, 1.0);
}
