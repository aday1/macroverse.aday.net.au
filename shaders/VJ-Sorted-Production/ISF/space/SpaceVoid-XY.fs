/*{
    "DESCRIPTION": "SpaceVoid-XY",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "space"
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
        "space"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

float Pi = 1.0;//3.14159265359	;
#define  fmax = 100.0
float sqr (float x) {return x*x;}

float ampx (float freq){
	return sqr(freq);
}
float ampy (float freq){
	return sqr(freq);
}

float amp (float f){
	//return 1.0;
	//return sqr(f);
	//return f;
	return 1.0/f;
	//return mod(f,2.)*1.0/(f);
}

float ffreq (float h) {
	return pow(2.0,h);
}

float ftime () {
	return 1.0;
	return mod(time*mouse.y*0.1,10.0/mouse.y);
}
float saw (float x) { return 2.0*(mod(x,1.0)-0.5);}

float triangle (float x) {return 2.0*abs(saw(x))-1.0;}

float dist (float a, float b, float steepness) {
	float x = abs(a-b)*steepness;
	return  1.0/(x*x/sqrt(x)+1.0);
}

float dist (vec2 a, vec2 b, float steepness){
	return dist(length(a-b),0.0,steepness);
}

float sum (vec2 pos) {
	float res = 1.0;
	float ceil = 0.0;
	for (float f = 1.0;  f <5.0; f++){
		float fq = ffreq(f)/pos.y;	
		//pos.y = sqr(pos.y/2.);
		//fq = 40.0-40.0*pos.y/pos.x+0.;
		//fq *= f;
		res += min(saw(fq * (pos.x + ftime())) * amp(fq), triangle(fq * (pos.x + ftime())) )* amp(fq);
		//res += max(res,max(saw(fq * (pos.y + ftime())) * amp(fq), triangle(fq * (pos.y + ftime())) * amp(fq)) );
		res += min(saw(fq * (pos.y + ftime())) * amp(fq), triangle(fq * (pos.y + ftime())) ) * amp(fq);
		ceil += 2.0*abs(amp(fq));
	}	
	//res = pos.y;
	return  (res)/(2.0*ceil);
}

void main( void ) {
	
	vec2 position = ( gl_FragCoord.xy / resolution.xy )*2.-vec2(1.0);// - mouse / 10.0;
	position.x *= sqr(2.*mouse.y);
	position.x += mouse.x-0.5;

	vec3 color = vec3(0.0);
	float rad = Pi/2.0;
	float ceil = 0.0;
	/*
	for (float f = 1.0;  f <2.0; f++){
		float fq = ffreq(f);
		//color += sin(fq*(position.x+ftime())*Pi)*amp(fq);	
		//color += saw(fq*(position.x+ftime())*Pi)*amp(fq);	
		color += triangle(fq*(position.x+ftime()))*amp(fq);
		ceil += abs(amp(fq));
	}
	
	vec3 colorn = (color+ceil)/(2.0*ceil);*/
	float eps = 0.005;
	float h = 2.0;
	float s = (sum(position)+sum(position+vec2(eps))+sum(position-vec2(eps)))/3.0;
	
	vec3 colorn = vec3(s);
	float dx = 0.01*s;
	float dy = 0.005+dx+0.1*s;
	colorn.r *= sum(vec2(position.x+dx, position.y+dy));
	
	//if (abs(position.y+0.5 - (colorn.r)) < eps) {colorn.g = 1.0;}
	
	//colorn.g += dist(position.y + 0.5,colorn.r,500.0);
	//colorn.g += dist(position.y + 0.5,sum(vec2(position.x,1.0)),200.0);
	
	eps = 0.01;
	//if (abs(position.y+0.5 - (ftime()/10.)) < eps && position.x <0.5) {colorn.r = 1.0;}
	
	//color += color/2.;
	
	//if (colorn.r > 1.0) { colorn.r = 0.0;}
	//if (colorn.r < 0.0) { colorn.r = 1.0;}
	
	//draw axes
	
	eps = 0.01;
	//if (abs(position.y)  < eps) {colorn = vec3(0.0);}
	
	colorn -= dist(position, vec2(position.x,0.0),2000.)+dist(position, vec2(position.x+eps,0.0),2000.);
	
	colorn -= dist(position, vec2(0.0,position.y),2000.);
	//if (abs(position.x)  < eps) {colorn = vec3(0.0);}
	
	//if (abs(position.y-.5)  < eps) {colorn = vec3(1.5);}
	
	gl_FragColor = vec4( vec3( colorn.r, colorn.g ,colorn.b ), 1.0 );

}
