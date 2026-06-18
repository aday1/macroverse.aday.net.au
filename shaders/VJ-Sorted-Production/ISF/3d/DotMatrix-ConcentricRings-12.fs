/*{
    "DESCRIPTION": "DotMatrix-ConcentricRings-12",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "3d"
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
        "geometric",
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// figuring out how to write a raytracer
// @psovodomrd

#ifdef GL_ES
precision mediump float;
#endif

#define INF 1000.

float
box(vec3 p, vec3 d, vec3 bp, vec3 bd, out vec3 n)
{
	vec3 a = -(p - bp + bd)/d;
	vec3 b = -(p - bp - bd)/d;
	float k = INF;

	n = vec3(0);
	if(a.x > 0. && a.x < k){ k = a.x; n = vec3(1,0,0); }
	if(b.x > 0. && b.x < k){ k = b.x; n = vec3(-1,0,0); }
	if(a.y > 0. && a.y < k){ k = a.y; n = vec3(0,1,0); }
	if(b.y > 0. && b.y < k){ k = b.y; n = vec3(0,-1,0); }
	if(a.z > 0. && a.z < k){ k = a.z; n = vec3(0,0,1); }
	if(b.z > 0. && b.z < k){ k = b.z; n = vec3(0,0,-1); }
	return k;
}

int
intersect(vec3 ro, vec3 rd, out vec3 outn, out float outk)
{
	int obj = 0;
	outk = INF;
	outn = vec3(1);
	
	// intersection with plane
	vec3 tmpn;
	float k = box(ro, rd, vec3(0), vec3(5), tmpn);
	if(k < outk){
		obj = 1;
		outk = k;
		outn = tmpn;
	}	

	// intersection with sphere
	vec3 so = vec3(0,2.+sin(time*1.2),0);
	float sr = 1.;
	vec3 sro = ro - so;
	float a = dot(rd, rd);
	float b = 2.*dot(sro, rd);
	float c = dot(sro, sro) - sr*sr;	
	float D = b*b - 4.*a*c;
	if(D > 0.){
		float k = (-b - sqrt(D))/(2.*a);
		if(k > 0. && k < outk){
			obj = 2;
			outk = k;
			outn = normalize(sro + k*rd);
		}
	}
	
	return obj;
}

vec3
checker(float x, float y, float z, vec3 c1, vec3 c2)
{
	if(mod(100.+floor(x+.001) + floor(y+.001) + floor(z+.001), 2.) < 1.)
		return c1;
	return c2;
}

vec3
light(vec3 co, vec3 xo, vec3 xn, int obj, vec3 c)
{
	vec3 lo = vec3(sin(time)*2., 2.4, cos(time)*2.);
	vec3 xld = normalize(lo - xo);
	vec3 xcd = normalize(co - xo); 
	vec3 unus;
	float k;
	float spec, amb, diff;
	
	amb = .1;
	spec = 0.;
	diff = 0.;
	
	intersect(xo+.01*xn, xld, unus, k);
	float d = length(lo - xo);
	if(k >= d){
		diff = clamp(dot(xld, xn), 0., 1.)/(.5 + .2*d + .2*d*d);
		if(obj == 2){			
			if(dot(xcd, reflect(-xld, xn)) > cos(6.2831/360.*10.)){
			//	return vec3(1,0,0);
			}
			//spec = pow(clamp(dot(co, reflect(xld, xn)), 0., 1.), 1.);
			spec = pow(clamp(dot(-xcd, reflect(xld, xn)), 0., 1.), 32.);
		}
	}
	
	c = spec + c*(amb + diff);
	return c;
}

void _userMain(void)
{
	vec2 p = (2.*gl_FragCoord.xy - resolution.xy) / resolution.y;
	vec3 xc;
	float c;
	vec3 xn, xo;
	float k;
	float l;
	
	vec3 ro = vec3(0,2,-3);
	vec3 rd = normalize(vec3(p.xy, 1));
	int obj = intersect(ro, rd, xn, k);
	
	//vec3 t1, t2;
	//obj = intersect(ro + k*rd + xn*.1, vec3(0,1,0), t1, k);
	
	xo = ro + k*rd;
	if(obj == 2){
		//vec3 xs = normalize(xo - vec3(0,1,0));
		vec3 xs = xn;
		float a = atan(xs.z, xs.x)/6.2831;
		float b = atan(sqrt(dot(xs.xz, xs.xz)), xs.y)/6.2831;
		//xc = checker(a*30., b*30., 0., vec3(.5,1,1), vec3(.8,1,1));
		xc = vec3(1,.7,.7);
	}else if(obj == 1){
		xc = checker(xo.x, xo.y, xo.z, vec3(.1,.1,.1), vec3(.7,.7,.7));
	}else{
		xc = vec3(1,1,0);
	}
	xc = light(ro, xo, xn, obj, xc);

	gl_FragColor = vec4(xc,1.);
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