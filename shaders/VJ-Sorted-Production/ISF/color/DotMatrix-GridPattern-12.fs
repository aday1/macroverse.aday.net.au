/*{
    "DESCRIPTION": "DotMatrix-GridPattern-12",
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
#ifdef GL_ES
precision mediump float;
#endif
#define SIZE 3.7
#define SPEED .17

const float pi = 3.14159;

float rand(vec3 co){
    return 0.9+0.1*fract(sin(co.z+dot(co.xy, vec2(12.9898,78.233))) * 43758.5453);
    //return 1.;
}

vec3 rotate(vec3 v,vec2 r) 
{
	mat3 rxmat = mat3(1,   0    ,    0    ,
			  0,cos(r.y),-sin(r.y),
			  0,sin(r.y), cos(r.y));
	mat3 rymat = mat3(cos(r.x), 0,-sin(r.x),
			     0    , 1,    0    ,
			  sin(r.x), 0,cos(r.x));

	return (v*rxmat)*rymat;
	
}

vec3 norm(vec3 v)
{
	//box made of 6 planes
	float tp = dot(v,vec3(0,-1,0))*SIZE;
	float bt = dot(v,vec3(0,1,0))*SIZE;
	float lf = dot(v,vec3(1,0,0))*SIZE;
	float rt = dot(v,vec3(-1,0,0))*SIZE;
	float fr = dot(v,vec3(0,0,1))*SIZE;
	float bk = dot(v,vec3(0,0,-1))*SIZE;
	
	return v/min(min(min(min(min(tp,bt),lf),rt),fr),bk);
}

float grid(vec3 v)
{
	float g;
	
	g = (length(v));
	g = (1.0-(g*g)*1.97)*1.38*rand(v);
	return g*g-0.225;
}
void _userMain( void ) {
	vec2 res = vec2(resolution.x/resolution.y,1.0);
	vec2 p = ( gl_FragCoord.xy / resolution.y ) -(res/2.0);
	
	vec2 m = vec2(cos(time*SPEED), sin(time*SPEED));
	
	vec3 color = vec3(0.0);
	
	vec3 pos = norm(rotate(vec3(p,0.5),vec2(m)));
	
	float c = grid(pos);
	color = vec3(c*.91, c*1., c*1.);
	//color = vec3(c*(0.5+0.5*pos.x),c*(0.5+0.5*pos.y),c*(0.5+0.5*pos.z));
	
	// Using http://glsl.heroku.com/e#9001.0
	vec3 vignette = vec3(sin(time)*.25, 0.0, 1.0);
	float l = sin(gl_FragCoord.y*abs(cos(sin(time*SPEED))*1.01+.1205)+time*SPEED);
	vignette += vec3(l*abs(cos(time*37.1*gl_FragCoord.y)), l*abs(cos(time*SPEED)), l*abs(cos(time*51.71)));
	
	vec3 final_c = mix(color, vignette, 0.04);
	
	gl_FragColor = vec4( final_c , 1.0 );
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