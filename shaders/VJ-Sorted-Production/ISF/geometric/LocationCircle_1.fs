/*{
    "DESCRIPTION": "LocationCircle",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "geometric"
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
        "geometric",
        "texture-input"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif
uniform sampler2D 	renderbuffer;

float line(vec2 p, vec2 a, vec2 b)
{
	b = b - a;
	a = p - a;
	return 1.-min(dot(distance(a, b * clamp(dot(a, b) / dot(b, b), 0., 1.)), 340.), 1.);
}

mat2 rmat(float t)
{
	float c = cos(t);
	float s = sin(t);
	return mat2(c, s, -s, c);
}

float mix_angle( float angle, float target, float rate)
{    
	angle 		= abs( angle - target - 1. ) < abs( angle + target ) ? angle - 1. 	: angle;
	angle 		= abs( angle - target + 1. ) < abs( angle - target ) ? angle + 1. 	: angle;
	rate 		= rate < .25 && abs(angle-target) > rate? rate * 1./abs(angle-target) 	: rate; //forced convergence for small interpolants
	return fract(mix(angle, target, rate));
}

float unit_atan(in float x, in float y)
{
	return atan(x, y)*.159154943+.5;
}

void main() 
{
	vec2 uv			= gl_FragCoord.xy/resolution;
	vec2 aspect		= resolution/min(resolution.x, resolution.y);
	vec2 p			= (uv-.5) * aspect;
	vec2 m			= (mouse-.5) * aspect;

	float phase		= unit_atan(p.x, p.y);
	float magnitude		= length(p*2.);
	
	float mphase		= unit_atan(m.x, m.y);
	float mmagnitude	= length(m*2.);

	float target		= mphase;

	vec4 memory		= texture2D(renderbuffer, vec2(.5)/resolution/resolution);	
	float follow		= mix_angle(memory.x, target, .005);
	float range		= abs(target-follow);
	range			= range > .5 ? 1.-range : range;
	
	float delta		= abs(range-memory.z) * 4.;
	
	if(gl_FragCoord.x < 8. && gl_FragCoord.y < 8.)
	{
		gl_FragColor	= vec4(follow, target, range, 0.);
	}
	else if(gl_FragCoord.x < 8.)
	{
		float x 	= floor(gl_FragCoord.x);
		
		gl_FragColor.x	= abs(x - 0.) < 1. ? float(follow>uv.y) : gl_FragColor.x;
		gl_FragColor.y	= abs(x - 2.) < 1. ? float(target>uv.y) : gl_FragColor.y;
		gl_FragColor.z	= abs(x - 4.) < 1. ? float(range>uv.y) 	: gl_FragColor.z;
		gl_FragColor.xy	= abs(x - 6.) < 1. ? vec2(delta>uv.y) 	: gl_FragColor.xy;
		gl_FragColor.xy += gl_FragColor.z * .25;
		gl_FragColor.w	= 1.;

	}
	else
	{		
		float len		= .47;
		vec2 target_pos		= normalize(m) * len;
		vec2 follow_pos		= normalize(vec2(0., -1.) * rmat(follow * (8.*atan(1.)))) * len;
		float target_line	= line(p, vec2(0.), target_pos);	
		float follow_line	= line(p, vec2(0.), follow_pos);
		float secant_line	= line(p, target_pos, follow_pos);
		
		float mask		= float(magnitude < .95);
		float ring_mask		= float(magnitude > .95 && magnitude < 1.);
		float ring		= float(phase) * ring_mask;
		
		gl_FragColor 		= vec4(0.,0.,0., 1.);
		gl_FragColor.x		+= follow_line;
		gl_FragColor.y		+= target_line;
		gl_FragColor.z		+= secant_line;
		gl_FragColor.xyz	*= mask;
		gl_FragColor		+= ring;
	}
}//sphinx

