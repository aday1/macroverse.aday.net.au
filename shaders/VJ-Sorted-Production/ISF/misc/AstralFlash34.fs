/*{
    "DESCRIPTION": "AstralFlash34",
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
//REAL TiME XPRESS "RTX1911" Demo division
//www.rtx1911.nrt
//@rtx1911

#ifdef GL_ES
precision mediump float;
#endif

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.xy );
	//p.x *= (resolution.x / resolution.y);
	//Write your Code here (Begin)
	
	vec3 col = vec3(tan(p.x + time * 0.25), cos(p.y + time * 0.75), 0.1);
	col *= vec3(step(0.5, fract(p.x * 5.0)));
	col *= vec3(step(0.5, fract(p.y  * 5.0)));
		//vec3(smoothstep(0.37, 0.35, length(fract(2.0 * p.xy) - 0.5)));
	//vec3 col = vec3(smoothstep(0.37, 0.35, length(fract(sin(time * 0.25) * 4.0 * p.xy) - 0.5)));

	gl_FragColor = vec4( col, 1.0 );

	//End Code
	gl_FragColor *= mod(gl_FragCoord.y, 3.0);
}
