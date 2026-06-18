/*{
    "DESCRIPTION": "DriftDream97",
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
        }
    ],
    "TAGS": [
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

void main( void ) 
{

	vec2 p = ( gl_FragCoord.xy / resolution.xy )-0.5;
	float c = 0.0;
	float a=atan(p.x,p.y)*11.;
	float d=3.5/length(p);
	c=(atan(cos(d-a+time)*3.)*9./pow( (d),1.14 ));
	gl_FragColor = vec4( vec3(2.,1.5,8)*vec3( c*c, c*c-c, c*c*c/(1.+c) ), 1.0 );

}
