/*{
    "DESCRIPTION": "ZenithPrism30",
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

#define p 0.15915494309
void main( void ) 
{
	vec2 position = ( gl_FragCoord.xy / resolution.x );
	float a = max(sign(mod((atan(-position.y+0.25,position.x-0.5)*p+0.5)*5.0+time/2.0,1.0)-0.5),0.0);
	float r = length(position-vec2(0.5,0.25));
	gl_FragColor = vec4( a*-sign(r-0.2+cos(time*2.0)/30.0),0.8+a*-sign(r-0.3+cos(time*2.0)/30.0),0.9+a*-sign(r-0.4+cos(time*2.0)/30.0), 1.0 );
}
