/*{
    "DESCRIPTION": "StarFight-XY",
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

// StarFight rotation & distance variation added by I.G.P.
// JuliaTraps2 originally created by inigo quilez - iq/2013
//   https://www.shadertoy.com/view/4dfGRn
// glslsandbox mod by Robert Schütze - trirop/2015
//   http://glslsandbox.com/e#29622.0
// duelingPlasmaBalls absurd tweaks by bpt
//   https://www.shadertoy.com/view/Mdt3Rj
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.

#ifdef GL_ES
precision highp float;
#endif

#define size 1.2 
#define rotationspeed 0.5
uniform float speed; // @expose 0 5

vec2 rotate(in vec2 v, in float angle) 
{
  float ca = cos(angle);
  float sa = sin(angle);
  return vec2( ca*v.x + sa*v.y, -sa*v.x + ca*v.y);
}

void main(void)
{
	vec2 p = gl_FragCoord.xy / resolution.xy;
	vec4 dmin = vec4(1000.);
	vec2 z = (1.6*p - 0.8)*vec2(resolution.x/resolution.y,1.0)*(size+mouse.y);
	z = rotate(z, rotationspeed * sin(time) - 0.15);
	
	float w = .1 * p.x * p.y;
	vec2 op = 1.-p;
	float d = 1.5+0.3*cos(time*3.0)+0.2*cos(time*8.0)+0.1*cos(time*12.0);
	vec2 mv = ((mouse-vec2(d))+vec2(1.-acos(op.x*w),1./acos(op.y*w)));
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
  vec3 color = vec3( dmin.w );
  color = mix( color, vec3(1.00,0.80,0.60),     min(1.0,pow(dmin.x*0.25,0.20)) );
  color = mix( color, vec3(0.72,0.70,0.60),     min(1.0,pow(dmin.y*0.50,0.50)) );
  color = mix( color, vec3(1.00,1.00,1.00), 1.0-min(1.0,pow(dmin.z*1.00,0.15) ));
  color = 1.5*color*color;
  color *= 0.5 + 0.5*pow(16.0*p.x*(1.0-p.x)*p.y*(1.0-p.y),0.15);
  gl_FragColor = vec4(color,1.0);  // original colors
  gl_FragColor = vec4(1.0-color.rgb*(0.5 + 0.5*pow(16.0*p.x*(1.0-p.x)*p.y*(1.0-p.y),1.5)),1.0);
}


