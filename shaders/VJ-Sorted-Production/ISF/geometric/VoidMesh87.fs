/*{
    "DESCRIPTION": "VoidMesh87",
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
        }
    ],
    "TAGS": [
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
// Shader by Nicolas Robert [NRX]
#ifdef GL_ES
precision mediump float;
#endif

void main () {
	vec2 f_ = (2.0 * gl_FragCoord.xy - resolution.xy) / resolution.y;
	
	vec3 d = normalize (vec3 (f_, 2.0)); vec3 p = vec3 (0.0, 0.0, time * 40.0);
	vec3 f = vec3 (cos (time), sin (time), 3.0 + 5.0 * sin (time * 0.5));
	
	float a = 3.14159 * sin (time * 0.5) * sin (time * 0.2);
	vec3 u = vec3 (sin (a), cos (a), 0.0); 
	mat3 r;
	r [2] = normalize (f);
	r [0] = normalize (cross (u, f));
	r [1] = cross (r [2], r [0]); 
	d = r * d; vec3 q; float l = 0.0;
	for (int s = 0; s < 200; ++s) {
		q = p + vec3 (4.0 * sin (p.z * 0.1), 10.0 * sin (p.z * 0.05) * sin (p.z * 0.01), 0.0);
		float d_ = 20.0 - length (q.xy); l += d_;
		
		if (d_ < 0.01 || l > 250.0)
			break;
		p += d * d_;
	}
	float angle = atan (q.y, q.x);
	gl_FragColor = vec4 (0.5 + sin (q.z * 0.4) * 0.5,0.5 + sin (angle * 8.0) * 0.5, 0.5 + sin (q.z * 0.2) * sin (angle * 4.0) * 0.5, 1.0) * (1.0 - l / 250.0);}
