/*{
    "DESCRIPTION": "FractalizedCanvas",
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
precision highp float;
#endif

#define PI 3.1415
#define maxIter 128

varying vec2 surfacePosition;

void main( void ) {
	float r=1.,g=1.,b=1.;
	
	vec2 c = surfacePosition;
	c = c*2.;
	c *= 0.1;
	c += vec2(-.9,.3);
	vec2 z = vec2(0);
	
	float I = 0.;
	
	for(int i=1; i<maxIter; i++)
	{
		z = vec2(pow(z.x, 2.)-pow(z.y, 2.),z.x*z.y*2.)+c;
		if(length(z)>32.)
		{
			//float zn = z.x*z.x+z.y*z.y;
			float zn=length(z);
			I=float(i);
			I=mod(sqrt(I+1.0-log2(log2(zn)))*.15,1.0);
			break;
		}
	}
	
	if(I>0.)
	{
		float roff=0.95; float goff=0.9; float boff=0.1;
		//float rexp=1.8; float gexp=0.9; float bexp=0.7;
		float rexp=2.7; float gexp=1.5; float bexp=2.;
		
		r = -4.*pow(pow(mod(I+roff,1.),rexp)-0.5,2.)+1.;
		g = -4.*pow(pow(mod(I+goff,1.),gexp)-0.5,2.)+1.;
		b = -4.*pow(pow(mod(I+boff,1.),bexp)-0.5,2.)+1.;
		
		r = pow(r,0.8);
		g = pow(g,0.6);
		b = pow(b,0.2);
	}
	
	gl_FragColor = vec4( r, g, b, 1 );

}
