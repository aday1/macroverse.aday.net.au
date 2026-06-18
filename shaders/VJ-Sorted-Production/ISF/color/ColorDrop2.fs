/*{
    "DESCRIPTION": "ColorDrop2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "color"
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
        "color"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
//from shadertoy ... ./gtr
#ifdef GL_ES
precision mediump float;
#endif

void main()
{   
    vec2 uv = gl_FragCoord.xy / resolution.xy;

    vec3 Gradient = vec3(1.0 - uv.y,1.0 - (uv.y * 2.0),1.0 - uv.y);
    gl_FragColor = vec4(Gradient,1.0);
    
	float offset = time * 0.51; 
    float angleOffset = 2.0 * 3.14159 * offset;
    
    for ( int i = 0; i < 16; i++ )
    {
    	float y = float(i) / 16.0;
    	float x = 0.5 * (uv.x + time * (0.1 + y));

	    float rand = vec2(uv.x  ,sin(time+uv.x)).x;

	rand = 0.25 + 0.65 * sin(rand * 3.14159);
    	if ( uv.y < rand )
    	{
            float fog = sqrt(x);
            
            vec3 Colour = vec3(1.0 - x,x,0.5 + (x * 0.5));
            
            float angle = x * 4.0 * 3.14159;
            float r = (cos(angle) + 1.0) * 0.5;
            float g = (cos(angle + angleOffset) + 1.1) * 0.5;
            float b = (cos(angle + (angleOffset * 2.0)) + 1.0) * 0.5;  
            Colour = vec3(r,g,b);
            
        	gl_FragColor = vec4(mix(Gradient,Colour, fog), 1.0);
    	}
        
        uv.y *= 1.15;
    }

}
