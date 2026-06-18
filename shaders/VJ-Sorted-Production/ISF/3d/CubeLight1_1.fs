/*{
    "DESCRIPTION": "CubeLight1",
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
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// Based on this video: https://www.youtube.com/watch?v=pADUaT6GTmY

#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

#define TRACE_MAX_DIST 19.

float vmax(vec3 v) { return max(v.x, max(v.y, v.z)); }

mat3 rotY(float angle)
{
  float c = cos(angle);
  float s = sin(angle);
  return mat3(c, 0., s, 0., 1., 0., -s, 0., c);
}

mat3 rotX(float angle)
{
  float c = cos(angle);
  float s = sin(angle);
  return mat3(1., 0., 0., 0., c, -s, 0., s, c);
}

float box(vec3 pos, vec3 size)
{
  return vmax(abs(pos) - size);
}

float world(vec3 pos)
{
  // return box(pos, vec3(.5));  // a box
  // return min(box(pos, vec3(.5)), pos.y + 1.); // a box with horizontal plane
  return min(box(rotX(sin(time/4.)*4.) * rotY(time*0.7) * pos, vec3(.5)), pos.y + .9); // a rotating box with horizontal plane
  //return min(length(pos) - .8, pos.y + 1.); // a sphere with horizontal plane
  //return length(pos) - .8; // just a sphere
}

vec3 normal(vec3 pos)
{
  vec2 delta = vec2(0, 0.001);
  float baseDist = world(pos);
  return normalize(vec3(world(pos + delta.yxx) - baseDist, world(pos + delta.xyx) - baseDist, world(pos + delta.xxy) - baseDist));
}

float trace(vec3 origin, vec3 direction, float current_dist, float max_dist)
{
  for (int i = 0; i < 128; ++i) {
    float new_dist = world(origin + direction*current_dist);
    current_dist += new_dist;
    if (new_dist < 0.001)
      break;
    if (current_dist > max_dist)
	    break;
  }

  return current_dist;
}

vec3 enlight(vec3 pos, vec3 normal, vec3 albedo, vec3 lightpos, vec3 lightcolor, vec3 ambient)
{
  vec3 vectorToLight = lightpos - pos;
  float distanceToLight = length(vectorToLight);
  vec3 lightDirection = normalize(vectorToLight);

  // Next two lines is shadow
  float traceToLightPoint = trace(pos + normal*0.011, lightDirection, 0., distanceToLight);
  if (traceToLightPoint < distanceToLight) return ambient + vec3(0.);

  return ambient + max(0.0, dot(lightDirection, normal)) * lightcolor * albedo / dot(vectorToLight, vectorToLight);
}

void _userMain( void ) {
  vec2 position = ( gl_FragCoord.xy / resolution.xy );
  position.y -= 0.5;
  position.x -= 0.5;
  vec2 mmouse = vec2(mouse.x - .5, mouse.y - .5);
  float aspect = resolution.y / resolution.x;
  vec2 aspectedPosition = position * vec2(1., aspect);

  vec3 cameraPos = vec3(0, 0, 6.);
  vec3 traceDirection = normalize(vec3(aspectedPosition, -1));
  float resultDist = trace(cameraPos, traceDirection, 0., TRACE_MAX_DIST);

  vec3 resultColor = vec3(0.);

  if (resultDist < TRACE_MAX_DIST) {
    vec3 resultSurfacePosition = cameraPos + traceDirection * resultDist;
    // Show lit pixels:
    vec3 lightPos = vec3(mmouse.x*4., 2., -mmouse.y*6.);
    resultColor = enlight(resultSurfacePosition, normal(resultSurfacePosition), vec3(1.), lightPos, vec3(2.), vec3(0.05));
	  
    // Show normals:    
    //resultColor = normal(resultSurfacePosition);

    // Show depth:
    //resultColor = vec3( 1./ resultDist);

  }

  gl_FragColor = vec4(resultColor, 1.);
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