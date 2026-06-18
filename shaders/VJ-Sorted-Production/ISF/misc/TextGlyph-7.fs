/*{
    "DESCRIPTION": "TextGlyph-7",
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
        "misc",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE

#extension GL_OES_standard_derivatives : enable

varying vec2 surfacePosition;
#define p (surfacePosition)
const float sqrt3 = sqrt(3.);
uniform sampler2D b;
float dr(vec2 d){
	return texture2D(b, fract((gl_FragCoord.xy+d)/resolution)).r;
}
void main( void ) {
	gl_FragColor = vec4(1);
	float r = .5;
	vec2 v = p+vec2(0.,r*.5);
	if(abs(v.x*sqrt3) < v.y && v.y < r){
		gl_FragColor = vec4(0);
	}
	
	vec2 mpos = (.5 - mouse)*sqrt(length(resolution))*3.;
	gl_FragColor.xyz *= (.125+dr(mpos));
	gl_FragColor.xyz /= (.5+(dr(mpos*vec2(-1,1))));

}
