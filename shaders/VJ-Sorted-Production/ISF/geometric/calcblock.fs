/*{
    "DESCRIPTION": "calcblock",
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
        "geometric"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float random(vec2 v) 
{
return fract(sin(dot(v ,vec2(3.9898,78.233))) * (10000.0+time*0.05));
}

void main( void ) 
{
	const int numColors = 3;
	vec3 colors[numColors];
	colors[0] = vec3( 0.3, 0.8, 0.8);
	colors[1] = vec3( 0.8, 0.9, 0.4);
	colors[2] = vec3( 0.4, 0.4, 0.5);
	
	vec2 screenPos = gl_FragCoord.xy;
	vec2 screenPosNorm = gl_FragCoord.xy / resolution.xy;
	vec2 position = screenPosNorm + mouse / 4.0;
	
	// calc block
	vec2 screenBlock0 = floor(screenPos*0.16 + vec2(time,0) + time*2.0);
	vec2 screenBlock1 = floor(screenPos*0.08 + vec2(time*1.5,0) + time*4.0);
	vec2 screenBlock2 = floor(screenPos*0.02 + vec2(time*2.0,0)+time*2.0);
	float rand0 = random(screenBlock0);
	float rand1 = random(screenBlock1);
	float rand2 = random(screenBlock2);
	
	float rand = rand1;
	if ( rand2 < 0.05 ) { rand = rand2; }
	
	// block color
	vec3 color = mix( colors[0], colors[1], pow(rand,5.0) );
	if ( rand < 0.05 ) { color=colors[2]; }
	
	float vignette = 1.6-length(screenPosNorm*2.0-1.0);
	vec4 finalColor = vec4(color*vignette, 1.0);
	
	gl_FragColor = finalColor;
}
