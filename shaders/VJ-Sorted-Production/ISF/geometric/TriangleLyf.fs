/*{
    "DESCRIPTION": "TriangleLyf",
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

#define TAU 6.283185307179586476925286766559
#define TAU3RD 2.09439510239319549230842891243194182739187391849102837418471928321863172
#define PIDIV2 1.57079632679489661923132169163981102938461918274019283049184092746583078
//THIS IS THE R.G.B TRIANGLE
float sigmoid(float x, float a) {
    float b = pow(x*2.,a)/2.;
    if (x > .5) {
        b = 1.-pow(2.-(x*2.),a)/2.;
    }
	return b;
}
vec2 rotate(float rot, vec2 uv) {
	return (mat2(cos(rot),-sin(rot),sin(rot),cos(rot))*uv);
}
vec3 barycentric(vec2 a, vec2 b, vec2 c, vec2 p)
{
    float d = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
    float alpha = ((b.y - c.y) * (p.x - c.x)+(c.x - b.x) * (p.y - c.y)) / d;
    float beta = ((c.y - a.y) * (p.x - c.x) + (a.x - c.x) * (p.y - c.y)) / d;
    float gamma = 1.0 - alpha - beta;
    return vec3(alpha, beta, gamma);
}
vec3 inRange3(vec3 p)
{
    return step(p, vec3(1.0)) * step(vec3(0.0), p);
}
float inRangeAll(vec3 p)
{
    vec3 r = inRange3(p);
    
    return r.x * r.y * r.z;
}
void main(void)
{
	vec2 uv = (gl_FragCoord.xy - resolution.xy / 2.0) / min(resolution.x, resolution.y)*3.;

	float angle = sigmoid(fract(time/3.),4.)*TAU;
	
	vec2 pos0 = vec2(cos(TAU3RD+PIDIV2),sin(TAU3RD+PIDIV2));
	pos0 = rotate(angle,pos0);
	vec2 pos1 = vec2(cos((TAU3RD*2.)+PIDIV2),sin((TAU3RD*2.)+PIDIV2));
	pos1 = rotate(angle,pos1);
	vec2 pos2 = vec2(cos((TAU3RD*3.)+PIDIV2),sin((TAU3RD*3.)+PIDIV2));
	pos2 = rotate(angle,pos2);
	
	vec3 bc = barycentric(pos0, pos1, pos2, uv);
	float val = inRangeAll(bc);
	
	gl_FragColor = vec4((bc*val)+(step(1.,length(uv))),1.);
}

