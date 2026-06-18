/*{
    "DESCRIPTION": "FlowerThing",
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
        "geometric"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// "Peacock Feather" by Martijn Steinrucken aka BigWings - 2015
// countfrolic@gmail.com
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.

#ifdef GL_ES
precision mediump float;
#endif

#define NUMSTRANDS 150.
#define PI 3.1415
#define MOD3 vec3(.1031,.11369,.13787)

//----------------------------------------------------------------------------------------
//  1 out, 1 in...
float hash11(float p)
{
    // From Dave Hoskins
	vec3 p3  = fract(vec3(p) * MOD3);
    p3 += dot(p3, p3.yzx + 19.19);
    return fract((p3.x + p3.y) * p3.z);
}

float point(vec2 uv, vec2 p, float intensity) {
	uv -=p;
    return intensity/dot(uv,uv);
    //return distance(uv, p)*intensity;
}

float bandstep(float v, float c, float w) {
	float e1 = c-w;
    float e2 = c+w;
    float e = .01;
    
    float o = smoothstep(e1, e1+e, v); 
    o *= smoothstep(e2+e, e2, v);
    
    return o;
}

float line( in vec2 p, in vec2 a, in vec2 b ) {
    // From IQ
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp( dot(pa,ba)/dot(ba,ba), 0.0, 1.0 );
    float d = length( pa - ba*h );
    
    return d;
}

float circ(vec2 uv, vec2 p, float sag) {
	uv -= p;
    uv.x *= 1.+uv.y*sag;
    return length(uv);
}

float circmask(vec2 uv, vec2 p, float sag, float start, float finish) {
	float c = circ(uv, p, sag);
    
    return smoothstep(start, finish, c);
}

void _userMain( void )
{
	float t= time;
    
    vec2 uv = ( gl_FragCoord.xy / resolution.xy );
	uv.x-=.5;
    uv.x *= resolution.x/resolution.y;
    
    uv*=3.;
    
    float wiggle = sin(uv.y+t)*.1; 
    uv.x += wiggle;
    
    vec2 m = mouse.xy/resolution.xy;
    
    vec2 up = vec2(1., 0.);
    vec2 p =  vec2(0., .0);
    
    vec2 v = p-uv;
    float len = length(v);
    float x = uv.y;
    x = x*(x-1.);
    float topStraight = 1.-smoothstep(1.75, 3., uv.y);
    float bend = 1.- x*(1.-len*.5)*topStraight;
    
    v /= pow(len,bend);
    
    //v.y+=l;
    
    float c = abs(dot(up, v));
    
    float id = c*PI*NUMSTRANDS;
    float featherLines = cos(id);
    id = floor(id/PI - PI*.5);
    float sN = hash11(id*sign(uv.x));
    
    float outline1 = smoothstep(1., .95, c);
    
    c= featherLines*outline1;
    
    float outline2 = circ(uv, vec2(0., 1.4), 0.5)*.7;
   
    c *= smoothstep(1.5, 0.5, outline2);
    float core = circ(uv, vec2(0., .95), 0.1)*.7;
    core = smoothstep(.9, 0.3, core)*outline1*.9;
    c = max(c, core);
    
    vec3 baseCol = vec3(.118, 1., .051);
    vec3 eyeCol = vec3(.024, .118, .878);
    vec3 eyeLightCol = vec3(.0, .89, .95);
    vec3 brownCol = vec3(.965, .459, .384);
    vec3 pupilCol = vec3(.1, .1, .12);
    
    vec3 col = baseCol;
    float brownMask =  circmask(uv, vec2(0., 1.2+sN*.05*uv.y), 0.4, .95, .85);
    col = mix(col, brownCol, clamp(0., 1., brownMask));
    
    float eyeMask = circmask(uv, vec2(0., 1.1+sN*.02), 0.4, .7, .65);
    float eyeMask2 =  circmask(uv, vec2(0., .4+sN*.03), -0.1, 1., .98);
    eyeMask *= eyeMask2;
    col = mix(col, eyeLightCol, eyeMask);
    
    eyeMask = circmask(uv, vec2(0., 1.1+sN*.02), 0.4, .69, .6);
    eyeMask2 = circmask(uv, vec2(0., .48+sN*.03), -0.1, .9, .88);
    eyeMask *= eyeMask2;
    col = mix(col, eyeCol, eyeMask);
    
    float cutMask = circ(uv*vec2(.75, 1.), vec2(0., .6), -0.6);
    cutMask += line(uv, vec2(0., 3.), vec2(0., .0));
    cutMask = smoothstep(.35, .25, cutMask);
    
    float pupilMask = circmask(uv, vec2(0., 1.1+sN*.05), 0.4, .5, .45);
    pupilMask *= circmask(uv, vec2(0., .55+sN*.03), -0.1, .8, .78);
    pupilMask *= 1.-cutMask;
    col = mix(col, pupilCol, clamp(pupilMask, 0., 1.));
    
    float strandFade = circmask(uv, vec2(0.), 0., 0., 1.5);
    col *= mix(1., sN+.25*strandFade, strandFade);
    
    col += clamp(sin(max(0.8, outline2+(sN+wiggle*10.)*.1)*15.), 0., 1.)*sN*strandFade;	// highlights
    
    vec4 fg = vec4(col*c, c);
    
    float bgFade = uv.y/3.;
    
    vec4 bg = mix(vec4(eyeCol*.5, 0.), vec4(0.), 1.-bgFade);
    gl_FragColor = mix(bg, fg, fg.a);
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