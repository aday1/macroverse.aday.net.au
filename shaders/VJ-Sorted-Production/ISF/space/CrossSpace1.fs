/*{
    "DESCRIPTION": "CrossSpace1",
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
        "space",
        "geometric"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// http://glslsandbox.com/e#28869.0
// Cross space
#ifdef GL_ES
precision mediump float;
#endif

vec2 rot(vec2 p, float t) {
	float s = sin(t);
	float c = cos(t);
	return p * mat2(c, -s, s, c);
}

float map(vec3 p) {
	float width = 0.01;
	
	float t = 1000.0;
	vec3 tp = p;

	t = min(t, length(mod(tp.xz, 11.0) - 0.5) - width);
	t = min(t, length(mod(tp.zy, 1.0) - 0.5) - width); 
	t = min(t, length(mod(tp.xy, 1.0) - 0.5) - width);

	t = max(t, (length(mod(p, 1.0) - 0.5) - 0.3));

	return t;
}

void main( void ) {
	vec2 uv = ( gl_FragCoord.xy / resolution.xy ) * 2.0 - 1.0;
	uv.x *= resolution.x / resolution.y;
	vec3 dir = normalize(vec3(uv, 1.0));
	dir.xz = rot(dir.xz, time * 0.1);
	dir.zy = rot(dir.zy, time * 0.2);
	vec3 pos = vec3(time, 0, time);
	float t = 0.0;
	float temp = 0.0;
	
	for(int i = 0; i < 100; i++) {
		temp = map(dir * t + pos);
		if(temp < 0.01) break;
		t += temp;
	}
	vec3 ip = pos + dir * t;
	vec3 col = 1.0 - vec3(t * 0.05) - temp + map(ip + 0.1);
	col += (1.0 - t) * 0.05;
	col *= vec3(1,2,3) * 0.7;
	gl_FragColor = vec4(col + dir * 0.3, 1.0);

}
