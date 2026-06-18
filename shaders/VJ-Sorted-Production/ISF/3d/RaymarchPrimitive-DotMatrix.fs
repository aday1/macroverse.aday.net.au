/*{
    "DESCRIPTION": "RaymarchPrimitive-DotMatrix",
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
        "geometric",
        "noise",
        "3d",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// 00f404afdd835ac3af3602c8943738ea - please mark changes (and/or add docs), and retain this line.

#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D backbuffer;

float _MinStep = 0.0125;

//////////////////////////////////////////////////////////////
// http://www.gamedev.net/topic/502913-fast-computed-noise/
//  simplified, and broken on purpose. fullreset
vec4 random4 (const vec4 x) {
    vec4 z = mod(mod(x, vec4(5612.0)), vec4(3.1415927 * 2.0));
    return fract ((z*z) * vec4(56812.5453));
}
const float A = 1.0;
const float B = 57.0;
const float C = 113.0;
const vec3 ABC = vec3(A, B, C);
const vec4 A3 = vec4(0, B, C, C+B);
const vec4 A4 = vec4(A, A+B, C+A, C+A+B);
float cnoise4 (const in vec3 x) {
    vec3 fx = fract(x);
    vec3 ix = x-fx;
    vec3 wx = fx*fx*(3.0-2.0*fx);
    float nn = dot(ix, ABC);
    vec4 R = random4(nn+A3);
    float re = mix(mix(R.x, R.y, wx.y), mix(R.z, R.w, wx.y), wx.z);
    return 1.0 - 2.0 * re;
}

//////////////////////////////////////////////////////////////
// distance functions
// http://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm
float sdSphere( vec3 p, float s ) { return length(p)-s; }
float udBox( vec3 p, vec3 b ) {  return length(max(abs(p)-b,0.0)); }
float udRoundBox( vec3 p, vec3 b, float r ) { return length(max(abs(p)-b,0.0))-r; }
float sdTorus( vec3 p, vec2 t ) { vec2 q = vec2(length(p.xz)-t.x,p.y);  return length(q)-t.y; }
vec3  opRep(vec3 p, vec3 r) { return mod(p,r)-0.5*r; }
vec3  opTx(vec3 p, mat4 m ) { return (m*vec4(p,1.0)).xyz; }
/////////////////////////////////////////////////////
// the rest

float fbm(vec3 p) {
 float N = 0.0;
  float D = 0.0;
  int i=0;
  float R = 0.0;
  D += (R = 1.0/pow(2.0,float(i+1))); N = cnoise4(p*pow(2.0,float(i)))*R + N; i+=1;
//  D += (R = 1.0/pow(2.0,float(i+1))); N = cnoise4(p*pow(2.0,float(i)))*R + N; i+=1;
//  D += (R = 1.0/pow(2.0,float(i+1))); N = cnoise4(p*pow(2.0,float(i)))*R + N; i+=1;
//  D += (R = 1.0/pow(2.0,float(i+1))); N = cnoise4(p*pow(2.0,float(i)))*R + N; i+=1;
  return N/D;
}
float scene(vec3 p) { 
  vec3 f = opRep(p * 1.0,vec3(2.7,17.,7.7));
  float d = udRoundBox(f,vec3(1.,0.,1.),0.15);
	
//  d = min(d,udRoundBox(p-pw*2.,vec3(100.,80.,.2),0.22));

   vec3 c = opRep(p,vec3(20.,4.6,14.))-vec3(0.,0.,7.);
   d = min(d,udRoundBox(c,vec3(1.75,2.,0.75),0.15));
	
  float n = fbm(p*10.0);  	
  return d+n*pow(sin(time) * 0.5 + 0.5, 5.0);
}

vec4 color(float d) {  return vec4(.125,d*5.,1.0,0.1)*float(d<0.2); }

vec4 ray(vec3 pos, vec3 step) {
  vec4 sum = vec4(0.);
  vec4 col;
  float d = 9999.0;
#define RAY1  { d = scene(pos); col = color(d); col.rgb *= col.a; sum += col*(1.0 - sum.a); pos += step*max(d,_MinStep); }
#define RAY4  RAY1 RAY1 RAY1 RAY1
  RAY4 RAY4 RAY4 RAY4
	  //sum += vec4(d, d * 0.5, d * 0.85, 0.0);
  return sum;
}

void _userMain( void ) {
  vec3 e = vec3(sin(time*0.2)*20.,12.,-1.); 
  vec3 p = vec3((gl_FragCoord.xy / resolution.xy) * 2. -1., 1.);
  p.x *= resolution.x/resolution.y;
  p += e;
  gl_FragColor = ray(p, normalize(p-e));
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