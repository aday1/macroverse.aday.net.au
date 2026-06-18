/*{
    "DESCRIPTION": "RaymarchPrimitive-ElectricArc",
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
        }
    ],
    "TAGS": [
        "3d",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

// I'm stuck on this. What 0..0.9999999 value can I store to approximate the original 
// color boundries (shape is okay) better than triliner interpolation on the grid vertex colors?
// uncomment to see what the ideal is:
//#define TARGET

uniform sampler2D bb;

float sdSphere(vec2 p, vec2 c, float r)
{
    return distance(p, c)-r;
}

float sdSphere1(vec2 p)
{
    return sdSphere(p, vec2(345.0, 256.0), 123.0);
}

float sdSphere2(vec2 p)
{
    return sdSphere(p, mouse*resolution, 96.0);
}

float sdSphere3(vec2 p)
{
    return sdSphere(p, vec2(195.0, 256.0), 106.0);
}

vec3 getColor(vec2 p)
{
    if (sdSphere1(p) < sdSphere2(p) && sdSphere1(p) < sdSphere3(p))
    {
        return vec3(0.3, 0.5, 0.1);
    }
    else if (sdSphere2(p) < sdSphere3(p))
    {
        return vec3(0.4, 0.6, 0.8);
    }
    else
    {
        return vec3(0.7, 0.2, 0.3);
    }
}

float shape(vec2 p)
{
    return min(min(sdSphere1(p), sdSphere2(p)), sdSphere3(p));
}

void main() 
{

    vec2 p = gl_FragCoord.xy;
    
    float gridSize = 32.0;
    float grid = max((1.0-smoothstep(0.0, 1.0, mod(p, gridSize).x)), (1.0-smoothstep(0.0, 1.0, mod(p, gridSize).y)));
    gl_FragColor = vec4(grid);
    
    float sphere1 = sdSphere1(p);
    float sphere2 = sdSphere2(p);
    float sphere3 = sdSphere3(p);
    
    vec2 p00 =  floor(p/gridSize)*gridSize;
    vec2 p10 = (floor(p/gridSize)+vec2(1.0, 0.0))*gridSize;
    vec2 p01 = (floor(p/gridSize)+vec2(0.0, 1.0))*gridSize;
    vec2 p11 = (floor(p/gridSize)+vec2(1.0, 1.0))*gridSize;
    vec3 color00 = getColor(p00);
    vec3 color10 = getColor(p10);
    vec3 color01 = getColor(p01);
    vec3 color11 = getColor(p11);
    vec2 np = mod(p, gridSize)/gridSize;
    
    vec3 trilinearColor = mix(mix(color00, color10, np.x), mix(color01, color11, np.x), np.y);
    float trilinearDistance = mix(mix(shape(p00), shape(p10), np.x), mix(shape(p01), shape(p11), np.x), np.y);
    
    #ifdef TARGET
    // how to interpolate to approximate this?
    gl_FragColor.rgb += getColor(p);
    gl_FragColor += vec4(smoothstep(-2.5, 2.5, -abs(shape(p))));
    #else
    gl_FragColor.rgb += trilinearColor;
    gl_FragColor += vec4(smoothstep(-2.5, 2.5, -abs(trilinearDistance)));
    #endif
    
}
