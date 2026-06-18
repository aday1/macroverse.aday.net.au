/*{
    "DESCRIPTION": "WeirdPlasmaLiekZoom",
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
        "plasma"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

void main( void ) {

    vec2 position = gl_FragCoord.xy / resolution.xy;
    float frequency = sin(time)*4.0;
    vec2 nearest = 2.0*fract(frequency * cos(position+time)) - 1.0;
    float dist = length(nearest);
    float radius = cos(time);
    vec3 white = vec3(1.0, 0.0, 1.0);
    vec3 black = vec3(0.0, sin(time), sin(time));
    vec3 fragcolor = mix(black, white, smoothstep(radius, abs(sin(time)), sin(dist)));
    gl_FragColor = vec4(fragcolor, 1.0);
}
