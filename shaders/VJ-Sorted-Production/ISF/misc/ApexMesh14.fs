/*{
    "DESCRIPTION": "ApexMesh14",
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
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

void main( void ) 
{
	vec2 position = (gl_FragCoord.xy - resolution * 1.9) / resolution.yy;
	
	float longest = sqrt(float(resolution.x*resolution.x) + float(resolution.y*resolution.y))*0.5;
	float dx = gl_FragCoord.x-resolution.x/2.0;
	float dy = 0.2+gl_FragCoord.y-resolution.y/2.0;
	float len = sqrt(dx*dx+dy*dy);
	float ds = len/longest;
	float md = time*2.0;
	
	float ang = -2.0*atan(dy,(len+dx));
	ang += pow(len, 0.5)*5.0;
	
	float red = (128.0 - sin(ang + md*3.141592*2.0) * 127.0)*(1.0-ds);
	float green = (128.0 - cos(ang + md*3.141592*2.0) * 127.0)*(1.0-ds);
	float blue = (128.0 + sin(ang  + md*3.141592*2.0) * 127.0)*(1.0-ds);

	gl_FragColor = vec4( vec3( red/255.0, green/255.0, blue/255.0), 1.0 );

}
