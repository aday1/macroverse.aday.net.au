/*{
    "DESCRIPTION": "lines2",
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
precision highp float;
#endif

// moded by seb.cc

const float COUNT = 3.0;

//MoltenMetal by CuriousChettai@gmail.com
//Linux fix

void main( void ) {  
	vec2 uPos = ( gl_FragCoord.xy / resolution.y );//normalize wrt y axis
	uPos -= vec2((resolution.x/resolution.y)/2.0, 0.5);//shift origin to center
uPos *= mouse;
	
	float vertColor = 0.0;
	for(float i=0.0; i<COUNT; i++){
		float t = time*(i*0.1+1.)/3.0 + (i*0.1+0.1); 
		uPos.y += sin(-t+uPos.x*2.0)*0.45 -t*0.3;
		uPos.x += sin(-t+uPos.y*5.0)*0.25 ;
		float value = (sin(uPos.y*10.0*0.5)+sin(uPos.x*10.1+t*0.3) );
		
		//float d=1./pow(distance(mouse,uPos),2.);
		
		float stripColor = 1.0/sqrt(abs(value));
		
		vertColor += stripColor/10.0;
	}
	
	float temp = vertColor;	
	vec3 color = vec3(temp*max(0.1,abs(sin(time*0.1))), max(0.1,temp*abs(sin(time*0.03+1.))), max(0.1,temp*abs(sin(time*0.02+3.))));	
	color *= color.r+color.g+color.b;
	gl_FragColor = vec4(color, vertColor);
}
