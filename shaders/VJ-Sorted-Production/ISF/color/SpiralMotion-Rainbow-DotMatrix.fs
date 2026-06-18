/*{
    "DESCRIPTION": "SpiralMotion-Rainbow-DotMatrix",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "color"
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
        "color",
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

#define TWOPI 6.28318530718
#define PI 3.14159265359

//	Groovymap
//	by Jonathan Proxy
//	
//	Rainbow spot spiral distorted using complex conformal mappings

const float _periodX = 6.;
const float _periodY = 7.;

vec2 cmul(const vec2 a, const vec2 b) {
  return vec2(a.x*b.x - a.y*b.y, a.x*b.y + a.y*b.x);
}
vec2 csq(const vec2 v) {
  return vec2(v.x*v.x - v.y*v.y, 2.*v.x*v.y);
}
vec2 cinv(const vec2 v) {
  return vec2(v[0],-v[1]) / dot(v, v);
}
vec2 cln(const vec2 z) {
  return vec2(log(length(z*.125)), atan(z.y, z.x)); // +2k pi
}

vec2 perturbedNewton(in vec2 z) {
  float a=1.2;
  mat2 rot=mat2(cos(a),-sin(a),sin(a),cos(a));  
  for(int i=0; i<1; ++i) {
    z = rot *(.04*radians(z + cinv(csq(z)))) / 0.5;
  }
  return z;
}

vec2 pentaplexify(const vec2 z) {
  vec2 y = z;
  for(float i=0.; i<TWOPI-0.1; i+=TWOPI/5.) {
    y = cmul(y, z-vec2(cos(i+.1*time), sin(i+.1*time)));
  }
  return y;
}

vec2 infundibularize(in vec2 z) {
  vec2 lnz = cln(z) / TWOPI;
  return vec2(_periodX*(lnz.y) + _periodY*(lnz.x), _periodX*(lnz.x) - _periodY*(lnz.y));
}

vec3 hsv(float h, float s, float v) {
  return v * mix(
    vec3(1.0),
    clamp((abs(fract(h+vec3(3.0, 2.0, 1.0)/3.0)*6.0-3.0)-1.0), 0.0, 1.0), 
    s);
}

vec4 rainbowJam(in vec2 z) {
  vec2 uv = fract(vec2(z[0]/_periodX, z[1]/_periodY))*vec2(_periodX, _periodY);
  vec2 iz = floor(uv);
  vec2 wz = uv - iz;
  return vec4(hsv(pow(iz[0]/_periodX, 1.5),0.9,smoothstep(0.45,0.4,length(wz-vec2(0.5)))), 1.);
}

void _userMain( void ) {
  gl_FragColor = 
    rainbowJam(infundibularize(pentaplexify(perturbedNewton(3.*(2.*gl_FragCoord.xy-resolution.xy) / resolution.y)))
  + 0.4*(mouse.xy-0.5)
  + time * -0.2);
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