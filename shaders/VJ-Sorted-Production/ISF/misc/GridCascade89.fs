/*{
    "DESCRIPTION": "GridCascade89",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "misc"
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
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

void main( void ) {

	vec2 position = (gl_FragCoord.xy - 0.5 * resolution.xy) / resolution.xx - 0.6*mouse.xy + 0.3;
	float r0 = length(position);
	float angle = atan(position.y, position.x);
	float t = time*0.2;
	position.xy = vec2(position.x * cos(t) + position.y * sin(t), -position.x * sin(t) + position.y * cos(t));
	float tr = time * r0*.7;
	position.xy = vec2(cos(.01*tr)*position.x + sin(.015*tr)*position.y, cos(.002*tr)*position.y-sin(.007*tr)*position.x);
	position.xy *= sin(position.xy);
	
	float r = length(position)*50.0;
	r += .09 * (sin((angle + time*0.015) * 100.0) + cos((angle + time*0.01) * 100.0));
	
	gl_FragColor = vec4(fract(r*3.0), fract(r*0.2), fract(r*0.7), 1.0);
}
