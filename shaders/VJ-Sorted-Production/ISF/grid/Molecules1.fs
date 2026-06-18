/*{
    "DESCRIPTION": "Molecules1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

vec2 random2f(vec2 p) {
	vec2 tmp = fract(vec2(sin(p.x * 591.32 + p.y * 154.077), cos(p.x * 391.32 + p.y * 49.077)));
	return vec2(.5+.5*sin(tmp.x*time + p.y),.5+.5*cos(tmp.y*time + p.x));
}

float voronoi( in vec2 x )
{
    vec2 p = vec2(floor( x ));
    vec2 f = fract( x );
	
    float res = 8.0;
    const float s = 1.0;	
    for( float j=-s; j<=s; j++ ) {
        for( float i=-s; i<=s; i++ ) {
	    float m = mod( 2. * abs( i + p.x ) + 3. * abs( j + p.y ), 7. );
	    vec2 b = vec2(i, j);
	    vec2  r = b - f + random2f(b + p);
	    float d = length(r) * pow( 1.2, m );
	    res = min(res, d);
        }
    }
    return 1. - res;
}

void main( void ) {

	vec2 p = gl_FragCoord.xy / resolution.xy;
	p.x *= resolution.x / resolution.y;
	vec2 q = 2.0 * p - 1.0;
	
	float col = voronoi(q * 10.0);
	gl_FragColor = vec4(col,col,col, 1.0);
}
