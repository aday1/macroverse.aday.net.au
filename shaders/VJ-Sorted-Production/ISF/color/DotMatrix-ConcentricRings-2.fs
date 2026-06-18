/*{
    "DESCRIPTION": "DotMatrix-ConcentricRings-2",
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

// quadratic bezier curve evaluation
// From "Random-Access Rendering of General Vector Graphics"
// posted by Trisomie21
// @mod* by rotwang

float det(vec2 a, vec2 b) { return a.x*b.y-b.x*a.y; }

vec2 get_distance_vector(vec2 b0, vec2 b1, vec2 b2)
{
	float a=det(b0,b2), b=2.0*det(b1,b0), d=2.0*det(b2,b1);
	float f=b*d-a*a;
	vec2 d21=b2-b1, d10=b1-b0, d20=b2-b0;
	vec2 gf=2.0*(b*d21+d*d10+a*d20);
	gf=vec2(gf.y,-gf.x);
	vec2 pp=-f*gf/dot(gf,gf);
	vec2 d0p=b0-pp;
	float ap=det(d0p,d20), bp=2.0*det(d10,d0p);
	float t=clamp((ap+bp)/(2.0*a+b+d), 0.0,1.0);
	return mix(mix(b0,b1,t),mix(b1,b2,t),t);
}

vec3 clr_a( vec2 pos)
{
	float d = length(pos) - 8.0;
	d = clamp(1.0-d, 0.0, 1.0);
	return vec3(d,d*0.6,d*0.2);
}

vec3 clr_b( vec2 pos)
{
	float d = length(pos) - 8.0;
	d = clamp(1.0-d, 0.0, 1.0);
	return vec3(d*0.2,d*0.6,d);
}

vec3 clr_c( vec2 pos)
{
	float d = length(pos)-1.0;
	d = clamp(1.0-d, 0.0, 1.0);
	return vec3(d,d,d);
}

void _userMain(void)
{
	vec2 pos = gl_FragCoord.xy;
	vec2 p[3];
	float s = 1.0;//resolution.x/1920.0;
	
	p[0] = vec2(resolution.x*.30,resolution.y*.4);
	p[1] = mouse*resolution;
	p[2] = vec2(resolution.x*.60,resolution.y*.6);

	// make the bez pass thru the control point (THX)
	p[1]+=(p[1]-((p[0]+p[2])*0.5));
	
	vec2 xy = get_distance_vector(p[0]-pos, p[1]-pos, p[2]-pos);
	
	vec3 ca = clr_a(xy);
	vec3 cb = clr_b(p[0] -pos);
	vec3 cc = clr_b(p[2] -pos);
	vec3 cd = clr_c(xy);
	vec3 clr = mix(ca+cb+cc,cd, 0.5);
	gl_FragColor = vec4(clr, 1.0);

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