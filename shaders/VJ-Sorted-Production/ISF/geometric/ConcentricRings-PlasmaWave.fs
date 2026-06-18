/*{
    "DESCRIPTION": "ConcentricRings-PlasmaWave",
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
        }
    ],
    "TAGS": [
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

vec3 circle(in vec2 p, float r, vec3 rcol)
{
	if (length(p) < r)
		return rcol;
	return vec3(0,0,0); 
}
void main( void ) {

	vec3 col = vec3(0,0,0);
	
	vec2 p = (2.0*gl_FragCoord.xy-resolution)/resolution.y;

	p = fract(p);
	p *= fract(-p);

	//col += circle(p,1.0, -col+vec3(1,1,1)); 
	
	float r0 = 0.5;
	for (int i = 0; i < 24; i++) {
		float ii = float(i)+time*3.0; 
		float iii = float(i)-time*2.0;
		col += mix(circle(p-r0*vec2(sin(ii),cos(ii-time)),r0,-col+vec3(sin(iii),sin(2.0*ii),sin(3.0*ii))),circle(p-r0*vec2(sin(ii-time),acos(ii)),r0,vec3(sin(iii),sin(2.0*ii),sin(3.0*ii))),sin(ii)); 
	
	      //  col += circle(p-r0*vec2(sin(iii-time),acos(ii)),r0,vec3(sin(iii),sin(2.0*iii),sin(3.0*iii)));
		
		r0 *= 0.75; 
	}

	gl_FragColor = vec4(col, 1.0); 
	
}
