/*{
    "DESCRIPTION": "SolidFluidSpace",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        "abstract",
        "space"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float hash( float n )
{
    return fract(sin(n)*475458.5453);
}

float noise( in vec3 x )
{
    vec3 p = floor(x);
    vec3 f = fract(x);

    f = f*f*(3.0-2.0*f);

    float n = p.x + p.y*57.0 + 113.0*p.z;

    float res = mix(mix(mix( hash(n+  0.0), hash(n+  1.0),f.x),
                        mix( hash(n+ 57.0), hash(n+ 58.0),f.x),f.y),
                    mix(mix( hash(n+113.0), hash(n+114.0),f.x),
                        mix( hash(n+170.0), hash(n+171.0),f.x),f.y),f.z);
    return res;
}

float map(vec3 p) {
	//return length(mod(p, 10.0) - 5.0) - 4.5 + noise(p * 5.0) * 0.1;
	float j = noise(p * 1.0);
	return 0.9 - j;
}

vec2 getuv(vec3 p) {
	const float pi = 3.141592653;
	float u = 0.5 + atan(p.z, p.x) / (2.0 * pi);
	float v = 0.5 - asin(p.y) / pi;
	return vec2(u, v);
}

void main( void ) {
	vec2 uv = ( gl_FragCoord.xy / resolution.xy ) * 2.0 - 1.0;
	uv.x *= resolution.x / resolution.y;

	vec3 dir = normalize(vec3(uv, 1.0));
	float dist = 2.0;
	vec3 pos = vec3(time,0,time);
	
	float t = 0.0;
	float temp = 0.0;
	for(int i = 0; i < 256; i++) {
		temp = map(dir * t + pos) * 0.15;
		if(temp < 0.01) break;
		t += temp;
	}
	vec3 ip = dir * t + pos;
	gl_FragColor = vec4(vec3(t * 0.05) + dir * 0.3, 1.0) + map(ip - 0.3) - temp;

}
