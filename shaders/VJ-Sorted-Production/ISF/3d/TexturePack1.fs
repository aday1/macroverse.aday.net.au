/*{
    "DESCRIPTION": "TexturePack1",
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
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

/**
  * 0.0 : Circle
  * 1.0 : Background
  */
bool isCircle(vec2 pos, vec2 center, float radius) {
	float d = distance(pos, center);
	return step(radius, d) == 0.0;
}

bool isRect(vec2 pos, vec2 center, float radius) {
	return pos.x > center.x - radius && pos.x < center.x + radius && pos.y > center.y - radius && pos.y < center.y + radius;
}

void main( void ) {
	vec3 circleColor = vec3(0.3, 0.2, 0.8);
	vec3 backgroundColor = vec3(0.7, 0.1, 0.2);
	
	vec2 fStep = vec2(50.0, 50.0);  // distance between circles
	vec2 fPos = mod(gl_FragCoord.xy + vec2(time * 100.0), fStep);
	
	vec3 color =  isCircle(fPos, fStep / 2.0, 20.0) ? circleColor : backgroundColor;
	
	vec2 fStep2 = vec2(30.0, 30.0);
	vec2 fPos2 = mod(gl_FragCoord.xy + vec2(time * 20.0), fStep2);
	color /= isRect(fPos2, fStep2, 15.0) ? vec3(0.2, 0.9, 0.1) : vec3(1.0);
	
	gl_FragColor = vec4(color, 1.0);
}
