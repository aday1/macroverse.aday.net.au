/*{
    "DESCRIPTION": "GlowBloom-FrostCrystal",
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

// Just a little faked perspective. ~Singularity

vec3 SUN_1 = vec3(1.,0.494,0.);
vec3 SUN_2 = vec3(0.753,0.749,0.678);
vec3 SUN_3 = vec3(1.,0.25,0.1);
vec3 SUN_4 = vec3(0.25,0.5,1.);
const float ssamount = 2.; //This is the supersampling factor. It renders the scene this many times squared to get rid of moire and general aliasing.
const float mblur = 1.; //Not really worth using. It renders everything this many times to provide motion blur.

float sigmoid(float x) {
	return 0.1/(1. + exp2(-x/4.)) + .9;
}
float sqr(float a) {
	return a*a;
}

void _userMain( void ) {
	vec3 color = vec3(0.);
	vec2 aspect = vec2(1.,resolution.y/resolution.x );
	for(float blur = 0.; blur < mblur; blur += 1.)
	{
	float timed = blur/mblur/4.+float(time)*5.;
	for(float x = 0.; x < ssamount; x+=1.)
	for(float y = 0.; y < ssamount; y+=1.)
	{
	vec2 position = gl_FragCoord.xy+vec2(x/ssamount,y/ssamount);
	position /= resolution;
	position -= 0.5;
	/*vec2 position2 = 0.5 + (position-0.5)/resolution*3.;
	float filter = sigmoid(pow(2.1,7.5)*(length((position/resolution-mouse + 0.5)*aspect) - 0.015))*0.5 +0.5;
	position -= (mouse-0.5)*resolution;
	position = mix(position, position2, filter) - 0.5;*/
	position.x+=1.;
	position.x*=2.;

	float angle = atan(position.y,position.x);
	float d = length(position);
	
	color += 0.1/length(vec2(.04,2.*position.y*sqr(position.x)-pow(sin(pow(position.x,3.)*2.5-timed),.5)))*SUN_1; // I'm sure there's an easier way to do this, this just happened to look nice and blurry.
	color += 0.1/length(vec2(.04,2.*position.y*sqr(position.x)+pow(sin(pow(position.x,3.)*2.5-timed),.5)))*SUN_1;
	//color += 0.1/length(vec2(.04,2.*position.y+cos(position.x*10.+timed*4.)))*SUN_2;
	//color += 0.1/length(vec2(.01,1.*position.y+sin(position.x*16.+timed)*sin(position.y*16.+time*position.x*position.y/240.)))*SUN_3;
	color += 0.1/length(vec2(.01,.5*position.y*sqr(position.x)+sin(pow(position.x,3.)*8.+sqr(position.x)*32.+timed)*sin(position.y*sqr(position.x)*16.+sin(timed/8.))))*SUN_3;
	color += 0.1/length(vec2(.01,2.*position.y*sqr(position.x)+sin(pow(position.x,3.)*2.+sqr(position.x)*8.+timed*4.)))*SUN_4;
	}}
	gl_FragColor = vec4(color/sqr(ssamount)/mblur, 1.0);
	//gl_FragColor = vec4(filter<1.);
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