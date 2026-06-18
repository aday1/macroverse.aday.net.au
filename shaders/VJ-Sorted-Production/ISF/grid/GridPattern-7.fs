/*{
    "DESCRIPTION": "GridPattern-7",
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
        "grid"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

// hey - lets simplify some of that maths!

vec2 d_pos = vec2(time * 100.0, time * 100.0);

float size = 64.0;
float border_size = 8.0; 
float hipst = 8.0;

float rand (vec2 co){
    return fract(sin((co.x+co.y*1e3)*1e-3) * 1e5) * 0.05;
}
void main( void ) {
	
	vec2 position = gl_FragCoord.xy + d_pos;
	
	vec3 color_0 = vec3(0.25, 0.25, 0.25)+ rand(floor(position));
	vec3 color_1 = vec3(0.5, 0.5, 0.5)+ rand(floor(position));
	vec3 border_color = vec3(75.0/255.0, 75.0/255.0, 75.0/255.0);
	
	// tiled floor
	vec2 p = floor(position/size);
	vec3 color = (mod(p.x+p.y, 2.0) > 0.5)?color_0:color_1;

	// border box
	vec2 c = resolution*0.5;
	vec2 r = c - vec2(border_size);
	p = abs(gl_FragCoord.xy - c) - r;
	float d = max(p.x, p.y);
	// shadow
	color *= 1.0 - hipst/(-4.0*d);
	// border
	if(d >= 0.0) color = border_color;
	// rim
	if(floor(d) == 0.0) color = border_color * 1.5;
	
	gl_FragColor = vec4( color , 1.0 );
}
