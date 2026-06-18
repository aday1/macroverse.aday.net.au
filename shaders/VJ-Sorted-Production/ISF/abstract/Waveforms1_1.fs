/*{
    "DESCRIPTION": "Waveforms1",
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
        "abstract"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// http://glslsandbox.com/e#29202.0
// SIGNALS

#ifdef GL_ES
precision mediump float;
#endif

//Signal modulation techniques
//Input signal (no modulation)
//AM modulated
//FM modulated
//Pulse width modulated

float carrier = 48.0;

float tau = atan(1.0)*2.0 * time;

vec2 scale = vec2(2, 8);

float distline(vec2 p0,vec2 p1,vec2 uv)
{
	vec2 dir = normalize(p1-p0);
	uv = (uv-p0) * mat2(dir.x,dir.y,-dir.y,dir.x);
	return distance(uv,clamp(uv,vec2(0),vec2(distance(p0,p1),0)));   
}

float signal(float x)
{
	return sin(tau*x*3.0 + time);
}

float fm(float carrier, float modulation, float x)
{
	float band = 2.0;
	return sin(tau * (x * carrier + modulation * band));
}

float am(float carrier, float modulation, float x)
{
	return sin(tau * x * carrier) * (modulation * 0.5 + 0.5);
}

float pwm(float carrier, float modulation, float x)
{
	return step(modulation * 0.5 + 0.5, mod(x * carrier, 1.0));
}

float f(float x, float mode)
{
	float sig = signal(x);
	
	if(mode == 3.0)
	{
		return sig;	
	}
	if(mode == 2.0)
	{
		return am(carrier, sig,x);
	}
	if(mode == 1.0)
	{
		return fm(carrier, sig,x);
	}
	if(mode == 0.0)
	{
		return pwm(carrier, sig,x);
	}
	return 0.0;
}

vec4 sample(vec4 sx, float mode)
{
	sx *= scale.x * 0.5;
	return vec4(f(sx.x, mode), f(sx.y, mode), f(sx.z, mode), f(sx.w, mode)) / scale.y * 0.5;
}

void _userMain(void) 
{
	float rep = 1.0 / (resolution.x / 2.0);
	vec2 aspect = resolution.xy / resolution.y;
	vec2 uv = ( gl_FragCoord.xy / resolution.y );
	
	float mode = floor(uv.y / 0.25);
	
	uv.y = mod(uv.y, 0.25);
	uv.y -= aspect.y/8.0;
	
	float dist = 1e6;
	
	vec2 ruv = vec2(mod(uv.x, rep), uv.y);
	
	vec4 offs = vec4(-1, 0, 1, 2);
	vec4 sx = (offs * rep);
	vec4 sy = sample((offs + floor(uv.x / rep)) * rep, mode);
	
	vec2 p0 = vec2(sx.x, sy.x);
	vec2 p1 = vec2(sx.y, sy.y);
	vec2 p2 = vec2(sx.z, sy.z);
	vec2 p3 = vec2(sx.w, sy.w);
	
	dist = min(dist, distline(p0, p1, ruv));
	dist = min(dist, distline(p1, p2, ruv));
	dist = min(dist, distline(p2, p3, ruv));
	
	float lw = 1.5 / resolution.y;
	
	float color = smoothstep(lw, 0.0, dist);
		
	gl_FragColor = vec4( vec3( 0, color, 0 ), 1.0 );

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