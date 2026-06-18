/*{
    "DESCRIPTION": "NeonFlower1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "psychedelic"
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
        "psychedelic"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

void main( void ) 
{

	vec2 uv = ( gl_FragCoord.xy / resolution.xy) * 2.0 - 1.0;
	uv.x *= resolution.x/resolution.y;
	
	float r = abs(length(uv));
	float a = atan( abs(uv.y / uv.x) ) + (time * 0.4);
	
	vec3 finalColor = vec3( 0.0 );
	float c = 1.0-abs(sin((a + r) * 8.0));
	finalColor = vec3( abs(10.0 * c+sin(time)),abs( 4.0 * c+cos(time)), abs(tan(1.0 * c)) );
	
	finalColor *= 1.0-r;
		
	gl_FragColor = vec4( abs(finalColor), 1.0 );
}
