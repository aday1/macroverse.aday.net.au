/*{
    "DESCRIPTION": "StarField-DotMatrix-25",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "particles"
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
        "geometric",
        "particles"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float rand(vec2 co)
{
	return fract(sin(dot(co.xy, vec2(12.9898, 78.233)))*43758.5453);
}

void main(void)
{
	vec2 adjustedRes = vec2(max(resolution.x, resolution.y),
				max(resolution.x, resolution.y));
	// Make sure our stars aren't stretched in non-square windows.
	
	vec2 coord = gl_FragCoord.xy/adjustedRes.xy;
	
	vec4 color = vec4(0., 0., 0., 0.);

	for (float i = 0.; i < 20.; i++) {
		const float min_period = 10.;
		const float max_period = 30.;
		float period = min_period + rand(vec2(i, 0.))*(max_period - min_period);

		float start = period*rand(vec2(i, 1.));

		const float min_radius = .01;
		const float max_radius = .05;
		float radius = min_radius + rand(vec2(i, 2.))*(max_radius - min_radius);

		float r = time - start;
		vec2 pos = vec2((mod(r, period))*2./period-.9, rand(vec2(.1*ceil(r/period), i))); // Fixed stars popping in and out of screen

		const float min_angle_speed = -.5;
		const float max_angle_speed = .5;
		float angle_speed = min_angle_speed + rand(vec2(i, .3))*(max_angle_speed - min_angle_speed);

		float angle = atan(pos.y - coord.y, pos.x - coord.x) + angle_speed*time;

		float dist = radius + .3*sin(5.*angle)*radius;

		color += (1. - smoothstep(dist, dist + .01, distance(coord, pos)))*vec4(rand(vec2(i, 4.)), rand(vec2(i, 5.)), rand(vec2(i, 6.)), 1.);
	}

	gl_FragColor = color;
}

