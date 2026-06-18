/*{
    "DESCRIPTION": "GLScape1",
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "geometric"
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

// mouse.x
// mouse.y
// inputColour.x
// inputColour.y
// inputColour.z
// inputColour.w

float map(vec3 p)
{
	float t = (length( mod(abs(p.xz), 20.0) - 10.0) - (2.2 + p.y * mouse.x));
	t = max(t, 0.5 - dot(p, vec3(0,1,0))) ;
	return t;
}

vec3 calcNormal(in vec3 pos)
{
    vec3  eps = vec3(.01,0.0,0.0);
    vec3 nor;
    nor.x = map(pos+eps.xyy) - map(pos-eps.xyy);
    nor.y = map(pos+eps.yxy) - map(pos-eps.yxy);
    nor.z = map(pos+eps.yyx) - map(pos-eps.yyx);
    return normalize(nor);
}

vec2 rot(vec2 p, float a)
{
	float c = cos(a);
	float s = sin(a);
	return vec2(p.x * c - p.y * s, p.x * s + p.y * c);
}

void main( void ) {
	vec2 uv = -inputColour.x + 2.0 * (gl_FragCoord.xy / resolution.xy );
	vec3 pos = vec3(0,-3.0,2.0 + time);
	vec3 dir = normalize(vec3(uv * vec2(resolution.x / resolution.y, -1), 1));
	dir.yz = rot(dir.yz, -inputColour.y);
	dir.xz = rot(dir.xz, inputColour.z);
	dir.yz = rot(dir.yz, inputColour.w);
	float t = 0.0;
	for(int i = 0 ; i < 70; i++)
	{
		float k = map(dir * t + pos);
		if(k < 0.01) break;
		t += k;
	}
	vec3 ip = pos + dir * t;
	vec3 L    = normalize(vec3(1.3,-1.2,1));
	vec3 sC   = vec3(3,2,1);
	vec3 sky  = pow(max(0.7, dot(L, dir)), 128.0) * sC + (pow(max(0.7, dot(L, dir)), 8.0) * sC) * 0.1;
	
	vec3 c    = vec3(0);
	if(t > 100.0) {
		c *= 0.7;
		
	} else {
		c += max(0.0, dot(calcNormal(ip), L)) * vec3(3,2,1) * 0.3;
	}
	
	if(ip.y > 0.0) {
		c += vec3(t) * 0.001 * vec3(1,2,3);
	} else {
		c = sky;
	}
	c += sky;
	c += (c / abs(dir.y)) * mouse.y;
	gl_FragColor = vec4(c.xyzz);
}


