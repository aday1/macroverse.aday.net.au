uniform float level; // @expose 0 1
uniform float saturation; // @expose 0 1
/*{
	"CATEGORIES": ["Color"],
	"DESCRIPTION": "Vibrant color feedback pattern (for feedback chains, use with IMG_THIS_NORM_PIXEL in Wire)",
	"INPUTS": [
		{
			"NAME": "level",
			"TYPE": "float",
			"DEFAULT": 0.9,
			"MIN": 0.0,
			"MAX": 1.0
		},
		{
			"NAME": "saturation",
			"TYPE": "float",
			"DEFAULT": 0.8,
			"MIN": 0.0,
			"MAX": 1.0
		}
	]
}*/

void main() {
	vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
	float base = sin(uv.x * 10.0 + TIME) * sin(uv.y * 10.0 - TIME);
	vec3 color = vec3(
		0.5 + 0.5 * sin(base * 3.14159 + 0.0),
		0.5 + 0.5 * sin(base * 3.14159 + 2.094),
		0.5 + 0.5 * sin(base * 3.14159 + 4.189)
	);
	float luma = dot(color, vec3(0.299, 0.587, 0.114));
	vec3 vibrant = mix(vec3(luma), color, saturation);
	gl_FragColor = vec4(vibrant * level, 1.0);
}
