/*{
    "DESCRIPTION": "LightShineSpark",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "particles"
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
        "particles"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

varying vec2 surfacePosition;

float ratio = resolution.x/resolution.y;

//return sin in range 0.0 to 1.0 instead of -1.0 to 1.0
float sin2(float a) {
	return pow(sin(a*.5),2.);
}
float karo(float angle) {
	return step(.1,sin2(angle));
}
float explosion(float angle) {
	return step(.75+sin(time)*0.15,sin2(angle));
}
void main() {
	vec2 p = surfacePosition*3.0;
	float c = 0.;
	float b = 0.;
	float a = atan(p.x,p.y);
	float r = length(p);
	c = explosion(a*(10.)*1.5+time*1.);
	b = explosion(a*(5.)*20.5+time*10.5);
	
	vec2 pp = vec2(0.,0.); pp.x *= ratio;
	float dist = distance( pp, surfacePosition );
	float heat = (.1/ dist);

        float tmp = pow(heat,3.)*b*c+heat;
	tmp = min(max(tmp, 0.0), 1.0);
	vec3 cc = vec3(tmp);
	gl_FragColor = vec4(cc,1)+vec4(0.2,0.4,0.7,0.0);
}
