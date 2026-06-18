/*{
    "DESCRIPTION": "RingPrism43",
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

vec3 box(in vec2 p)
{
	if (abs(p.x) < 0.08 && abs(p.y) < 0.5)
		return vec3(1.0);
	return vec3(0.0); 
}
vec3 curve(in vec2 p) 
{
	p.x +=  smoothstep(0.0, 1.0, pow(clamp(-p.y, 0.0, 1.0), 1.2));
	if (p.x > -0.1*smoothstep(1.0, 1.5, 1.0-p.y) && p.x < 0.15 && abs(p.y) < 0.5)
		return vec3(1.0);
	return vec3(0.0); 
	
}

void main( void ) {

	vec2 p = 2.0*( gl_FragCoord.xy / resolution.xy ) - 1.0;
	p.x *= resolution.x/resolution.y; 
	vec3 color = vec3(0.0); 
	
	color = box(p) + curve(p+vec2(0.30,0.0)) + curve(vec2(-p.x,p.y)+vec2(+0.30, 0.0));  
	color = color*vec3(sin(p.y*5.0-time),sin(p.y*6.0-5.0-time),sin(p.y*7.0+4.0-time)).yzx; 
	
	gl_FragColor = vec4(color, 1.0); 
}
