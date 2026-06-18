/*{
    "DESCRIPTION": "PlasmaWave-10",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "plasma"
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
        "plasma"
    ]
}*/#define PI 3.14159265359





#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#define PI 3.1415926535897932384626433832795
 
#endif

vec2 uk = vec2(79.0, 79.97);

vec3 plasma(vec2 c, float v, vec2 position, int color)
{
	v += sin((c.x+time));
        v += sin((c.y+time)/2.0);
        v += sin((c.x+c.y+time)/2.0);
        c += position/2.0 * vec2(sin(time/3.0), cos(time/2.0));
        v += sin(sqrt(c.x*c.x+c.y*c.y+1.9)+time*2.0);
        v = v/2.0;
	if (color == 1) {
	return vec3(1.0, sin(PI*v), cos(PI*v));
	}
	if (color == 0) {
	return vec3(0.0, cos(PI*v), sin(PI*v));
	}
}

void main( void ) {
	vec2 pos = (gl_FragCoord.xy / resolution.xy * 2.0 - 1.0);
	vec2 position = ( gl_FragCoord.xy / resolution.xy * 2.0 - 1.0 );
	position.x *= resolution.x / resolution.y;
	position.y += sin(time)*0.05;
	position.x += cos(time)*0.7;
        pos.x += sin(time)*0.35;
	pos.y += cos(time)+0.8;
	vec2 p;
	float r, a;
	r = sqrt(length(pow(position, vec2(1.0,1.0))));
	a = atan(position.y, position.x) / 5.003;
	p.x = 0.5 / r + time*0.5;
	p.y = a+time*0.2;
	vec3 color = plasma(p*uk, 0.0, uk, 0) * min(1.0, (pow(r, 3.05)));
	gl_FragColor = vec4( color, 1.0 );

}
