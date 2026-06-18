/*{
    "DESCRIPTION": "ColorXY",
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

void main( void ) {

	vec2 p =mouse.x*( gl_FragCoord.xy / resolution.xy ) - mouse.y; 
	vec3 col = vec3(0); 
	
	p.x *= resolution.x/resolution.y; 

	float ang = atan(p.y,p.x); 
	
	col.r = (0.5+0.5*sin(floor(floor(ang*4.3230+time*-1.1)))); 
	col.g = (0.5+0.5*sin(floor(floor(ang*3.323+time*1.5)))); 
	col.b = (0.5+0.5*sin(floor(floor(ang*9.323-time*1.3)))); 
	gl_FragColor = vec4(col, 1.0); 
}
