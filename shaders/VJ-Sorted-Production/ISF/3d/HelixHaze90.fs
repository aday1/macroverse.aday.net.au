/*{
    "DESCRIPTION": "HelixHaze90",
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

#define thickness 30.0

void main( void )
{
	float speed = 100.0;// * (1.0 + mouse.x / resolution.x);
	float amplitude = resolution.y * mouse.y;
	float wavelength = 0.1 * abs(mouse.x - resolution.x / 2.0);
	
	vec2 position = gl_FragCoord.xy - vec2(time * speed, 0);
	
	float y = sin(position.x / wavelength) * amplitude * (gl_FragCoord.x / (resolution.x / 2.0));
	
	float distance = abs(y / 2.0 + resolution.y / 2.0 - position.y);
	
	if(distance <= thickness)
	{
		float c = (thickness - distance) / thickness;
		
		gl_FragColor = vec4(tan(gl_FragCoord.x) * c, cos(gl_FragCoord.x) * c, sin(gl_FragCoord.x) * c, 1.0);
	}
	else
		gl_FragColor = vec4(0.0, 0.0, 0.0, 1.0);
}
