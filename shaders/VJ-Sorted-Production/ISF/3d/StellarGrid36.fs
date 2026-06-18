/*{
    "DESCRIPTION": "StellarGrid36",
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
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float rand1(float x) {
	return fract(sin(x) * 43.23);
}
 
float rand2(vec2 p) {
	return fract(sin(p.x * 15.32 + p.y * 35.68) * 43578.23);	
}
 
void main( void ) {
 
	vec2 p = ( gl_FragCoord.xy / resolution.xy );
	p = p * 2.0 - 1.0;
	p.x *= resolution.x / resolution.y;
 
	p.x += sin(p.y * 8.0 + time) * 0.1;
	float col = rand2(floor(p * 12.0));
	float d = distance(p, vec2(-0.05,-0.125));
	col = col * (1.5 - d);
 
	gl_FragColor = vec4( vec3( col ), 4.0 );
 
}

