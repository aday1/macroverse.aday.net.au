/*{
    "DESCRIPTION": "VoidSpace1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "space"
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
        "space"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

// dec?

void main( void )
{

	vec2 uPos = ( gl_FragCoord.xy / resolution.xy );
	uPos.xy *= 5.0;
	uPos.y -= 0.0;
	uPos.x -= 2.80;
	
	float vertColor = 0.0;
	for( float i = -0.4; i <= 1.0; i+=0.2 )
	{
		float t = time * i;
	
		uPos.x += (cos( uPos.y + t ) * 0.7)/(sin( uPos.x + t ) * 8.0);
	
		float fTemp = abs( 0.07 / uPos.x / 8.0);
		vertColor += fTemp;
	}
	
	vec4 color = vec4( vertColor , vertColor , vertColor * 1.5, 9.0 );
	gl_FragColor = color;
}
