/*{
    "DESCRIPTION": "DotMatrix-36",
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

float hash(float x) {
	return fract(sin(x) * 43578.24);
}

float hash2(vec2 uv) {
	return fract( sin(dot(vec2(15.23, 35.36), uv)) * 43578.24);
}

float hash2e(vec2 uv) {
	return fract( sin(15.23 * uv.x + 165.36 * uv.y) * 43578.24);
}
void main( void ) {

	vec2 uv = ( gl_FragCoord.xy / resolution.xy );
	uv.x *= resolution.x / resolution.y;

	float c = hash2e(floor(uv * 10.0));

	gl_FragColor = vec4( c, c, c, 1.0 );

}
