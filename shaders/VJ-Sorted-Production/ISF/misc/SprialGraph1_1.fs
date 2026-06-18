/*{
    "DESCRIPTION": "SprialGraph1",
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
 // time
 // resolution
 // mouse

void main(void){
	
	float t = time;
	vec2 r = resolution;
	
    	vec2 p = (gl_FragCoord.xy * 2.0 - r) / min(r.x, r.y);
	vec3 destColor = vec3(mouse.x, mouse.y, 0.7);
	float f = 0.0;
    	for(float i = 0.0; i < 20.0; i++){
        float s = sin(t + i * 0.314) * 0.5;
        float c = cos(t + i * 0.314) * 0.5;
        f += 0.0025 / abs(length(p + vec2(c, s)) - 1.5*abs(sin(1.3 * t)));
    	}
    	gl_FragColor = vec4(vec3(destColor * f), 1.0);
}
