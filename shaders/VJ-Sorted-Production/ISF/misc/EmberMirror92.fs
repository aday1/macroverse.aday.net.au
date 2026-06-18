/*{
    "DESCRIPTION": "EmberMirror92",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "misc"
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
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
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

void main()
{
	gl_FragColor = vec4(compute(gl_FragCoord.xy, resolution.xy, time),1.0);
}
