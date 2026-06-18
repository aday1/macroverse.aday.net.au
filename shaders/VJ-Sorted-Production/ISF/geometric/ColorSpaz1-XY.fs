/*{
    "DESCRIPTION": "ColorSpaz1-XY",
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

#define PI 3.1415927
#define PI2 (PI*2.0)

// http://glslsandbox.com/e#7109.0 simplified by logos7@o2.pl
// different colors by @hintz

void main(void)
{
	vec2 position = mouse.y * 64.0 * ((2.0 * gl_FragCoord.xy - resolution) / resolution.xx);

	float r = length(position);
	float a = atan(position.x, position.y) + PI;
	float d = r - a + PI2;
	float n = PI2 * float(int((d / PI2)));
	float da = a + n;
	float pos = (PI*0.02*mouse.x*4.0*da * da - time);
	vec3 norm;
	norm.xy = vec2(fract(pos) - .5, fract(d / PI2) - .5)*2.0;
	pos = floor(pos);
	float len = length(norm.xy);
	vec3 color = vec3(0.0);
	if (len < 1.0)
	{
		norm.z = sqrt(1.0 - len*len);
		vec3 lightdir = normalize(vec3(0.0, 1.0, 1.0));
		float dd = dot(lightdir, norm);
		dd = max(dd, 0.25);
		color = dd*vec3(0.5*cos(pos+2.0)+0.5, 0.5*cos(pos+4.0)+0.5, 0.5*cos(pos)+0.5);
		vec3 halfv = normalize(lightdir + vec3(0.0, 0.0, 1.0));
		float spec = dot(halfv, norm);
		spec = max(spec, 0.0);
		spec = pow(spec, 40.0);
		color += vec3(spec);
	}
	
	gl_FragColor.rgba = vec4(color,1.0);
}
