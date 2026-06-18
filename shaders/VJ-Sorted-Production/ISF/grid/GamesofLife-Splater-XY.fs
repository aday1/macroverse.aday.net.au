/*{
    "DESCRIPTION": "GamesofLife-Splater-XY",
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
        }
    ],
    "TAGS": [
        "grid",
        "texture-input"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
// Conway's game of life

//teh amaze ! conways life on quazicrystal lattice

//sphinx (except I didnt write any of the code)

#ifdef GL_ES
precision highp float;
#endif

uniform sampler2D backbuffer;

vec4 live = vec4(0.,1.0,0.7,1.);
vec4 dead = vec4(0.,0.,0.,1.);
vec4 blue = vec4(1.,0.,1.,1.);

#define PI 3.14159
#define TWO_PI (PI * 2.0)
#define N 7.0

vec4 omg(void) 
{
	vec2 v = (gl_FragCoord.xy - resolution) / min(resolution.y,resolution.x) * 25.0;

	float col = 0.1;

	for(float i = 0.0; i < N; i++) 
	{
	  	float a = i * (TWO_PI/N);
		col += cos(TWO_PI*(v.x * cos(a) + v.y * sin(a)+ mouse.y +i*mouse.x + sin(time*0.001)*100.0 ));
	}
	
	 col /= 1.0;

	return vec4(col, col, col, 1.0);

}

void main( void ) {
	vec2 position = ( gl_FragCoord.xy / resolution.xy );
	vec2 pixel = 1./resolution;

	if (length(position-mouse) < 0.01) {
		float rnd1 = mod(fract(sin(dot(position + time * 0.001, vec2(14.9898,78.233))) * 43758.5453), 1.0);
		if (rnd1 > 0.5) {
			gl_FragColor = live;
		} else {
			gl_FragColor = blue;
		}
	} else {
		float sum = 0.;
		sum += texture2D(backbuffer, position + pixel * vec2(-1., -1.)).g;
		sum += texture2D(backbuffer, position + pixel * vec2(-1., 0.)).g;
		sum += texture2D(backbuffer, position + pixel * vec2(-1., 1.)).g;
		sum += texture2D(backbuffer, position + pixel * vec2(1., -1.)).g;
		sum += texture2D(backbuffer, position + pixel * vec2(1., 0.)).g;
		sum += texture2D(backbuffer, position + pixel * vec2(1., 1.)).g;
		sum += texture2D(backbuffer, position + pixel * vec2(0., -1.)).g;
		sum += texture2D(backbuffer, position + pixel * vec2(0., 1.)).g;
		
		sum += omg().x;
		vec4 me = texture2D(backbuffer, position);

		if (me.g <= 0.1) {
			if ((sum >= 2.9) && (sum <= 3.1)) {
				gl_FragColor = live;
			} else if (me.b > 0.004) {
				gl_FragColor = vec4(0., 0., max(me.b - 0.004, 0.25), 0.);
			} else {
				gl_FragColor = dead;
			}
		} else {
			if ((sum >= 1.9) && (sum <= 3.1)) {
				gl_FragColor = live;
			} else {
				gl_FragColor = blue;
			}
		}
	}
}
