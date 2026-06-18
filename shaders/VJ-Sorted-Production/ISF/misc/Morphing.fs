/*{
    "DESCRIPTION": "Morphing",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "misc"
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
        }
    ],
    "TAGS": [
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

// EARTHBOUND SOMEONE?
// Edits courtesy by @Eiyeron.

#define BLADES 20.0
#define BIAS 1.0
#define SHARPNESS 4.0
#define COLOR 0.54, 0.72, 0.96
#define BG 0.34, 0.52, 0.76

void main( void ) {

	vec2 position1 = (( gl_FragCoord.xy / resolution.xy ) - vec2(0.5)) / vec2(resolution.y/resolution.x,1.0);
	position1.x += 0.3*cos(position1.y*5. + time/5.);
	vec2 position2 = (( gl_FragCoord.xy / resolution.xy ) - vec2(0.5)) / vec2(resolution.y/resolution.x,1.0);
	position2.x -= 0.3*cos(position2.y*5. + time/5.);
	vec3 color = vec3(0.5);
	
	float bladeh = mod(gl_FragCoord.y, 2.0) * clamp(pow(sin(time+atan(position1.x,position1.y)*BLADES)+BIAS, SHARPNESS), 0.0, 1.0);
	float bladev = mod(gl_FragCoord.y+1., 2.0) * clamp(pow(sin(time+atan(position2.x,position2.y)*BLADES)+BIAS, SHARPNESS), 0.0, 1.0);
	
	color = mix(vec3(COLOR)+vec3(sin(time/3.), cos(time/4.), cos(time/5.)), vec3(BG), bladev + bladeh);
	
	gl_FragColor = vec4( color, 1.0 );

}
