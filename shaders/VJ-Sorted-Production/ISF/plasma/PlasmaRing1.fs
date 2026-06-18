/*{
    "DESCRIPTION": "PlasmaRing1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "plasma"
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
        },
        {
            "NAME": "u_mouse",
            "TYPE": "vec2",
            "LABEL": "U Mouse"
        }
    ],
    "TAGS": [
        "plasma"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// Shader Remix by Anoki
// Mouse Movement Crazyness
// Original shaders from
// PlayingMarble.glsl
// original code from https://www.shadertoy.com/view/MtX3Ws
// simplified edit: Robert 25.11.2015

// see also https://www.shadertoy.com/view/Mlj3zWprecision mediump float;
// modified color calculation by I.G.P.

#ifdef GL_ES
precision mediump float;
#endif

uniform vec2 u_mouse;

vec3 roty(vec3 p,float a)
{ return p*mat3(cos(a),0,-sin(a),0,1,0,sin(a),0,cos(a)); }

float map(in vec3 p) 
{
	float res=0.;vec3 c = p;
	for (int i = 0; i < 5; i++) 
	{
		p =0.7*abs(p)/dot(p,p) -.7;
		p.yz= vec2(p.y*p.y-p.z*(sin(p.x)/0.2),2.*p.y*p.z);
		res += exp(-10. * abs(dot(p,c)));
	}
	return res/5.0;
}

vec3 raymarch(vec3 ro, vec3 rd)
{
	float t = 5.0;
	vec3 col=vec3(0);float c=0.;
	for( int i=0; i<64; i++ )
	{
		t += 0.02*exp(-2.0*c);
		c = map(ro+t*rd);               
		col = col + 0.08*vec3(c*c, c, c*c*c);  //green	
		col = col + vec3(c*c*c, c*c, c);  //blue
		col = col + vec3(c, c*c*c, c*c);  //red

	}
	return col;
}

void main()
{
    vec2 p = (gl_FragCoord.xy-resolution/2.0)/(resolution.y);
    vec3 ro = roty(vec3(3.),time*0.3);
    vec3 uu = normalize( cross(ro,vec3(1.0, 1.0, 1.0) ) );
    vec3 vv = normalize( cross(uu,ro));
    vec3 rd = normalize( p.x*uu + p.y*vv -ro*0.3 );
    gl_FragColor.rgb = 0.5*log(1.0+raymarch(ro,rd));
    gl_FragColor.a = 1.0;
}

