/*{
    "DESCRIPTION": "SuperDisco1",
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
#ifdef GL_ES
precision mediump float;
#endif

//Bouncy Time!

vec3 hsv2rgb(vec3 c)
{
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

float rand(vec2 n) { 
    return fract(sin(dot(n, vec2(12.9898, 4.1414))) * 43758.5453);
}

void main( void ) {
	
	vec2 pos = ( gl_FragCoord.xy / resolution.xy ) - vec2(0.5,0.5);	
        float horizon = 0.5 + .56 * cos(time * 3.); 
        float fov = .3;
	float scaling = 1.5 + 1. * sin(time * 3.);
	
	vec3 p = vec3(pos.x, fov, pos.y - horizon);      
	vec2 s = vec2(p.x/p.z, p.y/p.z) * scaling;
	vec2 sr;
	float t = time * 1.0;
	sr.x = s.x * cos(t) - s.y * sin(t);
	sr.y = s.x * sin(t) + s.y * cos(t);
	vec2 srm = mod(sr * 20., 2.0) - 1.0;
	
	float hue = rand(floor(sr * 10. + 0.5) + 100.);
	float bright = mod(rand(floor(sr * 10. + 0.5) + 150.) - time * 2.0, 1.0);
	
	//checkboard texture
	float pattern = 0.0 + sqrt(abs(srm.x)) * sqrt(abs(srm.y)) * bright;
	float color = pos.y < horizon ? pattern : 0.0;	
	//fading
	//color *= p.z*p.z*10.0;
	
	gl_FragColor = vec4( hsv2rgb(vec3(hue, 1.0, color)), 1.0 );

}
