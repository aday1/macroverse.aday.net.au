/*{
    "DESCRIPTION": "Mozacie-Like-XY",
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
//Voronoi II
//by nikoclass

#ifdef GL_FRAGMENT_PRECISION_HIGH
#ifdef GL_ES
	precision highp float;
#endif
#else
#ifdef GL_ES
	precision mediump float;
#endif
#endif

float M = 30.0;

float rand(vec2 co){
  return fract(sin(dot(co.xy ,vec2(12.988,78.233))) * 1758.5453);
}

vec3 colorInCuadrant(vec2 cuadrant) {
	float x = cuadrant.x;
	float y = cuadrant.y;
	float t = time;
	vec3 color = 0.5 + 0.5*vec3(sin(x*y*16.0 + 0.4*t), sin(y*7.0 + 1.0 + 0.2*t), sin(y*9.0 + 2.0 + -x * 3.0 + 0.1*t)); 
	color = clamp(abs(color), 0.0, 1.0);
	color = pow(color, vec3(2.2));
	return color;
}

vec2 posInCuadrant(vec2 cuadrant) {
	return vec2(rand(cuadrant), rand(cuadrant * 3.0 + 1.1)) / M;
}

void main( void ) {
	vec2 pos = ( gl_FragCoord.xy / resolution.xy );
	vec3 color = vec3(0);
	
	float md = 0.005 / distance(pos, mouse);
	//float md = -0.1*(1.0 - distance(pos, mouse));
	pos += md * normalize(pos - mouse);

	vec2 cuadrant = vec2(ivec2(pos * M))/M;

	float minDist = 10000.0;
	
	for (int i = -1; i <= 1; i++)
		for(int j = -1; j <= 1; j++) {
			vec2 offset = vec2(i, j) / M;
			vec2 neighborCuadrant = cuadrant + offset;		
			vec2 neighborPoint = neighborCuadrant + posInCuadrant(neighborCuadrant);

			float d = distance(pos, neighborPoint);
			if (d < minDist) {
				minDist = d;
				color = colorInCuadrant(neighborCuadrant);
			}
		}
	
	color += rand(pos)*0.05;	
	gl_FragColor = vec4(color, 1.0 );
}
