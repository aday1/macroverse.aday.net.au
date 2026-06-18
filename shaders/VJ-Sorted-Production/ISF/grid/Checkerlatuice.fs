/*{
    "DESCRIPTION": "Checkerlatuice",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

vec3 chequer(vec2 uv) {
    uv = fract(uv);
    return uv.x > 0.5 != uv.y > 0.5 ? vec3(0.6, 0.6, 1.0) : vec3(1.0, 1.0, 0.6);
}

#define PI 3.14159265359
#define TriplePI (3.0 * PI)
#define DoublePI (2.0 * PI)
#define HalfPI (PI / 2.0)

/* 
   These functions take a "camera" variable specifying:
     X/Y: Where on the screen we are rendering.
     Z: How far forwards we are.
   And return:
     X/Y: UVs you can use for texture mapping.
     Z: How far the pixel is.
*/

/* A wall directly in front of you. */
vec3 planeZ(float z, vec3 camera) {
    z -= camera.z;
    return vec3(camera.xy * z, z);
}

/* Parallel walls to the left/right of you. */
vec3 planeX(float x, vec3 camera) {
    float divisor = x / camera.x;
    return vec3(camera.y * divisor, abs(divisor) + camera.z, abs(divisor));
}
        
/* Parallel floor and ceiling. */
vec3 planeY(float y, vec3 camera) {
    float divisor = y / -camera.y;
    return vec3(camera.x * divisor, abs(divisor) + camera.z, abs(divisor));
}

/* 
	Parallel walls to the left/right of you. 
	Lines up with tunnels.
*/
vec3 planeXForTunnel(float x, vec3 camera) {
    float divisor = x / camera.x;
    return vec3(camera.y * divisor + (camera.x > 0.0 ? 0.0 : (camera.y > 0.0 ? 2.0 : -2.0)), abs(divisor) + camera.z, abs(divisor));
}
        
/* 
	Parallel floor and ceiling. 
	Lines up with tunnels.
*/
vec3 planeYForTunnel(float y, vec3 camera) {
    float divisor = y / -camera.y;
    return vec3(camera.x * divisor + (camera.y > 0.0 ? 1.0 : -1.0), abs(divisor) + camera.z, abs(divisor));
}

/* A round tunnel. */
vec3 tunnel(float radius, vec3 camera) {
    float dist = radius / length(camera.xy);
    return vec3(atan(camera.y, camera.x) * radius / HalfPI, dist + camera.z, dist);
}

/* Similar to planeZ, but matches up with tunnel. */
vec3 tunnelEnd(float z, float radius, vec3 camera) {
    float angle = atan(camera.x, camera.y);
    float dist = length(camera.xy);
    return vec3(angle * radius / HalfPI, dist * (z - camera.z), z - camera.z);
}

/* -------------------------------------------------------- */

void _userMain()
{
    vec2 aspectNdc = (gl_FragCoord.xy - (resolution.xy / 2.0)) / (min(resolution.x, resolution.y) / 2.0);
    
    vec3 camera = vec3(aspectNdc, time);
    
	vec3 ringInside = tunnel(mouse.x, camera);
    
    vec3 ringFront = tunnelEnd(floor(ringInside.y) + mouse.y, 1.0, camera);
    
    vec3 wall = planeX(2.0, camera);
    
    vec3 pillarSide = planeX(3.0, camera);
    vec3 pillarFront = planeZ(floor(pillarSide.y) + 0.75, camera);
    
    vec3 sea = planeY(1.0, camera);

    vec3 compute;
    
    if(fract(ringInside.y) < 0.25) {
        compute = ringInside;
    } else {
        if(ringFront.y < 1.2) {
            compute = ringFront;
        } else {
            if(abs(sea.x) > 2.0) {
                if(fract(pillarSide.y) > 0.75) {
                    if(abs(pillarSide.x) < 1.0) {
                        compute = pillarSide;
                    } else {
                        compute = sea;
                    }
                } else {
                    if(abs(pillarFront.y) < 1.0) {
                        if(abs(pillarFront.x) < 3.5) {
                            compute = pillarFront;
                        } else {
                            compute = sea;
                        }
                    } else {
                        compute = sea;
                    }
                }
            } else {
                compute = wall;
            }
        }
    }

	gl_FragColor = vec4(mix(vec3(0.8, 0.9, 1.0), mix(vec3(0.6, 0.7, 1.0), chequer(compute.xy) / (1.0 + compute.z * 0.5), 1.0 / (1.0 + compute.z * 0.2)), 1.0 / (1.0 + compute.z * 0.05)), 1.0);
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