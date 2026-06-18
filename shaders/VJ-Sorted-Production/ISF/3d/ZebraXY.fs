/*{
    "DESCRIPTION": "ZebraXY",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "3d"
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
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

float pi = 3.14159265359;
	
vec2 opCheapBend( vec2 p, float d )
{
	d = max(d,0.1) ;
	
    float c = cos(pi*p.x) * d;
    float s = sin(pi) * d;
    mat2  m = mat2(c,-s,s,c);
    return vec2(m*p.xy);
}

void main( void ) {
	vec2 position = ( gl_FragCoord.xy / resolution.xy );
	float ratio = resolution.x / resolution.y;

	float d = distance(mouse, position);

	position = opCheapBend( position , d);
	
	float v = sin(position.y*200.0)+0.15/d;
	
	v = step(0.1,v);
	
	vec4 col = vec4(v,v,v,1.0);
	
	gl_FragColor = col;
}


