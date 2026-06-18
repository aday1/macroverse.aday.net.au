/*{
    "DESCRIPTION": "StarField-Fire-DotMatrix",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "geometric"
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
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// ;-)
// sample code from shadertoy; 
// Modified by Gigatron Amiga Rules !   ---  Just need a Sinusscroll and a bounceing logo and you got a Amiga Cracktro
#ifdef GL_ES
precision mediump float;
#endif

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

#define PI 3.1415926536

 vec2 res = vec2(resolution.x,resolution.y);
const mat3 mRot = mat3(0.9553, -0.2955, 0.0, 0.2955, 0.9553, 0.0, 0.0, 0.0, 1.0);
const vec3 ro = vec3(0.0,0.0,-4.0);

const vec3 cRed = vec3(1.0,0.0,0.0);
const vec3 cWhite = vec3(1.0);
const vec3 cGrey = vec3(0.66);
const vec3 cPurple = vec3(0.51,0.29,0.51);

const float maxx = 0.378;
//                       _                                       _ _ _ _ _ _ _ 
//       /\             (_)                                     | | | | | | | |
//      /  \   _ __ ___  _  __ _  __ _  __ _  __ _  __ _  __ _  | | | | | | | |
//     / /\ \ | '_ ` _ \| |/ _` |/ _` |/ _` |/ _` |/ _` |/ _` | | | | | | | | |
//    / ____ \| | | | | | | (_| | (_| | (_| | (_| | (_| | (_| | |_|_|_|_|_|_|_|
//   /_/    \_\_| |_| |_|_|\__, |\__,_|\__,_|\__,_|\__,_|\__,_| (_|_|_|_|_|_|_)
//                          __/ |                                              
//                         |___/

//By @unitzeroone
//Check out http://www.youtube.com/watch?feature=player_detailpage&v=ZmIf-5MuQ7c#t=26s for context.
//Decyphering the code&magic numbers and optimizing is left as excercise to the reader ;-)

//-1/5/2013 FIX : Windows was rendering "inverted z checkerboard" on entire screen.
//-1/5/2013 CHANGE : Did a modification for the starting position, so ball doesn't start at bottom right.
//-1/5/2013 CHANGE : Tweaked edge bounce.

// Ok all partialy mixed <./>

void _userMain()
{
	gl_FragColor=vec4(0.);
	float x = gl_FragCoord.x;
	float y = gl_FragCoord.y;
    vec2 p = gl_FragCoord.xy / resolution.xy;
	vec2 c = p - vec2(0.25, 0.5);

    //Another amiga/atari copper fx 
    	
    float coppers = time*10.0;
    float rep = 8.;// try 8 16 32 64 128 256 ...
    vec3 col2 = vec3(0.5 + 0.5 * sin(x/rep + 3.14 + coppers), 0.5 + 0.5 * cos (x/rep + coppers), 0.5 + 0.5 * sin (x/rep + coppers));
    vec3 col3 = vec3(0.5 + 0.5 * sin(x/rep + 3.14 - coppers), 0.5 + 0.5 * cos (x/rep -coppers), 0.5 + 0.5 * sin (x/rep - coppers));
    vec3 col4 = vec3(0.5 + 0.5 * sin(y/rep + 3.14 + coppers), 0.5 + 0.5 * cos (y/rep + coppers), 0.5 + 0.5 * sin (y/rep + coppers));
    vec3 col5 = vec3(0.5 + 0.5 * sin(y/rep + 3.14 - coppers), 0.5 + 0.5 * cos (y/rep -coppers), 0.5 + 0.5 * sin (y/rep - coppers));

   	if ( p.y > 0.985 && p.y < 1.0 ) gl_FragColor = vec4 ( col3, 1.0 );
	   
   	if ( p.x > 0.990 && p.x < 1.0 ) gl_FragColor = vec4 ( col4, 1.0 );
		
	if ( p.y > 0.0 && p.y < .02)    gl_FragColor = vec4 ( col2, 1.0 );
	
	if ( p.x > 0.0 && p.x < .01 && p.y<.985) gl_FragColor = vec4 ( col5, 1.0 );

	{// stars forever
	vec2 uv = ( gl_FragCoord.xy / resolution.xy )*SIZE;
	
	float stars = 0.;
	float fl=0., s=0.;
	for (int layer = 0; layer < LAYERS; layer++) {
		fl = float(layer);
		s = (300.-fl*30.);
		stars += step(.1,pow(noise(mod(vec2(uv.x*s + time*SPEED*DIRECTION - fl*100.,uv.y*s),resolution.x)),18.)) * (fl/float(LAYERS));
	}
	gl_FragColor += vec4( vec3(stars), 1.0 );
	
	}

	float asp = resolution.y/resolution.x;
	vec2 uv = (gl_FragCoord.xy / resolution.xy);
	vec2 uvR = floor(uv*res*1.0);
	vec2 g = step(2.0,mod(uvR,16.0));
	vec3 bgcol=vec3(0.);// = mix(cPurple,mix(cPurple,cGrey,g.x),g.y);
	uv = uvR/res;
	float xt = mod(time+1.0,6.0);
	float dir = (step(xt,3.0)-.5)*-2.0;
	uv.x -= (maxx*2.0*dir)*mod(xt,3.0)/3.0+(-maxx*dir);
	uv.y -= abs(sin(4.5+time*1.3))*0.5-0.3;
	bgcol = mix(bgcol,bgcol-vec3(0.2),1.0-step(0.12,length(vec2(uv.x,uv.y*asp)-vec2(0.57,0.29))));
	vec3 rd = normalize(vec3((uv*2.0-1.0)*vec2(1.0,asp),1.5));
	float b = dot(rd,ro);
	float t1 = b*b-15.6;
        float t = -b-sqrt(t1);
	vec3 nor = normalize(ro+rd*t)*mRot;
	vec2 tuv = floor(vec2(atan(nor.x,nor.z)/PI+((floor((time*-dir)*60.0)/60.0)*0.5),acos(nor.y)/PI)*8.0);
	gl_FragColor += vec4(mix(bgcol,mix(cRed,cWhite,clamp(mod(tuv.x+tuv.y,2.0),0.0,1.0)),1.0-step(t1,0.0)),1.0);

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