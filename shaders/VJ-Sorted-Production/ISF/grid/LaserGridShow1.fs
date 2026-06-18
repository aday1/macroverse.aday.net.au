/*{
    "DESCRIPTION": "LaserGridShow1",
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

#define time TIME




#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif
//2017.01.29 tigrou dot ind at gmail dot com

//edit: set to 0.5

#extension GL_OES_standard_derivatives : enable

void main( void ) {
	float time = time*2.0;
	vec2 pos = ( gl_FragCoord.xy / resolution.xy ) - vec2(0.5,0.5);
        
	vec3 color = vec3(0.0);
	
	for(int i = -1 ; i <= 2 ; i++)
	{
		vec2 p = pos + vec2(float(i)/5.0,0.0);
		float dx = sin(time*0.5+float(i)*2.3);
		float dy = sin(time*0.2+float(i)*2.3);
		
		float c = p.x/p.y*dy*8.0+dx; 
		float k = p.y*dy;
		if(k < 0.0 && abs(c) < 10.0) 
		{
			float k0 = 0.3/abs(sin(c));
			float k1 = 0.1/abs(sin(c+3.1415/2.0));	
			float flash = sin(time*6.0);
			flash *= sin(time + float(i)*4.0) * 0.5 + 0.5;
			color += (vec3(0.1,0.035,0.25) *k0 + vec3(0.1, 0.5, 0.1) * k1 ) * flash;
			color += max(0.0, flash) * 0.03 * (vec3(0.0,03.5,0.5) - length(vec2(c,k)));
		}
        }	
	
        gl_FragColor = vec4(color, 1.0);
}
