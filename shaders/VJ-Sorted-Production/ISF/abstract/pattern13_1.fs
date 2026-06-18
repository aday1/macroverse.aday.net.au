/*{
    "DESCRIPTION": "pattern13",
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
        "abstract",
        "color"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
//MG
#ifdef GL_ES
precision mediump float;
#endif
#define BLU_SPEED	0.5
#define RED_SPEED	0.9
#define GRE_SPEED	0.2

vec2 sincostime( vec2 p ){
	p.x+=sin(p.x*2.0+time)*0.4-cos(p.y*1.0-time)*0.5-sin(p.x*3.0+time)*0.3+cos(p.y*3.0-time)*0.1;
	p.y+=sin(p.x*5.0+time)*0.7+cos(p.y*8.0-time)*0.3+sin(p.x*4.0+time)*0.5-cos(p.y*6.0-time)*0.3;
	return p;
}

void _userMain() {
	vec2 pos=(gl_FragCoord.xy/resolution.y);
	pos.x-=resolution.x/resolution.y/2.0;pos.y-=0.5;
	pos=sincostime(pos) / 6.0;
	vec2 mse;
	mse.x+=(mouse.x-0.5)*resolution.x/resolution.y;
        mse.y+=mouse.y-0.5;
	
	float fx=sin(pos.x*10.0+time*GRE_SPEED+mse.x*GRE_SPEED*2.0)/3.0;
	float dist=abs(pos.y-fx)*50.0*(1.+mse.y);
	gl_FragColor+=vec4(0.5/dist,1.0/dist,0.5/dist,1./dist);
	
	fx=cos(pos.x*20.0+time*RED_SPEED+mse.x*RED_SPEED*2.0)/4.0;
	dist=abs(pos.y-fx)*50.0*(1.+mse.y);
	gl_FragColor+=vec4(1.0/dist,0.5/dist,0.5/dist,1./dist);
	
	fx=cos(pos.x*5.0+time*BLU_SPEED+mse.x*BLU_SPEED*2.0)/2.0;
	dist=abs(pos.y-fx)*50.0*(1.+mse.y);
	gl_FragColor+=vec4(0.5/dist,0.5/dist,1.0/dist,1./dist);
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