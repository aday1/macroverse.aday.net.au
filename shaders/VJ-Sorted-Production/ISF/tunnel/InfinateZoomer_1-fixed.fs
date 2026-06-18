/*{
    "DESCRIPTION": "InfinateZoomer",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "tunnel"
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
        "tunnel"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE

#extension GL_OES_standard_derivatives : enable

float check(vec2 uv) { return float((uv.x > .5 && uv.y > .5) || (uv.x < .5 && uv.y < .5)); }
//#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
void main( void ) {
	vec2 uv = ( gl_FragCoord.xy / resolution.xy );
	const float lps = 11.;
	uv -= .5;
	uv = mix(uv,uv/2.,.5+.5*cos(.1/length(uv)-88.1*time)); //not what i wanteddddd ;___; weeeeeehhhuhuhuh weeeeeeeuhruiwagoijerwpaog0i398btiuehjrmfg
	uv += .5;
	float c = check(uv);
	float c2 = check(uv);
	for (float i = 0.; i < lps; i++) {
	    c += check(fract(uv*pow(2.,i)));
	    c2 += check(fract((uv*2.)*pow(2.,i)));
	}
	c /= lps; c2 /= lps;
	c = c;//mix(c,c2,fract(time));
	gl_FragColor = vec4(vec3(c), 1.0 );
}
