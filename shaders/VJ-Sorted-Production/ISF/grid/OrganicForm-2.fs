/*{
    "DESCRIPTION": "OrganicForm-2",
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
#ifdef GL_ES
precision mediump float;
#endif

/* original: http://glslsandbox.com/e#5891 */

const float Tau		= 6.2832;
const float Speed	= .05;
const float Density	= 0.1;
const float Shape	= .02;

float random( vec2 seed ) {
	return fract(sin(seed.x+seed.y*1e3)*1e5);
}

float Cell(vec2 coord) {
	vec2 cell = fract(coord) * vec2(.5,2.) - vec2(.0,.5);
	return (1.-length(cell*2.-1.))*step(random(floor(coord)),Density)*2.;
}

void main( void ) {
	
		vec2 position = ( gl_FragCoord.xy / resolution.xy ) + mouse / 6.0;
	
	float px = position.x;//gl_FragCoord.x / resolution.x - 0.5 - (sin(time) * 0.1);
    float py = position.y;//gl_FragCoord.y / resolution.y - 0.5 - (cos(time) * 0.1);
	
    vec2 p = vec2(px, py);
	
	float a = fract(atan(px, py) / Tau);
	float d = sqrt(px*px + py*py);
	
	vec2 coord = vec2(pow(d, Shape), a)*256.;
	vec2 delta = vec2(-time*Speed*256., .5);
	
	float c = 0.;
	for(int i=0; i<4; i++) {
		coord += delta;
		c = max(c, Cell(coord));
	}
	
    float f = c*d - length(p) + 0.8;
	gl_FragColor = vec4(0., f, f, 1.0);
}
