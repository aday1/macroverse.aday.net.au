/*{
    "DESCRIPTION": "CityLightningScapeXY",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "noise"
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
        "noise"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float noise2d(vec2 p) {
	return fract(sin(dot(p.xy ,vec2(1.,1.))));
}

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.xy );
	
	float a = mouse.y;
	for (int i = 1; i < 20; i++) {
		float fi = float(i);
		float s = floor(250.0*(p.x)/fi + 50.0*fi + time);
		
		if (p.y < noise2d(vec2(s))*fi/35.0 - fi*.05 + mouse.x) {
			a = float(i)/20.;
		}
	}
	
	gl_FragColor = vec4(vec3(a), 1.0 );
}
