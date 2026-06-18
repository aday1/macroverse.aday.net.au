/*{
    "DESCRIPTION": "Speedo1",
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

varying vec2 surfacePosition;

#define STEPSEC 0
#define HASCHRONO 0;
#define PI 3.14159265359
#define SCALE 1.75

const float radius   = 0.33;
const float radius_h = 0.29;
const float radius_m = 0.30;
const vec3 color1 = 0.8*vec3(0.2,0.9,0.7);
const vec3 color2 = vec3(1.1,0.05,0.05);

float d2y(float d){return 1./(0.2+d);}

vec2 p;
float a,r;

float angle(vec2 orig){
	vec2 t = p-orig;
	return 0.5-atan(t.x, -t.y)/(2.*PI);
}

float dtick(float r, float a, float n, float l, float f0, float f1){
	float h = f1*abs(fract(n*a+0.5)-0.5);
	float hi = f0*max(0., l-r);
	return 9.*length(vec2(h,hi));
}

float ticks(){
	float a = 0.5-atan(p.x, -p.y)/(2.*PI);
	
	float dh = dtick(r, a, 12., radius_h, 35., 6.);
	float dm = dtick(r, a, 60., radius_m, 200., 5.);
	
	return d2y(dh) + d2y(dm);
}

float circle(float R){
    float d=distance(r, R);
    return d2y(200.*d);
}

float hands(float e){
	float s = 16.0 * sin(0.3*time); // 0.00->0.99
	#if STEPSEC
	s=floor(s);
	#endif
	float ah = 60.*a;
	float dr = 0.15*r*min(min(abs(s-ah),abs(s-ah+60.)),abs(s-ah-60.))/(1.0 + 0.1 * sin(time*PI));
	float dl = 4.*max(0., r-0.27);
	float d = length(vec2(dr, dl));
	float x = 0.01;
	return d2y(e*d) * step(x,r) + circle(x);
}

float chrono(float e, vec2 orig, float k){
	float s = sin(k * time); // 0.00->0.99
	float r = distance(p,orig);
	float ah = k*angle(orig);
	float dr = 20.*r*min(min(abs(s-ah),abs(s-ah+k)),abs(s-ah-k));
	float dl = 60.*max(0., r-0.03);
	float d = length(vec2(dr, dl));
	return d2y(e*d);
}

void _userMain( void ) {
	p = SCALE*surfacePosition;
	r = length(p);
	a = angle(vec2(0.));
	float inCircle = step(r,radius);
	
	vec2 y = vec2(0.);
	y.x += circle(radius);
	y.x += ticks() * inCircle;
	y.x *= smoothstep(-0.3, 0.0, p.y);
	
	y.y += hands(50.);
	//y.y += chrono(12., vec2(0.1,-0.22),5.7); 
	//y.y += chrono(5., vec2(-0.1,-0.22), 15.0); 
	//y = pow(y, vec2(0.9));
	vec3 rgb = y.x*color1+y.y*color2;
	//rgb = 0.8*mix(rgb,rgb.gbr+rgb.brg,0.15);
	gl_FragColor = vec4(rgb, 1.0);
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