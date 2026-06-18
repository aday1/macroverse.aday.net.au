/*{
    "DESCRIPTION": "CipherSpiral99",
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

// WHAT DID I DO

#ifdef GL_ES
precision mediump float;
#endif

//

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))

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
