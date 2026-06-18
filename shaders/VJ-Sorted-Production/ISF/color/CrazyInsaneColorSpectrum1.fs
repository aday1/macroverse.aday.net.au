/*{
    "DESCRIPTION": "CrazyInsaneColorSpectrum1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "color"
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
        "color"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

//Random shader generator. anastadunba
float spd = 20.;
#define SEED floor(time*spd)*1.42
#define SEED2 floor(time*spd)*3.22
#define SEED3 floor(time*spd)*1.52
#define SEED4 floor(time*spd)*2.42

float rand(float co){
    return fract(sin(dot(vec2(co) ,vec2(12.9898,78.233))) * 43758.5453);
}

#define w_m 7.
//Get variable
#define w(a,c) ((float(a == 0.)*uv.x)+(float(a == 1.)*uv.y)+(float(a == 2.)*(1.-uv.x))+(float(a == 3.)*(1.-uv.y))+(float(a == 4.)*fract(time))+(float(a == 5.)*c)+(float(a == 6.)*length(uv-.5))+(float(a == 7.)*atan(uv.x-.5,uv.y-.5))) 
//Get color channel
#define g(d) ((float(floor(mod(d,4.)) == 0.)*color.r)+(float(floor(mod(d,4.)) == 1.)*color.g)+(float(floor(mod(d,4.)) == 2.)*color.b))

#define interact_m 12.
float interact(float a, float b, float type) {
	type = mod(floor(type*interact_m),interact_m+1.);
	float j = 0.;
	if (type == 0.) { j = a+b; }
	if (type == 1.) { j = a-b; }
	if (type == 2.) { j = a*b; }
	if (type == 3.) { j = a/b; }
	if (type == 4.) { j = pow(a,b*2.); }
	if (type == 5.) { j = mod(a,b); }
	if (type == 6.) { j = step(a,b); }
	if (type == 7.) { j = rand(a)*b; }
	if (type == 8.) { if (a*3. > b*3.) j = a; }
	if (type == 9.) { if (a*3. < b*3.) j = a; }
	if (type == 10.) { j = sqrt(pow(a,2.)+pow(b,2.)); }
	if (type == 11.) { j = floor(a*(b*7.))/(b*7.); }
	if (type == 12.) { j = sin(a*b*10.); }
	return j;
}

void main( void ) {

	vec2 uv = ( gl_FragCoord.xy / resolution.xy );
	const int loops = 14;
	vec3 color = vec3(w(SEED3,uv.x),w(SEED3+1.,uv.y),w(SEED3+2.,uv.x*uv.y));
	float d = 0.;
	float d2 = 0.;
	for (int j = 0; j < loops; j++) {
	    float i = float(j);
	    d2 += rand(i+SEED3);
	    d += rand(i+SEED4);
	    color.r = interact(w(mod(floor(i*SEED),w_m+1.),g(rand(i+d2)*4.)) , w(mod(floor(i*SEED2),w_m+1.),g(rand(i+d2)*4.)) , rand(SEED3+i+d));
	    d2 += rand(i+SEED3);
	    d += rand(i+SEED4);
	    color.g = interact(w(mod(floor(i*SEED),w_m+1.),g(rand(i+d2)*4.)) , w(mod(floor(i*SEED2),w_m+1.),g(rand(i+d2)*4.)) , rand(SEED3+i+d));
	    d2 += rand(i+SEED3);
	    d += rand(i+SEED4);
	    color.b = interact(w(mod(floor(i*SEED),w_m+1.),g(rand(i+d2)*4.)) , w(mod(floor(i*SEED2),w_m+1.),g(rand(i+d2)*4.)) , rand(SEED3+i+d));
	}
	
	gl_FragColor = vec4(fract(color), 1.0 );

}
