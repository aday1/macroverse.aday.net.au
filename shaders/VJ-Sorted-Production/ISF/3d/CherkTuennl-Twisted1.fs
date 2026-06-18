/*{
    "DESCRIPTION": "CherkTuennl-Twisted1",
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
        "tunnel",
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// http://glslsandbox.com/e#19844.0
// Checkertunnel 2

// Relief tunnel clone by Omar El Sayyed. The original effect is by Inigo Quilez and you can find it here:
//
// http://www.iquilezles.org/apps/shadertoy/index2.html
// You can find it under Plane deformations->Relief tunnel.
//
// For more information about smoothstep (Hermite interpolation),
// https://www.opengl.org/sdk/docs/man/html/smoothstep.xhtml
// 
// And watch:
// https://www.youtube.com/watch?v=Or19ilef4wE
// From 5:45 to 6:18 he discusses the smoothstep function. Don't watch more than this or you may be confused!
//
// Join us on our quest for learning shaders: 
// http://www.facebook.com/groups/graphics.shaders/
//
// And please like our page :P
// http://www.facebook.com/nomonesoftware

#ifdef GL_ES
precision highp float;
#endif

#define PI 3.1415927

vec4 checkerBoardTexture(vec2 position) {    
	position = fract(position);
	if (position.x < 0.5) {
		if (position.y < 0.5) {
			return vec4(1.0, 1.0, 1.0, 1.0);
		} else {
			return vec4(0.2, 0.2, 0.2, 1.0);
		}  
	} else {
		if (position.y < 0.5) {
			return vec4(0.2, 0.2, 0.2, 1.0);
		} else {
			return vec4(1.0, 1.0, 1.0, 1.0);
		}  
	}
}

void _userMain(void) {
	
	// Get point position in normalized coordinates,
	vec3 pointPosition = vec3(((gl_FragCoord.xy / resolution) * 2.0) - 1.0, 0.0);
	pointPosition.x *= resolution.x / resolution.y;

	// Move the camera around a bit,
	pointPosition.x += 0.7*sin(time * 0.456);
	pointPosition.y += 0.7*sin(time * 0.546);
		
	// Get point distance from the center of the screen (which is
	// directly proportional to the depth of the point in the tunnel),
	float radius = length(pointPosition);

	// Get point angle relative to screen center,
	float angle = atan(pointPosition.y, pointPosition.x);
	
	// Twist the angle a bit with time,
	angle += 0.35*sin(0.5*radius + 0.5*time);
	
	// Add fake relief,
	// Modify radius to mimic changes in tunnel radius based on the
	// angle,
	float displacement = 0.5 + 0.5*sin(7.0*angle);
	
	// Flatten the displacement a bit,
	float flatDisplacement = displacement;
	flatDisplacement = smoothstep(0.0, 1.0, flatDisplacement);
	flatDisplacement = smoothstep(0.0, 1.0, flatDisplacement);
	flatDisplacement = smoothstep(0.0, 1.0, flatDisplacement);
	
	// Get the deformed texture color,
	vec2 textureCoords;
	
	// The u is function in depth and time,
	textureCoords.x = radius + 0.2*flatDisplacement;
	
	// Add perspective (the pattern repeats quicker further),
	textureCoords.x = 2.0 / textureCoords.x;
	
	// Move the stripes with time,
	textureCoords.x += 2.0*time;
	
	// The v is function in angle,
	textureCoords.y = 7.0*angle/(2.0*PI);

	vec4 textureColor = checkerBoardTexture(textureCoords);

	// Add fog,
	// Make the displaced parts darker,
	float fog = 0.5 + 0.5*flatDisplacement;

	// Fade out into the tunnel, directly proportional to distance squared,
	fog *= radius * radius;
	
	// Add ambient occlusion (make the corners a bit darker),
	// Create a fine strip of smoothed values at the corners,
	float outerBounds = smoothstep(0.0, 0.4, displacement);
	float innerBounds = smoothstep(0.4, 0.7, displacement);
	float smoothCorners = outerBounds - innerBounds;
	
	// Make the effect less powerful as the tunnel goes deeper,
	smoothCorners *= radius;
	
	// Make these corners dark instead of bright,
	float ambientOcclusion = 1.0 - (0.5*smoothCorners);
	
	// Set the final fragment color,
	gl_FragColor = vec4(textureColor*fog*ambientOcclusion);
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