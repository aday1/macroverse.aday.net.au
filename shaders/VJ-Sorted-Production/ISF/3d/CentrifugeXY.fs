/*{
    "DESCRIPTION": "CentrifugeXY",
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

#define PI 3.14159265359
#define TWO_PI 6.28318530718

void main( void ) {
	vec2 p;
	//get center
	vec2 uv = vec2(gl_FragCoord.x / resolution.x, gl_FragCoord.y / resolution.y);
	uv -= 0.5;
	uv /= vec2(resolution.y / resolution.x, 1);
	
	/*p = ( gl_FragCoord.xy / resolution.xy );
	p.x *= resolution.x / resolution.y;
	p = p * 2.0 - 2.0;
	p.y = p.y + 1.0;*/
	p = uv*3.;

	//distance
	vec3 col = vec3(0.0);
	float r = length(p);
	float a = time + atan(p.x, p.y); //angle
	float s = tan(a * 6.0);
	float d = 0.5 + 0.2 * pow(s, mouse.x);

	float f = smoothstep(mouse.y, r+0.1, d);
	//f = .5;
	vec3 background_color = vec3(1., .0, 0.0);
	vec3 object_color = vec3(0.0, mouse.y, 5.0);
	col = mix(background_color, object_color, f);
	
	//vec2 e = vec2(p.x - 0.15, p.y);
	 
	//col = vec3(f);
	gl_FragColor = vec4 ( col, 1.0);

}
