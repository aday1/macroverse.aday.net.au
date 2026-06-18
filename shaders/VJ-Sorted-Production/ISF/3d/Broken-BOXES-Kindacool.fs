/*{
    "DESCRIPTION": "Broken-BOXES-Kindacool",
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/
#define E 2.71828182846
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

uniform vec4 inputColour;

#define PI 3.1415926535

struct Ray {
	vec3 pos;
	vec3 dir;
};
	
float udRoundBox( vec3 p, vec3 b, float r )
{
	return length(max(abs(p)-b,0.0))-r;
}

vec3 repPos( vec3 p, vec3 c )
{
    return mod(p,c)-mouse.x*c;
}

float subFunc(vec3 pos)
{
	float a = mod(atan(pos.y, pos.x), PI / 1.5) - PI / 1.5 / 2.0;
	float xyLen = length(pos.xy);
	a -= pos.z;
	pos.xy = vec2(xyLen * sin(a), xyLen * cos(a));
	pos = repPos(pos, vec3(mouse.y));
	return udRoundBox(pos, vec3(0.0138), 0.001);
}

float func(vec3 pos)
{
	float a = mod(atan(pos.y, pos.x), PI / 1.5) - PI / 1.5 / 2.0;
	float xyLen = length(pos.xy);
	a += pos.z;
	pos.xy = vec2(xyLen * sin(a), xyLen * cos(a));
	pos = repPos(pos, vec3(0.33));
	return udRoundBox(pos, vec3(inputColour.x), inputColour.x);
}

float distFunc(vec3 pos)
{
	
	return max(-subFunc(pos), func(pos));
	//return subFunc(pos);
}

vec3 getNormal(vec3 pos)
{
	const float d = 0.0001;
	return normalize(
		vec3(
			distFunc(pos + vec3(d, 0, 0)) - distFunc(pos - vec3(d, 0, 0)),
			distFunc(pos + vec3(0, d, 0)) - distFunc(pos - vec3(0, d, 0)),
			distFunc(pos + vec3(0, 0, d)) - distFunc(pos - vec3(0, 0, d))
		)
	);
}

vec3 rayMarching(vec2 pos) {
	vec3 cameraPos = vec3(0.0, 0.0, -10.0 + time * 0.2);
	Ray ray;
	ray.pos = cameraPos;
	ray.dir = normalize(vec3(pos * 2.0, 1.0));
	float d;
	for(int i = 0; i < 64; ++i)
	{
		d = distFunc(ray.pos);
		ray.pos += d * ray.dir;
		if (abs(d) < 0.001) break;
	}
	
	float light = (dot(getNormal(ray.pos), vec3(1, 1, -1)));
	return clamp(vec3(1.0, 0.7, 0.4) * light + (ray.pos - cameraPos).z * 0.5, 0.0, 1.0);
}

void main( void ) {

	vec2 pos1 = (gl_FragCoord.xy + vec2(0.0, 0.0) - resolution * 0.5)  / resolution.y + mouse - 0.5;
	vec2 pos2 = (gl_FragCoord.xy + vec2(0.0, 0.5) - resolution * 0.5)  / resolution.y + mouse - 0.5;
	vec2 pos3 = (gl_FragCoord.xy + vec2(0.5, 0.0) - resolution * 0.5)  / resolution.y + mouse - 0.5;
	vec2 pos4 = (gl_FragCoord.xy + vec2(0.5, 0.5) - resolution * 0.5)  / resolution.y + mouse - 0.5;
	gl_FragColor = vec4(vec3(rayMarching(pos1)), 1.0);
	return;
	gl_FragColor = 
		vec4(
			(rayMarching(pos1) + 
			rayMarching(pos2) + 
			rayMarching(pos3) + 
			rayMarching(pos4)) / 4.0
		, 1.0);
}
