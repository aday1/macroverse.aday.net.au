/*{
    "DESCRIPTION": "Clouder",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "plasma"
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "plasma",
        "noise"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// Procedural Burning Fire
// By Brandon Fogerty
// bfogerty at gmail dot com
// Special Thanks Inigo Quilez

#ifdef GL_ES
precision mediump float;
#endif

uniform vec4 inputColour;

#define FlameSpeed  0.09

#define Time        time * FlameSpeed
#define Resolution  resolution

#define ColorScale  0.45
#define Color0      vec3( 0.1, 0.1, 0.3 )     * ColorScale
#define Color1      vec3( 0.07, 0.4, 0.2 )     * ColorScale
#define Color2      vec3( 0.4, 0.15, 0.15 )   * ColorScale
#define Color3      vec3( 0.2, 0.15, 0.15 )   * ColorScale
#define Color4      vec3( 0.15, 0.15, 0.15 )  * ColorScale
#define Color5      vec3( 0.10, 0.10, 0.10 )  * ColorScale

float hash( float x )
{
    return fract( sin( x ) * 43758.5453 );
}

float noise( vec2 uv )
{
    vec3 x = vec3( uv.xy, 0.0 );
    
    vec3 p = floor( x );
    vec3 f = fract( x );
    
    f = f*f*(3.0 - 2.0*f);
    
    float n = dot( p, vec3(1.0, 57.0, 113.0) );
    
    return mix( mix(    mix( hash( n + 0.0 ),   hash( n + 1.0 ), f.x ),
                        mix( hash( n + 57.0),   hash( n + 58.0), f.x ), f.y ),
                mix(    mix( hash( n + 113.0),  hash( n + 114.0), f.x),
                        mix( hash( n + 170.0),  hash( n + 171.0), f.x), f.y), f.z);
}

float perlinNoise( vec2 uv )
{
    
    vec2 uv1 = uv + vec2( 0.0, -Time * 0.1 );
    vec2 uv2 = uv + vec2( cos(-Time) * 7.0, -Time * 0.3 );
    vec2 uv3 = uv + vec2( 0.0, -Time * 0.4 );
    vec2 uv4 = uv + vec2( sin(-Time * 2.0), -Time * 0.2 );
    
    float n =   noise( uv1 * 1.0 )  * 128.0 +
                noise( uv1 * 2.0 )  * 64.0 +
                noise( uv1 * 4.0 )  * 32.0 +
                noise( uv * 8.0 )   * 16.0 +
                noise( uv * 16.0 )  * 8.0 +
                noise( uv4 * 32.0 )     * 4.0 +
                noise( uv4 * 64.0 )     * 2.0 +
                noise( uv * 128.0 ) * 1.0;

    return  n / ( 1.0 + 2.0 + 4.0 + 8.0 + 16.0 + 32.0 + 64.0 + 128.0 );
}

float fBm( vec2 uv )
{
    float mag = 0.0;
    float freq = 1.0;
    
    for( int i = 0; i < 6; ++i )
    {
    uv = uv + vec2( 0.0, -Time * 2.0 );
        mag += abs(perlinNoise( uv * freq ) - 0.5) * 2.0/freq;
        freq *= 0.80;
    }
    
    return mag;
}

vec3 blendColor( vec2 uv, vec3 color0, vec3 color1, float scalar, float minLimit, float maxLimit )
{
    if( uv.y < minLimit || uv.y >= maxLimit )
    {
        return vec3( 0.0, 0.0, 0.0 );
    }
    
    float t = ( uv.y - minLimit ) / ( maxLimit - minLimit );
    return mix( color0 * scalar, color1 * scalar, t );
}

void _userMain(void)
{
    vec2 uv = gl_FragCoord.xy / Resolution.xy;
    uv.x *= (Resolution.x / Resolution.y);
    
        float c = fBm( uv * 5.0 );
    
    vec3 finalColor = vec3( 0.0, 0.0, 0.0 );
    finalColor =  blendColor( uv, Color0, Color1, c,  0.00, 0.30 );
    finalColor += blendColor( uv, Color1, Color2, c,  0.30, 0.70 );
    finalColor += blendColor( uv, Color2, Color3, c,  0.70, 0.80 );
    finalColor += blendColor( uv, Color3, Color4, c,  0.80, 0.95 );
    finalColor += blendColor( uv, Color4, Color5, c,  0.95, 1.00 );
    finalColor *= pow( c, 1.3 );
    
    gl_FragColor = vec4( finalColor, 1.0);
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