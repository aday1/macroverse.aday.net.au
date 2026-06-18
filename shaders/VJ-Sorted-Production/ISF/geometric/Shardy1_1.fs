/*{
    "DESCRIPTION": "Shardy1",
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
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

bool RayTriangleIntersection(out float t, vec3 v0, vec3 edge1, vec3 edge2, vec3 rayOrigin, vec3 rayDir)		 
{  
	vec3 tvec = rayOrigin - v0;  
	vec3 pvec = cross(rayDir, edge2);  
	float  det  = dot(edge1, pvec);  

	det = 1.0 / det;
	float u = dot(tvec, pvec) * det;  
	vec3 qvec = cross(tvec, edge1); 
	float v = dot(rayDir, qvec) * det;  
	
	t = dot(edge2, qvec) * det;  
	
	return u >= 0.0 && u <= 1.0 && v >= 0.0 && (u+v) <= 1.0 && t >= 0.0;
}

void raytrace(out float t, vec3 o, vec3 d) {
	for(int i = 0; i < 128; i++) {
		float temp;
		float seed = float(i);
		float seed2 = time + float(i / 2) * 5.0;
		float seed3 = time + float(i / 4) * 3.0;
		float scale = float(i);
		vec3 bary = vec3( sin(seed2 * 0.2), -cos(seed3 * 0.3), -sin(seed3 * 0.4)) * 5.0;
		vec3 p0 = vec3( sin(seed), cos(seed), -sin(seed2));
		vec3 p1 = vec3( cos(seed2),-cos(seed),  sin(seed));
		vec3 p2 = vec3( -sin(seed3),-cos(seed2), -sin(seed));
		if(RayTriangleIntersection(temp, p0 + bary, p1 + bary, p2 + bary, o, d)) {
			t = min(t, temp);
		}
	}
}

void _userMain( void ) {

	vec2 uv = ( gl_FragCoord.xy / resolution.xy ) * 2.0  - 1.0;

	vec3 dir = normalize(vec3(uv, 1.0));
	vec3 pos = vec3(sin(time), 2, cos(time) - 15.0);
	float t = 10000.0;
	raytrace(t, pos, dir);
	gl_FragColor = 1.0 - vec4(t * 0.01);
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