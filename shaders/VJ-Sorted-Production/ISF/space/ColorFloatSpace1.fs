/*{
    "DESCRIPTION": "ColorFloatSpace1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "space"
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
        "space"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float tmn(vec3 p) {
	return fract( abs(sin(p.x * 12356.0) + cos(7890.0 * p.z) - cos(27450.0 * p.y)) );
}

float mn(vec3 p) {
	float g = 0.0;
	for(int i = 1 ; i < 8; i++) {
		g += tmn(p * float(i));
	}
	g /= 2.0;
	return g;
}

vec2 rot(vec2 p, float t) {
	float c = cos(t);
	float s = sin(t);
	return vec2(
		p.x * c - s * p.y,
		p.x * s + c * p.y);
}

float map(vec3 p) {
	float nz = mn(p * 0.00005) * 0.1;
	float t = length(mod(p, 2.0) - 1.0) - 0.5 + nz;
	float t2 = length(mod(p.yz, 4.0) - 2.0) - 1.0 + nz;

	t = min(t, cos(p.x) * cos(p.y) + nz);
	t = max(t2, t);
	return t;
}

void main( void ) {
	vec2 uv = ( gl_FragCoord.xy / resolution.xy ) * 2.0 - 1.0;
	vec2 yuv = uv;
	uv.x *= resolution.x / resolution.y;
	vec3 dir = normalize(vec3(uv * vec2(1,-1), 1.0));
	dir.xy = rot(dir.xy, time * 0.1);
	dir.yz = rot(dir.yz, time * 0.1);
	vec3 pos = vec3(time, 0, time);
	float t = 0.0;
	for(int i = 0 ; i < 256; i++) {
		float temp = map(dir * t + pos);
		if((temp) < 0.01) break;
		t += temp * 0.4;
	}
	vec3 ip = dir * t + pos;
	vec3 c = vec3(t * 0.01) + map(ip - 0.02) + vec3(abs(fract(ip * 0.05)));
	
	c += vec3(t * 0.03);
	gl_FragColor = vec4(c + dir * 0.03, 1.0) * (1.0 - dot(yuv, yuv) * 0.5);

}
