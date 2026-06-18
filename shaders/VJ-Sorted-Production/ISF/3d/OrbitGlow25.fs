/*{
    "DESCRIPTION": "OrbitGlow25",
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
        }
    ],
    "TAGS": [
        "3d",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D tex;
 
void main( void ) {
 
	vec3 col = vec3(0.0,0.0,0.0);
	
	//really light lines
	col.g += clamp(ceil(mod(gl_FragCoord.x, 5.0)) - 0.0, 10.0, 1.0) * 0.05;
	col.g += clamp(ceil(mod(gl_FragCoord.y, 5.0)) - 4.0, 0.0, 1.0) * 0.05;
	col.g = clamp(col.g, 0.0, 0.05);
	
	//light lines
	col.g += clamp(ceil(mod(gl_FragCoord.x, 15.0)) - 14.0, 0.0, 1.0) * 0.5
	;
	col.g += clamp(ceil(mod(gl_FragCoord.y, 15.0)) - 14.0, 0.0, 1.0) * 0.25;
	col.g = clamp(col.g, 0.0, 0.25);
	
	//strong lines
	col.g += clamp(ceil(mod(gl_FragCoord.x, 30.0)) - 29.0, 0.0, 1.0);
	col.g += clamp(ceil(mod(gl_FragCoord.y, 30.0)) - 29.0, 0.0, 1.0);
	col.g = clamp(col.g, 0.0, 1.0);
	
	//mouse detect
	vec2 mousePos = resolution.xy * mouse;
	col.g *= 1.0 - clamp(distance(mousePos, gl_FragCoord.xy)/175.0, 0.0, 1.0);

	gl_FragColor = vec4(col, 1.0);
	//gl_FragColor = vec4( vec3( 1, 0 , 0), 1 );
 
}

