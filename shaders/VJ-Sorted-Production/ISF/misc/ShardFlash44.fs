/*{
    "DESCRIPTION": "ShardFlash44",
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

varying vec2 surfacePosition;

float sinc(float x){
	if(x == 0.) return x;
	x *= 3.14159265;
	return sin(x)/x;
}
	
void main( void ){
	vec2 p;
	p = surfacePosition;
	float localTime = time + p.x*2.;
	float r = length(p);
	float theta = atan(p.y,p.x);
	
	p = vec2(r*cos(theta+5.*r*cos(localTime)),
		 r*sin(theta+5.*r*cos(localTime)));
	
	float color = 0.;
	color = sinc(p.x*p.y)/(sinc(p.x)*sinc(p.y));
	float c1 = fract(color*pow(1e5,cos(localTime*1e-1)*cos(localTime*1e-1)));
	float c2 = fract(color*pow(1e5,cos(localTime*1.5e-1)*cos(localTime*1.3e-1)));
	float c3 = fract(color*pow(1e5,cos(localTime*2.0e-1)*cos(localTime*1.7e-1)));
	
	gl_FragColor = vec4( vec3( c1, c2, c3), 1.0 );

}
