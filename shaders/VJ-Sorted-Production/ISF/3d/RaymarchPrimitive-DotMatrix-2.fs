/*{
    "DESCRIPTION": "RaymarchPrimitive-DotMatrix-2",
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
        "color",
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

float sdSphere(vec3 p, float r){
	p.y += 4./(p.y +0.)-4.6;
	if(p.y<-1.) r = 100.;
	
	return length(p)-r;
}
float udRoundBox( vec3 p, vec3 b, float r ) {
	return length(max(abs(p)-b,0.0))-r;
}

float worldD(vec3 p){
	float d = sdSphere(p-vec3(0.0, -3.2, -5.0), 0.5);
	float d1 = udRoundBox(p-vec3(0.0, -0.8, -5.0), vec3(2.0, 0.001, 2.0), 0.05);
	return min(d,d1);
}

vec3 worldN(vec3 p, float coc) {
	vec3 e = vec3(coc, 0.0, 0.0);
	return normalize(
		vec3(
			worldD(p+e.xyy)-worldD(p-e.xyy),
			worldD(p+e.yxy)-worldD(p-e.yxy),
			worldD(p+e.yyx)-worldD(p-e.yyx)
			)
		);
}

void _userMain( void ) {

	float bgcolor = 0.2 - 0.1*cos(0.1*time);
	float color = bgcolor;
	float shadow = 1.;
	vec3 p1, p2;
	float d, td;
	float coc = 1./length(resolution);
	
	// camera setup
	vec3 view;
	view.xy = (-1.+2.*gl_FragCoord.xy/resolution)*vec2(resolution.x/resolution.y,1.);
	view.z = -2.1;
	
	// camera ray direction
	vec3 rd = normalize(view);
	// light direction
	vec3 ld = normalize(vec3(-1.,-3.,-1.))*vec3(sin(time),2.0,cos(time));
	
	td = worldD(vec3(0.0));
	// ray march to world
	for(int i=0; i<128; i++) {
		p1 = rd*td;
		d = worldD(p1);
		
		if(d<coc) {
			// hit the world, copute shadow
			td = coc;
			for(int j=0; j<128; j++) {
				p2 = p1-ld*td;
				d = worldD(p2)+0.2;
				
				if(d<coc) {
					shadow = 0.;
					break;
				}
				td += d;
				shadow = min(shadow,2.*d/td);
				if(td>20.) break;
			}
			// grab color
			color = clamp(dot(-ld*shadow,worldN(p1,coc)),0.0,1.0);
			break;
		}
		td += d;
		if(td>20.) break;
	}
	
	float cor = (1.+bgcolor);
	color = (color+bgcolor)/cor;
	gl_FragColor = vec4( vec3(color), 1.0 );

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