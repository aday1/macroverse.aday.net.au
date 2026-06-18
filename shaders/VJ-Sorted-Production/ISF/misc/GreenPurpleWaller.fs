/*{
    "DESCRIPTION": "GreenPurpleWaller",
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
// exchange

#ifdef GL_ES
precision mediump float;
#endif

void main( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.xy ) + time/100.;

	float color = 0.0;
	color += (sin( position.x * 80.0 ) + cos( (position.y + time/100.) * 40.0 ))*10000.;
	color -= (sin( (position.x + time/100.) * 40.0 ) + cos( position.y * 20.0 ))*10000.;
	gl_FragColor = vec4( vec3( color*0.00004, -color*0.00004, color*0.00004 ), 1.0 );

}
