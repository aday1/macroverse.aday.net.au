/*{
    "DESCRIPTION": "AuroraBloom10",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        "abstract"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define PI 3.14159265359

float s(float x, float b) 
{
  return 0.06+sin(x * 2. * PI * b) / b;
}

void main( void ) 
{
	vec2 position = ( gl_FragCoord.xy / resolution.xy );
	
	float x = position.x;
	float p = 0.0;
	float q = 0.01;
	for (float i = 1.0; i < 15.0; i += 1.0) 
	{
	  p  += s(x + sin(0.05 * time*i), pow(2.2, i));
	  q *= s(x + sin(0.62 * time*i), pow(2.0, i));
	}
	p = 0.07+0.3 * p;
	q = 1.4 * p;
	
	float color =  0.021 / abs(p - position.y) 
		     - 0.011 / abs(q - position.y);
		
	gl_FragColor = vec4( vec3(q*time/44., p*time/50., 10.0*sin(time))*color, 1.0 );
}

// must be funny to hear the wave as sound...
