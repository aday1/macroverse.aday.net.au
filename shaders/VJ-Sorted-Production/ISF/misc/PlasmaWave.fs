/*{
    "DESCRIPTION": "PlasmaWave",
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

	vec2 pos = -1.0+2.0*( gl_FragCoord.xy / resolution.xy );
	pos.x *= resolution.x/resolution.y;
	vec3 p = vec3(pos,cos(time/7.2314));
	float color = 0.0;
	for (int i = 0; i < 10; i++) {
		p = vec3(sin(time)*p.x - cos(time)*p.y, sin(time)*p.y + cos(time)*p.x, sin(time+2.464)*p.z);
		p = abs(p);
		p -= color;
		color += sin(float(i)+length(pos))*length(p);
	}

	gl_FragColor = vec4( sin(color*p.x)*0.5+0.5, sin(color*p.y)*0.5+0.5, sin(color*p.z)*0.5+0.5, 1.0 );
}
