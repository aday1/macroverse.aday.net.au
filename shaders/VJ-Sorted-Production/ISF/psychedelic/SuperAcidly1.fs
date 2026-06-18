/*{
    "DESCRIPTION": "SuperAcidly1",
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
precision highp float;
#endif

// interfered with by @danbri - split the colour channels
float random(vec2 seed)
{
	return fract(sin(dot(seed, vec2(2.2, 5.3))) * 6523.4);
}

float dots(vec2 pos)
{
	return step(0.5, fract(pos.x * resolution.x / 100.0)) + step(0.5, fract(pos.y * resolution.y / 100.0));
}

vec2 distort0(vec2 pos)
{
	return pos;
}

vec2 distort1(vec2 pos)
{
	return vec2(sin(pos.y * 15.0 + (time)) * 0.10 + pos.x, sin(pos.x * 10.0 + (time)) * 0.10 + pos.y);
}

vec2 distort2(vec2 pos)
{
	return pos / (length(pos) + (sin(time) + 1.1));
}

void main( void )
{
	vec2 position = (gl_FragCoord.xy / resolution.xy - vec2(0.5)) * 2.0;
	vec3 hippy = vec3( 1. - dots(distort1(distort1(distort1(position.xy))))  , dots(distort2(distort2(position.xy - ( mouse.xy-.5)))) , dots(distort1(distort2(position.xy / mouse.xy))) );
        vec3 bw = vec3 ( dots(position.xy+ .05*time) );
	vec3 red = vec3 ( dots(distort0(position.xy)) , 0.,0. );
	gl_FragColor = vec4(  hippy * (bw  + red) , 1.0 );
}
