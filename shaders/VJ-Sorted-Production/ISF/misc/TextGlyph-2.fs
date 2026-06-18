/*{
    "DESCRIPTION": "TextGlyph-2",
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

// from ST ; Mr rigster ... gtr for glslsandbox boolean vector 
int bvec[65];

bool insideText(in vec2 uv)
{
    float softIdx = uv.x * 13.0 - 0.5 + floor(uv.y * 8.0 - 2.0) * 13.0;
    bool ret = false;
        
	for (int i = 0; i < 65; i++)
    {
		if (softIdx >= float(i) && softIdx < float(i + 1))
        {
			ret = bvec[i] > 0;
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

}

void main()
{
	initText();
    
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    
    vec2 tuv = uv + vec2(0.03 + sin((1.0-uv.x) * uv.y * 10.0 + time) * 0.03, 0.03 + cos(uv.y * uv.x * 8.0 + time) * 0.03);
    
    if(insideText(tuv))
    {
        uv = vec2(1., 1.) - uv;
        gl_FragColor = vec4(fract(tuv.y*8.0)*0.5+0.5, fract(tuv.y*2.0)*0.2, fract(tuv.y*2.0)*0.8, 1.0);
    }
    else
    {
        //fragColor = vec4(0.0, 0.0, 0.0, 1.0);
        gl_FragColor = vec4(uv-0.8,0.1+0.1*sin(time),1.0);
    }
	
}


