/*{
    "DESCRIPTION": "BirthdayCake",
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
            "NAME": "timeScale",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Time speed"
        },
        {
            "NAME": "mouseX",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Mouse X"
        },
        {
            "NAME": "mouseY",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Mouse Y"
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
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

void main(void) {
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
