uniform bool invert;
uniform float hueShift;
uniform float brightness;
uniform float colorB;
uniform float colorG;
uniform float colorR;
uniform float contrast;
uniform float saturation;
uniform float speed;
uniform float zoom;
/*{
    "DESCRIPTION": "DotMatrix-TextGlyph-12",
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

#define M_2_PI 0.63661977236
#define M_PI2  1.57079632679

vec3 squereTexture(vec2 uv, vec3 firstColor, vec3 secondColor) { // Flag texture
	if(bool(mod(floor(20.0 * uv.x), 2.0)) ^^ bool(mod(floor(20.0 * uv.y), 2.0))) // I made a true table and this is the result
		return secondColor;
	else
		return firstColor;
}

void _userMain(){
	vec2 uv= gl_FragCoord.xy / resolution.xy; // Calculates UV screen coordinates
	vec2 mousePos = mouse;
	vec3 colorA = vec3(0.467, 0.161, 0.325);
	vec3 colorB = vec3(0.867, 0.282, 0.078);
	float radio = 0.2;
	
	uv.y *= resolution.y / resolution.x;
	mousePos.y *= resolution.y / resolution.x;
	
	if(length(uv - mousePos) <= radio) { // Fish eye effect!!
		float newLength;
		vec3 sphereNormal;
		vec3 rayDirection;
		float angle;
		
		uv -= mousePos;
		
		sphereNormal.x = uv.x;
		sphereNormal.y = uv.y;
		sphereNormal.z = sqrt(radio*radio - length(uv)*length(uv));
		
		newLength = radio * (1.0 - M_2_PI*acos(length(uv)/radio));
		uv /= length(uv);
		uv *= newLength;
		uv += mousePos;
		
		rayDirection = vec3(cos(time), sin(time), sin(time));
		
		angle = acos(((sphereNormal.x * rayDirection.x) + (sphereNormal.y * rayDirection.y) + (sphereNormal.z * rayDirection.z)) / (length(sphereNormal) * length(rayDirection)));
		
		if(angle <= M_PI2)
			gl_FragColor = vec4(squereTexture(uv, colorA, colorB), 1.0);
		else
			gl_FragColor = vec4(0.5 * squereTexture(uv, colorA, colorB), 1.0);
	}
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