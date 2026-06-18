/*{
    "DESCRIPTION": "RGB-Mover",
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

// bpt.modified.2016
// started with http://glslsandbox.com/e#7886.9

float sdEpicyloidLike(vec2 center, float radius, vec2 position, float n,float a,float b)
{
	float l = distance(position, center);
	
	float t = time*1.25;
	
	float spin = 10.3*cos(t*1.6)*cos(t*.7)*0.1*l*cos(t*0.125);
	
	float r1 = radius+0.2/(a+abs(sin(n*atan(position.y-center.y, position.x-center.x)+spin)));
	
	r1 = 1.-(l*r1);
	
	float fudgeFactor = b; // 1e0;
	
	float v = 1.-clamp(abs(asin(pow(l * r1, fudgeFactor))),0.0,1.0);
	
	return pow( v, 0.5 );
}

float test(vec2 center, float radius, vec2 position, float a, float b,float t)
{
	//t = asin(t+a*3.14);
	return sdEpicyloidLike( center, radius, position, floor(abs(cos(t+a-b)*1.0-6.0*sin(t*0.3-b))),a, b );
}

void main(void)
{
	vec2 position = 4.0*(gl_FragCoord.xy - 0.5 * resolution) / resolution.y;
	float enabler = 1.0;
	
	float t = time * 0.25;
	float A = 2.0 * sin(t*.5) + 2.0*sin(t*0.1) + 1.0; // allow it go all spikey (aka negative)
	float B = sin(t)*3.0+3.1; // mess with the densisty (for lack of a better term)
	
	float r = enabler*test(vec2(sin(t*1.01), cos(t*0.98)), 1.0+sin(t*.19+0.0), position, A, B, t);
	float g = enabler*test(vec2(cos(t*0.94), sin(t*0.97)), 1.5+sin(t*.18+1.1), position, A, B, t);
	float b = enabler*test(vec2(sin(t*0.93), sin(t*0.99)), 2.0+g+cos(t*.17+0.2), position, A, B, t);
	float m = enabler*test(vec2(-cos(t*1.02), -sin(t*0.96)), 3.5+b+sin(t*.16+2.3), position, A, B, t);
	float y = enabler*test(vec2(-sin(t*0.92), -sin(t*1.03)), -1.0+sin(t*.15+0.4), position, A, B, t);
	float w = enabler*test(vec2(-sin(t*1.05), cos(t*0.95)), -2.5+y+cos(t+1.5)*.14, position, A, B, t);
	float o = enabler*test(vec2(cos(t*0.91), -cos(t*1.04)), -2.0+w+sin(t+2.6)*.13, position, A, B, t);
	
	gl_FragColor = vec4(r+y+m+o+w, g+y+0.5*o+w, b+m+w, 1.0);

	//gl_FragColor = vec4( (r), (g), (b), 1.0);
}


