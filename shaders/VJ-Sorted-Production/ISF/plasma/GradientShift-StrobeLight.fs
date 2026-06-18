/*{
    "DESCRIPTION": "GradientShift-StrobeLight",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "plasma"
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
        "plasma"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
// Disco Inferno

#ifdef GL_ES
precision highp float;
#endif

void main(void)
{

	float scale = sin(0.3 * time) * 8.0 + 8.25;
	vec2 position = vec2((gl_FragCoord.x / resolution.x) - 0.5, (gl_FragCoord.y / resolution.y) - 0.5) * 4.0;

	vec2 rotated_position;
	rotated_position.x = ((position.x - 0.5) * cos(sin(0.25 * time) * 2.5) - (position.y - 0.5) * sin(sin(0.25 * time) * 2.5)) * scale;
	rotated_position.y = ((position.y - 0.5) * cos(sin(0.25 * time) * 2.5) + (position.x - 0.5) * sin(sin(0.25 * time) * 2.5)) * scale;
	position = rotated_position;

	vec2 coord = mod(position,1.0);	
	vec2 effect = floor(mod(position,5.0)); 
	float effect_number = effect.y * 4.0 + effect.x;
	vec2 effect_group = floor(position) * 7.0; 
 
	float gradient = 0.0;
	vec3 color = vec3(0.0);
 
	float angle = 0.0;
	float radius = 0.0;
	const float pi = 3.141592;
	float fade = 0.0;
 
	float u,v;
	float z;
 
	vec2 centered_coord = coord - vec2(0.5);

	float dist_from_center = length(centered_coord);
	float angle_from_center = atan(centered_coord.y, centered_coord.x);
	
	radius = dist_from_center + sin(time * 8.0) * 0.1 + 0.1;
	angle = angle_from_center + time;
 	gradient = 0.5 / radius + sin(angle * 5.0) * 0.3;
	color = vec3(gradient, gradient / 2.0, gradient / 3.0);
 
	color.r *= (sin(effect_group.x) * 0.5 + 0.5);
	color.g *= (sin(effect_group.y) * 0.5 + 0.5);
	color.b *= (sin(effect_group.x * effect_group.y) * 0.5 + 0.5);
 
	gl_FragColor = vec4(color, 1.0 );
}

