/*{
    "DESCRIPTION": "Osciliscope2-XY",
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
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define FLAT  0
#define MOUSE 1

void main( void ) {
	
	vec2 position = (( gl_FragCoord.xy )-0.5*resolution)/ resolution.y ;
	
	float c  = 0.;
	
	float dc = distance(position, vec2(0.));
	float x  = 0.5*position.x;
	float f1 = 38.;
	float f2 = 0.7*0.5*f1;
	
	#if MOUSE
	f1*= mouse.x*2.;
	f2*= mouse.y*2.;
	#endif
	float fctPos = -1.5*cos(f1*x)+0.7*cos(f2*x);
	float d = 8.*abs(7.*position.y - fctPos);
	c+= (1.-step(0.4,dc))/d;
	
	d=120.*abs(dc-0.4);
	c+= 1./d;
	
	#if FLAT
	c=1.-c;
	c=clamp(c,0.,1.);
	c = pow(c, 0.01);
	c=1.-c;
	#endif
	gl_FragColor = vec4( sqrt(c*vec3(.2,.08,.3)),1.0 );
}
