/*{
    "DESCRIPTION": "effectringblobs",
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

const float thresh = 2.0;
const float lowThresh = 0.25;

// effect ringblobs

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.x ) - vec2(0.5, 0.5 * (resolution.y / resolution.x));

	vec3 rgb = vec3(0.0);
	float c = 0.0;
	for (int i=0; i<64; i++)
	{
		vec2 pos = p + vec2(sin(float(i*i) + 1.75 * time) * 0.45, sin(float(i*i*i) + 0.65 * inversesqrt(time)) * 0.2667);
		c += pow(0.175 / length(pos/0.125), 4.0);
	}
	if (c > 1.0 && c < thresh) c = (thresh - c) / (thresh - 1.0);
	if (c >= thresh)
	{
		// do effect
		c = mod(sin(p.x * 10.0) + sin(p.y * 54.0) + sin(p.x * 111.0 + sin(p.y * 96.0 + 11.0 * time)) + sin(p.y * 64.0 + 33.0 * time + sin(p.x * 72.0 + p.y * 90.0)) + 4.0, 2.0);
		if (c > 1.0) c = 2.0 - c;
		rgb = vec3(c, c * 2.0, c * 3.0);
	}
	else if (c > lowThresh)
	{
		rgb = vec3(c);
	}
	else
	{
		float gr = p.x + p.y;
		float rx = floor(sin(gr * 15.0) + sin(gr * 25.0 - 3.0 * time) * 1.125);
		float ry = floor(sin(gr * 25.0 + time) + sin(gr * 35.0 + 4.0 * time) * 3.333);
		float rz = floor(sin(gr * 45.0) + sin(gr * 20.0 + sin(gr * 30.0 + 2.0 * time)) * 1.5);
		vec3 raster = vec3(rx, ry, rz) * 0.25;
		     raster -= vec3(ry, rz, rx) * 0.25;
		float d = c / lowThresh;
		rgb = d * vec3(c) + (1.0 - d) * raster;
	}
	
	gl_FragColor = vec4(rgb, 1.0);

}
