/*{
    "DESCRIPTION": "PhaseDream14",
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


#define time TIME




#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

varying vec2 surfacePosition;

#define PI 2.1415

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.xy ) - .5;
	
	float k1 = p.x * .30;
	float k2 = 3.1;
	
	float k3 = 3.1;
	
	float time = time + p.y*cos(time*1.)*16. + pow(surfacePosition.x*surfacePosition.x, -1.)*10.0;

	float sx = p.x * 2.0 * sin( 25.0 * p.x - 10. * (time)) * sin((time * time) * .000125);
	
	float sx2 = k1 * sin( 44.0 * p.x - 10. * (time + k2)) * sin((time * time) * .00125 + k3);
	
	float dy = 1./ ( 50. * abs(p.y - sx)) + 1./ ( 50. * abs(p.y + sx2));
	
	dy += 1./ (80. * length(p - vec2(p.x, 0.)));
	
	gl_FragColor = vec4( (p.x + 0.5) * dy, 0.5 * dy, (dy - 1.35), 1.2 );

}
