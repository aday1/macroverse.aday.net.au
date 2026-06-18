/*{
    "DESCRIPTION": "InverseSphereEvolution",
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
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

float hash (float n) {
	return fract(sin(n) * 43758.5453);
}
 
float noise (vec2 x) {
    vec2 p = floor(x);
    vec2 f = fract(x);
    f = f*f*(3.0-2.0*f);
    float n = p.x + p.y*57.0;
    float res = mix(mix( hash(n), hash(n + 1.0), f.x), mix( hash(n + 57.0), hash(n + 58.0), f.x), f.y);
    return res;
}
 
mat2 m = mat2(0.40,  0.50, -0.10,  0.70);
 
float fbm (vec2 p) {
	float f = 0.0;
	f += 0.5000 * noise(p);
	f += 0.2500 * noise(m*p*2.0);
	f += 0.1250 * noise(m*p*4.0);
	f += 0.0625 * noise(m*p*8.0);
	return f;
}
 
void main(void) {
	vec2 p = gl_FragCoord.xy / resolution;
	p = 2.0 * p - 1.0;
	p.x *= resolution.x / resolution.y;
	vec3 col = vec3(0.0);
	float r = length(p);
	col = vec3(0.0, 0.5, 0.6);
	float a = atan(p.y * p.y, p.x);
	float f = smoothstep(0.4, 0.8, fbm(vec2(20.0 * a + 3.0 * time, 0.8 * r - time)));
	col = mix(col, vec3(0.0, 1.0, 1.0), f);
	col = mix(col, vec3(0.0), smoothstep(0.0, 4.0, r));
	col = mix(col, vec3(1.0), 1.0 - smoothstep(0.0, 1.0 + cos(time), r));

	gl_FragColor = vec4(vec3(col), 1.0);
}

