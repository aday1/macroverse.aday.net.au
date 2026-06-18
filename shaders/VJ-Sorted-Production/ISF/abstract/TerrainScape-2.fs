/*{
    "DESCRIPTION": "TerrainScape-2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        "abstract"
    ]
}*/






#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif
#define pi 3.141592653589793238462643383279
uniform float speed; // @expose 0 5

// How fast it animates
float tscale = 1.5;

float wave(vec2 position, float freq, float height, float speed) {
	float result = sin(position.x*freq - time*tscale*speed);
	result = result * 30.0 - 1.0;
	result *= height+cos(position.x);
	return result;
}

vec3 combo(vec2 position, float center, float size) {
	
	float offset = pi * (center - 0.9);
	float lum   = abs(tan(position.y * pi + offset)) - pi/5.0;
	lum *= size;
	
        float red   = wave(position, 5.0, 0.9*size, 1.08);
	float green = wave(position, 3.5, 0.5*size, 1.23);
	float blue  = wave(position, 1.5, 1.2*size, 1.42);
	
	return vec3( lum + red, lum + green, lum + blue );
}

void main( void ) {
	// normalize position
	vec2 position = gl_FragCoord.xy / resolution.xy;
	
	vec3 result = vec3(0.0, 0.0, 0.0);
	result += combo(position, 0.1+0.05*sin(0.6*time + 4.0*position.x), 0.05);
	result += combo(position, 0.5+0.05*sin(0.7*time + 3.0*position.x), 0.25);
	result += combo(position, 0.85+0.05*sin(0.42*time + 1.3*position.x), 0.05);

	gl_FragColor = vec4(result, 1.0);

}
