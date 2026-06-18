/*{
    "DESCRIPTION": "BlackHoleSpot1",
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
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

struct Ray {
	vec3 pos;
	vec3 dir;
};
	
float distanceFunction(vec3 pos)
{
	return length(pos) - 5.0;
}
 
void main( void ) {
 
	vec2 pos = (gl_FragCoord.xy - resolution * 0.5)  / resolution.y + mouse - 0.5;
	Ray ray;
	ray.pos = vec3(0.0, 0.0, -10.0);
	ray.dir = normalize(vec3(pos * 3.0, 1.0));
	float d;
	for(int i = 0; i < 16; ++i)
	{
		d = distanceFunction(ray.pos);
		ray.pos += d * ray.dir;
		if (abs(d) < 0.001) break;
	}
	gl_FragColor = vec4(vec3(d), 1.0);
}
