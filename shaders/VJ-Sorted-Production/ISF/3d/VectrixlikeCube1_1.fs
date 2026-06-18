/*{
    "DESCRIPTION": "VectrixlikeCube1",
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
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

#define lineCap 1
#define lineWidth 1.

#define pos gl_FragCoord.xy

#define PI (atan(1.) * 4.)

void drawLine(vec2 p1, vec2 p2){
   	vec2 delta = p2 - p1;
	float len = length(delta);
	float dist = abs(delta.y * pos.x - delta.x * pos.y + p2.x * p1.y - p2.y * p1.x) / len;
	
	vec2 center = (p1 + p2) / 2.;
	vec2 perp2 = vec2(center.y - p1.y, p1.x - center.x) + center;
	
	float cDist = abs((perp2.y - center.y) * pos.x - (perp2.x - center.x) * pos.y + perp2.x * center.y - perp2.y * center.x) / len * 4.;
	
	if (cDist > len){
		if (lineCap == 1){
		    dist = min(length (p1 - pos), length (p2 - pos)) - lineWidth;
		}else{
		    dist = max(dist - lineWidth, cDist - len);
		}
	}else{
		dist -= lineWidth;
	}
	
	gl_FragColor = mix(gl_FragColor, vec4(1), 1. - clamp(dist, 0., 1.));
}

void drawLine(vec4 p1, vec4 p2){
	vec2 p21 = p1.xy / p1.w;
	vec2 p22 = p2.xy / p2.w;
	
	p21 = (p21 / 2. + .5) * resolution;
	p22 = (p22 / 2. + .5) * resolution;
	
	drawLine(p21, p22);
}

void _userMain( void ) {
	gl_FragColor = vec4(vec3(cos(PI)), 1);
	
	float fov = PI / 2.;
	float aspect = resolution.x / resolution.y;
	
	float f = 1. / tan(fov / 2.);
	
	mat4 mat = mat4(
		f / aspect, 0, 0, 0,
		0, f, 0, 0,
		0, 0, 0, -1,
		0, 0, 0, 0
	);
	
	mat *= mat4(
		1, 0, 0, 0,
		0, 1, 0, 0,
		0, 0, 1, 0,
		0, 0, -3, 1
	);
	
	mat *= mat4(
		cos(time), 0, sin(time), 0,
		0, 1, 0, 0,
		-sin(time), 0, cos(time), 0,
		0, 0, 0, 1
	);
	
	float size1 = sin(time);
	float size2 = cos(time);
	
	drawLine(mat * vec4(size1, size1, size1, 1), mat * vec4(size1, -size1, size1, 1));
	drawLine(mat * vec4(-size1, -size1, size1, 1), mat * vec4(size1, -size1, size1, 1));
	drawLine(mat * vec4(-size1, -size1, size1, 1), mat * vec4(-size1, size1, size1, 1));
	drawLine(mat * vec4(size1, size1, size1, 1), mat * vec4(-size1, size1, size1, 1));
	
	drawLine(mat * vec4(size1, size1, -size1, 1), mat * vec4(size1, -size1, -size1, 1));
	drawLine(mat * vec4(-size1, -size1, -size1, 1), mat * vec4(size1, -size1, -size1, 1));
	drawLine(mat * vec4(-size1, -size1, -size1, 1), mat * vec4(-size1, size1, -size1, 1));
	drawLine(mat * vec4(size1, size1, -size1, 1), mat * vec4(-size1, size1, -size1, 1));
	
	drawLine(mat * vec4(size1, size1, size1, 1), mat * vec4(size1, size1, -size1, 1));
	drawLine(mat * vec4(size1, -size1, size1, 1), mat * vec4(size1, -size1, -size1, 1));
	drawLine(mat * vec4(-size1, size1, size1, 1), mat * vec4(-size1, size1, -size1, 1));
	drawLine(mat * vec4(-size1, -size1, size1, 1), mat * vec4(-size1, -size1, -size1, 1));
	
	drawLine(mat * vec4(size2, size2, size2, 1), mat * vec4(size2, -size2, size2, 1));
	drawLine(mat * vec4(-size2, -size2, size2, 1), mat * vec4(size2, -size2, size2, 1));
	drawLine(mat * vec4(-size2, -size2, size2, 1), mat * vec4(-size2, size2, size2, 1));
	drawLine(mat * vec4(size2, size2, size2, 1), mat * vec4(-size2, size2, size2, 1));
	
	drawLine(mat * vec4(size2, size2, -size2, 1), mat * vec4(size2, -size2, -size2, 1));
	drawLine(mat * vec4(-size2, -size2, -size2, 1), mat * vec4(size2, -size2, -size2, 1));
	drawLine(mat * vec4(-size2, -size2, -size2, 1), mat * vec4(-size2, size2, -size2, 1));
	drawLine(mat * vec4(size2, size2, -size2, 1), mat * vec4(-size2, size2, -size2, 1));
	
	drawLine(mat * vec4(size2, size2, size2, 1), mat * vec4(size2, size2, -size2, 1));
	drawLine(mat * vec4(size2, -size2, size2, 1), mat * vec4(size2, -size2, -size2, 1));
	drawLine(mat * vec4(-size2, size2, size2, 1), mat * vec4(-size2, size2, -size2, 1));
	drawLine(mat * vec4(-size2, -size2, size2, 1), mat * vec4(-size2, -size2, -size2, 1));
	
	drawLine(mat * vec4(size2, size2, size2, 1), mat * vec4(size1, size1, size1, 1));
	drawLine(mat * vec4(-size2, size2, size2, 1), mat * vec4(-size1, size1, size1, 1));
	drawLine(mat * vec4(-size2, -size2, size2, 1), mat * vec4(-size1, -size1, size1, 1));
	drawLine(mat * vec4(size2, -size2, size2, 1), mat * vec4(size1, -size1, size1, 1));
	
	drawLine(mat * vec4(size2, size2, -size2, 1), mat * vec4(size1, size1, -size1, 1));
	drawLine(mat * vec4(-size2, size2, -size2, 1), mat * vec4(-size1, size1, -size1, 1));
	drawLine(mat * vec4(-size2, -size2, -size2, 1), mat * vec4(-size1, -size1, -size1, 1));
	drawLine(mat * vec4(size2, -size2, -size2, 1), mat * vec4(size1, -size1, -size1, 1));
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