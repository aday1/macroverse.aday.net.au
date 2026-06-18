/*{
    "DESCRIPTION": "FrostCrystal-DotMatrix-8",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "color"
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
        "color",
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

// 10 PRINT CHR$ (205.5 + RND (1)); : GOTO 10

// Shabby - the one line basic maze doin' the rounds in a frag shader, a very nice bit of creative coding 
// - Added border
// - scrolling
// - added some oldschool funk
void _userMain( void ) 
{
	vec3 colour = vec3(0.22,0.18,0.61);
	float xtime=mod(floor(time*110.0),5700.0);
	vec2 pos = ( gl_FragCoord.xy / resolution.xy );
	float san=cos((time+2.0)*0.75)*0.95;
	float can=sin((time+2.0)*0.75)*01.95;
	if (xtime>4000.0)
		{
		pos-=vec2(0.5,0.5);
		vec2 npos=vec2(pos.x*san+pos.y*can,pos.y*san-pos.x*can);
		pos=npos+vec2(0.5,0.5);
		}
		
	if (xtime>3000.0)
		pos=mod(pos,0.5)*2.0;
	if (xtime>2000.0)
		{
		pos-=vec2(0.5,0.5);
		vec2 npos=vec2(pos.x*san+pos.y*can,pos.y*san-pos.x*can);
		pos=npos+vec2(0.5,0.5);
		}
	vec2 bpos=0.5-abs(pos-0.5);
	pos=(vec2(pos.x,max(0.0,floor((xtime/60.0)-26.0)*0.0335)+1.0-(pos.y+0.015)))*vec2(3.0,1.5);
	float or=(pos.x*1.0/0.05)+(floor(pos.y*1.0/0.05)*60.0);
	pos=mod(pos,0.05);
	float r=(floor((sin(cos((floor(or)))*12.0)+1.0)));
	pos.x=((1.0-r)*-pos.x)+(r*pos.x);
	if (min(bpos.y,bpos.x)<0.084 ||(xtime>or && (abs(r*0.05-pos.x-pos.y)<0.01)))	colour=vec3(0.47,0.43,0.83);
	gl_FragColor = vec4( colour, 1.0 );

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