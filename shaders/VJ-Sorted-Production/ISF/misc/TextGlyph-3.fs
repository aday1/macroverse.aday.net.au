/*{
    "DESCRIPTION": "TextGlyph-3",
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
    float softIdx = uv.x * 12.0 + 6.0 + floor(uv.y * 32.0+abs(sin(time))*0.4 ) * 13.0;
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
    
    vec2 uv = ( gl_FragCoord.xy - resolution.xy*.5 ) / resolution.x;
    
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

	// 256 angle steps
        float angle = atan(uv.y,uv.x)/(1.*3.14159265359);
        angle -= floor(angle);
        float rad = length(uv);
        
        float color = 0.0;
        for (int i = 0; i < 5; i++) {
            float angleFract = fract(angle*36.);
            float angleRnd = floor(angle*360.)+1.;
            float angleRnd1 = fract(angleRnd*fract(angleRnd*.7235)*45.1);
            float angleRnd2 = fract(angleRnd*fract(angleRnd*.82657)*13.724);
            float t = time+angleRnd1*100.;
            float radDist = sqrt(angleRnd2+float(i));
            
            float adist = radDist/rad*.1;
            float dist = (t*.2+adist);
            dist = abs(fract(dist)-.5);
            color += max(0.,.5-dist*(mouse.y*10.)/adist)*(.5-abs(angleFract-.5))*5./adist/radDist;
            
            angle = fract(angle);
        }

	if(insideText(tuv))
    {
        uv = vec2(1., 2.2) - uv;
        gl_FragColor = vec4(fract(tuv.y*8.0)*0.5+0.5, fract(tuv.y*8.0)*0.7, fract(tuv.y*8.0)*0.7, 1.0);
    }
    else
    {
        //fragColor = vec4(0.0, 0.0, 0.0, 1.0);
        gl_FragColor = vec4(uv-0.8,0.1+0.1*sin(time),1.0);
	gl_FragColor +=vec4( color )*.3;    
    }

}


