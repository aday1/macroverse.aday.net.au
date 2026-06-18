/*{
    "DESCRIPTION": "GlyphMirror27",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "misc"
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
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

void main( void ) {
	float size = sin(mod(time / 2., 3.14*2.));
	size *= size / 2.;
	size += 0.01;
	vec2 xy = gl_FragCoord.xy;
	vec2 c = (xy / resolution.xy - 0.5) * 3.14;
	vec2 d = cos(c);
	d = 0.5 / d / d / resolution.x;
	c = tan(c) / 2.0 + time * size;
	vec2 left = (c - d/2.) / size;
	vec2 right = (c + d/2.) / size;
	vec2 m = d / size;
	
	vec2 lv = mod(floor(left), 2.0);
	vec2 rv = mod(floor(right), 2.0);
	vec2 lw = ceil(left) - left;
	vec2 rw = fract(right);
	
	vec2 v = clamp((lv * lw + rv * rw + (right - left - lw - rw) * 0.5) / m, 0., 1.);
	float r = min(v.x + v.y - .5, .0) + min(1.5 - v.x - v.y, .0) + max(v.y - v.x - .5, 0.) + max(v.x - v.y - .5, .0) + .5;
	
	gl_FragColor = vec4(r, r, r, 1.);
}
