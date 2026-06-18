/*{
    "DESCRIPTION": "DotMatrix-Emerald-LitSurface-3",
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
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

#define DIST_COEFF	1.00
#define ITE_MAX		300
#define DIST_MIN   	0.0001
#define DIST_MAX	500.0

vec3 diffColor;

float smoothMin(float d1, float d2, float k) {
  float h = exp(-k * d1) + exp(-k * d2);
  return -log(h) / k;
}

float distFuncFloor(vec3 pos) {
  return (1.0 + dot(pos, vec3(0.0, 1.0, 0.0)));
}

float distFuncSphere1(vec3 pos) {
  vec3 t_pos = vec3(pos.x + cos((time + 0.5) / 1.5) + 0.5, pos.y, pos.z + sin(time) + 1.0 );
  return length(t_pos) - 1.0;
}

float distFuncSphere2(vec3 pos) {
  vec3 t_pos = vec3(pos.x - 1.0, pos.y + 0.0, pos.z - 0.2);
  return length(t_pos) - 0.7;
}

float distanceFunctionWithColor(vec3 pos)
{
  float t = DIST_MAX;
  float w = 0.0;
  float st1 = 0.0;
  float st2 = 0.0;
  float st = 0.0;
  float ft = 0.0;
  
  w = distFuncSphere1(pos);
  st1 = min(t, w);

  w = distFuncSphere2(pos);
  st2 = min(t, w);
  
  w = distFuncFloor(pos);
  ft = min(t, w);

  st = min(st1, st2);
  t = min(st, ft);

  if (t == st1) {
    diffColor = vec3( 1.0, 0.0, 0.0 );
    return smoothMin(st1, st2, 8.0);
  } else if (t == st2) {
    diffColor = vec3( 1.0, 0.5, 1.0 );
    return smoothMin(st1, st2, 8.0);
  } else if (t == ft) {
    diffColor = vec3( 0.0, 1.0, 0.0 );
    return ft;
  }
  
  return t;
}

float distanceFunction(vec3 pos)
{
  float t = DIST_MAX;
  float w = 0.0;
  float st1 = 0.0;
  float st2 = 0.0;
  float ft = 0.0;
  
  w = distFuncSphere1(pos);
  st1 = min(t, w);
  
  w = distFuncSphere2(pos);
  st2 = min(t, w);
  
  w = distFuncFloor(pos);
  ft = min(t, w);

  st1 = smoothMin(st1, st2, 4.0);
  t = min(st1, ft);

  return t;
}

vec3 getNormal(vec3 p)
{
  float d = 0.0001;
  return
    normalize
    (
      vec3
      (
        distanceFunction(p+vec3(d,0.0,0.0))-distanceFunction(p+vec3(-d,0.0,0.0)),
        distanceFunction(p+vec3(0.0,d,0.0))-distanceFunction(p+vec3(0.0,-d,0.0)),
        distanceFunction(p+vec3(0.0,0.0,d))-distanceFunction(p+vec3(0.0,0.0,-d))
      )
    );
}

void _userMain() {
  vec2 pos = (gl_FragCoord.xy*2.0 -resolution) / resolution.y;

  vec3 camPos = vec3(0.0, 0.0 + 0.1 * cos(time), 2.0);	
  vec3 camDir = vec3(0.10 * cos(time/ 3.0), 0.0, -1.0);
  vec3 camUp = vec3(0.0, 1.0, 0.0);
  vec3 camSide = cross(camDir, camUp);
  float focus = 1.0;
 
  vec3 rayDir = normalize(camSide*pos.x + camUp*pos.y + camDir*focus);
 
  vec3 lightDir = vec3(-4.577, 4.577, 6.577);
 
  float t = 0.0, d;
  vec3 posOnRay = camPos;

  for(int i=0; i<ITE_MAX; ++i)
  {
    d = distanceFunctionWithColor(posOnRay);
    if(d < DIST_MIN) {break;}
    t += d * DIST_COEFF;
    posOnRay = camPos + t*rayDir;
  }

  vec3 ip = camPos + camDir * t;
  vec3 normal = getNormal(posOnRay);
  vec3 light = normalize(lightDir);

  vec3 color;
  float shadow = 1.0;
  
  if(abs(d) < DIST_MIN)
  {
    float diff = clamp(dot(lightDir, normal), 0.1, 1.0);
    float spec = pow(clamp(dot(normalize(lightDir - rayDir), normal), 0.0, 1.0), 20.0);
    color = diffColor * vec3(diff) + vec3(spec) * 0.8;

    color = vec3(color * 0.8);
  }else
  {
    color = vec3(1.0);
  }
  gl_FragColor = vec4(color, 1.0);
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