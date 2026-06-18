/*{
    "DESCRIPTION": "DotMatrix-10",
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

float noise2d(vec3 p) {
	return clamp((sin(dot(p.xy ,vec2(12.9898,78.233)))*tan(p.x+p.y)),1.0-p.z,1.0);
}

vec4 sample(int x, int y)
{
	vec2 p = ( (gl_FragCoord.xy + vec2(x, y)) / resolution.xy );
	
	float a = 0.0;
	for (int i = 1; i < 17; i++) {
		float fi = float(i);
		float cd = 116.0;
		float s = floor(173.0*(p.x)/fi + 61.0*fi + time / 0.09)- cd;
		if (p.y < noise2d(vec3(s,s,cd))*fi/(mouse.x*200.0) - fi*.0995 + mouse.y*1.5 ) {
			a = float(i)/10.;
		cd -= float(i) ;
		}
	}

	return vec4(vec3(a*p.x, a*p.y, a * (1. - p.x) ), 1.0 );
}

#define EDGE_THRESHOLD 1e-2

bool edge()
{
	vec4 mid = sample(0, 0);
	return distance(mid, sample(0, 1)) > EDGE_THRESHOLD || distance(mid, sample(1, 0)) > EDGE_THRESHOLD;
}

void main( void )
{
	gl_FragColor = edge() ? vec4(0.0, 0.0, 0.0, 1.0) : sample(0, 0);
}
