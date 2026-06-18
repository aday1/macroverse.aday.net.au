/*{
    "DESCRIPTION": "OKGO",
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
precision highp float;
#endif

void main( void ) {

	vec2 p = gl_FragCoord.xy / resolution.xy - 0.5;
	p.y *= resolution.y/resolution.x;
	
    	float a = atan(p.y,p.x);
    	float r = length(p);
	float sd = sign(r-0.5);
	vec3 tc=vec3(0.0,0.0,0.0);
	for( int s=0;s<10;s++)
	{
	float c = sin(r*70.*sin(time+float(s)*0.05) + sin(sin(a*4.0)*7.0)*0.5 ) >0. ? 0.1 : 1.0;
		float rs = float(s)/10.7 * 3.1415 * 2.0;
		tc += c*(0.5+0.5*time*cos(vec3(rs+1.0,rs+0.5,rs)));
	}
	tc /= 200.0;
    	gl_FragColor = vec4(tc,1.0);

}
