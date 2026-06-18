/*{
    "DESCRIPTION": "IonTrace38",
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
#ifdef GL_ES
precision mediump float;
#endif

vec3 color;
void main( void ) {
	vec2 position = gl_FragCoord.xy / resolution.xy;
	vec3 color = vec3(sin(distance(position, mouse)*mod(time, 4.0) / 3.0) * 4.0, sin(distance(position, mouse)*mod(time, 1.3) / 0.4), sin(distance(position, mouse) * mod(time, 10.0) / 0.1));
	float dist = mod(distance(position, mouse), 10.0);
	
	if (dist*mod(time, 3.0) > 2.0) {
		dist /= 2.0;
		position *= 0.4;
	} else {
		dist *= 2.0;
		position /= 0.4;
	}
	
	if (dist < 3.0)
		color.r *= sin(mod(time*position.x, 3.5));
	else if (dist < 6.0)
		color.g *= sin(mod(time*position.y, 5.2));
	else if (dist < 9.0)
		color.b *= sin(mod(time/position.x, 0.4));
	else
		color.rbg *= sin(mod(dist*time, 4.0));
	
	gl_FragColor = vec4(color.r, color.g, color.b, 1.0);
}
