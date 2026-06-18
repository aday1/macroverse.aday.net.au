/*{
    "DESCRIPTION": "DotMatrix-TextGlyph-2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        "abstract",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define PI 3.14159

#define RECT(l,s,p) float(p.x >= l.x && p.x <= l.x+s.x && p.y >= l.y && p.y <= l.y+s.y )

uniform sampler2D backbuffer;

float round(float v)
{
	if(fract(v) >= 0.5){ return ceil(v); }
	return floor(v);
}

float rand(vec2 co){	
    return fract(sin(dot(co.xy,vec2(12.9898,78.233))) * 43758.5453);
}

vec3 pattern(vec2 p,vec2 r,float ch)
{
	vec2 pos = mod(p,r);
	
	float c = 0.0;
	
	if(ch == 0.0)
	{
		c += RECT(vec2(3,2),vec2(8.,12.),pos);
		c -= RECT(vec2(5.,4.),vec2(4.,8.),pos);
	}
	if(ch == 1.0)
	{
		c += RECT(vec2(6.,2.),vec2(2.,12.),pos);
		c += RECT(vec2(4.,12.),vec2(2.,2.),pos);
		c += RECT(vec2(3.,2.),vec2(8.,2.),pos);
	}
	c = clamp(c,0.0,1.0);
	
	float b = (rand(vec2(round((p.x)/16.0+0.5))+time))*0.75+0.25;
	
	return vec3(0,c*b,0);
}

void main( void ) {

	vec2 p = ( gl_FragCoord.xy);

	vec3 color = vec3(0.0);

	vec2 rpos = vec2( round(p.x/16.+0.5), round(p.y/16.+0.5));
	float rch = mod(rpos.x,2.0);
	rch = round(rand(vec2(rpos+time)));
	
	if(p.y < 16.)
	{
		color = pattern(p,vec2(16.),rch);
	}
	
	color += texture2D(backbuffer,(p-vec2(0.0,16.0))/resolution).xyz*0.95;
	
	gl_FragColor = vec4( color, 1.0 );

}
