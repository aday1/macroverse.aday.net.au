/*{
    "DESCRIPTION": "Spiralgrapher1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        "abstract"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define TWOPI 6.2831853
const float num = 15.0;

void main( void ) {

	vec2 _pos = (gl_FragCoord.xy * 2.0 - resolution) / min(resolution.x, resolution.y);
	vec3 _destColor;
	
	_destColor.b = abs(sin(time));
	_destColor.g = abs(cos(time));
	_destColor.r = (_destColor.b + _destColor.g) / 1.0;
	
	float _exrate = abs(sin(time));
	
	float f = 0.0;
	for(float i = 0.0; i < num; ++i)
	{
		float theta = time + i * TWOPI / num;
		vec2 tr = vec2(cos(theta), sin(theta)) * _exrate;
		f += (mouse.y * 0.025 + 0.0025) / distance(length(_pos + tr), mouse.x);
	}
	gl_FragColor = vec4(vec3(_destColor * f), 1.0);
}
