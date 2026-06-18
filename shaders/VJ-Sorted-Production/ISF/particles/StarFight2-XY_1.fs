/*{
    "DESCRIPTION": "StarFight2-XY",
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
        "space",
        "particles"
    ]
}*/





#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(0.0)
#define resolution RENDERSIZE
precision mediump float;

// DoubleStar
// originally created by inigo quilez - iq/2013
// (https://www.shadertoy.com/view/4dfGRn)
// glslsandbox mod by Robert Schütze - trirop/2015
// (http://glslsandbox.com/e#29622.0)
// absurd tweaks by bpt
// (https://www.shadertoy.com/view/Mdt3Rj)
// rotation added by I.G.P.
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.

#ifdef GL_ES
precision highp float;
#endif

#define size 1.1 
#define rotationspeed 0.2
uniform float speed; // @expose 0 5

vec2 rotate(in vec2 v, in float angle) 
{
  float ca = cos(angle);
  float sa = sin(angle);
  return vec2( ca*v.x + sa*v.y, -sa*v.x + ca*v.y);
}

// ehh. colors
void main(void)
{
	vec2 p = gl_FragCoord.xy / resolution.xy;
	vec4 dmin = vec4(1000.);
	vec2 z = (1.6*p - 0.8)*vec2(1.7,1.0)*size;
	
	z = rotate(z, rotationspeed * time);
	
	float w = .1 * p.x * p.y;
	vec2 op = 1.-p;
	vec2 mv = ((mouse-vec2(1.2))+vec2(1.-acos(op.x*w),1./acos(op.y*w)));
	for( int i=0; i<8; i++ )
	{
		z = mv + vec2(z.x*z.x-z.y*z.y, 2.0*z.x*z.y);
		mv /= (dmin.x+dmin.y);//10.5;
		z += z*.5;
		dmin=min(dmin, vec4(abs(0.0+z.y+0.5*sin(z.x+time*5.))
				   ,abs(1.0+z.x+0.5*sin(z.y+time))
				   ,dot(z,z)
				   ,length(fract(z)-0.5)));
	}	
	vec3 color = vec3( mix(vec3(dot(dmin.rgb, -dmin.gba)), dmin.rgb, 1.0-dmin.a) );
	color = mix( color, vec3(1.00,1.00,0.00),  1.00-min(1.0,pow(dmin.z*1.00,0.15)));
	color = mix( color, vec3(0.00,1.00,1.00),  1.00-min(1.0,pow(1.0-dmin.x*0.25,18.20)));
	color = mix( color, vec3(-1.00,0.00,1.00),  1.00-min(1.0,pow(dmin.y*0.50,.1250)));
	color = mix( color, vec3(1.00,1.00,0.00),  1.00-min(1.0,pow(dmin.z*1.00,0.115)));
	color = 1.25 * color*color*color*color;
	color.r *= color.r;
	gl_FragColor = vec4(1.0-color.rgb*(0.5 + 0.5*pow(16.0*p.x*(1.0-p.x)*p.y*(1.0-p.y),1.5)),1.0);
}


