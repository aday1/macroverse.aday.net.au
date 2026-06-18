/*{
    "DESCRIPTION": "PlasmicSiner",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "plasma"
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
        }
    ],
    "TAGS": [
        "plasma"
    ]
}*/

#define time TIME




#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define PI 90

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.xy ) - 0.02;
	
	float time = time + pow(cos(p.x*33.)*cos(p.x*33.), sin(p.y*55.)*sin(p.y*64.));
	
	p.y += sin(p.x+time)*0.5/p.x;
	
	float sx = 0.3 * (p.x + 0.8) * sin( 25.0 * p.x - 1. * pow(time, 0.09)*4.);
	
	float dy;
	//dy = 4./ ( 123. * abs(p.y - sx));
	dy = 1./ (10. * length(p - vec2(p.x, 0.5)));
	dy += 1./ (10. * length(p - vec2(p.x, 0.)));

	gl_FragColor = vec4( (p.x + 0.1) * dy, 0.3 * dy, dy, 2.1 );

}
