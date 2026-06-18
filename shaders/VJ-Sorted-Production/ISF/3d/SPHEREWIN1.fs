/*{
    "DESCRIPTION": "SPHEREWIN1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "3d"
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/
#define E 2.71828182846

varying vec2 position;
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable
float s = 20.0;

uniform vec4 inputColour;
//This work by Void Chicken is licensed under a Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
void main( void ) {

	s=10.0;
	vec2 position = ( gl_FragCoord.xy / resolution.xy );
	float bb = length(position.x-mouse.x);
	position+=bb*time/2.0;
	
	position/=bb;
	
	float a = mod(position.x*s,mouse.y);
	float b = mod(position.y*s,inputColour.z);
	float c = b;
	c=c>0.5?inputColour.x:0.0;

	c += a=a>0.5?1.0:0.0;
	if (c>1.0)c=inputColour.w;
	gl_FragColor = vec4(c,c,c, inputColour.y )*pow(bb,length(vec2(1))-bb);

}
