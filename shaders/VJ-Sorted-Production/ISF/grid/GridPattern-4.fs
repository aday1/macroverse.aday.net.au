/*{
    "DESCRIPTION": "GridPattern-4",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
        "grid"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float checker(vec2 p, float x, float y){
	float ret = 0.5;
	if((p.x < x && p.x > x - 0.1) && (p.y < y && p.y > y - 0.1) || (p.x > x && p.x < x + 0.1) && (p.y > y && p.y < y + 0.1)){
		ret = 1.0;
	}else if((p.x > x && p.x < x + 0.1) && (p.y < y && p.y > y - 0.1) || (p.x < x && p.x > x - 0.1) && (p.y > y && p.y < y + 0.1)){
		ret = 0.0;
	}
	return ret;
}

float rot_checker(vec2 p, float x, float y){
	float ret = 0.5;
	y = mod(x, 0.2);
	if((p.x < x && p.x > x - 0.1) && (p.y < y && p.y > y - 0.1) || (p.x > x && p.x < x + 0.1) && (p.y > y && p.y < y + 0.1)){
		ret = 1.0;
	}else if((p.x > x && p.x < x + 0.1) && (p.y < y && p.y > y - 0.1) || (p.x < x && p.x > x - 0.1) && (p.y > y && p.y < y + 0.1)){
		ret = 0.0;
	}
	return ret;
}

void main( void ) {
	vec2 p = gl_FragCoord.xy / resolution.xy;
	p.x *= resolution.x / resolution.y;
	mat3 a = mat3(
		cos(time),-sin(time),0,
		sin(time),cos(time),0,
		0,0,0
	);
	
	float fcol = 0.5;

	p = (a * vec3(p, 1.0)).xy;
	fcol = rot_checker(mod(p, 0.7), 0.5, 0.0);

	gl_FragColor = vec4(fcol);

}
