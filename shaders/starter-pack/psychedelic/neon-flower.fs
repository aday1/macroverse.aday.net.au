/*{
    "DESCRIPTION": "neon flower",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "psychedelic"
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
        "psychedelic"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

// bpt.modified.2016 
// started with http://glslsandbox.com/e#7886.9

float sdEpicyloidLike(vec2 center, float radius, vec2 position, float n)
{
	float l = distance(position, center);
	float r1 = radius+0.2/(1.0+abs(sin(n*atan(position.y-center.y, position.x-center.x))));
	return clamp(abs(sin(((l * r1)) + 0.3)),-10.0,10.0);
}

float test(vec2 center, float radius, vec2 position)
{
	return sdEpicyloidLike( center, radius, position, floor(abs(10.*sin(time*0.3)))*2.+0. );
}

void main(void)
{
	vec2 position = 4.0*(gl_FragCoord.xy - 0.5 * resolution) / resolution.y;
	
	float enabler = 1.0;
	
	float r = test(vec2(sin(time*1.01), 0.5*cos(time*0.98)), 1.5-sin(time*.9), position);
	float g = enabler*test(vec2(cos(time*0.94), sin(time*0.97)), 1.5-sin(time*.8), position);
	float b = enabler*test(vec2(sin(time*0.93), sin(time*0.99)), 1.5-sin(time*.7), position);
	
	gl_FragColor = vec4(r, g, b, 1.0);
}

