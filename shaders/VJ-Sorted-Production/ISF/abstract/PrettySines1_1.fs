/*{
    "DESCRIPTION": "PrettySines1",
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
        }
    ],
    "TAGS": [
        "abstract"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_FRAGMENT_PRECISION_HIGH
#ifdef GL_ES
	precision highp float;
#endif
#else
#ifdef GL_ES
	precision mediump float;
#endif
#endif

void main( void ) {
	vec2 pos = ( gl_FragCoord.xy / resolution.xy );
	float three = 1.0 - ((pos.x + pos.y) / 2.0);
	vec3 color = vec3(three, pos.x, pos.y);
	vec3 color2 = color;
	
	pos = gl_FragCoord.xy / resolution.xy * 2.0 - 1.0;
	color *= abs(1.0 / (sin(pos.y + sin(pos.x + time) * 0.7) * 30.0));
	color2 *= abs(1.0 / (sin(pos.y + sin(pos.x*2. + time) * 0.7) * 30.0));
	color2 *= abs(1.0 / (sin(pos.y + sin(pos.x*3.2 + time) * 0.7) * sin(time*0.3)));
	color += color2;
	color /= 2.0;

	gl_FragColor = vec4(color, 1.0 );
}
