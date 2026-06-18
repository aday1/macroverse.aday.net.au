/*{
    "DESCRIPTION": "StarField-DotMatrix-24",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "particles"
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
        "particles"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
// ;-)
// sample code from shadertoy; 
// Modified by Gigatron Amiga Rules !   ---  Just need a Sinusscroll and a bounceing logo and you got a Amiga Cracktro
#ifdef GL_ES
precision mediump float;
#endif

float rand (in vec2 uv) { return fract(sin(dot(uv,vec2(12.4124,48.4124)))*48512.41241); }
const vec2 O = vec2(0.,1.);
float noise (in vec2 uv) {
	vec2 b = floor(uv);
	return mix(mix(rand(b),rand(b+O.yx),.5),mix(rand(b+O),rand(b+O.yy),.5),.5);
}

#define DIR_RIGHT -1.
#define DIR_LEFT 1.
#define DIRECTION DIR_LEFT

#define LAYERS 8
#define SPEED 50.
#define SIZE 5.

void main()
{
	float x = gl_FragCoord.x;
	float y = gl_FragCoord.y;
    vec2 p = gl_FragCoord.xy / resolution.xy;
	vec2 c = p - vec2(0.25, 0.5);

    //Another amiga/atari copper fx 
    	
    float coppers = time*10.0;
    float rep = 8.;// try 8 16 32 64 128 256 ...
    vec3 col2 = vec3(0.5 + 0.5 * sin(x/rep + 3.14 + coppers), 0.5 + 0.5 * cos (x/rep + coppers), 0.5 + 0.5 * sin (x/rep + coppers));
    vec3 col3 = vec3(0.5 + 0.5 * sin(x/rep + 3.14 - coppers), 0.5 + 0.5 * cos (x/rep -coppers), 0.5 + 0.5 * sin (x/rep - coppers));
    vec3 col4 = vec3(0.5 + 0.5 * sin(y/rep + 3.14 + coppers), 0.5 + 0.5 * cos (y/rep + coppers), 0.5 + 0.5 * sin (y/rep + coppers));
    vec3 col5 = vec3(0.5 + 0.5 * sin(y/rep + 3.14 - coppers), 0.5 + 0.5 * cos (y/rep -coppers), 0.5 + 0.5 * sin (y/rep - coppers));

   	if ( p.y > 0.985 && p.y < 1.0 ) gl_FragColor = vec4 ( col3, 1.0 );
	   
   	if ( p.x > 0.990 && p.x < 1.0 ) gl_FragColor = vec4 ( col4, 1.0 );
		
	if ( p.y > 0.0 && p.y < .02)    gl_FragColor = vec4 ( col2, 1.0 );
	
	if ( p.x > 0.0 && p.x < .01 && p.y<.985) gl_FragColor = vec4 ( col5, 1.0 );

	{// stars forever
	vec2 uv = ( gl_FragCoord.xy / resolution.xy )*SIZE;
	
	float stars = 0.;
	float fl, s;
	for (int layer = 0; layer < LAYERS; layer++) {
		fl = float(layer);
		s = (300.-fl*30.);
		stars += step(.1,pow(noise(mod(vec2(uv.x*s + time*SPEED*DIRECTION - fl*100.,uv.y*s),resolution.x)),18.)) * (fl/float(LAYERS));
	}
	gl_FragColor += vec4( vec3(stars), 1.0 );
	
	}

}
 
