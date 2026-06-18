/*{
    "DESCRIPTION": "FractalLifeForm",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "fractal"
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
        "fractal"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define MAX_ITER 128
#define ESCAPE 4.0

// tweaking old formula --joltz0r

void main( void ) {
	vec2 p = (( gl_FragCoord.xy / resolution.xy ) - 0.5) * 2.0;
	p.x *= resolution.x / resolution.y;
	vec2 z = p;
	
	float color = 0.0;
	
	for (int n = 0; n < MAX_ITER; n++) {
		float fn = float(n) / float(MAX_ITER);
		vec2 t = vec2 (1.0 - (1.0 / float(n+1))) * time * vec2(0.31, 0.51);
		vec2 c = vec2(sin(p.x - t.x), cos(p.y - t.y)) * 0.55;

		vec2 r = z;
		
		z = vec2(
			dot(r, vec2(z.x, -z.y)),
			dot(r, z.yx)
		) + c;
		z = vec2(
			dot(r, vec2(z.x, -z.y)),
			dot(r, z.yx)
		) - c.yx;
		z = vec2(
			dot(r, vec2(z.x, -z.y)),
			dot(r, z.yx)
		) + r;
		
		float ln = sqrt(z.x);
		if (ln > ESCAPE) {
		
			color = sqrt(z.x);
			break;
		}
	}
	
	color /= float(MAX_ITER);
	gl_FragColor = vec4( vec3( color ) * vec3(1.0, 0.2, 0.1), 1.0 );

}
