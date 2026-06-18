/*{
    "DESCRIPTION": "WHATDIDIDO",
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

// WHAT DID I DO


//

//#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)

void main( void ) 
{
  vec2 uv = ( gl_FragCoord.xy / resolution.xy ) * 2.0 - 1.0;
  uv.x *= resolution.x/resolution.y;
	
  float a = 4.+atan( uv.y / uv.x );
  float r = length( uv+sin(time*2.)*5. );
  float t = .9*abs( sin(((a + 2.0*sin(time*0.)*r)*3.0))*0.6 );
    
 vec3 C = (3.0-r) / vec3( sin(time*2.)*20.*t, cos(time*2.)*20.*t, tan(time*2.)*20.*t );
  
  gl_FragColor = vec4( C, 1.0 );

}
