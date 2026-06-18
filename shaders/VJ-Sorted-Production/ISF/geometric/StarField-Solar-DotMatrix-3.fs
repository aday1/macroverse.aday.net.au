/*{
    "DESCRIPTION": "StarField-Solar-DotMatrix-3",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "geometric"
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
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

varying vec2 surfacePosition;

#define PI 3.14159265359

#define border 0.002

vec4 star( vec2 p,vec2 o, int points,float radius,vec4 color ){
	p-=o;
	p.y=-p.y;
	float angle = PI*2./float(points);
	mat2 r = mat2(cos(angle),sin(angle),-sin(angle),cos(angle));
		
	int c = 0;
	bool onBorder=false;
	for (int i=0;i<25;i++)
	{
		if (i>= points)
			break;
		
		if (p.y < radius){
			onBorder=onBorder||abs(p.y-radius)<border*2.0;
			c++;
		}
		p*=r;
	}
	return (c > points - (points-1)/2 )?onBorder?vec4(0,0,0,1):color:vec4(0);
}

float distanceToSegment(vec2 pos,vec2 p1,vec2 p2){
	vec2 v=p2-p1;
		pos-=p1;
  float t = dot(pos , v)/ dot(v,v);
        if (t < 0.0){ return length(pos);}       // Beyond the 'v' end of the segment
	else if (t > 1.0){ return distance(pos, v);}  // Beyond the 'w' end of the segment
  return distance(pos, t*v);
}

vec4 line(vec2 p,vec2 l1, vec2 l2, float d,vec4 color) {
	float l=distanceToSegment(p,l1,l2);
  return abs(l-d)<border?vec4(0,0,0,1):l<d?color:vec4(0);
}

vec4 circle(vec2 p,vec2 o,float radius,vec4 color){
	float d=distance(p,o);
	if(abs(d-radius)<border){
		return vec4(0,0,0,1);
	}else{
		return step(distance(p,o),radius)*color;
	}
}

vec4 arc(vec2 p,vec2 o,float r1,float r2,float startangle,float endangle,vec4 color){
	p=p-o;
	float a=atan(p.y,p.x)+PI;
	if(a>endangle){
		return step(distance(p,r1*vec2(cos(endangle-PI),sin(endangle-PI))),r2)*color;
	}else if(a<startangle){
		return step(distance(p,r1*vec2(cos(startangle-PI),sin(startangle-PI))),r2)*color;
	}else{
		return step(abs(length(p)-r1),r2)*color;
	}
}

float powLength(vec2 p,float e){
	return pow(pow(abs(p.x),e)+pow(abs(p.y),e),1.0/e);
}

vec4 box(vec2 p,vec2 size,vec2 o,vec4 color){
	p-=o;
	vec2 d=abs(p);
	if(d.x<size.x&&d.y<size.y){
		return (abs(d.x-size.x)<border*2.0||abs(d.y-size.y)<border*2.0)?vec4(0,0,0,1):color;
	}
	return vec4(0);
}
vec4 roundBox(vec2 p,vec2 size,vec2 o,vec4 color,float factor){
	p-=o;
	p.y*=size.x/size.y;
	float f=powLength(p,factor);
	return abs(f-size.x)<border?vec4(0,0,0,1):step(f,size.x)*color;
}

vec4 overlay(vec4 bg,vec4 fg){
	return mix(vec4(bg.xyz,1.0),vec4(fg.xyz,1.0),fg.w);
}

vec3 background(vec2 p){
	return mix(mix(vec3(0.0,0.3,0.9),vec3(0.5,0.0,0.9),length(p)),vec3(0.3,0.5,1.0),step(0.5,fract(10.0*(atan(p.x,p.y)-0.1*time)/(2.0*PI))));
}

void main( void ) {
	vec2 p = ( gl_FragCoord.xy / resolution.xy );
	p.x*=resolution.x/resolution.y;
	p.y-=0.1;
	//vec2 p=surfacePosition;

	vec4 color=vec4(background(floor((p-vec2(0.7,0.5))*50.0)/50.0),1.0);
	
	//legs
	color=overlay(color,max(line(p,vec2(0.6,0.2),vec2(0.6,0.0),0.02,vec4(0.2,0.6,0.8,1.0)),
			       line(p,vec2(0.6,0.0),vec2(0.58,0.0),0.02,vec4(0.2,0.6,0.8,1.0))));
	color=overlay(color,max(line(p,vec2(0.8,0.2),vec2(0.8,0.0),0.02,vec4(0.2,0.6,0.8,1.0)),
			       line(p,vec2(0.8,0.0),vec2(0.82,0.0),0.02,vec4(0.2,0.6,0.8,1.0))));
	
	p.y+=sin(time*6.)*0.03;
	//arms
	float a = sin(time*12.)*0.05;
	
	color=overlay(color,line(p,vec2(0.3-a*0.5,0.45+a),vec2(0.5,0.4),0.02,vec4(0.2,0.6,0.8,1.0)));
	color=overlay(color,line(p,vec2(0.9,0.4),vec2(1.1-a*0.5,0.45-a),0.02,vec4(0.2,0.6,0.8,1.0)));

	//body
	color=overlay(color,roundBox(p,vec2(0.25,0.36),vec2(0.7,0.5),vec4(0.0,0.6,0.4,1.0),20.0));
	//screen
	color=overlay(color,roundBox(p,vec2(0.2,0.15),vec2(0.7,0.65),vec4(0.4,1.0,0.8,1.0),20.0));
	//face
	color=overlay(color,circle(p,vec2(0.61,0.7),0.014,vec4(0,0,0,1)));
	color=overlay(color,circle(p,vec2(0.79,0.7),0.014,vec4(0,0,0,1)));
	color=overlay(color,arc(p,vec2(0.7,0.72),0.07,0.005,PI/2.0-0.6,PI/2.0+0.6,vec4(0,0,0,1)));
	//buttons
	color=overlay(color,circle(p,vec2(0.82,0.44),0.017,vec4(0.2,0.2,1,1)*(0.7+0.3*cos(time*5.))));
	color=overlay(color,circle(p,vec2(0.81,0.25),0.03,vec4(1,0,0.4,1)*(0.9+0.1*cos(time*5.4+33.))));
	color=overlay(color,circle(p,vec2(0.85,0.31),0.02,vec4(0.1,0.9,0.1,1)*(0.8+0.2*cos(time*4.2+1.1))));
	//diskslot
	color=overlay(color,box(p,vec2(0.12,0.015),vec2(0.66,0.44),vec4(0.0,0.2,0.2,1.0)*(0.7+0.3*cos(time*5.))));
	//cross
	color=overlay(color,max(box(p,vec2(0.04,0.014),vec2(0.62,0.27),vec4(1.0,1.0,0.0,1.0)),
				box(p,vec2(0.014,0.04),vec2(0.62,0.27),vec4(1.0,1.0,0.0,1.0))));
	//round buttons
	color=overlay(color,roundBox(p,vec2(0.03,0.01),vec2(0.66,0.2),vec4(0.0,0.0,0.2,1.0),3.0));
	color=overlay(color,roundBox(p,vec2(0.03,0.01),vec2(0.58,0.2),vec4(0.0,0.0,0.2,1.0),3.0));
	//triangle
	color=overlay(color,star(p,vec2(0.79,0.32),3,0.015,vec4(0.0,1.0,1.0,1.0)));
	
	gl_FragColor=color;

}
