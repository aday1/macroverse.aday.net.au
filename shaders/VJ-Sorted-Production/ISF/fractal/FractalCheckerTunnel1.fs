/*{
    "DESCRIPTION": "FractalCheckerTunnel1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "fractal"
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
        "fractal"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// http://glslsandbox.com/e#11244.1
// hypno centersic
#ifdef GL_ES
precision mediump float;
#endif

#define PI 3.1415926

void main( void ) {

	vec2 p = gl_FragCoord.xy / resolution.y;
	p.y -= 0.5;
	p.x -= 0.5*resolution.x/resolution.y;
	
	float an = atan(p.y, p.x);
	/*an = mod(an, PI);*/
	float dy = 1.0/(distance(p, vec2(0., 0.)))*((sin(time/2.)+1.02)*3.) + 2.*an;
	
	gl_FragColor = vec4( vec3(cos(time*10.0+dy)*50.0)+0.5,1.0 );

}
