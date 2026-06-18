/*{
    "DESCRIPTION": "60sNEONSHAPER1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "psychedelic"
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
        "psychedelic"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

void main( void )
{
	vec3 finalcolor = vec3(0., 0., 0.);
	
	float fill = 0.;
	
	float radius = sqrt(pow(gl_FragCoord.x - resolution.x / 2., 2.) + pow(gl_FragCoord.y - resolution.y / 2., 2.)); //Pitagoras
	float dangle = asin((gl_FragCoord.y - resolution.y / 2.) / radius);
	float nangle = dangle + mod(time, 6.28318531); //2pi
	
	float bdef = 1. / ((radius * cos(nangle) / resolution.y) * 2.);
	//vec2 bpos = (( gl_FragCoord.xy / resolution.xy ) * 2. - 1.);
	
	float beam = abs(bdef / 10.);
	
	fill = beam;
	
	finalcolor.rb = vec2(fill, fill * 0.7);

	gl_FragColor = vec4(finalcolor, 1);
}
