/*{
    "DESCRIPTION": "DotMatrix-GridPattern-2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
        "grid"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define GRID_SIZE 32
#define PI 3.1416

vec3 color(float d) {
	return d * vec3(0, 1, 0);	
}

int mod(int a, int b) {
	return a - ((a / b) * b);
}

void main(void)
{

	vec2 p = (-1.0 + 2.0 * ((gl_FragCoord.xy) / resolution.xy));
	p.x += sin(time + (p.y * 2.5)) * 0.25;
	p.y *= p.y * p.y;
	//p -= (2.0 * mouse.xy) - vec2(1.0);
	p.x *= (resolution.x / resolution.y);
	vec2 uv;

	float a = (atan(p.y,p.x) + time);
	float r = sqrt(dot(p,p));

	uv.x = 0.1/r;
	uv.y = a/(PI);
	
	float len = dot(p,p);
	
	vec3 col = color(pow(fract(uv.y / -2.0), 15.0));
	if (len > 0.7) col = vec3(0.0,0.5,0.8);
	if (len > 0.73) col = vec3(0,0,0);
	
	bool grid_x = mod(int(gl_FragCoord.x) - int(resolution.x / 2.0), GRID_SIZE) == 0;
	bool grid_y = mod(int(gl_FragCoord.y) - int(resolution.y / 2.0), GRID_SIZE) == 0;
	
	if (len < 0.7)
	{
	if (grid_x || grid_y)
		col += color(0.5);
	
	if (grid_x && grid_y)
		col += color(1.0);
	}
	gl_FragColor = vec4(col, 1.0);
}
