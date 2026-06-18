/*{
    "DESCRIPTION": "Shapey-XYZW",
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/
#define E 2.71828182846

varying vec2 position;

uniform vec4 color;
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

uniform vec4 inputColour;

// mouse.x
// mouse.y
// inputColour.x
// inputColour.y
// inputColour.z
// inputColour.w

#define CLEAR_COLOR 0.0

vec3 rect(vec2 pos, float x, float y, float w, float h, vec3 color) {
	vec3 temp = vec3(CLEAR_COLOR, CLEAR_COLOR, CLEAR_COLOR);
	
	if(pos.x > x && pos.x < x + w && pos.y > y && pos.y < y + h) {
		temp = color;
	}
	
	return temp;
}

vec3 circle(vec2 pos, float x, float y, float radious, vec3 color) {
	vec3 temp = vec3(CLEAR_COLOR, CLEAR_COLOR, CLEAR_COLOR);
	float sx = pos.x - x;
	float sy = pos.y - y;
	float dist = sqrt(sx * sx + sy * sy);
	if(dist < radious) {
		temp = color;
	}
	return temp;
}

void main( void ) {

	vec2 position = vec2(mouse.x / resolution.x * gl_FragCoord.x, mouse.y / resolution.y * gl_FragCoord.y);

	vec3 color = vec3(CLEAR_COLOR, CLEAR_COLOR, CLEAR_COLOR);
	color += rect(position, inputColour.z, 0.5, 0.2, 0.2, vec3(0.2, 0.3, 0.8));
	color += rect(position, inputColour.y, 0.2, 0.2, 0.2, vec3(0.2, 0.8, 0.2));
	color += rect(position, inputColour.x, sin(time) * 0.3 + 0.3, 0.2, 0.2, vec3(0.8, 0.3, 0.2));
	color += circle(position, inputColour.w, cos(time) * 0.3 + 0.5, 0.2, vec3(0.8, 0.3, 0.2));
	gl_FragColor = vec4( color, inputColour.x );

}
