/*{
    "DESCRIPTION": "VoidOpen2",
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
        }
    ],
    "TAGS": [
        "fractal"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

varying vec2 surfacePosition;

const float max_its = 100.;

float mandelbrot(vec2 z){
	vec2 c = z;
	for(float i=0.;i<max_its;i++){
		if(dot(z,z)>4.) return i;
		z = vec2(z.x*z.x-z.y*z.y,2.*z.x*z.y)+dot(z*-abs(sin(time - z.x*z.y)),z + 1.0);
	}
	return max_its;
}

void main( void ) {

	vec2 p = surfacePosition;
	
	gl_FragColor = vec4(mandelbrot(p)/max_its);

}
