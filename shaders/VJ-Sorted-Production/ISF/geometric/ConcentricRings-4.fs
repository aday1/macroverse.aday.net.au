/*{
    "DESCRIPTION": "ConcentricRings-4",
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
        }
    ],
    "TAGS": [
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

const float thresh = 2.0;
const float lowThresh = 0.25;

// effect ringblobs

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.x ) - vec2(0.5, 0.5 * (resolution.y / resolution.x));

	vec3 rgb = vec3(0.0);
	float c = 0.0;
	for (int i=0; i<16; i++)
	{
		vec2 pos = p + vec2(sin(float(i*i) + 0.5 * time) * 0.4, sin(float(i*i*i) + 0.7 * time) * 0.2);
		c += pow(0.08 / length(pos), 8.0);
	}
	if (c > 1.0 && c < thresh) c = (thresh - c) / (thresh - 1.0);
	if (c >= thresh)
	{
		// do effect
		c = mod(sin(p.x * 80.0) + sin(p.y * 50.0) + sin(p.x * 16.0 + sin(p.y * 32.0 + 4.0 * time)) + sin(p.y * 24.0 + 3.0 * time + sin(p.x * 56.0 + p.y * 48.0)) + 4.0, 2.0);
		if (c > 1.0) c = 2.0 - c;
		rgb = vec3(c, c * 2.0, c * 4.0);
	}
	else if (c > lowThresh)
	{
		rgb = vec3(c);
	}
	else
	{
		float gr = p.x + p.y;
		float rx = floor(sin(gr * 15.0) + sin(gr * 25.0 - 3.0 * time) * 8.0);
		float ry = floor(sin(gr * 25.0 + time) + sin(gr * 35.0 + 4.0 * time) * 4.0);
		float rz = floor(sin(gr * 45.0) + sin(gr * 20.0 + sin(gr * 30.0 + 2.0 * time)) * 3.0);
		vec3 raster = vec3(rx, ry, rz) * 0.5;
		float d = c / lowThresh;
		rgb = d * vec3(c) + (1.0 - d) * raster;
	}
	
	gl_FragColor = vec4(rgb, 1.0);

}
