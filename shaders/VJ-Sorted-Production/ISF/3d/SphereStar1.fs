/*{
    "DESCRIPTION": "SphereStar1",
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
// http://glslsandbox.com/e#29694.0
// Check out its Parents

#ifdef GL_ES
precision mediump float;
#endif

#define FLAT  0
#define MOUSE 1
#define PI 3.14159265359

vec3 color = 0.3*vec3(0.9,0.2,0.2);
float d2y(float d){return 1./(0.2+d);}
float radius = 0.42;

float fct(vec2 p, float r){
	float a = 1.*mod(-atan(p.y, p.x)+time+(mix(sin(time*0.05),100.0,5.0))/(r*r), 2.*PI);

	float scan = 0.*1.;
	return (d2y(a)+scan)*(1.-step(radius,r));
}

float circle(vec2 p, float r){
	float d=distance(r, radius);
	return d2y(50.*d);
}

float grid(vec2 p, float y){
	float a = 0.2;
	float res = 10.;
	float e = 0.1;
	vec2 pi = fract(p*res);
	pi = step(e, pi);
	return a * y * pi.x * pi.y;
}

void main( void ) {
	
	vec2 position = (( gl_FragCoord.xy )-0.5*resolution)/ resolution.y ;
	position/=cos(2.5*length(position));
	float y  = 0.;
	
	float dc = length(position);
	
	y+=fct(position, dc);
	y+=circle(position, dc);
#if ! FLAT
	y+=grid(position, y);
#else
	y=1.-y;
	y=clamp(y,0.,1.);
	y=pow(y, 0.03);
	y=1.-y;
#endif
	y=pow(y,1.75);
	gl_FragColor = vec4( sqrt(y)*color,1.0 );
}
