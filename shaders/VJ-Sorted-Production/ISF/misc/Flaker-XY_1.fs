/*{
    "DESCRIPTION": "Flaker-XY",
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
        }
    ],
    "TAGS": [
        "misc"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif
 
// modified by @hintz
// modified by @quilime 

#define PI 3.14159
#define TWO_PI (PI * 2.0)
#define N 5.0
 
vec3 hsv2rgb(vec3 c)
{
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}
 
void main(void) 
{
	vec2 v = (gl_FragCoord.xy - resolution) / min(resolution.y,resolution.x) * 20.0;
 
	vec3 col = vec3(0.0, 0.0, 0.0);
 
	for(float i = 0.0; i < N; i++) 
	{
	  	float a = i * (TWO_PI/N);
		float v = cos(TWO_PI*(v.x * cos(a) + v.y * sin(a)+ mouse.y +mouse.x + sin(time*0.01)*100.0 ));
		col += hsv2rgb(vec3(a, 0.5, v));
	}
	
	 col /= 5.0;
 
	gl_FragColor = vec4(col.xyz, 1.0);

}

