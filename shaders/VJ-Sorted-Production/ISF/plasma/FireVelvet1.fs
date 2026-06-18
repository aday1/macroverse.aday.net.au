/*{
    "DESCRIPTION": "FireVelvet1",
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



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define MAX_ITER 64

void main( void ) {
	vec2 p = (gl_FragCoord.xy / resolution * 7.0) - vec2(15.0);
	vec2 i = p;
	
	float c = 2.9;
	float inten = .05;

	for (int n = 0; n < MAX_ITER; n++){
		float t = -time * (0.4 - (4.0 / float(n+1)));
		i = p + vec2(cos(t - i.x) + sin(t + i.y), sin(t - i.y) + cos(t + i.x));
		c += 1.1/length(vec2(p.x / (2.*sin(i.x+t)/inten),p.y / (cos(i.y+t)/inten)));
	}
	c /= float(MAX_ITER);
	c = 1.5-sqrt(pow(c,4.2));
	float col = c*c*c*c;
	gl_FragColor = vec4(vec3(col * 1.0, col * 0.5, col * 0.1), 1.0);
}

