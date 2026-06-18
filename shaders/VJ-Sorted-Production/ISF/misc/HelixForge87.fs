/*{
    "DESCRIPTION": "HelixForge87",
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
#define pi 3.14159

const int num = 50;
void main( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.x );

	vec3 color;
	for(int i = 0; i<num;i++ )
	{
		color += (max(1.0-sign(length(position-vec2(0.5-cos(float(i)*4.2+time)*mod(time+cos(float(i)+time),0.75),
		0.25-sin(float(i)*4.3+time)*mod(time+cos(float(i)+time),0.75)))-(0.025+mod(time/20.0+cos(float(i))*5.0,0.05))),0.0)/5.0)
			*vec3(cos(float(i)*2.5+time),sin(float(i)*3.7+time),cos(float(i)*1.3+time+pi));
	}

	gl_FragColor = vec4( color, 1.0 );

}
