/*{
    "DESCRIPTION": "NeonLines-XY",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "psychedelic"
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
        "psychedelic"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// http://glslsandbox.com/e#27036.0

// perspective 80s grid
#ifdef GL_ES
precision mediump float;
#endif

float prob_sum(float a, float b) {
	return 1.0 - (1.0 - a) * (1.0 - b);
}

void main( void ) {

	vec2 pos = gl_FragCoord.xy / resolution.xy;
	pos = pos * 2.0 - 1.0;
	pos *= 200.0;
	float z = 1.0 / (((gl_FragCoord.xy / resolution.xy).y+1.0));
	z = 1.0 - z;
	z*=4.0;
	pos.x *= z;

	float cyan = 0.1;
	float magenta = 0.2 * sin(time);
	
	float m = distance( mouse * resolution, pos ) / resolution.y;
	
	float xspeed = 50.0;
	float yspeed = -30.0;
	float cell_size = 30.0;
	float big_glow_size = 9.0 * pow( 0.5 + m, 0.8 );
	float small_glow_size = 1.0 * pow( 0.8 + m, 0.5 );
	
	float d;
	
	// Right side
	d = mod( pos.x + xspeed*time, cell_size );
	if ( d < big_glow_size )
		cyan = prob_sum(cyan, 1.0 - d / big_glow_size);
	if ( d < small_glow_size )
		magenta = prob_sum(magenta, 1.0 - d / small_glow_size);
	// Left side
	d = cell_size - d;
	if ( d < big_glow_size )
		magenta = prob_sum(magenta, 1.0 - d / big_glow_size);
	if ( d < small_glow_size )
		cyan = prob_sum(cyan, 1.0 - d / small_glow_size);
	// Top side
	d = mod( pos.y - yspeed*time, cell_size );
	if ( d < big_glow_size )
		cyan = prob_sum(cyan, 1.0 - d / big_glow_size);
	if ( d < small_glow_size )
		magenta = prob_sum(magenta, 1.0 - d / small_glow_size);
	// Bottom side
	d = cell_size - d;
	if ( d < big_glow_size )
		magenta = prob_sum(magenta, 1.0 - d / big_glow_size);
	if ( d < small_glow_size )
		cyan = prob_sum(cyan, 1.0 - d / small_glow_size);
	
	vec3 col = vec3( 0.0 );
	col.r = magenta;
	col.g = cyan;
	col.b = prob_sum(magenta, cyan);
//	col = vec3(z);
	gl_FragColor = vec4( col, 1.0 );

}
