/*{
    "DESCRIPTION": "RaymarchPrimitive-DotMatrix-9",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "geometric"
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
// by Karolius

#ifdef GL_ES
precision mediump float;
#endif

const vec3 c0 = vec3(1.20,0.60,1.00);
const vec3 c1 = vec3(0.0,0.60,0.20);

float hash(float x)
{
	return fract(sin(x) * 43758.5453);
}

vec2 hash2(vec2 v)
{
	return vec2(hash(v.x), hash(v.y));	
}

// combine with tv effect of http://glsl.heroku.com/e#9472.1
vec4 tv(vec4 col, vec2 pos)
{	
	float speed = 0.0;
	
	// vibrating rgb-separated scanlines
	col.r += sin(( pos.y + 0.001 + sin(time * 64.0) * 0.00012 ) * resolution.y * 2.0 + time * speed);
	col.g += sin(( pos.y + 0.003 - sin(time * 70.0) * 0.00015 ) * resolution.y * 2.0 + time * speed);
	col.b += sin(( pos.y + 0.006 + sin(time * 90.0) * 0.00017 ) * resolution.y * 2.0 + time * speed);
	col += 1.0;
	col *= 0.5;
	
	//col = max(vec4(0.1), col);
	
	// grain
	float grain = hash( ( pos.x + hash(pos.y) ) * time ) * 0.15;
	col += grain;
		
	// flickering
	//float flicker = hash(time * 64.0) * 0.05;
	//col += flicker;
	
	// vignette
	vec2 t = 2.0 * ( pos - vec2( 0.5 ) );
	
	t *= t;
	
	float d = 1.0 - clamp( length( t ), 0.0, 1.0 );
	
	col *= d;
	
	return col;
}

float sdCapsule(vec3 p, vec3 a, vec3 b, float r)
{
    vec3 ab = b - a;
    float t = dot(p - a, ab) / dot(ab, ab);
    t = clamp(t, 0.0, 1.0);
    return length((a + t * ab) - p) - r;
}

float flare(float e, float i, float s) { return exp(1.-(e*i))*s; }

void _userMain( void ) 
{
    vec2 unipos = (gl_FragCoord.xy / resolution);
    vec2 pos = unipos*2.0-1.0;
    pos.x *= resolution.x / resolution.y;

    vec2 t = mix(vec2(0.5),vec2(1.0),vec2(sin(time),cos(time)));

    float d0  = sdCapsule(vec3(pos.xy,0.),vec3(-.8,-.3,.0), vec3(-.8,.3,.0),.2);
    vec3 clr1 = c0 * flare(d0,6.3,.8) + flare(d0,3.3*(1.-t.x),0.08); 

    float d1  = sdCapsule(vec3(pos.xy,0.),vec3(-.8,mix(-.3,.3,t.x*.84),0.0), vec3(0.8,mix(0.20,-0.3,t.y),0.0), 0.12); 
    vec3 clr2 = mix(c0,c1,unipos.x)  * (flare(d1,7.3,.8) + flare(d1,4.3*t.x,0.09)) * .75; 

    float d2  = sdCapsule(vec3(pos.xy,0.),vec3(0.8,-0.3,0.0), vec3(0.8,0.3,0.0),.2);
    vec3 clr3 = c1 * flare(d2,6.3,0.8) + flare(d2,3.7,0.08);  

    vec4 col = vec4(clr1+clr2+clr3,5.);
    col = tv(col, unipos);
	
    gl_FragColor = col;
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