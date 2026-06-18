/*{
    "DESCRIPTION": "YellowGreenElemental1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "noise"
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
        "noise"
    ]
}*/
#define E 2.71828182846
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
//Will J
//Explosion practice

#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

uniform vec4 inputColour;

#define _Color vec3( mouse.x, cos(time+mouse.y), sin(time) );
#define _Radius 0.6
#define _PulseSpeed 55

float hash( float n ) { return fract(sin(n)*753.5453123); }
float noise( in vec2 x )
{
    vec2 p = floor(x);
    vec2 f = fract(x);
    f = f*f*(3.0-2.0*f);
    
    float n = p.x + p.y*157.0;
    return mix(
                    mix( hash(n+  mouse.x), hash(n+  mouse.y),f.x),
                    mix( hash(n+157.0), hash(n+158.0),f.x),
            f.y);
}

float fbm(vec2 p, vec3 a)
{
     float v = 0.0;
     v += noise(p*a.x)*.5;
     v += noise(p*a.y)*.25;
     v += noise(p*a.z)*.125;
     return v;
}

void main( void )
{

    vec2 uv = ( gl_FragCoord.xy / resolution.xy ) * 2.0 - 1.0;
    uv.x *= resolution.x /resolution.y;

    vec3 finalColor = vec3( inputColour.x );
    float dist = length(uv);
	 
    float size = 1.0/(mod(time,inputColour.w)) * 0.07;

    float t = sin(time * _PulseSpeed) *inputColour.w + 0.5;
	
    float aTan = atan(uv.y/uv.x); 
    float t1 =  fbm(uv,vec3(6,10,6)) * inputColour.z;
    float warp = mix(mouse.x, mouse.y, t1);

    if (dist + size  > _Radius + warp)
    {
        finalColor = vec3(0,0,0);
    }
    else
    {
        finalColor =  vec3(inputColour.x,0.51,0) * (inputColour.y/warp)  * ((_Radius + warp) - (dist + size));
    }

    gl_FragColor = vec4( finalColor, inputColour.x );
}
