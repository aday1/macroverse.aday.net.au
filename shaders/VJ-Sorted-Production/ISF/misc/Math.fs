/*{
    "DESCRIPTION": "Math",
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

#extension GL_OES_standard_derivatives : enable

float f(float x)
{ /** you can change the plot function here **/
	return sin(x+time*4.)*cos(time)*x; //hi 
}

bool cmp(float a, float b, float epsilon)
{
	return (abs(a-b))<epsilon;
}

void main( void ) {

	vec2 p = gl_FragCoord.xy / resolution.xy * 8.0 - 4.;
	vec2 plot = gl_FragCoord.xy / resolution.xy;
	
	if(cmp(p.y, f(p.x), 0.03))
		gl_FragColor = vec4(1., 0., 0., 1.);
	
	else if (cmp(0.5, plot.x, 0.002) || cmp(0.5, plot.y, 0.004)) gl_FragColor = vec4(0.0, 1.0, 0.0, 1.0);
	else if(cmp(mod(.5007-plot.x, 0.0625), 0., 0.0014) || cmp(mod(.5007-plot.y, 0.125), 0., 0.003))
	   gl_FragColor = vec4(1.);
		
	else gl_FragColor = vec4(.0);

}
