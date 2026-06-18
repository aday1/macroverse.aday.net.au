/*{
    "DESCRIPTION": "DotMatrix-CircuitMatrix-5",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "noise"
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
        "geometric",
        "noise"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

// by TOUNOUSSI YOUSSEF
// i found noise and hash fonctions in a stack overflow answer
// and i tried to modify it to get this pixel effect
// but the fractional brownian motion function was written by me
// enjoy my pixel world radar :D ^^

// important for animations

// [[cos(theta),-sin(theta)]
//  [sin(theta),sin(theta)]]
// a rotation matrix mixed with the time value
// to get rotation during the animation time
mat2 rotation_mat = mat2(cos(time/5.0),-sin(time/5.0),sin(time/5.0),cos(time/5.0));

float hash(vec2 n){
	float dot_prod = n.x*127.1 + n.y*311.7;
	return fract(sin(dot_prod)*43758.9876);
}

float noise(vec2 intervale){
	vec2 i = floor(intervale);
	vec2 f = fract(intervale);
	vec2 u = f*f*(1.0-2.0*f);
	
	return mix(mix(hash(i+vec2(0.0,0.0)),
		       hash(i+vec2(1.0,.0)),u.x),
		   mix(hash(i+vec2(5.0,1.0)),
		       hash(i+vec2(5.0,1.0)),u.x),
		   u.y);
}

//fractional brownian motion function
float fbm(vec2 p){
	float f = 0.0;
	float octave = 0.5;
	float sum = 0.0;
	
	for(int i=0;i<5;i++){
		sum += octave;
		f += octave*noise(p);
		p *= 2.01;
		octave /= 2.0;
	}
	
	f /= sum;
	
	return f;
}

void _userMain( void ) {
	vec2 pos = gl_FragCoord.xy/resolution.xy*2.0-1.0;// pixels positions
	pos.x *= resolution.x/resolution.y;// decressing the aspect ratio of the resolution
	
	float effect = fbm(3.0*pos*rotation_mat);// our fractional brownian motion effect
	vec3 color = vec3(effect*tan(-3.0*time/3.0+pos.x),effect+sin(time),effect+sin(time));// preparing the color of pixels
	
	gl_FragColor = vec4(color,1.0);
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