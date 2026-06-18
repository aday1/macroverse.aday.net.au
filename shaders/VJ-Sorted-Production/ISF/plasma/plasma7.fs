/*{
    "DESCRIPTION": "plasma7",
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
        "plasma"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

void main( void ) {

	vec2 p = 2.0 * gl_FragCoord.xy / resolution.xy - 1.0;

	p *= (1.+mouse);
	
	float r = 0.75 + sin( p.x ) * ( cos( 5.0  * p.x + time ) + sin( 7.0 * p.y - time ) + sin( time ) );
	float g = 0.25 + sin( p.y ) * ( cos( 3.0  * p.y + time ) + sin( 9.0 * p.x - time ) - cos( time ) );
	float b = 0.75 + sin( p.x ) * ( cos( 11.0 * p.x + time ) + sin( 3.0 * p.y + time ) + cos( time ) );
	
	gl_FragColor = vec4( r, g, b, max(r, max(g, b)) );

}
