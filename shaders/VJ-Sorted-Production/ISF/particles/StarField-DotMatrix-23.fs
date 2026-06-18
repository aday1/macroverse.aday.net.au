/*{
    "DESCRIPTION": "StarField-DotMatrix-23",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "particles"
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
            "NAME": "ready",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Ready"
        }
    ],
    "TAGS": [
        "particles"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

/**
 * Starscroll by LJ  :  U can use the scroller : http://glslsandbox.com/e#27935.1
 * @Gtr why so complicated :) ? 
 */

uniform float ready;

//For the Bars
vec2 position;
vec3 bars;
float barsize = 0.05;
float barsangle = 1.4;

//For the Beer
vec3 color;

//Starfield
float rand (in vec2 uv) { return fract(sin(dot(uv,vec2(12.4124,48.4124)))*48512.41241); }
const vec2 O = vec2(0.,1.);
float noise (in vec2 uv) {
        vec2 b = floor(uv);
        return mix(mix(rand(b),rand(b+O.yx),.5),mix(rand(b+O),rand(b+O.yy),.5),.5);
}

#define DIR_RIGHT -1.
#define DIR_LEFT 1.
#define DIRECTION DIR_LEFT

#define LAYERS 5
#define SPEED 100.
#define SIZE 4.
#define goTYPE vec2 p = ( gl_FragCoord.xy /resolution.xy ) * vec2(64,32);vec3 c = vec3(0);vec2 cpos = vec2(1,26);vec3 txColor = vec3(1);
#define goPRINT gl_FragColor = vec4(c, 1.0);

#define slashN cpos = vec2(1,cpos.y-6.);

#define inBLK txColor = vec3(0);
#define inWHT txColor = vec3(1);
#define inRED txColor = vec3(1,0,0);
#define inGRN txColor = vec3(0,1,0);
#define inBLU txColor = vec3(0,0,1);

// via http://www.dafont.com/pixelzim3x5.font
#define A c += txColor*Sprite3x5(31725.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define B c += txColor*Sprite3x5(31663.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define C c += txColor*Sprite3x5(31015.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define D c += txColor*Sprite3x5(27502.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define E c += txColor*Sprite3x5(31143.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define F c += txColor*Sprite3x5(31140.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define G c += txColor*Sprite3x5(31087.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define H c += txColor*Sprite3x5(23533.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define I c += txColor*Sprite3x5(29847.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define J c += txColor*Sprite3x5(4719.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define K c += txColor*Sprite3x5(23469.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define L c += txColor*Sprite3x5(18727.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define M c += txColor*Sprite3x5(24429.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define N c += txColor*Sprite3x5(7148.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define O c += txColor*Sprite3x5(31599.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define P c += txColor*Sprite3x5(31716.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define Q c += txColor*Sprite3x5(31609.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define R c += txColor*Sprite3x5(27565.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define S c += txColor*Sprite3x5(31183.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define T c += txColor*Sprite3x5(29842.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define U c += txColor*Sprite3x5(23407.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define V c += txColor*Sprite3x5(23402.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define W c += txColor*Sprite3x5(23421.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define X c += txColor*Sprite3x5(23213.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define Y c += txColor*Sprite3x5(23186.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define Z c += txColor*Sprite3x5(29351.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN

#define n0 c += txColor*Sprite3x5(31599.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define n1 c += txColor*Sprite3x5(9362.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define n2 c += txColor*Sprite3x5(29671.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define n3 c += txColor*Sprite3x5(29391.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define n4 c += txColor*Sprite3x5(23497.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define n5 c += txColor*Sprite3x5(31183.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define n6 c += txColor*Sprite3x5(31215.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define n7 c += txColor*Sprite3x5(29257.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define n8 c += txColor*Sprite3x5(31727.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define n9 c += txColor*Sprite3x5(31695.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN

#define DOT c += txColor*Sprite3x5(2.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define COLON c += txColor*Sprite3x5(1040.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define PLUS c += txColor*Sprite3x5(1488.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define DASH c += txColor*Sprite3x5(448.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define LPAREN c += txColor*Sprite3x5(10530.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define RPAREN c += txColor*Sprite3x5(8778.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN

#define _ cpos.x += 4.;if(cpos.x > 61.) slashN

#define BLOCK c += txColor*Sprite3x5(32767.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define QMARK c += txColor*Sprite3x5(25218.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define EXCLAM c += txColor*Sprite3x5(9346.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN

// all characters are 3x5 (15 bit) bitvectors starting at lower right corner --jz
//                                    row total
//          16384 |   8192 |   4096 = 28672
//           2048 |   1024 |    512 = 3584
//            256 |    128 |     64 = 448
//             32 |     16 |      8 = 56
//              4 |      2 |      1 = 7
//          =========================
//col total 18724 |   9362 |   4681

//returns 0/1 based on the state of the given bit in the given number
float getBit(float num,float bit){
    num = floor(num);
    bit = floor(bit);
    return float(mod(floor(num/pow(2.,bit)),2.) == 1.0);
}

float Sprite3x5(float sprite,vec2 p){
    float bounds = float(all(lessThan(p,vec2(3,5))) && all(greaterThanEqual(p,vec2(0,0))));
    return getBit(sprite,(2.0 - p.x) + 3.0 * p.y) * bounds;
}

//The Rasterbars
vec3 mixcol(float value, float r, float g, float b)
{
        return vec3(value * r, value * g, value * b);
}

void bar(float pos, float r, float g, float b)
{
         if ((position.y <= pos + barsize) && (position.y >= pos - barsize))
                bars = mixcol(1.0 - abs(pos - position.y) / barsize, r, g, b);
}

void main( void ) {

        //The bars
        {
            position = ( gl_FragCoord.xy / resolution.xy );
            position = position * vec2(2.0) - vec2(0.3);    
        
            bars = vec3(0., 0., 0.);
            float t = mod(time * 0.1, 10.) + time;

            bar(-0.5+abs(sin(t*2.)),                  2.0, 0.0, 0.0);
            bar(-0.5+abs(sin(t*2.+barsangle/6.)),     2.0, 1.0, 0.0);
            bar(-0.5+abs(sin(t*2.+barsangle/6.*2.)),  1.0, 1.0, 1.0);
            bar(-0.5+abs(sin(t*2.+barsangle/6.*3.)),  0.0, 0.0, 1.0);
            bar(-0.5+abs(sin(t*2.+barsangle/6.*4.)),  0.0, 1.0, 1.0);
            bar(-0.5+abs(sin(t*2.+barsangle/6.*5.)),  1.0, 0.0, 1.0);
        }
        
        //Some Stars
        vec2 uv = ( gl_FragCoord.xy / resolution.xy )*SIZE;
        
        float stars = 0.;
        float fl, s;
        for (int layer = 1; layer < LAYERS; layer++)
        {
                fl = float(layer);
                s = (200.-fl*11.);
                stars += step(.1,pow(noise(mod(vec2(uv.x*s + time*SPEED*DIRECTION - fl*100.,uv.y*s),resolution.x)),18.)) * (fl/float(LAYERS));
        }
        
        //The Text
        vec2 _P_ = ( gl_FragCoord.xy / resolution.xy );
        vec2 q = _P_ - vec2(0.3,0.7);
        vec2 p = ( gl_FragCoord.xy /resolution.xy ) *SIZE * vec2(32,32);
        vec3 c = vec3(0);
        vec2 cpos = vec2(1,35);
        vec3 txColor = vec3(1);
        slashN
        S E A R C H I N G _ F O R _ DASH
        slashN
        L O A D I N G
        slashN
        
        float _S_= step(1.64,_P_.x);
        c -= _S_;
        
        {
            R E A D Y DOT
            slashN
        }
        
        //Mixing it all together
        color = vec3(stars)+(c)+(bars);
        gl_FragColor = vec4( color, 1.0 );
}
