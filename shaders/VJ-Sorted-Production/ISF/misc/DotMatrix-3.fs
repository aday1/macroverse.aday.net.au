/*{
    "DESCRIPTION": "DotMatrix-3",
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
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

float abs_dist(vec2 a, vec2 b){
	vec2 d = abs(b - a);
	return abs(abs(max(d.x,d.y)-50.)-50.);
}

void main() {

	vec2 position = gl_FragCoord.xy;
	
//	vec2 point0 = vec2(100.0, 100.0);
	vec2 point0 = mouse * resolution;

	vec2 point1 = mouse.y * resolution;
	vec2 point2 = mouse.x * resolution;

	// vec2 point2 = vec2(200.0, 150.0);
	
	float dist = 1e20;
	
	//dist = min(dist, abs_dist(position, point0));
	dist = min(3000., abs_dist(position, point1));
	dist = min(dist, abs_dist(position, point2));
	
	dist *= 0.01;
	
	float f = floor(dist*30.0)/30.0;

	gl_FragColor = vec4(f);

}
