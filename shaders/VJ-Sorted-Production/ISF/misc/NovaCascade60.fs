/*{
    "DESCRIPTION": "NovaCascade60",
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

	vec2 position = ( 100.0*gl_FragCoord.xy / resolution.xy ) + mouse.x * time;

		position.x -= 100.0;
	float color = 0.0;
	for(int i = 0; i < 200; ++ i) {
		position.x += pow(sin(position.x * 0.0001 * time), 6.0) + cos(position.x * 0.0001 * time);
		position.y += pow(sin(position.y * 0.0001 * time), 6.0) + cos(position.y * 0.0001 * time);
		
		color += position.x;
		color += position.y;

	}

	gl_FragColor = vec4(sin(position.x), cos(position.y), sin(color * 0.0001), 1.0);

}
