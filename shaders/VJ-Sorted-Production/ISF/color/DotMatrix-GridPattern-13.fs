/*{
    "DESCRIPTION": "DotMatrix-GridPattern-13",
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

vec4 colour(float c, float d)
{
	float f = d*255.0;
	c*=12.0;
	vec3 res = vec3(0.0,0.0,0.0);
	res += smoothstep(1.0,2.0,c) * vec3(0.0,3.0,310.0)/255.0;
	res += smoothstep(2.0,3.0,c) * vec3(f*0.5,f*1.5,f)/255.0;
	res += smoothstep(3.0,4.0,c) * vec3(0.0,-1.0,25.0)/255.0;
	res += smoothstep(4.0,5.0,c) * vec3(0.0,0.0,32.0)/255.0;
	res += smoothstep(5.0,6.0,c) * vec3(0.0,1.0,23.0)/255.0;
	res += smoothstep(6.0,7.0,c) * vec3(0.0,0.0,-30.0)/255.0;
	res += smoothstep(7.0,8.0,c) * vec3(0.0,0.0,-57.0)/255.0;
	res += smoothstep(8.0,9.0,c) * vec3(0.0,70.0,-15.0)/255.0;
	res += smoothstep(9.0,10.0,c) * vec3(0.0,100.0,50.0)/255.0;
	res += smoothstep(10.0,11.0,c) * vec3(0.0,71.0,58.0)/255.0;
	res += smoothstep(11.0,12.0,c) * vec3(0.0,10.0,64.0)/255.0;
	res += smoothstep(12.0,13.0,c) * vec3(0.0,1.0,33.0)/255.0;
	return vec4(res,1.0) * sqrt(d);
}
float periodic(float x,float period,float dutycycle)
{
	x/=period;
	x=abs(x-floor(x)-0.5)-dutycycle*0.5;
	return x*period;
}

float pcount(float x,float period)
{
	return floor(x/period);
}

float distfunc(vec3 pos)
{
	vec3 gridpos=pos-floor(pos)-0.5;
	float r=length(pos.xy);
	float a=atan(pos.y,pos.x);
	a+=time*0.3*sin(pcount(r,3.0)+1.0)*sin(pcount(pos.z,1.0)*13.73);
	return min(max(max(
		periodic(r,3.0,0.2),
		periodic(pos.z,1.0,0.7+0.3*cos(time/3.0))),
		periodic(a*r,3.141592*2.0/6.0*r,0.7+0.3*cos(time/3.0))),0.25);
}

void _userMain()
{
	float d = (sin((time - 5.0) / 3.0)*0.5+0.5);

	float mx = 0.5;
	float my = 0.5;
	
	vec2 coords=(2.0*gl_FragCoord.xy-resolution)/max(resolution.x,resolution.y);

	vec3 ray_dir=normalize(vec3(coords,1.0+0.0*sqrt(coords.x*coords.x+coords.y*coords.y)));

	vec3 ray_pos=vec3(32.0*pow(0.5-mx, 1.0),32.0*(0.5-my),time*5.0);
	float a=cos(time)*0.0*0.4;
	ray_dir=ray_dir*mat3(
		cos(a),0.0,sin(a),
		0.0,1.0,0.0,
		-sin(a),0.0,cos(a)
	);

	float i=64.0;
	for(int j=0;j<64;j++)
	{
		float dist=distfunc(ray_pos);
		ray_pos+=dist*ray_dir;

		if(abs(dist)<0.001) { i=float(j); break; }
	}

	float c=i/192.0;
	gl_FragColor=colour(c, d)*2.0;//+flareColor();
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