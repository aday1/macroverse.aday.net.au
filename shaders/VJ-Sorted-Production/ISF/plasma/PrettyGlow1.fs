/*{
    "DESCRIPTION": "PrettyGlow1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "plasma"
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
        "color",
        "plasma"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// By: Brandon Fogerty
// bfogerty at gmail dot com

#ifdef GL_ES
precision mediump float;
#endif

void main( void ) 
{
	vec2 p = ( gl_FragCoord.xy / resolution.xy ) * 2.0 - 1.0 + sin(mod(time, 6.28)) / 10.;
	
	vec3 c = vec3( 0.0 );
	
	float amplitude = 0.30 + sin(mod(time, 6.28))/7.; 
	float glowT = sin(time) * 0.5 + 0.5 / (2. + sin(time));
	float glowFactor = mix( 0.1 + (0.1 + sin(mod(time*9., 6.28))/10.) , 0.35 + (0.1 + sin(mod((time + 8.)*3., 6.28))/10.), glowT );
	c += vec3(0.02 + cos(mod((time+ 21.)*3.0, 6.28))/10., 0.03, 0.13 + sin(mod(time*2.0, 6.28))/10.) * ( glowFactor * abs( 1.0 / sin(p.x + sin( p.y + time ) * amplitude ) ));
	c += vec3(0.02, 0.10 + cos(mod((time+ 21.)*3.0, 6.28))/10. , 0.03+ cos(mod((time+ 11.)*3.0, 6.28))/10.) * ( glowFactor * abs( 1.0 / sin(p.x + sin( p.y + time+1.00 ) * amplitude+0.1 ) ));
	c += vec3(0.02, 0.20 + cos(mod((time+ 21.)*3.0, 6.28))/10., 0.03) * ( glowFactor * abs( 1.1 / sin(p.x + sin( p.y + time+2.00 ) * amplitude+0.2 ) ));
	
	gl_FragColor = vec4( c, 1.9);

}
