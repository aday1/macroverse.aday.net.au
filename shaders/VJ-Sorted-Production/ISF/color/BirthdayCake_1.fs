/*{
    "DESCRIPTION": "BirthdayCake",
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
// HAPPY BIRTHDAY TO YOU :)
// co3moz (Dogan Derya)
// https://gist.github.com/co3moz/4abd3c0576100fff321f

#define CAKE_LENGTH 10.
#define CAKE_WIDTH 10.
#define CAKE_COLOR 0, i / CAKE_LENGTH, 2
#define CANDLE_LENGTH 9.
#define CANDLE_COUNT 9.
#define CANDLE_COLOR z / CANDLE_LENGTH * c(2., 2.), 0, 0
#define FIRE_LENGTH 6.
#define FIRE_COLOR 1. * z / FIRE_LENGTH, .15 + 1. * z / FIRE_LENGTH, .5 + (FIRE_LENGTH - z - 10.) / FIRE_LENGTH

#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable
#define rad .0174532925
#define s(i, a) i + sin(time) / a
#define c(i, a) i + cos(time) / a

void _userMain(void) {
	vec2 position = (gl_FragCoord.xy / resolution.xy);
	vec3 color = vec3(sin(time + gl_FragCoord.x / 5.0), 1.0 - sin(gl_FragCoord.x / 5.0 - time), cos(gl_FragCoord.x / 5.0));
	
	for (float i = 0.; i < CAKE_LENGTH; i += 1.) if (distance(position, vec2(s(.5, 7.), c(.5, 7.) + .01 * i)) < 0.01 * CAKE_WIDTH) color = vec3(CAKE_COLOR);
	if (distance(position, vec2(s(.5, 7.), c(.5, 7.) + .009 * CAKE_LENGTH)) < .009 * CAKE_WIDTH) color = vec3(.9, .7, 0);
	for (float i = 0.; i < 360.; i += 360. / CANDLE_COUNT) {
		for (float z = 0.; z < CANDLE_LENGTH; z += 1.) if (distance(position, vec2(s(.5, 7.) + sin(i * rad + time)/(120. / CAKE_WIDTH), c(.5, 7.) + cos(i * rad + time)/(120. / CAKE_WIDTH) + .009 * CAKE_LENGTH + 0.0045 * z)) < .006 - (z / 700. / CANDLE_LENGTH))  color = vec3(CANDLE_COLOR);
		for (float z = 0.; z < FIRE_LENGTH; z += 1.) if (distance(position, vec2(s(.5, 7.) + sin(i * rad + time)/(120. / CAKE_WIDTH) - .0005 * sin(time * (i + 1.)) * z, c(.5, 7.) + cos(i * rad + time)/(120. / CAKE_WIDTH) + .009 * CAKE_LENGTH + .0045 * CANDLE_LENGTH + .002 * z)) < .006 - (CANDLE_LENGTH / 4000.) - sin(z * 3.) * .0005 - (z / 900. / FIRE_LENGTH)) color = vec3(FIRE_COLOR);
	}
	
	gl_FragColor = vec4(color, 1.);

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