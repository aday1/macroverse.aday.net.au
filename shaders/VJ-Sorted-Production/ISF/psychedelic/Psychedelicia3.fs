/*{
    "DESCRIPTION": "Psychedelicia3",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "psychedelic"
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
        "psychedelic"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

//#extension GL_OES_standard_derivatives : enable

void main( void ) {

	vec2 p = ( gl_FragCoord.xy - 0.5 / resolution.xy ) / resolution.y; 

	p.x += 0.1 * sin(p.y * 3.14 + time);
	p.x = sin(p.x * 5.0);
	p.y = sin(p.y * 5.0);
	
	//	float g = distance(vec2(0.0,0.0), p);
	float g = distance(vec2(sin(time*0.1)), p);
	float d = sin(g * 3.14 * 5.0);
	
	vec3 ca = vec3(1.0, 0.0, 0.0);
	vec3 cb = vec3(0.0, 0.0, 1.0);
	vec3 cc = vec3(1.0, 1.0, 0.0);
	vec3 cd = vec3(1.0, 0.0, 1.0);
	
	float k = d * 0.5 + 0.5; //k가 d에 의해서 바뀌는 부분이 중요한 부분.
	vec3 c = vec3(0.0);
	
	c = mix(ca, cb, smoothstep(0.0, 0.5, k));
	c = mix(c, cc, smoothstep(0.5, 0.75, k));
	c = mix(c, cd, smoothstep(0.75, 1.0, k));

//	c = c * vec3(1.0) * sin(g * 3.14 +5.0);

	//지그재그만들기
	
	gl_FragColor = vec4( vec3(c), 1.0);

}
