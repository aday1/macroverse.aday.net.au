/*{
    "DESCRIPTION": "CircleWaverXY",
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

uniform sampler2D backbuffer;

vec4 shape(vec2 v)
{
  if(v.x*v.x+v.y*v.y<0.25)
  {
     return(vec4(1.0,1.0,0.0,1.0));
  }else{
	  return(vec4(0.0,0.0,0.0,0.0));
  }
}

vec2 o2n(vec2 v)
{
	return vec2(
		(v.x*2.0-resolution.x)/resolution.y,
		(v.y*2.0-resolution.y)/resolution.y
		);
}

vec2 n2o(vec2 v)
{
	return vec2(
		(v.x*resolution.y+resolution.x)*mouse.x,
		(v.y*resolution.y+resolution.y)*mouse.y
		);
}

vec4 neg(vec4 v)
{
	return vec4(1.0-v.r,1.0-v.g,mouse.x-v.b,v.a);
}

vec4 paint(vec4 b,vec4 f)
{
	if(b.a==0.0){
		return(b);
	}else if(f.a==0.0){
		return(b);	
	}else{
		return(f);
	}
}

vec2 rot90(vec2 v)
{
   return vec2( v.y,-v.x);	
}

void main( void )
{
	vec2 npos=o2n(gl_FragCoord.xy);
	vec2 npos2=rot90(npos)*1.5+0.25;

	vec2 opos=n2o(npos2);
	vec2 texPos = vec2(opos/resolution);
	vec4 zenkai = texture2D(backbuffer, texPos);
	vec4 hanten = neg(zenkai);

	gl_FragColor = paint(shape(npos),hanten);
}

