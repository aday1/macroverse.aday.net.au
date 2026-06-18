/*{
    "DESCRIPTION": "DotMatrix-Zooming-9",
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
precision highp float;
#endif

void _userMain( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.xy );

	float color = 0.0;	
	float vx, vy, vz, vxr;
	float dx, llx, dy, lly, dz, llz;
	int px, py, pz, ccc, P;
	float k, k2;
	ivec4 di;
	vec4 X;
	vec4 d=vec4(time,mouse.y*3.0,.0,.0);
	vx=(position.x-0.5)+0.0001;

	vy=(position.y-0.5)+0.0001;
	vz=0.5+.0001;
	vxr=(vx*cos(mouse.x*2.5)+vz*sin(mouse.x*2.5));
	vz=(-vx*sin(mouse.x*2.5)+vz*cos(mouse.x*2.5));
	vx=vxr;
	X=fract(d);
	dx = 1000.0/vx; dy = 1000.0/vy; dz = 1000.0/vz;
	px=1; llx=dx*(1.0-X[0]);
	py=16; lly=dy*(1.0-X[1]);
	pz=256; llz=dz*(1.0-X[2]);
	if (dx<.0) {px=-1; dx=-dx; llx=dx*X[0];}
	if (dy<.0) {py=-16; dy=-dy; lly=dy*X[1];}
	if (dz<.0) {pz=-256; dz=-dz; llz=dz*X[2];}
	ccc=0;
	di=ivec4(d[0],d[1],d[2],d[3]);
	P=di[2]*256+di[1]*16+di[0];
	color=1.0;
	for (int i=0; i<40; i++)
	{
		if ((llx<=lly) && (llx<=llz))
		{
			P+=px; llx+=dx; k=0.75;
		}
		else
		{
			if (lly<=llz)
			{
				P+=py; lly+=dy; k=0.9;
			}
			else
			{
				P+=pz; llz+=dz; k=1.0;
			}
		}
		if ((fract(float(P)/29.0)<.01)&&(color==1.0)) {
			color=float(i)/40.0;
			k2=k;
		}
	}
	gl_FragColor = vec4(k2*(1.0-color),k2*(1.0-color),k2*k2*(1.0-color),1.0 );

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