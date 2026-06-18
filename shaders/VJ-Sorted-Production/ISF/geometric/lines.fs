/*{
    "DESCRIPTION": "lines",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "geometric"
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
        "geometric",
        "texture-input"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D bbuff;

void main( void )
{

	vec2 uv = ( gl_FragCoord.xy / resolution.xy );//normalize wrt y axis
	
	uv.x -= 0.5;
	
	float vertColor = 0.0;
	for( float i = 0.0; i < 7.0; ++i )
	{
		float t = time * (i + 0.9);
	
		uv.x -= cos(mouse.x+uv.y) -(mouse.y+0.5*(uv.x) );
	
		float fTemp = abs(1.0 / uv.x / 100.0);
		vertColor += fTemp;
	}
	
	vec4 color = vec4( vertColor*0.5, vertColor * sin(time), vertColor * 2.5, vertColor );
	color *= color;
	uv = (gl_FragCoord.xy / resolution.xy);
	color+=texture2D(bbuff,vec2(uv))* + abs(sin(time*1.3) * sin(time * 3.7)) * .25;
	color+=texture2D(bbuff,uv + vec2(.0025, .0025))*.15;
	color+=texture2D(bbuff,uv + vec2(.0025, -.0025))*.15;
	color+=texture2D(bbuff,uv + vec2(-.0025, -.0025))*.15;
	color+=texture2D(bbuff,uv + vec2(-.0025, .0025))*.15;

	gl_FragColor = color;
}
