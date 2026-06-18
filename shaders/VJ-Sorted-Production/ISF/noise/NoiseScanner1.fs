/*{
    "DESCRIPTION": "NoiseScanner1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "noise"
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
        "geometric",
        "noise"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

// i found noise and hash fonctions in a stack overflow answer
// and i tried to modify it to get this pixel effect
// but the fractional brownian motion function was written by me
// enjoy my pixel world radar :D ^^

// important for animations

// [[cos(theta),-sin(theta)]
//  [sin(theta),sin(theta)]]
// a rotation matrix mixed with the time value
// to get rotation during the animation time
mat2 rotation_mat = mat2(cos(time/5.0),-sin(time/5.0),sin(time/5.0),cos(time/5.0));

float hash(vec2 n){
	float dot_prod = n.x*127.1 + n.y*311.7;
	return fract(sin(dot_prod)*43758.9876);
}

float noise(vec2 intervale){
	vec2 i = floor(intervale);
	vec2 f = fract(intervale);
	vec2 u = f*f*(1.0-2.0*f);
	
	return mix(mix(hash(i+vec2(0.0,0.0)),
		       hash(i+vec2(1.0,.0)),u.x),
		   mix(hash(i+vec2(0.0,1.0)),
		       hash(i+vec2(1.0,1.0)),u.x),
		   u.y);
}

//fractional brownian motion function
float fbm(vec2 p){
	float f = 0.0;
	float octave = 0.5;
	float sum = 0.0;
	
	for(int i=0;i<5;i++){
		sum += octave;
		f += octave*noise(p);
		p *= 2.01;
		octave /= 2.0;
	}
	
	f /= sum;
	
	return f;
}

void main( void ) {
	vec2 pos = gl_FragCoord.xy/resolution.xy*2.0-1.0;// pixels positions
	pos.x *= resolution.x/resolution.y;// decressing the aspect ratio of the resolution
	
	float effect = fbm(3.0*pos*rotation_mat);// our fractional brownian motion effect
	vec3 color = vec3(effect*tan(-3.0*time/3.0+pos.x),effect+sin(time),effect+sin(time));// preparing the color of pixels
	
	gl_FragColor = vec4(color,1.0);
}


