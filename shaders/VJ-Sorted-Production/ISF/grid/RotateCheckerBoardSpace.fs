/*{
    "DESCRIPTION": "RotateCheckerBoardSpace",
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

float checkers(vec2 uv)
{
	return mod(floor(uv.x) + floor(uv.y), 2.0);
}

void main( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.x);
	vec2 uv = position * 10.0 - vec2(5.0, 2.5);

	uv = uv * mat2(sin(time), -cos(time),  cos(time), sin(time));
	float divisor=-time/18.;
	float col;	
	col = checkers(uv);

	uv  = uv * mat2(sin(divisor), -cos(divisor),  cos(divisor), sin(divisor));
	col += checkers(uv+0.01)/divisor;
	col += checkers(uv+0.04)/6.;
	 
	// thanks ... much better now gtr !!
	
	gl_FragColor = vec4(length(col/(2.0+divisor)));

}
