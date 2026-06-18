/*{
    "DESCRIPTION": "Zooming-2",
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
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

void main( void ) {
	//Anastasia Dunbar testing endless frames.
	vec2 uv = ( gl_FragCoord.xy / resolution.xy );
	vec2 tri = abs(1.-(uv*2.));
	float zoom = min(pow(2.,floor(-log2(tri.x))),pow(2.,floor(-log2(tri.y))));
	float zoom_id = log2(zoom)+1.;
	float div = ((pow(2.,((-zoom_id)-1.))*((-2.)+pow(2.,zoom_id))));
	float a = (((uv.x)-(div))*zoom);
	float b = (((uv.y)-(div))*zoom);
	gl_FragColor = vec4(a,b,0.,1.);

}
