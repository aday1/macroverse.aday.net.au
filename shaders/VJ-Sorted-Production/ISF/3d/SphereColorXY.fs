/*{
    "DESCRIPTION": "SphereColorXY",
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

	varying vec2 surfacePosition;
	
	void main( void ) {
	
		vec2 p = surfacePosition;
		float color = mouse.y;

		vec3 p3 = vec3(p.x, p.y, cos(time+p.x));
		color = length(p3);
		float iRange = 1e1;
		for(int i = 0; i < 3; i++){
			color = cos(float(i)*iRange+color*10.+length(p3));
		}
		gl_FragColor = vec4( vec3( mouse.y, mouse.x * 0.5, sin( color + mouse.x / 3.0 ) * 5.75 ), 1.0 );
	
	}
