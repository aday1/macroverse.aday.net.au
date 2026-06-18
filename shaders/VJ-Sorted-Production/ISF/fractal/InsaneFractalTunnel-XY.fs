/*{
    "DESCRIPTION": "InsaneFractalTunnel-XY",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "fractal"
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
        "fractal"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define PI 3.141592653589

void main( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.xy ) - vec2(0.1,0.1) - mouse / 1.0;
	float r = (position.x*position.x+position.y*position.y);
	position = vec2(position.x/r, position.y/r);

	float color = 0.0;
	
	float t2 = time;
	float t2e = 5.0*exp(t2-floor(t2));
	
	color += (sin( position.x*t2e * cos( time /1.0 ) * 1.0 ) + cos( position.y*t2e * cos( time / 1.1 ) * 1.0 ))*(1.0-cos(t2*2.0*PI));
	
	t2 += 0.33;
	t2e = 0.1*exp(t2-floor(t2));
	
	color += (sin( position.y*t2e * sin( time / 1.0 ) * 1.0 ) + cos( position.x*t2e * sin( time / 1.1 ) * 1.0 ))*(1.0-cos(t2*2.0*PI));

	t2 += 0.33;
	t2e = 0.1*exp(t2-floor(t2));
	
	color += (sin( position.x*t2e * sin( time / 1.0 ) + 1.0 ) + sin( position.y*t2e * sin( time / 1.0 ) * 1.0 ))*(1.0-cos(t2*2.0*PI));
	r *= 1.0;
	color *= r/sqrt(0.0+r+r);

	gl_FragColor = vec4( vec3( color, color * 1.5, sin( color + time / 1.0 ) * 1.75 ), 1.0 );

}
