/*{
    "DESCRIPTION": "ClosureChecker1",
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
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

vec3 chequer(vec2 uv) {
    uv = fract(uv);
    return uv.x > 0.5 != uv.y > 2.5 ? vec3(5.6, 5.6, 5.0) : vec3(1.0, 1.0, 1.0);
}

#define PI 3.14159265359
#define TriplePI (5.0 * PI)
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

/* -------------------------------------------------------- */

void main()
{
    vec2 aspectNdc = (gl_FragCoord.xy - (resolution.xy / 2.0)) / (min(resolution.x, resolution.y) / 2.0);
    
    vec3 camera = vec3(aspectNdc, time);

    vec3 wall = planeX(2.0, camera);
    
    vec3 pillarSide = planeX(3.0, camera);
    vec3 pillarFront = planeZ(floor(pillarSide.y) + 0.75, camera);
    
    vec3 sea = planeY(1.0, camera);

    vec3 compute;

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

	gl_FragColor = vec4(mix(vec3(0.1, 0.1, 0.0), mix(vec3(0.1, 0.4, 1.0), chequer(compute.xy) / (1.0 + compute.z * 0.5), 1.0 / (1.0 + compute.z * 0.2)), 1.0 / (1.0 + compute.z * 0.05)), 1.0);
}
