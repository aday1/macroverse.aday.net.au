/*{
    "DESCRIPTION": "Colourxzzzzz",
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




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
// co3moz
// color madness
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

#define pi (atan(mouse.x) * inputColour.w)
#define threepi (atan(mouse.x) * 2.66666)

void rotate(out vec2 position, float deg) {
	float temp = position.x;
	position.x = temp * cos(deg) - position.y * sin(deg);
	position.y = temp * sin(deg) + position.y * cos(deg);
}

void main() {
	vec2 aspect = resolution.xy / min(resolution.x, resolution.y);
	vec2 position = (gl_FragCoord.xy / resolution.xy) * aspect;
	vec2 center = vec2(mouse.x);
	vec2 circlePosition = center;
	float circleRadius = mouse.x + sin(time * mouse.x) / 10.0;
	
	circlePosition.x += sin(time) / 10.0;
	circlePosition.y += cos(time) / 10.0;
	circlePosition *= aspect;
	
	rotate(position, time);
	rotate(center, time);
	rotate(circlePosition, time);

	vec3 color = vec3(sin(position.y), sin(position.x + threepi), sin(position.y + threepi));
	position -= center;
	color += sin(sqrt(abs(position.x * position.y * pow(position.x + position.y + pow(position.x - position.y, mouse.x), mouse.x))) * 50.0);
	position += center;
	
	if(distance(circlePosition, position) < circleRadius * 1.05882352941) {
		if(distance(circlePosition, position) < circleRadius) {
			color = vec3(1.0 - color.x, inputColour.y - color.y, inputColour.x - color.z);
			rotate(position, -time);
			position *= aspect;
			color += sin(sqrt(abs(position.x * position.y * pow(position.x + position.y, inputColour.z))) * 50.0);
		} else {
			color = vec3(position.x * position.y * 500.0);	
		}
	}
	
	gl_FragColor = vec4( color, mouse.x );
}
