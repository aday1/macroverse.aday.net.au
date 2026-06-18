/*{
    "DESCRIPTION": "FractalOrgy1-PIXEL",
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

// "Fractal Orgy" by Pablo Román Andrioli (Kali)

// Move image to right border of screen to remove the pixelation!

// YOUTUBE: http://www.youtube.com/watch?v=zvHhnGjSmr8

// Well, I never though this was going to be not only my most popular shader, but the one 
// with more views here at Shadertoy!

// If you are just passing by to see this, please also take a look at my other works and 
// spend some time browsing the rest of the site, cause there is a lot of great stuff in 
// here that shows how coding can be a very interesting kind of art :)

// original: https://www.shadertoy.com/view/4dsGWB

//#define VOYEUR_MODE

float orgy(vec2 p) {
	float pl=0., expsmo=0.;
	float t=sin(time*10.);
	float a=-.35+t*.02;
	p*=mat2(cos(a),sin(a),-sin(a),cos(a));
	p=p*.07+vec2(.728,-.565)+t*.017+vec2(0.,t*.014);
	for (int i=0; i<13; i++) {
		p.x=abs(p.x);
		p=p*2.+vec2(-2.,.85)-t*.04;
		p/=min(dot(p,p),1.06);  
		float l=length(p*p);
		expsmo+=exp(-1.2/abs(l-pl));
		pl=l;
	}
	return expsmo;
}

void main( void )
{
	vec2 uv = gl_FragCoord.xy/resolution.xy-.5;
  	uv.x*=resolution.x/resolution.y;
	vec2 p=uv; p.x*=1.2;
	if (mouse.x<0.9) p=floor(p*15.)/15.;
	float o=clamp(orgy(p)*.07,.3,1.); o=pow(o,1.8);
	vec3 col=vec3(o*.8,o*o*.87,o*o*o*.9);
	float hole=length(uv+vec2(.1,0.05))-.25;
	#ifdef VOYEUR_MODE 
		col*=pow(abs(1.-max(0.,hole)),80.);
	#endif
	gl_FragColor = vec4(col*1.2+.15, 1.0 );
}

