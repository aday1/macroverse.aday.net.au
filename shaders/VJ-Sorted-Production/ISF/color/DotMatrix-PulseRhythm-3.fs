/*{
    "DESCRIPTION": "DotMatrix-PulseRhythm-3",
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
        "color"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// forked from nimitz's "Neon parallax" https://www.shadertoy.com/view/XssXz4

#ifdef GL_ES
precision mediump float;
#endif

float pulse(float cn, float wi, float x)
{
	return 1.-smoothstep(0., wi, abs(x-cn));
}

float hash11(float n)
{
    return fract(sin(n)*43758.5453);
}

vec2 hash22(vec2 p)
{
    p = vec2( dot(p,vec2(127.1, 311.7)), dot(p,vec2(269.5, 183.3)));
	return fract(sin(p)*43758.5453);
}

vec2 field(in vec2 p)
{
	vec2 n = floor(p);
	vec2 f = fract(p);
	vec2 m = vec2(1.);
	vec2 o = hash22(n)*0.17;
	vec2 r = f+o-0.5;
	float d = abs(r.x) + abs(r.y);
	if(d<m.x)
    {
		m.x = d;
		m.y = hash11(dot(n,vec2(1., 2.)));
	}
	return vec2(m.x,m.y);
}

void _userMain(void)
{
	vec2 uv = gl_FragCoord.xy / resolution.xy-0.5;
	uv.x *= resolution.x/resolution.y*0.9;
	uv *= 4.;
	
	vec2 p = uv*.01;
	p *= 1./(p-1.);
	
	//global movement
	uv.y += time*1.2;
	uv.x += sin(time*0.3)*0.8;
	vec2 buv = uv;
	
	float rz = 0.;
	vec3 col = vec3(0.);
	for(float i=1.; i<=26.; i++)
	{
		vec2 rn = field(uv);
		uv -= p*(i-25.)*0.2;
		rn.x = pulse(0.35,.02, rn.x+rn.y*.15);
		col += rn.x*vec3(sin(rn.y*10.), cos(rn.y)*0.2,sin(rn.y)*0.5);
	}
	
	//animated grid
	buv*= mat2(0.707,-0.707,0.707,0.707);
	float rz2 = .4*(sin(buv*10.+1.).x*40.-39.5)*(sin(uv.x*10.)*0.5+0.5);
	vec3 col2 = vec3(0.2,0.4,2.)*rz2*(sin(2.+time*2.1+(uv.y*2.+uv.x*10.))*0.5+0.5);
	float rz3 = .3*(sin(buv*10.+4.).y*40.-39.5)*(sin(uv.x*10.)*0.5+0.5);
	vec3 col3 = vec3(1.9,0.4,2.)*rz3*(sin(time*4.-(uv.y*10.+uv.x*2.))*0.5+0.5);
	
	col = max(max(col,col2),col3);
	
	gl_FragColor = vec4(col,1.0);
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