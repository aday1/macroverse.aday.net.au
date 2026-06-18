/*{
    "DESCRIPTION": "PedalsStatic",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "misc"
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
        "misc"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

varying vec2 surfacePosition;

#define PI 3.141592653589793

void main( void ) {

	vec2 p = surfacePosition*10.;
	vec3 c = vec3(0);
	float a = atan(p.x,p.y);
	float r = length(p);
	
	const float PETALS = 5.;
	const float ROUTER = 3.;
	const float RINNER = 0.5;
	
	float si = sin(a*PETALS);
	float sip = sin(a*PETALS+.1);
	//si>sip &&
	if (si>0. && r>RINNER && r<ROUTER)
		c.r = pow(1.5,-r);
	if (r<RINNER+(ROUTER-RINNER)/2.+(ROUTER-RINNER)/2.*abs(cos(a*PETALS*.5))-abs(sin(a*PETALS)))
	//if (r>RINNER+(ROUTER-RINNER)/2.+(cos(a*5.)+cos(r)/2.))
		c.b = 1.;
	
	gl_FragColor = vec4( c , 1.0 );

}
