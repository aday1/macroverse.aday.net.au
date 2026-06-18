/*{
    "DESCRIPTION": "MeshMode1",
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
        }
    ],
    "TAGS": [
        "geometric"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

void rotate(inout vec2 p, float a) {
	float s = sin(a);
	float c = cos(a);
	
	p = mat2(c, s, -s, c)*p;
}

float len(vec3 p, float k) {
	p = pow(p, vec3(k));
	
	return pow(p.x + p.y + p.z, 1.0/k);
}

float de(vec3 p) {
	vec4 q = vec4(p, 1);
	q.xyz -= 1.0;
	
	for(int i = 0; i < 3; i++) {
		q.xyz = abs(q.xyz + 1.0) - 1.0;
		q /= clamp(dot(q.xyz, q.xyz), 0.25, 1.0);
		q *= 1.3;
		
		rotate(q.yx, time);
		rotate(q.xz, cos(5.0*time));
	}
	
	return min((length(q.xyz) - 1.5)/q.w, p.y + 2.0);
}

float trace(vec3 ro, vec3 rd, float mx) {
	float t = 0.0;
	for(int i = 0; i < 100; i++) {
		float d = de(ro + rd*t);
		if(d < 0.001 || t >= mx) break;
		t += d*0.75;
	}
	return t;
}

vec3 normal(vec3 p) {
	vec2 h = vec2(0.001, 0.0);
	vec3 n = vec3(
		de(p + h.xyy) - de(p - h.xyy),
		de(p + h.yxy) - de(p - h.yxy),
		de(p + h.yyx) - de(p - h.yyx)
	);
	return normalize(n);
}

vec3 render(vec3 ro, vec3 rd) {
	vec3 col = vec3(0);
	const vec3 key = normalize(vec3(0.8, 0.7, -0.6));
	
	float t = trace(ro, rd, 10.0);
	if(t < 10.0) {
		vec3 pos = ro + rd*t;
		vec3 nor = normal(pos);
		vec3 ref = reflect(key, nor);
		
		float sha = step(10.0, trace(pos + nor*0.01, key, 10.0));
		float dom = step(2.0, trace(pos + nor*0.01, ref, 2.0));
		
		col = vec3(0.15, 0.10, 0.3);
		col += vec3(1.64, 0.99, 0.15)*clamp(dot(nor, key), 0.0, 1.0)*sha;
		col += vec3(1.64, 0.55, 0.4)*clamp(dot(nor, -key), 0.0, 1.0);
		col += clamp(1.0 + dot(nor, rd), 0.0, 1.0)*dom;
		col += clamp(dot(ref, nor), 0.0, 1.0)*dom;
	}
	
	return col;
}

void main( void ) {
	vec2 p = (-resolution + 2.0*gl_FragCoord.xy)/resolution.y;
	
	vec3 ro = vec3(0, 0, -5);
	vec3 ww = normalize(vec3(0, -1, 0)-ro);
	vec3 uu = normalize(cross(vec3(0, 1, 0), ww));
	vec3 vv = normalize(cross(ww, uu));
	vec3 rd = normalize(p.x*uu + p.y*vv + 1.97*ww);
	
	vec3 col = render(ro, rd);
	
	col = pow(abs(col), vec3(1.0/2.2));
	
	gl_FragColor = vec4(col, 1);
}
