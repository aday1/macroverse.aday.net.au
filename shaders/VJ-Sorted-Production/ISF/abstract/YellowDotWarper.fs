/*{
    "DESCRIPTION": "YellowDotWarper",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "abstract"
    ]
}*/
#define E 2.71828182846
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

uniform vec4 inputColour;

#define _Color vec3( inputColour.x, inputColour.y, inputColour.z );
#define _Radius mouse.y
#define _PulseSpeed 10.0
#define _WaveAmount mouse.x

void main( void )
{

    vec2 uv = ( gl_FragCoord.xy / resolution.xy ) * 2.0 - 1.0;
    uv.x *= resolution.x /resolution.y;

    vec3 finalColor = vec3( 0.0 );
    float t1 = sin(uv.x + time * _WaveAmount) *0.5 + 0.5;
    float uvx = mix(uv.x-1.0, uv.x+1.0,t1);
    t1 = sin(uv.y * time * _WaveAmount) *inputColour.w + 0.5;
    float uvy = mix(uv.y-inputColour.w, uv.y+1.0,t1);
    float dist = sqrt(pow(uvx, 2.0) + pow(uvy,2.0));
	
    float t = sin(time * _PulseSpeed) *inputColour.w + 0.5;
    float pulse = mix(0.01, inputColour.w, t);
	
    if (dist + 0.0 > _Radius)
    {
        finalColor = vec3(inputColour.y,inputColour.z,inputColour.x);
    }
    else
    {
        finalColor = _Color;
    }

    gl_FragColor = vec4( finalColor, 1.0 );
}
