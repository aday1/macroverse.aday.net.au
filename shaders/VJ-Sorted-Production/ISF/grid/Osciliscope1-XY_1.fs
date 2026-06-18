/*{
    "DESCRIPTION": "Osciliscope1-XY",
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
        "grid"
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
vec3 color = 0.3*vec3(0.2,0.9,0.7);
float d2y(float d){return 1./(0.2+d);}
float radius = 0.42;

float fct(vec2 p, float r){
	float x  = 0.5*p.x;
	vec2 f = 38.*mouse;
	
	#if MOUSE
	f*=4.*mouse;
	#endif
	
	float o = time*1.98;
	vec2 t = cos(f*x+o);
	float fctPos = dot(t, vec2(1.5,-0.7));
	float d = 10.*abs(6.*p.y - fctPos);
	return d2y(d)*(1.-step(radius,r));
}

float circle(vec2 p, float r){
	float d=distance(r, radius);
	return d2y(100.*d);
}

float grid(vec2 p, float y){
	float a = 0.2;
	float res = 50.;
	float e = 0.1;
	vec2 pi = fract(p*res);
	pi = step(e, pi);
	return a * y * pi.x * pi.y;
}

void main( void ) {
	
	vec2 position = (( gl_FragCoord.xy )-0.5*resolution)/ resolution.y ;
	
	float y  = 0.;
	
	float dc = length(position);
	
	y+=fct(position, dc);
	y+=circle(position, dc);
#if ! FLAT
	y+=grid(position, y);
#else
	y=1.-y;
	y=clamp(y,0.,1.);
	y=pow(y, 0.01);
	y=1.-y;
#endif
	y=pow(y,1.5);
	gl_FragColor = vec4( sqrt(y)*color,1.0 );
}
