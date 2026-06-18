/*{
    "DESCRIPTION": "ChaoticNightmare-XY",
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

/*
 * inspired by http://www.fractalforums.com/new-theories-and-research/very-simple-formula-for-fractal-patterns/
 * a slight(?) different 
 * public domain
 */

#define N 40
void main( void ) {
	vec2 v = (gl_FragCoord.xy - resolution/2.0) / min(resolution.y,resolution.x) * 20.0;

	float rsum = 0.0;
	float pi2 = 3.1415926535 * 2.0;
	float C = cos(mouse.x * pi2);
	float S = sin(mouse.x * pi2);
	vec2 shift = vec2( 0.0, 1.0 );
	float zoom = (mouse.y*1.0 + 1.0);
	
	for ( int i = 0; i < N; i++ ){
		float rr = v.x*v.x+v.y*v.y;
		if ( rr > 1.0 ){
			rr = 1.0/rr;
			v.x = v.x * rr;
			v.y = v.y * rr;
			rsum *= 0.99;
			rsum += rr;
		}
		
		v = vec2( C*v.x-S*v.y, S*v.x+C*v.y ) * zoom + shift;
	}
	
	float col = rsum * .5;
	float t = sin(time*0.5);
	float u = sin(time*0.6);
	float uu = sin(time*0.7);

	gl_FragColor = vec4( cos(col*1.0)*t, cos(col*2.0)*u, cos(col*4.0)*uu, 1.0 );

}
