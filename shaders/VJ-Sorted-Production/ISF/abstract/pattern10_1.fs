/*{
    "DESCRIPTION": "pattern10",
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
        "color",
        "particles"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

// Posted by Trisomie21
// Switch MODE to test different configurations

#define DOTS	 	0
#define STARFIELD 	1
#define BLUBBER 	2
#define SNOW	 	3

#define MODE 		2

#if MODE==DOTS
const float LAYERS	= 5.0;
const float SPEED	= 0.001;
const float SCALE	= 100.0;
const float DENSITY	= 0.6;
const float SATURATION	= 0.9;
const float BRIGHTNESS	= 10.0;
const float TWIST	= 0.0;
       vec2 ORIGIN	= resolution.xy*.5;
const vec3  PALETTE	= vec3(0.25, 1.0, -0.5);
#endif

#if MODE==STARFIELD
const float LAYERS	= 6.0;
const float SPEED	= 0.0004;
const float SCALE	= 1000.0;
const float DENSITY	= 0.98;
const float SATURATION	= 2.0;
const float BRIGHTNESS	= 40.0;
const float TWIST	= 0.0;
       vec2 ORIGIN	= resolution.xy*.5;
const vec3  PALETTE	= vec3(1.0, 1.0, 1.0);
#endif

#if MODE==BLUBBER
const float LAYERS	= 3.0;
const float SPEED	= 0.002;
const float SCALE	= 20.0;
const float DENSITY	= 0.1;
const float SATURATION	= 1.5;
const float BRIGHTNESS	= 5.0;
const float TWIST	= 0.0;
       vec2 ORIGIN	= resolution.xy*.5;
const vec3  PALETTE	= vec3(0.25, -1.0, -0.5);
#endif

#if MODE==SNOW
const float LAYERS	= 10.0;
const float SPEED	= 0.0001;
const float SCALE	= 1000.0;
const float DENSITY	= 0.9;
const float SATURATION	= 1.0;
const float BRIGHTNESS	= 10.0;
const float TWIST	= 0.04;
       vec2 ORIGIN	= vec2(resolution.x*.5,resolution.y*1.5);
const vec3  PALETTE	= vec3(1.0, 1.0, 1.0);
#endif

float rand(vec2 co){ return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453); }

void _userMain( void ) {
	
	vec2   pos = gl_FragCoord.xy - ORIGIN;
	float dist = length(pos) / resolution.y;
	vec2 coord = vec2(pow(dist, 0.1), atan(pos.x, pos.y) / (3.1415926*2.0));
	
	vec3 color = vec3(0.0);
	for (float i = 0.0; i < LAYERS; ++i)
	{
		float t = i*10.0 + time*i*i;
		float r = coord.x - (t*SPEED);
		float c = fract(sin(time*.1+i)*TWIST + coord.y + i*.125);
		vec2  p = vec2(r, c*.5);
		vec2 uv = fract(p*SCALE);
		float a = 1.0-length(uv*2.0-1.0);
		vec3  m = fract(r*SCALE * PALETTE)*SATURATION+i*.2;
		float d = (rand(floor(p*SCALE))-DENSITY)*BRIGHTNESS;
		d = clamp(d*dist, 0.0, 1.0);
		color = max(color, a*m*d);
	}

	gl_FragColor =  vec4(color, max(color.r, max(color.g, color.b)));
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