/*{
    "DESCRIPTION": "ConvertINtoIlluminati",
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
        "geometric",
        "texture-input"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// how REAL coders make the distance field for a circle

#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D backbuffer;

float triangle(vec2 p, float h) {
	vec2 q = abs(p);
	return max(q.x*0.866025 + p.y*0.5, -p.y) - h*0.5;
}

vec3 hsv(float h,float s,float v) {
	return mix(vec3(1.),clamp((abs(fract(h+vec3(3.,2.,1.)/3.)*6.-3.)-1.),0.,1.),s)*v;
}
vec2 rotate(vec2 p, float a) {
	return vec2(p.x*sin(a) + p.y*cos(a), p.x*cos(a) - p.y*sin(a));
}

void main( void ) {
	vec2 p = gl_FragCoord.xy / resolution.xy * 2.0 - 1.0;
	p.x /= resolution.y / resolution.x;
	p = rotate(p, time*8.0);
	vec4 last = texture2D(backbuffer, gl_FragCoord.xy / resolution.xy);
	if (last.a < 0.001)
		last = vec4(999.999);
	float o = min(last.a, triangle(p, 0.5));
	gl_FragColor = vec4(hsv(o*8.0,1.0,1.0)*smoothstep(0.0, 0.005, o),o);

}
