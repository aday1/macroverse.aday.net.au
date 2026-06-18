/*{
    "DESCRIPTION": "DotMatrix-Solar",
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
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

const vec4 BLUE = vec4( 0.0, 0.5, 1.0, 1.0 );
const vec4 BLUET = vec4( 0.0, 0.75, 1.0, 1.0 );
const vec4 TEAL = vec4( 0.0, 1.0, 1.0, 1.0);
const vec4 TEALG = vec4( 0.0, 1.0, 0.5, 1.0);
const vec4 GREEN = vec4( 0.0, 1.0, 0.0, 1.0 );
const vec4 GREENY = vec4( 0.5, 1.0, 0.0, 1.0 );
const vec4 YELLOW = vec4( 1.0, 1.0, 0.0, 1.0);
const vec4 YELLOWR = vec4( 1.0, 0.5, 0.0, 1.0);
const vec4 RED = vec4( 1.0, 0.0, 0.0, 1.0 );
const vec4 REDP = vec4( 1.0, 0.0, 0.5, 1.0 );
const vec4 PURPLE = vec4( 1.0, 0.0, 1.0, 1.0 );
const int num = 3;
const float speed = 0.2;
float swell = 0.0, dist = 0.0;
vec3 controlPoints[50];

void main( void ) {
	controlPoints[13] = vec3(0.5+sin(time*speed)*0.5,0.3,1.0);
	controlPoints[14] = vec3(mouse,-1.0);
	for(int i = 0; i < num; i++){
		float j = float(i+1);
		float x = 0.5+sin(time*speed)*0.5+0.1*cos(time*j*speed);
		float y = 0.3+0.1*sin(time*j*speed);
		//controlPoints[i] = vec3(x,y,0.75-sin((time*j)*0.1)*0.5);
		controlPoints[i*4] = vec3(x,y,1.0);
		for(int k = 0; k < 4; k++){
			float x2 = x+0.1*cos(time*j*(float(k)+1.0)*speed);
			float y2 = y+0.1*sin(time*j*(float(k)+1.0)*speed);
			controlPoints[i*4+1+k] = vec3(x2,y2,0.5);
		}
	}
	
	for(int i = 0; i < num; i++){
		float j = float(i+1);
		float x = mouse.x+0.1*cos(time*j*speed);
		float y = mouse.y+0.1*sin(time*j*speed);
		controlPoints[i*4] = vec3(x,y,-0.5);
	}

	vec2 p = gl_FragCoord.xy / resolution.xy;
	float scale = 100.0/float(num);
	for(int i = 0; i < 15; i++){
		dist = distance(controlPoints[i].xy,p)*scale+controlPoints[i].z;
		if(dist > 0.0){
			swell += controlPoints[i].z/dist;
		} else {
			swell = 0.0;	
		}
	}
	swell = swell / float(num);
	
	if(swell < 0.2){
		gl_FragColor = BLUE;
	} else if(swell < 0.25){
		gl_FragColor = BLUET;
	} else if(swell < 0.30){
		gl_FragColor = TEAL;
	} else if(swell < 0.35){
		gl_FragColor = TEALG;
	} else if(swell < 0.40){
		gl_FragColor = GREEN;
	} else if(swell < 0.45){
		gl_FragColor = GREENY;
	} else if(swell < 0.50){
		gl_FragColor = YELLOW;
	} else if(swell < 0.55){
		gl_FragColor = YELLOWR;
	} else if(swell < 0.60){
		gl_FragColor = RED;
	} else if(swell < 0.65){
		gl_FragColor = REDP;
	} else {
		gl_FragColor = PURPLE;

	}
	
  	if (mod(swell*1000.0, 20.0) <= 0.5 && mod(swell*1000.0, 40.0) >= 0.2) {
		gl_FragColor = vec4(1.0,1.0,1.0,1.0); 
	}
}
