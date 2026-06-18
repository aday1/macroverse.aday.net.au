/*{
    "DESCRIPTION": "OrbitStorm23",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "misc"
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
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

void main( void ) {
	
	float s = 20.0;

	vec2 position = ( gl_FragCoord.xy / resolution.xy );
	vec2 mousepos = (mouse.xy*resolution.xy)+s;
	
	float color=0.0;
	if ((gl_FragCoord.x) < mousepos.x-mod(mousepos.x,s) && 
	    (gl_FragCoord.x+s) > mousepos.x-mod(mousepos.x,s) && 
	    (gl_FragCoord.y) < mousepos.y-mod(mousepos.y,s) && 
	    (gl_FragCoord.y+s) > mousepos.y-mod(mousepos.y,s)){
		color += 1.0;
	}
	else if (mod(gl_FragCoord.x,s)<=1.0 || mod(gl_FragCoord.y,s)<=1.0){
		color += 0.5;
	}
	
	color = min(color,0.8);
	gl_FragColor = vec4( vec3( color), 1.0 );

}
