/*{
    "DESCRIPTION": "VoidLens37",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "plasma"
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
        "plasma"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.xy ) -0.5;//+ mouse / 8.0;
//	float color = 4.0;
	
	float sx =0.3*(p.x+0.5)*sin(20.0*p.x-10.*time);
	float dy =7./(500.*abs(p.y-sx));//1/DICKE

	//gl_FragColor = vec4( vec3( .01, 0.9*dy ,dy*.9) ,1 );	
	float red =.03+1.*sin(.1*p.x);
	float green =.8*dy+0.1*sin(p.x);
	float blue = dy*5.+9.*sin(.1*p.x);

	//float red =.1;
	//float green =.1;
	//float blue =.1;
	
	if (blue< .6){
		
		//blue =*blue; //GLOW EFFEKT
	}
	//if ((p.y >((sin(time*10.))/2.+.1)&&p.y <(sin(time*.4))/2.)&&sin(p.x*100.+cos(p.y*tan(p.x)*6.)*30.)>.8){
	//	green *=3.;
	//}

	gl_FragColor = vec4( vec3( red, green ,blue) ,1.0 );

}
	
void draw(){

}
