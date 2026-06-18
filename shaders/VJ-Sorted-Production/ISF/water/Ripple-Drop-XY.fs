/*{
    "DESCRIPTION": "Ripple-Drop-XY",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "water"
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
        "water"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.xy );
	vec2 q = 2.0 * p - 1.0;
	q.x *= resolution.x / resolution.y;
	p = 2.0 * p - 1.0;
	p.x += 0.15 * sin(p.x * p.y * 3.14 * 1.0 + time);
	p = mod(length(p), 0.12) / 0.12 - 0.5 + (mouse * 2.0 - 1.0) * 5.0;
	float x = sin(atan(p.y, p.x) * 8.0 - time * 2.0);
	float c = 1.0 / (1.0 + exp(-(x-0.25)*10.0)) - 1.0 / (1.0 + exp(-(x+0.25)*10.0));
	float color = pow(0.5 - c * 0.5 + 0.05, 5.0);
	color = 1.0 / (1.0 + exp(-color * sin(length(p) * 3.14 * 5.0 - time)));
	color = pow(color, (length(p * q) * 3.14 * 1.0)) + 0.2;
	color += 0.2 / length(q + mouse * 2.0 - 1.0);

	gl_FragColor = vec4( vec3( 0.78 * color, 0.89 * color, 0.2 * color ), 1.0 );

}
