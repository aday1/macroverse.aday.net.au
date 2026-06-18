/*{
    "DESCRIPTION": "BoringGrid",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
        "grid",
        "texture-input"
    ]
}*/
#define E 2.71828182846

varying vec2 position;

uniform vec4 color;
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

uniform vec4 inputColour;

// mouse.x
// mouse.y
// inputColour.x
// inputColour.y
// inputColour.z
// inputColour.w

uniform sampler2D tex;
#define grid 3.
void main( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.xy );
	vec2 mousepos = vec2(0.0);
	
	float color=mouse.x;
	if ((gl_FragCoord.x) < mousepos.x-mod(mousepos.x,grid) && 
	    (gl_FragCoord.x+grid) > mousepos.x-mod(mousepos.x,grid) && 
	    (gl_FragCoord.y) < mousepos.y-mod(mousepos.y,grid) && 
	    (gl_FragCoord.y+grid) > mousepos.y-mod(mousepos.y,grid)){
		color += 1.0;
	}
	else if (mod(gl_FragCoord.x,grid)<=1.0 || mod(gl_FragCoord.y,grid)<=1.0){
		gl_FragColor = vec4(mouse.y,inputColour.x,inputColour.y,1);
		return;
	}
	
	color = min(color,0.8);
	gl_FragColor = texture2D(tex, position) + vec4( vec3( color, color, color), inputColour.x );

}
