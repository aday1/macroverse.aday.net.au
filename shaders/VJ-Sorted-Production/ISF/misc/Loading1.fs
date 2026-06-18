/*{
    "DESCRIPTION": "Loading1",
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
        "misc"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define pi 3.1415926536
#define N 8
void main( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.xy );
	vec2 center=position*2.-1.;
	center.x*=resolution.x/resolution.y;
	float c=0.;
	float r=0.3;
	float o;
	for(int i=0;i<N;i++)
	{
		vec2 xy;
		o=float(i)/float(N)*2.*pi;
		xy.x=r*cos(o);
		xy.y=r*sin(o);
		xy+=center;
		c+=pow(200000.,(1.-length(xy)*1.9)*(0.99+0.1*fract(float(-i)/float(N)-time*1.5)))/20000.0;
	}
	gl_FragColor = vec4( c*vec3(0.1,.15,.2),1.0 );

}
