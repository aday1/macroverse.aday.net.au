/*{
    "DESCRIPTION": "CipherDance16",
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

void main( void ) {
	//vec3 light_color = vec3(1.2,0.8,0.6);
	vec3 light_color = vec3(2,2,2);
	float a = pow(10.0,5.7*mouse.y);
	a = 0.1;
	float t = time*a;
	vec2 position = ( gl_FragCoord.xy -  resolution.xy*.5 ) / resolution.x;
 
	// 256 angle steps
	float angle = atan(position.y,position.x)/(2.*3.14159265359);
	angle -= floor(angle);
	float rad = length(position);
	angle = fract(angle+rad*8.*t/32.);//*.007;
	
	float color = 0.0;
 
	float angleFract = fract(angle*256.);
	float angleRnd = floor(angle*256.)+1.;
	float angleRnd1 = fract(angleRnd*fract(angleRnd*.7235)*45.1);
	float angleRnd2 = fract(angleRnd*fract(angleRnd*.82657)*13.724);
	float t2 = t+angleRnd1*10.;
	float radDist = sqrt(angleRnd2);
	
	float adist = radDist/rad*.1;
	float dist = (t2*.1+adist);
	dist = abs(fract(dist)-.5);
	color +=  (1.0 / (dist))*cos(0.7*(sin(t)))*adist/radDist/30.0;
 
	angle = fract(angle+.61);
	
	gl_FragColor = vec4(color,color,color,1.0)*vec4(light_color,1.0);
}

