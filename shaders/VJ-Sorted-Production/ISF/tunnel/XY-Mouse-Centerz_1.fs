/*{
    "DESCRIPTION": "XY-Mouse-Centerz",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "tunnel"
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
        "texture-input"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// by @notlion, @alteredq

#ifdef GL_ES
precision highp float;
#endif

uniform sampler2D backbuffer;

float getSpring(float r, vec2 pos, float power){
  //return (1. - r) * power;  
  return (texture2D(backbuffer, pos).r - r) * power;
}

void _userMain(){
  vec2 pos = gl_FragCoord.xy / resolution;
  vec2 pixel = 8. / resolution;
  float aspect = resolution.x / resolution.y;

  vec4 texel_prev =texture2D(backbuffer, pos);
  //  //
  float r_prev = texel_prev.r;
  float power = .5;

  float vel = texel_prev.a - 0.5;
  vel += getSpring(r_prev, pos + pixel * vec2(2, 3), 0.0022411859348636983 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(1, 3), 0.004759786021770571 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(0, 3), 0.005681818181818182 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-1, 3), 0.004759786021770571 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-2, 3), 0.0022411859348636983 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(3, 2), 0.0022411859348636983 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(2, 2), 0.0066566640639421 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(1, 2), 0.010022341036933013 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(0, 2), 0.011363636363636364 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-1, 2), 0.010022341036933013 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-2, 2), 0.0066566640639421 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-3, 2), 0.0022411859348636983 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(3, 1), 0.004759786021770571 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(2, 1), 0.010022341036933013 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(1, 1), 0.014691968395607415 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(0, 1), 0.017045454545454544 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-1, 1), 0.014691968395607415 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-2, 1), 0.010022341036933013 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-3, 1), 0.004759786021770571 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(3, 0), 0.005681818181818182 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(2, 0), 0.011363636363636364 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(1, 0), 0.017045454545454544 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-1, 0), 0.017045454545454544 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-2, 0), 0.011363636363636364 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-3, 0), 0.005681818181818182 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(3, -1), 0.004759786021770571 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(2, -1), 0.010022341036933013 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(1, -1), 0.014691968395607415 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(0, -1), 0.017045454545454544 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-1, -1), 0.014691968395607415 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-2, -1), 0.010022341036933013 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-3, -1), 0.004759786021770571 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(3, -2), 0.0022411859348636983 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(2, -2), 0.0066566640639421 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(1, -2), 0.010022341036933013 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(0, -2), 0.011363636363636364 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-1, -2), 0.010022341036933013 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-2, -2), 0.0066566640639421 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-3, -2), 0.0022411859348636983 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(2, -3), 0.0022411859348636983 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(1, -3), 0.004759786021770571 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(0, -3), 0.005681818181818182 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-1, -3), 0.004759786021770571 * power);
  vel += getSpring(r_prev, pos + pixel * vec2(-2, -3), 0.0022411859348636983 * power);

  vel += (.25 - r_prev) * .025 * power; 
  float sz = 1.;
  vel += max(0., .1 * (sz - (length((pos - mouse) * vec2(aspect, sz)) * 20.)));

  gl_FragColor = vec4(texel_prev.rgb + vel, vel * .98 + .5);
 // gl_FragColor *= vec4( 1.0, 0.85, 0.0, 1.0 );

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