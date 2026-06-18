/*{
    "DESCRIPTION": "StarField-Rainbow-DotMatrix-2",
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
        "particles"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

// Amiga raster bounce fx;
// from World of wonders intro 1996 !!!
// music Phalanx2 bss	Beathoven Synthesizer
//A music format created by Thomas Lopatic (Dr.Nobody/HQC) 1987, often used in Rainbow Arts and Tristar productions from 1987-88.

float rand (in vec2 uv) { return fract(sin(dot(uv,vec2(12.4124,48.4124)))*48512.41241); }
const vec2 O = vec2(0.,1.);
float noise (in vec2 uv) {
	vec2 b = floor(uv);
	return mix(mix(rand(b),rand(b+O.yx),.5),mix(rand(b+O),rand(b+O.yy),.5),.5);
}

#define DIR_RIGHT -1.
#define DIR_LEFT 1.
#define DIRECTION DIR_LEFT

#define LAYERS 8
#define SPEED 50.
#define SIZE 5.

int bvec[65];

bool insideText(in vec2 uv)
{
    float softIdx = uv.x * 13.0 - 0.5 + floor(uv.y * 8.0 - 2.4) * 13.0;
    bool ret = false;
        
	for (int i = 0; i < 65; i++)
    {
		if (softIdx >= float(i) && softIdx < float(i + 1))
        {
			ret = bvec[i] > 0; //ret 1
        }
	}
	return ret;
}

void initText()
{
    bvec[0] = 0;  // L5
    bvec[1] = 1;
    bvec[2] = 1;
    bvec[3] = 1;
    bvec[4] = 0;
    bvec[5] = 0;
    bvec[6] = 1;
    bvec[7] = 0; 
    bvec[8] = 0;
    bvec[9] = 1;
    bvec[10] = 0;
    bvec[11] = 1;
    bvec[12] = 0;

    bvec[13] = 0;// L4
    bvec[14] = 1;
	bvec[15] = 0;
	bvec[16] = 1;
    bvec[17] = 0;
    bvec[18] = 0;
    bvec[19] = 1;
    bvec[20] = 0; 
    bvec[21] = 0;
    bvec[22] = 1;
    bvec[23] = 0;
    bvec[24] = 1;
  	bvec[25] = 0;            
				 
    bvec[26] = 0;// L3
    bvec[27] = 1;
    bvec[28] = 0;
    bvec[29] = 1;
    bvec[30] = 0;
    bvec[31] = 0;
    bvec[32] = 1;
    bvec[33] = 0; 
    bvec[34] = 0;
    bvec[35] = 1;
    bvec[36] = 1;
    bvec[37] = 0;
  	bvec[38] = 0;

    bvec[39] = 0; // L2
    bvec[40] = 1;
    bvec[41] = 0;
    bvec[42] = 0;
    bvec[43] = 0;
    bvec[44] = 0;
    bvec[45] = 1;
    bvec[46] = 0; 
    bvec[47] = 0;
    bvec[48] = 1;
    bvec[49] = 0;
    bvec[50] = 1;
  	bvec[51] = 0;

    bvec[52] = 0;  // L1
    bvec[53] = 1;
    bvec[54] = 1;
    bvec[55] = 1;
    bvec[56] = 0;
    bvec[57] = 1;
    bvec[58] = 1;
    bvec[59] = 1; 
    bvec[60] = 0;
    bvec[61] = 1;
    bvec[62] = 1;
    bvec[63] = 1;
    bvec[64] = 0;
				 // .xxx.xxx.xxx.   L1
				 // .x....x..x.x.
 				 // .x.x..x..xx.. 
				 // .x.x..x..x.x.			
                                 // .xxx..x..x.x.   L5
    
}

#define goTYPE vec2 p = ( gl_FragCoord.xy /resolution.xy ) * vec2(64,32);vec3 c = vec3(0);vec2 cpos = vec2(-3.+3.*sin(t*1.2),3.+10.*abs(sin(t*2.)));vec3 txColor = vec3(1);
#define goPRINT gl_FragColor += vec4(c, 1.0);
#define slashN cpos = vec2(1,cpos.y-6.);
#define inBLK txColor = vec3(0);
#define inWHT txColor = vec3(1);
#define inRED txColor = vec3(1,0,0);
#define inYEL txColor = vec3(1,1,0);
#define inGRN txColor = vec3(0,1,0);
#define inCYA txColor = vec3(0,1,1);
#define inBLU txColor = vec3(0,0,1);
#define inPUR txColor = vec3(1,0,1);
#define inPCH txColor = vec3(1,0.7,0.6);
#define inPNK txColor = vec3(1,0.7,1);
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
#define n1 c += txColor*Sprite3x5(11410.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
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
#define EQUAL c += txColor*Sprite3x5(3640.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define HEART c += txColor*Sprite3x5(3024.,floor(p-cpos));cpos.x += 4.;if(cpos.x > 61.) slashN
#define getBit(num,bit) float(mod(floor(floor(num)/pow(2.,floor(bit))),2.) == 1.0)
#define Sprite3x5(sprite,p) getBit(sprite,(2.0 - p.x) + 3.0 * p.y) * float(all(lessThan(p,vec2(3,5))) && all(greaterThanEqual(p,vec2(0,0))))

float barsize = 0.08;
float cr=0.9; // color reduction ;
vec2 position=vec2(0); 
vec3 color=vec3(0);
float gtimer=0.0;

    float r=1.0;
    float g=0.0;
    float b=0.0;

vec3 mixcol(float value, float r, float g, float b)
{
	return vec3(value * r, value * g, value * b);
}

void bar(float pos, float r, float g, float b)
{
	 if ((position.y <= pos + barsize) && (position.y >= pos - barsize))
		color = mixcol(1.0 - abs(pos - position.y) / barsize, r, g, b);
}

float checkers(vec2 q)
{
    return mod(floor(q.x) + floor(q.y), 2.0);
}

void _userMain()
{
	    
    vec2 q = gl_FragCoord.xy / resolution.xy;
    position = ( gl_FragCoord.xy / resolution.xy );
	position = position * vec2(2.0) - vec2(1.0); 	
	
	float t = time;
    gtimer +=1.0;
   
    if(gtimer>=mod(1.+t,2.0))  r=0.0;g=1.0;b=1.0;

    float ps=0.5;
    float bf=20.0;

    for(float i=0.0;i<0.9;i+=0.1){
     cr-=0.1;
     bar(ps*abs(sin(t*3.+bf/6.*i)),r-cr,g-cr,b-cr);
        
    //bar(-0.1-p*abs(sin(t*3.+bf/6.*i)),r-cr,g-cr,b-cr);    
    }

    gl_FragColor = vec4(vec3(color),1.0);
    
    //***** continue to add 2 rasters like amiga  ...
    
    float x=gl_FragCoord.x;
    float coppers = -t*20.0;
    float rep = 64.;// try 8 16 32 64 128 256 ...
    vec3 col2 = vec3(0.5 + 0.5 * sin(x/rep + 3.14 + coppers), 0.5 + 0.5 * cos (x/rep + coppers), 0.5 + 0.5 * sin (x/rep + coppers));
    vec3 col3 = vec3(0.5 + 0.5 * sin(x/rep + 3.14 - coppers), 0.5 + 0.5 * cos (x/rep - coppers), 0.5 + 0.5 * sin (x/rep - coppers));	
    if ( q.y > 0.95 && q.y<0.956) gl_FragColor = vec4 (vec3(col2), 1.0 ); // mac 
	if ( q.y > 0.05 && q.y<0.056) gl_FragColor = vec4 (vec3(col3), 1.0 );
    //****** End rasters *********
    
    // will continue until ...the limit of human brain ! !
    // btw Happy new year for all sceners and shadertoy users ... Gtr
    goTYPE ;
    {inWHT}_ _ A M I G A _ _ F O R E V E R
	slashN ; 
 
    txColor = vec3(1.0-fract(t*1.33));
	//BLOCK slashN ;
	
	goPRINT ;
	
initText();
	vec2 uv=q;
	vec2 tuv = uv + vec2(0.03 + sin((1.0-uv.x) * uv.y * 10.0 + time) * 0.03, 0.03 + cos(uv.y * uv.x * 8.0 + time) * 0.03);
    
    if(insideText(tuv))
    {
         
        gl_FragColor += vec4(vec3(tuv+0.2,1.),1.0);
	 
    }

	vec2 cs = q - vec2(0.25, 0.5);

    //Another amiga/atari copper fx 

	{// stars forever
	vec2 uv = ( gl_FragCoord.xy / resolution.xy )*SIZE;
	
	float stars = 0.;
	float fl, s;
	for (int layer = 0; layer < LAYERS; layer++) {
		fl = float(layer);
		s = (300.-fl*30.);
		stars += step(.1,pow(noise(mod(vec2(uv.x*s + time*SPEED*DIRECTION - fl*100.,uv.y*s),resolution.x)),18.)) * (fl/float(LAYERS));
	}
	gl_FragColor += vec4( vec3(stars), 1.0 );
	
	}	

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