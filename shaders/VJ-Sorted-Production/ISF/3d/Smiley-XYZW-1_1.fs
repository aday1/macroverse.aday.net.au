/*{
    "DESCRIPTION": "Smiley-XYZW-1",
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/
#define E 2.71828182846

uniform vec4 color;



#define PI 3.14159265358979


#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable
// PI handled below

uniform vec4 inputColour;

float rand(float a, float b)
{
    return fract(sin(dot(vec2(a, b) ,vec2(12.9898,78.233))) * 43758.5453);
}

void main( void ) {

	vec2 pos = ( gl_FragCoord.xy - resolution.xy / 2.) / resolution.y + mouse - 0.5;
	
	vec3 color = vec3(inputColour.x, inputColour.y, inputColour.z);
	
	for(int i = 0; i < 200; i++)
	{
		float fi = float(i);
		float t  = pow(fract(time * 2.), 0.5);
		float t2 =     floor(time * 2.);
		float a = float(i) / PI;
		float len = 0.3 + (1. - t) * inputColour.w * rand(fi, t2);
		vec2 p = pos + vec2(cos(a), sin(a)) * len;
		float intencity = pow(0.003 / length(p), 1.5);
		color += intencity * vec3(rand(fi, 1.), rand(fi, 2.), rand(fi, 3.));
	}
	for(int i = 0; i < 20; i++)
	{
		float fi = float(i);
		float t  = pow(fract(time * 2.), 0.1);
		float t2 =     floor(time * 2.);
		float a = float(i - 10) / PI / 5.;
		float len = 0.3 + (1. - t) * 0.2 * rand(fi, t2);
		vec2 p = pos + vec2(sin(a), cos(a)) * len;
		p += vec2(0, -0.1);
		float intencity = pow(0.003 / length(p), 1.5);
		color += intencity * vec3(rand(fi, 1.), rand(fi, 2.), rand(fi, 3.));
	}
	for(int i = 0; i < 20; i++)
	{
		float fi = float(i);
		float t  = pow(fract(time * 2.), 0.1);
		float t2 =     floor(time * 2.);
		float a = float(i - 10) / PI;
		float len = 0.04 + (1. - t) * 0.2 * rand(fi, t2);
		vec2 p = pos + vec2(sin(a), cos(a)) * len;
		p += vec2(-0.13, -0.05);
		float intencity = pow(0.003 / length(p), 1.5);
		color += intencity * vec3(rand(fi, 1.), rand(fi, 2.), rand(fi, 3.));
	}
	for(int i = 0; i < 20; i++)
	{
		float fi = float(i);
		float t  = pow(fract(time * 2.), 0.1);
		float t2 =     floor(time * 2.);
		float a = float(i - 10) / PI;
		float len = 0.04 + (1. - t) * 0.2 * rand(fi, t2);
		vec2 p = pos + vec2(sin(a), cos(a)) * len;
		p += vec2(0.13, -0.05);
		float intencity = pow(0.003 / length(p), 1.5);
		color += intencity * vec3(rand(fi, 1.), rand(fi, 2.), rand(fi, 3.));
	}
	color += vec3(fract(pos.y * 20. + time) < 0.5) * 0.05;
	color += vec3(fract(pos.y * 20. - time) < 0.5) * 0.05;
	gl_FragColor = vec4(color, 1.0);

}
