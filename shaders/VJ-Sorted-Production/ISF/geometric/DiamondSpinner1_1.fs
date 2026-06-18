/*{
    "DESCRIPTION": "DiamondSpinner1",
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
        },
        {
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Speed"
        },
        {
            "NAME": "mouseX",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse X"
        },
        {
            "NAME": "mouseY",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse Y"
        },
        {
            "NAME": "zoom",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Zoom"
        },
        {
            "NAME": "colorR",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Red"
        },
        {
            "NAME": "colorG",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Green"
        },
        {
            "NAME": "colorB",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Blue"
        },
        {
            "NAME": "brightness",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Brightness"
        },
        {
            "NAME": "saturation",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Saturation"
        },
        {
            "NAME": "contrast",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Contrast"
        },
        {
            "NAME": "hueShift",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Hue Shift"
        },
        {
            "NAME": "invert",
            "TYPE": "bool",
            "DEFAULT": 0,
            "LABEL": "Invert Colors"
        }
    ],
    "TAGS": [
        "abstract",
        "geometric",
        "3d"
    ]
}*/#define time TIME




#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision highp float;
#endif

// ---------------------------------------------------------------------------------------------
// quick and dirty shadertoy -> glslsandbox bridge - .bpt.
// ---------------------------------------------------------------------------------------------

float iGlobalTime = time;
vec2 iResolution = resolution;
void mainImage( out vec4 fragColor, in vec2 fragCoord );
vec4 iMouse = vec4(mouse, 0.0, 1.0);
void _userMain( void ) { mainImage( gl_FragColor, gl_FragCoord.xy );}

// ---------------------------------------------------------------------------------------------
// from shadertoy - https://www.shadertoy.com/view/XsyGRW
// shadertoy user - https://www.shadertoy.com/user/hughsk
// ---------------------------------------------------------------------------------------------

#define TRACE_STEPS 1
//#define TRACE_RAY

// 0 = Distance Field Display
// 1 = Raymarched Edges
// 2 = Resulting Solid
// 3 = Distance Field Polarity
#define DISPLAY 0

// 0 = Sine Wave
// 1 = Circle
// 2 = Offset Circle
// 3 = Circle Join
// 4 = Smooth Circle Join
// 5 = Quadrant Circle (.bpt.)
// 6 = Striped Circle (.bpt.)
// 7 = Ying Yang WIP (.bpt.)
// 8 = Parametric Test (.bpt.)
// 9 = Supershape (borrowed from http://glslsandbox.com/e#2641.4)
#define SCENE 9

#if SCENE == 0
  #define SAMPLER(p) shape_sine(p)
#endif
#if SCENE == 1
  #define SAMPLER(p) shape_circle(p)
#endif
#if SCENE == 2
  #define SAMPLER(p) shape_circle(p + vec2(0.7, 0))
#endif
#if SCENE == 3
  #define SAMPLER(p) min(shape_circle(p - vec2(cos(iGlobalTime))), shape_circle(p + vec2(sin(iGlobalTime), 0)))
#endif
#if SCENE == 4
  #define SAMPLER(p) shape_circles_smin(p, iGlobalTime * 0.5)
#endif

// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

// Quadrant Circle (.bpt. - use for shape inversions)
#if SCENE == 5
  #define _BASE(p) ((p.x/p.y))
  #define _CLIP(p) (step(shape_circle(p),0.0))
  #define SAMPLER(p) sign( _BASE(p) * _CLIP(p) )
#endif

// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

//Striped Circle (.bpt. - use for shape inversions)
#if SCENE == 6
  #define _BASE(p) (sin((p.x-p.y)*3.141592*8.0)*shape_circle(p))
  #define _CLIP(p) (step(shape_circle(p),0.0))
  #define SAMPLER(p) sign( _BASE(p) * _CLIP(p) )
#endif

// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

// Ying Yang WIP (.bpt. - use for shape inversions)
#if SCENE == 7
  #define _BASE(p) (shape_static_sine(p)*shape_circle(p))
  #define _CLIP(p) (step(shape_circle(p),0.0))
  #define _DOTS(p) shape_circle( (mod(p*3.45+vec2(0.0,1.0),2.0) - 1.0 ) )
  #define SAMPLER(p) sign( _DOTS(p) * _BASE(p) * _CLIP(p) )
#endif

// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

// Parametric test (.bpt.)
#if SCENE == 8

vec2 test(float T, float R, float r, float d) {
    float A = (R+r);
    float B = (((R-r)/r)*T);
    return vec2(
       (A * cos(T) + d * cos( B )),
       (A * sin(T) - d * sin( B ))
    ) / (A+d);
}

#define SAMPLER(p) (length(p)-length(test( atan( p.y/p.x ), abs(10.0*sin(time)), abs(10.0*sin(time*0.3))*0.9, abs(10.0*sin(time*1.5))*0.5 )))

#endif

// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

// Supershape (borrowed from http://glslsandbox.com/e#2641.4)
#if SCENE == 9

// s is for scale, r is for rotation
float supershape(vec2 p, float m, float n1, float n2, float n3, float a, float b, float s, float r) {
    float ang = atan(p.y, p.x) + r;
    float l = length(p);
    ang += 5.0*sin(time*0.01+l*sin(l*4.0*sin(time*0.01))); // (.bpt.) silly term to spin and introduce some asymmetry
    float T = m * ang / 4.0;
    float v = pow(pow(abs(cos(T) / a), n2) + pow(abs(sin(T) / b), n3), -1.0 / n1);
    float A = v * s;
    float B = length(p);
    return tan(A/B)-(A/B);
}

float HMM(float t,float b,float m) {
    return m*(sin(t*0.1)+b);
}

#define SAMPLER_(p,d) supershape(p - vec2(0, 0), 10.0+18.0*floor(sin(time*1.)), HMM(time,0.5,30.0), HMM(time*0.9,0.5,30.0), HMM(time*0.2,0.5,30.0), 1.0, 1.0, 1.0, 3.141592 * 2.0 * (d*(time*1.2)))

#define SAMPLER(p) SAMPLER_(p,-1.0) * SAMPLER_(p*-1.5-0.5,1.0) * SAMPLER_(p*1.5-0.5,1.0)
#endif

// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

float smin(float a, float b, float k) {
  float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
  return mix(b, a, h) - k * h * (1.0 - h);
}

vec2 squareFrame(vec2 screenSize, vec2 coord) {
  vec2 position = 2.0 * (coord.xy / screenSize.xy) - 1.0;
  position.x *= screenSize.x / screenSize.y;
  return position;
}

const float PI = 3.14159265359;

//float time = iGlobalTime;

vec3 palette(float t, vec3 a, vec3 b, vec3 c, vec3 d) {
  return a + b * cos(6.28318 * (c * t + d));
}

// r^2 = x^2 + y^2
// r = sqrt(x^2 + y^2)
// r = length([x y])
// 0 = length([x y]) - r
float shape_circle(vec2 p) {
  return length(p) - 0.5;
}

// y = sin(5x + t) / 5
// 0 = sin(5x + t) / 5 - y
float shape_static_sine(vec2 p) {
  return p.y - sin(p.x * 5.0) * 0.2;
}
float shape_sine(vec2 p) {
  return p.y - sin(p.x * 5.0 + time) * 0.2;
}

float shape_box2d(vec2 p, vec2 b) {
  vec2 d = abs(p) - b;
  return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
}

float shape_line(vec2 p, vec2 a, vec2 b) {
  vec2 dir = b - a;
  return abs(dot(normalize(vec2(dir.y, -dir.x)), a - p));
}

float shape_segment(vec2 p, vec2 a, vec2 b) {
  float d = shape_line(p, a, b);
  float d0 = dot(p - b, b - a);
  float d1 = dot(p - a, b - a);
  return d1 < 0.0 ? length(a - p) : d0 > 0.0 ? length(b - p) : d;
}

float shape_circles_smin(vec2 p, float t) {
  return smin(shape_circle(p - vec2(cos(t))), shape_circle(p + vec2(sin(t), 0)), 0.8);
}

vec3 draw_line(float d, float thickness) {
  const float aa = 3.0;
  return vec3(smoothstep(0.0, aa / iResolution.y, max(0.0, abs(d) - thickness)));
}

vec3 draw_line(float d) {
  return draw_line(d, 0.0025);
}

float draw_solid(float d) {
  return smoothstep(0.0, 3.0 / iResolution.y, max(0.0, d));
}

vec3 draw_polarity(float d, vec2 p) {
  p += iGlobalTime * -0.1 * sign(d) * vec2(0, 1);
  p = mod(p + 0.06125, 0.125) - 0.06125;
  float s = sign(d) * 0.5 + 0.5;
  float base = draw_solid(d);
  float neg = shape_box2d(p, vec2(0.045, 0.0085) * 0.5);
  float pos = shape_box2d(p, vec2(0.0085, 0.045) * 0.5);
  pos = min(pos, neg);
  float pol = mix(neg, pos, s);

  float amp = abs(base - draw_solid(pol)) - 0.9 * s;

  return vec3(1.0 - amp);
}

vec3 draw_distance(float d, vec2 p) {
  float t = clamp(d * 0.85, 0.0, 1.0);
  vec3 grad = mix(vec3(1, 0.8, 0.5), vec3(0.3, 0.8, 1), t);

  float d0 = abs(1.0 - draw_line(mod(d + 0.1, 0.2) - 0.1).x);
  float d1 = abs(1.0 - draw_line(mod(d + 0.025, 0.05) - 0.025).x);
  float d2 = abs(1.0 - draw_line(d).x);
  vec3 rim = vec3(max(d2 * 0.85, max(d0 * 0.25, d1 * 0.06125)));

  grad -= rim;
  grad -= mix(vec3(0.05, 0.35, 0.35), vec3(0.0), draw_solid(d));

  return grad;
}

vec3 draw_trace(float d, vec2 p, vec2 ro, vec2 rd) {
  vec3 col = vec3(0);
  vec3 line = vec3(1, 1, 1);
  vec2 _ro = ro;

  for (int i = 0; i < TRACE_STEPS; i++) {
    float t = SAMPLER(ro);
    col += 0.8 * line * (1.0 - draw_line(length(p.xy - ro) - abs(t), 0.));
    col += 0.2 * line * (1.0 - draw_solid(length(p.xy - ro) - abs(t) + 0.02));
    col += line * (1.0 - draw_solid(length(p.xy - ro) - 0.015));
    ro += rd * t;
    if (t < 0.01) break;
  }

  #ifdef TRACE_RAY
    col += 1.0 - line * draw_line(shape_segment(p, _ro, ro), 0.);
  #endif

  return col;
}

void mainImage(out vec4 fragColor, in vec2 fragCoord) {
  float t = iGlobalTime * 0.5;
  vec2 uv = squareFrame(iResolution.xy, fragCoord);
  float d;
  vec3 col;
  vec2 ro = vec2(iMouse.xy / iResolution.xy) * 2.0 - 1.0;
  ro.x *= squareFrame(iResolution.xy, iResolution.xy).x;

  vec2 rd = normalize(-ro);

  d = SAMPLER(uv);

  #if DISPLAY == 0
    col = vec3(draw_distance(d, uv.xy));
    //col -= (iMouse.z > 0.0 ? 1.0 : 0.0) * vec3(draw_trace(d, uv.xy, ro, rd));
  #endif
  #if DISPLAY == 1
    col += 1.0 - vec3(draw_line(d));
    col += (iMouse.z > 0.0 ? 1.0 : 0.0) * vec3(1, 0.25, 0) * vec3(draw_trace(d, uv.xy, ro, rd));
    col = 1. - col;
  #endif
  #if DISPLAY == 2
    col = vec3(draw_solid(d));
  #endif
  #if DISPLAY == 3
    col = vec3(draw_polarity(d, uv.xy));
  #endif

  fragColor.rgb = vec3(d);
  fragColor.a   = 1.0;
}

void main() {
    _userMain();
    vec3 c = gl_FragColor.rgb;
    float a = gl_FragColor.a;
    float luma = dot(c, vec3(0.299, 0.587, 0.114));
    c = mix(vec3(luma), c, saturation);
    c = (c - 0.5) * contrast + 0.5;
    c *= vec3(colorR, colorG, colorB);
    c += brightness;
    if (hueShift > 0.001) {
        float cosH = cos(hueShift * 6.28318);
        float sinH = sin(hueShift * 6.28318);
        c = vec3(
            c.r * (0.299 + 0.701*cosH + 0.168*sinH) + c.g * (0.587 - 0.587*cosH + 0.330*sinH) + c.b * (0.114 - 0.114*cosH - 0.497*sinH),
            c.r * (0.299 - 0.299*cosH - 0.328*sinH) + c.g * (0.587 + 0.413*cosH + 0.035*sinH) + c.b * (0.114 - 0.114*cosH + 0.292*sinH),
            c.r * (0.299 - 0.300*cosH + 1.250*sinH) + c.g * (0.587 - 0.588*cosH - 1.050*sinH) + c.b * (0.114 + 0.886*cosH - 0.203*sinH)
        );
    }
    if (invert) c = 1.0 - c;
    gl_FragColor = vec4(clamp(c, 0.0, 1.0), a);
}