/*{
    "DESCRIPTION": "TheCloudsDONTMove",
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
precision highp float;

//#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
//#define resolution iResolution

#define MAX_ITER 20

float remap(float value, float l1, float h1, float l2, float h2) {
  return l2 + (value - l1) * (h2 - l2) / (h1 - l1);
}

float mapRed(float r) {
//  r = remap(r, 0.0, 1.0, 0.0, 1.0);
  return r;
}

float mapGreen(float g) {
//  g = remap(g, 0.0, 1.0, 0.0, 1.0);
  return g;
}

float mapBlue(float b) {
 //b = remap(b, 0.0, 1.0, 0.0, 0.9);
  return b;
}

void main( void ) {
	vec2 m = vec2((2.0*mouse.x-1.0)*resolution.x,(2.0*mouse.y-1.0)*resolution.y)/min(resolution.x,resolution.y);
	vec2 p = 10.0*((2.0*gl_FragCoord.xy-resolution)/min(resolution.x,resolution.y)) - 30.0*vec2(0.5,0.0);
	vec2 i = p;
	float scaleTime = time * 0.005 + 1000.0;

	float c = 1.0;
	float inten = .05;

	for (int n = 0; n < MAX_ITER; n++){
		float t = -scaleTime * (1.5 - (10.0 / float(n+1)));
    //i[1] = i[1] + (100.0 * sin(scaleTime * 0.1 + 1000.0));
    //i.y = i.y + scaleTime * 10.0;
		i = p + vec2(
      cos(t - i.x) + cos(t - i.y),
      sin(t - i.y) + cos(t + i.x)
    );
		c += 1.0/length(vec2(p.x / (cos(i.y + t)/inten)));
	}
	c /= float(MAX_ITER);
	//c = 1.2-sqrt(pow(c,3.0));
	float col = c*c*c*c*c;
	gl_FragColor = vec4(mapRed(col), mapGreen(col), mapBlue(col), 1.0);
}
