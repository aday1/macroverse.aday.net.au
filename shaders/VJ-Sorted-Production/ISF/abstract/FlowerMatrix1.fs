/*{
    "DESCRIPTION": "FlowerMatrix1",
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
        "abstract",
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// Flower Matrix
// By: Brandon Fogerty
// xdpixel.com

#ifdef GL_ES
precision mediump float;
#endif

void main( void ) 
{

	vec2 uv = ( gl_FragCoord.xy / resolution.xy ) * 2.0 - 1.0;
	uv.x *= resolution.x/resolution.y;
	
	vec2 px = vec2(gl_FragCoord.x, gl_FragCoord.y);
	///px *= 4.;

	vec3 finalColor = vec3( 0.0, 0.0, 0.0 );

	float a = 0.;//atan( uv.y / uv.x );
	float r = -1.5 + length( uv );

	float timeT = .1;//sin(time) * 0.5 + 0.5;
	float move = .6 + time;
	
     	float t = .7 + .1 * sin(move * 1.);
    
     	finalColor += vec3( 8.0 * t, 4.0 * t, 2.0 * t );
    
     	finalColor *= .5 * (1.-r);
	
	float g = -mod( px.y + time, cos( px.x ) + 0.004 ) * .5;
	finalColor *= vec3( 0.0, g, 0.0 );
	
	gl_FragColor = vec4( finalColor, 1.0 );

}
