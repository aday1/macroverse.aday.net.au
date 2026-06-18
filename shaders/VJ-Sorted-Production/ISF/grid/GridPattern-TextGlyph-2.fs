/*{
    "DESCRIPTION": "GridPattern-TextGlyph-2",
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
        }
    ],
    "TAGS": [
        "grid",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

uniform sampler2D tex;
#define grid 3.
void main( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.xy );
	vec2 mousepos = (mouse.xy*resolution.xy)+grid;
	
	float color=0.0;
	if ((gl_FragCoord.x) < mousepos.x-mod(mousepos.x,grid) && 
	    (gl_FragCoord.x+grid) > mousepos.x-mod(mousepos.x,grid) && 
	    (gl_FragCoord.y) < mousepos.y-mod(mousepos.y,grid) && 
	    (gl_FragCoord.y+grid) > mousepos.y-mod(mousepos.y,grid)){
		color += 1.0;
	}
	else if (mod(gl_FragCoord.x,grid)<=1.0 || mod(gl_FragCoord.y,grid)<=1.0){
		gl_FragColor = vec4(0.1,0.2,0.3,1);
		return;
	}
	
	color = min(color,0.8);
	gl_FragColor = texture2D(tex, position) + vec4( vec3( color, color, color), 1.0 );

}
