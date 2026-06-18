/*{
    "DESCRIPTION": "EchoFlash69",
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


#define time TIME




#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

varying vec2 surfacePosition;

float sinc(float x){
	if(x == 0.) return x;
	x *= 3.14159265;
	return sin(x)/x;
}
	
void main( void ){
	vec2 p;
	p = surfacePosition;
	float time = time + p.x*2.;
	float r = length(p);
	float theta = atan(p.y,p.x);
	
	p = vec2(r*cos(theta+5.*r*cos(time)),
		 r*sin(theta+5.*r*cos(time)));
	
	float color = 0.;
	color = sinc(p.x*p.y)/(sinc(p.x)*sinc(p.y));
	float c1 = fract(color*pow(1e5,cos(time*1e-1)*cos(time*1e-1)));
	float c2 = fract(color*pow(1e5,cos(time*1.5e-1)*cos(time*1.3e-1)));
	float c3 = fract(color*pow(1e5,cos(time*2.0e-1)*cos(time*1.7e-1)));
	
	gl_FragColor = vec4( vec3( c1, c2, c3), 1.0 );

}
