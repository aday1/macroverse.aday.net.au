/*{
    "DESCRIPTION": "GlyphDream72",
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
//
#ifdef GL_ES
precision mediump float;
#endif

#define M_PI 3.1415926535897932384626433832795
#define RAYS 16.0

vec4 map( vec2 p ) {
	float r = sqrt(p.x*p.x+p.y*p.y);
	float a = (atan(p.y, p.x) + M_PI) / (2.0 * M_PI);
	a += time * 0.025;
	float c = (r < 0.2) ? 0.0 : mod(floor(a * RAYS * 2.0), 2.0) - r / 2.0;
	return vec4(vec3(1.0, c, c), 1.0 );
}

void main()
{
	vec2 p = -1.0 + 2.0  * ( gl_FragCoord.xy / resolution.xy );
	gl_FragColor = map(p);
}
