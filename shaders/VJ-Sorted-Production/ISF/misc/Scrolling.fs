/*{
    "DESCRIPTION": "Scrolling",
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

// expand your variable spacing you disgusting, fucked up sociopath
void main( void ) 
{
	vec2 p = ( gl_FragCoord.xy / resolution.xy );
	float c = 0.0;
	p = p + time * 0.05;
	float ntime = time + length( p - time * 0.1 ) * 10.1;
	p = vec2( p.x * 1.17 + p.x, -p.x * 0.7);
	c = cos( ntime ) * 10.1 * atan( p.x );
	gl_FragColor = vec4( 0.5 * vec3( c * c * c / ( 01.1 * c ), c * 0.125, 1.4 * c ), 1.0 );
}  
