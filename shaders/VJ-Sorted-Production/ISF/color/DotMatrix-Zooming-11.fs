/*{
    "DESCRIPTION": "DotMatrix-Zooming-11",
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
        "color"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

vec2 spatial;

vec3 chequer(vec2 uv) {
    uv = fract(uv);
    return uv.x > 0.5 == uv.y > 0.5 ? vec3(1.0, 1.0, 0.6): vec3(0.6, 0.6, 1.0);
}

#define PI 3.142

vec2 worldUv;
vec3 col;

float camZ;

void platform(float left, float right, float top, float bottom, float front, float back) {    
    if(left < right) {
        if(spatial.x < left || spatial.x > right) return;
    } else {
    	if(spatial.x < left && spatial.x > right) return;
    }
    
    float topPos = (1.0 / (spatial.y / top)) + camZ;
    if(topPos < front) {
        float y = spatial.y * (front - camZ);
        if(y > bottom) return;
        col = vec3(0.3);
        worldUv = vec2(spatial.x * 6.0, y);
        return;
    }
    if(topPos > back) return;
    worldUv = vec2(spatial.x * 6.0, topPos);
    col = vec3(2.0 / ((top / 3.0) + 1.0));
}

vec3 compute(vec2 coord, vec2 res, float seconds) {
    coord -= res / 2.0;
    float extent = min(res.x, res.y);
    coord.y -= extent / 3.0;
    spatial = vec2((atan(coord.x, coord.y) / PI * 2.0) + 2.0, length(coord) * 2.0 / extent);
    seconds *= 0.25;
    spatial.x += seconds;
    spatial.x = mod(spatial.x, 4.0);
    camZ = sin(PI * seconds) * 4.0 + 4.0;
    
    col = vec3(0.0);
    
    platform(3.75, 0.25, 6.0, 7.0, 5.0, 16.0);
    
	platform(0.25, 0.375, 3.0, 25.0, 7.0, 9.0);
    
    platform(0.25, 0.5, 6.5, 7.0, 14.0, 16.0);
    platform(0.5, 0.75, 7.0, 7.5, 14.0, 16.0);
    platform(0.75, 1.0, 7.5, 8.0, 14.0, 16.0);
    platform(1.0, 1.25, 8.0, 8.5, 6.0, 16.0);
    platform(1.25, 2.0, 6.0, 8.5, 7.5, 16.0);
    platform(1.375, 1.625, 7.0, 7.5, 6.0, 7.5);
    platform(1.75, 2.0, 6.0, 6.5, 6.0, 7.5);
    platform(2.0, 2.5, 6.0, 8.5, 8.5, 16.0);
    
    platform(3.75, 3.8, 3.0, 6.0, 13.0, 15.0);
    platform(3.625, 3.75, 3.0, 25.0, 13.0, 15.0);
    
    platform(0.25, 0.375, 3.0, 9.0, 7.0, 9.0);
    platform(0.2, 0.25, 3.0, 6.0, 7.0, 9.0);
    
    platform(3.75, 3.8, 3.0, 6.0, 7.0, 9.0);
    platform(3.625, 3.75, 3.0, 25.0, 7.0, 9.0);
    
    platform(1.5, 2.5, 2.0, 6.0, 10.0, 16.0);
    platform(2.5, 2.75, 6.0, 7.0, 7.0, 16.0);
    platform(2.5, 3.75, 6.0, 7.0, 5.0, 7.0);
    
    vec3 color = chequer(worldUv) * col;
    
    return pow(color, vec3(1.0 / 2.2));
}

void _userMain()
{
	gl_FragColor = vec4(compute(gl_FragCoord.xy, resolution.xy, time),1.0);
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