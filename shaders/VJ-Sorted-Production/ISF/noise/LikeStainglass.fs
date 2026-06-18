/*{
    "DESCRIPTION": "LikeStainglass",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "noise"
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
        "noise"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif
//Is this jazz?

float hash(float x)
{
	return fract((sin(x*3424.525)+824.5234)*435.551);
}

float noise(float x)
{
	float a = hash(floor(x));
	float b = hash(ceil(x));
	float t = fract(x);
	return a*(1.0-t) + b*t;
}

void main( void ) {

	vec2 p = gl_FragCoord.xy / resolution;
	p.x *= resolution.x/resolution.y;
	vec3 color = vec3(0.0);
	
	int id = 0;
	for (int i=0; i<10; i++)
	{
		float h = sin(4.0*noise(p.x + (time*.6) + noise(float(i))*234.42)+sin(hash((p.x*200.)+(time+p.y))*.004));
		if (p.y < h) id++;
	}
	
	color.r = sin(float(id)*23.423);
	color.g = sin(float(id)*99.123);
	color.b = sin(float(id)*11.1313);

	gl_FragColor = vec4( color, 1.0 );

}
