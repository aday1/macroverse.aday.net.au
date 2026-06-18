/*{
    "DESCRIPTION": "PlasmaBridge27",
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
	vec2 pos = ( gl_FragCoord.xy / resolution.xy ) - vec2(0.5, 0.5);
	float color = (abs(pos.y*2.1-(sin(pos.x*10.0+time*3.0)+sin(pos.x*2.0+time*2.0)+sin(pos.x*30.0+time*2.1))*0.33)<0.01?1.0:0.0);
	color += (pow(sin(pos.x*150.0),20.0)+pow(sin(pos.y*150.0*resolution.y/resolution.x),200.0))*0.1;
	gl_FragColor = vec4( vec3( 0, color+0.1, 0), 1.0 );
}
