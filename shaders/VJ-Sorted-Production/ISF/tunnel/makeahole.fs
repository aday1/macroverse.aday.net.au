/*{
    "DESCRIPTION": "makeahole",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "tunnel"
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
        "tunnel"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// XOR'd carpets

#ifdef GL_ES
precision mediump float;
#endif

bool hit(vec2 p)
{
    float direction = 0.1; // -1.0 to zoom out
    ivec2 sectors;
    const int lim = 5;
    vec2 coordIter = p / pow(3.0, mod(direction*time, 1.0));
	
    for (int i=0; i < lim; i++) {
        sectors = ivec2(floor(coordIter.xy * 3.0));
        if (sectors.x == 1 && sectors.y == 1) {
            // make a hole
            return false;
        } else {
            // map current sector to whole carpet
            coordIter.xy = coordIter.xy * 3.0 - vec2(sectors.xy);
        }
    }

    return true;
}

void main(void)
{
    vec2 coordOrig = abs(gl_FragCoord.xy / resolution.xy-0.5);
    coordOrig.y *= resolution.y / resolution.x;
    coordOrig = mod(coordOrig, 1.0);
	vec4 color = vec4(1.0);
	for(float i = 0.; i < 4.; i++) {
		if (hit(i*0.1+coordOrig))
			color = 1.0 - color;
	}
    
    gl_FragColor = color;
}
