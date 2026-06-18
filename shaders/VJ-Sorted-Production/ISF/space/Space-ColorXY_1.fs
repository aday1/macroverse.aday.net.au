/*{
    "DESCRIPTION": "Space-ColorXY",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "space"
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
        "space"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(0.0)
#define resolution RENDERSIZE
// StarTrip
 
#ifdef GL_ES
precision mediump float;
#endif

#define numStars 40
#define speed .2
 
void main( void )
{
	vec2 position = ( gl_FragCoord.xy -  resolution.xy*.5 ) / resolution.x;
	position += mouse - 0.5;
 
	// 256 angle steps
	float angle = atan(position.y,position.x) / (2.*3.14159265359);
	angle -= floor(angle);
	float rad = length(position);
	
	vec3 color = vec3(0.0);
	for (int i = 0; i < numStars; i++) 
	{
		float angleFract = fract(angle*256.);
		float angleRnd = floor(angle*256.)+1.;
		float angleRnd1 = fract(angleRnd*fract(angleRnd*.7235)*45.1);
		float angleRnd2 = fract(angleRnd*fract(angleRnd*.82657)*13.724);
		float t = speed * time - angleRnd1*10.;
		float radDist = sqrt(angleRnd2+float(i));
		float adist = radDist/rad*0.003;
		float dist = abs(fract((t*.1+adist))-.5);
		color += vec3(max(0.,.5-dist*40./adist)*(.5-abs(angleFract-.5))*5./adist/radDist);
		color.r *= 1.05*pow(1.15,angleRnd2);
		color.b *= pow(angleRnd2, 0.3) * 1.4;
		color.g *= 1.02;
		angle = fract(angle+.61);
	}
	gl_FragColor = vec4( color*.3, 1.0);
}

