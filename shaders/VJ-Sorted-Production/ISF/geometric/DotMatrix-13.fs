/*{
    "DESCRIPTION": "DotMatrix-13",
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

void main( void ) {
	float aspect = resolution.y/resolution.x;
	vec2 p = (( gl_FragCoord.xy / resolution.x )-vec2(0.5, 0.5*aspect));
	float d = (abs(p.x)+abs(p.y))*15.0;
	vec3 c;
	
	if(dot(p, p) > 0.05){
		d -= time*8.0;	
	}
	else {
		d += time*8.0;
	}
	if(fract(d) > 0.5)
		c = vec3(1.0);
	else 
		c = vec3(0.0);
	float fd = fract(d);
	c = vec3(sin(abs(d)*2.0+0.8), sin(abs(d*4.5)+0.3), sin(abs(d*3.2)+0.5));

	gl_FragColor = vec4(c, 1.0);

}
