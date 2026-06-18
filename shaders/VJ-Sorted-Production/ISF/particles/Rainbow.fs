/*{
    "DESCRIPTION": "Rainbow",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "particles"
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
        "particles"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

 // Nyancat rainbow ST mu6k;

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.xy );

	vec4 c;
	p.y +=-0.9+p.y+sin(time); // guess who ?
	   
	if (0.0/6.0<p.y&&p.y<1.0/6.0 || p.x>0.1) c= vec4(255,43,14,255)/255.0;  
	if (1.0/6.0<p.y&&p.y<2.0/6.0 || p.x>0.2) c= vec4(255,168,6,255)/255.0;  
	if (2.0/6.0<p.y&&p.y<3.0/6.0 || p.x>0.3) c= vec4(255,244,0,255)/255.0;  
	if (3.0/6.0<p.y&&p.y<4.0/6.0 || p.x>0.4) c= vec4(51,234,5,255)/255.0;  
	if (4.0/6.0<p.y&&p.y<5.0/6.0 || p.x>0.5) c= vec4(8,163,255,255)/255.0;  
	if (5.0/6.0<p.y&&p.y<6.0/6.0 || p.x>0.6) c= vec4(122,85,255,255)/255.0; 

	gl_FragColor = vec4(c);

}
