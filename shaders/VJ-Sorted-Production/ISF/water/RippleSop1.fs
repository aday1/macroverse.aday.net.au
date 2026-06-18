/*{
    "DESCRIPTION": "RippleSop1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "water"
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
        "water"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

const float width = 30.0;
#define PI 3.1415926535897932384626433832795
void main( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.xy ) + mouse / 2.0;

	float color = 0.0;
	float li = 0.0;
	float rTime = time * 4.0;
	float len = length(((resolution.xy / 2.0) - gl_FragCoord.xy) / width);
	
	li = sin(len - rTime);
	if(li < 0.0) li *= -1.0;
	float offset = mod(rTime - len, PI);
	if(offset > 1.0) li *= 1.0 / offset;
	
	color = li;
	
	gl_FragColor = vec4( vec3(color, color, color), 1.0 );

}
