/*{
    "DESCRIPTION": "RGB-Static",
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

#define clamps(x) clamp(x,0.,1.)
void main( void ) 
{
	vec2 uv = gl_FragCoord.xy/resolution.xy;
	uv = uv * 8. - 4.;
	uv *= resolution.xy/min(resolution.x, resolution.y);
	
	//r, g, b, a
	//x, y, z, w
	//s, t, p, q
	vec3 t 	= vec3(
	length(uv - vec2( 1., -sqrt(2.)/2.)),
	length(uv - vec2(.0, sqrt(2.)/2.)),
	length(uv - vec2(-1., -sqrt(2.)/2.))
	);
	
	float sharp = mix(20.,140.,((sin(time+uv.y)+1.)/2.));
	//t = 1.-clamps((t-(1.-(.5/sharp)))*sharp); //No yellow, cyan, magenta.
	t = 1.-clamps((t-((2./sqrt(3.))-(.5/sharp)))*sharp); //No white.
	//t = 1.-clamps((t-(1.5-(.5/sharp)))*sharp); //All.
	
	gl_FragColor = vec4(t, 1.);
}
