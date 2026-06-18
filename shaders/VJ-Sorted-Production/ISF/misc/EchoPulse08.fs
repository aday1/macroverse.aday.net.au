/*{
    "DESCRIPTION": "EchoPulse08",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "misc"
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
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

vec3 tex(vec2 uv, vec3 col1, vec3 col2)
{
	return (mod(floor(uv.x) + floor(uv.y), 2.0)==0.0?col1:col2);	
}

void main( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.xy / mouse.x - mouse.y ) - vec2(0.5, 0.5); position.x *= resolution.x/resolution.y;
	vec2 uv = position*5.0;
	uv.x += sin(length(uv+time))*(sin(time*2.0)+1.0)*0.5;
	uv.y += cos(length(uv+time))*0.4;
	gl_FragColor = vec4(tex(uv, vec3(0.7, 0.1, 0.1), vec3(1.0, 1.0, 1.0)), 1.0 );

}
