/*{
    "DESCRIPTION": "DotMatrix-TextGlyph-22",
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
        "geometric",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

float pi = 3.14159265;

uniform sampler2D backbuffer;

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.xy ) * 2.0 - 1.0;

	float a = atan( p.y, p.x );
	float r = sqrt( dot( p, p ) );

	vec2 uv = vec2( 0, 0 );
	uv.x = mod( mouse.x * cos( a ) / r + time * 0.05, 1.0 );
	uv.y = mod( mouse.y * sin( a ) / r + time * 0.06, 1.0 );
	
	float amount = sin( time * 0.5 ) * 0.01;

	vec4 color0 = texture2D( backbuffer, uv );
	vec4 color1 = texture2D( backbuffer, uv + vec2( 0.0, - amount ) );
	vec4 color2 = texture2D( backbuffer, uv + vec2( 0.0, amount ) );
	vec4 color3 = texture2D( backbuffer, uv + vec2( amount, 0.0 ) );
	vec4 color4 = texture2D( backbuffer, uv + vec2( - amount, 0.0 ) );

	gl_FragColor = ( ( color0 + color1 + color2 + color3 + color4 ) / 8.0 ) + pow( 1.0 - r, 3.0 );

	float border = 0.95;

	if ( p.x < - border || p.x > border || p.y < - border || p.y > border ) {

		gl_FragColor = vec4( p.x + p.y );

	}

}
