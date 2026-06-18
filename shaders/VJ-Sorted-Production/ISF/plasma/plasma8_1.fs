/*{
    "DESCRIPTION": "plasma8",
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
        }
    ],
    "TAGS": [
        "plasma"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

const float pi=3.1415926535;

vec2 clog(vec2 v)
{
	return vec2(0.5*log(v.x*v.x+v.y*v.y),atan(-v.y,v.x));
}

vec2 cdiv(vec2 a,vec2 b)
{
	return vec2(a.x*b.x+a.y*b.y,a.y*b.x-a.x*b.y)/(b.x*b.x+b.y*b.y);
}

vec4 checkerboard(vec2 pos)
{
	float a=(fract(pos.x)-0.5)*(fract(pos.y)-0.5);
	if(a>0.0) return vec4(0.7,0.72,0.7,1.0);
	else return vec4(0.3,0.33,0.3,1.0);
}

vec4 grid(vec2 pos,float thickness)
{
	vec2 a=abs(fract(pos)-0.5);
	if(a.x>thickness && a.y>thickness) return vec4(0.7,0.72,0.7,1.0);
	else return vec4(0.3,0.33,0.3,1.0);
}

vec4 wave(vec2 pos)
{
	float a=fract(pos.x+0.5*sin(pos.y*1.0*pi));
	if(a>0.5) return vec4(0.7,0.72,0.7,1.0);
	else return vec4(0.3,0.33,0.3,1.0);
}

vec4 rainbow(vec2 pos)
{
	return vec4(sin(pos.x*2.0*pi)+1.0,sin(pos.x*2.0*pi+pi*1.0/3.0)+1.0,sin(pos.x*2.0*pi+pi*2.0/3.0)+1.0,1.0)*(sin(pos.y*8.0*pi)+1.0)*0.7;
}

void _userMain()
{
	vec2 position=2.0*((2.0*gl_FragCoord.xy-resolution)/max(resolution.x,resolution.y));
	position *= mouse;

	const float p1=1.0;
	const float p2=1.0;
	float u_corner=2.0*pi*p2;
	float v_corner=log(256.0)*p1;
	float diag=sqrt(u_corner*u_corner+v_corner*v_corner);
	float sin_a=v_corner/diag;
	float cos_a=u_corner/diag;
	float scale=diag/2.0/pi;

	float offset=0.7;

	vec2 p=clog(position+vec2(offset,0))
		-clog(position+vec2(-offset,0));

	vec2 rotated=vec2(p.x*cos_a-p.y*sin_a,
		          p.x*sin_a+p.y*cos_a);
	vec2 scaled=rotated*scale/vec2(log(256.0),2.0*pi);
//	vec2 scaled=p/vec2(log(256.0),2.0*pi);
//	vec2 translated=scaled-vec2(0.35+time*0.1,0.0);
	vec2 translated=scaled-vec2(time*0.1);

//	gl_FragColor=checkerboard(translated*2.0);
//	gl_FragColor=wave(translated*8.0+0.3);
	gl_FragColor=rainbow(translated);
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