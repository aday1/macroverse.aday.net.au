/*{
    "DESCRIPTION": "SineFutureColor1",
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
	float three = 1.0 - ((pos.x + pos.y) / 12000.0);
	vec3 color = vec3(three, pos.x, pos.y);

	pos = gl_FragCoord.xy / resolution.xy * 2.0 - 1.0;
	
	for(float i=0.0;i<0.5;i+=0.05){
	
	color *= abs(1.0+i /(sin(pos.y + sin(pos.x + i*time) * 4.) * 2.0));
		
	}

	gl_FragColor = vec4(color, 1.0 );
}
