/*{
    "DESCRIPTION": "GlowBloom-Emerald-FrostCrystal-2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "tunnel"
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
        "tunnel",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
////////////////////////////////////////////////////
// IMPORTANT NOTICE                                //
// Thist demo is interactive.                      //             
// At first, enter mouse cursor to below palette, //
// then move mouse cursor for drawing.            //
// Author : PumpkinHead                           //
///////////////////////////////////////////////////

#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D backbuffer;

vec2 redRegion = vec2(-1.0, -0.5);
vec2 greenRegion = vec2(-0.5, 0.0);
vec2 blueRegion = vec2(0.0, 0.5);
vec2 blackRegion = vec2(0.5, 1.0);
float colorPreservePixelSize = 2.0;

vec2 ScreenToCentroid(vec2 coord)
{
	vec2 normalizedCoord = (coord / resolution.xy);
	vec2 centroidCoord = normalizedCoord - vec2(0.5);
	return centroidCoord * 2.0;
}

vec2 CorrectAspect(vec2 coord)
{
	float aspect = resolution.y  /resolution.x;
	return vec2(coord.x / aspect, coord.y);
}

vec4 DecidePenColor(vec2 norMousePos)
{
	// if mouse enter each palette region, return region color
	if (-1.0 < norMousePos.y && norMousePos.y < -0.8) {
		if (redRegion.x < norMousePos.x && norMousePos.x < redRegion.y) {
			return vec4(1.0, 0.0, 0.0, 1.0);
		}else if (greenRegion.x < norMousePos.x && norMousePos.x < greenRegion.y) {
			return vec4(0.0, 1.0, 0.0, 1.0);
		}else if (blueRegion.x < norMousePos.x && norMousePos.x < blueRegion.y) {
			return vec4(0.0, 0.0, 1.0, 1.0);
		}else if (blackRegion.x < norMousePos.x && norMousePos.x < blackRegion.y) {
			return vec4(0.0, 0.0, 0.0, 0.0); // for after use, alpha = 0.0
		}
	}
	
	// if mouse not in palette region, return previous palette color in left corner area(very small !!)
	vec2 texelSize = 1.0/resolution;	
	return texture2D(backbuffer, vec2(texelSize.x*colorPreservePixelSize, texelSize.y*colorPreservePixelSize));
}

void _userMain(void)
{
	vec2 norPos = ScreenToCentroid(gl_FragCoord.xy);
	vec2 norMousePos = (mouse - 0.5)*2.0;
	
	// bad coordinate treat
	if (0.0 <= gl_FragCoord.x && gl_FragCoord.x <= colorPreservePixelSize &&
	    0.0 <= gl_FragCoord.y && gl_FragCoord.y <= colorPreservePixelSize) {
		// left corner pixel has pen color
		gl_FragColor = DecidePenColor((mouse-0.5)*2.0);
		return;
	}else if (norPos.y < -0.8) {
		// below area render color palette
		if (redRegion.x < norPos.x && norPos.x < redRegion.y) {
			gl_FragColor = vec4(1.0, 0.0, 0.0, 1.0);
		}else if (greenRegion.x < norPos.x && norPos.x < greenRegion.y) {
			gl_FragColor = vec4(0.0, 1.0, 0.0, 1.0);
		}else if (blueRegion.x < norPos.x && norPos.x < blueRegion.y) {
			gl_FragColor = vec4(0.0, 0.0, 1.0, 1.0);
		}else if (blackRegion.x < norPos.x && norPos.x < blackRegion.y) {
			gl_FragColor = vec4(0.0, 0.0, 0.0, 1.0);
		}		
	}else{
		// other area pencil drawing using current pen color using left corner color

		// get current pen color from left corner area
		vec2 texelSize = 1.0/resolution;
		vec4 penColor = texture2D(backbuffer, vec2(texelSize.x * colorPreservePixelSize / 2.0,
							   texelSize.y * colorPreservePixelSize / 2.0));

		// Calculate aspect corrected, and centroid coordinate for later use.
		norPos = CorrectAspect(norPos);
		norMousePos = CorrectAspect(norMousePos);

		// get distance from mouse positon
		vec2 d = norPos - norMousePos;
		float distance = sqrt(d.x*d.x + d.y*d.y);

		// for blur-like effect, get previous color
		vec4 prevFragColor = texture2D(backbuffer, (gl_FragCoord.xy/resolution.xy));
		
		float radius = 0.15;
		float effect1 = clamp(0.3 * sin(time) + 0.7, 0.0, 1.0);
		float effect2 = clamp(0.5 * sin(time) + 0.5, 0.0, 1.0);
		float e = -1.0 / radius * (distance - radius);
		e = pow(e, 5.0); // for radius region more small, and not-linear effect
		if (distance <= radius) {
			// in pen radius
			if (penColor.a > 0.5) {
				// if use Red, Green, Blue color, add previous color and pen color 
				gl_FragColor = prevFragColor + effect1 * penColor * e;
			}else{
				// if use Black, subtract color from previous
				gl_FragColor = prevFragColor - effect1 * vec4(1.0, 1.0, 1.0, 1.0) * e;
			}
		}else{
			// not in radius, remain previous color
			gl_FragColor = prevFragColor;
		}
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