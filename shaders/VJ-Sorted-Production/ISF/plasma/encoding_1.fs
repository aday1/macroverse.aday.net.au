/*{
    "DESCRIPTION": "encoding",
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
        "plasma",
        "texture-input"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D backbuffer;

// let's do hex --jz
// let's do alphanum 14 segment, pewh that was ugly (s(uv, seg=0) for testing all) --novalis
//    encoding: 0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F  G  H  I  J  K  L  M  N  O  P  Q  R  S  T  U  V  W  X  Y  Z
//              0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31 32 33 34 35
// nice :) let's throw in some pinball style gas-plasma coloring and slight blur. --jz
// cool! let's fix some bugs (no Z, sry for that; slightly adjusted segments)
//       + add mirror effect --novalis

float s(vec2 uv, int seg) {
	float f = 0.;
	if ((seg == 0 || seg ==  1) && uv.y >  abs(uv.x-.2941)+.7206 && uv.y > .9118 && uv.y < 1.) { f = 1.; }
	if ((seg == 0 || seg ==  2) && uv.y < -abs(uv.x-.2941)+.2794 && uv.y < .0882 && uv.y > 0.) { f = 1.; }
	if ((seg == 0 || seg ==  3) && uv.y >  uv.x+ .5147 && uv.y < -uv.x+.9853 && uv.x > .0 && uv.x < .0882) { f = 1.; }
	if ((seg == 0 || seg ==  4) && uv.y > -uv.x+1.1029 && uv.y <  uv.x+.3971 && uv.x > .5 && uv.x < .5882) { f = 1.; }
	if ((seg == 0 || seg ==  5) && uv.y >  uv.x+ .0147 && uv.y < -uv.x+.4853 && uv.x > .0 && uv.x < .0882) { f = 1.; }
	if ((seg == 0 || seg ==  6) && uv.y > -uv.x+ .6029 && uv.y <  uv.x-.1029 && uv.x > .5 && uv.x < .5882) { f = 1.; }
	if ((seg == 0 || seg ==  7) && uv.y < .8824 && uv.x > .2500 && uv.x < .3382 && uv.y >  abs(uv.x-.2941)+.5144) { f = 1.; }
	if ((seg == 0 || seg ==  8) && uv.y > .1176 && uv.x > .2500 && uv.x < .3382 && uv.y < -abs(uv.x-.2941)+.4846) { f = 1.; }
	if ((seg == 0 || seg ==  9) && uv.y > .4559 && uv.y < .5441 && uv.y > abs(uv.x-.1412)+.3700 && uv.y < -abs(uv.x-.1412)+.6300) { f = 1.; }
	if ((seg == 0 || seg == 10) && uv.y > .4559 && uv.y < .5441 && uv.y > abs(uv.x-.4471)+.3700 && uv.y < -abs(uv.x-.4471)+.6300) { f = 1.; }
	if ((seg == 0 || seg == 11) && uv.x > .1076 && uv.x < .2306 && uv.y > -uv.x*1.7067+.9500 && uv.y < -uv.x*1.7067+1.0824) { f = 1.; }
	if ((seg == 0 || seg == 12) && uv.x > .3576 && uv.x < .4806 && uv.y >  uv.x*1.7067-.0588 && uv.y <  uv.x*1.7067+ .0735) { f = 1.; }
	if ((seg == 0 || seg == 13) && uv.x > .1076 && uv.x < .2306 && uv.y >  uv.x*1.7067-.0852 && uv.y <  uv.x*1.7067+ .0471) { f = 1.; }
	if ((seg == 0 || seg == 14) && uv.x > .3576 && uv.x < .4806 && uv.y > -uv.x*1.7067+.9265 && uv.y < -uv.x*1.7067+1.0588) { f = 1.; }
	return f;
}

float alphanum(vec2 uv, int n) {
	//return s(uv, 0); //test
	n = int(mod(float(n), 36.));
	float seg = 0.;
	if (n!= 1 && n!= 4 && n!=17 && n!=19 && n!=20 && n!=21 && n!=22 && n!=23 && n!=30 && n!=31 && n!=32 && n!=33 && n!=34) { seg += s(uv, 1); }
	if (n!= 1 && n!= 4 && n!= 7 && n!=10 && n!=15 && n!=17 && n!=20 && n!=22 && n!=23 && n!=25 && n!=27 && n!=29 && n!=31 && n!=32 && n!=33 && n!=34) { seg += s(uv, 2); }
	if (n!= 1 && n!= 2 && n!= 3 && n!= 7 && n!=11 && n!=13 && n!=18 && n!=19 && n!=29 && n!=33 && n!=35) { seg += s(uv, 3); }
	if (n!= 5 && n!= 6 && n!=12 && n!=14 && n!=15 && n!=16 && n!=18 && n!=20 && n!=21 && n!=28 && n!=29 && n!=31 && n!=33 && n!=35) { seg += s(uv, 4); }
	if (n!= 1 && n!= 3 && n!= 4 && n!= 5 && n!= 7 && n!= 9 && n!=11 && n!=13 && n!=18 && n!=28 && n!=29 && n!=33 && n!=34 && n!=35) { seg += s(uv, 5); }
	if (n!= 2 && n!= 5 && n!=12 && n!=14 && n!=15 && n!=18 && n!=20 && n!=21 && n!=25 && n!=27 && n!=29 && n!=31 && n!=33 && n!=34 && n!=35) { seg += s(uv, 6); }
	if (n==11 || n==13 || n==18 || n==29) { seg += s(uv, 7); }
	if (n==11 || n==13 || n==18 || n==29 || n==34) { seg += s(uv, 8); }
	if (n== 2 || n== 3 || n== 4 || n== 5 || n== 6 || n== 8 || n== 9 || n==10 || n==14 || n==15 || n==17 || n==20 || n==25 || n==27 || n==28 || n==34) { seg += s(uv, 9); }
	if (n== 2 || n== 3 || n== 4 || n== 6 || n== 8 || n== 9 || n==10 || n==11 || n==14 || n==15 || n==16 || n==17 || n==25 || n==27 || n==28 || n==34) { seg += s(uv,10); }
	if (n==22 || n==23 || n==33) { seg += s(uv,11); }
	if (n== 0 || n==20 || n==22 || n==31 || n==33 || n==35) { seg += s(uv,12); }
	if (n== 0 || n==31 || n==32 || n==33 || n==35) { seg += s(uv,13); }
	if (n== 5 || n==20 || n==23 || n==26 || n==27 || n==32 || n==33) { seg += s(uv,14); }
	return seg;
}

// --------------------------------

void main(void) {
	vec2 pos = gl_FragCoord.xy / resolution;
	vec2 ps = 3.0/resolution;
	vec2 uv = gl_FragCoord.xy/min(resolution.x,resolution.y);
	
	vec2 uvs = uv*2.-vec2(0.1,.5);
	vec4 col = vec4(0.);
	vec4 mes = vec4(16,21,28,21); // "GLSL"
	int shift = int(mod(time, 9.));
	for (int j = 0; j < 2; j++) { // display and mirror
		for (int i = 0; i < 4; i++) {
			float m = j==0 ? 1.:-2.;
			col += alphanum((uvs-vec2(float(i)*.74+.35, .1-float(j)*ps.y*5.))*vec2(1.,m), int(mes[i])+shift)*abs(pow(m, -3.));
		}
		col = pow(col, vec4(vec3(1./2.2*float(j+1)),1.)); // gamma correction
	}
	
	col += texture2D(backbuffer, pos);
	col += texture2D(backbuffer, pos + vec2( ps.x,  0.0));
	col += texture2D(backbuffer, pos + vec2(-ps.x,  0.0));
	col += texture2D(backbuffer, pos + vec2(0.0,   ps.y));
	col += texture2D(backbuffer, pos + vec2(0.0,  -ps.y));
	col /= 8.5;
	col = clamp(col, 0.0, 1.0);

	gl_FragColor = shift==0 ? vec4(col.rgb * vec3(0.5, 1.25, 1.5), 1.):vec4(col.rgb * vec3(1.5, 1.25, 0.5), 1.);
}
