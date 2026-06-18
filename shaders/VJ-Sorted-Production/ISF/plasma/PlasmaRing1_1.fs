/*{
    "DESCRIPTION": "PlasmaRing1",
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
        },
        {
            "NAME": "u_mouse",
            "TYPE": "vec2",
            "LABEL": "U Mouse"
        }
    ],
    "TAGS": [
        "plasma"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// Shader Remix by Anoki
// Mouse Movement Crazyness
// Original shaders from
// PlayingMarble.glsl
// original code from https://www.shadertoy.com/view/MtX3Ws
// simplified edit: Robert 25.11.2015

// see also https://www.shadertoy.com/view/Mlj3zWprecision mediump float;
// modified color calculation by I.G.P.

#ifdef GL_ES
precision mediump float;
#endif

uniform vec2 u_mouse;

vec3 roty(vec3 p,float a)
{ return p*mat3(cos(a),0,-sin(a),0,1,0,sin(a),0,cos(a)); }

float map(in vec3 p) 
{
	float res=0.;vec3 c = p;
	for (int i = 0; i < 5; i++) 
	{
		p =0.7*abs(p)/dot(p,p) -.7;
		p.yz= vec2(p.y*p.y-p.z*(sin(p.x)/0.2),2.*p.y*p.z);
		res += exp(-10. * abs(dot(p,c)));
	}
	return res/5.0;
}

vec3 raymarch(vec3 ro, vec3 rd)
{
	float t = 5.0;
	vec3 col=vec3(0);float c=0.;
	for( int i=0; i<64; i++ )
	{
		t += 0.02*exp(-2.0*c);
		c = map(ro+t*rd);               
		col = col + 0.08*vec3(c*c, c, c*c*c);  //green	
		col = col + vec3(c*c*c, c*c, c);  //blue
		col = col + vec3(c, c*c*c, c*c);  //red

	}
	return col;
}

void _userMain()
{
    vec2 p = (gl_FragCoord.xy-resolution/2.0)/(resolution.y);
    vec3 ro = roty(vec3(3.),time*0.3);
    vec3 uu = normalize( cross(ro,vec3(1.0, 1.0, 1.0) ) );
    vec3 vv = normalize( cross(uu,ro));
    vec3 rd = normalize( p.x*uu + p.y*vv -ro*0.3 );
    gl_FragColor.rgb = 0.5*log(1.0+raymarch(ro,rd));
    gl_FragColor.a = 1.0;
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