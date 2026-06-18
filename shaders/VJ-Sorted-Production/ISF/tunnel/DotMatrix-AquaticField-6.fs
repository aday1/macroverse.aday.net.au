/*{
    "DESCRIPTION": "DotMatrix-AquaticField-6",
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
        "tunnel"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

#define N 16

#define CASE(n) if ( i == n )

#define BK 0  // black
#define WH 1  // white
#define BG 2  // beige
#define BR 3  // brown
#define RD 4  // red
#define YL 5  // yellow
#define GR 6  // green
#define WT 7  // water
#define BL 8  // blue
#define PR 9  // purple

int DS[256]; // dataSet[]
#define INITDATA(b,d0,d1,d2,d3,d4,d5,d6,d7,d8,d9,d10,d11,d12,d13,d14,d15) DS[b+0]=d0;DS[b+1]=d1;DS[b+2]=d2;DS[b+3]=d3;DS[b+4]=d4;DS[b+5]=d5;DS[b+6]=d6;DS[b+7]=d7;DS[b+8]=d8;DS[b+9]=d9;DS[b+10]=d10;DS[b+11]=d11;DS[b+12]=d12;DS[b+13]=d13;DS[b+14]=d14;DS[b+15]=d15;

// https://glsl.heroku.com/e#14516.0
void initData()
{
    INITDATA(  0,BK,BK,BK,BK,BK,BK,BK,BK,BK,BK,BK,BK,BK,BG,BG,BG)
    INITDATA( 16,BK,BK,BK,BK,BK,BK,RD,RD,RD,RD,RD,BK,BK,BG,BG,BG)
    INITDATA( 32,BK,BK,BK,BK,BK,RD,RD,RD,RD,RD,RD,RD,RD,RD,BG,BG)
    INITDATA( 48,BK,BK,BK,BK,BK,BR,BR,BR,BG,BG,BR,BG,BK,RD,RD,RD)
    INITDATA( 64,BK,BK,BK,BK,BR,BG,BR,BG,BG,BG,BR,BG,BG,RD,RD,RD)
    INITDATA( 80,BK,BK,BK,BK,BR,BG,BR,BR,BG,BG,BG,BR,BG,BG,BG,RD)
    INITDATA( 96,BK,BK,BK,BK,BR,BR,BG,BG,BG,BG,BR,BR,BR,BR,RD,BK)
    INITDATA(112,BK,BK,BK,BK,BK,BK,BG,BG,BG,BG,BG,BG,BG,RD,BK,BK)
    INITDATA(128,BK,BK,RD,RD,RD,RD,RD,BL,RD,RD,RD,BL,RD,BK,BK,BK)
    INITDATA(144,BK,RD,RD,RD,RD,RD,RD,RD,BL,RD,RD,RD,BL,BK,BK,BR)
    INITDATA(160,BG,BG,RD,RD,RD,RD,RD,RD,BL,BL,BL,BL,BL,BK,BK,BR)
    INITDATA(176,BG,BG,BG,BK,BL,BL,RD,BL,BL,YL,BL,BL,YL,BL,BR,BR)
    INITDATA(192,BK,BG,BK,BR,BL,BL,BL,BL,BL,BL,BL,BL,BL,BL,BR,BR)
    INITDATA(208,BK,BK,BR,BR,BR,BL,BL,BL,BL,BL,BL,BL,BL,BL,BR,BR)
    INITDATA(224,BK,BR,BR,BR,BL,BL,BL,BL,BL,BL,BL,BK,BK,BK,BK,BK)
    INITDATA(240,BK,BR,BK,BK,BL,BL,BL,BL,BK,BK,BK,BK,BK,BK,BK,BK)
}

vec3 getRgbColor( int i )
{
    vec3 result;
    CASE(0) result = vec3( float(  0.0/255.0), float(  0.0/255.0), float(  0.0/255.0)); // black
    CASE(1) result = vec3( float(255.0/255.0), float(255.0/255.0), float(255.0/255.0)); // white
    CASE(2) result = vec3( float(255.0/255.0), float(204.0/255.0), float(204.0/255.0)); // beige
    CASE(3) result = vec3( float(128.0/255.0), float(  0.0/255.0), float(  0.0/255.0)); // brown
    CASE(4) result = vec3( float(255.0/255.0), float(  0.0/255.0), float(  0.0/255.0)); // red
    CASE(5) result = vec3( float(255.0/255.0), float(255.0/255.0), float(  0.0/255.0)); // yellow
    CASE(6) result = vec3( float(  0.0/255.0), float(255.0/255.0), float(  0.0/255.0)); // green
    CASE(7) result = vec3( float(  0.0/255.0), float(255.0/255.0), float(255.0/255.0)); // water
    CASE(8) result = vec3( float(  0.0/255.0), float(  0.0/255.0), float(255.0/255.0)); // blue
    CASE(9) result = vec3( float(128.0/255.0), float(  0.0/255.0), float(128.0/255.0)); // purple
    return result;
}

// forked from http://tokyodemofest.jp/2014/7lines/index.html
void _userMain( void ) 
{
    initData();
    // x:-1 to 1
    vec2 pos = (gl_FragCoord.xy-.5*resolution)/min(resolution.x,resolution.y);
    float l;
    vec3 c = vec3(0);
    for ( int x = 0; x < N; x++ ) {
        for ( int y = 0; y < N; y++ ) {
            l = 1.0-sign(length(pos-vec2(sin(time)*float(x)/float(N),float(y)/float(N)-0.5))-0.03);
            int color = DS[(N-y) * N + x];
            c += l * getRgbColor(color);
        }
    }
    gl_FragColor = vec4( c, 1.0 );

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