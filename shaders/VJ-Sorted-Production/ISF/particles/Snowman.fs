/*{
    "DESCRIPTION": "Snowman",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "particles"
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
        "particles"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float smin(float a, float b, float k) {
	float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
	return mix(b, a, h) - k * h * (1.0 - h);
}

float map(vec3 p) {
	float d = 10.0;
	float s0 = length(p) - 0.5;
	float s1 = length(p - vec3(0.0, 0.75, 0.0)) - 0.23;
	float s2 = length(p - vec3(0.11, 0.8, 0.18)) - 0.05;
	float s21 = length(p - vec3(0.11, 0.8, 0.13)) - 0.03;
	float s3 = length(p - vec3(-0.11, 0.8, 0.18)) - 0.05;
	float s31 = length(p - vec3(-0.11, 0.8, 0.13)) - 0.03;
	float s4 = length(p - vec3(0.0, 0.7, 0.2)) - 0.04;
	d = min(d, smin(s0, s1, 0.2));
	d = max(d, -s2);
	d = max(d, -s3);
	d = smin(d, s21, 0.01);
	d = smin(d, s31, 0.01);
	d = smin(d, s4, 0.01);
	return d;
}

vec3 calcNormal(vec3 p) {
	vec2 e = vec2(-1.0, 1.0) * 0.0001;
	return normalize(
		e.xyy * map(p + e.xyy) +
		e.yxy * map(p + e.yxy) +
		e.yyx * map(p + e.yyx) +
		e.xxx * map(p + e.xxx)
	);
}

void main( void ) {
	vec2 p = ( gl_FragCoord.xy / resolution.xy );
	p = 2.0 * p - 1.0;
	p.x *= resolution.x / resolution.y;
	
	vec3 ro = vec3(0.0, 2.0, 3.0);
	vec3 ta = vec3(0.0, 0.0, 0.0);
	vec3 cw = normalize(ta - ro);
	vec3 up = vec3(0.0, 1.0, 0.0);
	vec3 cu = normalize(cross(cw, up));
	vec3 cv = normalize(cross(cu, cw));
	vec3 rd = normalize(p.x * cu + p.y * cv + 2.5 * cw);
	
	float e = 0.0001;
	float t = e * 2.0;
	float h = 0.0;
	for(int i = 0; i < 60; i++) {
		if(t < e || t > 20.0) continue;
		h = map(ro + rd * t);
		t += h;
	}
	
	float col = 1.0 / length(p) * 0.2;
	if(t < 20.0) {
		vec3 pos = ro + rd * t;
		vec3 nor = calcNormal(pos);
		vec3 lig = normalize(vec3(1.0, 3.0, 1.0));
		float dif = clamp(dot(lig, nor), 0.0, 1.0);
		float spe = pow( clamp(dot(reflect(lig, nor), rd), 0.0, 1.0), 32.0);
		float bak = clamp(0.5 + 0.5 * dot(rd, nor), 0.0, 1.0);
		col = dif + spe * 0.1 + bak;
		
	}

	gl_FragColor = vec4( vec3( col ), 1.0 );

}
