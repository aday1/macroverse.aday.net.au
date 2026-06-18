/*{
    "DESCRIPTION": "HorizonLight-FrostCrystal-8",
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
precision mediump float;
#endif

//MrOMGWTF
//play together with music, sync the tempo. works nicely.

uniform sampler2D bb;

const float bpm = 128.0;
const float bpmmult = bpm / 60.0;

//thanks iq
float impulse( float k, float x )
{
    float h = k*x;
    return h*exp(1.0-h);
}

float line(vec2 p, float r, float w)
{
	float ret = abs(p.x * sin(r) + p.y * cos(r));
	return ret < w ? pow(max(0.0, min(1.0, (1.0 - ret / w))), 20.0) : 0.0;
}

void main( void ) {
	vec2 uv = gl_FragCoord.xy / resolution.xy;
	vec2 p = ( gl_FragCoord.xy / resolution.xy * 2.0 - 1.0 );
	float trigger1 = (pow(sin(time), 0.35)) * sign(sin(time));
	p *= pow(abs(sin(time))*4.0, 0.35);
	p *= 1.0 - impulse(20.0, mod(time*bpmmult, 1.0))*0.2;
	p.x *= resolution.x / resolution.y;
	float h = 0.0;
	float c = sin(time*0.5)*1.5;
	for(int i = 0; i < 50; i++){
		h += line(p + c, c, sin(time*3.0)*0.05 + 0.25);
		c += 0.1;
	}
	vec3 horizon = vec3(0.2, 0.8, 0.7);
	vec3 zenith = vec3(0.15, 0.4, 0.5);
	vec3 final = mix(horizon, zenith, abs(p.y))*(0.15+(1.0 - trigger1)*0.05);
	final += h*trigger1;
	float size = 0.01;
	final += vec3(texture2D(bb, uv-vec2(size, 0.0)).r*0.2, 0.0, 0.0);
	final += vec3(0.0, 0.0, texture2D(bb, uv+vec2(size, 0.0)).b*0.2);
	final += texture2D(bb, uv-vec2(size, 0.1)).rgb*0.2;
	final += texture2D(bb, uv+vec2(size, 0.1)).rgb*0.2;
	final = max(vec3(0.0), min(vec3(1.0), final));
	vec3 curves = mix(vec3(0.8, 0.7, 1.0), vec3(1.0, 0.8, 0.7), sin(time)*0.5 + 0.5);
	final = pow(final, curves*0.7);
	final *= vec3( pow((2.0 - length(uv*2.0-1.0))*0.5, 0.5) );
	final *= 1.3;
	gl_FragColor = vec4( vec3( final ) , 1.0 );

}
