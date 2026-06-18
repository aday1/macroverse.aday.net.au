/*{
    "DESCRIPTION": "DotMatrix-35",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "geometric"
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
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float random (vec2 st) { 
    return fract(sin(dot(st.xy,
                         vec2(12.9898,78.233+0.0001)))* 
        43758.5453123);
}

void main( void ) {

	vec2 pos = ( gl_FragCoord.xy / resolution.xy -0.5 )*vec2(resolution.x/resolution.y,1.0);
	vec2 position = vec2(fract(atan(pos.y,pos.x)/6.2831853),length(pos));
	
	position.x *= 100.*(sin(time/10.0));
	position.y *= 100.*(sin(time/10.0));;

	float line = floor(position.y);
	position.x += time*40.*(mod(line,2.)*2. -1.)*random(vec2(line));

	vec2 ipos = floor(position);

	vec3 color = vec3(step(mouse.y*random(vec2(line)), mouse.x*random(ipos)));

	gl_FragColor = vec4( color, 1.0 );

}
