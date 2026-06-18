/*{
    "DESCRIPTION": "CheckerMode7-1",
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
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// http://glslsandbox.com/e#29548.0

#ifdef GL_ES
precision mediump float;
#endif

#define PI 3.14159265359
#define clamps(x) clamp(x,0.,1.)
vec2 magnify(vec2 p, float x, float y) {
	if (y < 0.)
		return p * (1. + x);
	else
		return p * (1. - x);
}

float rand(vec2 co){
    return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

void main( void ) {

	vec2 position = 2. * (( gl_FragCoord.xy / resolution.xy ) - 0.5);
	position.x *= resolution.x / resolution.y;
	
	vec2 pos;
	pos.x = position.x / abs(position.y);
	pos.y = tan(mix(PI/2.0, PI/4.0, abs(position.y)));
	
	pos = magnify(pos, 0.025 * sin(time * 6. * PI), position.y);
	pos.x += 0.025 * cos(time * 3. * PI);
	pos.y += time * 3.;
	
	float tile = mod(floor(pos.x) + floor(pos.y), 2.0);
	
	vec3 finalColor;
	finalColor += (0.7 + 0.3 * tile) * vec3(1., 1., 0.) * step(position.y, 0.);
	finalColor = clamps(finalColor);
	finalColor += vec3(.8,.5,1.) * vec3(position.y) * step(0., position.y);
	
	gl_FragColor = vec4( finalColor, 1.0 );

}
