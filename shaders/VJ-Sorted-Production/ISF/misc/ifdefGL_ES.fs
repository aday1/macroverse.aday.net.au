/*{
    "DESCRIPTION": "ifdefGL_ES",
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
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
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

uniform vec4 inputColour;

void main( void ) {
	
	float t = time*time*2.;
	vec2 uv = gl_FragCoord.xy / t;
	vec2 pos = vec2((resolution.x-t*1.45)/t-500./t, (resolution.y/t)*.5);
	vec2 p0 = pos - uv;
	
	vec2 p = vec2(0.);
	
	float c = 0.;
	
	for (int i = 0; i < 256; i++)
	{
		if (length(p*sin(time)) > 4.)
			break;
		float _x = p.x*p.x - p.y*p.y + p0.x;
		p.y = 2. * p.x * p.y + p0.y;
		p.x = _x;
		c+= 1./256.;	
	}

	gl_FragColor = vec4(fract(c), sin(atan(p.y,p.x)*c)*sin(time)*2., c*(2.-c), 1.);
}
