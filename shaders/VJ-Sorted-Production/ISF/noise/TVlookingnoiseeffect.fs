/*{
    "DESCRIPTION": "TVlookingnoiseeffect",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "noise"
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
        "noise"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

//TV looking noise effect
//By ninjapretzel

float random(in float a, in float b) { return fract((cos(dot(vec2(a,b) ,vec2(12.9898,78.233))) * 43758.5453)); }

void main( void ) {
	vec2 pos = ( gl_FragCoord.xy / resolution.xy );
	//pos += .1 * vec2(1. * sin(time), 1. * cos(time));
	pos *= 3.;
	
	float c = 0.;
	c += sin(pos.x * 30.);
	c += 4. * sin(time * 4. + pos.y * 40.);
	c += 9. * sin(time * 1. + pos.y * 100.);
	c += 9. * sin(time * 3. + pos.y * 350.);
	
	c += 1. * cos(time * 3. + pos.x * 10.);
	c *= sin(pos.x);
	c *= sin(pos.y);
	
	pos += time;
	
	float r = random(pos.x, pos.y);
	float g = random(pos.x * 3., pos.y * 3.);
	float b = random(pos.x * 9., pos.y * 9.);
	
	vec4 color = vec4(r*c, b*c, g*c, 1);
	
	gl_FragColor = color;
}
