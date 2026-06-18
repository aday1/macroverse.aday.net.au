/*{
    "DESCRIPTION": "mercury",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "water"
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
        "water"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

vec3 de(vec2 p) {
	p *= 0.4;
	p.x += time*0.5;
	p = mod(p + 0.5, 1.0) - 0.5;
	vec3 col = vec3(1);
	for(int i = 0; i < 10; i++) {
		p = abs(p)/max(dot(p, p), 0.1) - vec2(0.1);
		col = min(col, vec3(length(p), abs(cos(p + time))));
	}
	
	return col;
}

float gr(vec2 p) {
	return dot(vec3(0.21, 0.72, 0.07), de(p));
}

vec3 bump(vec2 p, float e, float z) {
	vec2 r = vec2(e, 0.0); vec2 l = r.yx;
	vec3 g = vec3(
		gr(p + r) - gr(p - r),
		gr(p + l) - gr(p - l),
		z);
	return normalize(g);
}

vec3 render(vec2 p) {
	vec3 rd = normalize(vec3(p, 1.97));
	vec3 sn = bump(p, 0.01, -0.7);
	vec3 re = normalize(reflect(rd, sn));
	
	return vec3(gr(p))*pow(clamp(dot(-rd, sn), 0.0, 1.0), 2.0)*de(p)
		+ pow(clamp(dot(-rd, re), 0.0, 1.0), 8.0)
		+ pow(clamp(1.0 + dot(rd, sn), 0.0, 1.0), 2.0);
}

void main( void ) {
	vec2 p = (-resolution + 2.0*gl_FragCoord.xy)/resolution.y;
	gl_FragColor = vec4(render(p), 1);
}
