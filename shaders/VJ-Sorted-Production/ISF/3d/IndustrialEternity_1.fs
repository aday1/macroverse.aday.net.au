/*{
    "DESCRIPTION": "IndustrialEternity",
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
        "tunnel",
        "particles",
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

float EPS = min(max(sin(time),.01),.1);

vec2 onRep(vec2 p, float interval) {
  return mod(p, interval) - interval * 0.5;
}

float distBar(vec2 p, float interval, float width) {
  return length(max(abs(onRep(p, interval)) - width, 0.0));
}

float distTube(vec2 p, float interval, float width) {
  return length(onRep(p, interval)) - width;
}

// distance function
float distScene(vec3 p) {
  float bar_x = distBar(p.yz, 1.0, 0.1);
  float bar_y = distBar(p.xz, 1.0, 0.1);
  float bar_z = distBar(p.xy, 1.0, 0.1);

  float tube_x = distTube(p.yz, 0.1, 0.025);
  float tube_y = distTube(p.xz, 0.1, 0.025);
  float tube_z = distTube(p.xy, 0.1, 0.025);

  return max(max(max(min(min(bar_x, bar_y),bar_z), -tube_x), -tube_y), -tube_z);
}

vec3 getNormal(vec3 p) {
  return normalize(vec3(
    distScene(p + vec3(  EPS, 0.0, 0.0)) - distScene(p + vec3( -EPS, 0.0, 0.0)),
    distScene(p + vec3(0.0,   EPS, 0.0)) - distScene(p + vec3(0.0,  -EPS, 0.0)),
    distScene(p + vec3(0.0, 0.0,   EPS)) - distScene(p + vec3(0.0, 0.0,  -EPS))
  ));
}

void _userMain(void) {
  // fragment position
  vec2 p = (gl_FragCoord.xy * 2.0 - resolution) / min(resolution.x, resolution.y);

  // camera and ray
  vec3 cPos = vec3(0.0, 0.0, time);
  vec3 cUp  = normalize(vec3(0.1, 0.4, 0.0));
  vec3 cDir = cross(cUp, vec3(-1.0, 0.0, 0.0));
  vec3 cSide = cross(cDir, cUp);
  float targetDepth = 1.0;
  vec3 ray = normalize(cSide * p.x + cUp * p.y + cDir * targetDepth);

  // direction light
  vec3 lightDir = normalize(vec3(1, 1, -2));

  // marching loop
  float dist;
  float depth = 0.0;
  vec3 dPos = cPos;
  for(int i = 0; i < 64; i++){
    dist = distScene(dPos);
    depth += dist;
    dPos = cPos + depth * ray;
    if (abs(dist) < EPS) break;
  }

  // hit check
  vec3 color;
  if (abs(dist) < EPS) {
    vec3 normal = getNormal(dPos);
    float diffuse = clamp(dot(lightDir, normal), 0.1, 1.0);
    color = vec3(1.0, 0.1, 0.1) * diffuse;
  } else {
    color = vec3(0.0);
  }
  gl_FragColor = vec4(color + 0.05 * depth, 1.0);
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