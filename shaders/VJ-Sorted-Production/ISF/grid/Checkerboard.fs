/*{
    "DESCRIPTION": "Checkerboard",
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
        }
    ],
    "TAGS": [
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

void main( void ) {
	
	vec2 pos = ( gl_FragCoord.xy / resolution.xy ) - vec2(0.5,0.5);	
        float horizon = 0.0; 
        float fov = 0.7; 
	float scaling = 0.1;
	float t = 0.1;
	
	mat2 rot = mat2(cos(t),sin(t),sin(t),cos(t)); // rot 2d pos ;
	
	pos  *=rot;

	vec3 p = vec3(pos.x, fov, pos.y - horizon);      
	vec2 s = vec2(p.x/p.z, p.y/p.z) * scaling;
	
	s.xy *=rot;
	
	//checkboard texture
	
	float dupa = mouse.x;
	float color =1.0;
	if(pos.y < 1.1) // top layer

	  color = sign((mod(s.x, 0.1) - 0.05) * (mod(s.y + dupa * mod(-time * 0.05, 1.0), 0.1) - 0.05));
	color *= p.z*p.z*14.0;
	
	 	 gl_FragColor = vec4( 0.5-p.y,0.2,0.6, 1.0 );
	
	//fading 2 black 

	vec4 c = vec4(0,0,0,1);
	gl_FragColor = c;

	gl_FragColor += vec4( vec3(color), 1.0 );

}
